using System;
using System.Collections.Generic;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// The table row can be referenced by a MD token
	/// </summary>
	public interface IMDTokenProvider {
		/// <summary>
		/// Returns the metadata token
		/// </summary>
		MDToken MDToken { get; }

		/// <summary>
		/// Getst/sets the row ID
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
		AssemblyFlags Flags { get; set; }

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
		UTF8String Locale { get; set; }
	}

	static partial class Extensions {
		/// <summary>
		/// Checks whether <paramref name="asm"/> appears to be the core library (eg.
		/// mscorlib or System.Runtime)
		/// </summary>
		/// <param name="asm">The assembly</param>
		public static bool IsCorLib(this IAssembly asm) {
			string asmName;
			return asm != null &&
				UTF8String.IsNullOrEmpty(asm.Locale) &&
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
			return new AssemblyRefUser(asm.Name, asm.Version, asm.PublicKeyOrToken, asm.Locale);
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

			var ownerModule = type.OwnerModule;
			if (ownerModule != null) {
				var corLibType = ownerModule.CorLibTypes.GetCorLibTypeSig(type);
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
		public static TypeDefOrRefSig ToTypeDefOrRefSig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as TypeDefOrRefSig;
		}

		/// <summary>
		/// Returns a <see cref="ClassOrValueTypeSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="ClassOrValueTypeSig"/> or <c>null</c> if it's not a
		/// <see cref="ClassOrValueTypeSig"/></returns>
		public static ClassOrValueTypeSig ToClassOrValueTypeSig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as ClassOrValueTypeSig;
		}

		/// <summary>
		/// Returns a <see cref="ValueTypeSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="ValueTypeSig"/> or <c>null</c> if it's not a
		/// <see cref="ValueTypeSig"/></returns>
		public static ValueTypeSig ToValueTypeSig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as ValueTypeSig;
		}

		/// <summary>
		/// Returns a <see cref="ClassSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="ClassSig"/> or <c>null</c> if it's not a
		/// <see cref="ClassSig"/></returns>
		public static ClassSig ToClassSig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as ClassSig;
		}

		/// <summary>
		/// Returns a <see cref="GenericSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="GenericSig"/> or <c>null</c> if it's not a
		/// <see cref="GenericSig"/></returns>
		public static GenericSig ToGenericSig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as GenericSig;
		}

		/// <summary>
		/// Returns a <see cref="GenericVar"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="GenericVar"/> or <c>null</c> if it's not a
		/// <see cref="GenericVar"/></returns>
		public static GenericVar ToGenericVar(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as GenericVar;
		}

		/// <summary>
		/// Returns a <see cref="GenericMVar"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="GenericMVar"/> or <c>null</c> if it's not a
		/// <see cref="GenericMVar"/></returns>
		public static GenericMVar ToGenericMVar(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as GenericMVar;
		}

		/// <summary>
		/// Returns a <see cref="GenericInstSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="GenericInstSig"/> or <c>null</c> if it's not a
		/// <see cref="GenericInstSig"/></returns>
		public static GenericInstSig ToGenericInstSig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as GenericInstSig;
		}

		/// <summary>
		/// Returns a <see cref="PtrSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="PtrSig"/> or <c>null</c> if it's not a
		/// <see cref="PtrSig"/></returns>
		public static PtrSig ToPtrSig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as PtrSig;
		}

		/// <summary>
		/// Returns a <see cref="ByRefSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="ByRefSig"/> or <c>null</c> if it's not a
		/// <see cref="ByRefSig"/></returns>
		public static ByRefSig ToByRefSig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as ByRefSig;
		}

		/// <summary>
		/// Returns a <see cref="ArraySig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="ArraySig"/> or <c>null</c> if it's not a
		/// <see cref="ArraySig"/></returns>
		public static ArraySig ToArraySig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as ArraySig;
		}

		/// <summary>
		/// Returns a <see cref="SZArraySig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="SZArraySig"/> or <c>null</c> if it's not a
		/// <see cref="SZArraySig"/></returns>
		public static SZArraySig ToSZArraySig(this ITypeDefOrRef type) {
			var ts = type as TypeSpec;
			return ts == null ? null : ts.TypeSig.RemovePinnedAndModifiers() as SZArraySig;
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
		/// It's an <see cref="dot10.DotNet.AssemblyRef"/> instance
		/// </summary>
		AssemblyRef,

		/// <summary>
		/// It's a <see cref="dot10.DotNet.ModuleRef"/> instance
		/// </summary>
		ModuleRef,

		/// <summary>
		/// It's a <see cref="dot10.DotNet.ModuleDef"/> instance
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
	/// Implemented by types, fields, methods, properties, events
	/// </summary>
	public interface IMemberRef : IFullName {
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
	public interface IField : ICodedToken, IFullName, IMemberRef {
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
		IList<DeclSecurity> DeclSecurities { get; }
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
		IList<GenericParam> GenericParams { get; }
	}

	static partial class Extensions {
		/// <summary>
		/// Convert a <see cref="TypeSig"/> to a <see cref="ITypeDefOrRef"/>
		/// </summary>
		/// <param name="sig">The sig</param>
		public static ITypeDefOrRef ToTypeDefOrRef(this TypeSig sig) {
			if (sig == null)
				return null;
			var tdrSig = sig as TypeDefOrRefSig;
			if (tdrSig != null)
				return tdrSig.TypeDefOrRef;
			var ownerModule = sig.OwnerModule;
			if (ownerModule == null)
				return new TypeSpecUser(sig);
			return ownerModule.UpdateRowId(new TypeSpecUser(sig));
		}
	}
}
