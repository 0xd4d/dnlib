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
