using System;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the CustomAttribute table
	/// </summary>
	public abstract class CustomAttribute : IMDTokenProvider {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.CustomAttribute, rid); }
		}

		/// <summary>
		/// From column CustomAttribute.Parent
		/// </summary>
		public abstract IHasCustomAttribute Parent { get; set; }

		/// <summary>
		/// From column CustomAttribute.Type
		/// </summary>
		public abstract ICustomAttributeType Type { get; set; }

		/// <summary>
		/// From column CustomAttribute.Value
		/// </summary>
		public abstract byte[] Value { get; set; }
	}

	/// <summary>
	/// A CustomAttribute row created by the user and not present in the original .NET file
	/// </summary>
	public class CustomAttributeUser : CustomAttribute {
		IHasCustomAttribute parent;
		ICustomAttributeType type;
		byte[] value;

		/// <inheritdoc/>
		public override IHasCustomAttribute Parent {
			get { return parent; }
			set { parent = value; }
		}

		/// <inheritdoc/>
		public override ICustomAttributeType Type {
			get { return type; }
			set { type = value; }
		}

		/// <inheritdoc/>
		public override byte[] Value {
			get { return value; }
			set { this.value = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public CustomAttributeUser() {
		}
	}

	/// <summary>
	/// Created from a row in the CustomAttribute table
	/// </summary>
	sealed class CustomAttributeMD : CustomAttribute {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawCustomAttributeRow rawRow;

		UserValue<IHasCustomAttribute> parent;
		UserValue<ICustomAttributeType> type;
		UserValue<byte[]> value;

		/// <inheritdoc/>
		public override IHasCustomAttribute Parent {
			get { return parent.Value; }
			set { parent.Value = value; }
		}

		/// <inheritdoc/>
		public override ICustomAttributeType Type {
			get { return type.Value; }
			set { type.Value = value; }
		}

		/// <inheritdoc/>
		public override byte[] Value {
			get { return value.Value; }
			set { this.value.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>CustomAttribute</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public CustomAttributeMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.Get(Table.CustomAttribute).IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("CustomAttribute rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			parent.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveHasCustomAttribute(rawRow.Parent);
			};
			type.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveCustomAttributeType(rawRow.Type);
			};
			value.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.BlobStream.Read(rawRow.Value);	//TODO: Shouldn't be a byte[]
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadCustomAttributeRow(rid);
		}
	}
}
