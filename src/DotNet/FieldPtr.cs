using System;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the FieldPtr table
	/// </summary>
	public abstract class FieldPtr : IMDTokenProvider {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.FieldPtr, rid); }
		}

		/// <inheritdoc/>
		public uint Rid {
			get { return rid; }
			set { rid = value; }
		}

		/// <summary>
		/// From column FieldPtr.Field
		/// </summary>
		public abstract FieldDef Field { get; set; }
	}

	/// <summary>
	/// A FieldPtr row created by the user and not present in the original .NET file
	/// </summary>
	public class FieldPtrUser : FieldPtr {
		FieldDef field;

		/// <inheritdoc/>
		public override FieldDef Field {
			get { return field; }
			set { field = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public FieldPtrUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="field">Field</param>
		public FieldPtrUser(FieldDef field) {
			this.field = field;
		}
	}

	/// <summary>
	/// Created from a row in the FieldPtr table
	/// </summary>
	sealed class FieldPtrMD : FieldPtr {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow"/> is called</summary>
		RawFieldPtrRow rawRow;

		UserValue<FieldDef> field;

		/// <inheritdoc/>
		public override FieldDef Field {
			get { return field.Value; }
			set { field.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>FieldPtr</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public FieldPtrMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.Get(Table.FieldPtr).IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("FieldPtr rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			field.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveField(rawRow.Field);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadFieldPtrRow(rid);
		}
	}
}
