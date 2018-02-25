// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using dnlib.IO;
using dnlib.PE;
using dnlib.Threading;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet.MD {
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
		/// The #Pdb stream
		/// </summary>
		protected PdbStream pdbStream;

		/// <summary>
		/// All the streams that are present in the PE image
		/// </summary>
		protected ThreadSafe.IList<DotNetStream> allStreams;

		/// <inheritdoc/>
		public bool IsStandalonePortablePdb {
			get { return isStandalonePortablePdb; }
		}
		/// <summary><c>true</c> if this is standalone Portable PDB metadata</summary>
		protected readonly bool isStandalonePortablePdb;

		uint[] fieldRidToTypeDefRid;
		uint[] methodRidToTypeDefRid;
		uint[] eventRidToTypeDefRid;
		uint[] propertyRidToTypeDefRid;
		uint[] gpRidToOwnerRid;
		uint[] gpcRidToOwnerRid;
		uint[] paramRidToOwnerRid;
		Dictionary<uint, RandomRidList> typeDefRidToNestedClasses;
		RandomRidList nonNestedTypes;

		/// <summary>
		/// Sorts a table by key column
		/// </summary>
		protected sealed class SortedTable {
			RowInfo[] rows;

			/// <summary>
			/// Remembers <c>rid</c> and key
			/// </summary>
			[DebuggerDisplay("{rid} {key}")]
			struct RowInfo : IComparable<RowInfo> {
				public readonly uint rid;
				public readonly uint key;

				/// <summary>
				/// Constructor
				/// </summary>
				/// <param name="rid">Row ID</param>
				/// <param name="key">Key</param>
				public RowInfo(uint rid, uint key) {
					this.rid = rid;
					this.key = key;
				}

				/// <inheritdoc/>
				public int CompareTo(RowInfo other) {
					if (key < other.key)
						return -1;
					if (key > other.key)
						return 1;
					return rid.CompareTo(other.rid);
				}
			}

			/// <summary>
			/// Constructor
			/// </summary>
			/// <param name="mdTable">The MD table</param>
			/// <param name="keyColIndex">Index of key column</param>
			public SortedTable(MDTable mdTable, int keyColIndex) {
				InitializeKeys(mdTable, keyColIndex);
				Array.Sort(rows);
			}

			void InitializeKeys(MDTable mdTable, int keyColIndex) {
				var keyColumn = mdTable.TableInfo.Columns[keyColIndex];
				rows = new RowInfo[mdTable.Rows + 1];
				if (mdTable.Rows == 0)
					return;
				using (var reader = mdTable.CloneImageStream()) {
					reader.Position = keyColumn.Offset;
					int increment = mdTable.TableInfo.RowSize - keyColumn.Size;
					for (uint i = 1; i <= mdTable.Rows; i++) {
						rows[i] = new RowInfo(i, keyColumn.Read(reader));
						if (i < mdTable.Rows)
							reader.Position += increment;
					}
				}
			}

			/// <summary>
			/// Binary searches for a row with a certain key
			/// </summary>
			/// <param name="key">The key</param>
			/// <returns>The row or 0 if not found</returns>
			int BinarySearch(uint key) {
				int lo = 1, hi = rows.Length - 1;
				while (lo <= hi && hi != -1) {
					int curr = (lo + hi) / 2;
					uint key2 = rows[curr].key;
					if (key == key2)
						return curr;
					if (key2 > key)
						hi = curr - 1;
					else
						lo = curr + 1;
				}

				return 0;
			}

			/// <summary>
			/// Find all rids that contain <paramref name="key"/>
			/// </summary>
			/// <param name="key">The key</param>
			/// <returns>A new <see cref="RidList"/> instance</returns>
			public RidList FindAllRows(uint key) {
				int startIndex = BinarySearch(key);
				if (startIndex == 0)
					return RidList.Empty;
				int endIndex = startIndex + 1;
				for (; startIndex > 1; startIndex--) {
					if (key != rows[startIndex - 1].key)
						break;
				}
				for (; endIndex < rows.Length; endIndex++) {
					if (key != rows[endIndex].key)
						break;
				}
				var list = new RandomRidList(endIndex - startIndex);
				for (int i = startIndex; i < endIndex; i++)
					list.Add(rows[i].rid);
				return list;
			}
		}
		SortedTable eventMapSortedTable;
		SortedTable propertyMapSortedTable;

		/// <inheritdoc/>
		public abstract bool IsCompressed { get; }

		/// <inheritdoc/>
		public ImageCor20Header ImageCor20Header {
			get { return cor20Header; }
		}

		/// <inheritdoc/>
		public ushort MajorVersion {
			get { return mdHeader.MajorVersion; }
		}

		/// <inheritdoc/>
		public ushort MinorVersion {
			get { return mdHeader.MinorVersion; }
		}

		/// <inheritdoc/>
		public string VersionString {
			get { return mdHeader.VersionString; }
		}

		/// <inheritdoc/>
		public IPEImage PEImage {
			get { return peImage; }
		}

		/// <inheritdoc/>
		public MetaDataHeader MetaDataHeader {
			get { return mdHeader; }
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

		/// <inheritdoc/>
		public PdbStream PdbStream {
			get { return pdbStream; }
		}

		/// <inheritdoc/>
		public ThreadSafe.IList<DotNetStream> AllStreams {
			get { return allStreams; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="peImage">The PE image</param>
		/// <param name="cor20Header">The .NET header</param>
		/// <param name="mdHeader">The MD header</param>
		protected MetaData(IPEImage peImage, ImageCor20Header cor20Header, MetaDataHeader mdHeader) {
			try {
				this.allStreams = ThreadSafeListCreator.Create<DotNetStream>();
				this.peImage = peImage;
				this.cor20Header = cor20Header;
				this.mdHeader = mdHeader;
				isStandalonePortablePdb = false;
			}
			catch {
				if (peImage != null)
					peImage.Dispose();
				throw;
			}
		}

		internal MetaData(MetaDataHeader mdHeader, bool isStandalonePortablePdb) {
			this.allStreams = ThreadSafeListCreator.Create<DotNetStream>();
			this.peImage = null;
			this.cor20Header = null;
			this.mdHeader = mdHeader;
			this.isStandalonePortablePdb = isStandalonePortablePdb;
		}

		/// <summary>
		/// Initializes the metadata, tables, streams
		/// </summary>
		public void Initialize(IImageStream mdStream) {
			InitializeInternal(mdStream);

			if (tablesStream == null)
				throw new BadImageFormatException("Missing MD stream");
			if (isStandalonePortablePdb && pdbStream == null)
				throw new BadImageFormatException("Missing #Pdb stream");
			InitializeNonExistentHeaps();
		}

		/// <summary>
		/// Creates empty heap objects if they're not present in the metadata
		/// </summary>
		protected void InitializeNonExistentHeaps() {
			if (stringsStream == null)
				stringsStream = new StringsStream();
			if (usStream == null)
				usStream = new USStream();
			if (blobStream == null)
				blobStream = new BlobStream();
			if (guidStream == null)
				guidStream = new GuidStream();
		}

		/// <summary>
		/// Called by <see cref="Initialize(IImageStream)"/>
		/// </summary>
		protected abstract void InitializeInternal(IImageStream mdStream);

		/// <inheritdoc/>
		public virtual RidList GetTypeDefRidList() {
			return new ContiguousRidList(1, tablesStream.TypeDefTable.Rows);
		}

		/// <inheritdoc/>
		public virtual RidList GetExportedTypeRidList() {
			return new ContiguousRidList(1, tablesStream.ExportedTypeTable.Rows);
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
		/// <paramref name="keyColIndex"/> is equal to <paramref name="key"/>. The
		/// <see cref="tablesStream"/> has acquired its lock so only <c>*_NoLock</c> methods
		/// may be called.
		/// </summary>
		/// <param name="tableSource">Table to search</param>
		/// <param name="keyColIndex">Key column index</param>
		/// <param name="key">Key</param>
		/// <returns>The <c>rid</c> of the found row, or 0 if none found</returns>
		protected abstract uint BinarySearch_NoLock(MDTable tableSource, int keyColIndex, uint key);

		/// <summary>
		/// Finds all rows owned by <paramref name="key"/> in table <paramref name="tableSource"/>
		/// whose index is <paramref name="keyColIndex"/>
		/// </summary>
		/// <param name="tableSource">Table to search</param>
		/// <param name="keyColIndex">Key column index</param>
		/// <param name="key">Key</param>
		/// <returns>A <see cref="RidList"/> instance</returns>
		protected RidList FindAllRows(MDTable tableSource, int keyColIndex, uint key) {
#if THREAD_SAFE
			tablesStream.theLock.EnterWriteLock(); try {
#endif
			uint startRid = BinarySearch_NoLock(tableSource, keyColIndex, key);
			if (tableSource.IsInvalidRID(startRid))
				return RidList.Empty;
			uint endRid = startRid + 1;
			var column = tableSource.TableInfo.Columns[keyColIndex];
			for (; startRid > 1; startRid--) {
				uint key2;
				if (!tablesStream.ReadColumn_NoLock(tableSource, startRid - 1, column, out key2))
					break;	// Should never happen since startRid is valid
				if (key != key2)
					break;
			}
			for (; endRid <= tableSource.Rows; endRid++) {
				uint key2;
				if (!tablesStream.ReadColumn_NoLock(tableSource, endRid, column, out key2))
					break;	// Should never happen since endRid is valid
				if (key != key2)
					break;
			}
			return new ContiguousRidList(startRid, endRid - startRid);
#if THREAD_SAFE
			} finally { tablesStream.theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Finds all rows owned by <paramref name="key"/> in table <paramref name="tableSource"/>
		/// whose index is <paramref name="keyColIndex"/>. Should be called if <paramref name="tableSource"/>
		/// could be unsorted.
		/// </summary>
		/// <param name="tableSource">Table to search</param>
		/// <param name="keyColIndex">Key column index</param>
		/// <param name="key">Key</param>
		/// <returns>A <see cref="RidList"/> instance</returns>
		protected virtual RidList FindAllRowsUnsorted(MDTable tableSource, int keyColIndex, uint key) {
			return FindAllRows(tableSource, keyColIndex, key);
		}

		/// <inheritdoc/>
		public RidList GetInterfaceImplRidList(uint typeDefRid) {
			return FindAllRowsUnsorted(tablesStream.InterfaceImplTable, 0, typeDefRid);
		}

		/// <inheritdoc/>
		public RidList GetGenericParamRidList(Table table, uint rid) {
			uint codedToken;
			if (!CodedToken.TypeOrMethodDef.Encode(new MDToken(table, rid), out codedToken))
				return RidList.Empty;
			return FindAllRowsUnsorted(tablesStream.GenericParamTable, 2, codedToken);
		}

		/// <inheritdoc/>
		public RidList GetGenericParamConstraintRidList(uint genericParamRid) {
			return FindAllRowsUnsorted(tablesStream.GenericParamConstraintTable, 0, genericParamRid);
		}

		/// <inheritdoc/>
		public RidList GetCustomAttributeRidList(Table table, uint rid) {
			uint codedToken;
			if (!CodedToken.HasCustomAttribute.Encode(new MDToken(table, rid), out codedToken))
				return RidList.Empty;
			return FindAllRowsUnsorted(tablesStream.CustomAttributeTable, 0, codedToken);
		}

		/// <inheritdoc/>
		public RidList GetDeclSecurityRidList(Table table, uint rid) {
			uint codedToken;
			if (!CodedToken.HasDeclSecurity.Encode(new MDToken(table, rid), out codedToken))
				return RidList.Empty;
			return FindAllRowsUnsorted(tablesStream.DeclSecurityTable, 1, codedToken);
		}

		/// <inheritdoc/>
		public RidList GetMethodSemanticsRidList(Table table, uint rid) {
			uint codedToken;
			if (!CodedToken.HasSemantic.Encode(new MDToken(table, rid), out codedToken))
				return RidList.Empty;
			return FindAllRowsUnsorted(tablesStream.MethodSemanticsTable, 2, codedToken);
		}

		/// <inheritdoc/>
		public RidList GetMethodImplRidList(uint typeDefRid) {
			return FindAllRowsUnsorted(tablesStream.MethodImplTable, 0, typeDefRid);
		}

		/// <inheritdoc/>
		public uint GetClassLayoutRid(uint typeDefRid) {
			var list = FindAllRowsUnsorted(tablesStream.ClassLayoutTable, 2, typeDefRid);
			return list.Length == 0 ? 0 : list[0];
		}

		/// <inheritdoc/>
		public uint GetFieldLayoutRid(uint fieldRid) {
			var list = FindAllRowsUnsorted(tablesStream.FieldLayoutTable, 1, fieldRid);
			return list.Length == 0 ? 0 : list[0];
		}

		/// <inheritdoc/>
		public uint GetFieldMarshalRid(Table table, uint rid) {
			uint codedToken;
			if (!CodedToken.HasFieldMarshal.Encode(new MDToken(table, rid), out codedToken))
				return 0;
			var list = FindAllRowsUnsorted(tablesStream.FieldMarshalTable, 0, codedToken);
			return list.Length == 0 ? 0 : list[0];
		}

		/// <inheritdoc/>
		public uint GetFieldRVARid(uint fieldRid) {
			var list = FindAllRowsUnsorted(tablesStream.FieldRVATable, 1, fieldRid);
			return list.Length == 0 ? 0 : list[0];
		}

		/// <inheritdoc/>
		public uint GetImplMapRid(Table table, uint rid) {
			uint codedToken;
			if (!CodedToken.MemberForwarded.Encode(new MDToken(table, rid), out codedToken))
				return 0;
			var list = FindAllRowsUnsorted(tablesStream.ImplMapTable, 1, codedToken);
			return list.Length == 0 ? 0 : list[0];
		}

		/// <inheritdoc/>
		public uint GetNestedClassRid(uint typeDefRid) {
			var list = FindAllRowsUnsorted(tablesStream.NestedClassTable, 0, typeDefRid);
			return list.Length == 0 ? 0 : list[0];
		}

		/// <inheritdoc/>
		public uint GetEventMapRid(uint typeDefRid) {
			// The EventMap and PropertyMap tables can only be trusted to be sorted if it's
			// an NGen image and it's the normal #- stream. The IsSorted bit must not be used
			// to check whether the tables are sorted. See coreclr: md/inc/metamodel.h / IsVerified()
			if (eventMapSortedTable == null)
				Interlocked.CompareExchange(ref eventMapSortedTable, new SortedTable(tablesStream.EventMapTable, 0), null);
			var list = eventMapSortedTable.FindAllRows(typeDefRid);
			return list.Length == 0 ? 0 : list[0];
		}

		/// <inheritdoc/>
		public uint GetPropertyMapRid(uint typeDefRid) {
			// Always unsorted, see comment in GetEventMapRid() above
			if (propertyMapSortedTable == null)
				Interlocked.CompareExchange(ref propertyMapSortedTable, new SortedTable(tablesStream.PropertyMapTable, 0), null);
			var list = propertyMapSortedTable.FindAllRows(typeDefRid);
			return list.Length == 0 ? 0 : list[0];
		}

		/// <inheritdoc/>
		public uint GetConstantRid(Table table, uint rid) {
			uint codedToken;
			if (!CodedToken.HasConstant.Encode(new MDToken(table, rid), out codedToken))
				return 0;
			var list = FindAllRowsUnsorted(tablesStream.ConstantTable, 2, codedToken);
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
			var newFieldRidToTypeDefRid = new uint[tablesStream.FieldTable.Rows];
			var ownerList = GetTypeDefRidList();
			for (uint i = 0; i < ownerList.Length; i++) {
				var ownerRid = ownerList[i];
				var fieldList = GetFieldRidList(ownerRid);
				for (uint j = 0; j < fieldList.Length; j++) {
					uint ridIndex = fieldList[j] - 1;
					if (newFieldRidToTypeDefRid[ridIndex] != 0)
						continue;
					newFieldRidToTypeDefRid[ridIndex] = ownerRid;
				}
			}
			Interlocked.CompareExchange(ref fieldRidToTypeDefRid, newFieldRidToTypeDefRid, null);
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
			var newMethodRidToTypeDefRid = new uint[tablesStream.MethodTable.Rows];
			var ownerList = GetTypeDefRidList();
			for (uint i = 0; i < ownerList.Length; i++) {
				var ownerRid = ownerList[i];
				var methodList = GetMethodRidList(ownerRid);
				for (uint j = 0; j < methodList.Length; j++) {
					uint ridIndex = methodList[j] - 1;
					if (newMethodRidToTypeDefRid[ridIndex] != 0)
						continue;
					newMethodRidToTypeDefRid[ridIndex] = ownerRid;
				}
			}
			Interlocked.CompareExchange(ref methodRidToTypeDefRid, newMethodRidToTypeDefRid, null);
		}

		/// <inheritdoc/>
		public uint GetOwnerTypeOfEvent(uint eventRid) {
			if (eventRidToTypeDefRid == null)
				InitializeInverseEventOwnerRidList();
			uint index = eventRid - 1;
			if (index >= eventRidToTypeDefRid.LongLength)
				return 0;
			return eventRidToTypeDefRid[index];
		}

		void InitializeInverseEventOwnerRidList() {
			if (eventRidToTypeDefRid != null)
				return;
			var newEventRidToTypeDefRid = new uint[tablesStream.EventTable.Rows];
			var ownerList = GetTypeDefRidList();
			for (uint i = 0; i < ownerList.Length; i++) {
				var ownerRid = ownerList[i];
				var eventList = GetEventRidList(GetEventMapRid(ownerRid));
				for (uint j = 0; j < eventList.Length; j++) {
					uint ridIndex = eventList[j] - 1;
					if (newEventRidToTypeDefRid[ridIndex] != 0)
						continue;
					newEventRidToTypeDefRid[ridIndex] = ownerRid;
				}
			}
			Interlocked.CompareExchange(ref eventRidToTypeDefRid, newEventRidToTypeDefRid, null);
		}

		/// <inheritdoc/>
		public uint GetOwnerTypeOfProperty(uint propertyRid) {
			if (propertyRidToTypeDefRid == null)
				InitializeInversePropertyOwnerRidList();
			uint index = propertyRid - 1;
			if (index >= propertyRidToTypeDefRid.LongLength)
				return 0;
			return propertyRidToTypeDefRid[index];
		}

		void InitializeInversePropertyOwnerRidList() {
			if (propertyRidToTypeDefRid != null)
				return;
			var newPropertyRidToTypeDefRid = new uint[tablesStream.PropertyTable.Rows];
			var ownerList = GetTypeDefRidList();
			for (uint i = 0; i < ownerList.Length; i++) {
				var ownerRid = ownerList[i];
				var propertyList = GetPropertyRidList(GetPropertyMapRid(ownerRid));
				for (uint j = 0; j < propertyList.Length; j++) {
					uint ridIndex = propertyList[j] - 1;
					if (newPropertyRidToTypeDefRid[ridIndex] != 0)
						continue;
					newPropertyRidToTypeDefRid[ridIndex] = ownerRid;
				}
			}
			Interlocked.CompareExchange(ref propertyRidToTypeDefRid, newPropertyRidToTypeDefRid, null);
		}

		/// <inheritdoc/>
		public uint GetOwnerOfGenericParam(uint gpRid) {
			// Don't use GenericParam.Owner column. If the GP table is sorted, it's
			// possible to have two blocks of GPs with the same owner. Only one of the
			// blocks is the "real" generic params for the owner. Of course, rarely
			// if ever will this occur, but could happen if some obfuscator has
			// added it.

			if (gpRidToOwnerRid == null)
				InitializeInverseGenericParamOwnerRidList();
			uint index = gpRid - 1;
			if (index >= gpRidToOwnerRid.LongLength)
				return 0;
			return gpRidToOwnerRid[index];
		}

		void InitializeInverseGenericParamOwnerRidList() {
			if (gpRidToOwnerRid != null)
				return;
			var gpTable = tablesStream.GenericParamTable;
			var newGpRidToOwnerRid = new uint[gpTable.Rows];

			// Find all owners by reading the GenericParam.Owner column
			var ownerCol = gpTable.TableInfo.Columns[2];
			var ownersDict = new Dictionary<uint, bool>();
#if THREAD_SAFE
			tablesStream.theLock.EnterWriteLock(); try {
#endif
			for (uint rid = 1; rid <= gpTable.Rows; rid++) {
				uint owner;
				if (!tablesStream.ReadColumn_NoLock(gpTable, rid, ownerCol, out owner))
					continue;
				ownersDict[owner] = true;
			}
#if THREAD_SAFE
			} finally { tablesStream.theLock.ExitWriteLock(); }
#endif

			// Now that we have the owners, find all the generic params they own. An obfuscated
			// module could have 2+ owners pointing to the same generic param row.
			var owners = new List<uint>(ownersDict.Keys);
			owners.Sort();
			for (int i = 0; i < owners.Count; i++) {
				uint ownerToken;
				if (!CodedToken.TypeOrMethodDef.Decode(owners[i], out ownerToken))
					continue;
				var ridList = GetGenericParamRidList(MDToken.ToTable(ownerToken), MDToken.ToRID(ownerToken));
				for (uint j = 0; j < ridList.Length; j++) {
					uint ridIndex = ridList[j] - 1;
					if (newGpRidToOwnerRid[ridIndex] != 0)
						continue;
					newGpRidToOwnerRid[ridIndex] = owners[i];
				}
			}
			Interlocked.CompareExchange(ref gpRidToOwnerRid, newGpRidToOwnerRid, null);
		}

		/// <inheritdoc/>
		public uint GetOwnerOfGenericParamConstraint(uint gpcRid) {
			// Don't use GenericParamConstraint.Owner column for the same reason
			// as described in GetOwnerOfGenericParam().

			if (gpcRidToOwnerRid == null)
				InitializeInverseGenericParamConstraintOwnerRidList();
			uint index = gpcRid - 1;
			if (index >= gpcRidToOwnerRid.LongLength)
				return 0;
			return gpcRidToOwnerRid[index];
		}

		void InitializeInverseGenericParamConstraintOwnerRidList() {
			if (gpcRidToOwnerRid != null)
				return;
			var gpcTable = tablesStream.GenericParamConstraintTable;
			var newGpcRidToOwnerRid = new uint[gpcTable.Rows];

			var ownerCol = gpcTable.TableInfo.Columns[0];
			var ownersDict = new Dictionary<uint, bool>();
#if THREAD_SAFE
			tablesStream.theLock.EnterWriteLock(); try {
#endif
			for (uint rid = 1; rid <= gpcTable.Rows; rid++) {
				uint owner;
				if (!tablesStream.ReadColumn_NoLock(gpcTable, rid, ownerCol, out owner))
					continue;
				ownersDict[owner] = true;
			}
#if THREAD_SAFE
			} finally { tablesStream.theLock.ExitWriteLock(); }
#endif

			var owners = new List<uint>(ownersDict.Keys);
			owners.Sort();
			for (int i = 0; i < owners.Count; i++) {
				uint ownerToken = owners[i];
				var ridList = GetGenericParamConstraintRidList(ownerToken);
				for (uint j = 0; j < ridList.Length; j++) {
					uint ridIndex = ridList[j] - 1;
					if (newGpcRidToOwnerRid[ridIndex] != 0)
						continue;
					newGpcRidToOwnerRid[ridIndex] = ownerToken;
				}
			}
			Interlocked.CompareExchange(ref gpcRidToOwnerRid, newGpcRidToOwnerRid, null);
		}

		/// <inheritdoc/>
		public uint GetOwnerOfParam(uint paramRid) {
			if (paramRidToOwnerRid == null)
				InitializeInverseParamOwnerRidList();
			uint index = paramRid - 1;
			if (index >= paramRidToOwnerRid.LongLength)
				return 0;
			return paramRidToOwnerRid[index];
		}

		void InitializeInverseParamOwnerRidList() {
			if (paramRidToOwnerRid != null)
				return;

			var newParamRidToOwnerRid = new uint[tablesStream.ParamTable.Rows];
			var table = tablesStream.MethodTable;
			for (uint rid = 1; rid <= table.Rows; rid++) {
				var ridList = GetParamRidList(rid);
				for (uint j = 0; j < ridList.Length; j++) {
					uint ridIndex = ridList[j] - 1;
					if (newParamRidToOwnerRid[ridIndex] != 0)
						continue;
					newParamRidToOwnerRid[ridIndex] = rid;
				}
			}
			Interlocked.CompareExchange(ref paramRidToOwnerRid, newParamRidToOwnerRid, null);
		}

		/// <inheritdoc/>
		public RidList GetNestedClassRidList(uint typeDefRid) {
			if (typeDefRidToNestedClasses == null)
				InitializeNestedClassesDictionary();
			RandomRidList ridList;
			if (typeDefRidToNestedClasses.TryGetValue(typeDefRid, out ridList))
				return ridList;
			return RidList.Empty;
		}

		void InitializeNestedClassesDictionary() {
			var table = tablesStream.NestedClassTable;
			var destTable = tablesStream.TypeDefTable;

			Dictionary<uint, bool> validTypeDefRids = null;
			var typeDefRidList = GetTypeDefRidList();
			if (typeDefRidList.Length != destTable.Rows) {
				validTypeDefRids = new Dictionary<uint, bool>((int)typeDefRidList.Length);
				for (uint i = 0; i < typeDefRidList.Length; i++)
					validTypeDefRids[typeDefRidList[i]] = true;
			}

			var nestedRidsDict = new Dictionary<uint, bool>((int)table.Rows);
			var nestedRids = new List<uint>((int)table.Rows);	// Need it so we add the rids in correct order
			for (uint rid = 1; rid <= table.Rows; rid++) {
				if (validTypeDefRids != null && !validTypeDefRids.ContainsKey(rid))
					continue;
				var row = tablesStream.ReadNestedClassRow(rid);
				if (row == null)
					continue;	// Should never happen since rid is valid
				if (!destTable.IsValidRID(row.NestedClass) || !destTable.IsValidRID(row.EnclosingClass))
					continue;
				if (nestedRidsDict.ContainsKey(row.NestedClass))
					continue;
				nestedRidsDict[row.NestedClass] = true;
				nestedRids.Add(row.NestedClass);
			}

			var newTypeDefRidToNestedClasses = new Dictionary<uint, RandomRidList>();
			foreach (var nestedRid in nestedRids) {
				var row = tablesStream.ReadNestedClassRow(GetNestedClassRid(nestedRid));
				if (row == null)
					continue;
				RandomRidList ridList;
				if (!newTypeDefRidToNestedClasses.TryGetValue(row.EnclosingClass, out ridList))
					newTypeDefRidToNestedClasses[row.EnclosingClass] = ridList = new RandomRidList();
				ridList.Add(nestedRid);
			}

			var newNonNestedTypes = new RandomRidList((int)(destTable.Rows - nestedRidsDict.Count));
			for (uint rid = 1; rid <= destTable.Rows; rid++) {
				if (validTypeDefRids != null && !validTypeDefRids.ContainsKey(rid))
					continue;
				if (nestedRidsDict.ContainsKey(rid))
					continue;
				newNonNestedTypes.Add(rid);
			}

			Interlocked.CompareExchange(ref nonNestedTypes, newNonNestedTypes, null);

			// Initialize this one last since it's tested by the callers of this method
			Interlocked.CompareExchange(ref typeDefRidToNestedClasses, newTypeDefRidToNestedClasses, null);
		}

		public RidList GetNonNestedClassRidList() {
			// Check typeDefRidToNestedClasses and not nonNestedTypes since
			// InitializeNestedClassesDictionary() writes to typeDefRidToNestedClasses last.
			if (typeDefRidToNestedClasses == null)
				InitializeNestedClassesDictionary();
			return nonNestedTypes;
		}

		public RidList GetLocalScopeRidList(uint methodRid) {
			return FindAllRows(tablesStream.LocalScopeTable, 0, methodRid);
		}

		public uint GetStateMachineMethodRid(uint methodRid) {
			var list = FindAllRows(tablesStream.StateMachineMethodTable, 0, methodRid);
			return list.Length == 0 ? 0 : list[0];
		}

		public RidList GetCustomDebugInformationRidList(Table table, uint rid) {
			uint codedToken;
			if (!CodedToken.HasCustomDebugInformation.Encode(new MDToken(table, rid), out codedToken))
				return RidList.Empty;
			return FindAllRows(tablesStream.CustomDebugInformationTable, 0, codedToken);
		}

		/// <inheritdoc/>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose method
		/// </summary>
		/// <param name="disposing"><c>true</c> if called by <see cref="Dispose()"/></param>
		protected virtual void Dispose(bool disposing) {
			if (!disposing)
				return;
			Dispose(peImage);
			Dispose(stringsStream);
			Dispose(usStream);
			Dispose(blobStream);
			Dispose(guidStream);
			Dispose(tablesStream);
			var as2 = allStreams;
			if (as2 != null) {
				foreach (var stream in as2.GetSafeEnumerable())
					Dispose(stream);
			}
			peImage = null;
			cor20Header = null;
			mdHeader = null;
			stringsStream = null;
			usStream = null;
			blobStream = null;
			guidStream = null;
			tablesStream = null;
			allStreams = null;
			fieldRidToTypeDefRid = null;
			methodRidToTypeDefRid = null;
			typeDefRidToNestedClasses = null;
		}

		static void Dispose(IDisposable id) {
			if (id != null)
				id.Dispose();
		}
	}
}
