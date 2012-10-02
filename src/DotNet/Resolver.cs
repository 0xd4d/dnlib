using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// Resolves types, methods, fields
	/// </summary>
	public class Resolver : IResolver {
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
			var asm = assemblyResolver.Resolve(typeRef.DefinitionAssembly, typeRef.OwnerModule);
			return asm == null ? null : asm.Find(typeRef);
		}

		/// <inheritdoc/>
		public IMemberForwarded Resolve(MemberRef memberRef) {
			if (memberRef == null)
				return null;

			var parent = memberRef.Class;
			if (parent == null)
				return null;

			var declaringTypeDef = parent as TypeDef;
			if (declaringTypeDef != null)
				return declaringTypeDef.Resolve(memberRef);

			var declaringTypeRef = parent as TypeRef;
			if (declaringTypeRef != null) {
				declaringTypeDef = Resolve(declaringTypeRef);
				return declaringTypeDef == null ? null : declaringTypeDef.Resolve(memberRef);
			}

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
				return globalType == null ? null : globalType.Resolve(memberRef);
			}

			// parent is Method or TypeSpec
			return null;
		}
	}
}
