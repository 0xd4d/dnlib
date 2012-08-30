using System;

namespace dot10.dotNET {
	/// <summary>
	/// Contains all possible coded token classes
	/// </summary>
	public static class CodedToken {
		/// <summary>TypeDefOrRef coded token</summary>
		public static readonly CodedTokenInfo TypeDefOrRef = new CodedTokenInfo(2, new Table[3] {
			Table.TypeDef, Table.TypeRef, Table.TypeSpec,
		});

		/// <summary>HasConstant coded token</summary>
		public static readonly CodedTokenInfo HasConstant = new CodedTokenInfo(2, new Table[3] {
			Table.Field, Table.Param, Table.Property,
		});

		/// <summary>HasCustomAttribute coded token</summary>
		public static readonly CodedTokenInfo HasCustomAttribute = new CodedTokenInfo(5, new Table[24] {
			Table.Method, Table.Field, Table.TypeRef, Table.TypeDef,
			Table.Param, Table.InterfaceImpl, Table.MemberRef, Table.Module,
			Table.DeclSecurity, Table.Property, Table.Event, Table.StandAloneSig,
			Table.ModuleRef, Table.TypeSpec, Table.Assembly, Table.AssemblyRef,
			Table.File, Table.ExportedType, Table.ManifestResource, Table.GenericParam,
			Table.GenericParamConstraint, Table.MethodSpec, 0, 0,
		});

		/// <summary>HasFieldMarshal coded token</summary>
		public static readonly CodedTokenInfo HasFieldMarshal = new CodedTokenInfo(1, new Table[2] {
			Table.Field, Table.Param,
		});

		/// <summary>HasDeclSecurity coded token</summary>
		public static readonly CodedTokenInfo HasDeclSecurity = new CodedTokenInfo(2, new Table[3] {
			Table.TypeDef, Table.Method, Table.Assembly,
		});

		/// <summary>MemberRefParent coded token</summary>
		public static readonly CodedTokenInfo MemberRefParent = new CodedTokenInfo(3, new Table[5] {
			Table.TypeDef, Table.TypeRef, Table.ModuleRef, Table.Method,
			Table.TypeSpec,
		});

		/// <summary>HasSemantic coded token</summary>
		public static readonly CodedTokenInfo HasSemantic = new CodedTokenInfo(1, new Table[2] {
			Table.Event, Table.Property,
		});

		/// <summary>MethodDefOrRef coded token</summary>
		public static readonly CodedTokenInfo MethodDefOrRef = new CodedTokenInfo(1, new Table[2] {
			Table.Method, Table.MemberRef,
		});

		/// <summary>MemberForwarded coded token</summary>
		public static readonly CodedTokenInfo MemberForwarded = new CodedTokenInfo(1, new Table[2] {
			Table.Field, Table.Method,
		});

		/// <summary>Implementation coded token</summary>
		public static readonly CodedTokenInfo Implementation = new CodedTokenInfo(2, new Table[3] {
			Table.File, Table.AssemblyRef, Table.ExportedType,
		});

		/// <summary>CustomAttributeType coded token</summary>
		public static readonly CodedTokenInfo CustomAttributeType = new CodedTokenInfo(3, new Table[4] {
			0, 0, Table.Method, Table.MemberRef,
		});

		/// <summary>ResolutionScope coded token</summary>
		public static readonly CodedTokenInfo ResolutionScope = new CodedTokenInfo(2, new Table[4] {
			Table.Module, Table.ModuleRef, Table.AssemblyRef, Table.TypeRef,
		});

		/// <summary>TypeOrMethodDef coded token</summary>
		public static readonly CodedTokenInfo TypeOrMethodDef = new CodedTokenInfo(1, new Table[2] {
			Table.TypeDef, Table.Method,
		});
	}
}
