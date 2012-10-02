namespace dot10.DotNet {
	/// <summary>
	/// Resolves types, methods, fields
	/// </summary>
	public interface IResolver : ITypeResolver, IMemberRefResolver {
	}

	/// <summary>
	/// Resolves types
	/// </summary>
	public interface ITypeResolver {
		/// <summary>
		/// Resolve a type
		/// </summary>
		/// <param name="typeRef">The type</param>
		/// <returns>A <see cref="TypeDef"/> instance or <c>null</c> if it couldn't be resolved</returns>
		TypeDef Resolve(TypeRef typeRef);
	}

	/// <summary>
	/// Resolves fields and methods
	/// </summary>
	public interface IMemberRefResolver {
		/// <summary>
		/// Resolves a method or a field
		/// </summary>
		/// <param name="memberRef">A method/field reference</param>
		/// <returns>A <see cref="MethodDef"/> or a <see cref="FieldDef"/> instance or <c>null</c>
		/// if it couldn't be resolved.</returns>
		IMemberForwarded Resolve(MemberRef memberRef);
	}
}
