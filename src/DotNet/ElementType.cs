// dnlib: See LICENSE.txt for more info

﻿namespace dnlib.DotNet {
	/// <summary>
	/// See CorHdr.h/CorElementType
	/// </summary>
	public enum ElementType : byte {
		/// <summary/>
		End			= 0x00,
		/// <summary>System.Void</summary>
		Void		= 0x01,
		/// <summary>System.Boolean</summary>
		Boolean		= 0x02,
		/// <summary>System.Char</summary>
		Char		= 0x03,
		/// <summary>System.SByte</summary>
		I1			= 0x04,
		/// <summary>System.Byte</summary>
		U1 			= 0x05,
		/// <summary>System.Int16</summary>
		I2 			= 0x06,
		/// <summary>System.UInt16</summary>
		U2 			= 0x07,
		/// <summary>System.Int32</summary>
		I4 			= 0x08,
		/// <summary>System.UInt32</summary>
		U4			= 0x09,
		/// <summary>System.Int64</summary>
		I8			= 0x0A,
		/// <summary>System.UInt64</summary>
		U8			= 0x0B,
		/// <summary>System.Single</summary>
		R4			= 0x0C,
		/// <summary>System.Double</summary>
		R8			= 0x0D,
		/// <summary>System.String</summary>
		String		= 0x0E,
		/// <summary>Pointer type (*)</summary>
		Ptr			= 0x0F,
		/// <summary>ByRef type (&amp;)</summary>
		ByRef		= 0x10,
		/// <summary>Value type</summary>
		ValueType	= 0x11,
		/// <summary>Reference type</summary>
		Class		= 0x12,
		/// <summary>Type generic parameter</summary>
		Var			= 0x13,
		/// <summary>Multidimensional array ([*], [,], [,,], ...)</summary>
		Array		= 0x14,
		/// <summary>Generic instance type</summary>
		GenericInst	= 0x15,
		/// <summary>Typed byref</summary>
		TypedByRef	= 0x16,
		/// <summary>Value array (don't use)</summary>
		ValueArray	= 0x17,
		/// <summary>System.IntPtr</summary>
		I			= 0x18,
		/// <summary>System.UIntPtr</summary>
		U			= 0x19,
		/// <summary>native real (don't use)</summary>
		R			= 0x1A,
		/// <summary>Function pointer</summary>
		FnPtr		= 0x1B,
		/// <summary>System.Object</summary>
		Object		= 0x1C,
		/// <summary>Single-dimension, zero lower bound array ([])</summary>
		SZArray		= 0x1D,
		/// <summary>Method generic parameter</summary>
		MVar		= 0x1E,
		/// <summary>Required C modifier</summary>
		CModReqd	= 0x1F,
		/// <summary>Optional C modifier</summary>
		CModOpt		= 0x20,
		/// <summary>Used internally by the CLR (don't use)</summary>
		Internal	= 0x21,
		/// <summary>Module (don't use)</summary>
		Module		= 0x3F,
		/// <summary>Sentinel (method sigs only)</summary>
		Sentinel	= 0x41,
		/// <summary>Pinned type (locals only)</summary>
		Pinned		= 0x45,
	}

	public static partial class Extensions {
		/// <summary>
		/// Returns <c>true</c> if it's an integer or a floating point type
		/// </summary>
		/// <param name="etype">Element type</param>
		/// <returns></returns>
		public static bool IsPrimitive(this ElementType etype) {
			switch (etype) {
			case ElementType.Boolean:
			case ElementType.Char:
			case ElementType.I1:
			case ElementType.U1:
			case ElementType.I2:
			case ElementType.U2:
			case ElementType.I4:
			case ElementType.U4:
			case ElementType.I8:
			case ElementType.U8:
			case ElementType.R4:
			case ElementType.R8:
			case ElementType.I:
			case ElementType.U:
			case ElementType.R:
				return true;

			default:
				return false;
			}
		}

		/// <summary>
		/// Returns the size of the element type in bytes or <c>-1</c> if it's unknown
		/// </summary>
		/// <param name="etype">Element type</param>
		/// <param name="ptrSize">Size of a pointer</param>
		/// <returns></returns>
		public static int GetPrimitiveSize(this ElementType etype, int ptrSize = -1) {
			switch (etype) {
			case ElementType.Boolean:
			case ElementType.I1:
			case ElementType.U1:
				return 1;

			case ElementType.Char:
			case ElementType.I2:
			case ElementType.U2:
				return 2;

			case ElementType.I4:
			case ElementType.U4:
			case ElementType.R4:
				return 4;

			case ElementType.I8:
			case ElementType.U8:
			case ElementType.R8:
				return 8;

			case ElementType.Ptr:
			case ElementType.FnPtr:
			case ElementType.I:
			case ElementType.U:
				return ptrSize;

			default:
				return -1;
			}
		}

		/// <summary>
		/// Checks whether it's a value type
		/// </summary>
		/// <param name="etype">this</param>
		/// <returns><c>true</c> if it's a value type, <c>false</c> if it's not a value type or
		/// if we couldn't determine whether it's a value type. Eg., it could be a generic
		/// instance type.</returns>
		public static bool IsValueType(this ElementType etype) {
			switch (etype) {
			case ElementType.Void:
			case ElementType.Boolean:
			case ElementType.Char:
			case ElementType.I1:
			case ElementType.U1:
			case ElementType.I2:
			case ElementType.U2:
			case ElementType.I4:
			case ElementType.U4:
			case ElementType.I8:
			case ElementType.U8:
			case ElementType.R4:
			case ElementType.R8:
			case ElementType.ValueType:
			case ElementType.TypedByRef:
			case ElementType.ValueArray:
			case ElementType.I:
			case ElementType.U:
			case ElementType.R:
				return true;

			case ElementType.GenericInst:
				// We don't have enough info to determine whether this is a value type
				return false;

			case ElementType.End:
			case ElementType.String:
			case ElementType.Ptr:
			case ElementType.ByRef:
			case ElementType.Class:
			case ElementType.Var:
			case ElementType.Array:
			case ElementType.FnPtr:
			case ElementType.Object:
			case ElementType.SZArray:
			case ElementType.MVar:
			case ElementType.CModReqd:
			case ElementType.CModOpt:
			case ElementType.Internal:
			case ElementType.Module:
			case ElementType.Sentinel:
			case ElementType.Pinned:
			default:
				return false;
			}
		}
	}
}
