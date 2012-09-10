using System;
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
	}

	/// <summary>
	/// Interface to access an <see cref="AssemblyRef"/> or an <see cref="AssemblyDef"/>
	/// </summary>
	public interface IAssembly {
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

	/// <summary>
	/// The table row can be referenced by a coded token
	/// </summary>
	public interface ICodedToken : IMDTokenProvider {
	}

	/// <summary>
	/// TypeDefOrRef coded token interface
	/// </summary>
	public interface ITypeDefOrRef : ICodedToken, IFullName {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int TypeDefOrRefTag { get; }
	}

	/// <summary>
	/// HasConstant coded token interface
	/// </summary>
	public interface IHasConstant : ICodedToken {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int HasConstantTag { get; }
	}

	/// <summary>
	/// HasCustomAttribute coded token interface
	/// </summary>
	public interface IHasCustomAttribute : ICodedToken {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int HasCustomAttributeTag { get; }
	}

	/// <summary>
	/// HasFieldMarshal coded token interface
	/// </summary>
	public interface IHasFieldMarshal : ICodedToken {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int HasFieldMarshalTag { get; }
	}

	/// <summary>
	/// HasDeclSecurity coded token interface
	/// </summary>
	public interface IHasDeclSecurity : ICodedToken {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int HasDeclSecurityTag { get; }
	}

	/// <summary>
	/// MemberRefParent coded token interface
	/// </summary>
	public interface IMemberRefParent : ICodedToken {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int MemberRefParentTag { get; }
	}

	/// <summary>
	/// HasSemantic coded token interface
	/// </summary>
	public interface IHasSemantic : ICodedToken {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int HasSemanticTag { get; }
	}

	/// <summary>
	/// MethodDefOrRef coded token interface
	/// </summary>
	public interface IMethodDefOrRef : ICodedToken {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int MethodDefOrRefTag { get; }
	}

	/// <summary>
	/// MemberForwarded coded token interface
	/// </summary>
	public interface IMemberForwarded : ICodedToken {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int MemberForwardedTag { get; }
	}

	/// <summary>
	/// Implementation coded token interface
	/// </summary>
	public interface IImplementation : ICodedToken {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int ImplementationTag { get; }
	}

	/// <summary>
	/// CustomAttributeType coded token interface
	/// </summary>
	public interface ICustomAttributeType : ICodedToken {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int CustomAttributeTypeTag { get; }
	}

	/// <summary>
	/// ResolutionScope coded token interface
	/// </summary>
	public interface IResolutionScope : ICodedToken {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int ResolutionScopeTag { get; }
	}

	/// <summary>
	/// TypeOrMethodDef coded token interface
	/// </summary>
	public interface ITypeOrMethodDef : ICodedToken {
		/// <summary>
		/// The coded token tag
		/// </summary>
		int TypeOrMethodDefTag { get; }
	}
}
