// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// Used when a #~ stream is present in the metadata
	/// </summary>
	sealed class CompressedMetadata : MetadataBase {
		readonly CLRRuntimeReaderKind runtime;

		/// <inheritdoc/>
		public override bool IsCompressed => true;

		/// <inheritdoc/>
		public CompressedMetadata(IPEImage peImage, ImageCor20Header cor20Header, MetadataHeader mdHeader, CLRRuntimeReaderKind runtime)
			: base(peImage, cor20Header, mdHeader) {
			this.runtime = runtime;
		}

		/// <inheritdoc/>
		internal CompressedMetadata(MetadataHeader mdHeader, bool isStandalonePortablePdb, CLRRuntimeReaderKind runtime)
			: base(mdHeader, isStandalonePortablePdb) {
			this.runtime = runtime;
		}

		/// <inheritdoc/>
		protected override void InitializeInternal(DataReaderFactory mdReaderFactory, uint metadataBaseOffset) {
			DotNetStream dns = null;
			var newAllStreams = new List<DotNetStream>(allStreams);
			try {
				for (int i = mdHeader.StreamHeaders.Count - 1; i >= 0; i--) {
					var sh = mdHeader.StreamHeaders[i];
					switch (sh.Name) {
					case "#Strings":
						if (stringsStream is null) {
							stringsStream = new StringsStream(mdReaderFactory, metadataBaseOffset, sh);
							newAllStreams.Add(stringsStream);
							continue;
						}
						break;

					case "#US":
						if (usStream is null) {
							usStream = new USStream(mdReaderFactory, metadataBaseOffset, sh);
							newAllStreams.Add(usStream);
							continue;
						}
						break;

					case "#Blob":
						if (blobStream is null) {
							blobStream = new BlobStream(mdReaderFactory, metadataBaseOffset, sh);
							newAllStreams.Add(blobStream);
							continue;
						}
						break;

					case "#GUID":
						if (guidStream is null) {
							guidStream = new GuidStream(mdReaderFactory, metadataBaseOffset, sh);
							newAllStreams.Add(guidStream);
							continue;
						}
						break;

					case "#~":
						if (tablesStream is null) {
							tablesStream = new TablesStream(mdReaderFactory, metadataBaseOffset, sh, runtime);
							newAllStreams.Add(tablesStream);
							continue;
						}
						break;

					case "#Pdb":
						if (isStandalonePortablePdb && pdbStream is null) {
							pdbStream = new PdbStream(mdReaderFactory, metadataBaseOffset, sh);
							newAllStreams.Add(pdbStream);
							continue;
						}
						break;
					}
					dns = new CustomDotNetStream(mdReaderFactory, metadataBaseOffset, sh);
					newAllStreams.Add(dns);
					dns = null;
				}
			}
			finally {
				dns?.Dispose();
				newAllStreams.Reverse();
				allStreams = newAllStreams;
			}

			if (tablesStream is null)
				throw new BadImageFormatException("Missing MD stream");

			if (!(pdbStream is null))
				tablesStream.Initialize(pdbStream.TypeSystemTableRows);
			else
				tablesStream.Initialize(null);
		}

		/// <inheritdoc/>
		public override RidList GetFieldRidList(uint typeDefRid) => GetRidList(tablesStream.TypeDefTable, typeDefRid, 4, tablesStream.FieldTable);

		/// <inheritdoc/>
		public override RidList GetMethodRidList(uint typeDefRid) => GetRidList(tablesStream.TypeDefTable, typeDefRid, 5, tablesStream.MethodTable);

		/// <inheritdoc/>
		public override RidList GetParamRidList(uint methodRid) => GetRidList(tablesStream.MethodTable, methodRid, 5, tablesStream.ParamTable);

		/// <inheritdoc/>
		public override RidList GetEventRidList(uint eventMapRid) => GetRidList(tablesStream.EventMapTable, eventMapRid, 1, tablesStream.EventTable);

		/// <inheritdoc/>
		public override RidList GetPropertyRidList(uint propertyMapRid) => GetRidList(tablesStream.PropertyMapTable, propertyMapRid, 1, tablesStream.PropertyTable);

		/// <summary>
		/// Gets a rid list (eg. field list)
		/// </summary>
		/// <param name="tableSource">Source table, eg. <c>TypeDef</c></param>
		/// <param name="tableSourceRid">Row ID in <paramref name="tableSource"/></param>
		/// <param name="colIndex">Column index in <paramref name="tableSource"/>, eg. 4 for <c>TypeDef.FieldList</c></param>
		/// <param name="tableDest">Destination table, eg. <c>Field</c></param>
		/// <returns>A new <see cref="RidList"/> instance</returns>
		RidList GetRidList(MDTable tableSource, uint tableSourceRid, int colIndex, MDTable tableDest) {
			var column = tableSource.TableInfo.Columns[colIndex];
			if (!tablesStream.TryReadColumn24(tableSource, tableSourceRid, column, out uint startRid))
				return RidList.Empty;
			bool hasNext = tablesStream.TryReadColumn24(tableSource, tableSourceRid + 1, column, out uint nextListRid);
			uint lastRid = tableDest.Rows + 1;
			if (startRid == 0 || startRid >= lastRid)
				return RidList.Empty;
			uint endRid = !hasNext || (nextListRid == 0 && tableSourceRid + 1 == tableSource.Rows && tableDest.Rows == 0xFFFF) ? lastRid : nextListRid;
			if (endRid < startRid)
				endRid = startRid;
			if (endRid > lastRid)
				endRid = lastRid;
			return RidList.Create(startRid, endRid - startRid);
		}

		/// <inheritdoc/>
		protected override uint BinarySearch(MDTable tableSource, int keyColIndex, uint key) {
			var keyColumn = tableSource.TableInfo.Columns[keyColIndex];
			uint ridLo = 1, ridHi = tableSource.Rows;
			while (ridLo <= ridHi) {
				uint rid = (ridLo + ridHi) / 2;
				if (!tablesStream.TryReadColumn24(tableSource, rid, keyColumn, out uint key2))
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
