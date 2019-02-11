// dnlib: See LICENSE.txt for more info

namespace dnlib.DotNet {
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
		public AssemblyDef Resolve(IAssembly assembly, ModuleDef sourceModule) => null;

		/// <inheritdoc/>
		public TypeDef Resolve(TypeRef typeRef, ModuleDef sourceModule) => null;

		/// <inheritdoc/>
		public IMemberForwarded Resolve(MemberRef memberRef) => null;
	}
}
