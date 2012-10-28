using System;
using System.Reflection;

namespace dot10.DotNet {
	/// <summary>
	/// Resolves assemblies
	/// </summary>
	public interface IAssemblyResolver {
		/// <summary>
		/// Finds and returns an <see cref="AssemblyDef"/>
		/// </summary>
		/// <param name="assembly">The assembly to find</param>
		/// <param name="sourceModule">The module that needs to resolve an assembly or <c>null</c></param>
		/// <returns>An <see cref="AssemblyDef"/> instance owned by the assembly resolver or
		/// <c>null</c> if the assembly couldn't be found.</returns>
		AssemblyDef Resolve(AssemblyNameInfo assembly, ModuleDef sourceModule);

		/// <summary>
		/// Add an assembly to the assembly cache
		/// </summary>
		/// <param name="asm">The assembly</param>
		/// <returns><c>true</c> if <paramref name="asm"/> is cached, <c>false</c> if it's not
		/// cached because some other assembly with the exact same full name has already been
		/// cached or if <paramref name="asm"/> is <c>null</c>.</returns>
		bool AddToCache(AssemblyDef asm);
	}

	static partial class Extensions {
		/// <summary>
		/// Add a module's assembly to the assembly cache
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="module">The module whose assembly should be cached</param>
		/// <returns><c>true</c> if <paramref name="module"/>'s assembly is cached, <c>false</c>
		/// if it's not cached because some other assembly with the exact same full name has
		/// already been cached or if <paramref name="module"/> or its assembly is <c>null</c>.</returns>
		public static bool AddToCache(this IAssemblyResolver self, ModuleDef module) {
			return module != null && self.AddToCache(module.Assembly);
		}

		/// <summary>
		/// Finds and returns an <see cref="AssemblyDef"/>
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="assembly">The assembly to find</param>
		/// <param name="sourceModule">The module that needs to resolve an assembly or <c>null</c></param>
		/// <returns>An <see cref="AssemblyDef"/> instance owned by the assembly resolver or
		/// <c>null</c> if the assembly couldn't be found.</returns>
		public static AssemblyDef Resolve(this IAssemblyResolver self, AssemblyName assembly, ModuleDef sourceModule) {
			if (assembly == null)
				return null;
			return self.Resolve(new AssemblyNameInfo(assembly), sourceModule);
		}

		/// <summary>
		/// Finds and returns an <see cref="AssemblyDef"/>
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="asmFullName">The assembly to find</param>
		/// <param name="sourceModule">The module that needs to resolve an assembly or <c>null</c></param>
		/// <returns>An <see cref="AssemblyDef"/> instance owned by the assembly resolver or
		/// <c>null</c> if the assembly couldn't be found.</returns>
		public static AssemblyDef Resolve(this IAssemblyResolver self, string asmFullName, ModuleDef sourceModule) {
			if (asmFullName == null)
				return null;
			return self.Resolve(new AssemblyNameInfo(asmFullName), sourceModule);
		}

		/// <summary>
		/// Finds and returns an <see cref="AssemblyDef"/>
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="assembly">The assembly to find</param>
		/// <param name="sourceModule">The module that needs to resolve an assembly or <c>null</c></param>
		/// <returns>An <see cref="AssemblyDef"/> instance owned by the assembly resolver or
		/// <c>null</c> if the assembly couldn't be found.</returns>
		public static AssemblyDef Resolve(this IAssemblyResolver self, IAssembly assembly, ModuleDef sourceModule) {
			if (assembly == null)
				return null;
			return self.Resolve(new AssemblyNameInfo(assembly), sourceModule);
		}

		/// <summary>
		/// Finds and returns an <see cref="AssemblyDef"/>
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="assembly">The assembly to find</param>
		/// <param name="sourceModule">The module that needs to resolve an assembly or <c>null</c></param>
		/// <returns>An <see cref="AssemblyDef"/> instance owned by the assembly resolver</returns>
		/// <exception cref="AssemblyResolveException">If the assembly couldn't be found.</exception>
		public static AssemblyDef ResolveThrow(this IAssemblyResolver self, AssemblyNameInfo assembly, ModuleDef sourceModule) {
			if (assembly == null)
				return null;
			var asm = self.Resolve(assembly, sourceModule);
			if (asm != null)
				return asm;
			throw new AssemblyResolveException(string.Format("Could not resolve assembly: {0}", assembly));
		}

		/// <summary>
		/// Finds and returns an <see cref="AssemblyDef"/>
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="assembly">The assembly to find</param>
		/// <param name="sourceModule">The module that needs to resolve an assembly or <c>null</c></param>
		/// <returns>An <see cref="AssemblyDef"/> instance owned by the assembly resolver</returns>
		/// <exception cref="AssemblyResolveException">If the assembly couldn't be found.</exception>
		public static AssemblyDef ResolveThrow(this IAssemblyResolver self, AssemblyName assembly, ModuleDef sourceModule) {
			if (assembly == null)
				return null;
			var asm = self.Resolve(new AssemblyNameInfo(assembly), sourceModule);
			if (asm != null)
				return asm;
			throw new AssemblyResolveException(string.Format("Could not resolve assembly: {0}", assembly));
		}

		/// <summary>
		/// Finds and returns an <see cref="AssemblyDef"/>
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="asmFullName">The assembly to find</param>
		/// <param name="sourceModule">The module that needs to resolve an assembly or <c>null</c></param>
		/// <returns>An <see cref="AssemblyDef"/> instance owned by the assembly resolver</returns>
		/// <exception cref="AssemblyResolveException">If the assembly couldn't be found.</exception>
		public static AssemblyDef ResolveThrow(this IAssemblyResolver self, string asmFullName, ModuleDef sourceModule) {
			if (asmFullName == null)
				return null;
			var asm = self.Resolve(new AssemblyNameInfo(asmFullName), sourceModule);
			if (asm != null)
				return asm;
			throw new AssemblyResolveException(string.Format("Could not resolve assembly: {0}", asmFullName));
		}

		/// <summary>
		/// Finds and returns an <see cref="AssemblyDef"/>
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="assembly">The assembly to find</param>
		/// <param name="sourceModule">The module that needs to resolve an assembly or <c>null</c></param>
		/// <returns>An <see cref="AssemblyDef"/> instance owned by the assembly resolver</returns>
		/// <exception cref="AssemblyResolveException">If the assembly couldn't be found.</exception>
		public static AssemblyDef ResolveThrow(this IAssemblyResolver self, IAssembly assembly, ModuleDef sourceModule) {
			if (assembly == null)
				return null;
			var asm = self.Resolve(new AssemblyNameInfo(assembly), sourceModule);
			if (asm != null)
				return asm;
			throw new AssemblyResolveException(string.Format("Could not resolve assembly: {0}", assembly));
		}
	}
}
