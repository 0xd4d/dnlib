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
		ContiguousRidList FindAllRows(Table tableSource, int keyColIndex, uint key) {
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

		/// <inheritdoc/>
		public RidList GetGenericParamRidList(Table table, uint rid) {
			var tbl = tablesStream.Get(table);
			if (tbl == null || tbl.IsInvalidRID(rid))
				return ContiguousRidList.Empty;
			uint codedToken;
			if (!CodedToken.TypeOrMethodDef.Encode(new MDToken(table, rid), out codedToken))
				return ContiguousRidList.Empty;
			return FindAllRows(Table.GenericParam, 2, codedToken);
		}

		/// <inheritdoc/>
		public RidList GetMethodSpecRidList(Table table, uint rid) {
			var tbl = tablesStream.Get(table);
			if (tbl == null || tbl.IsInvalidRID(rid))
				return ContiguousRidList.Empty;
			uint codedToken;
			if (!CodedToken.MethodDefOrRef.Encode(new MDToken(table, rid), out codedToken))
				return ContiguousRidList.Empty;
			return FindAllRows(Table.MethodSpec, 0, codedToken);
		}

		/// <inheritdoc/>
		public RidList GetInterfaceImplRidList(uint typeDefRid) {
			var tbl = tablesStream.Get(Table.TypeDef);
			if (tbl == null || tbl.IsInvalidRID(typeDefRid))
				return ContiguousRidList.Empty;
			return FindAllRows(Table.InterfaceImpl, 0, typeDefRid);
		}

		/// <inheritdoc/>
		public RidList GetCustomAttributeRidList(Table table, uint rid) {
			var tbl = tablesStream.Get(table);
			if (tbl == null || tbl.IsInvalidRID(rid))
				return ContiguousRidList.Empty;
			uint codedToken;
			if (!CodedToken.HasCustomAttribute.Encode(new MDToken(table, rid), out codedToken))
				return ContiguousRidList.Empty;
			return FindAllRows(Table.CustomAttribute, 0, codedToken);
		}

		/// <inheritdoc/>
		public RidList GetDeclSecurityRidList(Table table, uint rid) {
			var tbl = tablesStream.Get(table);
			if (tbl == null || tbl.IsInvalidRID(rid))
				return ContiguousRidList.Empty;
			uint codedToken;
			if (!CodedToken.HasDeclSecurity.Encode(new MDToken(table, rid), out codedToken))
				return ContiguousRidList.Empty;
			return FindAllRows(Table.DeclSecurity, 1, codedToken);
		}

		/// <inheritdoc/>
		public RidList GetMethodSemanticsRidList(Table table, uint rid) {
			var tbl = tablesStream.Get(table);
			if (tbl == null || tbl.IsInvalidRID(rid))
				return ContiguousRidList.Empty;
			uint codedToken;
			if (!CodedToken.HasSemantic.Encode(new MDToken(table, rid), out codedToken))
				return ContiguousRidList.Empty;
			return FindAllRows(Table.MethodSemantics, 2, codedToken);
		}

		/// <inheritdoc/>
		public RidList GetMethodImplRidList(uint typeDefRid) {
			var tbl = tablesStream.Get(Table.TypeDef);
			if (tbl == null || tbl.IsInvalidRID(typeDefRid))
				return ContiguousRidList.Empty;
			return FindAllRows(Table.MethodImpl, 0, typeDefRid);
		}

		/// <inheritdoc/>
		public RidList GetGenericParamConstraintRidList(uint genericParamRid) {
			var tbl = tablesStream.Get(Table.GenericParam);
			if (tbl == null || tbl.IsInvalidRID(genericParamRid))
				return ContiguousRidList.Empty;
			return FindAllRows(Table.GenericParamConstraint, 0, genericParamRid);
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
