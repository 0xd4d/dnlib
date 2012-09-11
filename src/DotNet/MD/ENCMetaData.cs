using System;
using dot10.IO;
using dot10.PE;

namespace dot10.DotNet.MD {
	/// <summary>
	/// Used when a #- stream is present in the metadata
	/// </summary>
	class ENCMetaData : MetaData {
		static readonly UTF8String DeletedName = new UTF8String("_Deleted");
		bool hasMethodPtr, hasFieldPtr, hasParamPtr, hasEventPtr, hasPropertyPtr;
		bool hasDeletedRows;

		/// <inheritdoc/>
		public ENCMetaData(IPEImage peImage, ImageCor20Header cor20Header, MetaDataHeader mdHeader)
			: base(peImage, cor20Header, mdHeader) {
		}

		/// <inheritdoc/>
		public override void Initialize() {
			IImageStream imageStream = null;
			DotNetStream dns = null;
			try {
				var mdRva = cor20Header.MetaData.VirtualAddress;
				foreach (var sh in mdHeader.StreamHeaders) {
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

					case "#-":
						if (tablesStream == null) {
							tablesStream = new ENCTablesStream(imageStream, sh);
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

				if (tablesStream == null)
					throw new BadImageFormatException("Missing MD stream");
				tablesStream.Initialize(peImage);

				// The pointer tables are used iff row count != 0
				hasFieldPtr = tablesStream.Get(Table.FieldPtr).Rows > 0;
				hasMethodPtr = tablesStream.Get(Table.MethodPtr).Rows > 0;
				hasParamPtr = tablesStream.Get(Table.ParamPtr).Rows > 0;
				hasEventPtr = tablesStream.Get(Table.EventPtr).Rows > 0;
				hasPropertyPtr = tablesStream.Get(Table.PropertyPtr).Rows > 0;
				hasDeletedRows = tablesStream.HasDelete;
			}
			finally {
				if (imageStream != null)
					imageStream.Dispose();
				if (dns != null)
					dns.Dispose();
			}
		}

		/// <summary>
		/// Converts a logical <c>Field</c> rid to a physical <c>Field</c> rid
		/// </summary>
		/// <param name="listRid">A valid rid</param>
		/// <returns>Converted rid or any invalid rid value if <paramref name="listRid"/> is invalid</returns>
		uint ToFieldRid(uint listRid) {
			if (!hasFieldPtr)
				return listRid;
			uint listValue;
			return tablesStream.ReadColumn(Table.FieldPtr, listRid, 0, out listValue) ? listValue : 0;
		}

		/// <summary>
		/// Converts a logical <c>Method</c> rid to a physical <c>Method</c> rid
		/// </summary>
		/// <param name="listRid">A valid rid</param>
		/// <returns>Converted rid or any invalid rid value if <paramref name="listRid"/> is invalid</returns>
		uint ToMethodRid(uint listRid) {
			if (!hasMethodPtr)
				return listRid;
			uint listValue;
			return tablesStream.ReadColumn(Table.MethodPtr, listRid, 0, out listValue) ? listValue : 0;
		}

		/// <summary>
		/// Converts a logical <c>Param</c> rid to a physical <c>Param</c> rid
		/// </summary>
		/// <param name="listRid">A valid rid</param>
		/// <returns>Converted rid or any invalid rid value if <paramref name="listRid"/> is invalid</returns>
		uint ToParamRid(uint listRid) {
			if (!hasParamPtr)
				return listRid;
			uint listValue;
			return tablesStream.ReadColumn(Table.ParamPtr, listRid, 0, out listValue) ? listValue : 0;
		}

		/// <summary>
		/// Converts a logical <c>Event</c> rid to a physical <c>Event</c> rid
		/// </summary>
		/// <param name="listRid">A valid rid</param>
		/// <returns>Converted rid or any invalid rid value if <paramref name="listRid"/> is invalid</returns>
		uint ToEventRid(uint listRid) {
			if (!hasEventPtr)
				return listRid;
			uint listValue;
			return tablesStream.ReadColumn(Table.EventPtr, listRid, 0, out listValue) ? listValue : 0;
		}

		/// <summary>
		/// Converts a logical <c>Property</c> rid to a physical <c>Property</c> rid
		/// </summary>
		/// <param name="listRid">A valid rid</param>
		/// <returns>Converted rid or any invalid rid value if <paramref name="listRid"/> is invalid</returns>
		uint ToPropertyRid(uint listRid) {
			if (!hasPropertyPtr)
				return listRid;
			uint listValue;
			return tablesStream.ReadColumn(Table.PropertyPtr, listRid, 0, out listValue) ? listValue : 0;
		}

		/// <inheritdoc/>
		public override RidRange GetFieldRange(uint typeDefRid) {
			var range = GetListRange(Table.TypeDef, typeDefRid, 4, Table.Field);
			if (range.Length == 0 || (!hasFieldPtr && !hasDeletedRows))
				return range;

			var destTable = tablesStream.Get(Table.Field);
			var newRange = new RandomRidRange((int)range.Length);
			for (uint i = 0; i < range.Length; i++) {
				var rid = ToFieldRid(range[i]);
				if (rid == 0 || rid > destTable.Rows)
					continue;
				if (hasDeletedRows) {
					// It's a deleted row if RTSpecialName is set and name is "_Deleted"
					var row = tablesStream.ReadFieldRow(rid);
					if (row == null)
						continue;	// Should never happen since rid is valid
					if ((row.Flags & (uint)FieldAttributes.RTSpecialName) != 0) {
						var name = stringsStream.Read(row.Name);
						if ((object)name != null && name == DeletedName)
							continue;	// ignore this deleted row
					}
				}
				// It's a valid non-deleted rid so add it
				newRange.Add(rid);
			}
			return newRange;
		}

		/// <inheritdoc/>
		public override RidRange GetMethodRange(uint typeDefRid) {
			var range = GetListRange(Table.TypeDef, typeDefRid, 5, Table.Method);
			if (range.Length == 0 || (!hasMethodPtr && !hasDeletedRows))
				return range;

			var destTable = tablesStream.Get(Table.Method);
			var newRange = new RandomRidRange((int)range.Length);
			for (uint i = 0; i < range.Length; i++) {
				var rid = ToMethodRid(range[i]);
				if (rid == 0 || rid > destTable.Rows)
					continue;
				if (hasDeletedRows) {
					// It's a deleted row if RTSpecialName is set and name is "_Deleted"
					var row = tablesStream.ReadMethodRow(rid);
					if (row == null)
						continue;	// Should never happen since rid is valid
					if ((row.Flags & (uint)MethodAttributes.RTSpecialName) != 0) {
						var name = stringsStream.Read(row.Name);
						if ((object)name != null && name == DeletedName)
							continue;	// ignore this deleted row
					}
				}
				// It's a valid non-deleted rid so add it
				newRange.Add(rid);
			}
			return newRange;
		}

		/// <inheritdoc/>
		public override RidRange GetParamRange(uint methodRid) {
			var range = GetListRange(Table.Method, methodRid, 5, Table.Param);
			if (range.Length == 0 || !hasParamPtr)
				return range;

			var destTable = tablesStream.Get(Table.Param);
			var newRange = new RandomRidRange((int)range.Length);
			for (uint i = 0; i < range.Length; i++) {
				var rid = ToParamRid(range[i]);
				if (rid == 0 || rid > destTable.Rows)
					continue;
				newRange.Add(rid);
			}
			return newRange;
		}

		/// <inheritdoc/>
		public override RidRange GetEventRange(uint eventMapRid) {
			var range = GetListRange(Table.EventMap, eventMapRid, 1, Table.Event);
			if (range.Length == 0 || (!hasEventPtr && !hasDeletedRows))
				return range;

			var destTable = tablesStream.Get(Table.Event);
			var newRange = new RandomRidRange((int)range.Length);
			for (uint i = 0; i < range.Length; i++) {
				var rid = ToEventRid(range[i]);
				if (rid == 0 || rid > destTable.Rows)
					continue;
				if (hasDeletedRows) {
					// It's a deleted row if RTSpecialName is set and name is "_Deleted"
					var row = tablesStream.ReadEventRow(rid);
					if (row == null)
						continue;	// Should never happen since rid is valid
					if ((row.EventFlags & (uint)EventAttributes.RTSpecialName) != 0) {
						var name = stringsStream.Read(row.Name);
						if ((object)name != null && name == DeletedName)
							continue;	// ignore this deleted row
					}
				}
				// It's a valid non-deleted rid so add it
				newRange.Add(rid);
			}
			return newRange;
		}

		/// <inheritdoc/>
		public override RidRange GetPropertyRange(uint propertyMapRid) {
			var range = GetListRange(Table.PropertyMap, propertyMapRid, 1, Table.Property);
			if (range.Length == 0 || (!hasPropertyPtr && !hasDeletedRows))
				return range;

			var destTable = tablesStream.Get(Table.Property);
			var newRange = new RandomRidRange((int)range.Length);
			for (uint i = 0; i < range.Length; i++) {
				var rid = ToPropertyRid(range[i]);
				if (rid == 0 || rid > destTable.Rows)
					continue;
				if (hasDeletedRows) {
					// It's a deleted row if RTSpecialName is set and name is "_Deleted"
					var row = tablesStream.ReadPropertyRow(rid);
					if (row == null)
						continue;	// Should never happen since rid is valid
					if ((row.PropFlags & (uint)PropertyAttributes.RTSpecialName) != 0) {
						var name = stringsStream.Read(row.Name);
						if ((object)name != null && name == DeletedName)
							continue;	// ignore this deleted row
					}
				}
				// It's a valid non-deleted rid so add it
				newRange.Add(rid);
			}
			return newRange;
		}

		/// <summary>
		/// Gets a list range (eg. field list)
		/// </summary>
		/// <param name="tableSource">Source table, eg. <c>TypeDef</c></param>
		/// <param name="tableSourceRid">Row ID in <paramref name="tableSource"/></param>
		/// <param name="colIndex">Column index in <paramref name="tableSource"/>, eg. 4 for <c>TypeDef.FieldList</c></param>
		/// <param name="tableDest">Destination table, eg. <c>Field</c></param>
		/// <returns>A new <see cref="RidRange"/> instance</returns>
		ContiguousRidRange GetListRange(Table tableSource, uint tableSourceRid, int colIndex, Table tableDest) {
			var column = tablesStream.Get(tableSource).TableInfo.Columns[colIndex];
			uint startRid;
			if (!tablesStream.ReadColumn(tableSource, tableSourceRid, column, out startRid))
				return ContiguousRidRange.Empty;
			uint nextListRid;
			bool hasNext = tablesStream.ReadColumn(tableSource, tableSourceRid + 1, column, out nextListRid);

			uint lastRid = tablesStream.Get(tableDest).Rows + 1;
			if (startRid == 0 || startRid >= lastRid)
				return ContiguousRidRange.Empty;
			uint endRid = hasNext && nextListRid != 0 ? nextListRid : lastRid;
			if (endRid < startRid)
				endRid = startRid;
			if (endRid > lastRid)
				endRid = lastRid;
			return new ContiguousRidRange(startRid, endRid - startRid);
		}
	}
}
