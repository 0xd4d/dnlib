// dnlib: See LICENSE.txt for more info

﻿namespace dnlib.DotNet.Emit {
	/// <summary>
	/// Contains all valid CIL opcodes
	/// </summary>
	public static class OpCodes {
		/// <summary>
		/// All one-byte opcodes
		/// </summary>
		public static readonly OpCode[] OneByteOpCodes = new OpCode[0x100];

		/// <summary>
		/// All two-byte opcodes (first byte is <c>0xFE</c>)
		/// </summary>
		public static readonly OpCode[] TwoByteOpCodes = new OpCode[0x100];

#pragma warning disable 1591	// disable XML doc warning
		public static readonly OpCode UNKNOWN1		= new OpCode("UNKNOWN1",		Code.UNKNOWN1, OperandType.InlineNone, FlowControl.Meta, OpCodeType.Nternal, StackBehaviour.Push0, StackBehaviour.Pop0);
		public static readonly OpCode UNKNOWN2		= new OpCode("UNKNOWN2",		Code.UNKNOWN2, OperandType.InlineNone, FlowControl.Meta, OpCodeType.Nternal, StackBehaviour.Push0, StackBehaviour.Pop0);
		public static readonly OpCode Nop			= new OpCode("nop",				Code.Nop, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push0, StackBehaviour.Pop0);
		public static readonly OpCode Break			= new OpCode("break",			Code.Break, OperandType.InlineNone, FlowControl.Break, OpCodeType.Primitive, StackBehaviour.Push0, StackBehaviour.Pop0);
		public static readonly OpCode Ldarg_0		= new OpCode("ldarg.0",			Code.Ldarg_0, OperandType.InlineNone, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Push1, StackBehaviour.Pop0);
		public static readonly OpCode Ldarg_1		= new OpCode("ldarg.1",			Code.Ldarg_1, OperandType.InlineNone, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Push1, StackBehaviour.Pop0);
		public static readonly OpCode Ldarg_2		= new OpCode("ldarg.2",			Code.Ldarg_2, OperandType.InlineNone, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Push1, StackBehaviour.Pop0);
		public static readonly OpCode Ldarg_3		= new OpCode("ldarg.3",			Code.Ldarg_3, OperandType.InlineNone, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Push1, StackBehaviour.Pop0);
		public static readonly OpCode Ldloc_0		= new OpCode("ldloc.0",			Code.Ldloc_0, OperandType.InlineNone, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Push1, StackBehaviour.Pop0);
		public static readonly OpCode Ldloc_1		= new OpCode("ldloc.1",			Code.Ldloc_1, OperandType.InlineNone, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Push1, StackBehaviour.Pop0);
		public static readonly OpCode Ldloc_2		= new OpCode("ldloc.2",			Code.Ldloc_2, OperandType.InlineNone, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Push1, StackBehaviour.Pop0);
		public static readonly OpCode Ldloc_3		= new OpCode("ldloc.3",			Code.Ldloc_3, OperandType.InlineNone, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Push1, StackBehaviour.Pop0);
		public static readonly OpCode Stloc_0		= new OpCode("stloc.0",			Code.Stloc_0, OperandType.InlineNone, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Pop1);
		public static readonly OpCode Stloc_1		= new OpCode("stloc.1",			Code.Stloc_1, OperandType.InlineNone, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Pop1);
		public static readonly OpCode Stloc_2		= new OpCode("stloc.2",			Code.Stloc_2, OperandType.InlineNone, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Pop1);
		public static readonly OpCode Stloc_3		= new OpCode("stloc.3",			Code.Stloc_3, OperandType.InlineNone, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Pop1);
		public static readonly OpCode Ldarg_S		= new OpCode("ldarg.s",			Code.Ldarg_S, OperandType.ShortInlineVar, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Push1, StackBehaviour.Pop0);
		public static readonly OpCode Ldarga_S		= new OpCode("ldarga.s",		Code.Ldarga_S, OperandType.ShortInlineVar, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Pushi, StackBehaviour.Pop0);
		public static readonly OpCode Starg_S		= new OpCode("starg.s",			Code.Starg_S, OperandType.ShortInlineVar, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Pop1);
		public static readonly OpCode Ldloc_S		= new OpCode("ldloc.s",			Code.Ldloc_S, OperandType.ShortInlineVar, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Push1, StackBehaviour.Pop0);
		public static readonly OpCode Ldloca_S		= new OpCode("ldloca.s",		Code.Ldloca_S, OperandType.ShortInlineVar, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Pushi, StackBehaviour.Pop0);
		public static readonly OpCode Stloc_S		= new OpCode("stloc.s",			Code.Stloc_S, OperandType.ShortInlineVar, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Pop1);
		public static readonly OpCode Ldnull		= new OpCode("ldnull",			Code.Ldnull, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushref, StackBehaviour.Pop0);
		public static readonly OpCode Ldc_I4_M1		= new OpCode("ldc.i4.m1",		Code.Ldc_I4_M1, OperandType.InlineNone, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Pushi, StackBehaviour.Pop0);
		public static readonly OpCode Ldc_I4_0		= new OpCode("ldc.i4.0",		Code.Ldc_I4_0, OperandType.InlineNone, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Pushi, StackBehaviour.Pop0);
		public static readonly OpCode Ldc_I4_1		= new OpCode("ldc.i4.1",		Code.Ldc_I4_1, OperandType.InlineNone, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Pushi, StackBehaviour.Pop0);
		public static readonly OpCode Ldc_I4_2		= new OpCode("ldc.i4.2",		Code.Ldc_I4_2, OperandType.InlineNone, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Pushi, StackBehaviour.Pop0);
		public static readonly OpCode Ldc_I4_3		= new OpCode("ldc.i4.3",		Code.Ldc_I4_3, OperandType.InlineNone, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Pushi, StackBehaviour.Pop0);
		public static readonly OpCode Ldc_I4_4		= new OpCode("ldc.i4.4",		Code.Ldc_I4_4, OperandType.InlineNone, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Pushi, StackBehaviour.Pop0);
		public static readonly OpCode Ldc_I4_5		= new OpCode("ldc.i4.5",		Code.Ldc_I4_5, OperandType.InlineNone, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Pushi, StackBehaviour.Pop0);
		public static readonly OpCode Ldc_I4_6		= new OpCode("ldc.i4.6",		Code.Ldc_I4_6, OperandType.InlineNone, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Pushi, StackBehaviour.Pop0);
		public static readonly OpCode Ldc_I4_7		= new OpCode("ldc.i4.7",		Code.Ldc_I4_7, OperandType.InlineNone, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Pushi, StackBehaviour.Pop0);
		public static readonly OpCode Ldc_I4_8		= new OpCode("ldc.i4.8",		Code.Ldc_I4_8, OperandType.InlineNone, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Pushi, StackBehaviour.Pop0);
		public static readonly OpCode Ldc_I4_S		= new OpCode("ldc.i4.s",		Code.Ldc_I4_S, OperandType.ShortInlineI, FlowControl.Next, OpCodeType.Macro, StackBehaviour.Pushi, StackBehaviour.Pop0);
		public static readonly OpCode Ldc_I4		= new OpCode("ldc.i4",			Code.Ldc_I4, OperandType.InlineI, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop0);
		public static readonly OpCode Ldc_I8		= new OpCode("ldc.i8",			Code.Ldc_I8, OperandType.InlineI8, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi8, StackBehaviour.Pop0);
		public static readonly OpCode Ldc_R4		= new OpCode("ldc.r4",			Code.Ldc_R4, OperandType.ShortInlineR, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushr4, StackBehaviour.Pop0);
		public static readonly OpCode Ldc_R8		= new OpCode("ldc.r8",			Code.Ldc_R8, OperandType.InlineR, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushr8, StackBehaviour.Pop0);
		public static readonly OpCode Dup			= new OpCode("dup",				Code.Dup, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push1_push1, StackBehaviour.Pop1);
		public static readonly OpCode Pop			= new OpCode("pop",				Code.Pop, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push0, StackBehaviour.Pop1);
		public static readonly OpCode Jmp			= new OpCode("jmp",				Code.Jmp, OperandType.InlineMethod, FlowControl.Call, OpCodeType.Primitive, StackBehaviour.Push0, StackBehaviour.Pop0);
		public static readonly OpCode Call			= new OpCode("call",			Code.Call, OperandType.InlineMethod, FlowControl.Call, OpCodeType.Primitive, StackBehaviour.Varpush, StackBehaviour.Varpop);
		public static readonly OpCode Calli			= new OpCode("calli",			Code.Calli, OperandType.InlineSig, FlowControl.Call, OpCodeType.Primitive, StackBehaviour.Varpush, StackBehaviour.Varpop);
		public static readonly OpCode Ret			= new OpCode("ret",				Code.Ret, OperandType.InlineNone, FlowControl.Return, OpCodeType.Primitive, StackBehaviour.Push0, StackBehaviour.Varpop);
		public static readonly OpCode Br_S			= new OpCode("br.s",			Code.Br_S, OperandType.ShortInlineBrTarget, FlowControl.Branch, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Pop0);
		public static readonly OpCode Brfalse_S		= new OpCode("brfalse.s",		Code.Brfalse_S, OperandType.ShortInlineBrTarget, FlowControl.Cond_Branch, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Popi);
		public static readonly OpCode Brtrue_S		= new OpCode("brtrue.s",		Code.Brtrue_S, OperandType.ShortInlineBrTarget, FlowControl.Cond_Branch, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Popi);
		public static readonly OpCode Beq_S			= new OpCode("beq.s",			Code.Beq_S, OperandType.ShortInlineBrTarget, FlowControl.Cond_Branch, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Bge_S			= new OpCode("bge.s",			Code.Bge_S, OperandType.ShortInlineBrTarget, FlowControl.Cond_Branch, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Bgt_S			= new OpCode("bgt.s",			Code.Bgt_S, OperandType.ShortInlineBrTarget, FlowControl.Cond_Branch, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Ble_S			= new OpCode("ble.s",			Code.Ble_S, OperandType.ShortInlineBrTarget, FlowControl.Cond_Branch, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Blt_S			= new OpCode("blt.s",			Code.Blt_S, OperandType.ShortInlineBrTarget, FlowControl.Cond_Branch, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Bne_Un_S		= new OpCode("bne.un.s",		Code.Bne_Un_S, OperandType.ShortInlineBrTarget, FlowControl.Cond_Branch, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Bge_Un_S		= new OpCode("bge.un.s",		Code.Bge_Un_S, OperandType.ShortInlineBrTarget, FlowControl.Cond_Branch, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Bgt_Un_S		= new OpCode("bgt.un.s",		Code.Bgt_Un_S, OperandType.ShortInlineBrTarget, FlowControl.Cond_Branch, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Ble_Un_S		= new OpCode("ble.un.s",		Code.Ble_Un_S, OperandType.ShortInlineBrTarget, FlowControl.Cond_Branch, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Blt_Un_S		= new OpCode("blt.un.s",		Code.Blt_Un_S, OperandType.ShortInlineBrTarget, FlowControl.Cond_Branch, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Br			= new OpCode("br",				Code.Br, OperandType.InlineBrTarget, FlowControl.Branch, OpCodeType.Primitive, StackBehaviour.Push0, StackBehaviour.Pop0);
		public static readonly OpCode Brfalse		= new OpCode("brfalse",			Code.Brfalse, OperandType.InlineBrTarget, FlowControl.Cond_Branch, OpCodeType.Primitive, StackBehaviour.Push0, StackBehaviour.Popi);
		public static readonly OpCode Brtrue		= new OpCode("brtrue",			Code.Brtrue, OperandType.InlineBrTarget, FlowControl.Cond_Branch, OpCodeType.Primitive, StackBehaviour.Push0, StackBehaviour.Popi);
		public static readonly OpCode Beq			= new OpCode("beq",				Code.Beq, OperandType.InlineBrTarget, FlowControl.Cond_Branch, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Bge			= new OpCode("bge",				Code.Bge, OperandType.InlineBrTarget, FlowControl.Cond_Branch, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Bgt			= new OpCode("bgt",				Code.Bgt, OperandType.InlineBrTarget, FlowControl.Cond_Branch, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Ble			= new OpCode("ble",				Code.Ble, OperandType.InlineBrTarget, FlowControl.Cond_Branch, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Blt			= new OpCode("blt",				Code.Blt, OperandType.InlineBrTarget, FlowControl.Cond_Branch, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Bne_Un		= new OpCode("bne.un",			Code.Bne_Un, OperandType.InlineBrTarget, FlowControl.Cond_Branch, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Bge_Un		= new OpCode("bge.un",			Code.Bge_Un, OperandType.InlineBrTarget, FlowControl.Cond_Branch, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Bgt_Un		= new OpCode("bgt.un",			Code.Bgt_Un, OperandType.InlineBrTarget, FlowControl.Cond_Branch, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Ble_Un		= new OpCode("ble.un",			Code.Ble_Un, OperandType.InlineBrTarget, FlowControl.Cond_Branch, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Blt_Un		= new OpCode("blt.un",			Code.Blt_Un, OperandType.InlineBrTarget, FlowControl.Cond_Branch, OpCodeType.Macro, StackBehaviour.Push0, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Switch		= new OpCode("switch",			Code.Switch, OperandType.InlineSwitch, FlowControl.Cond_Branch, OpCodeType.Primitive, StackBehaviour.Push0, StackBehaviour.Popi);
		public static readonly OpCode Ldind_I1		= new OpCode("ldind.i1",		Code.Ldind_I1, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Popi);
		public static readonly OpCode Ldind_U1		= new OpCode("ldind.u1",		Code.Ldind_U1, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Popi);
		public static readonly OpCode Ldind_I2		= new OpCode("ldind.i2",		Code.Ldind_I2, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Popi);
		public static readonly OpCode Ldind_U2		= new OpCode("ldind.u2",		Code.Ldind_U2, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Popi);
		public static readonly OpCode Ldind_I4		= new OpCode("ldind.i4",		Code.Ldind_I4, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Popi);
		public static readonly OpCode Ldind_U4		= new OpCode("ldind.u4",		Code.Ldind_U4, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Popi);
		public static readonly OpCode Ldind_I8		= new OpCode("ldind.i8",		Code.Ldind_I8, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi8, StackBehaviour.Popi);
		public static readonly OpCode Ldind_I		= new OpCode("ldind.i",			Code.Ldind_I, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Popi);
		public static readonly OpCode Ldind_R4		= new OpCode("ldind.r4",		Code.Ldind_R4, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushr4, StackBehaviour.Popi);
		public static readonly OpCode Ldind_R8		= new OpCode("ldind.r8",		Code.Ldind_R8, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushr8, StackBehaviour.Popi);
		public static readonly OpCode Ldind_Ref		= new OpCode("ldind.ref",		Code.Ldind_Ref, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushref, StackBehaviour.Popi);
		public static readonly OpCode Stind_Ref		= new OpCode("stind.ref",		Code.Stind_Ref, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push0, StackBehaviour.Popi_popi);
		public static readonly OpCode Stind_I1		= new OpCode("stind.i1",		Code.Stind_I1, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push0, StackBehaviour.Popi_popi);
		public static readonly OpCode Stind_I2		= new OpCode("stind.i2",		Code.Stind_I2, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push0, StackBehaviour.Popi_popi);
		public static readonly OpCode Stind_I4		= new OpCode("stind.i4",		Code.Stind_I4, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push0, StackBehaviour.Popi_popi);
		public static readonly OpCode Stind_I8		= new OpCode("stind.i8",		Code.Stind_I8, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push0, StackBehaviour.Popi_popi8);
		public static readonly OpCode Stind_R4		= new OpCode("stind.r4",		Code.Stind_R4, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push0, StackBehaviour.Popi_popr4);
		public static readonly OpCode Stind_R8		= new OpCode("stind.r8",		Code.Stind_R8, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push0, StackBehaviour.Popi_popr8);
		public static readonly OpCode Add			= new OpCode("add",				Code.Add, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push1, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Sub			= new OpCode("sub",				Code.Sub, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push1, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Mul			= new OpCode("mul",				Code.Mul, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push1, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Div			= new OpCode("div",				Code.Div, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push1, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Div_Un		= new OpCode("div.un",			Code.Div_Un, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push1, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Rem			= new OpCode("rem",				Code.Rem, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push1, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Rem_Un		= new OpCode("rem.un",			Code.Rem_Un, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push1, StackBehaviour.Pop1_pop1);
		public static readonly OpCode And			= new OpCode("and",				Code.And, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push1, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Or			= new OpCode("or",				Code.Or, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push1, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Xor			= new OpCode("xor",				Code.Xor, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push1, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Shl			= new OpCode("shl",				Code.Shl, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push1, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Shr			= new OpCode("shr",				Code.Shr, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push1, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Shr_Un		= new OpCode("shr.un",			Code.Shr_Un, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push1, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Neg			= new OpCode("neg",				Code.Neg, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push1, StackBehaviour.Pop1);
		public static readonly OpCode Not			= new OpCode("not",				Code.Not, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push1, StackBehaviour.Pop1);
		public static readonly OpCode Conv_I1		= new OpCode("conv.i1",			Code.Conv_I1, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1);
		public static readonly OpCode Conv_I2		= new OpCode("conv.i2",			Code.Conv_I2, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1);
		public static readonly OpCode Conv_I4		= new OpCode("conv.i4",			Code.Conv_I4, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1);
		public static readonly OpCode Conv_I8		= new OpCode("conv.i8",			Code.Conv_I8, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi8, StackBehaviour.Pop1);
		public static readonly OpCode Conv_R4		= new OpCode("conv.r4",			Code.Conv_R4, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushr4, StackBehaviour.Pop1);
		public static readonly OpCode Conv_R8		= new OpCode("conv.r8",			Code.Conv_R8, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushr8, StackBehaviour.Pop1);
		public static readonly OpCode Conv_U4		= new OpCode("conv.u4",			Code.Conv_U4, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1);
		public static readonly OpCode Conv_U8		= new OpCode("conv.u8",			Code.Conv_U8, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi8, StackBehaviour.Pop1);
		public static readonly OpCode Callvirt		= new OpCode("callvirt",		Code.Callvirt, OperandType.InlineMethod, FlowControl.Call, OpCodeType.Objmodel, StackBehaviour.Varpush, StackBehaviour.Varpop);
		public static readonly OpCode Cpobj			= new OpCode("cpobj",			Code.Cpobj, OperandType.InlineType, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Push0, StackBehaviour.Popi_popi);
		public static readonly OpCode Ldobj			= new OpCode("ldobj",			Code.Ldobj, OperandType.InlineType, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Push1, StackBehaviour.Popi);
		public static readonly OpCode Ldstr			= new OpCode("ldstr",			Code.Ldstr, OperandType.InlineString, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Pushref, StackBehaviour.Pop0);
		public static readonly OpCode Newobj		= new OpCode("newobj",			Code.Newobj, OperandType.InlineMethod, FlowControl.Call, OpCodeType.Objmodel, StackBehaviour.Pushref, StackBehaviour.Varpop);
		public static readonly OpCode Castclass		= new OpCode("castclass",		Code.Castclass, OperandType.InlineType, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Pushref, StackBehaviour.Popref);
		public static readonly OpCode Isinst		= new OpCode("isinst",			Code.Isinst, OperandType.InlineType, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Pushi, StackBehaviour.Popref);
		public static readonly OpCode Conv_R_Un		= new OpCode("conv.r.un",		Code.Conv_R_Un, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushr8, StackBehaviour.Pop1);
		public static readonly OpCode Unbox			= new OpCode("unbox",			Code.Unbox, OperandType.InlineType, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Popref);
		public static readonly OpCode Throw			= new OpCode("throw",			Code.Throw, OperandType.InlineNone, FlowControl.Throw, OpCodeType.Objmodel, StackBehaviour.Push0, StackBehaviour.Popref);
		public static readonly OpCode Ldfld			= new OpCode("ldfld",			Code.Ldfld, OperandType.InlineField, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Push1, StackBehaviour.Popref);
		public static readonly OpCode Ldflda		= new OpCode("ldflda",			Code.Ldflda, OperandType.InlineField, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Pushi, StackBehaviour.Popref);
		public static readonly OpCode Stfld			= new OpCode("stfld",			Code.Stfld, OperandType.InlineField, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Push0, StackBehaviour.Popref_pop1);
		public static readonly OpCode Ldsfld		= new OpCode("ldsfld",			Code.Ldsfld, OperandType.InlineField, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Push1, StackBehaviour.Pop0);
		public static readonly OpCode Ldsflda		= new OpCode("ldsflda",			Code.Ldsflda, OperandType.InlineField, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Pushi, StackBehaviour.Pop0);
		public static readonly OpCode Stsfld		= new OpCode("stsfld",			Code.Stsfld, OperandType.InlineField, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Push0, StackBehaviour.Pop1);
		public static readonly OpCode Stobj			= new OpCode("stobj",			Code.Stobj, OperandType.InlineType, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push0, StackBehaviour.Popi_pop1);
		public static readonly OpCode Conv_Ovf_I1_Un= new OpCode("conv.ovf.i1.un",	Code.Conv_Ovf_I1_Un, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1);
		public static readonly OpCode Conv_Ovf_I2_Un= new OpCode("conv.ovf.i2.un",	Code.Conv_Ovf_I2_Un, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1);
		public static readonly OpCode Conv_Ovf_I4_Un= new OpCode("conv.ovf.i4.un",	Code.Conv_Ovf_I4_Un, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1);
		public static readonly OpCode Conv_Ovf_I8_Un= new OpCode("conv.ovf.i8.un",	Code.Conv_Ovf_I8_Un, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi8, StackBehaviour.Pop1);
		public static readonly OpCode Conv_Ovf_U1_Un= new OpCode("conv.ovf.u1.un",	Code.Conv_Ovf_U1_Un, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1);
		public static readonly OpCode Conv_Ovf_U2_Un= new OpCode("conv.ovf.u2.un",	Code.Conv_Ovf_U2_Un, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1);
		public static readonly OpCode Conv_Ovf_U4_Un= new OpCode("conv.ovf.u4.un",	Code.Conv_Ovf_U4_Un, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1);
		public static readonly OpCode Conv_Ovf_U8_Un= new OpCode("conv.ovf.u8.un",	Code.Conv_Ovf_U8_Un, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi8, StackBehaviour.Pop1);
		public static readonly OpCode Conv_Ovf_I_Un	= new OpCode("conv.ovf.i.un",	Code.Conv_Ovf_I_Un, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1);
		public static readonly OpCode Conv_Ovf_U_Un	= new OpCode("conv.ovf.u.un",	Code.Conv_Ovf_U_Un, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1);
		public static readonly OpCode Box			= new OpCode("box",				Code.Box, OperandType.InlineType, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushref, StackBehaviour.Pop1);
		public static readonly OpCode Newarr		= new OpCode("newarr",			Code.Newarr, OperandType.InlineType, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Pushref, StackBehaviour.Popi);
		public static readonly OpCode Ldlen			= new OpCode("ldlen",			Code.Ldlen, OperandType.InlineNone, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Pushi, StackBehaviour.Popref);
		public static readonly OpCode Ldelema		= new OpCode("ldelema",			Code.Ldelema, OperandType.InlineType, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Pushi, StackBehaviour.Popref_popi);
		public static readonly OpCode Ldelem_I1		= new OpCode("ldelem.i1",		Code.Ldelem_I1, OperandType.InlineNone, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Pushi, StackBehaviour.Popref_popi);
		public static readonly OpCode Ldelem_U1		= new OpCode("ldelem.u1",		Code.Ldelem_U1, OperandType.InlineNone, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Pushi, StackBehaviour.Popref_popi);
		public static readonly OpCode Ldelem_I2		= new OpCode("ldelem.i2",		Code.Ldelem_I2, OperandType.InlineNone, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Pushi, StackBehaviour.Popref_popi);
		public static readonly OpCode Ldelem_U2		= new OpCode("ldelem.u2",		Code.Ldelem_U2, OperandType.InlineNone, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Pushi, StackBehaviour.Popref_popi);
		public static readonly OpCode Ldelem_I4		= new OpCode("ldelem.i4",		Code.Ldelem_I4, OperandType.InlineNone, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Pushi, StackBehaviour.Popref_popi);
		public static readonly OpCode Ldelem_U4		= new OpCode("ldelem.u4",		Code.Ldelem_U4, OperandType.InlineNone, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Pushi, StackBehaviour.Popref_popi);
		public static readonly OpCode Ldelem_I8		= new OpCode("ldelem.i8",		Code.Ldelem_I8, OperandType.InlineNone, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Pushi8, StackBehaviour.Popref_popi);
		public static readonly OpCode Ldelem_I		= new OpCode("ldelem.i",		Code.Ldelem_I, OperandType.InlineNone, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Pushi, StackBehaviour.Popref_popi);
		public static readonly OpCode Ldelem_R4		= new OpCode("ldelem.r4",		Code.Ldelem_R4, OperandType.InlineNone, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Pushr4, StackBehaviour.Popref_popi);
		public static readonly OpCode Ldelem_R8		= new OpCode("ldelem.r8",		Code.Ldelem_R8, OperandType.InlineNone, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Pushr8, StackBehaviour.Popref_popi);
		public static readonly OpCode Ldelem_Ref	= new OpCode("ldelem.ref",		Code.Ldelem_Ref, OperandType.InlineNone, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Pushref, StackBehaviour.Popref_popi);
		public static readonly OpCode Stelem_I		= new OpCode("stelem.i",		Code.Stelem_I, OperandType.InlineNone, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Push0, StackBehaviour.Popref_popi_popi);
		public static readonly OpCode Stelem_I1		= new OpCode("stelem.i1",		Code.Stelem_I1, OperandType.InlineNone, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Push0, StackBehaviour.Popref_popi_popi);
		public static readonly OpCode Stelem_I2		= new OpCode("stelem.i2",		Code.Stelem_I2, OperandType.InlineNone, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Push0, StackBehaviour.Popref_popi_popi);
		public static readonly OpCode Stelem_I4		= new OpCode("stelem.i4",		Code.Stelem_I4, OperandType.InlineNone, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Push0, StackBehaviour.Popref_popi_popi);
		public static readonly OpCode Stelem_I8		= new OpCode("stelem.i8",		Code.Stelem_I8, OperandType.InlineNone, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Push0, StackBehaviour.Popref_popi_popi8);
		public static readonly OpCode Stelem_R4		= new OpCode("stelem.r4",		Code.Stelem_R4, OperandType.InlineNone, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Push0, StackBehaviour.Popref_popi_popr4);
		public static readonly OpCode Stelem_R8		= new OpCode("stelem.r8",		Code.Stelem_R8, OperandType.InlineNone, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Push0, StackBehaviour.Popref_popi_popr8);
		public static readonly OpCode Stelem_Ref	= new OpCode("stelem.ref",		Code.Stelem_Ref, OperandType.InlineNone, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Push0, StackBehaviour.Popref_popi_popref);
		public static readonly OpCode Ldelem		= new OpCode("ldelem",			Code.Ldelem, OperandType.InlineType, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Push1, StackBehaviour.Popref_popi);
		public static readonly OpCode Stelem		= new OpCode("stelem",			Code.Stelem, OperandType.InlineType, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Push0, StackBehaviour.Popref_popi_pop1);
		public static readonly OpCode Unbox_Any		= new OpCode("unbox.any",		Code.Unbox_Any, OperandType.InlineType, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Push1, StackBehaviour.Popref);
		public static readonly OpCode Conv_Ovf_I1	= new OpCode("conv.ovf.i1",		Code.Conv_Ovf_I1, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1);
		public static readonly OpCode Conv_Ovf_U1	= new OpCode("conv.ovf.u1",		Code.Conv_Ovf_U1, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1);
		public static readonly OpCode Conv_Ovf_I2	= new OpCode("conv.ovf.i2",		Code.Conv_Ovf_I2, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1);
		public static readonly OpCode Conv_Ovf_U2	= new OpCode("conv.ovf.u2",		Code.Conv_Ovf_U2, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1);
		public static readonly OpCode Conv_Ovf_I4	= new OpCode("conv.ovf.i4",		Code.Conv_Ovf_I4, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1);
		public static readonly OpCode Conv_Ovf_U4	= new OpCode("conv.ovf.u4",		Code.Conv_Ovf_U4, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1);
		public static readonly OpCode Conv_Ovf_I8	= new OpCode("conv.ovf.i8",		Code.Conv_Ovf_I8, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi8, StackBehaviour.Pop1);
		public static readonly OpCode Conv_Ovf_U8	= new OpCode("conv.ovf.u8",		Code.Conv_Ovf_U8, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi8, StackBehaviour.Pop1);
		public static readonly OpCode Refanyval		= new OpCode("refanyval",		Code.Refanyval, OperandType.InlineType, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1);
		public static readonly OpCode Ckfinite		= new OpCode("ckfinite",		Code.Ckfinite, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushr8, StackBehaviour.Pop1);
		public static readonly OpCode Mkrefany		= new OpCode("mkrefany",		Code.Mkrefany, OperandType.InlineType, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push1, StackBehaviour.Popi);
		public static readonly OpCode Ldtoken		= new OpCode("ldtoken",			Code.Ldtoken, OperandType.InlineTok, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop0);
		public static readonly OpCode Conv_U2		= new OpCode("conv.u2",			Code.Conv_U2, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1);
		public static readonly OpCode Conv_U1		= new OpCode("conv.u1",			Code.Conv_U1, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1);
		public static readonly OpCode Conv_I		= new OpCode("conv.i",			Code.Conv_I, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1);
		public static readonly OpCode Conv_Ovf_I	= new OpCode("conv.ovf.i",		Code.Conv_Ovf_I, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1);
		public static readonly OpCode Conv_Ovf_U	= new OpCode("conv.ovf.u",		Code.Conv_Ovf_U, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1);
		public static readonly OpCode Add_Ovf		= new OpCode("add.ovf",			Code.Add_Ovf, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push1, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Add_Ovf_Un	= new OpCode("add.ovf.un",		Code.Add_Ovf_Un, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push1, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Mul_Ovf		= new OpCode("mul.ovf",			Code.Mul_Ovf, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push1, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Mul_Ovf_Un	= new OpCode("mul.ovf.un",		Code.Mul_Ovf_Un, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push1, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Sub_Ovf		= new OpCode("sub.ovf",			Code.Sub_Ovf, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push1, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Sub_Ovf_Un	= new OpCode("sub.ovf.un",		Code.Sub_Ovf_Un, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push1, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Endfinally	= new OpCode("endfinally",		Code.Endfinally, OperandType.InlineNone, FlowControl.Return, OpCodeType.Primitive, StackBehaviour.Push0, StackBehaviour.PopAll);
		public static readonly OpCode Leave			= new OpCode("leave",			Code.Leave, OperandType.InlineBrTarget, FlowControl.Branch, OpCodeType.Primitive, StackBehaviour.Push0, StackBehaviour.PopAll);
		public static readonly OpCode Leave_S		= new OpCode("leave.s",			Code.Leave_S, OperandType.ShortInlineBrTarget, FlowControl.Branch, OpCodeType.Primitive, StackBehaviour.Push0, StackBehaviour.PopAll);
		public static readonly OpCode Stind_I		= new OpCode("stind.i",			Code.Stind_I, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push0, StackBehaviour.Popi_popi);
		public static readonly OpCode Conv_U		= new OpCode("conv.u",			Code.Conv_U, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1);
		public static readonly OpCode Prefix7		= new OpCode("prefix7",			Code.Prefix7, OperandType.InlineNone, FlowControl.Meta, OpCodeType.Nternal, StackBehaviour.Push0, StackBehaviour.Pop0);
		public static readonly OpCode Prefix6		= new OpCode("prefix6",			Code.Prefix6, OperandType.InlineNone, FlowControl.Meta, OpCodeType.Nternal, StackBehaviour.Push0, StackBehaviour.Pop0);
		public static readonly OpCode Prefix5		= new OpCode("prefix5",			Code.Prefix5, OperandType.InlineNone, FlowControl.Meta, OpCodeType.Nternal, StackBehaviour.Push0, StackBehaviour.Pop0);
		public static readonly OpCode Prefix4		= new OpCode("prefix4",			Code.Prefix4, OperandType.InlineNone, FlowControl.Meta, OpCodeType.Nternal, StackBehaviour.Push0, StackBehaviour.Pop0);
		public static readonly OpCode Prefix3		= new OpCode("prefix3",			Code.Prefix3, OperandType.InlineNone, FlowControl.Meta, OpCodeType.Nternal, StackBehaviour.Push0, StackBehaviour.Pop0);
		public static readonly OpCode Prefix2		= new OpCode("prefix2",			Code.Prefix2, OperandType.InlineNone, FlowControl.Meta, OpCodeType.Nternal, StackBehaviour.Push0, StackBehaviour.Pop0);
		public static readonly OpCode Prefix1		= new OpCode("prefix1",			Code.Prefix1, OperandType.InlineNone, FlowControl.Meta, OpCodeType.Nternal, StackBehaviour.Push0, StackBehaviour.Pop0);
		public static readonly OpCode Prefixref		= new OpCode("prefixref",		Code.Prefixref, OperandType.InlineNone, FlowControl.Meta, OpCodeType.Nternal, StackBehaviour.Push0, StackBehaviour.Pop0);
		public static readonly OpCode Arglist		= new OpCode("arglist",			Code.Arglist, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop0);
		public static readonly OpCode Ceq			= new OpCode("ceq",				Code.Ceq, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Cgt			= new OpCode("cgt",				Code.Cgt, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Cgt_Un		= new OpCode("cgt.un",			Code.Cgt_Un, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Clt			= new OpCode("clt",				Code.Clt, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Clt_Un		= new OpCode("clt.un",			Code.Clt_Un, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1_pop1);
		public static readonly OpCode Ldftn			= new OpCode("ldftn",			Code.Ldftn, OperandType.InlineMethod, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop0);
		public static readonly OpCode Ldvirtftn		= new OpCode("ldvirtftn",		Code.Ldvirtftn, OperandType.InlineMethod, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Popref);
		public static readonly OpCode Ldarg			= new OpCode("ldarg",			Code.Ldarg, OperandType.InlineVar, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push1, StackBehaviour.Pop0);
		public static readonly OpCode Ldarga		= new OpCode("ldarga",			Code.Ldarga, OperandType.InlineVar, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop0);
		public static readonly OpCode Starg			= new OpCode("starg",			Code.Starg, OperandType.InlineVar, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push0, StackBehaviour.Pop1);
		public static readonly OpCode Ldloc			= new OpCode("ldloc",			Code.Ldloc, OperandType.InlineVar, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push1, StackBehaviour.Pop0);
		public static readonly OpCode Ldloca		= new OpCode("ldloca",			Code.Ldloca, OperandType.InlineVar, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop0);
		public static readonly OpCode Stloc			= new OpCode("stloc",			Code.Stloc, OperandType.InlineVar, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push0, StackBehaviour.Pop1);
		public static readonly OpCode Localloc		= new OpCode("localloc",		Code.Localloc, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Popi);
		public static readonly OpCode Endfilter		= new OpCode("endfilter",		Code.Endfilter, OperandType.InlineNone, FlowControl.Return, OpCodeType.Primitive, StackBehaviour.Push0, StackBehaviour.Popi);
		public static readonly OpCode Unaligned		= new OpCode("unaligned.",		Code.Unaligned, OperandType.ShortInlineI, FlowControl.Meta, OpCodeType.Prefix, StackBehaviour.Push0, StackBehaviour.Pop0);
		public static readonly OpCode Volatile		= new OpCode("volatile.",		Code.Volatile, OperandType.InlineNone, FlowControl.Meta, OpCodeType.Prefix, StackBehaviour.Push0, StackBehaviour.Pop0);
		public static readonly OpCode Tailcall		= new OpCode("tail.",			Code.Tailcall, OperandType.InlineNone, FlowControl.Meta, OpCodeType.Prefix, StackBehaviour.Push0, StackBehaviour.Pop0);
		public static readonly OpCode Initobj		= new OpCode("initobj",			Code.Initobj, OperandType.InlineType, FlowControl.Next, OpCodeType.Objmodel, StackBehaviour.Push0, StackBehaviour.Popi);
		public static readonly OpCode Constrained	= new OpCode("constrained.",	Code.Constrained, OperandType.InlineType, FlowControl.Meta, OpCodeType.Prefix, StackBehaviour.Push0, StackBehaviour.Pop0);
		public static readonly OpCode Cpblk			= new OpCode("cpblk",			Code.Cpblk, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push0, StackBehaviour.Popi_popi_popi);
		public static readonly OpCode Initblk		= new OpCode("initblk",			Code.Initblk, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Push0, StackBehaviour.Popi_popi_popi);
//		public static readonly OpCode No			= new OpCode("no.",				Code.No, OperandType.ShortInlineI, FlowControl.Meta, OpCodeType.Prefix, StackBehaviour.Push0, StackBehaviour.Pop0);
		public static readonly OpCode Rethrow		= new OpCode("rethrow",			Code.Rethrow, OperandType.InlineNone, FlowControl.Throw, OpCodeType.Objmodel, StackBehaviour.Push0, StackBehaviour.Pop0);
		public static readonly OpCode Sizeof		= new OpCode("sizeof",			Code.Sizeof, OperandType.InlineType, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop0);
		public static readonly OpCode Refanytype	= new OpCode("refanytype",		Code.Refanytype, OperandType.InlineNone, FlowControl.Next, OpCodeType.Primitive, StackBehaviour.Pushi, StackBehaviour.Pop1);
		public static readonly OpCode Readonly		= new OpCode("readonly.",		Code.Readonly, OperandType.InlineNone, FlowControl.Meta, OpCodeType.Prefix, StackBehaviour.Push0, StackBehaviour.Pop0);
#pragma warning restore

		static OpCodes() {
			// The OpCode ctor copies itself to one of these arrays. Whatever are still null
			// are unsupported opcodes. Set them all to UNKNOWN1 or UNKNOWN2.
			for (int i = 0; i < OneByteOpCodes.Length; i++) {
				if (OneByteOpCodes[i] == null)
					OneByteOpCodes[i] = UNKNOWN1;
			}
			for (int i = 0; i < TwoByteOpCodes.Length; i++) {
				if (TwoByteOpCodes[i] == null)
					TwoByteOpCodes[i] = UNKNOWN2;
			}
		}
	}
}
