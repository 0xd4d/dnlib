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
		public override uint GetFieldRange(uint typeDefRid, out uint startRid) {
			return GetListRange(Table.TypeDef, typeDefRid, 4, Table.Field, out startRid);
		}

		/// <inheritdoc/>
		public override uint ToFieldRid(uint listRid) {
			return listRid;
		}

		/// <inheritdoc/>
		public override uint GetMethodRange(uint typeDefRid, out uint startRid) {
			return GetListRange(Table.TypeDef, typeDefRid, 5, Table.Method, out startRid);
		}

		/// <inheritdoc/>
		public override uint ToMethodRid(uint listRid) {
			return listRid;
		}

		/// <inheritdoc/>
		public override uint GetParamRange(uint methodRid, out uint startRid) {
			return GetListRange(Table.Method, methodRid, 5, Table.Param, out startRid);
		}

		/// <inheritdoc/>
		public override uint ToParamRid(uint listRid) {
			return listRid;
		}

		/// <inheritdoc/>
		public override uint GetEventMapRange(uint eventMapRid, out uint startRid) {
			return GetListRange(Table.EventMap, eventMapRid, 1, Table.Event, out startRid);
		}

		/// <inheritdoc/>
		public override uint ToEventRid(uint listRid) {
			return listRid;
		}

		/// <inheritdoc/>
		public override uint GetPropertyMapRange(uint propertyMapRid, out uint startRid) {
			return GetListRange(Table.PropertyMap, propertyMapRid, 1, Table.Property, out startRid);
		}

		/// <inheritdoc/>
		public override uint ToPropertyRid(uint listRid) {
			return listRid;
		}

		/// <summary>
		/// Gets a list range (eg. field list)
		/// </summary>
		/// <param name="tableSource">Source table, eg. <c>TypeDef</c></param>
		/// <param name="tableSourceRid">Row ID in <paramref name="tableSource"/></param>
		/// <param name="colIndex">Column index in <paramref name="tableSource"/>, eg. 4 for <c>TypeDef.FieldList</c></param>
		/// <param name="tableDest">Destination table, eg. <c>Field</c></param>
		/// <param name="startRid">Start rid in <paramref name="tableDest"/></param>
		/// <returns>Size of range starting from <paramref name="startRid"/></returns>
		uint GetListRange(Table tableSource, uint tableSourceRid, int colIndex, Table tableDest, out uint startRid) {
			var column = tablesStream.Get(tableSource).TableInfo.Columns[colIndex];
			if (!tablesStream.ReadColumn(tableSource, tableSourceRid, column, out startRid))
				return 0;
			uint nextListRid;
			bool hasNext = tablesStream.ReadColumn(tableSource, tableSourceRid + 1, column, out nextListRid);

			uint lastRid = tablesStream.Get(tableDest).Rows + 1;
			if (startRid == 0 || startRid >= lastRid)
				return 0;
			uint endRid = hasNext ? nextListRid : lastRid;
			if (endRid < startRid)
				endRid = startRid;
			if (endRid > lastRid)
				endRid = lastRid;
			return endRid - startRid;
		}
	}
}
