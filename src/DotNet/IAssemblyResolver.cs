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
	}

	static partial class Extensions {
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
