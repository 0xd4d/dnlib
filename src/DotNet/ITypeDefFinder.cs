// dnlib: See LICENSE.txt for more info

﻿namespace dnlib.DotNet {
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
		/// Finds a <see cref="TypeDef"/>. <paramref name="typeRef"/>'s scope (i.e., module or
		/// assembly) is ignored when looking up the type.
		/// </summary>
		/// <param name="typeRef">The type ref</param>
		/// <returns>An existing <see cref="TypeDef"/> or <c>null</c> if it wasn't found.</returns>
		TypeDef Find(TypeRef typeRef);
	}

	public static partial class Extensions {
		/// <summary>
		/// Finds a <see cref="TypeDef"/>. Its scope (i.e., module or assembly) is ignored when
		/// looking up the type.
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="typeRef">The type ref</param>
		/// <returns>An existing <see cref="TypeDef"/> or <c>null</c> if it wasn't found.</returns>
		/// <exception cref="TypeResolveException">If type couldn't be found</exception>
		public static TypeDef FindThrow(this ITypeDefFinder self, TypeRef typeRef) {
			var type = self.Find(typeRef);
			if (type != null)
				return type;
			throw new TypeResolveException(string.Format("Could not find type: {0}", typeRef));
		}

		/// <summary>
		/// Finds a <see cref="TypeDef"/>
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="fullName">Full name of the type (no assembly information)</param>
		/// <param name="isReflectionName"><c>true</c> if it's a reflection name, and nested
		/// type names are separated by a <c>+</c> character. If <c>false</c>, nested type names
		/// are separated by a <c>/</c> character.</param>
		/// <returns>An existing <see cref="TypeDef"/></returns>
		/// <exception cref="TypeResolveException">If type couldn't be found</exception>
		public static TypeDef FindThrow(this ITypeDefFinder self, string fullName, bool isReflectionName) {
			var type = self.Find(fullName, isReflectionName);
			if (type != null)
				return type;
			throw new TypeResolveException(string.Format("Could not find type: {0}", fullName));
		}

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
		/// <param name="fullName">Full name of the type (no assembly information). Nested types are separated by <c>/</c></param>
		/// <returns>An existing <see cref="TypeDef"/></returns>
		/// <exception cref="TypeResolveException">If type couldn't be found</exception>
		public static TypeDef FindNormalThrow(this ITypeDefFinder self, string fullName) {
			var type = self.Find(fullName, false);
			if (type != null)
				return type;
			throw new TypeResolveException(string.Format("Could not find type: {0}", fullName));
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
		/// Finds a <see cref="TypeDef"/>
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="fullName">Full name of the type (no assembly information). Nested types are separated by <c>+</c></param>
		/// <returns>An existing <see cref="TypeDef"/></returns>
		/// <exception cref="TypeResolveException">If type couldn't be found</exception>
		public static TypeDef FindReflectionThrow(this ITypeDefFinder self, string fullName) {
			var type = self.Find(fullName, true);
			if (type != null)
				return type;
			throw new TypeResolveException(string.Format("Could not find type: {0}", fullName));
		}

		/// <summary>
		/// Checks whether a <see cref="TypeDef"/> exists. <paramref name="typeRef"/>'s scope (i.e.,
		/// module or assembly) is ignored when looking up the type.
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="typeRef">The type ref</param>
		/// <returns><c>true</c> if the <see cref="TypeDef"/> exists, <c>false</c> otherwise</returns>
		public static bool TypeExists(this ITypeDefFinder self, TypeRef typeRef) {
			return self.Find(typeRef) != null;
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
