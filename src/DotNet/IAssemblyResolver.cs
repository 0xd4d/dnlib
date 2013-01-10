/*
    Copyright (C) 2012-2013 de4dot@gmail.com

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
using System.Reflection;

namespace dnlib.DotNet {
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

		/// <summary>
		/// Removes the assembly from the cache
		/// </summary>
		/// <param name="asm">The assembly</param>
		/// <returns><c>true</c> if it was removed, <c>false</c> if it wasn't removed since it
		/// wasn't in the cache or if <paramref name="asm"/> was <c>null</c></returns>
		bool Remove(AssemblyDef asm);
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
		/// Removes a module's assembly from the cache
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="module">The module</param>
		/// <returns><c>true</c> if its assembly was removed, <c>false</c> if it wasn't removed
		/// since it wasn't in the cache, it has no assembly, or <paramref name="module"/> was
		/// <c>null</c></returns>
		public static bool Remove(this IAssemblyResolver self, ModuleDef module) {
			return module != null && self.Remove(module.Assembly);
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
