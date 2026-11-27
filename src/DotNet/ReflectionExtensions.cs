// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace dnlib.DotNet {
	/// <summary>
	/// Extension methods for reflection types, methods, fields
	/// </summary>
	static class ReflectionExtensions {
		public static void GetTypeNamespaceAndName_TypeDefOrRef(this Type type, out string @namespace, out string name) {
			Debug2.Assert(type.IsTypeDef());
			name = Unescape(type.Name) ?? string.Empty;
			if (!type.IsNested)
				@namespace = type.Namespace ?? string.Empty;
			else {
				var declTypeFullName = Unescape(type.DeclaringType!.FullName)!;
				var typeFullName = Unescape(type.FullName)!;
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
				return ElementType.End; // Any invalid one is good enough
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

			if (a == typeof(void)) return ElementType.Void;
			if (a == typeof(bool)) return ElementType.Boolean;
			if (a == typeof(char)) return ElementType.Char;
			if (a == typeof(sbyte)) return ElementType.I1;
			if (a == typeof(byte)) return ElementType.U1;
			if (a == typeof(short)) return ElementType.I2;
			if (a == typeof(ushort)) return ElementType.U2;
			if (a == typeof(int)) return ElementType.I4;
			if (a == typeof(uint)) return ElementType.U4;
			if (a == typeof(long)) return ElementType.I8;
			if (a == typeof(ulong)) return ElementType.U8;
			if (a == typeof(float)) return ElementType.R4;
			if (a == typeof(double)) return ElementType.R8;
			if (a == typeof(string)) return ElementType.String;
			if (a == typeof(TypedReference)) return ElementType.TypedByRef;
			if (a == typeof(IntPtr)) return ElementType.I;
			if (a == typeof(UIntPtr)) return ElementType.U;
			if (a == typeof(object)) return ElementType.Object;

			if (a.IsNativeValueType2())
				return ElementType.NativeValueTypeZapSig;
			return a.IsValueType ? ElementType.ValueType : ElementType.Class;
		}

		/// <summary>
		/// <c>true</c> if it's the CLR's native view of a type (<see cref="ElementType.NativeValueTypeZapSig"/> in
		/// a signature, "VALUETYPE" and the name of the type in System.Reflection). It has an element type but
		/// <see cref="Type.HasElementType"/> is <c>false</c>, and its <see cref="Type.Namespace"/> throws.
		/// </summary>
		internal static bool IsNativeValueType2([NotNullWhen(true)] this Type? a) =>
			a is not null && !a.HasElementType && !a.IsGenericParameter && !a.IsGenericType && !a.IsFunctionPointer2() && a.GetElementType() is not null;

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
		public static bool IsFunctionPointer2([NotNullWhen(true)] this Type? self) {
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
		public static Type? GetFunctionPointerReturnType2(this Type self) {
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
		/// Checks whether <paramref name="self"/> is a .NET 8+ modified type (returned by eg. <c>FieldInfo.GetModifiedFieldType()</c>).
		/// It has the custom modifiers but doesn't support most other members.
		/// </summary>
		/// <param name="self">The type</param>
		public static bool IsModifiedType(this Type self) {
			var modifiedType = ModifiedTypeReflection.modifiedType;
			return modifiedType is not null && modifiedType.IsAssignableFrom(self.GetType());
		}

		/// <summary>
		/// Checks whether <paramref name="modifiedType"/> (a .NET 8+ modified type of <paramref name="type"/> or <c>null</c>)
		/// is a generic type instantiated with its own generic parameters, eg. the type of a field whose type is its
		/// declaring type. It's a generic instance type in the signature, but reflection uses the generic type definition
		/// for both of them so <paramref name="type"/> (the unmodified type) is the generic type definition.
		/// </summary>
		/// <param name="modifiedType">The modified type or <c>null</c></param>
		/// <param name="type">The unmodified type</param>
		internal static bool IsGenericInstOfOwnGenericParams([NotNullWhen(true)] this Type? modifiedType, Type type) =>
			// A modified type is created from a signature which can't have a generic type definition (only a generic
			// instance type), so it's a generic instance type if its unmodified type is a generic type definition. Its
			// IsGenericTypeDefinition property can't be used: ModifiedType doesn't override it so it's always false.
			modifiedType is not null && type.IsGenericTypeDefinition;

		/// <summary>
		/// Gets the generic arguments of <paramref name="modifiedType"/> (a .NET 8+ modified type or <c>null</c>). They
		/// have the custom modifiers and function pointer calling conventions, eg. <c>List&lt;delegate* unmanaged[Cdecl]&lt;void&gt;[]&gt;</c>.
		/// Returns <c>null</c> if it's <c>null</c> or if it doesn't have the same number of generic arguments as the unmodified type.
		/// </summary>
		/// <param name="modifiedType">The modified type or <c>null</c></param>
		/// <param name="genericArguments">The generic arguments of the unmodified type</param>
		internal static Type[]? GetModifiedGenericArguments(this Type? modifiedType, Type[] genericArguments) {
			if (modifiedType is null || !modifiedType.IsGenericType)
				return null;
			var modifiedArgs = modifiedType.GetGenericArguments();
			return modifiedArgs.Length == genericArguments.Length ? modifiedArgs : null;
		}

		static class ModifiedTypeReflection {
			// The class is internal so typeof() can't be used. It's looked up in CoreLib so a user type with the same
			// name can't match. It's null if the runtime doesn't have modified types (.NET Framework, < .NET 8).
			public static readonly Type? modifiedType = Type.GetType("System.Reflection.ModifiedType, System.Private.CoreLib", throwOnError: false);
		}

		/// <summary>
		/// Same as .NET 8+ <c>ParameterInfo.GetModifiedParameterType()</c>, returns <c>null</c> on older runtimes
		/// </summary>
		/// <param name="self">The parameter</param>
		public static Type? GetModifiedParameterType2(this ParameterInfo self) {
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
		public static Type? GetModifiedFieldType2(this FieldInfo self) {
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
		public static bool ContainsFunctionPointer([NotNullWhen(true)] this Type? type) {
			if (!SupportsFunctionPointerTypes)
				return false;
			return ContainsFunctionPointer(type, 0);
		}

		static bool ContainsFunctionPointer([NotNullWhen(true)] Type? type, int depth) {
			// Function pointers can't be generic arguments but eg. arrays of function pointers can
			for (; type is not null && depth < 1000; depth++) {
				if (type.IsFunctionPointer2())
					return true;
				if (type.IsGenericType && !type.IsGenericTypeDefinition) {
					foreach (var genArg in type.GetGenericArguments()) {
						if (ContainsFunctionPointer(genArg, depth + 1))
							return true;
					}
					return false;
				}
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
		public static CallingConvention? GetFunctionPointerCallingConvention(Type type, Type? modifiedType, out CallingConvention? alternative) {
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
			case "CallConvCdecl": cc = CallingConvention.C; break;
			case "CallConvStdcall": cc = CallingConvention.StdCall; break;
			case "CallConvThiscall": cc = CallingConvention.ThisCall; break;
			case "CallConvFastcall": cc = CallingConvention.FastCall; break;
			default: return CallingConvention.Unmanaged;
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
		public static Type? GetModifiedPropertyType2(this PropertyInfo self) {
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

		// NativeAOT has no metadata tokens: MemberInfo.MetadataToken throws InvalidOperationException and
		// Module.ResolveMethod()/ResolveField() throw PlatformNotSupportedException.
		static readonly bool hasMetadataTokens = CalculateHasMetadataTokens();

		static bool CalculateHasMetadataTokens() {
			try {
				return typeof(object).MetadataToken != 0;
			}
			catch (InvalidOperationException) {
				return false;
			}
		}

		/// <summary>
		/// Gets the metadata token of <paramref name="member"/>. Returns <c>false</c> if the runtime has no
		/// metadata tokens (NativeAOT).
		/// </summary>
		public static bool TryGetMetadataToken(this MemberInfo member, out int token) {
			if (!hasMetadataTokens) {
				token = 0;
				return false;
			}
			token = member.MetadataToken;
			return true;
		}

#if NET
		/// <summary>
		/// Gets the member in the generic type definition if the declaring type of <paramref name="member"/> is a
		/// generic type instance, else <paramref name="member"/> is returned. Doesn't use metadata tokens.
		/// </summary>
		static MemberInfo GetMemberOfGenericTypeDefinition(MemberInfo member) {
			var declType = member.DeclaringType;
			if (!declType.IsGenericButNotGenericTypeDefinition())
				return member;
			try {
				return declType.GetGenericTypeDefinition().GetMemberWithSameMetadataDefinitionAs(member);
			}
			catch (ArgumentException) {
				return member;
			}
		}
#endif

		const string ResolveOwnTokenJustification = "The token is the member's own token and it's resolved in the member's own module. The trimmer " +
			"keeps the definition of any member that's kept, and the tokens in a trimmed module are consistent with that module.";

		/// <summary>
		/// Gets the method definition of <paramref name="method"/>, eg. the method in the generic type definition
		/// if its declaring type is a generic type instance
		/// </summary>
		[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = ResolveOwnTokenJustification)]
		public static MethodBase ResolveMethodDefinition(this MethodBase method) {
#if NET
			if (!hasMetadataTokens) {
				if (method is MethodInfo gmi && gmi.IsGenericMethod && !gmi.IsGenericMethodDefinition)
					method = gmi.GetGenericMethodDefinition();
				return (MethodBase)GetMemberOfGenericTypeDefinition(method);
			}
#endif
			var def = method.Module.ResolveMethod(method.MetadataToken)!;
			// The runtime can return the generic method instantiated with its own generic parameters instead of the
			// generic method definition, eg. Base64Helper.Base64DecoderByte.DecodeWithWhiteSpaceBlockwiseWrapper<T>()
			// in .NET 10's System.Private.CoreLib.
			if (def is MethodInfo mi && mi.IsGenericMethod && !mi.IsGenericMethodDefinition)
				def = mi.GetGenericMethodDefinition();
			return def;
		}

		/// <summary>
		/// Gets the field definition of <paramref name="field"/>, eg. the field in the generic type definition
		/// if its declaring type is a generic type instance
		/// </summary>
		[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = ResolveOwnTokenJustification)]
		public static FieldInfo ResolveFieldDefinition(this FieldInfo field) {
#if NET
			if (!hasMetadataTokens)
				return (FieldInfo)GetMemberOfGenericTypeDefinition(field);
#endif
			return field.Module.ResolveField(field.MetadataToken)!;
		}

		const string ResolveOwnDefinitionJustification = "The property/event is looked up in the generic type definition of its own declaring type. " +
			"If the trimmer removed it, the original property/event is returned.";
		const BindingFlags DeclaredMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

		/// <summary>
		/// Gets the property definition of <paramref name="property"/>, eg. the property in the generic type definition
		/// if its declaring type is a generic type instance
		/// </summary>
		[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = ResolveOwnDefinitionJustification)]
		public static PropertyInfo ResolvePropertyDefinition(this PropertyInfo property) {
			var declType = property.DeclaringType;
			if (!declType.IsGenericButNotGenericTypeDefinition())
				return property;
#if NET
			if (!hasMetadataTokens)
				return (PropertyInfo)GetMemberOfGenericTypeDefinition(property);
#endif
			int token = property.MetadataToken;
			foreach (var p in declType.GetGenericTypeDefinition().GetProperties(DeclaredMembers)) {
				if (p.MetadataToken == token)
					return p;
			}
			return property;
		}

		/// <summary>
		/// Gets the event definition of <paramref name="evt"/>, eg. the event in the generic type definition
		/// if its declaring type is a generic type instance
		/// </summary>
		[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = ResolveOwnDefinitionJustification)]
		public static EventInfo ResolveEventDefinition(this EventInfo evt) {
			var declType = evt.DeclaringType;
			if (!declType.IsGenericButNotGenericTypeDefinition())
				return evt;
#if NET
			if (!hasMetadataTokens)
				return (EventInfo)GetMemberOfGenericTypeDefinition(evt);
#endif
			int token = evt.MetadataToken;
			foreach (var e in declType.GetGenericTypeDefinition().GetEvents(DeclaredMembers)) {
				if (e.MetadataToken == token)
					return e;
			}
			return evt;
		}

		/// <summary>
		/// Instantiates <paramref name="methodDef"/> (a method in a generic type definition) with the generic method
		/// arguments of <paramref name="instance"/>. The result is only used to compare and hash signatures.
		/// </summary>
		[UnconditionalSuppressMessage("Trimming", "IL2060", Justification = "The result is only used to read the signature, it's never invoked")]
		[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The result is only used to read the signature, it's never invoked, so no native code is needed")]
		public static MethodBase MakeGenericMethodForSignature(this MethodInfo methodDef, MethodBase instance) => methodDef.MakeGenericMethod(instance.GetGenericArguments());

#if !NET8_0_OR_GREATER
		static class FnPtrReflection {
			public static readonly Func<Type, bool>? isFunctionPointer = CreateGetter<Type, bool>("IsFunctionPointer");
			public static readonly Func<Type, bool>? isUnmanagedFunctionPointer = CreateGetter<Type, bool>("IsUnmanagedFunctionPointer");
			public static readonly Func<Type, Type>? getFunctionPointerReturnType = CreateMethod<Type, Type>("GetFunctionPointerReturnType");
			public static readonly Func<Type, Type[]>? getFunctionPointerParameterTypes = CreateMethod<Type, Type[]>("GetFunctionPointerParameterTypes");
			public static readonly Func<Type, Type[]>? getFunctionPointerCallingConventions = CreateMethod<Type, Type[]>("GetFunctionPointerCallingConventions");
			public static readonly Func<Type, Type[]>? getRequiredCustomModifiers = CreateMethod<Type, Type[]>("GetRequiredCustomModifiers");
			public static readonly Func<Type, Type[]>? getOptionalCustomModifiers = CreateMethod<Type, Type[]>("GetOptionalCustomModifiers");
			public static readonly Func<ParameterInfo, Type>? getModifiedParameterType = CreateMethod<ParameterInfo, Type>("GetModifiedParameterType");
			public static readonly Func<FieldInfo, Type>? getModifiedFieldType = CreateMethod<FieldInfo, Type>("GetModifiedFieldType");
			public static readonly Func<PropertyInfo, Type>? getModifiedPropertyType = CreateMethod<PropertyInfo, Type>("GetModifiedPropertyType");

			static Func<T, TResult>? CreateGetter<T, TResult>(string name) {
				var prop = typeof(T).GetProperty(name, BindingFlags.Public | BindingFlags.Instance, null, typeof(TResult), Type.EmptyTypes, null);
				return CreateDelegate<T, TResult>(prop?.GetGetMethod());
			}

			static Func<T, TResult>? CreateMethod<T, TResult>(string name) =>
				CreateDelegate<T, TResult>(typeof(T).GetMethod(name, BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null));

			static Func<T, TResult>? CreateDelegate<T, TResult>(MethodInfo? method) {
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
		public static bool IsGenericButNotGenericTypeDefinition([NotNullWhen(true)] this Type? type) =>
			type is not null && !type.IsGenericTypeDefinition && type.IsGenericType;

		/// <summary>
		/// Returns <c>true</c> if <paramref name="mb"/> is a generic method, but
		/// not a generic method definition, i.e., a MethodSpec.
		/// </summary>
		/// <param name="mb">The method</param>
		public static bool IsGenericButNotGenericMethodDefinition([NotNullWhen(true)] this MethodBase? mb) =>
			mb is not null && !mb.IsGenericMethodDefinition && mb.IsGenericMethod;

		/// <summary>
		/// Checks whether a parameter/prop/event type should be treated as if it is really a
		/// generic instance type and not a generic type definition. In the .NET metadata (method
		/// sig), the parameter is a generic instance type, but the CLR treats it as if it's just
		/// a generic type def. Reflection uses the generic type def for a generic instance type instantiated
		/// with its own generic parameters, eg. a method similar to: <c>MyType&lt;!0&gt; MyType::SomeMethod()</c>,
		/// or a method in a generic instance type that is instantiated with the type's generic parameters, eg.
		/// <c>ConfiguredTaskAwaitable&lt;TResult&gt;(Task&lt;TResult&gt;)</c> where <c>TResult</c> is <c>Task&lt;TResult&gt;</c>'s
		/// generic parameter. A generic type def can't be used in a signature so it's a generic instance type.
		/// </summary>
		/// <param name="declaringType">Declaring type of field/method/event/property</param>
		/// <param name="t">Field/parameter/property/event type</param>
		internal static bool MustTreatTypeAsGenericInstType([NotNullWhen(true)] this Type? declaringType, [NotNullWhen(true)] Type? t) {
			if (declaringType is null || t is null || !t.IsGenericTypeDefinition)
				return false;
			if (declaringType.IsGenericTypeDefinition)
				return t == declaringType;
			if (declaringType.IsGenericType) {
				// t's generic params can only be in the sig if the declaring type is instantiated with them
				foreach (var genArg in declaringType.GetGenericArguments()) {
					if (genArg.IsGenericParameter && genArg.DeclaringMethod is null && genArg.DeclaringType == t)
						return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Checks whether <paramref name="type"/> is a type definition and not a type spec
		/// (eg. pointer, generic type instantiation or generic parameter)
		/// </summary>
		/// <param name="type">this</param>
		public static bool IsTypeDef([NotNullWhen(true)] this Type? type) =>
			type is not null && !type.HasElementType && !type.IsGenericParameter && (!type.IsGenericType || type.IsGenericTypeDefinition) && !type.IsNativeValueType2();

		[return: NotNullIfNotNull(nameof(name))]
		internal static string? Unescape(string? name) {
			if (String2.IsNullOrEmpty(name) || name.IndexOf('\\') < 0)
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
