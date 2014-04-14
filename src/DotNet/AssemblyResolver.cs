/*
    Copyright (C) 2012-2014 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using dnlib.Threading;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet {
	/// <summary>
	/// Resolves assemblies
	/// </summary>
	public class AssemblyResolver : IAssemblyResolver {
		static readonly ModuleDef nullModule = new ModuleDefUser();

		// DLL files are searched before EXE files
		static readonly IList<string> assemblyExtensions = new string[] { ".dll", ".exe" };

		static readonly GacInfo gac2Info;	// .NET 1.x and 2.x
		static readonly GacInfo gac4Info;	// .NET 4.x

		ModuleContext defaultModuleContext;
		readonly Dictionary<ModuleDef, IList<string>> moduleSearchPaths = new Dictionary<ModuleDef, IList<string>>();
		readonly Dictionary<string, AssemblyDef> cachedAssemblies = new Dictionary<string, AssemblyDef>(StringComparer.Ordinal);
		readonly ThreadSafe.IList<string> preSearchPaths = ThreadSafeListCreator.Create<string>();
		readonly ThreadSafe.IList<string> postSearchPaths = ThreadSafeListCreator.Create<string>();
		bool findExactMatch;
		bool enableTypeDefCache;
#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		sealed class GacInfo {
			public readonly string path;
			public readonly string prefix;
			public readonly IList<string> subDirs;

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
					"GAC_32", "GAC_64", "GAC_MSIL", "GAC"
				});
				gac4Info = new GacInfo("v4.0_", Path.Combine(Path.Combine(windir, "Microsoft.NET"), "assembly"), new string[] {
					"GAC_32", "GAC_64", "GAC_MSIL"
				});
			}
		}

		/// <summary>
		/// Gets/sets the default <see cref="ModuleContext"/>
		/// </summary>
		public ModuleContext DefaultModuleContext {
			get { return defaultModuleContext; }
			set { defaultModuleContext = value; }
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
		public ThreadSafe.IList<string> PreSearchPaths {
			get { return preSearchPaths; }
		}

		/// <summary>
		/// Gets paths searched after trying the standard locations
		/// </summary>
		public ThreadSafe.IList<string> PostSearchPaths {
			get { return postSearchPaths; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public AssemblyResolver()
			: this(null, true) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="defaultModuleContext">Module context for all resolved assemblies</param>
		public AssemblyResolver(ModuleContext defaultModuleContext)
			: this(defaultModuleContext, true) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="defaultModuleContext">Module context for all resolved assemblies</param>
		/// <param name="addOtherSearchPaths">If <c>true</c>, add other common assembly search
		/// paths, not just the module search paths and the GAC.</param>
		public AssemblyResolver(ModuleContext defaultModuleContext, bool addOtherSearchPaths) {
			this.defaultModuleContext = defaultModuleContext;
			if (addOtherSearchPaths)
				AddOtherSearchPaths(postSearchPaths);
		}

		/// <inheritdoc/>
		public AssemblyDef Resolve(AssemblyNameInfo assembly, ModuleDef sourceModule) {
			if (assembly == null)
				return null;

#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			AssemblyDef resolvedAssembly = Resolve2(assembly, sourceModule);
			if (resolvedAssembly == null) {
				string asmName = UTF8String.ToSystemStringOrEmpty(assembly.Name);
				string asmNameTrimmed = asmName.Trim();
				if (asmName != asmNameTrimmed) {
					assembly = new AssemblyNameInfo {
						Name = asmNameTrimmed,
						Version = assembly.Version,
						PublicKeyOrToken = assembly.PublicKeyOrToken,
						Locale = assembly.Locale,
					};
					resolvedAssembly = Resolve2(assembly, sourceModule);
				}
			}

			if (resolvedAssembly == null) {
				// Make sure we don't search for this assembly again. This speeds up callers who
				// keep asking for this assembly when trying to resolve many different TypeRefs
				cachedAssemblies[GetAssemblyNameKey(assembly)] = null;
				return null;
			}

			var key1 = GetAssemblyNameKey(new AssemblyNameInfo(resolvedAssembly));
			var key2 = GetAssemblyNameKey(assembly);
			AssemblyDef asm1, asm2;
			cachedAssemblies.TryGetValue(key1, out asm1);
			cachedAssemblies.TryGetValue(key2, out asm2);

			if (asm1 != resolvedAssembly && asm2 != resolvedAssembly) {
				// This assembly was just resolved
				if (enableTypeDefCache) {
					foreach (var module in resolvedAssembly.Modules.GetSafeEnumerable()) {
						if (module != null)
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
			if (dupeModule != null)
				dupeModule.Dispose();
			return asm1 ?? asm2;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <inheritdoc/>
		public bool AddToCache(AssemblyDef asm) {
			if (asm == null)
				return false;
			var asmKey = GetAssemblyNameKey(new AssemblyNameInfo(asm));
			AssemblyDef cachedAsm;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (cachedAssemblies.TryGetValue(asmKey, out cachedAsm) && cachedAsm != null)
				return asm == cachedAsm;
			cachedAssemblies[asmKey] = asm;
			return true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <inheritdoc/>
		public bool Remove(AssemblyDef asm) {
			if (asm == null)
				return false;
			var asmKey = GetAssemblyNameKey(new AssemblyNameInfo(asm));
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			return cachedAssemblies.Remove(asmKey);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		static string GetAssemblyNameKey(AssemblyNameInfo asmName) {
			// Make sure the name contains PublicKeyToken= and not PublicKey=
			return asmName.FullNameToken.ToUpperInvariant();
		}

		AssemblyDef Resolve2(AssemblyNameInfo assembly, ModuleDef sourceModule) {
			AssemblyDef resolvedAssembly;

			if (cachedAssemblies.TryGetValue(GetAssemblyNameKey(assembly), out resolvedAssembly))
				return resolvedAssembly;

			var moduleContext = defaultModuleContext;
			if (moduleContext == null && sourceModule != null)
				moduleContext = sourceModule.Context;

			resolvedAssembly = FindExactAssembly(assembly, PreFindAssemblies(assembly, sourceModule, true), moduleContext) ??
					FindExactAssembly(assembly, FindAssemblies(assembly, sourceModule, true), moduleContext) ??
					FindExactAssembly(assembly, PostFindAssemblies(assembly, sourceModule, true), moduleContext);
			if (resolvedAssembly != null)
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
		/// <param name="assembly">Assembly name to find</param>
		/// <param name="paths">Search paths or <c>null</c> if none</param>
		/// <param name="moduleContext">Module context</param>
		/// <returns>An <see cref="AssemblyDef"/> instance or <c>null</c> if an exact match
		/// couldn't be found.</returns>
		AssemblyDef FindExactAssembly(AssemblyNameInfo assembly, IEnumerable<string> paths, ModuleContext moduleContext) {
			if (paths == null)
				return null;
			var asmComparer = new AssemblyNameComparer(AssemblyNameComparerFlags.All);
			foreach (var path in paths.GetSafeEnumerable()) {
				ModuleDefMD mod = null;
				try {
					mod = ModuleDefMD.Load(path, moduleContext);
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
				if (asm == null)
					continue;
				if (asmComparer.CompareClosest(assembly, new AssemblyNameInfo(closest), new AssemblyNameInfo(asm)) == 1)
					closest = asm;
			}
			return closest;
		}

		AssemblyDef FindClosestAssembly(AssemblyNameInfo assembly, AssemblyDef closest, IEnumerable<string> paths, ModuleContext moduleContext) {
			if (paths == null)
				return closest;
			var asmComparer = new AssemblyNameComparer(AssemblyNameComparerFlags.All);
			foreach (var path in paths.GetSafeEnumerable()) {
				ModuleDefMD mod = null;
				try {
					mod = ModuleDefMD.Load(path, moduleContext);
					var asm = mod.Assembly;
					if (asm != null && asmComparer.CompareClosest(assembly, new AssemblyNameInfo(closest), new AssemblyNameInfo(asm)) == 1) {
						if (!IsCached(closest) && closest != null) {
							var closeMod = closest.ManifestModule;
							if (closeMod != null)
								closeMod.Dispose();
						}
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
					foreach (var path in paths.GetSafeEnumerable()) {
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
				foreach (var path in searchPaths.GetSafeEnumerable()) {
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
			IList<string> searchPaths;
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
			if (module == null)
				return new string[0];
			var asm = module.Assembly;
			if (asm == null)
				return new string[0];
			module = asm.ManifestModule;
			if (module == null)
				return new string[0];	// Should never happen

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
			if (baseDir != null)
				return new List<string> { baseDir };
			return new string[0];
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

		/// <summary>
		/// Add other common search paths
		/// </summary>
		/// <param name="paths">A list that gets updated with the new paths</param>
		protected static void AddOtherSearchPaths(IList<string> paths) {
			AddOtherAssemblySearchPaths(paths, Environment.GetEnvironmentVariable("ProgramFiles"));
			AddOtherAssemblySearchPaths(paths, Environment.GetEnvironmentVariable("ProgramFiles(x86)"));
		}

		static void AddOtherAssemblySearchPaths(IList<string> paths, string path) {
			if (string.IsNullOrEmpty(path))
				return;
			AddSilverlightDirs(paths, Path.Combine(path, @"Microsoft Silverlight"));
			AddIfExists(paths, path, @"Microsoft SDKs\Silverlight\v2.0\Libraries\Client");
			AddIfExists(paths, path, @"Microsoft SDKs\Silverlight\v2.0\Libraries\Server");
			AddIfExists(paths, path, @"Microsoft SDKs\Silverlight\v2.0\Reference Assemblies");
			AddIfExists(paths, path, @"Microsoft SDKs\Silverlight\v3.0\Libraries\Client");
			AddIfExists(paths, path, @"Microsoft SDKs\Silverlight\v3.0\Libraries\Server");
			AddIfExists(paths, path, @"Microsoft SDKs\Silverlight\v4.0\Libraries\Client");
			AddIfExists(paths, path, @"Microsoft SDKs\Silverlight\v4.0\Libraries\Server");
			AddIfExists(paths, path, @"Microsoft SDKs\Silverlight\v5.0\Libraries\Client");
			AddIfExists(paths, path, @"Microsoft SDKs\Silverlight\v5.0\Libraries\Server");
			AddIfExists(paths, path, @"Microsoft.NET\SDK\CompactFramework\v2.0\WindowsCE");
			AddIfExists(paths, path, @"Microsoft.NET\SDK\CompactFramework\v3.5\WindowsCE");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\Framework\Silverlight\v3.0");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\Framework\Silverlight\v4.0");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\Framework\Silverlight\v5.0");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\FSharp\2.0\Runtime\v2.0");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\FSharp\2.0\Runtime\v4.0");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\FSharp\3.0\Runtime\.NETPortable");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\FSharp\3.0\Runtime\v2.0");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\FSharp\3.0\Runtime\v4.0");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\WindowsPowerShell\v1.0");
			AddIfExists(paths, path, @"Reference Assemblies\Microsoft\WindowsPowerShell\3.0");
			AddIfExists(paths, path, @"Microsoft Visual Studio .NET\Common7\IDE\PublicAssemblies");
			AddIfExists(paths, path, @"Microsoft Visual Studio .NET\Common7\IDE\PrivateAssemblies");
			AddIfExists(paths, path, @"Microsoft Visual Studio .NET 2003\Common7\IDE\PublicAssemblies");
			AddIfExists(paths, path, @"Microsoft Visual Studio .NET 2003\Common7\IDE\PrivateAssemblies");
			AddIfExists(paths, path, @"Microsoft Visual Studio 8\Common7\IDE\PublicAssemblies");
			AddIfExists(paths, path, @"Microsoft Visual Studio 8\Common7\IDE\PrivateAssemblies");
			AddIfExists(paths, path, @"Microsoft Visual Studio 9.0\Common7\IDE\PublicAssemblies");
			AddIfExists(paths, path, @"Microsoft Visual Studio 9.0\Common7\IDE\PrivateAssemblies");
			AddIfExists(paths, path, @"Microsoft Visual Studio 10.0\Common7\IDE\PublicAssemblies");
			AddIfExists(paths, path, @"Microsoft Visual Studio 10.0\Common7\IDE\PrivateAssemblies");
			AddIfExists(paths, path, @"Microsoft Visual Studio 11.0\Common7\IDE\PublicAssemblies");
			AddIfExists(paths, path, @"Microsoft Visual Studio 11.0\Common7\IDE\PrivateAssemblies");
			AddIfExists(paths, path, @"Microsoft XNA\XNA Game Studio\v2.0\References\Windows\x86");
			AddIfExists(paths, path, @"Microsoft XNA\XNA Game Studio\v2.0\References\Xbox360");
			AddIfExists(paths, path, @"Microsoft XNA\XNA Game Studio\v3.0\References\Windows\x86");
			AddIfExists(paths, path, @"Microsoft XNA\XNA Game Studio\v3.0\References\Xbox360");
			AddIfExists(paths, path, @"Microsoft XNA\XNA Game Studio\v3.0\References\Zune");
			AddIfExists(paths, path, @"Microsoft XNA\XNA Game Studio\v3.1\References\Windows\x86");
			AddIfExists(paths, path, @"Microsoft XNA\XNA Game Studio\v3.1\References\Xbox360");
			AddIfExists(paths, path, @"Microsoft XNA\XNA Game Studio\v3.1\References\Zune");
			AddIfExists(paths, path, @"Microsoft XNA\XNA Game Studio\v4.0\References\Windows\x86");
			AddIfExists(paths, path, @"Microsoft XNA\XNA Game Studio\v4.0\References\Xbox360");
			AddIfExists(paths, path, @"Windows CE Tools\wce500\Windows Mobile 5.0 Pocket PC SDK\Designtimereferences");
			AddIfExists(paths, path, @"Windows CE Tools\wce500\Windows Mobile 5.0 Smartphone SDK\Designtimereferences");
			AddIfExists(paths, path, @"Windows Mobile 5.0 SDK R2\Managed Libraries");
			AddIfExists(paths, path, @"Windows Mobile 6 SDK\Managed Libraries");
			AddIfExists(paths, path, @"Windows Mobile 6.5.3 DTK\Managed Libraries");
			AddIfExists(paths, path, @"Microsoft SQL Server\90\SDK\Assemblies");
			AddIfExists(paths, path, @"Microsoft SQL Server\100\SDK\Assemblies");
			AddIfExists(paths, path, @"Microsoft SQL Server\110\SDK\Assemblies");
			AddIfExists(paths, path, @"Microsoft ASP.NET\ASP.NET MVC 2\Assemblies");
			AddIfExists(paths, path, @"Microsoft ASP.NET\ASP.NET MVC 3\Assemblies");
			AddIfExists(paths, path, @"Microsoft ASP.NET\ASP.NET MVC 4\Assemblies");
			AddIfExists(paths, path, @"Microsoft ASP.NET\ASP.NET Web Pages\v1.0\Assemblies");
			AddIfExists(paths, path, @"Microsoft ASP.NET\ASP.NET Web Pages\v2.0\Assemblies");
			AddIfExists(paths, path, @"Microsoft SDKs\F#\3.0\Framework\v4.0");
		}

		static void AddSilverlightDirs(IList<string> paths, string basePath) {
			try {
				var di = new DirectoryInfo(basePath);
				foreach (var dir in di.GetDirectories()) {
					if (Regex.IsMatch(dir.Name, @"^\d+(?:\.\d+){3}$"))
						AddIfExists(paths, basePath, dir.Name);
				}
			}
			catch {
			}
		}

		static void AddIfExists(IList<string> paths, string basePath, string extraPath) {
			var path = Path.Combine(basePath, extraPath);
			if (Directory.Exists(path))
				paths.Add(path);
		}
	}
}
