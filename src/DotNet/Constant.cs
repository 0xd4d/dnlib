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

﻿using System;
using System.Text;
using dnlib.Utils;
using dnlib.DotNet.MD;
using dnlib.Threading;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the Constant table
	/// </summary>
	public abstract class Constant : IMDTokenProvider {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.Constant, rid); }
		}

		/// <inheritdoc/>
		public uint Rid {
			get { return rid; }
			set { rid = value; }
		}

		/// <summary>
		/// From column Constant.Type
		/// </summary>
		public abstract ElementType Type { get; set; }

		/// <summary>
		/// From column Constant.Value
		/// </summary>
		public abstract object Value { get; set; }
	}

	/// <summary>
	/// A Constant row created by the user and not present in the original .NET file
	/// </summary>
	public class ConstantUser : Constant {
		ElementType type;
		object value;

		/// <inheritdoc/>
		public override ElementType Type {
			get { return type; }
			set { type = value; }
		}

		/// <inheritdoc/>
		public override object Value {
			get { return value; }
			set { this.value = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public ConstantUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Value</param>
		public ConstantUser(object value) {
			this.type = GetElementType(value);
			this.value = value == null ? 0 : value;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Value</param>
		/// <param name="type">Type</param>
		public ConstantUser(object value, ElementType type) {
			this.type = type;
			this.value = value;
		}

		static ElementType GetElementType(object value) {
			if (value == null)
				return ElementType.Class;
			switch (System.Type.GetTypeCode(value.GetType())) {
			case TypeCode.Boolean:	return ElementType.Boolean;
			case TypeCode.Char:		return ElementType.Char;
			case TypeCode.SByte:	return ElementType.I1;
			case TypeCode.Byte:		return ElementType.U1;
			case TypeCode.Int16:	return ElementType.I2;
			case TypeCode.UInt16:	return ElementType.U2;
			case TypeCode.Int32:	return ElementType.I4;
			case TypeCode.UInt32:	return ElementType.U4;
			case TypeCode.Int64:	return ElementType.I8;
			case TypeCode.UInt64:	return ElementType.U8;
			case TypeCode.Single:	return ElementType.R4;
			case TypeCode.Double:	return ElementType.R8;
			case TypeCode.String:	return ElementType.String;
			default: return ElementType.Void;
			}
		}
	}

	/// <summary>
	/// Created from a row in the Constant table
	/// </summary>
	sealed class ConstantMD : Constant {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow_NoLock"/> is called</summary>
		RawConstantRow rawRow;

		UserValue<ElementType> type;
		UserValue<object> value;
#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		/// <inheritdoc/>
		public override ElementType Type {
			get { return type.Value; }
			set { type.Value = value; }
		}

		/// <inheritdoc/>
		public override object Value {
			get { return value.Value; }
			set { this.value.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>Constant</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public ConstantMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.ConstantTable.IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("Constant rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			type.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				return (ElementType)rawRow.Type;
			};
			value.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				return GetValue((ElementType)rawRow.Type, readerModule.BlobStream.ReadNoNull(rawRow.Value));
			};
#if THREAD_SAFE
			type.Lock = theLock;
			value.Lock = theLock;
#endif
		}

		static object GetValue(ElementType etype, byte[] data) {
			switch (etype) {
			case ElementType.Boolean:
				if (data == null || data.Length < 1)
					return false;
				return BitConverter.ToBoolean(data, 0);

			case ElementType.Char:
				if (data == null || data.Length < 2)
					return (char)0;
				return BitConverter.ToChar(data, 0);

			case ElementType.I1:
				if (data == null || data.Length < 1)
					return (sbyte)0;
				return (sbyte)data[0];

			case ElementType.U1:
				if (data == null || data.Length < 1)
					return (byte)0;
				return data[0];

			case ElementType.I2:
				if (data == null || data.Length < 2)
					return (short)0;
				return BitConverter.ToInt16(data, 0);

			case ElementType.U2:
				if (data == null || data.Length < 2)
					return (ushort)0;
				return BitConverter.ToUInt16(data, 0);

			case ElementType.I4:
				if (data == null || data.Length < 4)
					return (int)0;
				return BitConverter.ToInt32(data, 0);

			case ElementType.U4:
				if (data == null || data.Length < 4)
					return (uint)0;
				return BitConverter.ToUInt32(data, 0);

			case ElementType.I8:
				if (data == null || data.Length < 8)
					return (long)0;
				return BitConverter.ToInt64(data, 0);

			case ElementType.U8:
				if (data == null || data.Length < 8)
					return (ulong)0;
				return BitConverter.ToUInt64(data, 0);

			case ElementType.R4:
				if (data == null || data.Length < 4)
					return (float)0;
				return BitConverter.ToSingle(data, 0);

			case ElementType.R8:
				if (data == null || data.Length < 8)
					return (double)0;
				return BitConverter.ToDouble(data, 0);

			case ElementType.String:
				if (data == null)
					return string.Empty;
				return Encoding.Unicode.GetString(data, 0, data.Length / 2 * 2);

			case ElementType.Class:
				return null;

			default:
				return null;
			}
		}

		void InitializeRawRow_NoLock() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadConstantRow(rid);
		}
	}
}
