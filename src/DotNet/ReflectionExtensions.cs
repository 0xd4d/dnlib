// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Reflection;

namespace dnlib.DotNet {
	/// <summary>
	/// Extension methods for reflection types, methods, fields
	/// </summary>
	static class ReflectionExtensions {
		/// <summary>
		/// Checks whether it's a <see cref="ElementType.SZArray"/>
		/// </summary>
		/// <param name="self">The type</param>
		public static bool IsSZArray(this Type self) {
			if (self == null || !self.IsArray)
				return false;
			var prop = self.GetType().GetProperty("IsSzArray", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (prop != null)
				return (bool)prop.GetValue(self, new object[0]);
			return (self.Name ?? string.Empty).EndsWith("[]");
		}

		/// <summary>
		/// Gets a <see cref="Type"/>'s <see cref="ElementType"/>
		/// </summary>
		/// <param name="a">The type</param>
		/// <returns>The type's element type</returns>
		public static ElementType GetElementType2(this Type a) {
			if (a == null)
				return ElementType.End;	// Any invalid one is good enough
			if (a.IsArray)
				return IsSZArray(a) ? ElementType.SZArray : ElementType.Array;
			if (a.IsByRef)
				return ElementType.ByRef;
			if (a.IsPointer)
				return ElementType.Ptr;
			if (a.IsGenericParameter)
				return a.DeclaringMethod == null ? ElementType.Var : ElementType.MVar;
			if (a.IsGenericType && !a.IsGenericTypeDefinition)
				return ElementType.GenericInst;

			if (a == typeof(void))			return ElementType.Void;
			if (a == typeof(bool))			return ElementType.Boolean;
			if (a == typeof(char))			return ElementType.Char;
			if (a == typeof(sbyte))			return ElementType.I1;
			if (a == typeof(byte))			return ElementType.U1;
			if (a == typeof(short))			return ElementType.I2;
			if (a == typeof(ushort))		return ElementType.U2;
			if (a == typeof(int))			return ElementType.I4;
			if (a == typeof(uint))			return ElementType.U4;
			if (a == typeof(long))			return ElementType.I8;
			if (a == typeof(ulong))			return ElementType.U8;
			if (a == typeof(float))			return ElementType.R4;
			if (a == typeof(double))		return ElementType.R8;
			if (a == typeof(string))		return ElementType.String;
			if (a == typeof(TypedReference))return ElementType.TypedByRef;
			if (a == typeof(IntPtr))		return ElementType.I;
			if (a == typeof(UIntPtr))		return ElementType.U;
			if (a == typeof(object))		return ElementType.Object;

			return a.IsValueType ? ElementType.ValueType : ElementType.Class;
		}

		/// <summary>
		/// Returns <c>true</c> if <paramref name="mb"/> is a generic method, but
		/// not a generic method definition, i.e., a MethodSpec.
		/// </summary>
		/// <param name="mb">The method</param>
		public static bool IsGenericButNotGenericMethodDefinition(this MethodBase mb) {
			return mb != null && !mb.IsGenericMethodDefinition && mb.IsGenericMethod;
		}

		/// <summary>
		/// Checks whether a parameter/prop/event type should be treated as if it is really a
		/// generic instance type and not a generic type definition. In the .NET metadata (method
		/// sig), the parameter is a generic instance type, but the CLR treats it as if it's just
		/// a generic type def. This seems to happen only if the parameter type is exactly the same
		/// type as the declaring type, eg. a method similar to: <c>MyType&lt;!0&gt; MyType::SomeMethod()</c>.
		/// </summary>
		/// <param name="declaringType">Declaring type of method/event/property</param>
		/// <param name="t">Parameter/property/event type</param>
		internal static bool MustTreatTypeAsGenericInstType(this Type declaringType, Type t) {
			return declaringType != null &&
				declaringType.IsGenericTypeDefinition &&
				t == declaringType;
		}

		/// <summary>
		/// Checks whether <paramref name="type"/> is a type definition and not a type spec
		/// (eg. pointer or generic type instantiation)
		/// </summary>
		/// <param name="type">this</param>
		public static bool IsTypeDef(this Type type) {
			return type != null &&
				!type.HasElementType &&
				(!type.IsGenericType || type.IsGenericTypeDefinition);
		}
	}
}
