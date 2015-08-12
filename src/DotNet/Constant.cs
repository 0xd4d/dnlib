// dnlib: See LICENSE.txt for more info

using System;
using System.Text;
using dnlib.DotNet.MD;

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
		public ElementType Type {
			get { return type; }
			set { type = value; }
		}
		/// <summary/>
		protected ElementType type;

		/// <summary>
		/// From column Constant.Value
		/// </summary>
		public object Value {
			get { return value; }
			set { this.value = value; }
		}
		/// <summary/>
		protected object value;
	}

	/// <summary>
	/// A Constant row created by the user and not present in the original .NET file
	/// </summary>
	public class ConstantUser : Constant {
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
			this.value = value;
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
	sealed class ConstantMD : Constant, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

		readonly uint origRid;

		/// <inheritdoc/>
		public uint OrigRid {
			get { return origRid; }
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
			this.origRid = rid;
			this.rid = rid;
			this.readerModule = readerModule;
			uint value = readerModule.TablesStream.ReadConstantRow(origRid, out this.type);
			this.value = GetValue(this.type, readerModule.BlobStream.ReadNoNull(value));
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
	}
}
