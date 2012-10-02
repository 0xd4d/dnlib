namespace dot10.DotNet {
	/// <summary>
	/// A resolver that always fails
	/// </summary>
	public class NullResolver : IResolver {
		/// <summary>
		/// The one and only instance of this type
		/// </summary>
		public static readonly NullResolver Instance = new NullResolver();

		NullResolver() {
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

	/// <summary>
	/// An assembly resolver that always fails
	/// </summary>
	public class NullAssemblyResolver : IAssemblyResolver {
		/// <summary>
		/// The one and only instance of this type
		/// </summary>
		public static readonly NullAssemblyResolver Instance = new NullAssemblyResolver();

		NullAssemblyResolver() {
		}

		/// <inheritdoc/>
		public AssemblyDef Resolve(AssemblyNameInfo assembly, ModuleDef sourceModule) {
			return null;
		}
	}
}
