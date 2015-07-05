// dnlib: See LICENSE.txt for more info

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
		/// <c>true</c> to project WinMD types to CLR types, eg. <c>Windows.UI.Xaml.Interop.TypeName</c>
		/// gets converted to <c>System.Type</c> before trying to resolve the type. This is enabled
		/// by default.
		/// </summary>
		public bool ProjectWinMDRefs {
			get { return projectWinMDRefs; }
			set { projectWinMDRefs = value; }
		}
		bool projectWinMDRefs = true;

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
		public TypeDef Resolve(TypeRef typeRef, ModuleDef sourceModule) {
			if (typeRef == null)
				return null;

			if (ProjectWinMDRefs)
				typeRef = WinMDHelpers.ToCLR(typeRef.Module ?? sourceModule, typeRef) ?? typeRef;

			var nonNestedTypeRef = TypeRef.GetNonNestedTypeRef(typeRef);
			if (nonNestedTypeRef == null)
				return null;

			var nonNestedResolutionScope = nonNestedTypeRef.ResolutionScope;
			var nonNestedModule = nonNestedTypeRef.Module;
			var asmRef = nonNestedResolutionScope as AssemblyRef;
			if (asmRef != null) {
				var asm = assemblyResolver.Resolve(asmRef, sourceModule ?? nonNestedModule);
				return asm == null ? null : asm.Find(typeRef) ?? ResolveExportedType(asm.Modules, typeRef, sourceModule);
			}

			var moduleDef = nonNestedResolutionScope as ModuleDef;
			if (moduleDef != null)
				return moduleDef.Find(typeRef) ??
					ResolveExportedType(new ModuleDef[] { moduleDef }, typeRef, sourceModule);

			var moduleRef = nonNestedResolutionScope as ModuleRef;
			if (moduleRef != null) {
				if (nonNestedModule == null)
					return null;
				if (new SigComparer().Equals(moduleRef, nonNestedModule))
					return nonNestedModule.Find(typeRef) ??
						ResolveExportedType(new ModuleDef[] { nonNestedModule }, typeRef, sourceModule);
				var nonNestedAssembly = nonNestedModule.Assembly;
				if (nonNestedAssembly == null)
					return null;
				var resolvedModule = nonNestedAssembly.FindModule(moduleRef.Name);
				return resolvedModule == null ? null : resolvedModule.Find(typeRef) ??
						ResolveExportedType(new ModuleDef[] { resolvedModule }, typeRef, sourceModule);
			}

			return null;
		}

		TypeDef ResolveExportedType(IList<ModuleDef> modules, TypeRef typeRef, ModuleDef sourceModule) {
			for (int i = 0; i < 30; i++) {
				var exportedType = FindExportedType(modules, typeRef);
				if (exportedType == null)
					return null;

				var asmResolver = modules[0].Context.AssemblyResolver;
				var etAsm = asmResolver.Resolve(exportedType.DefinitionAssembly, sourceModule ?? typeRef.Module);
				if (etAsm == null)
					return null;

				var td = etAsm.Find(typeRef);
				if (td != null)
					return td;

				modules = etAsm.Modules;
			}

			return null;
		}

		static ExportedType FindExportedType(IList<ModuleDef> modules, TypeRef typeRef) {
			if (typeRef == null)
				return null;
			foreach (var module in modules.GetSafeEnumerable()) {
				foreach (var exportedType in module.ExportedTypes.GetSafeEnumerable()) {
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
			if (ProjectWinMDRefs)
				memberRef = WinMDHelpers.ToCLR(memberRef.Module, memberRef) ?? memberRef;
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

			var ts = parent as TypeSpec;
			if (ts != null)
				parent = ts.ScopeType;

			var declaringTypeDef = parent as TypeDef;
			if (declaringTypeDef != null)
				return declaringTypeDef;

			var declaringTypeRef = parent as TypeRef;
			if (declaringTypeRef != null)
				return Resolve(declaringTypeRef, memberRef.Module);

			// A module ref is used to reference the global type of a module in the same
			// assembly as the current module.
			var moduleRef = parent as ModuleRef;
			if (moduleRef != null) {
				var module = memberRef.Module;
				if (module == null)
					return null;
				TypeDef globalType = null;
				if (new SigComparer().Equals(module, moduleRef))
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

			return null;
		}
	}
}
