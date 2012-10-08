using System;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the FieldLayout table
	/// </summary>
	public abstract class FieldLayout : IMDTokenProvider {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.FieldLayout, rid); }
		}

		/// <inheritdoc/>
		public uint Rid {
			get { return rid; }
			set { rid = value; }
		}

		/// <summary>
		/// From column FieldLayout.OffSet
		/// </summary>
		public abstract uint Offset { get; set; }

		/// <summary>
		/// From column FieldLayout.Field
		/// </summary>
		public abstract FieldDef Field { get; set; }
	}

	/// <summary>
	/// A FieldLayout row created by the user and not present in the original .NET file
	/// </summary>
	public class FieldLayoutUser : FieldLayout {
		uint offset;
		FieldDef field;

		/// <summary>
		/// From column FieldLayout.OffSet
		/// </summary>
		public override uint Offset {
			get { return offset; }
			set { offset = value; }
		}

		/// <summary>
		/// From column FieldLayout.Field
		/// </summary>
		public override FieldDef Field {
			get { return field; }
			set { field = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public FieldLayoutUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="field">Field</param>
		/// <param name="offset">Offset</param>
		public FieldLayoutUser(FieldDef field, uint offset) {
			this.offset = offset;
			this.field = field;
		}
	}

	/// <summary>
	/// Created from a row in the FieldLayout table
	/// </summary>
	sealed class FieldLayoutMD : FieldLayout {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow"/> is called</summary>
		RawFieldLayoutRow rawRow;

		UserValue<uint> offset;
		UserValue<FieldDef> field;

		/// <summary>
		/// From column FieldLayout.OffSet
		/// </summary>
		public override uint Offset {
			get { return offset.Value; }
			set { offset.Value = value; }
		}

		/// <summary>
		/// From column FieldLayout.Field
		/// </summary>
		public override FieldDef Field {
			get { return field.Value; }
			set { field.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>FieldLayout</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public FieldLayoutMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.Get(Table.FieldLayout).IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("FieldLayout rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			offset.ReadOriginalValue = () => {
				InitializeRawRow();
				return rawRow.OffSet;
			};
			field.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveField(rawRow.Field);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadFieldLayoutRow(rid);
		}
	}
}
