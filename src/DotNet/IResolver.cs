// dnlib: See LICENSE.txt for more info

namespace dnlib.DotNet {
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
		/// Resolves a type
		/// </summary>
		/// <param name="typeRef">The type</param>
		/// <param name="sourceModule">The module that needs to resolve the type or <c>null</c></param>
		/// <returns>A <see cref="TypeDef"/> instance or <c>null</c> if it couldn't be resolved</returns>
		TypeDef Resolve(TypeRef typeRef, ModuleDef sourceModule);
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

	public static partial class Extensions {
		/// <summary>
		/// Resolves a type
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="typeRef">The type</param>
		/// <returns>A <see cref="TypeDef"/> instance or <c>null</c> if it couldn't be resolved</returns>
		public static TypeDef Resolve(this ITypeResolver self, TypeRef typeRef) => self.Resolve(typeRef, null);

		/// <summary>
		/// Resolves a type
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="typeRef">The type</param>
		/// <returns>A <see cref="TypeDef"/> instance</returns>
		/// <exception cref="TypeResolveException">If the type couldn't be resolved</exception>
		public static TypeDef ResolveThrow(this ITypeResolver self, TypeRef typeRef) => self.ResolveThrow(typeRef, null);

		/// <summary>
		/// Resolves a type
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="typeRef">The type</param>
		/// <param name="sourceModule">The module that needs to resolve the type or <c>null</c></param>
		/// <returns>A <see cref="TypeDef"/> instance</returns>
		/// <exception cref="TypeResolveException">If the type couldn't be resolved</exception>
		public static TypeDef ResolveThrow(this ITypeResolver self, TypeRef typeRef, ModuleDef sourceModule) {
			var type = self.Resolve(typeRef, sourceModule);
			if (type != null)
				return type;
			throw new TypeResolveException($"Could not resolve type: {typeRef} ({typeRef?.DefinitionAssembly})");
		}

		/// <summary>
		/// Resolves a method or a field
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="memberRef">A method/field reference</param>
		/// <returns>A <see cref="MethodDef"/> or a <see cref="FieldDef"/> instance</returns>
		/// <exception cref="MemberRefResolveException">If the method/field couldn't be resolved</exception>
		public static IMemberForwarded ResolveThrow(this IMemberRefResolver self, MemberRef memberRef) {
			var memberDef = self.Resolve(memberRef);
			if (memberDef != null)
				return memberDef;
			throw new MemberRefResolveException($"Could not resolve method/field: {memberRef} ({memberRef?.GetDefinitionAssembly()})");
		}

		/// <summary>
		/// Resolves a field
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="memberRef">A field reference</param>
		/// <returns>A <see cref="FieldDef"/> instance or <c>null</c> if it couldn't be resolved.</returns>
		public static FieldDef ResolveField(this IMemberRefResolver self, MemberRef memberRef) => self.Resolve(memberRef) as FieldDef;

		/// <summary>
		/// Resolves a field
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="memberRef">A field reference</param>
		/// <returns>A <see cref="FieldDef"/> instance or <c>null</c> if it couldn't be resolved.</returns>
		/// <exception cref="MemberRefResolveException">If the field couldn't be resolved</exception>
		public static FieldDef ResolveFieldThrow(this IMemberRefResolver self, MemberRef memberRef) {
			if (self.Resolve(memberRef) is FieldDef field)
				return field;
			throw new MemberRefResolveException($"Could not resolve field: {memberRef} ({memberRef?.GetDefinitionAssembly()})");
		}

		/// <summary>
		/// Resolves a method
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="memberRef">A method reference</param>
		/// <returns>A <see cref="MethodDef"/> instance or <c>null</c> if it couldn't be resolved.</returns>
		public static MethodDef ResolveMethod(this IMemberRefResolver self, MemberRef memberRef) => self.Resolve(memberRef) as MethodDef;

		/// <summary>
		/// Resolves a method
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="memberRef">A method reference</param>
		/// <returns>A <see cref="MethodDef"/> instance or <c>null</c> if it couldn't be resolved.</returns>
		/// <exception cref="MemberRefResolveException">If the method couldn't be resolved</exception>
		public static MethodDef ResolveMethodThrow(this IMemberRefResolver self, MemberRef memberRef) {
			if (self.Resolve(memberRef) is MethodDef method)
				return method;
			throw new MemberRefResolveException($"Could not resolve method: {memberRef} ({memberRef?.GetDefinitionAssembly()})");
		}
	}
}
