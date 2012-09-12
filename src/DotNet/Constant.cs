using System;
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

		/// <summary>
		/// From column Constant.Type
		/// </summary>
		public abstract ElementType Type { get; set; }

		/// <summary>
		/// From column Constant.Parent
		/// </summary>
		public abstract IHasConstant Parent { get; set; }

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
		IHasConstant parent;
		object value;

		/// <inheritdoc/>
		public override ElementType Type {
			get { return type; }
			set { type = value; }
		}

		/// <inheritdoc/>
		public override IHasConstant Parent {
			get { return parent; }
			set { parent = value; }
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
		/// <param name="parent">Parent</param>
		public ConstantUser(IHasConstant parent) {
			this.parent = parent;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parent">Parent</param>
		/// <param name="value">Value</param>
		public ConstantUser(IHasConstant parent, object value) {
			this.type = GetElementType(value);
			this.parent = parent;
			this.value = value == null ? 0 : value;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parent">Parent</param>
		/// <param name="value">Value</param>
		/// <param name="type">Type</param>
		public ConstantUser(IHasConstant parent, object value, ElementType type) {
			this.type = type;
			this.parent = parent;
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
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawConstantRow rawRow;

		UserValue<ElementType> type;
		UserValue<IHasConstant> parent;
		UserValue<object> value;

		/// <inheritdoc/>
		public override ElementType Type {
			get { return type.Value; }
			set { type.Value = value; }
		}

		/// <inheritdoc/>
		public override IHasConstant Parent {
			get { return parent.Value; }
			set { parent.Value = value; }
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
			parent.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveHasConstant(rawRow.Parent);
			};
			value.ReadOriginalValue = () => {
				InitializeRawRow();
				return null;	//TODO:
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadConstantRow(rid);
		}
	}
}
