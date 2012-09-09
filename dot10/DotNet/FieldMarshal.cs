using System;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the FieldMarshal table
	/// </summary>
	public abstract class FieldMarshal : IMDTokenProvider {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.FieldMarshal, rid); }
		}

		/// <summary>
		/// From column FieldMarshal.Parent
		/// </summary>
		public abstract IHasFieldMarshal Field { get; set; }

		/// <summary>
		/// From column FieldMarshal.NativeType
		/// </summary>
		public abstract byte[] NativeType { get; set; }
	}

	/// <summary>
	/// A FieldMarshal row created by the user and not present in the original .NET file
	/// </summary>
	public class FieldMarshalUser : FieldMarshal {
		IHasFieldMarshal field;
		byte[] nativeType;

		/// <inheritdoc/>
		public override IHasFieldMarshal Field {
			get { return field; }
			set { field = value; }
		}

		/// <inheritdoc/>
		public override byte[] NativeType {
			get { return nativeType; }
			set { nativeType = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public FieldMarshalUser() {
		}
	}

	/// <summary>
	/// Created from a row in the FieldMarshal table
	/// </summary>
	sealed class FieldMarshalMD : FieldMarshal {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawFieldMarshalRow rawRow;

		UserValue<IHasFieldMarshal> field;
		UserValue<byte[]> nativeType;

		/// <inheritdoc/>
		public override IHasFieldMarshal Field {
			get { return field.Value; }
			set { field.Value = value; }
		}

		/// <inheritdoc/>
		public override byte[] NativeType {
			get { return nativeType.Value; }
			set { nativeType.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>FieldMarshal</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public FieldMarshalMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			if (readerModule.TablesStream.Get(Table.FieldMarshal).Rows < rid)
				throw new BadImageFormatException(string.Format("FieldMarshal rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			field.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveHasFieldMarshal(rawRow.Parent);
			};
			nativeType.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.BlobStream.Read(rawRow.NativeType);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadFieldMarshalRow(rid);
		}
	}
}
