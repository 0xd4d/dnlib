namespace dot10.DotNet {
	/// <summary>
	/// Finds <see cref="TypeDef"/>s
	/// </summary>
	public interface ITypeDefFinder {
		/// <summary>
		/// Finds a <see cref="TypeDef"/>
		/// </summary>
		/// <param name="fullName">Full name of the type (no assembly information)</param>
		/// <param name="isReflectionName"><c>true</c> if it's a reflection name, and nested
		/// type names are separated by a <c>+</c> character. If <c>false</c>, nested type names
		/// are separated by a <c>/</c> character.</param>
		/// <returns>An existing <see cref="TypeDef"/> or <c>null</c> if it wasn't found.</returns>
		TypeDef Find(string fullName, bool isReflectionName);

		/// <summary>
		/// Finds a <see cref="TypeDef"/>. Its scope (i.e., module or assembly) is ignored when
		/// looking up the type.
		/// </summary>
		/// <param name="typeRef">The type ref</param>
		/// <returns>An existing <see cref="TypeDef"/> or <c>null</c> if it wasn't found.</returns>
		TypeDef Find(TypeRef typeRef);
	}

	static partial class Extensions {
		/// <summary>
		/// Finds a <see cref="TypeDef"/>
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="fullName">Full name of the type (no assembly information). Nested types are separated by <c>/</c></param>
		/// <returns>An existing <see cref="TypeDef"/> or <c>null</c> if it wasn't found.</returns>
		public static TypeDef FindNormal(this ITypeDefFinder self, string fullName) {
			return self.Find(fullName, false);
		}

		/// <summary>
		/// Finds a <see cref="TypeDef"/>
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="fullName">Full name of the type (no assembly information). Nested types are separated by <c>+</c></param>
		/// <returns>An existing <see cref="TypeDef"/> or <c>null</c> if it wasn't found.</returns>
		public static TypeDef FindReflection(this ITypeDefFinder self, string fullName) {
			return self.Find(fullName, true);
		}

		/// <summary>
		/// Checks whether a <see cref="TypeDef"/> exists
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="fullName">Full name of the type (no assembly information)</param>
		/// <param name="isReflectionName"><c>true</c> if it's a reflection name, and nested
		/// type names are separated by a <c>+</c> character. If <c>false</c>, nested type names
		/// are separated by a <c>/</c> character.</param>
		/// <returns><c>true</c> if the <see cref="TypeDef"/> exists, <c>false</c> otherwise</returns>
		public static bool TypeExists(this ITypeDefFinder self, string fullName, bool isReflectionName) {
			return self.Find(fullName, isReflectionName) != null;
		}

		/// <summary>
		/// Checks whether a <see cref="TypeDef"/> exists
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="fullName">Full name of the type (no assembly information). Nested types are separated by <c>/</c></param>
		/// <returns><c>true</c> if the <see cref="TypeDef"/> exists, <c>false</c> otherwise</returns>
		public static bool TypeExistsNormal(this ITypeDefFinder self, string fullName) {
			return self.Find(fullName, false) != null;
		}

		/// <summary>
		/// Checks whether a <see cref="TypeDef"/> exists
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="fullName">Full name of the type (no assembly information). Nested types are separated by <c>+</c></param>
		/// <returns><c>true</c> if the <see cref="TypeDef"/> exists, <c>false</c> otherwise</returns>
		public static bool TypeExistsReflection(this ITypeDefFinder self, string fullName) {
			return self.Find(fullName, true) != null;
		}
	}
}
