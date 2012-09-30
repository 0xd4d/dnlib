using System;
using System.Reflection;

namespace dot10.DotNet {
	/// <summary>
	/// Thrown if any of the <c>ResolveThrow()</c> methods fail to resolve an assembly
	/// </summary>
	[Serializable]
	public class AssemblyResolveException : Exception {
		/// <summary>
		/// Default constructor
		/// </summary>
		public AssemblyResolveException() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Exception message</param>
		public AssemblyResolveException(string message)
			: base(message) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Exception message</param>
		/// <param name="innerException">Inner exception or <c>null</c> if none</param>
		public AssemblyResolveException(string message, Exception innerException)
			: base(message, innerException) {
		}
	}

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
			var asm = self.Resolve(assembly, sourceModule);
			if (asm != null)
				return asm;
			throw new AssemblyResolveException(string.Format("Could not resolve {0}", assembly));
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
			var asm = self.Resolve(new AssemblyNameInfo(assembly), sourceModule);
			if (asm != null)
				return asm;
			throw new AssemblyResolveException(string.Format("Could not resolve {0}", assembly));
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
			var asm = self.Resolve(new AssemblyNameInfo(asmFullName), sourceModule);
			if (asm != null)
				return asm;
			throw new AssemblyResolveException(string.Format("Could not resolve {0}", asmFullName));
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
			var asm = self.Resolve(new AssemblyNameInfo(assembly), sourceModule);
			if (asm != null)
				return asm;
			throw new AssemblyResolveException(string.Format("Could not resolve {0}", assembly));
		}
	}
}
