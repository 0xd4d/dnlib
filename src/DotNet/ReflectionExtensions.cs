// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace dnlib.DotNet {
	/// <summary>
	/// Extension methods for reflection types, methods, fields
	/// </summary>
	static class ReflectionExtensions {
		public static void GetTypeNamespaceAndName_TypeDefOrRef(this Type type, out string @namespace, out string name) {
			Debug.Assert(type.IsTypeDef());
			name = Unescape(type.Name) ?? string.Empty;
			if (!type.IsNested)
				@namespace = type.Namespace ?? string.Empty;
			else {
				var declTypeFullName = Unescape(type.DeclaringType.FullName);
				var typeFullName = Unescape(type.FullName);
				if (declTypeFullName.Length + 1 + name.Length == typeFullName.Length)
					@namespace = string.Empty;
				else
					@namespace = typeFullName.Substring(declTypeFullName.Length + 1, typeFullName.Length - declTypeFullName.Length - 1 - name.Length - 1);
			}
		}

		/// <summary>
		/// Checks whether it's a <see cref="ElementType.SZArray"/>
		/// </summary>
		/// <param name="self">The type</param>
		public static bool IsSZArray(this Type self) {
			if (self is null || !self.IsArray)
				return false;
#if NETCOREAPP
			return self.IsSZArray;
#else
			// .NET Core / .NET 5+ have a public Type.IsSZArray, .NET Framework has an internal RuntimeType.IsSzArray
			const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			var type = self.GetType();
			var prop = type.GetProperty("IsSZArray", flags) ?? type.GetProperty("IsSzArray", flags);
			if (prop is not null && prop.PropertyType == typeof(bool) && prop.GetIndexParameters().Length == 0)
				return (bool)prop.GetValue(self, Array2.Empty<object>());
			if (self.GetArrayRank() != 1)
				return false;
			// Last resort. Avoid it if possible since the runtime creates the name recursively which can
			// overflow the stack if the element type is deeply nested.
			return (self.Name ?? string.Empty).EndsWith("[]");
#endif
		}

		/// <summary>
		/// Gets a <see cref="Type"/>'s <see cref="ElementType"/>
		/// </summary>
		/// <param name="a">The type</param>
		/// <returns>The type's element type</returns>
		public static ElementType GetElementType2(this Type a) {
			if (a is null)
				return ElementType.End;	// Any invalid one is good enough
			if (a.IsArray)
				return IsSZArray(a) ? ElementType.SZArray : ElementType.Array;
			if (a.IsByRef)
				return ElementType.ByRef;
			if (a.IsPointer)
				return ElementType.Ptr;
			if (a.IsFunctionPointer2())
				return ElementType.FnPtr;
			if (a.IsGenericParameter)
				return a.DeclaringMethod is null ? ElementType.Var : ElementType.MVar;
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
		/// <c>true</c> if the runtime has function pointer types (.NET 8+). Older runtimes use <see cref="IntPtr"/>
		/// for function pointers in reflection signatures.
		/// </summary>
		public static bool SupportsFunctionPointerTypes =>
#if NET8_0_OR_GREATER
			true;
#else
			FnPtrReflection.isFunctionPointer is not null;
#endif

		/// <summary>
		/// Same as .NET 8+ <c>Type.IsFunctionPointer</c>, returns <c>false</c> on older runtimes
		/// </summary>
		/// <param name="self">The type</param>
		public static bool IsFunctionPointer2(this Type self) {
			if (self is null)
				return false;
#if NET8_0_OR_GREATER
			return self.IsFunctionPointer;
#else
			var f = FnPtrReflection.isFunctionPointer;
			return f is not null && f(self);
#endif
		}

		/// <summary>
		/// Same as .NET 8+ <c>Type.IsUnmanagedFunctionPointer</c>
		/// </summary>
		/// <param name="self">A function pointer type</param>
		public static bool IsUnmanagedFunctionPointer2(this Type self) {
#if NET8_0_OR_GREATER
			return self.IsUnmanagedFunctionPointer;
#else
			var f = FnPtrReflection.isUnmanagedFunctionPointer;
			return f is not null && f(self);
#endif
		}

		/// <summary>
		/// Same as .NET 8+ <c>Type.GetFunctionPointerReturnType()</c>
		/// </summary>
		/// <param name="self">A function pointer type</param>
		public static Type GetFunctionPointerReturnType2(this Type self) {
#if NET8_0_OR_GREATER
			return self.GetFunctionPointerReturnType();
#else
			return FnPtrReflection.getFunctionPointerReturnType?.Invoke(self);
#endif
		}

		/// <summary>
		/// Same as .NET 8+ <c>Type.GetFunctionPointerParameterTypes()</c>
		/// </summary>
		/// <param name="self">A function pointer type</param>
		public static Type[] GetFunctionPointerParameterTypes2(this Type self) {
#if NET8_0_OR_GREATER
			return self.GetFunctionPointerParameterTypes();
#else
			return FnPtrReflection.getFunctionPointerParameterTypes?.Invoke(self) ?? Type.EmptyTypes;
#endif
		}

		/// <summary>
		/// Same as .NET 8+ <c>Type.GetFunctionPointerCallingConventions()</c>. Only modified types return
		/// calling conventions.
		/// </summary>
		/// <param name="self">A function pointer type</param>
		public static Type[] GetFunctionPointerCallingConventions2(this Type self) {
#if NET8_0_OR_GREATER
			return self.GetFunctionPointerCallingConventions();
#else
			return FnPtrReflection.getFunctionPointerCallingConventions?.Invoke(self) ?? Type.EmptyTypes;
#endif
		}

		/// <summary>
		/// Same as .NET 8+ <c>Type.GetRequiredCustomModifiers()</c>. Only modified types have modifiers.
		/// </summary>
		/// <param name="self">The type</param>
		public static Type[] GetRequiredCustomModifiers2(this Type self) {
#if NET8_0_OR_GREATER
			return self.GetRequiredCustomModifiers();
#else
			return FnPtrReflection.getRequiredCustomModifiers?.Invoke(self) ?? Type.EmptyTypes;
#endif
		}

		/// <summary>
		/// Same as .NET 8+ <c>Type.GetOptionalCustomModifiers()</c>. Only modified types have modifiers.
		/// </summary>
		/// <param name="self">The type</param>
		public static Type[] GetOptionalCustomModifiers2(this Type self) {
#if NET8_0_OR_GREATER
			return self.GetOptionalCustomModifiers();
#else
			return FnPtrReflection.getOptionalCustomModifiers?.Invoke(self) ?? Type.EmptyTypes;
#endif
		}

		/// <summary>
		/// Same as .NET 8+ <c>ParameterInfo.GetModifiedParameterType()</c>, returns <c>null</c> on older runtimes
		/// </summary>
		/// <param name="self">The parameter</param>
		public static Type GetModifiedParameterType2(this ParameterInfo self) {
			try {
#if NET8_0_OR_GREATER
				return self.GetModifiedParameterType();
#else
				return FnPtrReflection.getModifiedParameterType?.Invoke(self);
#endif
			}
			catch (NotSupportedException) {
				return null;
			}
			catch (NotImplementedException) {
				return null;
			}
		}

		/// <summary>
		/// Same as .NET 8+ <c>FieldInfo.GetModifiedFieldType()</c>, returns <c>null</c> on older runtimes
		/// </summary>
		/// <param name="self">The field</param>
		public static Type GetModifiedFieldType2(this FieldInfo self) {
			try {
#if NET8_0_OR_GREATER
				return self.GetModifiedFieldType();
#else
				return FnPtrReflection.getModifiedFieldType?.Invoke(self);
#endif
			}
			catch (NotSupportedException) {
				return null;
			}
			catch (NotImplementedException) {
				return null;
			}
		}

		/// <summary>
		/// Checks whether <paramref name="type"/> is a function pointer or has a function pointer element type
		/// </summary>
		/// <param name="type">The type</param>
		public static bool ContainsFunctionPointer(this Type type) {
			if (!SupportsFunctionPointerTypes)
				return false;
			// Function pointers can't be generic arguments so only check element types
			for (int i = 0; type is not null && i < 1000; i++) {
				if (type.IsFunctionPointer2())
					return true;
				if (!type.HasElementType)
					break;
				type = type.GetElementType();
			}
			return false;
		}

		/// <summary>
		/// Gets the unmanaged calling convention of a function pointer type. Returns <c>null</c> if it's
		/// a managed function pointer (reflection doesn't say if it's <see cref="CallingConvention.Default"/>
		/// or <see cref="CallingConvention.VarArg"/>) or if <paramref name="modifiedType"/> is <c>null</c>
		/// (only modified types have calling conventions).
		/// </summary>
		/// <param name="type">Function pointer type</param>
		/// <param name="modifiedType">Modified type of <paramref name="type"/> or <c>null</c></param>
		/// <param name="alternative">Set to another calling convention that's also possible or <c>null</c>.
		/// Reflection can't tell <c>unmanaged modopt(CallConvX)</c> from <c>X modopt(CallConvX)</c>.</param>
		public static CallingConvention? GetFunctionPointerCallingConvention(Type type, Type modifiedType, out CallingConvention? alternative) {
			alternative = null;
			if (modifiedType is null || !type.IsUnmanagedFunctionPointer2())
				return null;
			// If the sig's calling convention is C, StdCall, ThisCall or FastCall, the runtime returns only
			// that calling convention and ignores the return type's CallConv* modifiers. If it's Unmanaged,
			// it returns the return type's CallConv* modifiers.
			var callConvs = modifiedType.GetFunctionPointerCallingConventions2();
			if (callConvs.Length != 1 || !IsCallConvType(callConvs[0], out var name))
				return CallingConvention.Unmanaged;
			CallingConvention cc;
			switch (name) {
			case "CallConvCdecl":		cc = CallingConvention.C; break;
			case "CallConvStdcall":		cc = CallingConvention.StdCall; break;
			case "CallConvThiscall":	cc = CallingConvention.ThisCall; break;
			case "CallConvFastcall":	cc = CallingConvention.FastCall; break;
			default:					return CallingConvention.Unmanaged;
			}
			// If the return type's only CallConv* modifier is the same type, it could also be Unmanaged
			int count = 0;
			bool same = false;
			var modRetType = modifiedType.GetFunctionPointerReturnType2();
			if (modRetType is not null) {
				foreach (var modifier in modRetType.GetOptionalCustomModifiers2()) {
					if (IsCallConvType(modifier, out var modName)) {
						count++;
						same = modName == name;
					}
				}
			}
			if (count == 1 && same) {
				alternative = cc;
				return CallingConvention.Unmanaged;
			}
			return cc;
		}

		static bool IsCallConvType(Type type, out string name) {
			name = type.Name;
			return !type.IsNested && type.Namespace == "System.Runtime.CompilerServices" && name.StartsWith("CallConv", StringComparison.Ordinal);
		}

		/// <summary>
		/// Same as .NET 8+ <c>PropertyInfo.GetModifiedPropertyType()</c>, returns <c>null</c> on older runtimes
		/// </summary>
		/// <param name="self">The property</param>
		public static Type GetModifiedPropertyType2(this PropertyInfo self) {
			try {
#if NET8_0_OR_GREATER
				return self.GetModifiedPropertyType();
#else
				return FnPtrReflection.getModifiedPropertyType?.Invoke(self);
#endif
			}
			catch (NotSupportedException) {
				return null;
			}
			catch (NotImplementedException) {
				return null;
			}
		}

#if !NET8_0_OR_GREATER
		static class FnPtrReflection {
			public static readonly Func<Type, bool> isFunctionPointer = CreateGetter<Type, bool>("IsFunctionPointer");
			public static readonly Func<Type, bool> isUnmanagedFunctionPointer = CreateGetter<Type, bool>("IsUnmanagedFunctionPointer");
			public static readonly Func<Type, Type> getFunctionPointerReturnType = CreateMethod<Type, Type>("GetFunctionPointerReturnType");
			public static readonly Func<Type, Type[]> getFunctionPointerParameterTypes = CreateMethod<Type, Type[]>("GetFunctionPointerParameterTypes");
			public static readonly Func<Type, Type[]> getFunctionPointerCallingConventions = CreateMethod<Type, Type[]>("GetFunctionPointerCallingConventions");
			public static readonly Func<Type, Type[]> getRequiredCustomModifiers = CreateMethod<Type, Type[]>("GetRequiredCustomModifiers");
			public static readonly Func<Type, Type[]> getOptionalCustomModifiers = CreateMethod<Type, Type[]>("GetOptionalCustomModifiers");
			public static readonly Func<ParameterInfo, Type> getModifiedParameterType = CreateMethod<ParameterInfo, Type>("GetModifiedParameterType");
			public static readonly Func<FieldInfo, Type> getModifiedFieldType = CreateMethod<FieldInfo, Type>("GetModifiedFieldType");
			public static readonly Func<PropertyInfo, Type> getModifiedPropertyType = CreateMethod<PropertyInfo, Type>("GetModifiedPropertyType");

			static Func<T, TResult> CreateGetter<T, TResult>(string name) {
				var prop = typeof(T).GetProperty(name, BindingFlags.Public | BindingFlags.Instance, null, typeof(TResult), Type.EmptyTypes, null);
				return CreateDelegate<T, TResult>(prop?.GetGetMethod());
			}

			static Func<T, TResult> CreateMethod<T, TResult>(string name) =>
				CreateDelegate<T, TResult>(typeof(T).GetMethod(name, BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null));

			static Func<T, TResult> CreateDelegate<T, TResult>(MethodInfo method) {
				if (method is null || method.ReturnType != typeof(TResult))
					return null;
				try {
					return (Func<T, TResult>)Delegate.CreateDelegate(typeof(Func<T, TResult>), method);
				}
				catch (ArgumentException) {
					return null;
				}
			}
		}
#endif

		/// <summary>
		/// Returns <c>true</c> if <paramref name="type"/> is a generic type, but
		/// not a generic type definition, i.e., a TypeSpec.
		/// </summary>
		/// <param name="type">The type</param>
		public static bool IsGenericButNotGenericTypeDefinition(this Type type) =>
			type is not null && !type.IsGenericTypeDefinition && type.IsGenericType;

		/// <summary>
		/// Returns <c>true</c> if <paramref name="mb"/> is a generic method, but
		/// not a generic method definition, i.e., a MethodSpec.
		/// </summary>
		/// <param name="mb">The method</param>
		public static bool IsGenericButNotGenericMethodDefinition(this MethodBase mb) =>
			mb is not null && !mb.IsGenericMethodDefinition && mb.IsGenericMethod;

		/// <summary>
		/// Checks whether a parameter/prop/event type should be treated as if it is really a
		/// generic instance type and not a generic type definition. In the .NET metadata (method
		/// sig), the parameter is a generic instance type, but the CLR treats it as if it's just
		/// a generic type def. This seems to happen only if the parameter type is exactly the same
		/// type as the declaring type, eg. a method similar to: <c>MyType&lt;!0&gt; MyType::SomeMethod()</c>.
		/// </summary>
		/// <param name="declaringType">Declaring type of field/method/event/property</param>
		/// <param name="t">Field/parameter/property/event type</param>
		internal static bool MustTreatTypeAsGenericInstType(this Type declaringType, Type t) =>
			declaringType is not null && declaringType.IsGenericTypeDefinition && t == declaringType;

		/// <summary>
		/// Checks whether <paramref name="type"/> is a type definition and not a type spec
		/// (eg. pointer or generic type instantiation)
		/// </summary>
		/// <param name="type">this</param>
		public static bool IsTypeDef(this Type type) =>
			type is not null && !type.HasElementType && (!type.IsGenericType || type.IsGenericTypeDefinition);

		internal static string Unescape(string name) {
			if (string.IsNullOrEmpty(name) || name.IndexOf('\\') < 0)
				return name;
			var sb = new StringBuilder(name.Length);
			for (int i = 0; i < name.Length; i++) {
				if (name[i] == '\\' && i < name.Length - 1 && IsReservedTypeNameChar(name[i + 1]))
					sb.Append(name[++i]);
				else
					sb.Append(name[i]);
			}
			return sb.ToString();
		}

		static bool IsReservedTypeNameChar(char c) {
			switch (c) {
			case ',':
			case '+':
			case '&':
			case '*':
			case '[':
			case ']':
			case '\\':
				return true;
			default:
				return false;
			}
		}
	}
}
