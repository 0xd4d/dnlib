// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using dnlib.DotNet.Pdb.Symbols;
using dnlib.IO;

namespace dnlib.DotNet.Pdb.Managed {
	sealed class DbiModule {
		public DbiModule() {
			Functions = new List<DbiFunction>();
			Documents = new List<DbiDocument>();
		}

		public ushort StreamId { get; private set; }
		uint cbSyms;
		uint cbOldLines;
		uint cbLines;

		public string ModuleName { get; private set; }
		public string ObjectName { get; private set; }

		public List<DbiFunction> Functions { get; private set; }
		public List<DbiDocument> Documents { get; private set; }

		public void Read(ref DataReader reader) {
			reader.Position += 34;
			StreamId = reader.ReadUInt16();
			cbSyms = reader.ReadUInt32();
			cbOldLines = reader.ReadUInt32();
			cbLines = reader.ReadUInt32();
			reader.Position += 16;

			if ((int)cbSyms < 0)
				cbSyms = 0;
			if ((int)cbOldLines < 0)
				cbOldLines = 0;
			if ((int)cbLines < 0)
				cbLines = 0;

			ModuleName = PdbReader.ReadCString(ref reader);
			ObjectName = PdbReader.ReadCString(ref reader);

			reader.Position = (reader.Position + 3) & (~3U);
		}

		public void LoadFunctions(PdbReader pdbReader, ref DataReader reader) {
			reader.Position = 0;
			ReadFunctions(reader.Slice(reader.Position, cbSyms));

			if (Functions.Count > 0) {
				reader.Position += cbSyms + cbOldLines;
				ReadLines(pdbReader, reader.Slice(reader.Position, cbLines));
			}
		}

		void ReadFunctions(DataReader reader) {
			if (reader.ReadUInt32() != 4)
				throw new PdbException("Invalid signature");

			while (reader.Position < reader.Length) {
				var size = reader.ReadUInt16();
				var begin = reader.Position;
				var end = begin + size;

				var type = (SymbolType)reader.ReadUInt16();
				switch (type) {
					case SymbolType.S_GMANPROC:
					case SymbolType.S_LMANPROC:
						var func = new DbiFunction();
						func.Read(ref reader, end);
						Functions.Add(func);
						break;
					default:
						reader.Position = end;
						break;
				}
			}
		}

		void ReadLines(PdbReader pdbReader, DataReader reader) {
			var docs = new Dictionary<uint, DbiDocument>();

			reader.Position = 0;
			while (reader.Position < reader.Length) {
				var sig = (ModuleStreamType)reader.ReadUInt32();
				var size = reader.ReadUInt32();
				var begin = reader.Position;
				var end = (begin + size + 3) & ~3U;

				if (sig == ModuleStreamType.FileInfo)
					ReadFiles(pdbReader, docs, ref reader, end);

				reader.Position = end;
			}

			var sortedFuncs = new DbiFunction[Functions.Count];
			Functions.CopyTo(sortedFuncs, 0);
			Array.Sort(sortedFuncs, (a, b) => a.Address.CompareTo(b.Address));

			reader.Position = 0;
			while (reader.Position < reader.Length) {
				var sig = (ModuleStreamType)reader.ReadUInt32();
				var size = reader.ReadUInt32();
				var begin = reader.Position;
				var end = begin + size;

				if (sig == ModuleStreamType.Lines)
					ReadLines(sortedFuncs, docs, ref reader, end);

				reader.Position = end;
			}
		}

		void ReadFiles(PdbReader pdbReader, Dictionary<uint, DbiDocument> documents, ref DataReader reader, uint end) {
			var begin = reader.Position;
			while (reader.Position < end) {
				var id = reader.Position - begin;

				var nameId = reader.ReadUInt32();
				var len = reader.ReadByte();
				/*var type = */reader.ReadByte();
				var doc = pdbReader.GetDocument(nameId);
				documents.Add(id, doc);

				reader.Position += len;
				reader.Position = (reader.Position + 3) & (~3U);
			}
		}

		void ReadLines(DbiFunction[] funcs, Dictionary<uint, DbiDocument> documents, ref DataReader reader, uint end) {
			var address = PdbAddress.ReadAddress(ref reader);

			int first = 0;
			int last = funcs.Length - 1;
			int found = -1;
			while (first <= last) {
				var index = first + ((last - first) >> 1);
				var addr = funcs[index].Address;
				if (addr < address) {
					first = index + 1;
				}
				else if (addr > address) {
					last = index - 1;
				}
				else {
					found = index;
					break;
				}
			}
			if (found == -1)
				return;

			var flags = reader.ReadUInt16();
			reader.Position += 4;

			if (funcs[found].Lines is null) {
				while (found > 0) {
					var prevFunc = funcs[found - 1];
					if (!(prevFunc is null) || prevFunc.Address != address)
						break;
					found--;
				}
			}
			else {
				while (found < funcs.Length - 1 && !(funcs[found] is null)) {
					var nextFunc = funcs[found + 1];
					if (nextFunc.Address != address)
						break;
					found++;
				}
			}
			var func = funcs[found];
			if (!(func.Lines is null))
				return;
			func.Lines = new List<SymbolSequencePoint>();

			while (reader.Position < end) {
				var document = documents[reader.ReadUInt32()];
				var count = reader.ReadUInt32();
				reader.Position += 4;

				const int LINE_ENTRY_SIZE = 8;
				const int COL_ENTRY_SIZE = 4;
				var lineTablePos = reader.Position;
				var colTablePos = reader.Position + count * LINE_ENTRY_SIZE;

				for (uint i = 0; i < count; i++) {
					reader.Position = lineTablePos + i * LINE_ENTRY_SIZE;

					var line = new SymbolSequencePoint {
						Document = document
					};
					line.Offset = reader.ReadInt32();
					var lineFlags = reader.ReadUInt32();

					line.Line = (int)(lineFlags & 0x00ffffff);
					line.EndLine = line.Line + (int)((lineFlags >> 24) & 0x7F);
					if ((flags & 1) != 0) {
						reader.Position = colTablePos + i * COL_ENTRY_SIZE;
						line.Column = reader.ReadUInt16();
						line.EndColumn = reader.ReadUInt16();
					}

					func.Lines.Add(line);
				}
			}
		}
	}
}
