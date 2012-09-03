using System;
using System.Collections.Generic;

namespace dot10.dotNET.Types {
	/// <summary>
	/// Created from a row in the TypeDef table
	/// </summary>
	class TypeDefMD : TypeDef {
		/// <summary>The .NET metadata where this instance is located</summary>
		IMetaData metaData;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawTypeDefRow rawRow;

		UserValue<TypeAttributes> flags;
		UserValue<UTF8String> name;
		UserValue<UTF8String> @namespace;
		UserValue<ITypeDefOrRef> extends;

		/// <inheritdoc/>
		public override TypeAttributes Flags {
			get { return flags.Value; }
			set { flags.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Namespace {
			get { return @namespace.Value; }
			set { @namespace.Value = value; }
		}

		/// <inheritdoc/>
		public override ITypeDefOrRef Extends {
			get { throw new System.NotImplementedException();/*TODO*/ }
			set { throw new System.NotImplementedException();/*TODO*/ }
		}

		/// <inheritdoc/>
		public override IList<FieldDef> Fields {
			get { throw new System.NotImplementedException();/*TODO*/ }
		}

		/// <inheritdoc/>
		public override IList<MethodDef> Methods {
			get { throw new System.NotImplementedException();/*TODO*/ }
		}

		public TypeDefMD(IMetaData metaData, uint rid) {
			if (metaData == null)
				throw new ArgumentNullException("metaData");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			this.rid = rid;
			this.metaData = metaData;
#if DEBUG
			if (metaData.TablesStream.Get(Table.TypeDef).Rows < rid)
				throw new BadImageFormatException(string.Format("TypeDef rid {0} does not exist", rid));
#endif
			Initialize();
		}

		void Initialize() {
			flags.ReadOriginalValue = () => {
				InitializeRawRow();
				return (TypeAttributes)rawRow.Flags;
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow();
				return metaData.StringsStream.Read(rawRow.Name);
			};
			@namespace.ReadOriginalValue = () => {
				InitializeRawRow();
				return metaData.StringsStream.Read(rawRow.Namespace);
			};
			extends.ReadOriginalValue = () => {
				InitializeRawRow();
				throw new NotImplementedException();//TODO:
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = metaData.TablesStream.ReadTypeDefRow(rid);
		}
	}
}
