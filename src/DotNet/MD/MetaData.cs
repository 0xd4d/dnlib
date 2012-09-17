using System;
using System.Collections.Generic;
using dot10.PE;

namespace dot10.DotNet.MD {
	/// <summary>
	/// Common base class for #~ and #- metadata classes
	/// </summary>
	abstract class MetaData : IMetaData {
		/// <summary>
		/// The PE image
		/// </summary>
		protected IPEImage peImage;

		/// <summary>
		/// The .NET header
		/// </summary>
		protected ImageCor20Header cor20Header;

		/// <summary>
		/// The MD header
		/// </summary>
		protected MetaDataHeader mdHeader;

		/// <summary>
		/// The #Strings stream
		/// </summary>
		protected StringsStream stringsStream;

		/// <summary>
		/// The #US stream
		/// </summary>
		protected USStream usStream;

		/// <summary>
		/// The #Blob stream
		/// </summary>
		protected BlobStream blobStream;

		/// <summary>
		/// The #GUID stream
		/// </summary>
		protected GuidStream guidStream;

		/// <summary>
		/// The #~ or #- stream
		/// </summary>
		protected TablesStream tablesStream;

		/// <summary>
		/// All the streams that are present in the PE image
		/// </summary>
		protected List<DotNetStream> allStreams;

		uint[] fieldRidToTypeDefRid;
		uint[] methodRidToTypeDefRid;

		/// <inheritdoc/>
		public IPEImage PEImage {
			get { return peImage; }
		}

		/// <inheritdoc/>
		public StringsStream StringsStream {
			get { return stringsStream; }
		}

		/// <inheritdoc/>
		public USStream USStream {
			get { return usStream; }
		}

		/// <inheritdoc/>
		public BlobStream BlobStream {
			get { return blobStream; }
		}

		/// <inheritdoc/>
		public GuidStream GuidStream {
			get { return guidStream; }
		}

		/// <inheritdoc/>
		public TablesStream TablesStream {
			get { return tablesStream; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="peImage">The PE image</param>
		/// <param name="cor20Header">The .NET header</param>
		/// <param name="mdHeader">The MD header</param>
		protected MetaData(IPEImage peImage, ImageCor20Header cor20Header, MetaDataHeader mdHeader) {
			try {
				this.allStreams = new List<DotNetStream>();
				this.peImage = peImage;
				this.cor20Header = cor20Header;
				this.mdHeader = mdHeader;
			}
			catch {
				if (peImage != null)
					peImage.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Initializes the metadata, tables, streams
		/// </summary>
		public abstract void Initialize();

		/// <inheritdoc/>
		public virtual RidList GetTypeDefRidList() {
			return new ContiguousRidList(1, tablesStream.Get(Table.TypeDef).Rows);
		}

		/// <inheritdoc/>
		public virtual RidList GetExportedTypeRidList() {
			return new ContiguousRidList(1, tablesStream.Get(Table.ExportedType).Rows);
		}

		/// <inheritdoc/>
		public abstract RidList GetFieldRidList(uint typeDefRid);

		/// <inheritdoc/>
		public abstract RidList GetMethodRidList(uint typeDefRid);

		/// <inheritdoc/>
		public abstract RidList GetParamRidList(uint methodRid);

		/// <inheritdoc/>
		public abstract RidList GetEventRidList(uint eventMapRid);

		/// <inheritdoc/>
		public abstract RidList GetPropertyRidList(uint propertyMapRid);

		/// <summary>
		/// Binary searches the table for a <c>rid</c> whose key column at index
		/// <paramref name="keyColIndex"/> is equal to <paramref name="key"/>.
		/// </summary>
		/// <param name="tableSource">Table to search</param>
		/// <param name="keyColIndex">Key column index</param>
		/// <param name="key">Key</param>
		/// <returns>The <c>rid</c> of the found row, or 0 if none found</returns>
		protected abstract uint BinarySearch(Table tableSource, int keyColIndex, uint key);

		/// <summary>
		/// Finds all rows owned by <paramref name="key"/> in table <paramref name="tableSource"/>
		/// whose index is <paramref name="keyColIndex"/>
		/// </summary>
		/// <param name="tableSource">Table to search</param>
		/// <param name="keyColIndex">Key column index</param>
		/// <param name="key">Key</param>
		/// <returns>A <see cref="ContiguousRidList"/> instance</returns>
		protected RidList FindAllRows(Table tableSource, int keyColIndex, uint key) {
			uint startRid = BinarySearch(tableSource, keyColIndex, key);
			var table = tablesStream.Get(tableSource);
			if (table == null || table.IsInvalidRID(startRid))
				return ContiguousRidList.Empty;
			uint endRid = startRid + 1;
			for (; startRid > 1; startRid--) {
				uint key2;
				if (!tablesStream.ReadColumn(tableSource, startRid - 1, keyColIndex, out key2))
					break;	// Should never happen since startRid is valid
				if (key != key2)
					break;
			}
			for (; endRid <= table.Rows; endRid++) {
				uint key2;
				if (!tablesStream.ReadColumn(tableSource, endRid, keyColIndex, out key2))
					break;	// Should never happen since endRid is valid
				if (key != key2)
					break;
			}
			return new ContiguousRidList(startRid, endRid - startRid);
		}

		/// <summary>
		/// Finds all rows owned by <paramref name="key"/> in table <paramref name="tableSource"/>
		/// whose index is <paramref name="keyColIndex"/>. Should be called if <paramref name="tableSource"/>
		/// could be unsorted.
		/// </summary>
		/// <param name="tableSource">Table to search</param>
		/// <param name="keyColIndex">Key column index</param>
		/// <param name="key">Key</param>
		/// <returns>A <see cref="ContiguousRidList"/> instance</returns>
		protected virtual RidList FindAllRowsUnsorted(Table tableSource, int keyColIndex, uint key) {
			return FindAllRows(tableSource, keyColIndex, key);
		}

		/// <inheritdoc/>
		public RidList GetInterfaceImplRidList(uint typeDefRid) {
			var tbl = tablesStream.Get(Table.TypeDef);
			if (tbl == null || tbl.IsInvalidRID(typeDefRid))
				return ContiguousRidList.Empty;
			return FindAllRowsUnsorted(Table.InterfaceImpl, 0, typeDefRid);
		}

		/// <inheritdoc/>
		public RidList GetGenericParamRidList(Table table, uint rid) {
			var tbl = tablesStream.Get(table);
			if (tbl == null || tbl.IsInvalidRID(rid))
				return ContiguousRidList.Empty;
			uint codedToken;
			if (!CodedToken.TypeOrMethodDef.Encode(new MDToken(table, rid), out codedToken))
				return ContiguousRidList.Empty;
			// Sorted or not, the CLR only searches this table as if it were sorted.
			return FindAllRows(Table.GenericParam, 2, codedToken);
		}

		/// <inheritdoc/>
		public RidList GetGenericParamConstraintRidList(uint genericParamRid) {
			var tbl = tablesStream.Get(Table.GenericParam);
			if (tbl == null || tbl.IsInvalidRID(genericParamRid))
				return ContiguousRidList.Empty;
			return FindAllRowsUnsorted(Table.GenericParamConstraint, 0, genericParamRid);
		}

		/// <inheritdoc/>
		public RidList GetCustomAttributeRidList(Table table, uint rid) {
			var tbl = tablesStream.Get(table);
			if (tbl == null || tbl.IsInvalidRID(rid))
				return ContiguousRidList.Empty;
			uint codedToken;
			if (!CodedToken.HasCustomAttribute.Encode(new MDToken(table, rid), out codedToken))
				return ContiguousRidList.Empty;
			return FindAllRowsUnsorted(Table.CustomAttribute, 0, codedToken);
		}

		/// <inheritdoc/>
		public RidList GetDeclSecurityRidList(Table table, uint rid) {
			var tbl = tablesStream.Get(table);
			if (tbl == null || tbl.IsInvalidRID(rid))
				return ContiguousRidList.Empty;
			uint codedToken;
			if (!CodedToken.HasDeclSecurity.Encode(new MDToken(table, rid), out codedToken))
				return ContiguousRidList.Empty;
			return FindAllRowsUnsorted(Table.DeclSecurity, 1, codedToken);
		}

		/// <inheritdoc/>
		public RidList GetMethodSemanticsRidList(Table table, uint rid) {
			var tbl = tablesStream.Get(table);
			if (tbl == null || tbl.IsInvalidRID(rid))
				return ContiguousRidList.Empty;
			uint codedToken;
			if (!CodedToken.HasSemantic.Encode(new MDToken(table, rid), out codedToken))
				return ContiguousRidList.Empty;
			return FindAllRowsUnsorted(Table.MethodSemantics, 2, codedToken);
		}

		/// <inheritdoc/>
		public RidList GetMethodImplRidList(uint typeDefRid) {
			var tbl = tablesStream.Get(Table.TypeDef);
			if (tbl == null || tbl.IsInvalidRID(typeDefRid))
				return ContiguousRidList.Empty;
			return FindAllRowsUnsorted(Table.MethodImpl, 0, typeDefRid);
		}

		/// <inheritdoc/>
		public uint GetClassLayoutRid(uint typeDefRid) {
			var tbl = tablesStream.Get(Table.TypeDef);
			if (tbl == null || tbl.IsInvalidRID(typeDefRid))
				return 0;
			var list = FindAllRowsUnsorted(Table.ClassLayout, 2, typeDefRid);
			return list.Length == 0 ? 0 : list[0];
		}

		/// <inheritdoc/>
		public uint GetFieldLayoutRid(uint fieldRid) {
			var tbl = tablesStream.Get(Table.Field);
			if (tbl == null || tbl.IsInvalidRID(fieldRid))
				return 0;
			var list = FindAllRowsUnsorted(Table.FieldLayout, 1, fieldRid);
			return list.Length == 0 ? 0 : list[0];
		}

		/// <inheritdoc/>
		public uint GetFieldMarshalRid(Table table, uint rid) {
			var tbl = tablesStream.Get(table);
			if (tbl == null || tbl.IsInvalidRID(rid))
				return 0;
			uint codedToken;
			if (!CodedToken.HasFieldMarshal.Encode(new MDToken(table, rid), out codedToken))
				return 0;
			var list = FindAllRowsUnsorted(Table.FieldMarshal, 0, codedToken);
			return list.Length == 0 ? 0 : list[0];
		}

		/// <inheritdoc/>
		public uint GetFieldRVARid(uint fieldRid) {
			var tbl = tablesStream.Get(Table.Field);
			if (tbl == null || tbl.IsInvalidRID(fieldRid))
				return 0;
			var list = FindAllRowsUnsorted(Table.FieldRVA, 1, fieldRid);
			return list.Length == 0 ? 0 : list[0];
		}

		/// <inheritdoc/>
		public uint GetImplMapRid(Table table, uint rid) {
			var tbl = tablesStream.Get(table);
			if (tbl == null || tbl.IsInvalidRID(rid))
				return 0;
			uint codedToken;
			if (!CodedToken.MemberForwarded.Encode(new MDToken(table, rid), out codedToken))
				return 0;
			var list = FindAllRowsUnsorted(Table.ImplMap, 1, codedToken);
			return list.Length == 0 ? 0 : list[0];
		}

		/// <inheritdoc/>
		public uint GetNestedClassRid(uint typeDefRid) {
			var tbl = tablesStream.Get(Table.TypeDef);
			if (tbl == null || tbl.IsInvalidRID(typeDefRid))
				return 0;
			var list = FindAllRowsUnsorted(Table.NestedClass, 0, typeDefRid);
			return list.Length == 0 ? 0 : list[0];
		}

		/// <inheritdoc/>
		public uint GetEventMapRid(uint typeDefRid) {
			var tbl = tablesStream.Get(Table.TypeDef);
			if (tbl == null || tbl.IsInvalidRID(typeDefRid))
				return 0;
			var list = FindAllRowsUnsorted(Table.EventMap, 0, typeDefRid);
			return list.Length == 0 ? 0 : list[0];
		}

		/// <inheritdoc/>
		public uint GetPropertyMapRid(uint typeDefRid) {
			var tbl = tablesStream.Get(Table.TypeDef);
			if (tbl == null || tbl.IsInvalidRID(typeDefRid))
				return 0;
			var list = FindAllRowsUnsorted(Table.PropertyMap, 0, typeDefRid);
			return list.Length == 0 ? 0 : list[0];
		}

		/// <inheritdoc/>
		public uint GetConstantRid(Table table, uint rid) {
			var tbl = tablesStream.Get(table);
			if (tbl == null || tbl.IsInvalidRID(rid))
				return 0;
			uint codedToken;
			if (!CodedToken.HasConstant.Encode(new MDToken(table, rid), out codedToken))
				return 0;
			var list = FindAllRowsUnsorted(Table.Constant, 1, codedToken);
			return list.Length == 0 ? 0 : list[0];
		}

		/// <inheritdoc/>
		public uint GetOwnerTypeOfField(uint fieldRid) {
			if (fieldRidToTypeDefRid == null) 
				InitializeInverseFieldOwnerRidList();
			uint index = fieldRid - 1;
			if (index >= fieldRidToTypeDefRid.LongLength)
				return 0;
			return fieldRidToTypeDefRid[index];
		}

		void InitializeInverseFieldOwnerRidList() {
			if (fieldRidToTypeDefRid != null)
				return;
			fieldRidToTypeDefRid = new uint[tablesStream.Get(Table.Field).Rows];
			var ownerList = GetTypeDefRidList();
			for (uint i = 0; i < ownerList.Length; i++) {
				var ownerRid = ownerList[i];
				var fieldList = GetFieldRidList(ownerRid);
				for (uint j = 0; j < fieldList.Length; j++) {
					uint ridIndex = fieldList[j] - 1;
					if (fieldRidToTypeDefRid[ridIndex] != 0)
						continue;
					fieldRidToTypeDefRid[ridIndex] = ownerRid;
				}
			}
		}

		/// <inheritdoc/>
		public uint GetOwnerTypeOfMethod(uint methodRid) {
			if (methodRidToTypeDefRid == null)
				InitializeInverseMethodOwnerRidList();
			uint index = methodRid - 1;
			if (index >= methodRidToTypeDefRid.LongLength)
				return 0;
			return methodRidToTypeDefRid[index];
		}

		void InitializeInverseMethodOwnerRidList() {
			if (methodRidToTypeDefRid != null)
				return;
			methodRidToTypeDefRid = new uint[tablesStream.Get(Table.Method).Rows];
			var ownerList = GetTypeDefRidList();
			for (uint i = 0; i < ownerList.Length; i++) {
				var ownerRid = ownerList[i];
				var methodList = GetMethodRidList(ownerRid);
				for (uint j = 0; j < methodList.Length; j++) {
					uint ridIndex = methodList[j] - 1;
					if (methodRidToTypeDefRid[ridIndex] != 0)
						continue;
					methodRidToTypeDefRid[ridIndex] = ownerRid;
				}
			}
		}

		/// <inheritdoc/>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose method
		/// </summary>
		/// <param name="disposing">true if called by <see cref="Dispose()"/></param>
		protected virtual void Dispose(bool disposing) {
			if (!disposing)
				return;
			if (peImage != null)
				peImage.Dispose();
			if (stringsStream != null)
				stringsStream.Dispose();
			if (usStream != null)
				usStream.Dispose();
			if (blobStream != null)
				blobStream.Dispose();
			if (guidStream != null)
				guidStream.Dispose();
			if (tablesStream != null)
				tablesStream.Dispose();
			peImage = null;
			cor20Header = null;
			mdHeader = null;
			stringsStream = null;
			usStream = null;
			blobStream = null;
			guidStream = null;
			tablesStream = null;
			allStreams = null;
		}
	}
}
