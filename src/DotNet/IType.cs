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
	/// Interface to get the full name of a type
	/// </summary>
	public interface IType : IFullName, IOwnerModule, ICodedToken, IGenericParameterProvider {
		/// <summary>
		/// <c>true</c> if it's a value type
		/// </summary>
		bool IsValueType { get; }

		/// <summary>
		/// Returns the name of this type
		/// </summary>
		string TypeName { get; }

		/// <summary>
		/// Returns the reflection name of this type
		/// </summary>
		string ReflectionName { get; }

		/// <summary>
		/// Returns the namespace of this type
		/// </summary>
		string Namespace { get; }

		/// <summary>
		/// Returns the reflection namespace of this type
		/// </summary>
		string ReflectionNamespace { get; }

		/// <summary>
		/// Returns the reflection name of this type. See also <see cref="AssemblyQualifiedName"/>.
		/// </summary>
		string ReflectionFullName { get; }

		/// <summary>
		/// Returns the reflection name of this type, and includes the assembly name where the
		/// type is located. It can be passed to <see cref="System.Type.GetType(string)"/> to
		/// load the type.
		/// </summary>
		string AssemblyQualifiedName { get; }

		/// <summary>
		/// Gets the assembly where this type is defined
		/// </summary>
		IAssembly DefinitionAssembly { get; }

		/// <summary>
		/// Gets the scope, which is different from <see cref="DefinitionAssembly"/> since it
		/// can differentiate between modules within the same assembly.
		/// </summary>
		IScope Scope { get; }

		/// <summary>
		/// Gets the type whose scope is returned by <see cref="Scope"/> and whose assembly
		/// is returned by <see cref="DefinitionAssembly"/>. This is always a
		/// <see cref="TypeDef"/>, <see cref="TypeRef"/> or <c>null</c>. It can also be a
		/// nested <see cref="TypeRef"/>.
		/// For example, if this type is a System.String&amp;, then this value is a System.String.
		/// If it's a generic instance type (eg. List&lt;int&gt;), then the generic type is
		/// returned (eg. List&lt;T&gt;). In other words, the first <see cref="TypeDef"/> or
		/// <see cref="TypeRef"/> that is found (without searching generic arguments) is returned.
		/// </summary>
		ITypeDefOrRef ScopeType { get; }
	}

	public static partial class Extensions {
		/// <summary>
		/// Returns <see cref="IType.ScopeType"/>, but if it's a nested <see cref="TypeRef"/>,
		/// return the non-nested <see cref="TypeRef"/>
		/// </summary>
		/// <param name="type">this</param>
		/// <returns>The scope type</returns>
		public static ITypeDefOrRef GetNonNestedTypeRefScope(this IType type) {
			if (type == null)
				return null;
			var scopeType = type.ScopeType;
			var tr = scopeType as TypeRef;
			if (tr == null)
				return scopeType;
			for (int i = 0; i < 100; i++) {
				var dt = tr.ResolutionScope as TypeRef;
				if (dt == null)
					return tr;
				tr = dt;
			}
			return tr;
		}
	}
}
