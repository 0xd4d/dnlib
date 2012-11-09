namespace dot10.DotNet {
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
				var asm = assemblyResolver.Resolve(asmRef, nonNestedTypeRef.OwnerModule);
				return asm == null ? null : asm.Find(typeRef);
			}

			var moduleDef = nonNestedTypeRef.ResolutionScope as ModuleDef;
			if (moduleDef != null)
				return moduleDef.Find(typeRef);

			var moduleRef = nonNestedTypeRef.ResolutionScope as ModuleRef;
			if (moduleRef != null) {
				if (nonNestedTypeRef.OwnerModule == null)
					return null;
				if (new SigComparer().Equals(moduleRef, nonNestedTypeRef.OwnerModule))
					return nonNestedTypeRef.OwnerModule.Find(typeRef);
				if (nonNestedTypeRef.OwnerModule.Assembly == null)
					return null;
				var resolvedModule = nonNestedTypeRef.OwnerModule.Assembly.FindModule(moduleRef.Name);
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
				var ownerModule = memberRef.OwnerModule;
				if (ownerModule == null)
					return null;
				TypeDef globalType = null;
				if (new SigComparer(0).Equals(ownerModule, moduleRef))
					globalType = ownerModule.GlobalType;
				if (globalType == null && ownerModule.Assembly != null) {
					var moduleDef = ownerModule.Assembly.FindModule(moduleRef.Name);
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
