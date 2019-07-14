// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using dnlib.DotNet.MD;
using dnlib.IO;

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
		public MDToken MDToken => new MDToken(Table.Constant, rid);

		/// <inheritdoc/>
		public uint Rid {
			get => rid;
			set => rid = value;
		}

		/// <summary>
		/// From column Constant.Type
		/// </summary>
		public ElementType Type {
			get => type;
			set => type = value;
		}
		/// <summary/>
		protected ElementType type;

		/// <summary>
		/// From column Constant.Value
		/// </summary>
		public object Value {
			get => value;
			set => this.value = value;
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
			type = GetElementType(value);
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
			if (value is null)
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
		readonly uint origRid;

		/// <inheritdoc/>
		public uint OrigRid => origRid;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>Constant</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public ConstantMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule is null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.ConstantTable.IsInvalidRID(rid))
				throw new BadImageFormatException($"Constant rid {rid} does not exist");
#endif
			origRid = rid;
			this.rid = rid;
			bool b = readerModule.TablesStream.TryReadConstantRow(origRid, out var row);
			Debug.Assert(b);
			type = (ElementType)row.Type;
			var reader = readerModule.BlobStream.CreateReader(row.Value);
			value = GetValue(type, ref reader);
		}

		static object GetValue(ElementType etype, ref DataReader reader) {
			switch (etype) {
			case ElementType.Boolean:
				if (reader.Length < 1)
					return false;
				return reader.ReadBoolean();

			case ElementType.Char:
				if (reader.Length < 2)
					return (char)0;
				return reader.ReadChar();

			case ElementType.I1:
				if (reader.Length < 1)
					return (sbyte)0;
				return reader.ReadSByte();

			case ElementType.U1:
				if (reader.Length < 1)
					return (byte)0;
				return reader.ReadByte();

			case ElementType.I2:
				if (reader.Length < 2)
					return (short)0;
				return reader.ReadInt16();

			case ElementType.U2:
				if (reader.Length < 2)
					return (ushort)0;
				return reader.ReadUInt16();

			case ElementType.I4:
				if (reader.Length < 4)
					return (int)0;
				return reader.ReadInt32();

			case ElementType.U4:
				if (reader.Length < 4)
					return (uint)0;
				return reader.ReadUInt32();

			case ElementType.I8:
				if (reader.Length < 8)
					return (long)0;
				return reader.ReadInt64();

			case ElementType.U8:
				if (reader.Length < 8)
					return (ulong)0;
				return reader.ReadUInt64();

			case ElementType.R4:
				if (reader.Length < 4)
					return (float)0;
				return reader.ReadSingle();

			case ElementType.R8:
				if (reader.Length < 8)
					return (double)0;
				return reader.ReadDouble();

			case ElementType.String:
				return reader.ReadUtf16String((int)(reader.BytesLeft / 2));

			case ElementType.Class:
				return null;

			default:
				return null;
			}
		}
	}
}
