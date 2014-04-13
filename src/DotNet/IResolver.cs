/*
    Copyright (C) 2012-2014 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

﻿namespace dnlib.DotNet {
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

	public static partial class Extensions {
		/// <summary>
		/// Resolves a type
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="typeRef">The type</param>
		/// <returns>A <see cref="TypeDef"/> instance</returns>
		/// <exception cref="TypeResolveException">If the type couldn't be resolved</exception>
		public static TypeDef ResolveThrow(this ITypeResolver self, TypeRef typeRef) {
			var type = self.Resolve(typeRef);
			if (type != null)
				return type;
			throw new TypeResolveException(string.Format("Could not resolve type: {0}", typeRef));
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
			throw new MemberRefResolveException(string.Format("Could not resolve method/field: {0}", memberRef));
		}

		/// <summary>
		/// Resolves a field
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="memberRef">A field reference</param>
		/// <returns>A <see cref="FieldDef"/> instance or <c>null</c> if it couldn't be resolved.</returns>
		public static FieldDef ResolveField(this IMemberRefResolver self, MemberRef memberRef) {
			return self.Resolve(memberRef) as FieldDef;
		}

		/// <summary>
		/// Resolves a field
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="memberRef">A field reference</param>
		/// <returns>A <see cref="FieldDef"/> instance or <c>null</c> if it couldn't be resolved.</returns>
		/// <exception cref="MemberRefResolveException">If the field couldn't be resolved</exception>
		public static FieldDef ResolveFieldThrow(this IMemberRefResolver self, MemberRef memberRef) {
			var field = self.Resolve(memberRef) as FieldDef;
			if (field != null)
				return field;
			throw new MemberRefResolveException(string.Format("Could not resolve field: {0}", memberRef));
		}

		/// <summary>
		/// Resolves a method
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="memberRef">A method reference</param>
		/// <returns>A <see cref="MethodDef"/> instance or <c>null</c> if it couldn't be resolved.</returns>
		public static MethodDef ResolveMethod(this IMemberRefResolver self, MemberRef memberRef) {
			return self.Resolve(memberRef) as MethodDef;
		}

		/// <summary>
		/// Resolves a method
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="memberRef">A method reference</param>
		/// <returns>A <see cref="MethodDef"/> instance or <c>null</c> if it couldn't be resolved.</returns>
		/// <exception cref="MemberRefResolveException">If the method couldn't be resolved</exception>
		public static MethodDef ResolveMethodThrow(this IMemberRefResolver self, MemberRef memberRef) {
			var method = self.Resolve(memberRef) as MethodDef;
			if (method != null)
				return method;
			throw new MemberRefResolveException(string.Format("Could not resolve method: {0}", memberRef));
		}
	}
}
