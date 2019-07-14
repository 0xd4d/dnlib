// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using dnlib.Threading;

namespace dnlib.DotNet {
	/// <summary>
	/// Resolves assemblies
	/// </summary>
	public class AssemblyResolver : IAssemblyResolver {
		static readonly ModuleDef nullModule = new ModuleDefUser();

		// DLL files are searched before EXE files
		static readonly string[] assemblyExtensions = new string[] { ".dll", ".exe" };
		static readonly string[] winMDAssemblyExtensions = new string[] { ".winmd" };

		static readonly List<GacInfo> gacInfos;
		static readonly string[] extraMonoPaths;
		static readonly string[] monoVerDirs = new string[] {
			// The "-api" dirs are reference assembly dirs.
			"4.5", @"4.5\Facades", "4.5-api", @"4.5-api\Facades", "4.0", "4.0-api",
			"3.5", "3.5-api", "3.0", "3.0-api", "2.0", "2.0-api",
			"1.1", "1.0",
		};

		ModuleContext defaultModuleContext;
		readonly Dictionary<ModuleDef, List<string>> moduleSearchPaths = new Dictionary<ModuleDef, List<string>>();
		readonly Dictionary<string, AssemblyDef> cachedAssemblies = new Dictionary<string, AssemblyDef>(StringComparer.OrdinalIgnoreCase);
		readonly List<string> preSearchPaths = new List<string>();
		readonly List<string> postSearchPaths = new List<string>();
		bool findExactMatch;
		bool enableFrameworkRedirect;
		bool enableTypeDefCache = true;
		bool useGac = true;
#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		sealed class GacInfo {
			public readonly int Version;
			public readonly string Path;
			public readonly string Prefix;
			public readonly string[] SubDirs;

			public GacInfo(int version, string prefix, string path, string[] subDirs) {
				Version = version;
				Prefix = prefix;
				Path = path;
				SubDirs = subDirs;
			}
		}

		static AssemblyResolver() {
			gacInfos = new List<GacInfo>();

			if (!(Type.GetType("Mono.Runtime") is null)) {
				var dirs = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
				var extraMonoPathsList = new List<string>();
				foreach (var prefix in FindMonoPrefixes()) {
					var dir = Path.Combine(Path.Combine(Path.Combine(prefix, "lib"), "mono"), "gac");
					if (dirs.ContainsKey(dir))
						continue;
					dirs[dir] = true;

					if (Directory.Exists(dir)) {
						gacInfos.Add(new GacInfo(-1, "", Path.GetDirectoryName(dir), new string[] {
							Path.GetFileName(dir)
						}));
					}

					dir = Path.GetDirectoryName(dir);
					foreach (var verDir in monoVerDirs) {
						var dir2 = dir;
						foreach (var d in verDir.Split(new char[] { '\\' }))
							dir2 = Path.Combine(dir2, d);
						if (Directory.Exists(dir2))
							extraMonoPathsList.Add(dir2);
					}
				}

				var paths = Environment.GetEnvironmentVariable("MONO_PATH");
				if (!(paths is null)) {
					foreach (var tmp in paths.Split(Path.PathSeparator)) {
						var path = tmp.Trim();
						if (path != string.Empty && Directory.Exists(path))
							extraMonoPathsList.Add(path);
					}
				}
				extraMonoPaths = extraMonoPathsList.ToArray();
			}
			else {
				var windir = Environment.GetEnvironmentVariable("WINDIR");
				if (!string.IsNullOrEmpty(windir)) {
					string path;

					// .NET Framework 1.x and 2.x
					path = Path.Combine(windir, "assembly");
					if (Directory.Exists(path)) {
						gacInfos.Add(new GacInfo(2, "", path, new string[] {
							"GAC_32", "GAC_64", "GAC_MSIL", "GAC"
						}));
					}

					// .NET Framework 4.x
					path = Path.Combine(Path.Combine(windir, "Microsoft.NET"), "assembly");
					if (Directory.Exists(path)) {
						gacInfos.Add(new GacInfo(4, "v4.0_", path, new string[] {
							"GAC_32", "GAC_64", "GAC_MSIL"
						}));
					}
				}
			}
		}

		static string GetCurrentMonoPrefix() {
			var path = typeof(object).Module.FullyQualifiedName;
			for (int i = 0; i < 4; i++)
				path = Path.GetDirectoryName(path);
			return path;
		}

		static IEnumerable<string> FindMonoPrefixes() {
			yield return GetCurrentMonoPrefix();

			var prefixes = Environment.GetEnvironmentVariable("MONO_GAC_PREFIX");
			if (!string.IsNullOrEmpty(prefixes)) {
				foreach (var tmp in prefixes.Split(Path.PathSeparator)) {
					var prefix = tmp.Trim();
					if (prefix != string.Empty)
						yield return prefix;
				}
			}
		}

		/// <summary>
		/// Gets/sets the default <see cref="ModuleContext"/>
		/// </summary>
		public ModuleContext DefaultModuleContext {
			get => defaultModuleContext;
			set => defaultModuleContext = value;
		}

		/// <summary>
		/// <c>true</c> if <see cref="Resolve"/> should find an assembly that matches exactly.
		/// <c>false</c> if it first tries to match exactly, and if that fails, it picks an
		/// assembly that is closest to the requested assembly.
		/// </summary>
		public bool FindExactMatch {
			get => findExactMatch;
			set => findExactMatch = value;
		}

		/// <summary>
		/// <c>true</c> if resolved .NET framework assemblies can be redirected to the source
		/// module's framework assembly version. Eg. if a resolved .NET 3.5 assembly can be
		/// redirected to a .NET 4.0 assembly if the source module is a .NET 4.0 assembly. This is
		/// ignored if <see cref="FindExactMatch"/> is <c>true</c>.
		/// </summary>
		public bool EnableFrameworkRedirect {
			get => enableFrameworkRedirect;
			set => enableFrameworkRedirect = value;
		}

		/// <summary>
		/// If <c>true</c>, all modules in newly resolved assemblies will have their
		/// <see cref="ModuleDef.EnableTypeDefFindCache"/> property set to <c>true</c>. This is
		/// enabled by default since these modules shouldn't be modified by the user.
		/// </summary>
		public bool EnableTypeDefCache {
			get => enableTypeDefCache;
			set => enableTypeDefCache = value;
		}

		/// <summary>
		/// true to search the Global Assembly Cache. Default value is true.
		/// </summary>
		public bool UseGAC {
			get => useGac;
			set => useGac = value;
		}

		/// <summary>
		/// Gets paths searched before trying the standard locations
		/// </summary>
		public IList<string> PreSearchPaths => preSearchPaths;

		/// <summary>
		/// Gets paths searched after trying the standard locations
		/// </summary>
		public IList<string> PostSearchPaths => postSearchPaths;

		/// <summary>
		/// Default constructor
		/// </summary>
		public AssemblyResolver()
			: this(null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="defaultModuleContext">Module context for all resolved assemblies</param>
		public AssemblyResolver(ModuleContext defaultModuleContext) {
			this.defaultModuleContext = defaultModuleContext;
			enableFrameworkRedirect = true;
		}

		/// <inheritdoc/>
		public AssemblyDef Resolve(IAssembly assembly, ModuleDef sourceModule) {
			if (assembly is null)
				return null;

			if (EnableFrameworkRedirect && !FindExactMatch)
				FrameworkRedirect.ApplyFrameworkRedirect(ref assembly, sourceModule);

#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var resolvedAssembly = Resolve2(assembly, sourceModule);
			if (resolvedAssembly is null) {
				string asmName = UTF8String.ToSystemStringOrEmpty(assembly.Name);
				string asmNameTrimmed = asmName.Trim();
				if (asmName != asmNameTrimmed) {
					assembly = new AssemblyNameInfo {
						Name = asmNameTrimmed,
						Version = assembly.Version,
						PublicKeyOrToken = assembly.PublicKeyOrToken,
						Culture = assembly.Culture,
					};
					resolvedAssembly = Resolve2(assembly, sourceModule);
				}
			}

			if (resolvedAssembly is null) {
				// Make sure we don't search for this assembly again. This speeds up callers who
				// keep asking for this assembly when trying to resolve many different TypeRefs
				cachedAssemblies[GetAssemblyNameKey(assembly)] = null;
				return null;
			}

			var key1 = GetAssemblyNameKey(resolvedAssembly);
			var key2 = GetAssemblyNameKey(assembly);
			cachedAssemblies.TryGetValue(key1, out var asm1);
			cachedAssemblies.TryGetValue(key2, out var asm2);

			if (asm1 != resolvedAssembly && asm2 != resolvedAssembly) {
				// This assembly was just resolved
				if (enableTypeDefCache) {
					var modules = resolvedAssembly.Modules;
					int count = modules.Count;
					for (int i = 0; i < count; i++) {
						var module = modules[i];
						if (!(module is null))
							module.EnableTypeDefFindCache = true;
					}
				}
			}

			bool inserted = false;
			if (!cachedAssemblies.ContainsKey(key1)) {
				cachedAssemblies.Add(key1, resolvedAssembly);
				inserted = true;
			}
			if (!cachedAssemblies.ContainsKey(key2)) {
				cachedAssemblies.Add(key2, resolvedAssembly);
				inserted = true;
			}
			if (inserted || asm1 == resolvedAssembly || asm2 == resolvedAssembly)
				return resolvedAssembly;

			// Dupe assembly. Don't insert it.
			var dupeModule = resolvedAssembly.ManifestModule;
			if (!(dupeModule is null))
				dupeModule.Dispose();
			return asm1 ?? asm2;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Add a module's assembly to the assembly cache
		/// </summary>
		/// <param name="module">The module whose assembly should be cached</param>
		/// <returns><c>true</c> if <paramref name="module"/>'s assembly is cached, <c>false</c>
		/// if it's not cached because some other assembly with the exact same full name has
		/// already been cached or if <paramref name="module"/> or its assembly is <c>null</c>.</returns>
		public bool AddToCache(ModuleDef module) => !(module is null) && AddToCache(module.Assembly);

		/// <summary>
		/// Add an assembly to the assembly cache
		/// </summary>
		/// <param name="asm">The assembly</param>
		/// <returns><c>true</c> if <paramref name="asm"/> is cached, <c>false</c> if it's not
		/// cached because some other assembly with the exact same full name has already been
		/// cached or if <paramref name="asm"/> is <c>null</c>.</returns>
		public bool AddToCache(AssemblyDef asm) {
			if (asm is null)
				return false;
			var asmKey = GetAssemblyNameKey(asm);
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (cachedAssemblies.TryGetValue(asmKey, out var cachedAsm) && !(cachedAsm is null))
				return asm == cachedAsm;
			cachedAssemblies[asmKey] = asm;
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Removes a module's assembly from the cache
		/// </summary>
		/// <param name="module">The module</param>
		/// <returns><c>true</c> if its assembly was removed, <c>false</c> if it wasn't removed
		/// since it wasn't in the cache, it has no assembly, or <paramref name="module"/> was
		/// <c>null</c></returns>
		public bool Remove(ModuleDef module) => !(module is null) && Remove(module.Assembly);

		/// <summary>
		/// Removes the assembly from the cache
		/// </summary>
		/// <param name="asm">The assembly</param>
		/// <returns><c>true</c> if it was removed, <c>false</c> if it wasn't removed since it
		/// wasn't in the cache or if <paramref name="asm"/> was <c>null</c></returns>
		public bool Remove(AssemblyDef asm) {
			if (asm is null)
				return false;
			var asmKey = GetAssemblyNameKey(asm);
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			return cachedAssemblies.Remove(asmKey);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Clears the cache and calls <see cref="IDisposable.Dispose()"/> on each cached module.
		/// Use <see cref="Remove(AssemblyDef)"/> to remove any assemblies you added yourself
		/// using <see cref="AddToCache(AssemblyDef)"/> before calling this method if you don't want
		/// them disposed.
		/// </summary>
		public void Clear() {
			List<AssemblyDef> asms;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			asms = new List<AssemblyDef>(cachedAssemblies.Values);
			cachedAssemblies.Clear();
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
			foreach (var asm in asms) {
				if (asm is null)
					continue;
				foreach (var mod in asm.Modules)
					mod.Dispose();
			}
		}

		/// <summary>
		/// Gets the cached assemblies in this resolver.
		/// </summary>
		/// <returns>The cached assemblies.</returns>
		public IEnumerable<AssemblyDef> GetCachedAssemblies() {
			AssemblyDef[] assemblies;
#if THREAD_SAFE
			theLock.EnterReadLock(); try {
#endif
			assemblies = cachedAssemblies.Values.ToArray();
#if THREAD_SAFE
			} finally { theLock.ExitReadLock(); }
#endif
			return assemblies;
		}

		static string GetAssemblyNameKey(IAssembly asmName) {
			// Make sure the name contains PublicKeyToken= and not PublicKey=
			return asmName.FullNameToken;
		}

		AssemblyDef Resolve2(IAssembly assembly, ModuleDef sourceModule) {
			if (cachedAssemblies.TryGetValue(GetAssemblyNameKey(assembly), out var resolvedAssembly))
				return resolvedAssembly;

			var moduleContext = defaultModuleContext;
			if (moduleContext is null && !(sourceModule is null))
				moduleContext = sourceModule.Context;

			resolvedAssembly = FindExactAssembly(assembly, PreFindAssemblies(assembly, sourceModule, true), moduleContext) ??
					FindExactAssembly(assembly, FindAssemblies(assembly, sourceModule, true), moduleContext) ??
					FindExactAssembly(assembly, PostFindAssemblies(assembly, sourceModule, true), moduleContext);
			if (!(resolvedAssembly is null))
				return resolvedAssembly;

			if (!findExactMatch) {
				resolvedAssembly = FindClosestAssembly(assembly);
				resolvedAssembly = FindClosestAssembly(assembly, resolvedAssembly, PreFindAssemblies(assembly, sourceModule, false), moduleContext);
				resolvedAssembly = FindClosestAssembly(assembly, resolvedAssembly, FindAssemblies(assembly, sourceModule, false), moduleContext);
				resolvedAssembly = FindClosestAssembly(assembly, resolvedAssembly, PostFindAssemblies(assembly, sourceModule, false), moduleContext);
			}

			return resolvedAssembly;
		}

		/// <summary>
		/// Finds an assembly that exactly matches the requested assembly
		/// </summary>
		/// <param name="assembly">Assembly to find</param>
		/// <param name="paths">Search paths or <c>null</c> if none</param>
		/// <param name="moduleContext">Module context</param>
		/// <returns>An <see cref="AssemblyDef"/> instance or <c>null</c> if an exact match
		/// couldn't be found.</returns>
		AssemblyDef FindExactAssembly(IAssembly assembly, IEnumerable<string> paths, ModuleContext moduleContext) {
			if (paths is null)
				return null;
			var asmComparer = AssemblyNameComparer.CompareAll;
			foreach (var path in paths) {
				ModuleDefMD mod = null;
				try {
					mod = ModuleDefMD.Load(path, moduleContext);
					var asm = mod.Assembly;
					if (!(asm is null) && asmComparer.Equals(assembly, asm)) {
						mod = null;
						return asm;
					}
				}
				catch {
				}
				finally {
					if (!(mod is null))
						mod.Dispose();
				}
			}
			return null;
		}

		/// <summary>
		/// Finds the closest assembly from the already cached assemblies
		/// </summary>
		/// <param name="assembly">Assembly to find</param>
		/// <returns>The closest <see cref="AssemblyDef"/> or <c>null</c> if none found</returns>
		AssemblyDef FindClosestAssembly(IAssembly assembly) {
			AssemblyDef closest = null;
			var asmComparer = AssemblyNameComparer.CompareAll;
			foreach (var kv in cachedAssemblies) {
				var asm = kv.Value;
				if (asm is null)
					continue;
				if (asmComparer.CompareClosest(assembly, closest, asm) == 1)
					closest = asm;
			}
			return closest;
		}

		AssemblyDef FindClosestAssembly(IAssembly assembly, AssemblyDef closest, IEnumerable<string> paths, ModuleContext moduleContext) {
			if (paths is null)
				return closest;
			var asmComparer = AssemblyNameComparer.CompareAll;
			foreach (var path in paths) {
				ModuleDefMD mod = null;
				try {
					mod = ModuleDefMD.Load(path, moduleContext);
					var asm = mod.Assembly;
					if (!(asm is null) && asmComparer.CompareClosest(assembly, closest, asm) == 1) {
						if (!IsCached(closest) && !(closest is null)) {
							var closeMod = closest.ManifestModule;
							if (!(closeMod is null))
								closeMod.Dispose();
						}
						closest = asm;
						mod = null;
					}
				}
				catch {
				}
				finally {
					if (!(mod is null))
						mod.Dispose();
				}
			}

			return closest;
		}

		/// <summary>
		/// Returns <c>true</c> if <paramref name="asm"/> is inserted in <see cref="cachedAssemblies"/>
		/// </summary>
		/// <param name="asm">Assembly to check</param>
		bool IsCached(AssemblyDef asm) {
			if (asm is null)
				return false;
			return cachedAssemblies.TryGetValue(GetAssemblyNameKey(asm), out var cachedAsm) &&
					cachedAsm == asm;
		}

		IEnumerable<string> FindAssemblies2(IAssembly assembly, IEnumerable<string> paths) {
			if (!(paths is null)) {
				var asmSimpleName = UTF8String.ToSystemStringOrEmpty(assembly.Name);
				var exts = assembly.IsContentTypeWindowsRuntime ? winMDAssemblyExtensions : assemblyExtensions;
				foreach (var ext in exts) {
					foreach (var path in paths) {
						string fullPath;
						try {
							fullPath = Path.Combine(path, asmSimpleName + ext);
						}
						catch (ArgumentException) {
							// Invalid path chars
							yield break;
						}
						if (File.Exists(fullPath))
							yield return fullPath;
					}
				}
			}
		}

		/// <summary>
		/// Called before <see cref="FindAssemblies"/>
		/// </summary>
		/// <param name="assembly">Assembly to find</param>
		/// <param name="sourceModule">The module that needs to resolve an assembly or <c>null</c></param>
		/// <param name="matchExactly">We're trying to find an exact match</param>
		/// <returns><c>null</c> or an enumerable of full paths to try</returns>
		protected virtual IEnumerable<string> PreFindAssemblies(IAssembly assembly, ModuleDef sourceModule, bool matchExactly) {
			foreach (var path in FindAssemblies2(assembly, preSearchPaths))
				yield return path;
		}

		/// <summary>
		/// Called after <see cref="FindAssemblies"/> (if it fails)
		/// </summary>
		/// <param name="assembly">Assembly to find</param>
		/// <param name="sourceModule">The module that needs to resolve an assembly or <c>null</c></param>
		/// <param name="matchExactly">We're trying to find an exact match</param>
		/// <returns><c>null</c> or an enumerable of full paths to try</returns>
		protected virtual IEnumerable<string> PostFindAssemblies(IAssembly assembly, ModuleDef sourceModule, bool matchExactly) {
			foreach (var path in FindAssemblies2(assembly, postSearchPaths))
				yield return path;
		}

		/// <summary>
		/// Called after <see cref="PreFindAssemblies"/> (if it fails)
		/// </summary>
		/// <param name="assembly">Assembly to find</param>
		/// <param name="sourceModule">The module that needs to resolve an assembly or <c>null</c></param>
		/// <param name="matchExactly">We're trying to find an exact match</param>
		/// <returns><c>null</c> or an enumerable of full paths to try</returns>
		protected virtual IEnumerable<string> FindAssemblies(IAssembly assembly, ModuleDef sourceModule, bool matchExactly) {
			if (assembly.IsContentTypeWindowsRuntime) {
				string path;
				try {
					path = Path.Combine(Path.Combine(Environment.SystemDirectory, "WinMetadata"), assembly.Name + ".winmd");
				}
				catch (ArgumentException) {
					// Invalid path chars
					path = null;
				}
				if (File.Exists(path))
					yield return path;
			}
			else {
				if (UseGAC) {
					foreach (var path in FindAssembliesGac(assembly, sourceModule, matchExactly))
						yield return path;
				}
			}
			foreach (var path in FindAssembliesModuleSearchPaths(assembly, sourceModule, matchExactly))
				yield return path;
		}

		IEnumerable<string> FindAssembliesGac(IAssembly assembly, ModuleDef sourceModule, bool matchExactly) {
			if (matchExactly)
				return FindAssembliesGacExactly(assembly, sourceModule);
			return FindAssembliesGacAny(assembly, sourceModule);
		}

		IEnumerable<GacInfo> GetGacInfos(ModuleDef sourceModule) {
			int version = sourceModule is null ? int.MinValue : sourceModule.IsClr40 ? 4 : 2;
			// Try the correct GAC first (eg. GAC4 if it's a .NET 4 assembly)
			foreach (var gacInfo in gacInfos) {
				if (gacInfo.Version == version)
					yield return gacInfo;
			}
			foreach (var gacInfo in gacInfos) {
				if (gacInfo.Version != version)
					yield return gacInfo;
			}
		}

		IEnumerable<string> FindAssembliesGacExactly(IAssembly assembly, ModuleDef sourceModule) {
			foreach (var gacInfo in GetGacInfos(sourceModule)) {
				foreach (var path in FindAssembliesGacExactly(gacInfo, assembly, sourceModule))
					yield return path;
			}
			if (!(extraMonoPaths is null)) {
				foreach (var path in GetExtraMonoPaths(assembly, sourceModule))
					yield return path;
			}
		}

		static IEnumerable<string> GetExtraMonoPaths(IAssembly assembly, ModuleDef sourceModule) {
			if (!(extraMonoPaths is null)) {
				foreach (var dir in extraMonoPaths) {
					string file;
					try {
						file = Path.Combine(dir, assembly.Name + ".dll");
					}
					catch (ArgumentException) {
						// Invalid path chars
						break;
					}
					if (File.Exists(file))
						yield return file;
				}
			}
		}

		IEnumerable<string> FindAssembliesGacExactly(GacInfo gacInfo, IAssembly assembly, ModuleDef sourceModule) {
			var pkt = PublicKeyBase.ToPublicKeyToken(assembly.PublicKeyOrToken);
			if (!(gacInfo is null) && !(pkt is null)) {
				string pktString = pkt.ToString();
				string verString = Utils.CreateVersionWithNoUndefinedValues(assembly.Version).ToString();
				var cultureString = UTF8String.ToSystemStringOrEmpty(assembly.Culture);
				if (cultureString.Equals("neutral", StringComparison.OrdinalIgnoreCase))
					cultureString = string.Empty;
				var asmSimpleName = UTF8String.ToSystemStringOrEmpty(assembly.Name);
				foreach (var subDir in gacInfo.SubDirs) {
					var baseDir = Path.Combine(gacInfo.Path, subDir);
					try {
						baseDir = Path.Combine(baseDir, asmSimpleName);
					}
					catch (ArgumentException) {
						// Invalid path chars
						break;
					}
					baseDir = Path.Combine(baseDir, $"{gacInfo.Prefix}{verString}_{cultureString}_{pktString}");
					var pathName = Path.Combine(baseDir, asmSimpleName + ".dll");
					if (File.Exists(pathName))
						yield return pathName;
				}
			}
		}

		IEnumerable<string> FindAssembliesGacAny(IAssembly assembly, ModuleDef sourceModule) {
			foreach (var gacInfo in GetGacInfos(sourceModule)) {
				foreach (var path in FindAssembliesGacAny(gacInfo, assembly, sourceModule))
					yield return path;
			}
			if (!(extraMonoPaths is null)) {
				foreach (var path in GetExtraMonoPaths(assembly, sourceModule))
					yield return path;
			}
		}

		IEnumerable<string> FindAssembliesGacAny(GacInfo gacInfo, IAssembly assembly, ModuleDef sourceModule) {
			if (!(gacInfo is null)) {
				var asmSimpleName = UTF8String.ToSystemStringOrEmpty(assembly.Name);
				foreach (var subDir in gacInfo.SubDirs) {
					var baseDir = Path.Combine(gacInfo.Path, subDir);
					try {
						baseDir = Path.Combine(baseDir, asmSimpleName);
					}
					catch (ArgumentException) {
						// Invalid path chars
						break;
					}
					foreach (var dir in GetDirs(baseDir)) {
						var pathName = Path.Combine(dir, asmSimpleName + ".dll");
						if (File.Exists(pathName))
							yield return pathName;
					}
				}
			}
		}

		IEnumerable<string> GetDirs(string baseDir) {
			if (!Directory.Exists(baseDir))
				return Array2.Empty<string>();
			var dirs = new List<string>();
			try {
				foreach (var di in new DirectoryInfo(baseDir).GetDirectories())
					dirs.Add(di.FullName);
			}
			catch {
			}
			return dirs;
		}

		IEnumerable<string> FindAssembliesModuleSearchPaths(IAssembly assembly, ModuleDef sourceModule, bool matchExactly) {
			string asmSimpleName = UTF8String.ToSystemStringOrEmpty(assembly.Name);
			var searchPaths = GetSearchPaths(sourceModule);
			var exts = assembly.IsContentTypeWindowsRuntime ? winMDAssemblyExtensions : assemblyExtensions;
			foreach (var ext in exts) {
				foreach (var path in searchPaths) {
					for (int i = 0; i < 2; i++) {
						string path2;
						try {
							if (i == 0)
								path2 = Path.Combine(path, asmSimpleName + ext);
							else
								path2 = Path.Combine(Path.Combine(path, asmSimpleName), asmSimpleName + ext);
						}
						catch (ArgumentException) {
							// Invalid path chars
							yield break;
						}
						if (File.Exists(path2))
							yield return path2;
					}
				}
			}
		}

		/// <summary>
		/// Gets all search paths to use for this module
		/// </summary>
		/// <param name="module">The module or <c>null</c> if unknown</param>
		/// <returns>A list of all search paths to use for this module</returns>
		IEnumerable<string> GetSearchPaths(ModuleDef module) {
			var keyModule = module;
			if (keyModule is null)
				keyModule = nullModule;
			if (moduleSearchPaths.TryGetValue(keyModule, out var searchPaths))
				return searchPaths;
			moduleSearchPaths[keyModule] = searchPaths = new List<string>(GetModuleSearchPaths(module));
			return searchPaths;
		}

		/// <summary>
		/// Gets all module search paths. This is usually empty unless its assembly has
		/// a <c>.config</c> file specifying any additional private search paths in a
		/// &lt;probing/&gt; element.
		/// </summary>
		/// <param name="module">The module or <c>null</c> if unknown</param>
		/// <returns>A list of search paths</returns>
		protected virtual IEnumerable<string> GetModuleSearchPaths(ModuleDef module) => GetModulePrivateSearchPaths(module);

		/// <summary>
		/// Gets all private assembly search paths as found in the module's <c>.config</c> file.
		/// </summary>
		/// <param name="module">The module or <c>null</c> if unknown</param>
		/// <returns>A list of search paths</returns>
		protected IEnumerable<string> GetModulePrivateSearchPaths(ModuleDef module) {
			if (module is null)
				return Array2.Empty<string>();
			var asm = module.Assembly;
			if (asm is null)
				return Array2.Empty<string>();
			module = asm.ManifestModule;
			if (module is null)
				return Array2.Empty<string>();  // Should never happen

			string baseDir = null;
			try {
				var imageName = module.Location;
				if (imageName != string.Empty) {
					baseDir = Directory.GetParent(imageName).FullName;
					var configName = imageName + ".config";
					if (File.Exists(configName))
						return GetPrivatePaths(baseDir, configName);
				}
			}
			catch {
			}
			if (!(baseDir is null))
				return new List<string> { baseDir };
			return Array2.Empty<string>();
		}

		IEnumerable<string> GetPrivatePaths(string baseDir, string configFileName) {
			var searchPaths = new List<string>();

			try {
				var dirName = Path.GetDirectoryName(Path.GetFullPath(configFileName));
				searchPaths.Add(dirName);

				using (var xmlStream = new FileStream(configFileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
					var doc = new XmlDocument();
					doc.Load(XmlReader.Create(xmlStream));
					foreach (var tmp in doc.GetElementsByTagName("probing")) {
						var probingElem = tmp as XmlElement;
						if (probingElem is null)
							continue;
						var privatePath = probingElem.GetAttribute("privatePath");
						if (string.IsNullOrEmpty(privatePath))
							continue;
						foreach (var tmp2 in privatePath.Split(';')) {
							var path = tmp2.Trim();
							if (path == "")
								continue;
							var newPath = Path.GetFullPath(Path.Combine(dirName, path.Replace('\\', Path.DirectorySeparatorChar)));
							if (Directory.Exists(newPath) && newPath.StartsWith(baseDir + Path.DirectorySeparatorChar))
								searchPaths.Add(newPath);
						}
					}
				}
			}
			catch (ArgumentException) {
			}
			catch (IOException) {
			}
			catch (XmlException) {
			}

			return searchPaths;
		}
	}
}
