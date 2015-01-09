// dnlib: See LICENSE.txt for more info

﻿namespace dnlib.DotNet.MD {
	/// <summary>
	/// The metadata tables
	/// </summary>
	public enum Table : byte {
		/// <summary>Module table (00h)</summary>
		Module,
		/// <summary>TypeRef table (01h)</summary>
		TypeRef,
		/// <summary>TypeDef table (02h)</summary>
		TypeDef,
		/// <summary>FieldPtr table (03h)</summary>
		FieldPtr,
		/// <summary>Field table (04h)</summary>
		Field,
		/// <summary>MethodPtr table (05h)</summary>
		MethodPtr,
		/// <summary>Method table (06h)</summary>
		Method,
		/// <summary>ParamPtr table (07h)</summary>
		ParamPtr,
		/// <summary>Param table (08h)</summary>
		Param,
		/// <summary>InterfaceImpl table (09h)</summary>
		InterfaceImpl,
		/// <summary>MemberRef table (0Ah)</summary>
		MemberRef,
		/// <summary>Constant table (0Bh)</summary>
		Constant,
		/// <summary>CustomAttribute table (0Ch)</summary>
		CustomAttribute,
		/// <summary>FieldMarshal table (0Dh)</summary>
		FieldMarshal,
		/// <summary>DeclSecurity table (0Eh)</summary>
		DeclSecurity,
		/// <summary>ClassLayout table (0Fh)</summary>
		ClassLayout,
		/// <summary>FieldLayout table (10h)</summary>
		FieldLayout,
		/// <summary>StandAloneSig table (11h)</summary>
		StandAloneSig,
		/// <summary>EventMap table (12h)</summary>
		EventMap,
		/// <summary>EventPtr table (13h)</summary>
		EventPtr,
		/// <summary>Event table (14h)</summary>
		Event,
		/// <summary>PropertyMap table (15h)</summary>
		PropertyMap,
		/// <summary>PropertyPtr table (16h)</summary>
		PropertyPtr,
		/// <summary>Property table (17h)</summary>
		Property,
		/// <summary>MethodSemantics table (18h)</summary>
		MethodSemantics,
		/// <summary>MethodImpl table (19h)</summary>
		MethodImpl,
		/// <summary>ModuleRef table (1Ah)</summary>
		ModuleRef,
		/// <summary>TypeSpec table (1Bh)</summary>
		TypeSpec,
		/// <summary>ImplMap table (1Ch)</summary>
		ImplMap,
		/// <summary>FieldRVA table (1Dh)</summary>
		FieldRVA,
		/// <summary>ENCLog table (1Eh)</summary>
		ENCLog,
		/// <summary>ENCMap table (1Fh)</summary>
		ENCMap,
		/// <summary>Assembly table (20h)</summary>
		Assembly,
		/// <summary>AssemblyProcessor table (21h)</summary>
		AssemblyProcessor,
		/// <summary>AssemblyOS table (22h)</summary>
		AssemblyOS,
		/// <summary>AssemblyRef table (23h)</summary>
		AssemblyRef,
		/// <summary>AssemblyRefProcessor table (24h)</summary>
		AssemblyRefProcessor,
		/// <summary>AssemblyRefOS table (25h)</summary>
		AssemblyRefOS,
		/// <summary>File table (26h)</summary>
		File,
		/// <summary>ExportedType table (27h)</summary>
		ExportedType,
		/// <summary>ManifestResource table (28h)</summary>
		ManifestResource,
		/// <summary>NestedClass table (29h)</summary>
		NestedClass,
		/// <summary>GenericParam table (2Ah)</summary>
		GenericParam,
		/// <summary>MethodSpec table (2Bh)</summary>
		MethodSpec,
		/// <summary>GenericParamConstraint table (2Ch)</summary>
		GenericParamConstraint,
	}
}
