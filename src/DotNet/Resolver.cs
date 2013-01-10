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

﻿namespace dnlib.DotNet {
	/// <summary>
	/// Resolves types, methods, fields
	/// </summary>
	public sealed class Resolver : IResolver {
		IAssemblyResolver assemblyResolver;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="assemblyResolver">The assembly resolver</param>
		public Resolver(IAssemblyResolver assemblyResolver) {
			this.assemblyResolver = assemblyResolver;
		}

		/// <inheritdoc/>
		public TypeDef Resolve(TypeRef typeRef) {
			if (typeRef == null)
				return null;

			var nonNestedTypeRef = TypeRef.GetNonNestedTypeRef(typeRef);
			if (nonNestedTypeRef == null)
				return null;

			var asmRef = nonNestedTypeRef.ResolutionScope as AssemblyRef;
			if (asmRef != null) {
				var asm = assemblyResolver.Resolve(asmRef, nonNestedTypeRef.Module);
				return asm == null ? null : asm.Find(typeRef);
			}

			var moduleDef = nonNestedTypeRef.ResolutionScope as ModuleDef;
			if (moduleDef != null)
				return moduleDef.Find(typeRef);

			var moduleRef = nonNestedTypeRef.ResolutionScope as ModuleRef;
			if (moduleRef != null) {
				if (nonNestedTypeRef.Module == null)
					return null;
				if (new SigComparer().Equals(moduleRef, nonNestedTypeRef.Module))
					return nonNestedTypeRef.Module.Find(typeRef);
				if (nonNestedTypeRef.Module.Assembly == null)
					return null;
				var resolvedModule = nonNestedTypeRef.Module.Assembly.FindModule(moduleRef.Name);
				return resolvedModule == null ? null : resolvedModule.Find(typeRef);
			}

			return null;
		}

		/// <inheritdoc/>
		public IMemberForwarded Resolve(MemberRef memberRef) {
			if (memberRef == null)
				return null;
			var method = memberRef.Class as MethodDef;
			if (method != null)
				return method;
			var declaringType = GetDeclaringType(memberRef);
			return declaringType == null ? null : declaringType.Resolve(memberRef);
		}

		TypeDef GetDeclaringType(MemberRef memberRef) {
			if (memberRef == null)
				return null;

			var parent = memberRef.Class;
			if (parent == null)
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
				if (globalType == null && module.Assembly != null) {
					var moduleDef = module.Assembly.FindModule(moduleRef.Name);
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
