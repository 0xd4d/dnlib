using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// Resolves assemblies
	/// </summary>
	public class AssemblyResolver : IAssemblyResolver {
		static readonly ModuleDef nullModule = new ModuleDefUser();

		// DLL files are searched before EXE files
		static readonly IList<string> assemblyExtensions = new string[] { ".dll", ".exe" };

		static readonly GacInfo gac2Info;	// .NET 1.x and 2.x
		static readonly GacInfo gac4Info;	// .NET 4.x

		Dictionary<ModuleDef, List<string>> moduleSearchPaths = new Dictionary<ModuleDef, List<string>>();
		Dictionary<string, AssemblyDef> cachedAssemblies = new Dictionary<string, AssemblyDef>(StringComparer.Ordinal);
		List<string> preSearchPaths = new List<string>();
		List<string> postSearchPaths = new List<string>();
		bool findExactMatch;
		bool enableTypeDefCache;

		class GacInfo {
			public string path;
			public string prefix;
			public IList<string> subDirs;

			public GacInfo(string prefix, string path, IList<string> subDirs) {
				this.prefix = prefix;
				this.path = path;
				this.subDirs = subDirs;
			}
		}

		static AssemblyResolver() {
			var windir = Environment.GetEnvironmentVariable("WINDIR");
			if (!string.IsNullOrEmpty(windir)) {
				gac2Info = new GacInfo("", Path.Combine(windir, "assembly"), new string[] {
					"GAC_MSIL", "GAC_32", "GAC_64", "GAC"
				});
				gac4Info = new GacInfo("v4.0_", Path.Combine(Path.Combine(windir, "Microsoft.NET"), "assembly"), new string[] {
					"GAC_MSIL", "GAC_32", "GAC_64"
				});
			}
		}

		/// <summary>
		/// <c>true</c> if <see cref="Resolve"/> should find an assembly that matches exactly.
		/// <c>false</c> if it first tries to match exactly, and if that fails, it picks an
		/// assembly that is closest to the requested assembly.
		/// </summary>
		public bool FindExactMatch {
			get { return findExactMatch; }
			set { findExactMatch = value; }
		}

		/// <summary>
		/// If <c>true</c>, all modules in newly resolved assemblies will have their
		/// <see cref="ModuleDef.EnableTypeDefFindCache"/> property set to <c>true</c>.
		/// </summary>
		public bool EnableTypeDefCache {
			get { return enableTypeDefCache; }
			set { enableTypeDefCache = value; }
		}

		/// <summary>
		/// Gets paths searched before trying the standard locations
		/// </summary>
		public IList<string> PreSearchPaths {
			get { return preSearchPaths; }
		}

		/// <summary>
		/// Gets paths searched after trying the standard locations
		/// </summary>
		public IList<string> PostSearchPaths {
			get { return postSearchPaths; }
		}

		/// <inheritdoc/>
		public AssemblyDef Resolve(AssemblyNameInfo assembly, ModuleDef sourceModule) {
			if (assembly == null)
				return null;

			AssemblyDef resolvedAssembly = Resolve2(assembly, sourceModule);
			if (resolvedAssembly == null) {
				string asmName = UTF8String.ToSystemStringOrEmpty(assembly.Name);
				string asmNameTrimmed = asmName.Trim();
				if (asmName != asmNameTrimmed) {
					assembly = new AssemblyNameInfo {
						Name = new UTF8String(asmNameTrimmed),
						Version = assembly.Version,
						PublicKeyOrToken = assembly.PublicKeyOrToken,
						Locale = assembly.Locale,
					};
					resolvedAssembly = Resolve2(assembly, sourceModule);
				}
			}

			if (resolvedAssembly == null)
				return null;

			var key1 = GetAssemblyNameKey(new AssemblyNameInfo(resolvedAssembly));
			var key2 = GetAssemblyNameKey(assembly);
			AssemblyDef asm1, asm2;
			cachedAssemblies.TryGetValue(key1, out asm1);
			cachedAssemblies.TryGetValue(key2, out asm2);

			if (asm1 != resolvedAssembly && asm2 != resolvedAssembly) {
				// This assembly was just resolved
				if (enableTypeDefCache) {
					foreach (var module in resolvedAssembly.Modules)
						module.EnableTypeDefFindCache = true;
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
			if (resolvedAssembly.ManifestModule != null)
				resolvedAssembly.ManifestModule.Dispose();
			return asm1 ?? asm2;
		}

		/// <summary>
		/// Add an assembly to the assembly cache
		/// </summary>
		/// <param name="asm">The assembly</param>
		/// <returns><c>true</c> if <paramref name="asm"/> is cached, <c>false</c> if it's not
		/// cached because some other assembly with the exact same full name has already been
		/// cached or if <paramref name="asm"/> is <c>null</c>.</returns>
		public bool AddToCache(AssemblyDef asm) {
			if (asm == null)
				return false;
			var asmKey = GetAssemblyNameKey(new AssemblyNameInfo(asm));
			AssemblyDef cachedAsm;
			if (cachedAssemblies.TryGetValue(asmKey, out cachedAsm))
				return asm == cachedAsm;
			cachedAssemblies.Add(asmKey, asm);
			return true;
		}

		/// <summary>
		/// Add a module's assembly to the assembly cache
		/// </summary>
		/// <param name="module">The module whose assembly should be cached</param>
		/// <returns><c>true</c> if <paramref name="module"/>'s assembly is cached, <c>false</c>
		/// if it's not cached because some other assembly with the exact same full name has
		/// already been cached or if <paramref name="module"/> or its assembly is <c>null</c>.</returns>
		public bool AddToCache(ModuleDef module) {
			return module != null && AddToCache(module.Assembly);
		}

		static string GetAssemblyNameKey(AssemblyNameInfo asmName) {
			// Make sure the name contains PublicKeyToken= and not PublicKey=
			return asmName.FullNameToken.ToLowerInvariant();
		}

		AssemblyDef Resolve2(AssemblyNameInfo assembly, ModuleDef sourceModule) {
			AssemblyDef resolvedAssembly;

			if (cachedAssemblies.TryGetValue(GetAssemblyNameKey(assembly), out resolvedAssembly))
				return resolvedAssembly;

			resolvedAssembly = FindExactAssembly(assembly, PreFindAssemblies(assembly, sourceModule, true)) ??
					FindExactAssembly(assembly, FindAssemblies(assembly, sourceModule, true)) ??
					FindExactAssembly(assembly, PostFindAssemblies(assembly, sourceModule, true));
			if (resolvedAssembly != null)
				return resolvedAssembly;

			if (!findExactMatch) {
				resolvedAssembly = FindClosestAssembly(assembly);
				resolvedAssembly = FindClosestAssembly(assembly, resolvedAssembly, PreFindAssemblies(assembly, sourceModule, false));
				resolvedAssembly = FindClosestAssembly(assembly, resolvedAssembly, FindAssemblies(assembly, sourceModule, false));
				resolvedAssembly = FindClosestAssembly(assembly, resolvedAssembly, PostFindAssemblies(assembly, sourceModule, false));
			}

			return resolvedAssembly;
		}

		/// <summary>
		/// Finds an assembly that exactly matches the requested assembly
		/// </summary>
		/// <param name="assembly">Assembly name to find</param>
		/// <param name="paths">Search paths or <c>null</c> if none</param>
		/// <returns>An <see cref="AssemblyDef"/> instance or <c>null</c> if an exact match
		/// couldn't be found.</returns>
		AssemblyDef FindExactAssembly(AssemblyNameInfo assembly, IEnumerable<string> paths) {
			if (paths == null)
				return null;
			var asmComparer = new AssemblyNameComparer(AssemblyNameComparerFlags.All);
			foreach (var path in paths) {
				ModuleDefMD mod = null;
				try {
					mod = ModuleDefMD.Load(path);
					var asm = mod.Assembly;
					if (asm != null && asmComparer.Equals(assembly, new AssemblyNameInfo(asm))) {
						mod = null;
						return asm;
					}
				}
				catch {
				}
				finally {
					if (mod != null)
						mod.Dispose();
				}
			}
			return null;
		}

		/// <summary>
		/// Finds the closest assembly from the already cached assemblies
		/// </summary>
		/// <param name="assembly">Assembly name to find</param>
		/// <returns>The closest <see cref="AssemblyDef"/> or <c>null</c> if none found</returns>
		AssemblyDef FindClosestAssembly(AssemblyNameInfo assembly) {
			AssemblyDef closest = null;
			var asmComparer = new AssemblyNameComparer(AssemblyNameComparerFlags.All);
			foreach (var asm in cachedAssemblies.Values) {
				if (asmComparer.CompareClosest(assembly, new AssemblyNameInfo(closest), new AssemblyNameInfo(asm)) == 1)
					closest = asm;
			}
			return closest;
		}

		AssemblyDef FindClosestAssembly(AssemblyNameInfo assembly, AssemblyDef closest, IEnumerable<string> paths) {
			if (paths == null)
				return closest;
			var asmComparer = new AssemblyNameComparer(AssemblyNameComparerFlags.All);
			foreach (var path in paths) {
				ModuleDefMD mod = null;
				try {
					mod = ModuleDefMD.Load(path);
					var asm = mod.Assembly;
					if (asm != null && asmComparer.CompareClosest(assembly, new AssemblyNameInfo(closest), new AssemblyNameInfo(asm)) == 1) {
						if (!IsCached(closest) && closest != null && closest.ManifestModule != null)
							closest.ManifestModule.Dispose();
						closest = asm;
						mod = null;
					}
				}
				catch {
				}
				finally {
					if (mod != null)
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
			if (asm == null)
				return false;
			AssemblyDef cachedAsm;
			return cachedAssemblies.TryGetValue(GetAssemblyNameKey(new AssemblyNameInfo(asm)), out cachedAsm) &&
					cachedAsm == asm;
		}

		IEnumerable<string> FindAssemblies2(AssemblyNameInfo assembly, IEnumerable<string> paths) {
			if (paths != null) {
				var asmSimpleName = UTF8String.ToSystemStringOrEmpty(assembly.Name);
				foreach (var ext in assemblyExtensions) {
					foreach (var path in paths) {
						var fullPath = Path.Combine(path, asmSimpleName + ext);
						if (File.Exists(fullPath))
							yield return fullPath;
					}
				}
			}
		}

		/// <summary>
		/// Called before <see cref="FindAssemblies"/>
		/// </summary>
		/// <param name="assembly">Simple assembly name</param>
		/// <param name="sourceModule">The module that needs to resolve an assembly or <c>null</c></param>
		/// <param name="matchExactly">We're trying to find an exact match</param>
		/// <returns><c>null</c> or an enumerable of full paths to try</returns>
		protected virtual IEnumerable<string> PreFindAssemblies(AssemblyNameInfo assembly, ModuleDef sourceModule, bool matchExactly) {
			foreach (var path in FindAssemblies2(assembly, preSearchPaths))
				yield return path;
		}

		/// <summary>
		/// Called after <see cref="FindAssemblies"/> (if it fails)
		/// </summary>
		/// <param name="assembly">Simple assembly name</param>
		/// <param name="sourceModule">The module that needs to resolve an assembly or <c>null</c></param>
		/// <param name="matchExactly">We're trying to find an exact match</param>
		/// <returns><c>null</c> or an enumerable of full paths to try</returns>
		protected virtual IEnumerable<string> PostFindAssemblies(AssemblyNameInfo assembly, ModuleDef sourceModule, bool matchExactly) {
			foreach (var path in FindAssemblies2(assembly, postSearchPaths))
				yield return path;
		}

		/// <summary>
		/// Called after <see cref="PreFindAssemblies"/> (if it fails)
		/// </summary>
		/// <param name="assembly">Simple assembly name</param>
		/// <param name="sourceModule">The module that needs to resolve an assembly or <c>null</c></param>
		/// <param name="matchExactly">We're trying to find an exact match</param>
		/// <returns><c>null</c> or an enumerable of full paths to try</returns>
		protected virtual IEnumerable<string> FindAssemblies(AssemblyNameInfo assembly, ModuleDef sourceModule, bool matchExactly) {
			foreach (var path in FindAssembliesGac(assembly, sourceModule, matchExactly))
				yield return path;
			foreach (var path in FindAssembliesModuleSearchPaths(assembly, sourceModule, matchExactly))
				yield return path;
		}

		IEnumerable<string> FindAssembliesGac(AssemblyNameInfo assembly, ModuleDef sourceModule, bool matchExactly) {
			if (matchExactly)
				return FindAssembliesGacExactly(assembly, sourceModule);
			return FindAssembliesGacAny(assembly, sourceModule);
		}

		IEnumerable<string> FindAssembliesGacExactly(AssemblyNameInfo assembly, ModuleDef sourceModule) {
			foreach (var path in FindAssembliesGacExactly(gac2Info, assembly, sourceModule))
				yield return path;
			foreach (var path in FindAssembliesGacExactly(gac4Info, assembly, sourceModule))
				yield return path;
		}

		IEnumerable<string> FindAssembliesGacExactly(GacInfo gacInfo, AssemblyNameInfo assembly, ModuleDef sourceModule) {
			var pkt = PublicKeyBase.ToPublicKeyToken(assembly.PublicKeyOrToken);
			if (gacInfo != null && pkt != null) {
				string pktString = pkt.ToString();
				string verString = Utils.CreateVersionWithNoUndefinedValues(assembly.Version).ToString();
				var asmSimpleName = UTF8String.ToSystemStringOrEmpty(assembly.Name);
				foreach (var subDir in gacInfo.subDirs) {
					var baseDir = Path.Combine(gacInfo.path, subDir);
					baseDir = Path.Combine(baseDir, asmSimpleName);
					baseDir = Path.Combine(baseDir, string.Format("{0}{1}__{2}", gacInfo.prefix, verString, pktString));
					var pathName = Path.Combine(baseDir, asmSimpleName + ".dll");
					if (File.Exists(pathName))
						yield return pathName;
				}
			}
		}

		IEnumerable<string> FindAssembliesGacAny(AssemblyNameInfo assembly, ModuleDef sourceModule) {
			foreach (var path in FindAssembliesGacAny(gac2Info, assembly, sourceModule))
				yield return path;
			foreach (var path in FindAssembliesGacAny(gac4Info, assembly, sourceModule))
				yield return path;
		}

		IEnumerable<string> FindAssembliesGacAny(GacInfo gacInfo, AssemblyNameInfo assembly, ModuleDef sourceModule) {
			if (gacInfo != null) {
				var asmSimpleName = UTF8String.ToSystemStringOrEmpty(assembly.Name);
				foreach (var subDir in gacInfo.subDirs) {
					var baseDir = Path.Combine(gacInfo.path, subDir);
					baseDir = Path.Combine(baseDir, asmSimpleName);
					foreach (var dir in GetDirs(baseDir)) {
						var pathName = Path.Combine(dir, asmSimpleName + ".dll");
						if (File.Exists(pathName))
							yield return pathName;
					}
				}
			}
		}

		IEnumerable<string> GetDirs(string baseDir) {
			var dirs = new List<string>();
			try {
				foreach (var di in new DirectoryInfo(baseDir).GetDirectories())
					dirs.Add(di.FullName);
			}
			catch {
			}
			return dirs;
		}

		IEnumerable<string> FindAssembliesModuleSearchPaths(AssemblyNameInfo assembly, ModuleDef sourceModule, bool matchExactly) {
			string asmSimpleName = UTF8String.ToSystemStringOrEmpty(assembly.Name);
			var searchPaths = GetSearchPaths(sourceModule);
			foreach (var ext in assemblyExtensions) {
				foreach (var path in searchPaths) {
					for (int i = 0; i < 2; i++) {
						string path2;
						if (i == 0)
							path2 = Path.Combine(path, asmSimpleName + ext);
						else
							path2 = Path.Combine(Path.Combine(path, asmSimpleName), asmSimpleName + ext);
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
			ModuleDef keyModule = module;
			if (keyModule == null)
				keyModule = nullModule;
			List<string> searchPaths;
			if (moduleSearchPaths.TryGetValue(keyModule, out searchPaths))
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
		protected virtual IEnumerable<string> GetModuleSearchPaths(ModuleDef module) {
			return GetModulePrivateSearchPaths(module);
		}

		/// <summary>
		/// Gets all private assembly search paths as found in the module's <c>.config</c> file.
		/// </summary>
		/// <param name="module">The module or <c>null</c> if unknown</param>
		/// <returns>A list of search paths</returns>
		protected IEnumerable<string> GetModulePrivateSearchPaths(ModuleDef module) {
			if (module == null || module.Assembly == null)
				return new List<string>();
			module = module.Assembly.ManifestModule;
			if (module == null)
				return new List<string>();	// Should never happen

			string baseDir = null;
			try {
				var imageName = module.Location;
				if (imageName != "") {
					baseDir = Directory.GetParent(imageName).FullName;
					var configName = imageName + ".config";
					if (File.Exists(configName))
						return GetPrivatePaths(baseDir, configName);
				}
			}
			catch {
			}
			if (baseDir != null)
				return new List<string> { baseDir };
			return new List<string>();
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
						if (probingElem == null)
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
