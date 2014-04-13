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

﻿using System;
using dnlib.Threading;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet {
	/// <summary>
	/// The table row can be referenced by a MD token
	/// </summary>
	public interface IMDTokenProvider {
		/// <summary>
		/// Returns the metadata token
		/// </summary>
		MDToken MDToken { get; }

		/// <summary>
		/// Gets/sets the row ID
		/// </summary>
		uint Rid { get; set; }
	}

	/// <summary>
	/// Interface to access an <see cref="AssemblyRef"/> or an <see cref="AssemblyDef"/>
	/// </summary>
	public interface IAssembly : IFullName {
		/// <summary>
		/// The assembly version
		/// </summary>
		Version Version { get; set; }

		/// <summary>
		/// Assembly flags
		/// </summary>
		AssemblyAttributes Attributes { get; set; }

		/// <summary>
		/// Public key or public key token
		/// </summary>
		PublicKeyBase PublicKeyOrToken { get; }

		/// <summary>
		/// Simple assembly name
		/// </summary>
		UTF8String Name { get; set; }

		/// <summary>
		/// Locale, aka culture
		/// </summary>
		UTF8String Culture { get; set; }
	}

	public static partial class Extensions {
		/// <summary>
		/// Checks whether <paramref name="asm"/> appears to be the core library (eg.
		/// mscorlib or System.Runtime)
		/// </summary>
		/// <param name="asm">The assembly</param>
		public static bool IsCorLib(this IAssembly asm) {
			string asmName;
			return asm != null &&
				UTF8String.IsNullOrEmpty(asm.Culture) &&
				((asmName = UTF8String.ToSystemStringOrEmpty(asm.Name)).Equals("mscorlib", StringComparison.OrdinalIgnoreCase) ||
				asmName.Equals("System.Runtime", StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Converts <paramref name="asm"/> to a <see cref="AssemblyRef"/> instance
		/// </summary>
		/// <param name="asm">The assembly</param>
		/// <returns>A new <see cref="AssemblyRef"/> instance</returns>
		public static AssemblyRef ToAssemblyRef(this IAssembly asm) {
			if (asm == null)
				return null;
			// Always create a new one, even if it happens to be an AssemblyRef
			return new AssemblyRefUser(asm.Name, asm.Version, asm.PublicKeyOrToken, asm.Culture);
		}

		/// <summary>
		/// Converts <paramref name="type"/> to a <see cref="TypeSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="TypeSig"/> instance or <c>null</c> if <paramref name="type"/>
		/// is invalid</returns>
		public static TypeSig ToTypeSig(this ITypeDefOrRef type) {
			return ToTypeSig(type, true);
		}

		/// <summary>
		/// Converts <paramref name="type"/> to a <see cref="TypeSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <param name="checkValueType"><c>true</c> if we should try to figure out whether
		/// <paramref name="type"/> is a <see cref="ValueType"/></param>
		/// <returns>A <see cref="TypeSig"/> instance or <c>null</c> if <paramref name="type"/>
		/// is invalid</returns>
		public static TypeSig ToTypeSig(this ITypeDefOrRef type, bool checkValueType) {
			if (type == null)
				return null;

			var module = type.Module;
			if (module != null) {
				var corLibType = module.CorLibTypes.GetCorLibTypeSig(type);
				if (corLibType != null)
					return corLibType;
			}

			var td = type as TypeDef;
			if (td != null)
				return CreateClassOrValueType(type, checkValueType ? td.IsValueType : false);

			var tr = type as TypeRef;
			if (tr != null) {
				if (checkValueType)
					td = tr.Resolve();
				return CreateClassOrValueType(type, td == null ? false : td.IsValueType);
			}

			var ts = type as TypeSpec;
			if (ts != null)
				return ts.TypeSig;

			return null;
		}

		static TypeSig CreateClassOrValueType(ITypeDefOrRef type, bool isValueType) {
			if (isValueType)
				return new ValueTypeSig(type);
			return new ClassSig(type);
		}

		/// <summary>
		/// Returns a <see cref="TypeDefOrRefSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="TypeDefOrRefSig"/> or <c>null</c> if it's not a
		/// <see cref="TypeDefOrRefSig"/></returns>
		public static TypeDefOrRefSig TryGetTypeDefOrRefSig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as TypeDefOrRefSig;
		}

		/// <summary>
		/// Returns a <see cref="ClassOrValueTypeSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="ClassOrValueTypeSig"/> or <c>null</c> if it's not a
		/// <see cref="ClassOrValueTypeSig"/></returns>
		public static ClassOrValueTypeSig TryGetClassOrValueTypeSig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as ClassOrValueTypeSig;
		}

		/// <summary>
		/// Returns a <see cref="ValueTypeSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="ValueTypeSig"/> or <c>null</c> if it's not a
		/// <see cref="ValueTypeSig"/></returns>
		public static ValueTypeSig TryGetValueTypeSig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as ValueTypeSig;
		}

		/// <summary>
		/// Returns a <see cref="ClassSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="ClassSig"/> or <c>null</c> if it's not a
		/// <see cref="ClassSig"/></returns>
		public static ClassSig TryGetClassSig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as ClassSig;
		}

		/// <summary>
		/// Returns a <see cref="GenericSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="GenericSig"/> or <c>null</c> if it's not a
		/// <see cref="GenericSig"/></returns>
		public static GenericSig TryGetGenericSig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as GenericSig;
		}

		/// <summary>
		/// Returns a <see cref="GenericVar"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="GenericVar"/> or <c>null</c> if it's not a
		/// <see cref="GenericVar"/></returns>
		public static GenericVar TryGetGenericVar(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as GenericVar;
		}

		/// <summary>
		/// Returns a <see cref="GenericMVar"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="GenericMVar"/> or <c>null</c> if it's not a
		/// <see cref="GenericMVar"/></returns>
		public static GenericMVar TryGetGenericMVar(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as GenericMVar;
		}

		/// <summary>
		/// Returns a <see cref="GenericInstSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="GenericInstSig"/> or <c>null</c> if it's not a
		/// <see cref="GenericInstSig"/></returns>
		public static GenericInstSig TryGetGenericInstSig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as GenericInstSig;
		}

		/// <summary>
		/// Returns a <see cref="PtrSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="PtrSig"/> or <c>null</c> if it's not a
		/// <see cref="PtrSig"/></returns>
		public static PtrSig TryGetPtrSig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as PtrSig;
		}

		/// <summary>
		/// Returns a <see cref="ByRefSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="ByRefSig"/> or <c>null</c> if it's not a
		/// <see cref="ByRefSig"/></returns>
		public static ByRefSig TryGetByRefSig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as ByRefSig;
		}

		/// <summary>
		/// Returns a <see cref="ArraySig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="ArraySig"/> or <c>null</c> if it's not a
		/// <see cref="ArraySig"/></returns>
		public static ArraySig TryGetArraySig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as ArraySig;
		}

		/// <summary>
		/// Returns a <see cref="SZArraySig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="SZArraySig"/> or <c>null</c> if it's not a
		/// <see cref="SZArraySig"/></returns>
		public static SZArraySig TryGetSZArraySig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as SZArraySig;
		}

		/// <summary>
		/// Returns the base type of <paramref name="tdr"/>. Throws if we can't resolve
		/// a <see cref="TypeRef"/>.
		/// </summary>
		/// <param name="tdr">The type</param>
		/// <returns>The base type or <c>null</c> if there's no base type</returns>
		public static ITypeDefOrRef GetBaseTypeThrow(this ITypeDefOrRef tdr) {
			return tdr.GetBaseType(true);
		}

		/// <summary>
		/// Returns the base type of <paramref name="tdr"/>
		/// </summary>
		/// <param name="tdr">The type</param>
		/// <returns>The base type or <c>null</c> if there's no base type, or if
		/// we couldn't resolve a <see cref="TypeRef"/></returns>
		public static ITypeDefOrRef GetBaseType(this ITypeDefOrRef tdr) {
			return tdr.GetBaseType(false);
		}

		/// <summary>
		/// Returns the base type of <paramref name="tdr"/>
		/// </summary>
		/// <param name="tdr">The type</param>
		/// <param name="throwOnResolveFailure"><c>true</c> if we should throw if we can't
		/// resolve a <see cref="TypeRef"/>. <c>false</c> if we should ignore the error and
		/// just return <c>null</c>.</param>
		/// <returns>The base type or <c>null</c> if there's no base type, or if
		/// <paramref name="throwOnResolveFailure"/> is <c>true</c> and we couldn't resolve
		/// a <see cref="TypeRef"/></returns>
		public static ITypeDefOrRef GetBaseType(this ITypeDefOrRef tdr, bool throwOnResolveFailure) {
			var td = tdr as TypeDef;
			if (td != null)
				return td.BaseType;

			var tr = tdr as TypeRef;
			if (tr != null) {
				td = throwOnResolveFailure ? tr.ResolveThrow() : tr.Resolve();
				return td == null ? null : td.BaseType;
			}

			var ts = tdr as TypeSpec;
			if (ts == null)
				return null;

			var git = ts.TypeSig.ToGenericInstSig();
			if (git != null) {
				var genType = git.GenericType;
				tdr = genType == null ? null : genType.TypeDefOrRef;
			}
			else {
				var sig = ts.TypeSig.ToTypeDefOrRefSig();
				tdr = sig == null ? null : sig.TypeDefOrRef;
			}

			td = tdr as TypeDef;
			if (td != null)
				return td.BaseType;

			tr = tdr as TypeRef;
			if (tr != null) {
				td = throwOnResolveFailure ? tr.ResolveThrow() : tr.Resolve();
				return td == null ? null : td.BaseType;
			}

			return null;
		}

		/// <summary>
		/// Gets the scope type, resolves it, and returns the <see cref="TypeDef"/>
		/// </summary>
		/// <param name="tdr">Type</param>
		/// <returns>A <see cref="TypeDef"/> or <c>null</c> if input was <c>null</c> or if we
		/// couldn't resolve the reference.</returns>
		public static TypeDef ResolveTypeDef(this ITypeDefOrRef tdr) {
			var td = tdr as TypeDef;
			if (td != null)
				return td;

			var tr = tdr as TypeRef;
			if (tr != null)
				return tr.Resolve();

			if (tdr == null)
				return null;
			tdr = tdr.ScopeType;

			td = tdr as TypeDef;
			if (td != null)
				return td;

			tr = tdr as TypeRef;
			if (tr != null)
				return tr.Resolve();

			return null;
		}
	}

	/// <summary>
	/// Implemented by <see cref="MethodDef"/> and <see cref="FileDef"/>, which are the only
	/// valid managed entry point tokens.
	/// </summary>
	public interface IManagedEntryPoint : ICodedToken {
	}

	/// <summary>
	/// Interface to access a module def/ref
	/// </summary>
	public interface IModule : IScope {
		/// <summary>
		/// Gets the module name
		/// </summary>
		UTF8String Name { get; }
	}

	/// <summary>
	/// Type of scope
	/// </summary>
	public enum ScopeType {
		/// <summary>
		/// It's an <see cref="dnlib.DotNet.AssemblyRef"/> instance
		/// </summary>
		AssemblyRef,

		/// <summary>
		/// It's a <see cref="dnlib.DotNet.ModuleRef"/> instance
		/// </summary>
		ModuleRef,

		/// <summary>
		/// It's a <see cref="dnlib.DotNet.ModuleDef"/> instance
		/// </summary>
		ModuleDef,
	}

	/// <summary>
	/// Implemented by modules and assemblies
	/// </summary>
	public interface IScope {
		/// <summary>
		/// Gets the scope type
		/// </summary>
		ScopeType ScopeType { get; }

		/// <summary>
		/// Gets the scope name
		/// </summary>
		string ScopeName { get; }
	}

	/// <summary>
	/// Interface to get the full name of a type, field, or method
	/// </summary>
	public interface IFullName {
		/// <summary>
		/// Gets the full name
		/// </summary>
		string FullName { get; }
	}

	/// <summary>
	/// Implemented by all member refs and types
	/// </summary>
	public interface IOwnerModule {
		/// <summary>
		/// Gets the owner module
		/// </summary>
		ModuleDef Module { get; }
	}

	/// <summary>
	/// Implemented by types, fields, methods, properties, events
	/// </summary>
	public interface IMemberRef : ICodedToken, IFullName, IOwnerModule {
		/// <summary>
		/// Gets/sets the name
		/// </summary>
		UTF8String Name { get; set; }
	}

	/// <summary>
	/// Implemented by types and methods
	/// </summary>
	public interface IGenericParameterProvider : ICodedToken {
		/// <summary>
		/// <c>true</c> if this is a method
		/// </summary>
		bool IsMethod { get; }

		/// <summary>
		/// <c>true</c> if this is a type
		/// </summary>
		bool IsType { get; }

		/// <summary>
		/// Gets the number of generic parameters / arguments
		/// </summary>
		int NumberOfGenericParameters { get; }
	}

	/// <summary>
	/// Implemented by fields (<see cref="FieldDef"/> and <see cref="MemberRef"/>)
	/// </summary>
	public interface IField : ICodedToken, ITokenOperand, IFullName, IMemberRef {
		/// <summary>
		/// Gets/sets the field signature
		/// </summary>
		FieldSig FieldSig { get; set; }

		/// <summary>
		/// Gets the declaring type
		/// </summary>
		ITypeDefOrRef DeclaringType { get; }
	}

	/// <summary>
	/// Implemented by methods (<see cref="MethodDef"/>, <see cref="MemberRef"/> and <see cref="MethodSpec"/>)
	/// </summary>
	public interface IMethod : ICodedToken, ITokenOperand, IFullName, IGenericParameterProvider, IMemberRef {
		/// <summary>
		/// Method signature
		/// </summary>
		MethodSig MethodSig { get; set; }

		/// <summary>
		/// Gets the declaring type
		/// </summary>
		ITypeDefOrRef DeclaringType { get; }
	}

	/// <summary>
	/// Implemented by tables that can be a token in the <c>ldtoken</c> instruction
	/// </summary>
	public interface ITokenOperand : ICodedToken {
	}

	/// <summary>
	/// The table row can be referenced by a coded token
	/// </summary>
	public interface ICodedToken : IMDTokenProvider {
	}

	/// <summary>
	/// TypeDefOrRef coded token interface
	/// </summary>
	public interface ITypeDefOrRef : ICodedToken, IHasCustomAttribute, IMemberRefParent, IType, ITokenOperand, IMemberRef {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int TypeDefOrRefTag { get; }
	}

	/// <summary>
	/// HasConstant coded token interface
	/// </summary>
	public interface IHasConstant : ICodedToken, IHasCustomAttribute, IFullName {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int HasConstantTag { get; }

		/// <summary>
		/// Gets/sets the constant value
		/// </summary>
		Constant Constant { get; set; }
	}

	/// <summary>
	/// HasCustomAttribute coded token interface
	/// </summary>
	public interface IHasCustomAttribute : ICodedToken {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int HasCustomAttributeTag { get; }

		/// <summary>
		/// Gets all custom attributes
		/// </summary>
		CustomAttributeCollection CustomAttributes { get; }

		/// <summary>
		/// <c>true</c> if <see cref="CustomAttributes"/> is not empty
		/// </summary>
		bool HasCustomAttributes { get; }
	}

	/// <summary>
	/// HasFieldMarshal coded token interface
	/// </summary>
	public interface IHasFieldMarshal : ICodedToken, IHasCustomAttribute, IHasConstant, IFullName {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int HasFieldMarshalTag { get; }

		/// <summary>
		/// Gets/sets the field marshal
		/// </summary>
		FieldMarshal FieldMarshal { get; set; }
	}

	/// <summary>
	/// HasDeclSecurity coded token interface
	/// </summary>
	public interface IHasDeclSecurity : ICodedToken, IHasCustomAttribute, IFullName {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int HasDeclSecurityTag { get; }

		/// <summary>
		/// Gets the permission sets
		/// </summary>
		ThreadSafe.IList<DeclSecurity> DeclSecurities { get; }
	}

	/// <summary>
	/// MemberRefParent coded token interface
	/// </summary>
	public interface IMemberRefParent : ICodedToken, IHasCustomAttribute, IFullName {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int MemberRefParentTag { get; }
	}

	/// <summary>
	/// HasSemantic coded token interface
	/// </summary>
	public interface IHasSemantic : ICodedToken, IHasCustomAttribute, IFullName {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int HasSemanticTag { get; }
	}

	/// <summary>
	/// MethodDefOrRef coded token interface
	/// </summary>
	public interface IMethodDefOrRef : ICodedToken, IHasCustomAttribute, ICustomAttributeType, IMethod {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int MethodDefOrRefTag { get; }
	}

	/// <summary>
	/// MemberForwarded coded token interface
	/// </summary>
	public interface IMemberForwarded : ICodedToken, IHasCustomAttribute, IFullName {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int MemberForwardedTag { get; }

		/// <summary>
		/// Gets/sets the impl map
		/// </summary>
		ImplMap ImplMap { get; set; }
	}

	/// <summary>
	/// Implementation coded token interface
	/// </summary>
	public interface IImplementation : ICodedToken, IHasCustomAttribute, IFullName {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int ImplementationTag { get; }
	}

	/// <summary>
	/// CustomAttributeType coded token interface
	/// </summary>
	public interface ICustomAttributeType : ICodedToken, IHasCustomAttribute {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int CustomAttributeTypeTag { get; }
	}

	/// <summary>
	/// ResolutionScope coded token interface
	/// </summary>
	public interface IResolutionScope : ICodedToken, IHasCustomAttribute, IFullName {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int ResolutionScopeTag { get; }
	}

	/// <summary>
	/// TypeOrMethodDef coded token interface
	/// </summary>
	public interface ITypeOrMethodDef : ICodedToken, IHasCustomAttribute, IHasDeclSecurity, IMemberRefParent, IFullName {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int TypeOrMethodDefTag { get; }

		/// <summary>
		/// Gets the generic parameters
		/// </summary>
		ThreadSafe.IList<GenericParam> GenericParameters { get; }
	}

	public static partial class Extensions {
		/// <summary>
		/// Converts a <see cref="TypeSig"/> to a <see cref="ITypeDefOrRef"/>
		/// </summary>
		/// <param name="sig">The sig</param>
		public static ITypeDefOrRef ToTypeDefOrRef(this TypeSig sig) {
			if (sig == null)
				return null;
			var tdrSig = sig as TypeDefOrRefSig;
			if (tdrSig != null)
				return tdrSig.TypeDefOrRef;
			var module = sig.Module;
			if (module == null)
				return new TypeSpecUser(sig);
			return module.UpdateRowId(new TypeSpecUser(sig));
		}
	}
}
