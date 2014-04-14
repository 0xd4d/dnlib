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

using System;
using System.Collections.Generic;
using dnlib.Threading;

﻿namespace dnlib.DotNet {
	/// <summary>
	/// Resolves types, methods, fields
	/// </summary>
	public sealed class Resolver : IResolver {
		readonly IAssemblyResolver assemblyResolver;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="assemblyResolver">The assembly resolver</param>
		public Resolver(IAssemblyResolver assemblyResolver) {
			if (assemblyResolver == null)
				throw new ArgumentNullException("assemblyResolver");
			this.assemblyResolver = assemblyResolver;
		}

		/// <inheritdoc/>
		public TypeDef Resolve(TypeRef typeRef) {
			if (typeRef == null)
				return null;

			var nonNestedTypeRef = TypeRef.GetNonNestedTypeRef(typeRef);
			if (nonNestedTypeRef == null)
				return null;

			var nonNestedResolutionScope = nonNestedTypeRef.ResolutionScope;
			var nonNestedModule = nonNestedTypeRef.Module;
			var asmRef = nonNestedResolutionScope as AssemblyRef;
			if (asmRef != null) {
				var asm = assemblyResolver.Resolve(asmRef, nonNestedModule);
				return asm == null ? null : asm.Find(typeRef) ?? ResolveExportedType(asm.Modules, typeRef);
			}

			var moduleDef = nonNestedResolutionScope as ModuleDef;
			if (moduleDef != null)
				return moduleDef.Find(typeRef) ??
					ResolveExportedType(new ModuleDef[] { moduleDef }, typeRef);

			var moduleRef = nonNestedResolutionScope as ModuleRef;
			if (moduleRef != null) {
				if (nonNestedModule == null)
					return null;
				if (new SigComparer().Equals(moduleRef, nonNestedModule))
					return nonNestedModule.Find(typeRef) ??
						ResolveExportedType(new ModuleDef[] { nonNestedModule }, typeRef);
				var nonNestedAssembly = nonNestedModule.Assembly;
				if (nonNestedAssembly == null)
					return null;
				var resolvedModule = nonNestedAssembly.FindModule(moduleRef.Name);
				return resolvedModule == null ? null : resolvedModule.Find(typeRef) ??
						ResolveExportedType(new ModuleDef[] { resolvedModule }, typeRef);
			}

			return null;
		}

		TypeDef ResolveExportedType(IList<ModuleDef> modules, TypeRef typeRef) {
			var exportedType = FindExportedType(modules, typeRef);
			if (exportedType == null)
				return null;

			var asmResolver = modules[0].Context.AssemblyResolver;
			var etAsm = asmResolver.Resolve(exportedType.DefinitionAssembly, typeRef.Module);
			if (etAsm == null)
				return null;

			return etAsm.Find(typeRef);
		}

		static ExportedType FindExportedType(IList<ModuleDef> modules, TypeRef typeRef) {
			if (typeRef == null)
				return null;
			foreach (var module in modules.GetSafeEnumerable()) {
				foreach (var exportedType in module.ExportedTypes.GetSafeEnumerable()) {
					if (!exportedType.IsForwarder)
						continue;
					if (new SigComparer(SigComparerOptions.DontCompareTypeScope).Equals(exportedType, typeRef))
						return exportedType;
				}
			}
			return null;
		}

		/// <inheritdoc/>
		public IMemberForwarded Resolve(MemberRef memberRef) {
			if (memberRef == null)
				return null;
			var parent = memberRef.Class;
			var method = parent as MethodDef;
			if (method != null)
				return method;
			var declaringType = GetDeclaringType(memberRef, parent);
			return declaringType == null ? null : declaringType.Resolve(memberRef);
		}

		TypeDef GetDeclaringType(MemberRef memberRef, IMemberRefParent parent) {
			if (memberRef == null || parent == null)
				return null;

			var declaringTypeDef = parent as TypeDef;
			if (declaringTypeDef != null)
				return declaringTypeDef;

			var declaringTypeRef = parent as TypeRef;
			if (declaringTypeRef != null)
				return Resolve(declaringTypeRef);

			// A module ref is used to reference the global type of a module in the same
			// assembly as the current module.
			var moduleRef = parent as ModuleRef;
			if (moduleRef != null) {
				var module = memberRef.Module;
				if (module == null)
					return null;
				TypeDef globalType = null;
				if (new SigComparer(0).Equals(module, moduleRef))
					globalType = module.GlobalType;
				var modAsm = module.Assembly;
				if (globalType == null && modAsm != null) {
					var moduleDef = modAsm.FindModule(moduleRef.Name);
					if (moduleDef != null)
						globalType = moduleDef.GlobalType;
				}
				return globalType;
			}

			var method = parent as MethodDef;
			if (method != null)
				return method.DeclaringType;

			var ts = parent as TypeSpec;
			if (ts != null) {
				var git = ts.TypeSig as GenericInstSig;
				if (git != null) {
					var td = git.GenericType.TypeDef;
					if (td != null)
						return td;
					var tr = git.GenericType.TypeRef;
					if (tr != null)
						return Resolve(tr);
				}
				return null;
			}

			return null;
		}
	}
}
