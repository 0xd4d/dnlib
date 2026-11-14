// dnlib: See LICENSE.txt for more info

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb.Symbols;
using dnlib.DotNet.Pdb.WindowsPdb;
using dnlib.IO;

namespace dnlib.DotNet.Pdb.Managed {
	/// <summary>
	/// A managed PDB reader implementation for .NET modules.
	/// </summary>
	sealed class PdbReader : SymbolReader {
		MsfStream[] streams;
		Dictionary<string, uint> names;
		Dictionary<uint, string> strings;
		List<DbiModule> modules;
		ModuleDef module;

		const int STREAM_ROOT = 0;
		const int STREAM_NAMES = 1;
		const int STREAM_TPI = 2;
		const int STREAM_DBI = 3;
		const ushort STREAM_INVALID_INDEX = ushort.MaxValue;

		Dictionary<string, DbiDocument> documents;
		HashSet<uint> documentStreams;
		Dictionary<int, DbiFunction> functions;
		byte[] sourcelinkData;
		byte[] srcsrvData;
		uint entryPt;

		public override PdbFileKind PdbFileKind => PdbFileKind.WindowsPDB;

		uint Age { get; set; }
		Guid Guid { get; set; }

		internal bool MatchesModule => expectedGuid == Guid && expectedAge == Age;
		readonly Guid expectedGuid;
		readonly uint expectedAge;

		public PdbReader(Guid expectedGuid, uint expectedAge) {
			this.expectedGuid = expectedGuid;
			this.expectedAge = expectedAge;
		}

		public override void Initialize(ModuleDef module) => this.module = module;

		/// <summary>
		/// Read the PDB in the specified stream.
		/// </summary>
		/// <param name="reader">PDB file data reader</param>
		public void Read(DataReader reader) {
			try {
				ReadInternal(ref reader);
			}
			catch (Exception ex) {
				if (ex is PdbException)
					throw;
				throw new PdbException(ex);
			}
			finally {
				streams = null;
				names = null;
				strings = null;
				modules = null;
				documentStreams = null;
			}
		}

		static uint RoundUpDiv(uint value, uint divisor) => (uint)(((ulong)value + divisor - 1) / divisor);

		void ReadInternal(ref DataReader reader) {
			string sig = reader.ReadString(30, Encoding.ASCII);
			if (sig != "Microsoft C/C++ MSF 7.00\r\n\u001ADS\0")
				throw new PdbException("Invalid signature");
			reader.Position += 2;

			uint pageSize = reader.ReadUInt32();
			/*uint fpm = */reader.ReadUInt32();
			uint pageCount = reader.ReadUInt32();
			uint rootSize = reader.ReadUInt32();
			reader.ReadUInt32();
			// Valid page sizes are 512, 1024, 2048, 4096, 8192, 16384 and 32768. A tiny page size
			// would make the page arrays many times bigger than the file.
			if (pageSize < 0x200 || pageSize > 0x8000 || (pageSize & (pageSize - 1)) != 0)
				throw new PdbException("Invalid page size");
			var numOfRootPages = RoundUpDiv(rootSize, pageSize);
			var numOfPtrPages = RoundUpDiv(numOfRootPages * 4, pageSize);
			if ((ulong)pageCount * pageSize != reader.Length)
				throw new PdbException("File size mismatch");
			if (numOfRootPages > pageCount)
				throw new PdbException("Invalid root stream size");

			var pages = new DataReader[pageCount];
			uint offset = 0;
			for (uint i = 0; i < pageCount; i++) {
				pages[i] = reader.Slice(offset, pageSize);
				offset += pageSize;
			}

			var rootPages = new DataReader[numOfRootPages];
			int pageIndex = 0;
			for (int i = 0; i < numOfPtrPages && pageIndex < numOfRootPages; i++) {
				var ptrPage = pages[reader.ReadUInt32()];
				ptrPage.Position = 0;
				for (; ptrPage.Position < ptrPage.Length && pageIndex < numOfRootPages; pageIndex++)
					rootPages[pageIndex] = pages[ptrPage.ReadUInt32()];
			}

			ReadRootDirectory(new MsfStream(rootPages, rootSize), pages, pageSize);

			ReadNames();
			if (!MatchesModule)
				return;
			ReadStringTable();
			var tokenMapStream = ReadModules();

			documents = new Dictionary<string, DbiDocument>(StringComparer.OrdinalIgnoreCase);
			documentStreams = new HashSet<uint>();
			foreach (var module in modules) {
				if (IsValidStreamIndex(module.StreamId))
					module.LoadFunctions(this, ref streams[module.StreamId].Content);
			}

			if (IsValidStreamIndex(tokenMapStream ?? STREAM_INVALID_INDEX))
				ApplyRidMap(ref streams[tokenMapStream.Value].Content);

			functions = new Dictionary<int, DbiFunction>();
			foreach (var module in modules) {
				foreach (var func in module.Functions) {
					func.reader = this;
					functions.Add(func.Token, func);
				}
			}

			sourcelinkData = TryGetRawFileData("sourcelink");
			srcsrvData = TryGetRawFileData("srcsrv");
		}

		byte[] TryGetRawFileData(string name) {
			if (!names.TryGetValue(name, out uint streamId))
				return null;
			if (streamId > ushort.MaxValue || !IsValidStreamIndex((ushort)streamId))
				return null;
			return streams[streamId].Content.ToArray();
		}

		bool IsValidStreamIndex(ushort index) => index != STREAM_INVALID_INDEX && index < streams.Length;

		void ReadRootDirectory(MsfStream stream, DataReader[] pages, uint pageSize) {
			uint streamNum = stream.Content.ReadUInt32();
			if (streamNum > stream.Content.BytesLeft / 4)
				throw new PdbException("Invalid number of streams");
			var streamSizes = new uint[streamNum];
			for (int i = 0; i < streamSizes.Length; i++)
				streamSizes[i] = stream.Content.ReadUInt32();

			// Writers don't make streams share pages so limit their total size to the size of the
			// file. A small file could otherwise use many GBs since all streams could use the same pages.
			ulong fileSize = (ulong)pages.Length * pageSize;
			ulong totalStreamSize = 0;
			streams = new MsfStream[streamNum];
			for (int i = 0; i < streamSizes.Length; i++) {
				if (streamSizes[i] == 0xffffffff) {
					streams[i] = null;
					continue;
				}
				totalStreamSize += streamSizes[i];
				if (totalStreamSize > fileSize)
					throw new PdbException("Stream sizes are bigger than the file");
				var pageCount = RoundUpDiv(streamSizes[i], pageSize);
				var streamPages = new DataReader[pageCount];
				for (int j = 0; j < streamPages.Length; j++)
					streamPages[j] = pages[stream.Content.ReadUInt32()];
				streams[i] = new MsfStream(streamPages, streamSizes[i]);
			}
		}

		void ReadNames() {
			ref var stream = ref streams[STREAM_NAMES].Content;
			stream.Position = 8;
			Age = stream.ReadUInt32();
			Guid = stream.ReadGuid();

			uint nameSize = stream.ReadUInt32();
			var nameData = stream.Slice(stream.Position, nameSize);
			stream.Position += nameSize;

			/*uint entryCount = */stream.ReadUInt32();
			uint entryCapacity = stream.ReadUInt32();
			int entryOkWords = stream.ReadInt32();
			if ((uint)entryOkWords > stream.BytesLeft / 4)
				throw new PdbException("Invalid bit vector size");
			var entryOk = new BitArray(stream.ReadBytes(entryOkWords * 4));
			if (stream.ReadUInt32() != 0)
				throw new NotSupportedException();

			names = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
			var namesByPos = new Dictionary<uint, string>();
			uint totalSize = 0;
			entryCapacity = Math.Min(entryCapacity, (uint)entryOk.Count);
			for (int i = 0; i < entryCapacity; i++) {
				if (!entryOk[i])
					continue;

				var pos = stream.ReadUInt32();
				var streamId = stream.ReadUInt32();
				if (!namesByPos.TryGetValue(pos, out var streamName)) {
					streamName = ReadCString(ref nameData, pos, ref totalSize);
					namesByPos.Add(pos, streamName);
				}
				names[streamName] = streamId;
			}
		}

		void ReadStringTable() {
			if (!names.TryGetValue("/names", out uint streamId))
				throw new PdbException("String table not found");

			ref var stream = ref streams[streamId].Content;
			stream.Position = 8;

			uint strSize = stream.ReadUInt32();
			var strData = stream.Slice(stream.Position, strSize);
			stream.Position += strSize;

			uint count = stream.ReadUInt32();
			if (count > stream.BytesLeft / 4)
				throw new PdbException("Invalid number of strings");
			strings = new Dictionary<uint, string>((int)count);
			uint totalSize = 0;
			for (uint i = 0; i < count; i++) {
				var pos = stream.ReadUInt32();
				if (pos == 0 || strings.ContainsKey(pos))
					continue;
				strings[pos] = ReadCString(ref strData, pos, ref totalSize);
			}
		}

		// Reads a string from a string table. Writers don't make strings share bytes so the total size of
		// all strings read from a table is limited to the size of the table. A small file could otherwise
		// use many GBs since every string could be a suffix of the same big string.
		static string ReadCString(ref DataReader table, uint pos, ref uint totalSize) {
			table.Position = pos;
			var s = ReadCString(ref table);
			totalSize += table.Position - pos;
			if (totalSize > table.Length)
				throw new PdbException("Strings are bigger than the string table");
			return s;
		}

		static uint ReadSizeField(ref DataReader reader) {
			int size = reader.ReadInt32();
			return size <= 0 ? 0 : (uint)size;
		}

		ushort? ReadModules() {
			ref var stream = ref streams[STREAM_DBI].Content;
			modules = new List<DbiModule>();
			if (stream.Length == 0)
				return null;
			stream.Position = 20;
			ushort symrecStream = stream.ReadUInt16();
			stream.Position += 2;
			uint gpmodiSize = ReadSizeField(ref stream); // gpmodiSize
			uint otherSize = 0;
			otherSize += ReadSizeField(ref stream); // secconSize
			otherSize += ReadSizeField(ref stream); // secmapSize
			otherSize += ReadSizeField(ref stream); // filinfSize
			otherSize += ReadSizeField(ref stream); // tsmapSize
			stream.ReadUInt32(); // mfcIndex
			uint dbghdrSize = ReadSizeField(ref stream);
			otherSize += ReadSizeField(ref stream); // ecinfoSize
			stream.Position += 8;

			var moduleStream = stream.Slice(stream.Position, gpmodiSize);
			while (moduleStream.Position < moduleStream.Length) {
				var module = new DbiModule();
				module.Read(ref moduleStream);
				modules.Add(module);
			}

			if (IsValidStreamIndex(symrecStream))
				ReadGlobalSymbols(ref streams[symrecStream].Content);

			if (dbghdrSize != 0) {
				stream.Position += gpmodiSize;
				stream.Position += otherSize;
				stream.Position += 12;
				return stream.ReadUInt16();
			}
			return null;
		}

		internal DbiDocument GetDocument(uint nameId) {
			var name = strings[nameId];

			if (!documents.TryGetValue(name, out var doc)) {
				doc = new DbiDocument(name);

				if (names.TryGetValue($"/src/files/{name}", out uint streamId)) {
					// Writers don't make documents share streams so every stream can be read by only one
					// document. A small file could otherwise use many GBs since every document makes its
					// own copy of the embedded source code and all of them could use the same big stream.
					if (!documentStreams.Add(streamId))
						throw new PdbException("Documents share a stream");
					doc.Read(ref streams[streamId].Content);
				}
				documents.Add(name, doc);
			}
			return doc;
		}

		void ReadGlobalSymbols(ref DataReader reader) {
			reader.Position = 0;
			while (reader.Position < reader.Length) {
				var size = reader.ReadUInt16();
				var begin = reader.Position;
				var end = begin + size;

				if ((SymbolType)reader.ReadUInt16() == SymbolType.S_PUB32) {
					reader.Position += 4;
					var offset = reader.ReadUInt32();
					reader.Position += 2;
					var name = ReadCString(ref reader, end);

					if (name == "COM+_Entry_Point") {
						entryPt = offset;
						break;
					}
				}

				reader.Position = end;
			}
		}

		void ApplyRidMap(ref DataReader reader) {
			reader.Position = 0;
			var map = new uint[reader.Length / 4];
			for (int i = 0; i < map.Length; i++)
				map[i] = reader.ReadUInt32();

			foreach (var module in modules) {
				foreach (var func in module.Functions) {
					var rid = (uint)func.Token & 0x00ffffff;
					rid = map[rid];
					func.token = (int)((func.Token & 0xff000000) | rid);
				}
			}

			if (entryPt != 0) {
				var rid = entryPt & 0x00ffffff;
				rid = map[rid];
				entryPt = (entryPt & 0xff000000) | rid;
			}
		}

		internal static string ReadCString(ref DataReader reader) => reader.TryReadZeroTerminatedUtf8String() ?? string.Empty;

		// Reads a name that must end before 'end', the end of its symbol record, so it can't come from later records
		internal static string ReadCString(ref DataReader reader, uint end) {
			var pos = reader.Position;
			if (end > reader.Length)
				end = reader.Length;
			if (pos >= end)
				return string.Empty;
			var nameReader = reader.Slice(pos, end - pos);
			var name = nameReader.TryReadZeroTerminatedUtf8String();
			if (name is null) {
				reader.Position = end;
				return string.Empty;
			}
			reader.Position = pos + nameReader.Position;
			return name;
		}

		public override SymbolMethod GetMethod(MethodDef method, int version) {
			if (version != 1)
				return null;
			if (functions.TryGetValue(method.MDToken.ToInt32(), out var symMethod))
				return symMethod;
			return null;
		}

		public override IList<SymbolDocument> Documents {
			get {
				if (documentsResult is null) {
					var docs = new SymbolDocument[documents.Count];
					int i = 0;
					foreach (var kv in documents)
						docs[i++] = kv.Value;
					documentsResult = docs;
				}
				return documentsResult;
			}
		}
		volatile SymbolDocument[] documentsResult;

		public override int UserEntryPoint => (int)entryPt;

		internal void GetCustomDebugInfos(DbiFunction symMethod, MethodDef method, CilBody body, IList<PdbCustomDebugInfo> result) {
			const string CDI_NAME = "MD2";
			var asyncMethod = PseudoCustomDebugInfoFactory.TryCreateAsyncMethod(method.Module, method, body, symMethod.AsyncKickoffMethod, symMethod.AsyncStepInfos, symMethod.AsyncCatchHandlerILOffset);
			if (asyncMethod is not null)
				result.Add(asyncMethod);

			var cdiData = symMethod.Root.GetSymAttribute(CDI_NAME);
			if (cdiData is null)
				return;
			PdbCustomDebugInfoReader.Read(method, body, result, cdiData);
		}

		public override void GetCustomDebugInfos(int token, GenericParamContext gpContext, IList<PdbCustomDebugInfo> result) {
			if (token == 0x00000001)
				GetCustomDebugInfos_ModuleDef(result);
		}

		void GetCustomDebugInfos_ModuleDef(IList<PdbCustomDebugInfo> result) {
			if (sourcelinkData is not null)
				result.Add(new PdbSourceLinkCustomDebugInfo(sourcelinkData));
			if (srcsrvData is not null)
				result.Add(new PdbSourceServerCustomDebugInfo(srcsrvData));
		}
	}
}
