using System;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the PropertyPtr table
	/// </summary>
	public abstract class PropertyPtr : IMDTokenProvider {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.PropertyPtr, rid); }
		}

		/// <summary>
		/// From column PropertyPtr.Property
		/// </summary>
		public abstract PropertyDef Property { get; set; }
	}

	/// <summary>
	/// A PropertyPtr row created by the user and not present in the original .NET file
	/// </summary>
	public class PropertyPtrUser : PropertyPtr {
		PropertyDef property;

		/// <inheritdoc/>
		public override PropertyDef Property {
			get { return property; }
			set { property = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public PropertyPtrUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="property">Property</param>
		public PropertyPtrUser(PropertyDef property) {
			this.property = property;
		}
	}

	/// <summary>
	/// Created from a row in the PropertyPtr table
	/// </summary>
	sealed class PropertyPtrMD : PropertyPtr {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawPropertyPtrRow rawRow;

		UserValue<PropertyDef> property;

		/// <inheritdoc/>
		public override PropertyDef Property {
			get { return property.Value; }
			set { property.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>PropertyPtr</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public PropertyPtrMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.Get(Table.PropertyPtr).IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("PropertyPtr rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			property.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveProperty(rawRow.Property);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadPropertyPtrRow(rid);
		}
	}
}
