// dnlib: See LICENSE.txt for more info

﻿using dnlib.DotNet.MD;

namespace dnlib.DotNet.Emit {
	/// <summary>
	/// CIL opcode operand type
	/// </summary>
	public enum OperandType : byte {
		/// <summary>4-byte relative instruction offset</summary>
		InlineBrTarget,
		/// <summary>4-byte field token (<see cref="Table.Field"/> or <see cref="Table.MemberRef"/>)</summary>
		InlineField,
		/// <summary>int32</summary>
		InlineI,
		/// <summary>int64</summary>
		InlineI8,
		/// <summary>4-byte method token (<see cref="Table.Method"/>, <see cref="Table.MemberRef"/>
		/// or <see cref="Table.MethodSpec"/>)</summary>
		InlineMethod,
		/// <summary>No operand</summary>
		InlineNone,
		/// <summary>Never used</summary>
		InlinePhi,
		/// <summary>64-bit real</summary>
		InlineR,
		/// <summary/>
		NOT_USED_8,
		/// <summary>4-byte method sig token (<see cref="Table.StandAloneSig"/>)</summary>
		InlineSig,
		/// <summary>4-byte string token (<c>0x70xxxxxx</c>)</summary>
		InlineString,
		/// <summary>4-byte count N followed by N 4-byte relative instruction offsets</summary>
		InlineSwitch,
		/// <summary>4-byte token (<see cref="Table.Field"/>, <see cref="Table.MemberRef"/>,
		/// <see cref="Table.Method"/>, <see cref="Table.MethodSpec"/>, <see cref="Table.TypeDef"/>,
		/// <see cref="Table.TypeRef"/> or <see cref="Table.TypeSpec"/>)</summary>
		InlineTok,
		/// <summary>4-byte type token (<see cref="Table.TypeDef"/>, <see cref="Table.TypeRef"/> or
		/// <see cref="Table.TypeSpec"/>)</summary>
		InlineType,
		/// <summary>2-byte param/local index</summary>
		InlineVar,
		/// <summary>1-byte relative instruction offset</summary>
		ShortInlineBrTarget,
		/// <summary>1-byte sbyte (<see cref="Code.Ldc_I4_S"/>) or byte (the rest)</summary>
		ShortInlineI,
		/// <summary>32-bit real</summary>
		ShortInlineR,
		/// <summary>1-byte param/local index</summary>
		ShortInlineVar,
	}
}
