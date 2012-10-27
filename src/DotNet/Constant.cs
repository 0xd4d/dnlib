using System;
using System.Text;
using dot10.DotNet.MD;

namespace dot10.DotNet {
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
			if (value is bool)
				return ElementType.Boolean;
			if (value is char)
				return ElementType.Char;
			if (value is sbyte)
				return ElementType.I1;
			if (value is byte)
				return ElementType.U1;
			if (value is short)
				return ElementType.I2;
			if (value is ushort)
				return ElementType.U2;
			if (value is int)
				return ElementType.I4;
			if (value is uint)
				return ElementType.U4;
			if (value is long)
				return ElementType.I8;
			if (value is ulong)
				return ElementType.U8;
			if (value is float)
				return ElementType.R4;
			if (value is double)
				return ElementType.R8;
			if (value is string)
				return ElementType.String;
			return ElementType.Void;
		}
	}

	/// <summary>
	/// Created from a row in the Constant table
	/// </summary>
	sealed class ConstantMD : Constant {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow"/> is called</summary>
		RawConstantRow rawRow;

		UserValue<ElementType> type;
		UserValue<object> value;

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
			if (readerModule.TablesStream.Get(Table.Constant).IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("Constant rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			type.ReadOriginalValue = () => {
				InitializeRawRow();
				return (ElementType)rawRow.Type;
			};
			value.ReadOriginalValue = () => {
				InitializeRawRow();
				return GetValue((ElementType)rawRow.Type, readerModule.BlobStream.ReadNoNull(rawRow.Value));
			};
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

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadConstantRow(rid);
		}
	}
}
