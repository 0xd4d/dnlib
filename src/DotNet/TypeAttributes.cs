// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Diagnostics;
using System.Text;

namespace dnlib.DotNet {
	/// <summary>
	/// TypeDef and ExportedType flags. See CorHdr.h/CorTypeAttr
	/// </summary>
	[Flags, DebuggerDisplay("{Extensions.ToString(this),nq}")]
	public enum TypeAttributes : uint {
		/// <summary>Use this mask to retrieve the type visibility information.</summary>
		VisibilityMask			= 0x00000007,
		/// <summary>Class is not public scope.</summary>
		NotPublic				= 0x00000000,
		/// <summary>Class is public scope.</summary>
		Public					= 0x00000001,
		/// <summary>Class is nested with public visibility.</summary>
		NestedPublic			= 0x00000002,
		/// <summary>Class is nested with private visibility.</summary>
		NestedPrivate			= 0x00000003,
		/// <summary>Class is nested with family visibility.</summary>
		NestedFamily			= 0x00000004,
		/// <summary>Class is nested with assembly visibility.</summary>
		NestedAssembly			= 0x00000005,
		/// <summary>Class is nested with family and assembly visibility.</summary>
		NestedFamANDAssem		= 0x00000006,
		/// <summary>Class is nested with family or assembly visibility.</summary>
		NestedFamORAssem		= 0x00000007,

		/// <summary>Use this mask to retrieve class layout information</summary>
		LayoutMask				= 0x00000018,
		/// <summary>Class fields are auto-laid out</summary>
		AutoLayout				= 0x00000000,
		/// <summary>Class fields are laid out sequentially</summary>
		SequentialLayout		= 0x00000008,
		/// <summary>Layout is supplied explicitly</summary>
		ExplicitLayout			= 0x00000010,

		/// <summary>Use this mask to retrieve class semantics information.</summary>
		ClassSemanticsMask		= 0x00000020,
		/// <summary>Use this mask to retrieve class semantics information.</summary>
		ClassSemanticMask		= ClassSemanticsMask,
		/// <summary>Type is a class.</summary>
		Class					= 0x00000000,
		/// <summary>Type is an interface.</summary>
		Interface				= 0x00000020,

		/// <summary>Class is abstract</summary>
		Abstract				= 0x00000080,
		/// <summary>Class is concrete and may not be extended</summary>
		Sealed					= 0x00000100,
		/// <summary>Class name is special.  Name describes how.</summary>
		SpecialName				= 0x00000400,

		/// <summary>Class / interface is imported</summary>
		Import					= 0x00001000,
		/// <summary>The class is Serializable.</summary>
		Serializable			= 0x00002000,
		/// <summary>The type is a Windows Runtime type</summary>
		WindowsRuntime			= 0x00004000,

		/// <summary>Use StringFormatMask to retrieve string information for native interop</summary>
		StringFormatMask		= 0x00030000,
		/// <summary>LPTSTR is interpreted as ANSI in this class</summary>
		AnsiClass				= 0x00000000,
		/// <summary>LPTSTR is interpreted as UNICODE</summary>
		UnicodeClass			= 0x00010000,
		/// <summary>LPTSTR is interpreted automatically</summary>
		AutoClass				= 0x00020000,
		/// <summary>A non-standard encoding specified by CustomFormatMask</summary>
		CustomFormatClass		= 0x00030000,
		/// <summary>Use this mask to retrieve non-standard encoding information for native interop. The meaning of the values of these 2 bits is unspecified.</summary>
		CustomFormatMask		= 0x00C00000,

		/// <summary>Initialize the class any time before first static field access.</summary>
		BeforeFieldInit			= 0x00100000,
		/// <summary>This ExportedType is a type forwarder.</summary>
		Forwarder				= 0x00200000,

		/// <summary>Flags reserved for runtime use.</summary>
		ReservedMask			= 0x00040800,
		/// <summary>Runtime should check name encoding.</summary>
		RTSpecialName			= 0x00000800,
		/// <summary>Class has security associate with it.</summary>
		HasSecurity				= 0x00040000,
	}

	public static partial class Extensions {
		internal static string ToString(TypeAttributes flags) {
			var sb = new StringBuilder();

			switch ((flags & TypeAttributes.VisibilityMask)) {
			case TypeAttributes.NotPublic: sb.Append("NotPublic"); break;
			case TypeAttributes.Public: sb.Append("Public"); break;
			case TypeAttributes.NestedPublic: sb.Append("NestedPublic"); break;
			case TypeAttributes.NestedPrivate: sb.Append("NestedPrivate"); break;
			case TypeAttributes.NestedFamily: sb.Append("NestedFamily"); break;
			case TypeAttributes.NestedAssembly: sb.Append("NestedAssembly"); break;
			case TypeAttributes.NestedFamANDAssem: sb.Append("NestedFamANDAssem"); break;
			case TypeAttributes.NestedFamORAssem: sb.Append("NestedFamORAssem"); break;
			}

			switch ((flags & TypeAttributes.LayoutMask)) {
			case TypeAttributes.AutoLayout: sb.Append(" | AutoLayout"); break;
			case TypeAttributes.SequentialLayout: sb.Append(" | SequentialLayout"); break;
			case TypeAttributes.ExplicitLayout: sb.Append(" | ExplicitLayout"); break;
			default: sb.Append(" | Layout_UNKNOWN"); break;
			}

			if ((flags & TypeAttributes.Interface) != 0)
				sb.Append(" | Interface");
			else
				sb.Append(" | Class");

			if ((flags & TypeAttributes.Abstract) != 0)
				sb.Append(" | Abstract");

			if ((flags & TypeAttributes.Sealed) != 0)
				sb.Append(" | Sealed");

			if ((flags & TypeAttributes.SpecialName) != 0)
				sb.Append(" | SpecialName");

			if ((flags & TypeAttributes.Import) != 0)
				sb.Append(" | Import");

			if ((flags & TypeAttributes.Serializable) != 0)
				sb.Append(" | Serializable");

			if ((flags & TypeAttributes.WindowsRuntime) != 0)
				sb.Append(" | WindowsRuntime");

			switch ((flags & TypeAttributes.StringFormatMask)) {
			case TypeAttributes.AnsiClass: sb.Append(" | AnsiClass"); break;
			case TypeAttributes.UnicodeClass: sb.Append(" | UnicodeClass"); break;
			case TypeAttributes.AutoClass: sb.Append(" | AutoClass"); break;
			case TypeAttributes.CustomFormatClass: sb.Append(" | CustomFormatClass"); break;
			}

			if ((flags & TypeAttributes.BeforeFieldInit) != 0)
				sb.Append(" | BeforeFieldInit");

			if ((flags & TypeAttributes.Forwarder) != 0)
				sb.Append(" | Forwarder");

			if ((flags & TypeAttributes.RTSpecialName) != 0)
				sb.Append(" | RTSpecialName");

			if ((flags & TypeAttributes.HasSecurity) != 0)
				sb.Append(" | HasSecurity");

			return sb.ToString();
		}
	}
}
