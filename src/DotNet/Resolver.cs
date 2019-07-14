// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;

namespace dnlib.DotNet {
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
			get => projectWinMDRefs;
			set => projectWinMDRefs = value;
		}
		bool projectWinMDRefs = true;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="assemblyResolver">The assembly resolver</param>
		public Resolver(IAssemblyResolver assemblyResolver) =>
			this.assemblyResolver = assemblyResolver ?? throw new ArgumentNullException(nameof(assemblyResolver));

		/// <inheritdoc/>
		public TypeDef Resolve(TypeRef typeRef, ModuleDef sourceModule) {
			if (typeRef is null)
				return null;

			if (ProjectWinMDRefs)
				typeRef = WinMDHelpers.ToCLR(typeRef.Module ?? sourceModule, typeRef) ?? typeRef;

			var nonNestedTypeRef = TypeRef.GetNonNestedTypeRef(typeRef);
			if (nonNestedTypeRef is null)
				return null;

			var nonNestedResolutionScope = nonNestedTypeRef.ResolutionScope;
			var nonNestedModule = nonNestedTypeRef.Module;
			if (nonNestedResolutionScope is AssemblyRef asmRef) {
				var asm = assemblyResolver.Resolve(asmRef, sourceModule ?? nonNestedModule);
				return asm is null ? null : asm.Find(typeRef) ?? ResolveExportedType(asm.Modules, typeRef, sourceModule);
			}

			if (nonNestedResolutionScope is ModuleDef moduleDef)
				return moduleDef.Find(typeRef) ?? ResolveExportedType(new ModuleDef[] { moduleDef }, typeRef, sourceModule);

			if (nonNestedResolutionScope is ModuleRef moduleRef) {
				if (nonNestedModule is null)
					return null;
				if (new SigComparer().Equals(moduleRef, nonNestedModule))
					return nonNestedModule.Find(typeRef) ??
						ResolveExportedType(new ModuleDef[] { nonNestedModule }, typeRef, sourceModule);
				var nonNestedAssembly = nonNestedModule.Assembly;
				if (nonNestedAssembly is null)
					return null;
				var resolvedModule = nonNestedAssembly.FindModule(moduleRef.Name);
				return resolvedModule is null ? null : resolvedModule.Find(typeRef) ??
						ResolveExportedType(new ModuleDef[] { resolvedModule }, typeRef, sourceModule);
			}

			return null;
		}

		TypeDef ResolveExportedType(IList<ModuleDef> modules, TypeRef typeRef, ModuleDef sourceModule) {
			for (int i = 0; i < 30; i++) {
				var exportedType = FindExportedType(modules, typeRef);
				if (exportedType is null)
					return null;

				var asmResolver = modules[0].Context.AssemblyResolver;
				var etAsm = asmResolver.Resolve(exportedType.DefinitionAssembly, sourceModule ?? typeRef.Module);
				if (etAsm is null)
					return null;

				var td = etAsm.Find(typeRef);
				if (!(td is null))
					return td;

				modules = etAsm.Modules;
			}

			return null;
		}

		static ExportedType FindExportedType(IList<ModuleDef> modules, TypeRef typeRef) {
			if (typeRef is null)
				return null;
			int count = modules.Count;
			for (int i = 0; i < count; i++) {
				var module = modules[i];
				var exportedTypes = module.ExportedTypes;
				int count2 = exportedTypes.Count;
				for (int j = 0; j < count2; j++) {
					var exportedType = exportedTypes[j];
					if (new SigComparer(SigComparerOptions.DontCompareTypeScope).Equals(exportedType, typeRef))
						return exportedType;
				}
			}
			return null;
		}

		/// <inheritdoc/>
		public IMemberForwarded Resolve(MemberRef memberRef) {
			if (memberRef is null)
				return null;
			if (ProjectWinMDRefs)
				memberRef = WinMDHelpers.ToCLR(memberRef.Module, memberRef) ?? memberRef;
			var parent = memberRef.Class;
			if (parent is MethodDef method)
				return method;
			return GetDeclaringType(memberRef, parent)?.Resolve(memberRef);
		}

		TypeDef GetDeclaringType(MemberRef memberRef, IMemberRefParent parent) {
			if (memberRef is null || parent is null)
				return null;

			if (parent is TypeSpec ts)
				parent = ts.ScopeType;

			if (parent is TypeDef declaringTypeDef)
				return declaringTypeDef;

			if (parent is TypeRef declaringTypeRef)
				return Resolve(declaringTypeRef, memberRef.Module);

			// A module ref is used to reference the global type of a module in the same
			// assembly as the current module.
			if (parent is ModuleRef moduleRef) {
				var module = memberRef.Module;
				if (module is null)
					return null;
				TypeDef globalType = null;
				if (new SigComparer().Equals(module, moduleRef))
					globalType = module.GlobalType;
				var modAsm = module.Assembly;
				if (globalType is null && !(modAsm is null)) {
					var moduleDef = modAsm.FindModule(moduleRef.Name);
					if (!(moduleDef is null))
						globalType = moduleDef.GlobalType;
				}
				return globalType;
			}

			if (parent is MethodDef method)
				return method.DeclaringType;

			return null;
		}
	}
}
