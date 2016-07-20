// dnlib: See LICENSE.txt for more info

﻿using System.Collections.Generic;
using System.Text;
using dnlib.Threading;

namespace dnlib.DotNet {
	/// <summary>
	/// Helps <see cref="FullNameCreator"/> create a name
	/// </summary>
	public interface IFullNameCreatorHelper {
		/// <summary>
		/// Checks whether the assembly name should be included when printing
		/// the full type name. The assembly name isn't required in custom attributes
		/// when the type already exists in the same module as the CA, or if the type
		/// exists in mscorlib.
		/// </summary>
		/// <param name="type">The type (<c>TypeDef</c>, <c>TypeRef</c> or <c>ExportedType</c>)
		/// or <c>null</c></param>
		/// <returns><c>true</c> if the assembly name must be included, <c>false</c> otherwise</returns>
		bool MustUseAssemblyName(IType type);
	}

	/// <summary>
	/// Creates type names, method names, etc.
	/// </summary>
	public struct FullNameCreator {
		const string RECURSION_ERROR_RESULT_STRING = "<<<INFRECURSION>>>";
		const string NULLVALUE = "<<<NULL>>>";
		readonly StringBuilder sb;
		readonly bool isReflection;
		readonly IFullNameCreatorHelper helper;
		GenericArguments genericArguments;
		RecursionCounter recursionCounter;

		/// <summary>
		/// Checks whether the assembly name should be included when printing the full name.
		/// See <see cref="IFullNameCreatorHelper.MustUseAssemblyName"/> for more info.
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="type">The type (<c>TypeDef</c>, <c>TypeRef</c> or <c>ExportedType</c>)
		/// or <c>null</c></param>
		/// <returns><c>true</c> if the assembly name must be included, <c>false</c> otherwise</returns>
		public static bool MustUseAssemblyName(ModuleDef module, IType type) {
			var td = type as TypeDef;
			if (td != null)
				return td.Module != module;

			var tr = type as TypeRef;
			if (tr == null)
				return true;
			if (tr.ResolutionScope == AssemblyRef.CurrentAssembly)
				return false;
			if (!tr.DefinitionAssembly.IsCorLib())
				return true;
			// If it's present in this module, but it's a corlib type, then we will need the
			// assembly name.
			return module.Find(tr) != null;
		}

		/// <summary>
		/// Returns the full name of a <see cref="IType"/>
		/// </summary>
		/// <param name="type">The <c>TypeRef</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="helper">Helps print the name</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The full name</returns>
		public static string FullName(IType type, bool isReflection, IFullNameCreatorHelper helper, StringBuilder sb) {
			return FullNameSB(type, isReflection, helper, sb).ToString();
		}

		/// <summary>
		/// Returns the full name of a <see cref="IType"/>
		/// </summary>
		/// <param name="type">The <c>TypeRef</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="helper">Helps print the name</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The full name</returns>
		public static StringBuilder FullNameSB(IType type, bool isReflection, IFullNameCreatorHelper helper, StringBuilder sb) {
			var td = type as TypeDef;
			if (td != null)
				return FullNameSB(td, isReflection, helper, sb);
			var tr = type as TypeRef;
			if (tr != null)
				return FullNameSB(tr, isReflection, helper, sb);
			var ts = type as TypeSpec;
			if (ts != null)
				return FullNameSB(ts, isReflection, helper, sb);
			var sig = type as TypeSig;
			if (sig != null)
				return FullNameSB(sig, isReflection, helper, null, null, sb);
			var et = type as ExportedType;
			if (et != null)
				return FullNameSB(et, isReflection, helper, sb);
			return sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the name of a <see cref="IType"/>
		/// </summary>
		/// <param name="type">The <c>TypeRef</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The full name</returns>
		public static string Name(IType type, bool isReflection, StringBuilder sb) {
			return NameSB(type, isReflection, sb).ToString();
		}

		/// <summary>
		/// Returns the name of a <see cref="IType"/>
		/// </summary>
		/// <param name="type">The <c>TypeRef</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The full name</returns>
		public static StringBuilder NameSB(IType type, bool isReflection, StringBuilder sb) {
			var td = type as TypeDef;
			if (td != null)
				return NameSB(td, isReflection, sb);
			var tr = type as TypeRef;
			if (tr != null)
				return NameSB(tr, isReflection, sb);
			var ts = type as TypeSpec;
			if (ts != null)
				return NameSB(ts, isReflection, sb);
			var sig = type as TypeSig;
			if (sig != null)
				return NameSB(sig, false, sb);
			var et = type as ExportedType;
			if (et != null)
				return NameSB(et, isReflection, sb);
			return sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the namespace of a <see cref="IType"/>
		/// </summary>
		/// <param name="type">The <c>TypeRef</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The full name</returns>
		public static string Namespace(IType type, bool isReflection, StringBuilder sb) {
			return NamespaceSB(type, isReflection, sb).ToString();
		}

		/// <summary>
		/// Returns the namespace of a <see cref="IType"/>
		/// </summary>
		/// <param name="type">The <c>TypeRef</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The full name</returns>
		public static StringBuilder NamespaceSB(IType type, bool isReflection, StringBuilder sb) {
			var td = type as TypeDef;
			if (td != null)
				return NamespaceSB(td, isReflection, sb);
			var tr = type as TypeRef;
			if (tr != null)
				return NamespaceSB(tr, isReflection, sb);
			var ts = type as TypeSpec;
			if (ts != null)
				return NamespaceSB(ts, isReflection, sb);
			var sig = type as TypeSig;
			if (sig != null)
				return NamespaceSB(sig, false, sb);
			var et = type as ExportedType;
			if (et != null)
				return NamespaceSB(et, isReflection, sb);
			return sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the assembly qualified full name of a <see cref="IType"/>
		/// </summary>
		/// <param name="type">The <c>IType</c></param>
		/// <param name="helper">Helps print the name</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The assembly qualified full name</returns>
		public static string AssemblyQualifiedName(IType type, IFullNameCreatorHelper helper = null, StringBuilder sb = null) {
			return AssemblyQualifiedNameSB(type, helper, sb).ToString();
		}

		/// <summary>
		/// Returns the assembly qualified full name of a <see cref="IType"/>
		/// </summary>
		/// <param name="type">The <c>IType</c></param>
		/// <param name="helper">Helps print the name</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The assembly qualified full name</returns>
		public static StringBuilder AssemblyQualifiedNameSB(IType type, IFullNameCreatorHelper helper, StringBuilder sb) {
			var td = type as TypeDef;
			if (td != null)
				return AssemblyQualifiedNameSB(td, helper, sb);

			var tr = type as TypeRef;
			if (tr != null)
				return AssemblyQualifiedNameSB(tr, helper, sb);

			var ts = type as TypeSpec;
			if (ts != null)
				return AssemblyQualifiedNameSB(ts, helper, sb);

			var sig = type as TypeSig;
			if (sig != null)
				return AssemblyQualifiedNameSB(sig, helper, sb);

			var et = type as ExportedType;
			if (et != null)
				return AssemblyQualifiedNameSB(et, helper, sb);

			return sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the full name of a property
		/// </summary>
		/// <param name="declaringType">Declaring type full name or <c>null</c> if none</param>
		/// <param name="name">Name of property</param>
		/// <param name="propertySig">Property signature</param>
		/// <param name="typeGenArgs">Type generic arguments or <c>null</c> if none</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>Property full name</returns>
		public static string PropertyFullName(string declaringType, UTF8String name, CallingConventionSig propertySig, IList<TypeSig> typeGenArgs = null, StringBuilder sb = null) {
			return PropertyFullNameSB(declaringType, name, propertySig, typeGenArgs, sb).ToString();
		}

		/// <summary>
		/// Returns the full name of a property
		/// </summary>
		/// <param name="declaringType">Declaring type full name or <c>null</c> if none</param>
		/// <param name="name">Name of property</param>
		/// <param name="propertySig">Property signature</param>
		/// <param name="typeGenArgs">Type generic arguments or <c>null</c> if none</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>Property full name</returns>
		public static StringBuilder PropertyFullNameSB(string declaringType, UTF8String name, CallingConventionSig propertySig, IList<TypeSig> typeGenArgs, StringBuilder sb) {
			var fnc = new FullNameCreator(false, null, sb);
			if (typeGenArgs != null) {
				fnc.genericArguments = new GenericArguments();
				fnc.genericArguments.PushTypeArgs(typeGenArgs);
			}

			fnc.CreatePropertyFullName(declaringType, name, propertySig);
			return fnc.sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the full name of a property
		/// </summary>
		/// <param name="declaringType">Declaring type full name or <c>null</c> if none</param>
		/// <param name="name">Name of property</param>
		/// <param name="typeDefOrRef">Event type</param>
		/// <param name="typeGenArgs">Type generic arguments or <c>null</c> if none</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>Property full name</returns>
		public static string EventFullName(string declaringType, UTF8String name, ITypeDefOrRef typeDefOrRef, IList<TypeSig> typeGenArgs = null, StringBuilder sb = null) {
			return EventFullNameSB(declaringType, name, typeDefOrRef, typeGenArgs, sb).ToString();
		}

		/// <summary>
		/// Returns the full name of a property
		/// </summary>
		/// <param name="declaringType">Declaring type full name or <c>null</c> if none</param>
		/// <param name="name">Name of property</param>
		/// <param name="typeDefOrRef">Event type</param>
		/// <param name="typeGenArgs">Type generic arguments or <c>null</c> if none</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>Property full name</returns>
		public static StringBuilder EventFullNameSB(string declaringType, UTF8String name, ITypeDefOrRef typeDefOrRef, IList<TypeSig> typeGenArgs, StringBuilder sb) {
			var fnc = new FullNameCreator(false, null, sb);
			if (typeGenArgs != null) {
				fnc.genericArguments = new GenericArguments();
				fnc.genericArguments.PushTypeArgs(typeGenArgs);
			}

			fnc.CreateEventFullName(declaringType, name, typeDefOrRef);
			return fnc.sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the full name of a field
		/// </summary>
		/// <param name="declaringType">Declaring type full name or <c>null</c> if none</param>
		/// <param name="name">Name of field</param>
		/// <param name="fieldSig">Field signature</param>
		/// <param name="typeGenArgs">Type generic arguments or <c>null</c> if none</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>Field full name</returns>
		public static string FieldFullName(string declaringType, string name, FieldSig fieldSig, IList<TypeSig> typeGenArgs = null, StringBuilder sb = null) {
			return FieldFullNameSB(declaringType, name, fieldSig, typeGenArgs, sb).ToString();
		}

		/// <summary>
		/// Returns the full name of a field
		/// </summary>
		/// <param name="declaringType">Declaring type full name or <c>null</c> if none</param>
		/// <param name="name">Name of field</param>
		/// <param name="fieldSig">Field signature</param>
		/// <param name="typeGenArgs">Type generic arguments or <c>null</c> if none</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>Field full name</returns>
		public static StringBuilder FieldFullNameSB(string declaringType, string name, FieldSig fieldSig, IList<TypeSig> typeGenArgs, StringBuilder sb) {
			var fnc = new FullNameCreator(false, null, sb);
			if (typeGenArgs != null) {
				fnc.genericArguments = new GenericArguments();
				fnc.genericArguments.PushTypeArgs(typeGenArgs);
			}

			fnc.CreateFieldFullName(declaringType, name, fieldSig);
			return fnc.sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the full name of a method
		/// </summary>
		/// <param name="declaringType">Declaring type full name or <c>null</c> if none</param>
		/// <param name="name">Name of method or <c>null</c> if none</param>
		/// <param name="methodSig">Method signature</param>
		/// <param name="typeGenArgs">Type generic arguments or <c>null</c> if none</param>
		/// <param name="methodGenArgs">Method generic arguments or <c>null</c> if none</param>
		/// <param name="gppMethod">Generic parameter owner method or <c>null</c></param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>Method full name</returns>
		public static string MethodFullName(string declaringType, string name, MethodSig methodSig, IList<TypeSig> typeGenArgs = null, IList<TypeSig> methodGenArgs = null, MethodDef gppMethod = null, StringBuilder sb = null) {
			return MethodFullNameSB(declaringType, name, methodSig, typeGenArgs, methodGenArgs, gppMethod, sb).ToString();
		}

		/// <summary>
		/// Returns the full name of a method
		/// </summary>
		/// <param name="declaringType">Declaring type full name or <c>null</c> if none</param>
		/// <param name="name">Name of method or <c>null</c> if none</param>
		/// <param name="methodSig">Method signature</param>
		/// <param name="typeGenArgs">Type generic arguments or <c>null</c> if none</param>
		/// <param name="methodGenArgs">Method generic arguments or <c>null</c> if none</param>
		/// <param name="gppMethod">Generic parameter owner method or <c>null</c></param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>Method full name</returns>
		public static StringBuilder MethodFullNameSB(string declaringType, string name, MethodSig methodSig, IList<TypeSig> typeGenArgs, IList<TypeSig> methodGenArgs, MethodDef gppMethod, StringBuilder sb) {
			var fnc = new FullNameCreator(false, null, sb);
			if (typeGenArgs != null || methodGenArgs != null)
				fnc.genericArguments = new GenericArguments();
			if (typeGenArgs != null)
				fnc.genericArguments.PushTypeArgs(typeGenArgs);
			if (methodGenArgs != null)
				fnc.genericArguments.PushMethodArgs(methodGenArgs);
			fnc.CreateMethodFullName(declaringType, name, methodSig, gppMethod);
			return fnc.sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the full name of a property sig
		/// </summary>
		/// <param name="sig">Property sig</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>Property sig full name</returns>
		public static string MethodBaseSigFullName(MethodBaseSig sig, StringBuilder sb = null) {
			return MethodBaseSigFullNameSB(sig, sb).ToString();
		}

		/// <summary>
		/// Returns the full name of a property sig
		/// </summary>
		/// <param name="sig">Property sig</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>Property sig full name</returns>
		public static StringBuilder MethodBaseSigFullNameSB(MethodBaseSig sig, StringBuilder sb) {
			var fnc = new FullNameCreator(false, null, sb);
			fnc.CreateMethodFullName(null, null, sig, null);
			return fnc.sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the full name of a sig
		/// </summary>
		/// <param name="declType">Declaring type or null</param>
		/// <param name="name">Name or null</param>
		/// <param name="sig">Method sig</param>
		/// <param name="gppMethod">Owner method or null</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>Sig full name</returns>
		public static string MethodBaseSigFullName(string declType, string name, MethodBaseSig sig, MethodDef gppMethod, StringBuilder sb = null) {
			return MethodBaseSigFullNameSB(declType, name, sig, gppMethod, sb).ToString();
		}

		/// <summary>
		/// Returns the full name of a sig
		/// </summary>
		/// <param name="declType">Declaring type or null</param>
		/// <param name="name">Name or null</param>
		/// <param name="sig">Method sig</param>
		/// <param name="gppMethod">Owner method or null</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>Sig full name</returns>
		public static StringBuilder MethodBaseSigFullNameSB(string declType, string name, MethodBaseSig sig, MethodDef gppMethod, StringBuilder sb) {
			var fnc = new FullNameCreator(false, null, sb);
			fnc.CreateMethodFullName(declType, name, sig, gppMethod);
			return fnc.sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the namespace of a <see cref="TypeRef"/>
		/// </summary>
		/// <param name="typeRef">The <c>TypeRef</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The namespace</returns>
		public static string Namespace(TypeRef typeRef, bool isReflection, StringBuilder sb = null) {
			return NamespaceSB(typeRef, isReflection, sb).ToString();
		}

		/// <summary>
		/// Returns the namespace of a <see cref="TypeRef"/>
		/// </summary>
		/// <param name="typeRef">The <c>TypeRef</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The namespace</returns>
		public static StringBuilder NamespaceSB(TypeRef typeRef, bool isReflection, StringBuilder sb) {
			var fnc = new FullNameCreator(isReflection, null, sb);
			fnc.CreateNamespace(typeRef);
			return fnc.sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the name of a <see cref="TypeRef"/>
		/// </summary>
		/// <param name="typeRef">The <c>TypeRef</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The name</returns>
		public static string Name(TypeRef typeRef, bool isReflection, StringBuilder sb = null) {
			return NameSB(typeRef, isReflection, sb).ToString();
		}

		/// <summary>
		/// Returns the name of a <see cref="TypeRef"/>
		/// </summary>
		/// <param name="typeRef">The <c>TypeRef</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The name</returns>
		public static StringBuilder NameSB(TypeRef typeRef, bool isReflection, StringBuilder sb) {
			var fnc = new FullNameCreator(isReflection, null, sb);
			fnc.CreateName(typeRef);
			return fnc.sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the full name of a <see cref="TypeRef"/>
		/// </summary>
		/// <param name="typeRef">The <c>TypeRef</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="helper">Helps print the name</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The full name</returns>
		public static string FullName(TypeRef typeRef, bool isReflection, IFullNameCreatorHelper helper = null, StringBuilder sb = null) {
			return FullNameSB(typeRef, isReflection, helper, sb).ToString();
		}

		/// <summary>
		/// Returns the full name of a <see cref="TypeRef"/>
		/// </summary>
		/// <param name="typeRef">The <c>TypeRef</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="helper">Helps print the name</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The full name</returns>
		public static StringBuilder FullNameSB(TypeRef typeRef, bool isReflection, IFullNameCreatorHelper helper, StringBuilder sb) {
			var fnc = new FullNameCreator(isReflection, helper, sb);
			fnc.CreateFullName(typeRef);
			return fnc.sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the assembly qualified full name of a <see cref="TypeRef"/>
		/// </summary>
		/// <param name="typeRef">The <c>TypeRef</c></param>
		/// <param name="helper">Helps print the name</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The assembly qualified full name</returns>
		public static string AssemblyQualifiedName(TypeRef typeRef, IFullNameCreatorHelper helper = null, StringBuilder sb = null) {
			return AssemblyQualifiedNameSB(typeRef, helper, sb).ToString();
		}

		/// <summary>
		/// Returns the assembly qualified full name of a <see cref="TypeRef"/>
		/// </summary>
		/// <param name="typeRef">The <c>TypeRef</c></param>
		/// <param name="helper">Helps print the name</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The assembly qualified full name</returns>
		public static StringBuilder AssemblyQualifiedNameSB(TypeRef typeRef, IFullNameCreatorHelper helper, StringBuilder sb) {
			var fnc = new FullNameCreator(true, helper, sb);
			fnc.CreateAssemblyQualifiedName(typeRef);
			return fnc.sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the assembly where this type is defined
		/// </summary>
		/// <param name="typeRef">The <c>TypeRef</c></param>
		/// <returns>A <see cref="IAssembly"/> or <c>null</c> if none found</returns>
		public static IAssembly DefinitionAssembly(TypeRef typeRef) {
			return new FullNameCreator().GetDefinitionAssembly(typeRef);
		}

		/// <summary>
		/// Gets the scope
		/// </summary>
		/// <param name="typeRef">The <c>TypeRef</c></param>
		/// <returns>The <see cref="IScope"/> or <c>null</c> if none found</returns>
		public static IScope Scope(TypeRef typeRef) {
			return new FullNameCreator().GetScope(typeRef);
		}

		/// <summary>
		/// Returns the owner module. The type was created from metadata in this module.
		/// </summary>
		/// <param name="typeRef">The <c>TypeRef</c></param>
		/// <returns>A <see cref="ModuleDef"/> or <c>null</c> if none found</returns>
		public static ModuleDef OwnerModule(TypeRef typeRef) {
			return new FullNameCreator().GetOwnerModule(typeRef);
		}

		/// <summary>
		/// Returns the namespace of a <see cref="TypeDef"/>
		/// </summary>
		/// <param name="typeDef">The <c>TypeDef</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The namespace</returns>
		public static string Namespace(TypeDef typeDef, bool isReflection, StringBuilder sb = null) {
			return NamespaceSB(typeDef, isReflection, sb).ToString();
		}

		/// <summary>
		/// Returns the namespace of a <see cref="TypeDef"/>
		/// </summary>
		/// <param name="typeDef">The <c>TypeDef</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The namespace</returns>
		public static StringBuilder NamespaceSB(TypeDef typeDef, bool isReflection, StringBuilder sb) {
			var fnc = new FullNameCreator(isReflection, null, sb);
			fnc.CreateNamespace(typeDef);
			return fnc.sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the name of a <see cref="TypeDef"/>
		/// </summary>
		/// <param name="typeDef">The <c>TypeDef</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The name</returns>
		public static string Name(TypeDef typeDef, bool isReflection, StringBuilder sb = null) {
			return NameSB(typeDef, isReflection, sb).ToString();
		}

		/// <summary>
		/// Returns the name of a <see cref="TypeDef"/>
		/// </summary>
		/// <param name="typeDef">The <c>TypeDef</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The name</returns>
		public static StringBuilder NameSB(TypeDef typeDef, bool isReflection, StringBuilder sb) {
			var fnc = new FullNameCreator(isReflection, null, sb);
			fnc.CreateName(typeDef);
			return fnc.sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the full name of a <see cref="TypeDef"/>
		/// </summary>
		/// <param name="typeDef">The <c>TypeDef</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="helper">Helps print the name</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The full name</returns>
		public static string FullName(TypeDef typeDef, bool isReflection, IFullNameCreatorHelper helper = null, StringBuilder sb = null) {
			return FullNameSB(typeDef, isReflection, helper, sb).ToString();
		}

		/// <summary>
		/// Returns the full name of a <see cref="TypeDef"/>
		/// </summary>
		/// <param name="typeDef">The <c>TypeDef</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="helper">Helps print the name</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The full name</returns>
		public static StringBuilder FullNameSB(TypeDef typeDef, bool isReflection, IFullNameCreatorHelper helper, StringBuilder sb) {
			var fnc = new FullNameCreator(isReflection, helper, sb);
			fnc.CreateFullName(typeDef);
			return fnc.sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the assembly qualified full name of a <see cref="TypeDef"/>
		/// </summary>
		/// <param name="typeDef">The <c>TypeDef</c></param>
		/// <param name="helper">Helps print the name</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The assembly qualified full name</returns>
		public static string AssemblyQualifiedName(TypeDef typeDef, IFullNameCreatorHelper helper = null, StringBuilder sb = null) {
			return AssemblyQualifiedNameSB(typeDef, helper, sb).ToString();
		}

		/// <summary>
		/// Returns the assembly qualified full name of a <see cref="TypeDef"/>
		/// </summary>
		/// <param name="typeDef">The <c>TypeDef</c></param>
		/// <param name="helper">Helps print the name</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The assembly qualified full name</returns>
		public static StringBuilder AssemblyQualifiedNameSB(TypeDef typeDef, IFullNameCreatorHelper helper, StringBuilder sb) {
			var fnc = new FullNameCreator(true, helper, sb);
			fnc.CreateAssemblyQualifiedName(typeDef);
			return fnc.sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the assembly where this type is defined
		/// </summary>
		/// <param name="typeDef">The <c>TypeDef</c></param>
		/// <returns>A <see cref="IAssembly"/> or <c>null</c> if none found</returns>
		public static IAssembly DefinitionAssembly(TypeDef typeDef) {
			return new FullNameCreator().GetDefinitionAssembly(typeDef);
		}

		/// <summary>
		/// Returns the owner module. The type was created from metadata in this module.
		/// </summary>
		/// <param name="typeDef">The <c>TypeDef</c></param>
		/// <returns>A <see cref="ModuleDef"/> or <c>null</c> if none found</returns>
		public static ModuleDef OwnerModule(TypeDef typeDef) {
			return new FullNameCreator().GetOwnerModule(typeDef);
		}

		/// <summary>
		/// Returns the namespace of a <see cref="TypeSpec"/>
		/// </summary>
		/// <param name="typeSpec">The <c>TypeSpec</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The namespace</returns>
		public static string Namespace(TypeSpec typeSpec, bool isReflection, StringBuilder sb = null) {
			return NamespaceSB(typeSpec, isReflection, sb).ToString();
		}

		/// <summary>
		/// Returns the namespace of a <see cref="TypeSpec"/>
		/// </summary>
		/// <param name="typeSpec">The <c>TypeSpec</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The namespace</returns>
		public static StringBuilder NamespaceSB(TypeSpec typeSpec, bool isReflection, StringBuilder sb) {
			var fnc = new FullNameCreator(isReflection, null, sb);
			fnc.CreateNamespace(typeSpec);
			return fnc.sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the name of a <see cref="TypeSpec"/>
		/// </summary>
		/// <param name="typeSpec">The <c>TypeSpec</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The name</returns>
		public static string Name(TypeSpec typeSpec, bool isReflection, StringBuilder sb = null) {
			return NameSB(typeSpec, isReflection, sb).ToString();
		}

		/// <summary>
		/// Returns the name of a <see cref="TypeSpec"/>
		/// </summary>
		/// <param name="typeSpec">The <c>TypeSpec</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The name</returns>
		public static StringBuilder NameSB(TypeSpec typeSpec, bool isReflection, StringBuilder sb) {
			var fnc = new FullNameCreator(isReflection, null, sb);
			fnc.CreateName(typeSpec);
			return fnc.sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the full name of a <see cref="TypeSpec"/>
		/// </summary>
		/// <param name="typeSpec">The <c>TypeSpec</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="helper">Helps print the name</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The full name</returns>
		public static string FullName(TypeSpec typeSpec, bool isReflection, IFullNameCreatorHelper helper = null, StringBuilder sb = null) {
			return FullNameSB(typeSpec, isReflection, helper, sb).ToString();
		}

		/// <summary>
		/// Returns the full name of a <see cref="TypeSpec"/>
		/// </summary>
		/// <param name="typeSpec">The <c>TypeSpec</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="helper">Helps print the name</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The full name</returns>
		public static StringBuilder FullNameSB(TypeSpec typeSpec, bool isReflection, IFullNameCreatorHelper helper, StringBuilder sb) {
			var fnc = new FullNameCreator(isReflection, helper, sb);
			fnc.CreateFullName(typeSpec);
			return fnc.sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the assembly qualified full name of a <see cref="TypeSpec"/>
		/// </summary>
		/// <param name="typeSpec">The <c>TypeSpec</c></param>
		/// <param name="helper">Helps print the name</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The assembly qualified full name</returns>
		public static string AssemblyQualifiedName(TypeSpec typeSpec, IFullNameCreatorHelper helper = null, StringBuilder sb = null) {
			return AssemblyQualifiedNameSB(typeSpec, helper, sb).ToString();
		}

		/// <summary>
		/// Returns the assembly qualified full name of a <see cref="TypeSpec"/>
		/// </summary>
		/// <param name="typeSpec">The <c>TypeSpec</c></param>
		/// <param name="helper">Helps print the name</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The assembly qualified full name</returns>
		public static StringBuilder AssemblyQualifiedNameSB(TypeSpec typeSpec, IFullNameCreatorHelper helper, StringBuilder sb) {
			var fnc = new FullNameCreator(true, helper, sb);
			fnc.CreateAssemblyQualifiedName(typeSpec);
			return fnc.sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the assembly where this type is defined
		/// </summary>
		/// <param name="typeSpec">The <c>TypeSpec</c></param>
		/// <returns>A <see cref="IAssembly"/> or <c>null</c> if none found</returns>
		public static IAssembly DefinitionAssembly(TypeSpec typeSpec) {
			return new FullNameCreator().GetDefinitionAssembly(typeSpec);
		}

		/// <summary>
		/// Gets the scope type
		/// </summary>
		/// <param name="typeSpec">The <c>TypeSpec</c></param>
		/// <returns>The scope type or <c>null</c> if none found</returns>
		public static ITypeDefOrRef ScopeType(TypeSpec typeSpec) {
			return new FullNameCreator().GetScopeType(typeSpec);
		}

		/// <summary>
		/// Gets the scope
		/// </summary>
		/// <param name="typeSpec">The <c>TypeSpec</c></param>
		/// <returns>The <see cref="IScope"/> or <c>null</c> if none found</returns>
		public static IScope Scope(TypeSpec typeSpec) {
			return new FullNameCreator().GetScope(typeSpec);
		}

		/// <summary>
		/// Returns the owner module. The type was created from metadata in this module.
		/// </summary>
		/// <param name="typeSpec">The <c>TypeSpec</c></param>
		/// <returns>A <see cref="ModuleDef"/> or <c>null</c> if none found</returns>
		public static ModuleDef OwnerModule(TypeSpec typeSpec) {
			return new FullNameCreator().GetOwnerModule(typeSpec);
		}

		/// <summary>
		/// Returns the namespace of a <see cref="TypeSig"/>
		/// </summary>
		/// <param name="typeSig">The type sig</param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The namespace</returns>
		public static string Namespace(TypeSig typeSig, bool isReflection, StringBuilder sb = null) {
			return NamespaceSB(typeSig, isReflection, sb).ToString();
		}

		/// <summary>
		/// Returns the namespace of a <see cref="TypeSig"/>
		/// </summary>
		/// <param name="typeSig">The type sig</param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The namespace</returns>
		public static StringBuilder NamespaceSB(TypeSig typeSig, bool isReflection, StringBuilder sb) {
			var fnc = new FullNameCreator(isReflection, null, sb);
			fnc.CreateNamespace(typeSig);
			return fnc.sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the name of a <see cref="TypeSig"/>
		/// </summary>
		/// <param name="typeSig">The type sig</param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The name</returns>
		public static string Name(TypeSig typeSig, bool isReflection, StringBuilder sb = null) {
			return NameSB(typeSig, isReflection, sb).ToString();
		}

		/// <summary>
		/// Returns the name of a <see cref="TypeSig"/>
		/// </summary>
		/// <param name="typeSig">The type sig</param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The name</returns>
		public static StringBuilder NameSB(TypeSig typeSig, bool isReflection, StringBuilder sb) {
			var fnc = new FullNameCreator(isReflection, null, sb);
			fnc.CreateName(typeSig);
			return fnc.sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the full name of a <see cref="TypeSig"/>
		/// </summary>
		/// <param name="typeSig">The type sig</param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="helper">Helps print the name</param>
		/// <param name="typeGenArgs">Type generic args or <c>null</c> if none</param>
		/// <param name="methodGenArgs">Method generic args or <c>null</c> if none</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The full name</returns>
		public static string FullName(TypeSig typeSig, bool isReflection, IFullNameCreatorHelper helper = null, IList<TypeSig> typeGenArgs = null, IList<TypeSig> methodGenArgs = null, StringBuilder sb = null) {
			return FullNameSB(typeSig, isReflection, helper, typeGenArgs, methodGenArgs, sb).ToString();
		}

		/// <summary>
		/// Returns the full name of a <see cref="TypeSig"/>
		/// </summary>
		/// <param name="typeSig">The type sig</param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="helper">Helps print the name</param>
		/// <param name="typeGenArgs">Type generic args or <c>null</c> if none</param>
		/// <param name="methodGenArgs">Method generic args or <c>null</c> if none</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The full name</returns>
		public static StringBuilder FullNameSB(TypeSig typeSig, bool isReflection, IFullNameCreatorHelper helper, IList<TypeSig> typeGenArgs, IList<TypeSig> methodGenArgs, StringBuilder sb) {
			var fnc = new FullNameCreator(isReflection, helper, sb);
			if (typeGenArgs != null || methodGenArgs != null)
				fnc.genericArguments = new GenericArguments();
			if (typeGenArgs != null)
				fnc.genericArguments.PushTypeArgs(typeGenArgs);
			if (methodGenArgs != null)
				fnc.genericArguments.PushMethodArgs(methodGenArgs);
			fnc.CreateFullName(typeSig);
			return fnc.sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the assembly qualified full name of a <see cref="TypeSig"/>
		/// </summary>
		/// <param name="typeSig">The <c>TypeSig</c></param>
		/// <param name="helper">Helps print the name</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The assembly qualified full name</returns>
		public static string AssemblyQualifiedName(TypeSig typeSig, IFullNameCreatorHelper helper = null, StringBuilder sb = null) {
			return AssemblyQualifiedNameSB(typeSig, helper, sb).ToString();
		}

		/// <summary>
		/// Returns the assembly qualified full name of a <see cref="TypeSig"/>
		/// </summary>
		/// <param name="typeSig">The <c>TypeSig</c></param>
		/// <param name="helper">Helps print the name</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The assembly qualified full name</returns>
		public static StringBuilder AssemblyQualifiedNameSB(TypeSig typeSig, IFullNameCreatorHelper helper, StringBuilder sb) {
			var fnc = new FullNameCreator(true, helper, sb);
			fnc.CreateAssemblyQualifiedName(typeSig);
			return fnc.sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the assembly where this type is defined
		/// </summary>
		/// <param name="typeSig">The <c>TypeSig</c></param>
		/// <returns>A <see cref="IAssembly"/> or <c>null</c> if none found</returns>
		public static IAssembly DefinitionAssembly(TypeSig typeSig) {
			return new FullNameCreator().GetDefinitionAssembly(typeSig);
		}

		/// <summary>
		/// Gets the scope
		/// </summary>
		/// <param name="typeSig">The <c>TypeSig</c></param>
		/// <returns>The <see cref="IScope"/> or <c>null</c> if none found</returns>
		public static IScope Scope(TypeSig typeSig) {
			return new FullNameCreator().GetScope(typeSig);
		}

		/// <summary>
		/// Gets the scope type
		/// </summary>
		/// <param name="typeSig">The <c>TypeSig</c></param>
		/// <returns>The scope type or <c>null</c> if none found</returns>
		public static ITypeDefOrRef ScopeType(TypeSig typeSig) {
			return new FullNameCreator().GetScopeType(typeSig);
		}

		/// <summary>
		/// Returns the owner module. The type was created from metadata in this module.
		/// </summary>
		/// <param name="typeSig">The <c>TypeSig</c></param>
		/// <returns>A <see cref="ModuleDef"/> or <c>null</c> if none found</returns>
		public static ModuleDef OwnerModule(TypeSig typeSig) {
			return new FullNameCreator().GetOwnerModule(typeSig);
		}

		/// <summary>
		/// Returns the namespace of a <see cref="ExportedType"/>
		/// </summary>
		/// <param name="exportedType">The <c>ExportedType</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The namespace</returns>
		public static string Namespace(ExportedType exportedType, bool isReflection, StringBuilder sb = null) {
			return NamespaceSB(exportedType, isReflection, sb).ToString();
		}

		/// <summary>
		/// Returns the namespace of a <see cref="ExportedType"/>
		/// </summary>
		/// <param name="exportedType">The <c>ExportedType</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The namespace</returns>
		public static StringBuilder NamespaceSB(ExportedType exportedType, bool isReflection, StringBuilder sb) {
			var fnc = new FullNameCreator(isReflection, null, sb);
			fnc.CreateNamespace(exportedType);
			return fnc.sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the name of a <see cref="ExportedType"/>
		/// </summary>
		/// <param name="exportedType">The <c>ExportedType</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The name</returns>
		public static string Name(ExportedType exportedType, bool isReflection, StringBuilder sb = null) {
			return NameSB(exportedType, isReflection, sb).ToString();
		}

		/// <summary>
		/// Returns the name of a <see cref="ExportedType"/>
		/// </summary>
		/// <param name="exportedType">The <c>ExportedType</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The name</returns>
		public static StringBuilder NameSB(ExportedType exportedType, bool isReflection, StringBuilder sb) {
			var fnc = new FullNameCreator(isReflection, null, sb);
			fnc.CreateName(exportedType);
			return fnc.sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the full name of a <see cref="ExportedType"/>
		/// </summary>
		/// <param name="exportedType">The <c>ExportedType</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="helper">Helps print the name</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The full name</returns>
		public static string FullName(ExportedType exportedType, bool isReflection, IFullNameCreatorHelper helper = null, StringBuilder sb = null) {
			return FullNameSB(exportedType, isReflection, helper, sb).ToString();
		}

		/// <summary>
		/// Returns the full name of a <see cref="ExportedType"/>
		/// </summary>
		/// <param name="exportedType">The <c>ExportedType</c></param>
		/// <param name="isReflection">Set if output should be compatible with reflection</param>
		/// <param name="helper">Helps print the name</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The full name</returns>
		public static StringBuilder FullNameSB(ExportedType exportedType, bool isReflection, IFullNameCreatorHelper helper, StringBuilder sb) {
			var fnc = new FullNameCreator(isReflection, helper, sb);
			fnc.CreateFullName(exportedType);
			return fnc.sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the assembly qualified full name of a <see cref="ExportedType"/>
		/// </summary>
		/// <param name="exportedType">The <c>ExportedType</c></param>
		/// <param name="helper">Helps print the name</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The assembly qualified full name</returns>
		public static string AssemblyQualifiedName(ExportedType exportedType, IFullNameCreatorHelper helper = null, StringBuilder sb = null) {
			return AssemblyQualifiedNameSB(exportedType, helper, sb).ToString();
		}

		/// <summary>
		/// Returns the assembly qualified full name of a <see cref="ExportedType"/>
		/// </summary>
		/// <param name="exportedType">The <c>ExportedType</c></param>
		/// <param name="helper">Helps print the name</param>
		/// <param name="sb">String builder to use or null</param>
		/// <returns>The assembly qualified full name</returns>
		public static StringBuilder AssemblyQualifiedNameSB(ExportedType exportedType, IFullNameCreatorHelper helper, StringBuilder sb) {
			var fnc = new FullNameCreator(true, helper, sb);
			fnc.CreateAssemblyQualifiedName(exportedType);
			return fnc.sb ?? new StringBuilder();
		}

		/// <summary>
		/// Returns the assembly where this type is defined
		/// </summary>
		/// <param name="exportedType">The <c>ExportedType</c></param>
		/// <returns>A <see cref="IAssembly"/> or <c>null</c> if none found</returns>
		public static IAssembly DefinitionAssembly(ExportedType exportedType) {
			return new FullNameCreator().GetDefinitionAssembly(exportedType);
		}

		/// <summary>
		/// Gets the scope type
		/// </summary>
		/// <param name="exportedType">The <c>ExportedType</c></param>
		/// <returns>The scope type or <c>null</c> if none found</returns>
		public static ITypeDefOrRef ScopeType(ExportedType exportedType) {
			return new FullNameCreator().GetScopeType(exportedType);
		}

		/// <summary>
		/// Gets the scope
		/// </summary>
		/// <param name="exportedType">The <c>ExportedType</c></param>
		/// <returns>The <see cref="IScope"/> or <c>null</c> if none found</returns>
		public static IScope Scope(ExportedType exportedType) {
			return new FullNameCreator().GetScope(exportedType);
		}

		/// <summary>
		/// Returns the owner module. The type was created from metadata in this module.
		/// </summary>
		/// <param name="exportedType">The <c>ExportedType</c></param>
		/// <returns>A <see cref="ModuleDef"/> or <c>null</c> if none found</returns>
		public static ModuleDef OwnerModule(ExportedType exportedType) {
			return new FullNameCreator().GetOwnerModule(exportedType);
		}

		string Result {
			get { return sb == null ? null : sb.ToString(); }
		}

		FullNameCreator(bool isReflection, IFullNameCreatorHelper helper, StringBuilder sb) {
			this.sb = sb ?? new StringBuilder();
			this.isReflection = isReflection;
			this.helper = helper;
			this.genericArguments = null;
			this.recursionCounter = new RecursionCounter();
		}

		bool MustUseAssemblyName(IType type) {
			if (helper == null)
				return true;
			return helper.MustUseAssemblyName(GetDefinitionType(type));
		}

		IType GetDefinitionType(IType type) {
			if (!recursionCounter.Increment())
				return type;

			TypeSpec ts = type as TypeSpec;
			if (ts != null)
				type = ts.TypeSig;

			TypeSig sig = type as TypeSig;
			if (sig != null) {
				TypeDefOrRefSig tdr;
				GenericInstSig gis;
				if ((tdr = sig as TypeDefOrRefSig) != null)
					type = GetDefinitionType(tdr.TypeDefOrRef);
				else if ((gis = sig as GenericInstSig) != null)
					type = GetDefinitionType(gis.GenericType);
				else
					type = GetDefinitionType(sig.Next);
			}

			recursionCounter.Decrement();
			return type;
		}

		void CreateFullName(ITypeDefOrRef typeDefOrRef) {
			if (typeDefOrRef is TypeRef)
				CreateFullName((TypeRef)typeDefOrRef);
			else if (typeDefOrRef is TypeDef)
				CreateFullName((TypeDef)typeDefOrRef);
			else if (typeDefOrRef is TypeSpec)
				CreateFullName((TypeSpec)typeDefOrRef);
			else
				sb.Append(NULLVALUE);
		}

		void CreateNamespace(ITypeDefOrRef typeDefOrRef) {
			if (typeDefOrRef is TypeRef)
				CreateNamespace((TypeRef)typeDefOrRef);
			else if (typeDefOrRef is TypeDef)
				CreateNamespace((TypeDef)typeDefOrRef);
			else if (typeDefOrRef is TypeSpec)
				CreateNamespace((TypeSpec)typeDefOrRef);
			else
				sb.Append(NULLVALUE);
		}

		void CreateName(ITypeDefOrRef typeDefOrRef) {
			if (typeDefOrRef is TypeRef)
				CreateName((TypeRef)typeDefOrRef);
			else if (typeDefOrRef is TypeDef)
				CreateName((TypeDef)typeDefOrRef);
			else if (typeDefOrRef is TypeSpec)
				CreateName((TypeSpec)typeDefOrRef);
			else
				sb.Append(NULLVALUE);
		}

		void CreateAssemblyQualifiedName(ITypeDefOrRef typeDefOrRef) {
			if (typeDefOrRef is TypeRef)
				CreateAssemblyQualifiedName((TypeRef)typeDefOrRef);
			else if (typeDefOrRef is TypeDef)
				CreateAssemblyQualifiedName((TypeDef)typeDefOrRef);
			else if (typeDefOrRef is TypeSpec)
				CreateAssemblyQualifiedName((TypeSpec)typeDefOrRef);
			else
				sb.Append(NULLVALUE);
		}

		void CreateAssemblyQualifiedName(TypeRef typeRef) {
			if (typeRef == null) {
				sb.Append(NULLVALUE);
				return;
			}
			if (!recursionCounter.Increment()) {
				sb.Append(RECURSION_ERROR_RESULT_STRING);
				return;
			}

			CreateFullName(typeRef);
			if (MustUseAssemblyName(typeRef))
				AddAssemblyName(GetDefinitionAssembly(typeRef));

			recursionCounter.Decrement();
		}

		void CreateFullName(TypeRef typeRef) {
			if (typeRef == null) {
				sb.Append(NULLVALUE);
				return;
			}
			if (!recursionCounter.Increment()) {
				sb.Append(RECURSION_ERROR_RESULT_STRING);
				return;
			}

			var declaringTypeRef = typeRef.ResolutionScope as TypeRef;
			if (declaringTypeRef != null) {
				CreateFullName(declaringTypeRef);
				AddNestedTypeSeparator();
			}

			if (AddNamespace(typeRef.Namespace))
				sb.Append('.');
			AddName(typeRef.Name);

			recursionCounter.Decrement();
		}

		void CreateNamespace(TypeRef typeRef) {
			if (typeRef == null) {
				sb.Append(NULLVALUE);
				return;
			}
			AddNamespace(typeRef.Namespace);
		}

		void CreateName(TypeRef typeRef) {
			if (typeRef == null) {
				sb.Append(NULLVALUE);
				return;
			}
			AddName(typeRef.Name);
		}

		void CreateAssemblyQualifiedName(TypeDef typeDef) {
			if (typeDef == null) {
				sb.Append(NULLVALUE);
				return;
			}
			if (!recursionCounter.Increment()) {
				sb.Append(RECURSION_ERROR_RESULT_STRING);
				return;
			}

			CreateFullName(typeDef);
			if (MustUseAssemblyName(typeDef))
				AddAssemblyName(GetDefinitionAssembly(typeDef));

			recursionCounter.Decrement();
		}

		void CreateFullName(TypeDef typeDef) {
			if (typeDef == null) {
				sb.Append(NULLVALUE);
				return;
			}
			if (!recursionCounter.Increment()) {
				sb.Append(RECURSION_ERROR_RESULT_STRING);
				return;
			}

			var declaringTypeDef = typeDef.DeclaringType;
			if (declaringTypeDef != null) {
				CreateFullName(declaringTypeDef);
				AddNestedTypeSeparator();
			}

			if (AddNamespace(typeDef.Namespace))
				sb.Append('.');
			AddName(typeDef.Name);

			recursionCounter.Decrement();
		}

		void CreateNamespace(TypeDef typeDef) {
			if (typeDef == null) {
				sb.Append(NULLVALUE);
				return;
			}
			AddNamespace(typeDef.Namespace);
		}

		void CreateName(TypeDef typeDef) {
			if (typeDef == null) {
				sb.Append(NULLVALUE);
				return;
			}
			AddName(typeDef.Name);
		}

		void CreateAssemblyQualifiedName(TypeSpec typeSpec) {
			if (typeSpec == null) {
				sb.Append(NULLVALUE);
				return;
			}
			CreateAssemblyQualifiedName(typeSpec.TypeSig);
		}

		void CreateFullName(TypeSpec typeSpec) {
			if (typeSpec == null) {
				sb.Append(NULLVALUE);
				return;
			}
			CreateFullName(typeSpec.TypeSig);
		}

		void CreateNamespace(TypeSpec typeSpec) {
			if (typeSpec == null) {
				sb.Append(NULLVALUE);
				return;
			}
			CreateNamespace(typeSpec.TypeSig);
		}

		void CreateName(TypeSpec typeSpec) {
			if (typeSpec == null) {
				sb.Append(NULLVALUE);
				return;
			}
			CreateName(typeSpec.TypeSig);
		}


		void CreateAssemblyQualifiedName(TypeSig typeSig) {
			if (typeSig == null) {
				sb.Append(NULLVALUE);
				return;
			}
			if (!recursionCounter.Increment()) {
				sb.Append(RECURSION_ERROR_RESULT_STRING);
				return;
			}

			CreateFullName(typeSig);
			if (MustUseAssemblyName(typeSig))
				AddAssemblyName(GetDefinitionAssembly(typeSig));

			recursionCounter.Decrement();
		}

		void CreateFullName(TypeSig typeSig) {
			CreateTypeSigName(typeSig, TYPESIG_NAMESPACE | TYPESIG_NAME);
		}

		void CreateNamespace(TypeSig typeSig) {
			CreateTypeSigName(typeSig, TYPESIG_NAMESPACE);
		}

		void CreateName(TypeSig typeSig) {
			CreateTypeSigName(typeSig, TYPESIG_NAME);
		}

		TypeSig ReplaceGenericArg(TypeSig typeSig) {
			if (genericArguments == null)
				return typeSig;
			var newTypeSig = genericArguments.Resolve(typeSig);
			if (newTypeSig != typeSig)
				genericArguments = null;
			return newTypeSig;
		}

		const int TYPESIG_NAMESPACE = 1;
		const int TYPESIG_NAME = 2;
		void CreateTypeSigName(TypeSig typeSig, int flags) {
			if (typeSig == null) {
				sb.Append(NULLVALUE);
				return;
			}
			if (!recursionCounter.Increment()) {
				sb.Append(RECURSION_ERROR_RESULT_STRING);
				return;
			}

			var old = genericArguments;
			typeSig = ReplaceGenericArg(typeSig);

			bool createNamespace = (flags & TYPESIG_NAMESPACE) != 0;
			bool createName = (flags & TYPESIG_NAME) != 0;
			switch (typeSig.ElementType) {
			case ElementType.Void:
			case ElementType.Boolean:
			case ElementType.Char:
			case ElementType.I1:
			case ElementType.U1:
			case ElementType.I2:
			case ElementType.U2:
			case ElementType.I4:
			case ElementType.U4:
			case ElementType.I8:
			case ElementType.U8:
			case ElementType.R4:
			case ElementType.R8:
			case ElementType.String:
			case ElementType.TypedByRef:
			case ElementType.I:
			case ElementType.U:
			case ElementType.Object:
			case ElementType.ValueType:
			case ElementType.Class:
				if (createNamespace && createName)
					CreateFullName(((TypeDefOrRefSig)typeSig).TypeDefOrRef);
				else if (createNamespace)
					CreateNamespace(((TypeDefOrRefSig)typeSig).TypeDefOrRef);
				else if (createName)
					CreateName(((TypeDefOrRefSig)typeSig).TypeDefOrRef);
				break;

			case ElementType.Ptr:
				CreateTypeSigName(typeSig.Next, flags);
				if (createName)
					sb.Append('*');
				break;

			case ElementType.ByRef:
				CreateTypeSigName(typeSig.Next, flags);
				if (createName)
					sb.Append('&');
				break;

			case ElementType.Array:
				CreateTypeSigName(typeSig.Next, flags);
				if (createName) {
					var arraySig = (ArraySig)typeSig;
					sb.Append('[');
					uint rank = arraySig.Rank;
					if (rank == 0)
						sb.Append("<RANK0>");	// Not allowed
					else if (rank == 1)
						sb.Append('*');
					else for (int i = 0; i < (int)rank; i++) {
						if (i != 0)
							sb.Append(',');
						if (!isReflection) {
							const int NO_LOWER = int.MinValue;
							const uint NO_SIZE = uint.MaxValue;
							int lower = arraySig.LowerBounds.Get(i, NO_LOWER);
							uint size = arraySig.Sizes.Get(i, NO_SIZE);
							if (lower != NO_LOWER) {
								sb.Append(lower);
								sb.Append("..");
								if (size != NO_SIZE)
									sb.Append(lower + (int)size - 1);
								else
									sb.Append('.');
							}
						}
					}
					sb.Append(']');
				}
				break;

			case ElementType.SZArray:
				CreateTypeSigName(typeSig.Next, flags);
				if (createName)
					sb.Append("[]");
				break;

			case ElementType.CModReqd:
				CreateTypeSigName(typeSig.Next, flags);
				if (!isReflection && createName) {
					sb.Append(" modreq(");
					if (createNamespace)
						CreateFullName(((ModifierSig)typeSig).Modifier);
					else
						CreateName(((ModifierSig)typeSig).Modifier);
					sb.Append(")");
				}
				break;

			case ElementType.CModOpt:
				CreateTypeSigName(typeSig.Next, flags);
				if (!isReflection && createName) {
					sb.Append(" modopt(");
					if (createNamespace)
						CreateFullName(((ModifierSig)typeSig).Modifier);
					else
						CreateName(((ModifierSig)typeSig).Modifier);
					sb.Append(")");
				}
				break;

			case ElementType.Pinned:
				CreateTypeSigName(typeSig.Next, flags);
				break;

			case ElementType.ValueArray:
				CreateTypeSigName(typeSig.Next, flags);
				if (createName) {
					var valueArraySig = (ValueArraySig)typeSig;
					sb.Append(" ValueArray(");
					sb.Append(valueArraySig.Size);
					sb.Append(')');
				}
				break;

			case ElementType.Module:
				CreateTypeSigName(typeSig.Next, flags);
				if (createName) {
					var moduleSig = (ModuleSig)typeSig;
					sb.Append(" Module(");
					sb.Append(moduleSig.Index);
					sb.Append(')');
				}
				break;

			case ElementType.GenericInst:
				var genericInstSig = (GenericInstSig)typeSig;
				var typeGenArgs = genericInstSig.GenericArguments;
				CreateTypeSigName(genericInstSig.GenericType, flags);
				if (createNamespace && createName) {
					if (isReflection) {
						sb.Append('[');
						int i = -1;
						foreach (var genArg in typeGenArgs.GetSafeEnumerable()) {
							i++;
							if (i != 0)
								sb.Append(',');

							bool mustWriteAssembly = MustUseAssemblyName(genArg);
							if (mustWriteAssembly)
								sb.Append('[');

							CreateFullName(genArg);

							if (mustWriteAssembly) {
								sb.Append(", ");
								var asm = GetDefinitionAssembly(genArg);
								if (asm == null)
									sb.Append(NULLVALUE);
								else
									sb.Append(EscapeAssemblyName(GetAssemblyName(asm)));
								sb.Append(']');
							}
						}
						sb.Append(']');
					}
					else {
						sb.Append('<');
						int i = -1;
						foreach (var genArg in typeGenArgs.GetSafeEnumerable()) {
							i++;
							if (i != 0)
								sb.Append(',');
							CreateFullName(genArg);
						}
						sb.Append('>');
					}
				}
				break;

			case ElementType.Var:
			case ElementType.MVar:
				if (createName) {
					var gs = (GenericSig)typeSig;
					var gp = gs.GenericParam;
					if (gp == null || !AddName(gp.Name)) {
						sb.Append(gs.IsMethodVar ? "!!" : "!");
						sb.Append(gs.Number);
					}
				}
				break;

			case ElementType.FnPtr:
				if (createName) {
					if (isReflection)
						sb.Append("(fnptr)");
					else
						CreateMethodFullName(null, null, ((FnPtrSig)typeSig).MethodSig, null);
				}
				break;

			case ElementType.Sentinel:
				break;

			case ElementType.End:
			case ElementType.R:
			case ElementType.Internal:
			default:
				break;
			}

			genericArguments = old;
			recursionCounter.Decrement();
		}

		void CreateAssemblyQualifiedName(ExportedType exportedType) {
			if (exportedType == null) {
				sb.Append(NULLVALUE);
				return;
			}
			if (!recursionCounter.Increment()) {
				sb.Append(RECURSION_ERROR_RESULT_STRING);
				return;
			}

			CreateFullName(exportedType);
			if (MustUseAssemblyName(exportedType))
				AddAssemblyName(GetDefinitionAssembly(exportedType));

			recursionCounter.Decrement();
		}

		void CreateFullName(ExportedType exportedType) {
			if (exportedType == null) {
				sb.Append(NULLVALUE);
				return;
			}
			if (!recursionCounter.Increment()) {
				sb.Append(RECURSION_ERROR_RESULT_STRING);
				return;
			}

			var declaringExportedType = exportedType.Implementation as ExportedType;
			if (declaringExportedType != null) {
				CreateFullName(declaringExportedType);
				AddNestedTypeSeparator();
			}

			if (AddNamespace(exportedType.TypeNamespace))
				sb.Append('.');
			AddName(exportedType.TypeName);

			recursionCounter.Decrement();
		}

		void CreateNamespace(ExportedType exportedType) {
			if (exportedType == null) {
				sb.Append(NULLVALUE);
				return;
			}
			AddNamespace(exportedType.TypeNamespace);
		}

		void CreateName(ExportedType exportedType) {
			if (exportedType == null) {
				sb.Append(NULLVALUE);
				return;
			}
			AddName(exportedType.TypeName);
		}

		static string GetAssemblyName(IAssembly assembly) {
			var pk = assembly.PublicKeyOrToken;
			if (pk is PublicKey)
				pk = ((PublicKey)pk).Token;
			return Utils.GetAssemblyNameString(EscapeAssemblyName(assembly.Name), assembly.Version, assembly.Culture, pk, assembly.Attributes);
		}

		static string EscapeAssemblyName(UTF8String asmSimplName) {
			return EscapeAssemblyName(UTF8String.ToSystemString(asmSimplName));
		}

		static string EscapeAssemblyName(string asmSimplName) {
			var sb = new StringBuilder(asmSimplName.Length);
			foreach (var c in asmSimplName) {
				if (c == ']')
					sb.Append('\\');
				sb.Append(c);
			}
			return sb.ToString();
		}

		void AddNestedTypeSeparator() {
			if (isReflection)
				sb.Append('+');
			else
				sb.Append('/');
		}

		bool AddNamespace(UTF8String @namespace) {
			if (UTF8String.IsNullOrEmpty(@namespace))
				return false;
			AddIdentifier(@namespace.String);
			return true;
		}

		bool AddName(UTF8String name) {
			if (UTF8String.IsNullOrEmpty(name))
				return false;
			AddIdentifier(name.String);
			return true;
		}

		void AddAssemblyName(IAssembly assembly) {
			sb.Append(", ");
			if (assembly == null)
				sb.Append(NULLVALUE);
			else {
				var pkt = assembly.PublicKeyOrToken;
				if (pkt is PublicKey)
					pkt = ((PublicKey)pkt).Token;
				sb.Append(Utils.GetAssemblyNameString(assembly.Name, assembly.Version, assembly.Culture, pkt, assembly.Attributes));
			}
		}

		void AddIdentifier(string id) {
			if (isReflection) {
				// Periods are not escaped by Reflection, even if they're part of a type name.
				foreach (var c in id) {
					switch (c) {
					case ',':
					case '+':
					case '&':
					case '*':
					case '[':
					case ']':
					case '\\':
						sb.Append('\\');
						break;
					}
					sb.Append(c);
				}
			}
			else
				sb.Append(id);
		}

		IAssembly GetDefinitionAssembly(ITypeDefOrRef typeDefOrRef) {
			var tr = typeDefOrRef as TypeRef;
			if (tr != null)
				return GetDefinitionAssembly(tr);

			var td = typeDefOrRef as TypeDef;
			if (td != null)
				return GetDefinitionAssembly(td);

			var ts = typeDefOrRef as TypeSpec;
			if (ts != null)
				return GetDefinitionAssembly(ts);

			return null;
		}

		IScope GetScope(ITypeDefOrRef typeDefOrRef) {
			var tr = typeDefOrRef as TypeRef;
			if (tr != null)
				return GetScope(tr);

			var td = typeDefOrRef as TypeDef;
			if (td != null)
				return td.Scope;

			var ts = typeDefOrRef as TypeSpec;
			if (ts != null)
				return GetScope(ts);

			return null;
		}

		ITypeDefOrRef GetScopeType(ITypeDefOrRef typeDefOrRef) {
			var tr = typeDefOrRef as TypeRef;
			if (tr != null)
				return tr;

			var td = typeDefOrRef as TypeDef;
			if (td != null)
				return td;

			var ts = typeDefOrRef as TypeSpec;
			if (ts != null)
				return GetScopeType(ts);

			return null;
		}

		ModuleDef GetOwnerModule(ITypeDefOrRef typeDefOrRef) {
			var tr = typeDefOrRef as TypeRef;
			if (tr != null)
				return GetOwnerModule(tr);

			var td = typeDefOrRef as TypeDef;
			if (td != null)
				return GetOwnerModule(td);

			var ts = typeDefOrRef as TypeSpec;
			if (ts != null)
				return GetOwnerModule(ts);

			return null;
		}

		IAssembly GetDefinitionAssembly(TypeRef typeRef) {
			if (typeRef == null)
				return null;
			if (!recursionCounter.Increment())
				return null;
			IAssembly result;

			var scope = typeRef.ResolutionScope;
			if (scope == null)
				result = null;	//TODO: Check ownerModule's ExportedType table
			else if (scope is TypeRef)
				result = GetDefinitionAssembly((TypeRef)scope);
			else if (scope is AssemblyRef)
				result = (AssemblyRef)scope;
			else if (scope is ModuleRef) {
				var ownerModule = GetOwnerModule(typeRef);
				result = ownerModule == null ? null : ownerModule.Assembly;
			}
			else if (scope is ModuleDef)
				result = ((ModuleDef)scope).Assembly;
			else
				result = null;	// Should never be reached

			recursionCounter.Decrement();
			return result;
		}

		IScope GetScope(TypeRef typeRef) {
			if (typeRef == null)
				return null;
			if (!recursionCounter.Increment())
				return null;
			IScope result;
			TypeRef tr;
			AssemblyRef asmRef;
			ModuleRef modRef;
			ModuleDef modDef;

			var scope = typeRef.ResolutionScope;
			if (scope == null)
				result = null;	//TODO: Check ownerModule's ExportedType table
			else if ((tr = scope as TypeRef) != null)
				result = GetScope(tr);
			else if ((asmRef = scope as AssemblyRef) != null)
				result = asmRef;
			else if ((modRef = scope as ModuleRef) != null)
				result = modRef;
			else if ((modDef = scope as ModuleDef) != null)
				result = modDef;
			else
				result = null;	// Should never be reached

			recursionCounter.Decrement();
			return result;
		}

		ModuleDef GetOwnerModule(TypeRef typeRef) {
			if (typeRef == null)
				return null;
			return typeRef.Module;
		}

		IAssembly GetDefinitionAssembly(TypeDef typeDef) {
			var ownerModule = GetOwnerModule(typeDef);
			return ownerModule == null ? null : ownerModule.Assembly;
		}

		ModuleDef GetOwnerModule(TypeDef typeDef) {
			if (typeDef == null)
				return null;

			ModuleDef result = null;
			for (int i = recursionCounter.Counter; i < RecursionCounter.MAX_RECURSION_COUNT; i++) {
				var declaringType = typeDef.DeclaringType;
				if (declaringType == null) {
					result = typeDef.Module2;
					break;
				}
				typeDef = declaringType;
			}

			return result;
		}

		IAssembly GetDefinitionAssembly(TypeSpec typeSpec) {
			if (typeSpec == null)
				return null;
			return GetDefinitionAssembly(typeSpec.TypeSig);
		}

		IScope GetScope(TypeSpec typeSpec) {
			if (typeSpec == null)
				return null;
			return GetScope(typeSpec.TypeSig);
		}

		ITypeDefOrRef GetScopeType(TypeSpec typeSpec) {
			if (typeSpec == null)
				return null;
			return GetScopeType(typeSpec.TypeSig);
		}

		ModuleDef GetOwnerModule(TypeSpec typeSpec) {
			if (typeSpec == null)
				return null;
			return GetOwnerModule(typeSpec.TypeSig);
		}

		IAssembly GetDefinitionAssembly(TypeSig typeSig) {
			if (typeSig == null)
				return null;
			if (!recursionCounter.Increment())
				return null;
			IAssembly result;

			var old = genericArguments;
			typeSig = ReplaceGenericArg(typeSig);

			switch (typeSig.ElementType) {
			case ElementType.Void:
			case ElementType.Boolean:
			case ElementType.Char:
			case ElementType.I1:
			case ElementType.U1:
			case ElementType.I2:
			case ElementType.U2:
			case ElementType.I4:
			case ElementType.U4:
			case ElementType.I8:
			case ElementType.U8:
			case ElementType.R4:
			case ElementType.R8:
			case ElementType.String:
			case ElementType.TypedByRef:
			case ElementType.I:
			case ElementType.U:
			case ElementType.Object:
			case ElementType.ValueType:
			case ElementType.Class:
				result = GetDefinitionAssembly(((TypeDefOrRefSig)typeSig).TypeDefOrRef);
				break;

			case ElementType.Ptr:
			case ElementType.ByRef:
			case ElementType.Array:
			case ElementType.SZArray:
			case ElementType.CModReqd:
			case ElementType.CModOpt:
			case ElementType.Pinned:
			case ElementType.ValueArray:
			case ElementType.Module:
				result = GetDefinitionAssembly(typeSig.Next);
				break;

			case ElementType.GenericInst:
				var genericInstSig = (GenericInstSig)typeSig;
				var genericType = genericInstSig.GenericType;
				result = GetDefinitionAssembly(genericType == null ? null : genericType.TypeDefOrRef);
				break;

			case ElementType.Var:
			case ElementType.MVar:
			case ElementType.FnPtr:
			case ElementType.Sentinel:
			case ElementType.End:
			case ElementType.R:
			case ElementType.Internal:
			default:
				result = null;
				break;
			}

			genericArguments = old;
			recursionCounter.Decrement();
			return result;
		}

		ITypeDefOrRef GetScopeType(TypeSig typeSig) {
			if (typeSig == null)
				return null;
			if (!recursionCounter.Increment())
				return null;
			ITypeDefOrRef result;

			var old = genericArguments;
			typeSig = ReplaceGenericArg(typeSig);

			switch (typeSig.ElementType) {
			case ElementType.Void:
			case ElementType.Boolean:
			case ElementType.Char:
			case ElementType.I1:
			case ElementType.U1:
			case ElementType.I2:
			case ElementType.U2:
			case ElementType.I4:
			case ElementType.U4:
			case ElementType.I8:
			case ElementType.U8:
			case ElementType.R4:
			case ElementType.R8:
			case ElementType.String:
			case ElementType.TypedByRef:
			case ElementType.I:
			case ElementType.U:
			case ElementType.Object:
			case ElementType.ValueType:
			case ElementType.Class:
				result = GetScopeType(((TypeDefOrRefSig)typeSig).TypeDefOrRef);
				break;

			case ElementType.Ptr:
			case ElementType.ByRef:
			case ElementType.Array:
			case ElementType.SZArray:
			case ElementType.CModReqd:
			case ElementType.CModOpt:
			case ElementType.Pinned:
			case ElementType.ValueArray:
			case ElementType.Module:
				result = GetScopeType(typeSig.Next);
				break;

			case ElementType.GenericInst:
				var genericInstSig = (GenericInstSig)typeSig;
				var genericType = genericInstSig.GenericType;
				result = GetScopeType(genericType == null ? null : genericType.TypeDefOrRef);
				break;

			case ElementType.Var:
			case ElementType.MVar:
			case ElementType.FnPtr:
			case ElementType.Sentinel:
			case ElementType.End:
			case ElementType.R:
			case ElementType.Internal:
			default:
				result = null;
				break;
			}

			genericArguments = old;
			recursionCounter.Decrement();
			return result;
		}

		IScope GetScope(TypeSig typeSig) {
			if (typeSig == null)
				return null;
			if (!recursionCounter.Increment())
				return null;
			IScope result;

			var old = genericArguments;
			typeSig = ReplaceGenericArg(typeSig);

			switch (typeSig.ElementType) {
			case ElementType.Void:
			case ElementType.Boolean:
			case ElementType.Char:
			case ElementType.I1:
			case ElementType.U1:
			case ElementType.I2:
			case ElementType.U2:
			case ElementType.I4:
			case ElementType.U4:
			case ElementType.I8:
			case ElementType.U8:
			case ElementType.R4:
			case ElementType.R8:
			case ElementType.String:
			case ElementType.TypedByRef:
			case ElementType.I:
			case ElementType.U:
			case ElementType.Object:
			case ElementType.ValueType:
			case ElementType.Class:
				result = GetScope(((TypeDefOrRefSig)typeSig).TypeDefOrRef);
				break;

			case ElementType.Ptr:
			case ElementType.ByRef:
			case ElementType.Array:
			case ElementType.SZArray:
			case ElementType.CModReqd:
			case ElementType.CModOpt:
			case ElementType.Pinned:
			case ElementType.ValueArray:
			case ElementType.Module:
				result = GetScope(typeSig.Next);
				break;

			case ElementType.GenericInst:
				var genericInstSig = (GenericInstSig)typeSig;
				var genericType = genericInstSig.GenericType;
				result = GetScope(genericType == null ? null : genericType.TypeDefOrRef);
				break;

			case ElementType.Var:
			case ElementType.MVar:
			case ElementType.FnPtr:
			case ElementType.Sentinel:
			case ElementType.End:
			case ElementType.R:
			case ElementType.Internal:
			default:
				result = null;
				break;
			}

			genericArguments = old;
			recursionCounter.Decrement();
			return result;
		}

		ModuleDef GetOwnerModule(TypeSig typeSig) {
			if (typeSig == null)
				return null;
			if (!recursionCounter.Increment())
				return null;
			ModuleDef result;

			var old = genericArguments;
			typeSig = ReplaceGenericArg(typeSig);

			switch (typeSig.ElementType) {
			case ElementType.Void:
			case ElementType.Boolean:
			case ElementType.Char:
			case ElementType.I1:
			case ElementType.U1:
			case ElementType.I2:
			case ElementType.U2:
			case ElementType.I4:
			case ElementType.U4:
			case ElementType.I8:
			case ElementType.U8:
			case ElementType.R4:
			case ElementType.R8:
			case ElementType.String:
			case ElementType.TypedByRef:
			case ElementType.I:
			case ElementType.U:
			case ElementType.Object:
			case ElementType.ValueType:
			case ElementType.Class:
				result = GetOwnerModule(((TypeDefOrRefSig)typeSig).TypeDefOrRef);
				break;

			case ElementType.Ptr:
			case ElementType.ByRef:
			case ElementType.Array:
			case ElementType.SZArray:
			case ElementType.CModReqd:
			case ElementType.CModOpt:
			case ElementType.Pinned:
			case ElementType.ValueArray:
			case ElementType.Module:
				result = GetOwnerModule(typeSig.Next);
				break;

			case ElementType.GenericInst:
				var genericInstSig = (GenericInstSig)typeSig;
				var genericType = genericInstSig.GenericType;
				result = GetOwnerModule(genericType == null ? null : genericType.TypeDefOrRef);
				break;

			case ElementType.Var:
			case ElementType.MVar:
			case ElementType.FnPtr:
			case ElementType.Sentinel:
			case ElementType.End:
			case ElementType.R:
			case ElementType.Internal:
			default:
				result = null;
				break;
			}

			genericArguments = old;
			recursionCounter.Decrement();
			return result;
		}

		IAssembly GetDefinitionAssembly(ExportedType exportedType) {
			if (exportedType == null)
				return null;
			if (!recursionCounter.Increment())
				return null;
			IAssembly result;
			ExportedType et;
			AssemblyRef asmRef;

			var scope = exportedType.Implementation;
			if ((et = scope as ExportedType) != null)
				result = GetDefinitionAssembly(et);
			else if ((asmRef = scope as AssemblyRef) != null)
				result = asmRef;
			else if (scope is FileDef) {
				var ownerModule = GetOwnerModule(exportedType);
				result = ownerModule == null ? null : ownerModule.Assembly;
			}
			else
				result = null;

			recursionCounter.Decrement();
			return result;
		}

		ITypeDefOrRef GetScopeType(ExportedType exportedType) {
			return null;
		}

		IScope GetScope(ExportedType exportedType) {
			if (exportedType == null)
				return null;
			if (!recursionCounter.Increment())
				return null;
			IScope result;
			ExportedType et;
			AssemblyRef asmRef;
			FileDef file;

			var scope = exportedType.Implementation;
			if ((et = scope as ExportedType) != null)
				result = GetScope(et);
			else if ((asmRef = scope as AssemblyRef) != null)
				result = asmRef;
			else if ((file = scope as FileDef) != null) {
				var ownerModule = GetOwnerModule(exportedType);
				//TODO: Not all modules' names are equal to the name in FileDef.Name
				var modRef = new ModuleRefUser(ownerModule, file.Name);
				if (ownerModule != null)
					ownerModule.UpdateRowId(modRef);
				result = modRef;
			}
			else
				result = null;

			recursionCounter.Decrement();
			return result;
		}

		ModuleDef GetOwnerModule(ExportedType exportedType) {
			if (exportedType == null)
				return null;
			return exportedType.Module;
		}

		void CreateFieldFullName(string declaringType, string name, FieldSig fieldSig) {
			CreateFullName(fieldSig == null ? null : fieldSig.Type);
			sb.Append(' ');

			if (declaringType != null) {
				sb.Append(declaringType);
				sb.Append("::");
			}
			if (name != null)
				sb.Append(name);
		}

		void CreateMethodFullName(string declaringType, string name, MethodBaseSig methodSig, MethodDef gppMethod) {
			if (methodSig == null) {
				sb.Append(NULLVALUE);
				return;
			}

			CreateFullName(methodSig.RetType);
			sb.Append(' ');
			if (declaringType != null) {
				sb.Append(declaringType);
				sb.Append("::");
			}
			if (name != null)
				sb.Append(name);

			if (methodSig.Generic) {
				sb.Append('<');
				uint genParamCount = methodSig.GenParamCount;
				for (uint i = 0; i < genParamCount; i++) {
					if (i != 0)
						sb.Append(',');
					CreateFullName(new GenericMVar(i, gppMethod));
				}
				sb.Append('>');
			}
			sb.Append('(');
			int count = PrintMethodArgList(methodSig.Params, false, false);
			PrintMethodArgList(methodSig.ParamsAfterSentinel, count > 0, true);
			sb.Append(')');
		}

		int PrintMethodArgList(IEnumerable<TypeSig> args, bool hasPrintedArgs, bool isAfterSentinel) {
			if (args == null)
				return 0;
			if (isAfterSentinel) {
				if (hasPrintedArgs)
					sb.Append(',');
				sb.Append("...");
				hasPrintedArgs = true;
			}
			int count = 0;
			foreach (var arg in args.GetSafeEnumerable()) {
				count++;
				if (hasPrintedArgs)
					sb.Append(',');
				CreateFullName(arg);
				hasPrintedArgs = true;
			}
			return count;
		}

		void CreatePropertyFullName(string declaringType, UTF8String name, CallingConventionSig propertySig) {
			CreateMethodFullName(declaringType, UTF8String.ToSystemString(name), propertySig as MethodBaseSig, null);
		}

		void CreateEventFullName(string declaringType, UTF8String name, ITypeDefOrRef typeDefOrRef) {
			CreateFullName(typeDefOrRef);
			sb.Append(' ');
			if (declaringType != null) {
				sb.Append(declaringType);
				sb.Append("::");
			}
			if (!UTF8String.IsNull(name))
				sb.Append(UTF8String.ToSystemString(name));
		}

		/// <inheritdoc/>
		public override string ToString() {
			return Result;
		}
	}
}
