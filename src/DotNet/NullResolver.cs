namespace dot10.DotNet {
	/// <summary>
	/// A resolver that always fails
	/// </summary>
	public sealed class NullResolver : IAssemblyResolver, IResolver {
		/// <summary>
		/// The one and only instance of this type
		/// </summary>
		public static readonly NullResolver Instance = new NullResolver();

		NullResolver() {
		}

		/// <inheritdoc/>
		public AssemblyDef Resolve(AssemblyNameInfo assembly, ModuleDef sourceModule) {
			return null;
		}

		/// <inheritdoc/>
		public bool AddToCache(AssemblyDef asm) {
			return true;
		}

		/// <inheritdoc/>
		public bool Remove(AssemblyDef asm) {
			return false;
		}

		/// <inheritdoc/>
		public TypeDef Resolve(TypeRef typeRef) {
			return null;
		}

		/// <inheritdoc/>
		public IMemberForwarded Resolve(MemberRef memberRef) {
			return null;
		}
	}
}
