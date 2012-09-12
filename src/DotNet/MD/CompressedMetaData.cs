using System;
using dot10.IO;
using dot10.PE;

namespace dot10.DotNet.MD {
	/// <summary>
	/// Used when a #~ stream is present in the metadata
	/// </summary>
	class CompressedMetaData : MetaData {
		/// <inheritdoc/>
		public CompressedMetaData(IPEImage peImage, ImageCor20Header cor20Header, MetaDataHeader mdHeader)
			: base(peImage, cor20Header, mdHeader) {
		}

		/// <inheritdoc/>
		public override void Initialize() {
			IImageStream imageStream = null;
			DotNetStream dns = null;
			try {
				var mdRva = cor20Header.MetaData.VirtualAddress;
				for (int i = mdHeader.StreamHeaders.Count - 1; i >= 0; i--) {
					var sh = mdHeader.StreamHeaders[i];
					var rva = mdRva + sh.Offset;
					imageStream = peImage.CreateStream(rva, sh.StreamSize);
					switch (sh.Name) {
					case "#Strings":
						if (stringsStream == null) {
							stringsStream = new StringsStream(imageStream, sh);
							imageStream = null;
							allStreams.Add(stringsStream);
							continue;
						}
						break;

					case "#US":
						if (usStream == null) {
							usStream = new USStream(imageStream, sh);
							imageStream = null;
							allStreams.Add(usStream);
							continue;
						}
						break;

					case "#Blob":
						if (blobStream == null) {
							blobStream = new BlobStream(imageStream, sh);
							imageStream = null;
							allStreams.Add(blobStream);
							continue;
						}
						break;

					case "#GUID":
						if (guidStream == null) {
							guidStream = new GuidStream(imageStream, sh);
							imageStream = null;
							allStreams.Add(guidStream);
							continue;
						}
						break;

					case "#~":
						if (tablesStream == null) {
							tablesStream = new CompressedTablesStream(imageStream, sh);
							imageStream = null;
							allStreams.Add(tablesStream);
							continue;
						}
						break;
					}
					dns = new DotNetStream(imageStream, sh);
					imageStream = null;
					allStreams.Add(dns);
					dns = null;
				}
				allStreams.Reverse();

				if (tablesStream == null)
					throw new BadImageFormatException("Missing MD stream");
				tablesStream.Initialize(peImage);
			}
			finally {
				if (imageStream != null)
					imageStream.Dispose();
				if (dns != null)
					dns.Dispose();
			}
		}

		/// <inheritdoc/>
		public override RidList GetFieldRidList(uint typeDefRid) {
			return GetRidList(Table.TypeDef, typeDefRid, 4, Table.Field);
		}

		/// <inheritdoc/>
		public override RidList GetMethodRidList(uint typeDefRid) {
			return GetRidList(Table.TypeDef, typeDefRid, 5, Table.Method);
		}

		/// <inheritdoc/>
		public override RidList GetParamRidList(uint methodRid) {
			return GetRidList(Table.Method, methodRid, 5, Table.Param);
		}

		/// <inheritdoc/>
		public override RidList GetEventRidList(uint eventMapRid) {
			return GetRidList(Table.EventMap, eventMapRid, 1, Table.Event);
		}

		/// <inheritdoc/>
		public override RidList GetPropertyRidList(uint propertyMapRid) {
			return GetRidList(Table.PropertyMap, propertyMapRid, 1, Table.Property);
		}

		/// <summary>
		/// Gets a rid list (eg. field list)
		/// </summary>
		/// <param name="tableSource">Source table, eg. <c>TypeDef</c></param>
		/// <param name="tableSourceRid">Row ID in <paramref name="tableSource"/></param>
		/// <param name="colIndex">Column index in <paramref name="tableSource"/>, eg. 4 for <c>TypeDef.FieldList</c></param>
		/// <param name="tableDest">Destination table, eg. <c>Field</c></param>
		/// <returns>A new <see cref="RidList"/> instance</returns>
		RidList GetRidList(Table tableSource, uint tableSourceRid, int colIndex, Table tableDest) {
			var column = tablesStream.Get(tableSource).TableInfo.Columns[colIndex];
			uint startRid;
			if (!tablesStream.ReadColumn(tableSource, tableSourceRid, column, out startRid))
				return ContiguousRidList.Empty;
			uint nextListRid;
			bool hasNext = tablesStream.ReadColumn(tableSource, tableSourceRid + 1, column, out nextListRid);

			uint lastRid = tablesStream.Get(tableDest).Rows + 1;
			if (startRid == 0 || startRid >= lastRid)
				return ContiguousRidList.Empty;
			uint endRid = hasNext ? nextListRid : lastRid;
			if (endRid < startRid)
				endRid = startRid;
			if (endRid > lastRid)
				endRid = lastRid;
			return new ContiguousRidList(startRid, endRid - startRid);
		}

		/// <inheritdoc/>
		protected override uint BinarySearch(Table tableSource, int keyColIndex, uint key) {
			var table = tablesStream.Get(tableSource);
			if (table == null)
				return 0;
			var keyColumn = table.TableInfo.Columns[keyColIndex];
			uint ridLo = 1, ridHi = table.Rows;
			while (ridLo <= ridHi) {
				uint rid = (ridLo + ridHi) / 2;
				uint key2;
				if (!tablesStream.ReadColumn(tableSource, rid, keyColumn, out key2))
					break;	// Never happens since rid is valid
				if (key == key2)
					return rid;
				if (key2 > key)
					ridHi = rid - 1;
				else
					ridLo = rid + 1;
			}

			return 0;
		}
	}
}
