// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// Common base class for #~ and #- metadata classes
	/// </summary>
	abstract class MetadataBase : Metadata {
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
		protected MetadataHeader mdHeader;

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
		protected IList<DotNetStream> allStreams;

		public override bool IsStandalonePortablePdb => isStandalonePortablePdb;
		/// <summary><c>true</c> if this is standalone Portable PDB metadata</summary>
		protected readonly bool isStandalonePortablePdb;

		uint[] fieldRidToTypeDefRid;
		uint[] methodRidToTypeDefRid;
		uint[] eventRidToTypeDefRid;
		uint[] propertyRidToTypeDefRid;
		uint[] gpRidToOwnerRid;
		uint[] gpcRidToOwnerRid;
		uint[] paramRidToOwnerRid;
		Dictionary<uint, List<uint>> typeDefRidToNestedClasses;
		StrongBox<RidList> nonNestedTypes;

		DataReaderFactory mdReaderFactoryToDisposeLater;

		/// <summary>
		/// Sorts a table by key column
		/// </summary>
		protected sealed class SortedTable {
			RowInfo[] rows;

			/// <summary>
			/// Remembers <c>rid</c> and key
			/// </summary>
			[DebuggerDisplay("{rid} {key}")]
			readonly struct RowInfo : IComparable<RowInfo> {
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
				Debug.Assert(keyColumn.Size == 2 || keyColumn.Size == 4);
				rows = new RowInfo[mdTable.Rows + 1];
				if (mdTable.Rows == 0)
					return;
				var reader = mdTable.DataReader;
				reader.Position = (uint)keyColumn.Offset;
				uint increment = (uint)(mdTable.TableInfo.RowSize - keyColumn.Size);
				for (uint i = 1; i <= mdTable.Rows; i++) {
					rows[i] = new RowInfo(i, keyColumn.Unsafe_Read24(ref reader));
					if (i < mdTable.Rows)
						reader.Position += increment;
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
				var list = new List<uint>(endIndex - startIndex);
				for (int i = startIndex; i < endIndex; i++)
					list.Add(rows[i].rid);
				return RidList.Create(list);
			}
		}
		SortedTable eventMapSortedTable;
		SortedTable propertyMapSortedTable;

		public override ImageCor20Header ImageCor20Header => cor20Header;
		public override uint Version => ((uint)mdHeader.MajorVersion << 16) | mdHeader.MinorVersion;
		public override string VersionString => mdHeader.VersionString;
		public override IPEImage PEImage => peImage;
		public override MetadataHeader MetadataHeader => mdHeader;
		public override StringsStream StringsStream => stringsStream;
		public override USStream USStream => usStream;
		public override BlobStream BlobStream => blobStream;
		public override GuidStream GuidStream => guidStream;
		public override TablesStream TablesStream => tablesStream;
		public override PdbStream PdbStream => pdbStream;
		public override IList<DotNetStream> AllStreams => allStreams;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="peImage">The PE image</param>
		/// <param name="cor20Header">The .NET header</param>
		/// <param name="mdHeader">The MD header</param>
		protected MetadataBase(IPEImage peImage, ImageCor20Header cor20Header, MetadataHeader mdHeader) {
			try {
				allStreams = new List<DotNetStream>();
				this.peImage = peImage;
				this.cor20Header = cor20Header;
				this.mdHeader = mdHeader;
				isStandalonePortablePdb = false;
			}
			catch {
				if (!(peImage is null))
					peImage.Dispose();
				throw;
			}
		}

		internal MetadataBase(MetadataHeader mdHeader, bool isStandalonePortablePdb) {
			allStreams = new List<DotNetStream>();
			peImage = null;
			cor20Header = null;
			this.mdHeader = mdHeader;
			this.isStandalonePortablePdb = isStandalonePortablePdb;
		}

		/// <summary>
		/// Initializes the metadata, tables, streams
		/// </summary>
		public void Initialize(DataReaderFactory mdReaderFactory) {
			mdReaderFactoryToDisposeLater = mdReaderFactory;
			uint metadataBaseOffset;
			if (!(peImage is null)) {
				Debug.Assert(mdReaderFactory is null);
				Debug.Assert(!(cor20Header is null));
				metadataBaseOffset = (uint)peImage.ToFileOffset(cor20Header.Metadata.VirtualAddress);
				mdReaderFactory = peImage.DataReaderFactory;
			}
			else {
				Debug.Assert(!(mdReaderFactory is null));
				metadataBaseOffset = 0;
			}
			InitializeInternal(mdReaderFactory, metadataBaseOffset);

			if (tablesStream is null)
				throw new BadImageFormatException("Missing MD stream");
			if (isStandalonePortablePdb && pdbStream is null)
				throw new BadImageFormatException("Missing #Pdb stream");
			InitializeNonExistentHeaps();
		}

		/// <summary>
		/// Creates empty heap objects if they're not present in the metadata
		/// </summary>
		protected void InitializeNonExistentHeaps() {
			if (stringsStream is null)
				stringsStream = new StringsStream();
			if (usStream is null)
				usStream = new USStream();
			if (blobStream is null)
				blobStream = new BlobStream();
			if (guidStream is null)
				guidStream = new GuidStream();
		}

		/// <summary>
		/// Called by <see cref="Initialize(DataReaderFactory)"/>
		/// </summary>
		protected abstract void InitializeInternal(DataReaderFactory mdReaderFactory, uint metadataBaseOffset);

		public override RidList GetTypeDefRidList() => RidList.Create(1, tablesStream.TypeDefTable.Rows);
		public override RidList GetExportedTypeRidList() => RidList.Create(1, tablesStream.ExportedTypeTable.Rows);

		/// <summary>
		/// Binary searches the table for a <c>rid</c> whose key column at index
		/// <paramref name="keyColIndex"/> is equal to <paramref name="key"/>.
		/// </summary>
		/// <param name="tableSource">Table to search</param>
		/// <param name="keyColIndex">Key column index</param>
		/// <param name="key">Key</param>
		/// <returns>The <c>rid</c> of the found row, or 0 if none found</returns>
		protected abstract uint BinarySearch(MDTable tableSource, int keyColIndex, uint key);

		/// <summary>
		/// Finds all rows owned by <paramref name="key"/> in table <paramref name="tableSource"/>
		/// whose index is <paramref name="keyColIndex"/>
		/// </summary>
		/// <param name="tableSource">Table to search</param>
		/// <param name="keyColIndex">Key column index</param>
		/// <param name="key">Key</param>
		/// <returns>A <see cref="RidList"/> instance</returns>
		protected RidList FindAllRows(MDTable tableSource, int keyColIndex, uint key) {
			uint startRid = BinarySearch(tableSource, keyColIndex, key);
			if (tableSource.IsInvalidRID(startRid))
				return RidList.Empty;
			uint endRid = startRid + 1;
			var column = tableSource.TableInfo.Columns[keyColIndex];
			for (; startRid > 1; startRid--) {
				if (!tablesStream.TryReadColumn24(tableSource, startRid - 1, column, out uint key2))
					break;	// Should never happen since startRid is valid
				if (key != key2)
					break;
			}
			for (; endRid <= tableSource.Rows; endRid++) {
				if (!tablesStream.TryReadColumn24(tableSource, endRid, column, out uint key2))
					break;	// Should never happen since endRid is valid
				if (key != key2)
					break;
			}
			return RidList.Create(startRid, endRid - startRid);
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
		protected virtual RidList FindAllRowsUnsorted(MDTable tableSource, int keyColIndex, uint key) => FindAllRows(tableSource, keyColIndex, key);

		public override RidList GetInterfaceImplRidList(uint typeDefRid) => FindAllRowsUnsorted(tablesStream.InterfaceImplTable, 0, typeDefRid);

		public override RidList GetGenericParamRidList(Table table, uint rid) {
			if (!CodedToken.TypeOrMethodDef.Encode(new MDToken(table, rid), out uint codedToken))
				return RidList.Empty;
			return FindAllRowsUnsorted(tablesStream.GenericParamTable, 2, codedToken);
		}

		public override RidList GetGenericParamConstraintRidList(uint genericParamRid) =>
			FindAllRowsUnsorted(tablesStream.GenericParamConstraintTable, 0, genericParamRid);

		public override RidList GetCustomAttributeRidList(Table table, uint rid) {
			if (!CodedToken.HasCustomAttribute.Encode(new MDToken(table, rid), out uint codedToken))
				return RidList.Empty;
			return FindAllRowsUnsorted(tablesStream.CustomAttributeTable, 0, codedToken);
		}

		public override RidList GetDeclSecurityRidList(Table table, uint rid) {
			if (!CodedToken.HasDeclSecurity.Encode(new MDToken(table, rid), out uint codedToken))
				return RidList.Empty;
			return FindAllRowsUnsorted(tablesStream.DeclSecurityTable, 1, codedToken);
		}

		public override RidList GetMethodSemanticsRidList(Table table, uint rid) {
			if (!CodedToken.HasSemantic.Encode(new MDToken(table, rid), out uint codedToken))
				return RidList.Empty;
			return FindAllRowsUnsorted(tablesStream.MethodSemanticsTable, 2, codedToken);
		}

		public override RidList GetMethodImplRidList(uint typeDefRid) => FindAllRowsUnsorted(tablesStream.MethodImplTable, 0, typeDefRid);

		public override uint GetClassLayoutRid(uint typeDefRid) {
			var list = FindAllRowsUnsorted(tablesStream.ClassLayoutTable, 2, typeDefRid);
			return list.Count == 0 ? 0 : list[0];
		}

		override public uint GetFieldLayoutRid(uint fieldRid) {
			var list = FindAllRowsUnsorted(tablesStream.FieldLayoutTable, 1, fieldRid);
			return list.Count == 0 ? 0 : list[0];
		}

		public override uint GetFieldMarshalRid(Table table, uint rid) {
			if (!CodedToken.HasFieldMarshal.Encode(new MDToken(table, rid), out uint codedToken))
				return 0;
			var list = FindAllRowsUnsorted(tablesStream.FieldMarshalTable, 0, codedToken);
			return list.Count == 0 ? 0 : list[0];
		}

		public override uint GetFieldRVARid(uint fieldRid) {
			var list = FindAllRowsUnsorted(tablesStream.FieldRVATable, 1, fieldRid);
			return list.Count == 0 ? 0 : list[0];
		}

		public override uint GetImplMapRid(Table table, uint rid) {
			if (!CodedToken.MemberForwarded.Encode(new MDToken(table, rid), out uint codedToken))
				return 0;
			var list = FindAllRowsUnsorted(tablesStream.ImplMapTable, 1, codedToken);
			return list.Count == 0 ? 0 : list[0];
		}

		public override uint GetNestedClassRid(uint typeDefRid) {
			var list = FindAllRowsUnsorted(tablesStream.NestedClassTable, 0, typeDefRid);
			return list.Count == 0 ? 0 : list[0];
		}

		public override uint GetEventMapRid(uint typeDefRid) {
			// The EventMap and PropertyMap tables can only be trusted to be sorted if it's
			// an NGen image and it's the normal #- stream. The IsSorted bit must not be used
			// to check whether the tables are sorted. See coreclr: md/inc/metamodel.h / IsVerified()
			if (eventMapSortedTable is null)
				Interlocked.CompareExchange(ref eventMapSortedTable, new SortedTable(tablesStream.EventMapTable, 0), null);
			var list = eventMapSortedTable.FindAllRows(typeDefRid);
			return list.Count == 0 ? 0 : list[0];
		}

		public override uint GetPropertyMapRid(uint typeDefRid) {
			// Always unsorted, see comment in GetEventMapRid() above
			if (propertyMapSortedTable is null)
				Interlocked.CompareExchange(ref propertyMapSortedTable, new SortedTable(tablesStream.PropertyMapTable, 0), null);
			var list = propertyMapSortedTable.FindAllRows(typeDefRid);
			return list.Count == 0 ? 0 : list[0];
		}

		public override uint GetConstantRid(Table table, uint rid) {
			if (!CodedToken.HasConstant.Encode(new MDToken(table, rid), out uint codedToken))
				return 0;
			var list = FindAllRowsUnsorted(tablesStream.ConstantTable, 2, codedToken);
			return list.Count == 0 ? 0 : list[0];
		}

		public override uint GetOwnerTypeOfField(uint fieldRid) {
			if (fieldRidToTypeDefRid is null)
				InitializeInverseFieldOwnerRidList();
			uint index = fieldRid - 1;
			if (index >= fieldRidToTypeDefRid.LongLength)
				return 0;
			return fieldRidToTypeDefRid[index];
		}

		void InitializeInverseFieldOwnerRidList() {
			if (!(fieldRidToTypeDefRid is null))
				return;
			var newFieldRidToTypeDefRid = new uint[tablesStream.FieldTable.Rows];
			var ownerList = GetTypeDefRidList();
			for (int i = 0; i < ownerList.Count; i++) {
				var ownerRid = ownerList[i];
				var fieldList = GetFieldRidList(ownerRid);
				for (int j = 0; j < fieldList.Count; j++) {
					uint ridIndex = fieldList[j] - 1;
					if (newFieldRidToTypeDefRid[ridIndex] != 0)
						continue;
					newFieldRidToTypeDefRid[ridIndex] = ownerRid;
				}
			}
			Interlocked.CompareExchange(ref fieldRidToTypeDefRid, newFieldRidToTypeDefRid, null);
		}

		public override uint GetOwnerTypeOfMethod(uint methodRid) {
			if (methodRidToTypeDefRid is null)
				InitializeInverseMethodOwnerRidList();
			uint index = methodRid - 1;
			if (index >= methodRidToTypeDefRid.LongLength)
				return 0;
			return methodRidToTypeDefRid[index];
		}

		void InitializeInverseMethodOwnerRidList() {
			if (!(methodRidToTypeDefRid is null))
				return;
			var newMethodRidToTypeDefRid = new uint[tablesStream.MethodTable.Rows];
			var ownerList = GetTypeDefRidList();
			for (int i = 0; i < ownerList.Count; i++) {
				var ownerRid = ownerList[i];
				var methodList = GetMethodRidList(ownerRid);
				for (int j = 0; j < methodList.Count; j++) {
					uint ridIndex = methodList[j] - 1;
					if (newMethodRidToTypeDefRid[ridIndex] != 0)
						continue;
					newMethodRidToTypeDefRid[ridIndex] = ownerRid;
				}
			}
			Interlocked.CompareExchange(ref methodRidToTypeDefRid, newMethodRidToTypeDefRid, null);
		}

		public override uint GetOwnerTypeOfEvent(uint eventRid) {
			if (eventRidToTypeDefRid is null)
				InitializeInverseEventOwnerRidList();
			uint index = eventRid - 1;
			if (index >= eventRidToTypeDefRid.LongLength)
				return 0;
			return eventRidToTypeDefRid[index];
		}

		void InitializeInverseEventOwnerRidList() {
			if (!(eventRidToTypeDefRid is null))
				return;
			var newEventRidToTypeDefRid = new uint[tablesStream.EventTable.Rows];
			var ownerList = GetTypeDefRidList();
			for (int i = 0; i < ownerList.Count; i++) {
				var ownerRid = ownerList[i];
				var eventList = GetEventRidList(GetEventMapRid(ownerRid));
				for (int j = 0; j < eventList.Count; j++) {
					uint ridIndex = eventList[j] - 1;
					if (newEventRidToTypeDefRid[ridIndex] != 0)
						continue;
					newEventRidToTypeDefRid[ridIndex] = ownerRid;
				}
			}
			Interlocked.CompareExchange(ref eventRidToTypeDefRid, newEventRidToTypeDefRid, null);
		}

		public override uint GetOwnerTypeOfProperty(uint propertyRid) {
			if (propertyRidToTypeDefRid is null)
				InitializeInversePropertyOwnerRidList();
			uint index = propertyRid - 1;
			if (index >= propertyRidToTypeDefRid.LongLength)
				return 0;
			return propertyRidToTypeDefRid[index];
		}

		void InitializeInversePropertyOwnerRidList() {
			if (!(propertyRidToTypeDefRid is null))
				return;
			var newPropertyRidToTypeDefRid = new uint[tablesStream.PropertyTable.Rows];
			var ownerList = GetTypeDefRidList();
			for (int i = 0; i < ownerList.Count; i++) {
				var ownerRid = ownerList[i];
				var propertyList = GetPropertyRidList(GetPropertyMapRid(ownerRid));
				for (int j = 0; j < propertyList.Count; j++) {
					uint ridIndex = propertyList[j] - 1;
					if (newPropertyRidToTypeDefRid[ridIndex] != 0)
						continue;
					newPropertyRidToTypeDefRid[ridIndex] = ownerRid;
				}
			}
			Interlocked.CompareExchange(ref propertyRidToTypeDefRid, newPropertyRidToTypeDefRid, null);
		}

		public override uint GetOwnerOfGenericParam(uint gpRid) {
			// Don't use GenericParam.Owner column. If the GP table is sorted, it's
			// possible to have two blocks of GPs with the same owner. Only one of the
			// blocks is the "real" generic params for the owner. Of course, rarely
			// if ever will this occur, but could happen if some obfuscator has
			// added it.

			if (gpRidToOwnerRid is null)
				InitializeInverseGenericParamOwnerRidList();
			uint index = gpRid - 1;
			if (index >= gpRidToOwnerRid.LongLength)
				return 0;
			return gpRidToOwnerRid[index];
		}

		void InitializeInverseGenericParamOwnerRidList() {
			if (!(gpRidToOwnerRid is null))
				return;
			var gpTable = tablesStream.GenericParamTable;
			var newGpRidToOwnerRid = new uint[gpTable.Rows];

			// Find all owners by reading the GenericParam.Owner column
			var ownerCol = gpTable.TableInfo.Columns[2];
			var ownersDict = new Dictionary<uint, bool>();
			for (uint rid = 1; rid <= gpTable.Rows; rid++) {
				if (!tablesStream.TryReadColumn24(gpTable, rid, ownerCol, out uint owner))
					continue;
				ownersDict[owner] = true;
			}

			// Now that we have the owners, find all the generic params they own. An obfuscated
			// module could have 2+ owners pointing to the same generic param row.
			var owners = new List<uint>(ownersDict.Keys);
			owners.Sort();
			for (int i = 0; i < owners.Count; i++) {
				if (!CodedToken.TypeOrMethodDef.Decode(owners[i], out uint ownerToken))
					continue;
				var ridList = GetGenericParamRidList(MDToken.ToTable(ownerToken), MDToken.ToRID(ownerToken));
				for (int j = 0; j < ridList.Count; j++) {
					uint ridIndex = ridList[j] - 1;
					if (newGpRidToOwnerRid[ridIndex] != 0)
						continue;
					newGpRidToOwnerRid[ridIndex] = owners[i];
				}
			}
			Interlocked.CompareExchange(ref gpRidToOwnerRid, newGpRidToOwnerRid, null);
		}

		public override uint GetOwnerOfGenericParamConstraint(uint gpcRid) {
			// Don't use GenericParamConstraint.Owner column for the same reason
			// as described in GetOwnerOfGenericParam().

			if (gpcRidToOwnerRid is null)
				InitializeInverseGenericParamConstraintOwnerRidList();
			uint index = gpcRid - 1;
			if (index >= gpcRidToOwnerRid.LongLength)
				return 0;
			return gpcRidToOwnerRid[index];
		}

		void InitializeInverseGenericParamConstraintOwnerRidList() {
			if (!(gpcRidToOwnerRid is null))
				return;
			var gpcTable = tablesStream.GenericParamConstraintTable;
			var newGpcRidToOwnerRid = new uint[gpcTable.Rows];

			var ownerCol = gpcTable.TableInfo.Columns[0];
			var ownersDict = new Dictionary<uint, bool>();
			for (uint rid = 1; rid <= gpcTable.Rows; rid++) {
				if (!tablesStream.TryReadColumn24(gpcTable, rid, ownerCol, out uint owner))
					continue;
				ownersDict[owner] = true;
			}

			var owners = new List<uint>(ownersDict.Keys);
			owners.Sort();
			for (int i = 0; i < owners.Count; i++) {
				uint ownerToken = owners[i];
				var ridList = GetGenericParamConstraintRidList(ownerToken);
				for (int j = 0; j < ridList.Count; j++) {
					uint ridIndex = ridList[j] - 1;
					if (newGpcRidToOwnerRid[ridIndex] != 0)
						continue;
					newGpcRidToOwnerRid[ridIndex] = ownerToken;
				}
			}
			Interlocked.CompareExchange(ref gpcRidToOwnerRid, newGpcRidToOwnerRid, null);
		}

		public override uint GetOwnerOfParam(uint paramRid) {
			if (paramRidToOwnerRid is null)
				InitializeInverseParamOwnerRidList();
			uint index = paramRid - 1;
			if (index >= paramRidToOwnerRid.LongLength)
				return 0;
			return paramRidToOwnerRid[index];
		}

		void InitializeInverseParamOwnerRidList() {
			if (!(paramRidToOwnerRid is null))
				return;

			var newParamRidToOwnerRid = new uint[tablesStream.ParamTable.Rows];
			var table = tablesStream.MethodTable;
			for (uint rid = 1; rid <= table.Rows; rid++) {
				var ridList = GetParamRidList(rid);
				for (int j = 0; j < ridList.Count; j++) {
					uint ridIndex = ridList[j] - 1;
					if (newParamRidToOwnerRid[ridIndex] != 0)
						continue;
					newParamRidToOwnerRid[ridIndex] = rid;
				}
			}
			Interlocked.CompareExchange(ref paramRidToOwnerRid, newParamRidToOwnerRid, null);
		}

		public override RidList GetNestedClassRidList(uint typeDefRid) {
			if (typeDefRidToNestedClasses is null)
				InitializeNestedClassesDictionary();
			if (typeDefRidToNestedClasses.TryGetValue(typeDefRid, out var ridList))
				return RidList.Create(ridList);
			return RidList.Empty;
		}

		void InitializeNestedClassesDictionary() {
			var table = tablesStream.NestedClassTable;
			var destTable = tablesStream.TypeDefTable;

			Dictionary<uint, bool> validTypeDefRids = null;
			var typeDefRidList = GetTypeDefRidList();
			if ((uint)typeDefRidList.Count != destTable.Rows) {
				validTypeDefRids = new Dictionary<uint, bool>(typeDefRidList.Count);
				for (int i = 0; i < typeDefRidList.Count; i++)
					validTypeDefRids[typeDefRidList[i]] = true;
			}

			var nestedRidsDict = new Dictionary<uint, bool>((int)table.Rows);
			var nestedRids = new List<uint>((int)table.Rows);	// Need it so we add the rids in correct order
			for (uint rid = 1; rid <= table.Rows; rid++) {
				if (!(validTypeDefRids is null) && !validTypeDefRids.ContainsKey(rid))
					continue;
				if (!tablesStream.TryReadNestedClassRow(rid, out var row))
					continue;	// Should never happen since rid is valid
				if (!destTable.IsValidRID(row.NestedClass) || !destTable.IsValidRID(row.EnclosingClass))
					continue;
				if (nestedRidsDict.ContainsKey(row.NestedClass))
					continue;
				nestedRidsDict[row.NestedClass] = true;
				nestedRids.Add(row.NestedClass);
			}

			var newTypeDefRidToNestedClasses = new Dictionary<uint, List<uint>>();
			int count = nestedRids.Count;
			for (int i = 0; i < count; i++) {
				var nestedRid = nestedRids[i];
				if (!tablesStream.TryReadNestedClassRow(GetNestedClassRid(nestedRid), out var row))
					continue;
				if (!newTypeDefRidToNestedClasses.TryGetValue(row.EnclosingClass, out var ridList))
					newTypeDefRidToNestedClasses[row.EnclosingClass] = ridList = new List<uint>();
				ridList.Add(nestedRid);
			}

			var newNonNestedTypes = new List<uint>((int)(destTable.Rows - nestedRidsDict.Count));
			for (uint rid = 1; rid <= destTable.Rows; rid++) {
				if (!(validTypeDefRids is null) && !validTypeDefRids.ContainsKey(rid))
					continue;
				if (nestedRidsDict.ContainsKey(rid))
					continue;
				newNonNestedTypes.Add(rid);
			}

			Interlocked.CompareExchange(ref nonNestedTypes, new StrongBox<RidList>(RidList.Create(newNonNestedTypes)), null);

			// Initialize this one last since it's tested by the callers of this method
			Interlocked.CompareExchange(ref typeDefRidToNestedClasses, newTypeDefRidToNestedClasses, null);
		}

		public override RidList GetNonNestedClassRidList() {
			// Check typeDefRidToNestedClasses and not nonNestedTypes since
			// InitializeNestedClassesDictionary() writes to typeDefRidToNestedClasses last.
			if (typeDefRidToNestedClasses is null)
				InitializeNestedClassesDictionary();
			return nonNestedTypes.Value;
		}

		public override RidList GetLocalScopeRidList(uint methodRid) => FindAllRows(tablesStream.LocalScopeTable, 0, methodRid);

		public override uint GetStateMachineMethodRid(uint methodRid) {
			var list = FindAllRows(tablesStream.StateMachineMethodTable, 0, methodRid);
			return list.Count == 0 ? 0 : list[0];
		}

		public override RidList GetCustomDebugInformationRidList(Table table, uint rid) {
			if (!CodedToken.HasCustomDebugInformation.Encode(new MDToken(table, rid), out uint codedToken))
				return RidList.Empty;
			return FindAllRows(tablesStream.CustomDebugInformationTable, 0, codedToken);
		}

		public override void Dispose() {
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
			peImage?.Dispose();
			stringsStream?.Dispose();
			usStream?.Dispose();
			blobStream?.Dispose();
			guidStream?.Dispose();
			tablesStream?.Dispose();
			var as2 = allStreams;
			if (!(as2 is null)) {
				foreach (var stream in as2)
					stream?.Dispose();
			}
			mdReaderFactoryToDisposeLater?.Dispose();
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
			mdReaderFactoryToDisposeLater = null;
		}
	}
}
