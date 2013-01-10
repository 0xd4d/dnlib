/*
    Copyright (C) 2012-2013 de4dot@gmail.com

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
