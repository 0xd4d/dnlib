// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// Used when a #~ stream is present in the metadata
	/// </summary>
	sealed class CompressedMetaData : MetaData {
		/// <inheritdoc/>
		public override bool IsCompressed => true;

		/// <inheritdoc/>
		public CompressedMetaData(IPEImage peImage, ImageCor20Header cor20Header, MetaDataHeader mdHeader)
			: base(peImage, cor20Header, mdHeader) {
		}

		/// <inheritdoc/>
		internal CompressedMetaData(MetaDataHeader mdHeader, bool isStandalonePortablePdb)
			: base(mdHeader, isStandalonePortablePdb) {
		}

		/// <inheritdoc/>
		protected override void InitializeInternal(IImageStream mdStream) {
			bool disposeOfMdStream = false;
			IImageStream imageStream = null, fullStream = null;
			DotNetStream dns = null;
			var newAllStreams = new List<DotNetStream>(allStreams);
			try {
				if (peImage != null) {
					Debug.Assert(mdStream == null);
					Debug.Assert(cor20Header != null);
					var mdOffset = peImage.ToFileOffset(cor20Header.MetaData.VirtualAddress);
					mdStream = peImage.CreateStream(mdOffset, cor20Header.MetaData.Size);
					disposeOfMdStream = true;
				}
				else
					Debug.Assert(mdStream != null);
				for (int i = mdHeader.StreamHeaders.Count - 1; i >= 0; i--) {
					var sh = mdHeader.StreamHeaders[i];
					imageStream = mdStream.Create((FileOffset)sh.Offset, sh.StreamSize);
					switch (sh.Name) {
					case "#Strings":
						if (stringsStream == null) {
							stringsStream = new StringsStream(imageStream, sh);
							imageStream = null;
							newAllStreams.Add(stringsStream);
							continue;
						}
						break;

					case "#US":
						if (usStream == null) {
							usStream = new USStream(imageStream, sh);
							imageStream = null;
							newAllStreams.Add(usStream);
							continue;
						}
						break;

					case "#Blob":
						if (blobStream == null) {
							blobStream = new BlobStream(imageStream, sh);
							imageStream = null;
							newAllStreams.Add(blobStream);
							continue;
						}
						break;

					case "#GUID":
						if (guidStream == null) {
							guidStream = new GuidStream(imageStream, sh);
							imageStream = null;
							newAllStreams.Add(guidStream);
							continue;
						}
						break;

					case "#~":
						if (tablesStream == null) {
							tablesStream = new TablesStream(imageStream, sh);
							imageStream = null;
							newAllStreams.Add(tablesStream);
							continue;
						}
						break;

					case "#Pdb":
						if (isStandalonePortablePdb && pdbStream == null) {
							pdbStream = new PdbStream(imageStream, sh);
							imageStream = null;
							allStreams.Add(pdbStream);
							continue;
						}
						break;
					}
					dns = new DotNetStream(imageStream, sh);
					imageStream = null;
					newAllStreams.Add(dns);
					dns = null;
				}
			}
			finally {
				if (disposeOfMdStream)
					mdStream.Dispose();
				if (imageStream != null)
					imageStream.Dispose();
				if (fullStream != null)
					fullStream.Dispose();
				if (dns != null)
					dns.Dispose();
				newAllStreams.Reverse();
				allStreams = newAllStreams;
			}

			if (tablesStream == null)
				throw new BadImageFormatException("Missing MD stream");

			if (pdbStream != null)
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
			uint startRid, nextListRid;
			bool hasNext;
#if THREAD_SAFE
			tablesStream.theLock.EnterWriteLock(); try {
#endif
			if (!tablesStream.TryReadColumn_NoLock(tableSource, tableSourceRid, column, out startRid))
				return RidList.Empty;
			hasNext = tablesStream.TryReadColumn_NoLock(tableSource, tableSourceRid + 1, column, out nextListRid);
#if THREAD_SAFE
			} finally { tablesStream.theLock.ExitWriteLock(); }
#endif
			uint lastRid = tableDest.Rows + 1;
			if (startRid == 0 || startRid >= lastRid)
				return RidList.Empty;
			uint endRid = hasNext ? nextListRid : lastRid;
			if (endRid < startRid)
				endRid = startRid;
			if (endRid > lastRid)
				endRid = lastRid;
			return RidList.Create(startRid, endRid - startRid);
		}

		/// <inheritdoc/>
		protected override uint BinarySearch_NoLock(MDTable tableSource, int keyColIndex, uint key) {
			var keyColumn = tableSource.TableInfo.Columns[keyColIndex];
			uint ridLo = 1, ridHi = tableSource.Rows;
			while (ridLo <= ridHi) {
				uint rid = (ridLo + ridHi) / 2;
				if (!tablesStream.TryReadColumn_NoLock(tableSource, rid, keyColumn, out uint key2))
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
