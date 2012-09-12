using System;
using dot10.DotNet.MD;
using dot10.PE;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the FieldRVA table
	/// </summary>
	public abstract class FieldRVA : IMDTokenProvider {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.FieldRVA, rid); }
		}

		/// <summary>
		/// From column FieldRVA.RVA
		/// </summary>
		public abstract RVA RVA { get; set; }

		/// <summary>
		/// From column FieldRVA.Field
		/// </summary>
		public abstract FieldDef Field { get; set; }
	}

	/// <summary>
	/// A FieldRVA row created by the user and not present in the original .NET file
	/// </summary>
	public class FieldRVAUser : FieldRVA {
		RVA rva;
		FieldDef field;

		/// <inheritdoc/>
		public override RVA RVA {
			get { return rva; }
			set { rva = value; }
		}

		/// <inheritdoc/>
		public override FieldDef Field {
			get { return field; }
			set { field = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public FieldRVAUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="field">Field</param>
		/// <param name="rva">RVA</param>
		public FieldRVAUser(FieldDef field, RVA rva) {
			this.rva = rva;
			this.field = field;
		}
	}

	/// <summary>
	/// Created from a row in the FieldRVA table
	/// </summary>
	sealed class FieldRVAMD : FieldRVA {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawFieldRVARow rawRow;

		UserValue<RVA> rva;
		UserValue<FieldDef> field;

		/// <inheritdoc/>
		public override RVA RVA {
			get { return rva.Value; }
			set { rva.Value = value; }
		}

		/// <inheritdoc/>
		public override FieldDef Field {
			get { return field.Value; }
			set { field.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>FieldRVA</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public FieldRVAMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.Get(Table.FieldRVA).IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("FieldRVA rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			rva.ReadOriginalValue = () => {
				InitializeRawRow();
				return new RVA(rawRow.RVA);
			};
			field.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveField(rawRow.Field);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadFieldRVARow(rid);
		}
	}
}
