// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using dnlib.IO;
using dnlib.PE;
using dnlib.Threading;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// Used when a #~ stream is present in the metadata
	/// </summary>
	sealed class CompressedMetaData : MetaData {
		/// <inheritdoc/>
		public override bool IsCompressed {
			get { return true; }
		}

		/// <inheritdoc/>
		public CompressedMetaData(IPEImage peImage, ImageCor20Header cor20Header, MetaDataHeader mdHeader)
			: base(peImage, cor20Header, mdHeader) {
		}

		/// <inheritdoc/>
		internal CompressedMetaData(MetaDataHeader mdHeader, bool isStandalonePortablePdb)
			: base(mdHeader, isStandalonePortablePdb) {
		}

		static CompressedMetaData() {
			var windir = Environment.GetEnvironmentVariable("WINDIR");
			if (!string.IsNullOrEmpty(windir)) {
				var baseDir = Path.Combine(windir, "assembly");
				nativeImages40 = Path.Combine(baseDir, "NativeImages_v4.0.30319");
			}
		}

		static string nativeImages40;
		static HotHeapVersion GetHotHeapVersion(string fileName, string version) {
			// Some .NET 2.0 assemblies are stored in the 4.0 GAC. The version is not easily
			// detectable from the data in the image so check the path.
			if (nativeImages40 != null && fileName != null && fileName.StartsWith(nativeImages40, StringComparison.OrdinalIgnoreCase))
				return HotHeapVersion.CLR40;

			if (version.StartsWith(MDHeaderRuntimeVersion.MS_CLR_20_PREFIX))
				return HotHeapVersion.CLR20;
			if (version.StartsWith(MDHeaderRuntimeVersion.MS_CLR_40_PREFIX))
				return HotHeapVersion.CLR40;

			return HotHeapVersion.CLR40;
		}

		/// <inheritdoc/>
		protected override void InitializeInternal(IImageStream mdStream) {
			var hotHeapVersion = peImage == null ? HotHeapVersion.CLR20 : GetHotHeapVersion(peImage.FileName, mdHeader.VersionString);

			bool disposeOfMdStream = false;
			IImageStream imageStream = null, fullStream = null;
			DotNetStream dns = null;
			List<HotStream> hotStreams = null;
			HotStream hotStream = null;
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

					case "#!":
						if (peImage == null)
							break;
						if (hotStreams == null)
							hotStreams = new List<HotStream>();
						fullStream = peImage.CreateFullStream();
						hotStream = HotStream.Create(hotHeapVersion, imageStream, sh, fullStream, mdStream.FileOffset + sh.Offset);
						fullStream = null;
						hotStreams.Add(hotStream);
						newAllStreams.Add(hotStream);
						hotStream = null;
						imageStream = null;
						continue;

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
				if (hotStream != null)
					hotStream.Dispose();
				newAllStreams.Reverse();
				allStreams = ThreadSafeListCreator.MakeThreadSafe(newAllStreams);
			}

			if (tablesStream == null)
				throw new BadImageFormatException("Missing MD stream");

			if (hotStreams != null) {
				hotStreams.Reverse();
				InitializeHotStreams(hotStreams);
			}

			if (pdbStream != null)
				tablesStream.Initialize(pdbStream.TypeSystemTableRows);
			else
				tablesStream.Initialize(null);
		}

		int GetPointerSize() {
			if (peImage == null)
				return 4;
			return peImage.ImageNTHeaders.OptionalHeader.Magic == 0x10B ? 4 : 8;
		}

		void InitializeHotStreams(IList<HotStream> hotStreams) {
			if (hotStreams == null || hotStreams.Count == 0)
				return;

			// If this is a 32-bit image, make sure that we emulate this by masking
			// all base offsets to 32 bits.
			long offsetMask = GetPointerSize() == 8 ? -1L : uint.MaxValue;

			// It's always the last one found that is used
			var hotTable = hotStreams[hotStreams.Count - 1].HotTableStream;
			if (hotTable != null) {
				hotTable.Initialize(offsetMask);
				tablesStream.HotTableStream = hotTable;
			}

			HotHeapStream hotStrings = null, hotBlob = null, hotGuid = null, hotUs = null;
			for (int i = hotStreams.Count - 1; i >= 0; i--) {
				var hotStream = hotStreams[i];
				var hotHeapStreams = hotStream.HotHeapStreams;
				if (hotHeapStreams == null)
					continue;

				// It's always the last one found that is used
				for (int j = hotHeapStreams.Count - 1; j >= 0; j--) {
					var hotHeap = hotHeapStreams[j];
					switch (hotHeap.HeapType) {
					case HeapType.Strings:
						if (hotStrings == null) {
							hotHeap.Initialize(offsetMask);
							hotStrings = hotHeap;
						}
						break;

					case HeapType.Guid:
						if (hotGuid == null) {
							hotHeap.Initialize(offsetMask);
							hotGuid = hotHeap;
						}
						break;

					case HeapType.Blob:
						if (hotBlob == null) {
							hotHeap.Initialize(offsetMask);
							hotBlob = hotHeap;
						}
						break;

					case HeapType.US:
						if (hotUs == null) {
							hotHeap.Initialize(offsetMask);
							hotUs = hotHeap;
						}
						break;
					}
				}
			}
			InitializeNonExistentHeaps();
			stringsStream.HotHeapStream = hotStrings;
			guidStream.HotHeapStream = hotGuid;
			blobStream.HotHeapStream = hotBlob;
			usStream.HotHeapStream = hotUs;
		}

		/// <inheritdoc/>
		public override RidList GetFieldRidList(uint typeDefRid) {
			return GetRidList(tablesStream.TypeDefTable, typeDefRid, 4, tablesStream.FieldTable);
		}

		/// <inheritdoc/>
		public override RidList GetMethodRidList(uint typeDefRid) {
			return GetRidList(tablesStream.TypeDefTable, typeDefRid, 5, tablesStream.MethodTable);
		}

		/// <inheritdoc/>
		public override RidList GetParamRidList(uint methodRid) {
			return GetRidList(tablesStream.MethodTable, methodRid, 5, tablesStream.ParamTable);
		}

		/// <inheritdoc/>
		public override RidList GetEventRidList(uint eventMapRid) {
			return GetRidList(tablesStream.EventMapTable, eventMapRid, 1, tablesStream.EventTable);
		}

		/// <inheritdoc/>
		public override RidList GetPropertyRidList(uint propertyMapRid) {
			return GetRidList(tablesStream.PropertyMapTable, propertyMapRid, 1, tablesStream.PropertyTable);
		}

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
			if (!tablesStream.ReadColumn_NoLock(tableSource, tableSourceRid, column, out startRid))
				return RidList.Empty;
			hasNext = tablesStream.ReadColumn_NoLock(tableSource, tableSourceRid + 1, column, out nextListRid);
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
			return new ContiguousRidList(startRid, endRid - startRid);
		}

		/// <inheritdoc/>
		protected override uint BinarySearch_NoLock(MDTable tableSource, int keyColIndex, uint key) {
			var keyColumn = tableSource.TableInfo.Columns[keyColIndex];
			uint ridLo = 1, ridHi = tableSource.Rows;
			while (ridLo <= ridHi) {
				uint rid = (ridLo + ridHi) / 2;
				uint key2;
				if (!tablesStream.ReadColumn_NoLock(tableSource, rid, keyColumn, out key2))
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
