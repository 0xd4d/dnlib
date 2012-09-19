using System;
using System.Collections.Generic;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the PropertyMap table
	/// </summary>
	public abstract class PropertyMap : IMDTokenProvider {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.PropertyMap, rid); }
		}

		/// <summary>
		/// From column PropertyMap.Parent
		/// </summary>
		public abstract TypeDef Parent { get; set; }

		/// <summary>
		/// From column PropertyMap.PropertyList
		/// </summary>
		public abstract IList<PropertyDef> Properties { get; }
	}

	/// <summary>
	/// A PropertyMap row created by the user and not present in the original .NET file
	/// </summary>
	public class PropertyMapUser : PropertyMap {
		TypeDef parent;
		IList<PropertyDef> properties;

		/// <inheritdoc/>
		public override TypeDef Parent {
			get { return parent; }
			set { parent = value; }
		}

		/// <inheritdoc/>
		public override IList<PropertyDef> Properties {
			get { return properties; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public PropertyMapUser() {
			this.properties = new List<PropertyDef>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parent">Parent</param>
		/// <param name="property1">Property #1</param>
		public PropertyMapUser(TypeDef parent, PropertyDef property1) {
			this.parent = parent;
			this.properties = new List<PropertyDef> { property1 };
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parent">Parent</param>
		/// <param name="property1">Property #1</param>
		/// <param name="property2">Property #2</param>
		public PropertyMapUser(TypeDef parent, PropertyDef property1, PropertyDef property2) {
			this.parent = parent;
			this.properties = new List<PropertyDef> { property1, property2 };
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parent">Parent</param>
		/// <param name="property1">Property #1</param>
		/// <param name="property2">Property #2</param>
		/// <param name="property3">Property #3</param>
		public PropertyMapUser(TypeDef parent, PropertyDef property1, PropertyDef property2, PropertyDef property3) {
			this.parent = parent;
			this.properties = new List<PropertyDef> { property1, property2, property3 };
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parent">Parent</param>
		/// <param name="properties">Properties</param>
		public PropertyMapUser(TypeDef parent, params PropertyDef[] properties) {
			this.parent = parent;
			this.properties = new List<PropertyDef>(properties);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parent">Parent</param>
		/// <param name="properties">Properties</param>
		public PropertyMapUser(TypeDef parent, IEnumerable<PropertyDef> properties) {
			this.parent = parent;
			this.properties = new List<PropertyDef>(properties);
		}
	}

	/// <summary>
	/// Created from a row in the PropertyMap table
	/// </summary>
	sealed class PropertyMapMD : PropertyMap {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow"/> is called</summary>
		RawPropertyMapRow rawRow;

		UserValue<TypeDef> parent;
		LazyList<PropertyDef> properties;

		/// <inheritdoc/>
		public override TypeDef Parent {
			get { return parent.Value; }
			set { parent.Value = value; }
		}

		/// <inheritdoc/>
		public override IList<PropertyDef> Properties {
			get {
				if (properties == null) {
					var list = readerModule.MetaData.GetPropertyRidList(rid);
					properties = new LazyList<PropertyDef>((int)list.Length, list, (list2, index) => readerModule.ResolveProperty(((RidList)list2)[index]));
				}
				return properties;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>PropertyMap</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public PropertyMapMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.Get(Table.PropertyMap).IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("PropertyMap rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			parent.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveTypeDef(rawRow.Parent);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadPropertyMapRow(rid);
		}
	}
}
