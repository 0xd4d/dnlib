// dnlib: See LICENSE.txt for more info

﻿using System;
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

		public IList<DbiFunction> Functions { get; private set; }
		public IList<DbiDocument> Documents { get; private set; }

		public void Read(IImageStream stream) {
			stream.Position += 34;
			StreamId = stream.ReadUInt16();
			cbSyms = stream.ReadUInt32();
			cbOldLines = stream.ReadUInt32();
			cbLines = stream.ReadUInt32();
			stream.Position += 16;

			if ((int)cbSyms < 0)
				cbSyms = 0;
			if ((int)cbOldLines < 0)
				cbOldLines = 0;
			if ((int)cbLines < 0)
				cbLines = 0;

			ModuleName = PdbReader.ReadCString(stream);
			ObjectName = PdbReader.ReadCString(stream);

			stream.Position = (stream.Position + 3) & (~3);
		}

		public void LoadFunctions(PdbReader reader, IImageStream stream) {
			stream.Position = 0;
			using (var substream = stream.Create(stream.FileOffset + stream.Position, cbSyms))
				ReadFunctions(substream);

			if (Functions.Count > 0) {
				stream.Position += cbSyms + cbOldLines;
				using (var substream = stream.Create(stream.FileOffset + stream.Position, cbLines))
					ReadLines(reader, substream);
			}
		}

		void ReadFunctions(IImageStream stream) {
			if (stream.ReadUInt32() != 4)
				throw new PdbException("Invalid signature");

			while (stream.Position < stream.Length) {
				var size = stream.ReadUInt16();
				var begin = stream.Position;
				var end = begin + size;

				var type = (SymbolType)stream.ReadUInt16();
				switch (type) {
					case SymbolType.S_GMANPROC:
					case SymbolType.S_LMANPROC:
						var func = new DbiFunction();
						func.Read(stream, end);
						Functions.Add(func);
						break;
					default:
						stream.Position = end;
						break;
				}
			}
		}

		void ReadLines(PdbReader reader, IImageStream stream) {
			var docs = new Dictionary<long, DbiDocument>();

			stream.Position = 0;
			while (stream.Position < stream.Length) {
				var sig = (ModuleStreamType)stream.ReadUInt32();
				var size = stream.ReadUInt32();
				var begin = stream.Position;
				var end = (begin + size + 3) & ~3;

				if (sig == ModuleStreamType.FileInfo)
					ReadFiles(reader, docs, stream, end);

				stream.Position = end;
			}

			var sortedFuncs = new DbiFunction[Functions.Count];
			Functions.CopyTo(sortedFuncs, 0);
			Array.Sort(sortedFuncs, (a, b) => a.Address.CompareTo(b.Address));

			stream.Position = 0;
			while (stream.Position < stream.Length) {
				var sig = (ModuleStreamType)stream.ReadUInt32();
				var size = stream.ReadUInt32();
				var begin = stream.Position;
				var end = begin + size;

				if (sig == ModuleStreamType.Lines)
					ReadLines(sortedFuncs, docs, stream, end);

				stream.Position = end;
			}
		}

		void ReadFiles(PdbReader reader, Dictionary<long, DbiDocument> documents, IImageStream stream, long end) {
			var begin = stream.Position;
			while (stream.Position < end) {
				var id = stream.Position - begin;

				var nameId = stream.ReadUInt32();
				var len = stream.ReadByte();
				/*var type = */stream.ReadByte();
				var doc = reader.GetDocument(nameId);
				documents.Add(id, doc);

				stream.Position += len;
				stream.Position = (stream.Position + 3) & (~3);
			}
		}

		void ReadLines(DbiFunction[] funcs, Dictionary<long, DbiDocument> documents, IImageStream stream, long end) {
			var address = PdbAddress.ReadAddress(stream);

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

			var flags = stream.ReadUInt16();
			stream.Position += 4;

			if (funcs[found].Lines == null) {
				while (found > 0) {
					var prevFunc = funcs[found - 1];
					if (prevFunc != null || prevFunc.Address != address)
						break;
					found--;
				}
			}
			else {
				while (found < funcs.Length - 1 && funcs[found] != null) {
					var nextFunc = funcs[found + 1];
					if (nextFunc.Address != address)
						break;
					found++;
				}
			}
			var func = funcs[found];
			if (func.Lines != null)
				return;
			func.Lines = new List<SymbolSequencePoint>();

			while (stream.Position < end) {
				var document = documents[stream.ReadUInt32()];
				var count = stream.ReadUInt32();
				stream.Position += 4;

				const int LINE_ENTRY_SIZE = 8;
				const int COL_ENTRY_SIZE = 4;
				var lineTablePos = stream.Position;
				var colTablePos = stream.Position + count * LINE_ENTRY_SIZE;

				for (uint i = 0; i < count; i++) {
					stream.Position = lineTablePos + i * LINE_ENTRY_SIZE;

					var line = new SymbolSequencePoint {
						Document = document
					};
					line.Offset = stream.ReadInt32();
					var lineFlags = stream.ReadUInt32();

					line.Line = (int)(lineFlags & 0x00ffffff);
					line.EndLine = line.Line + (int)((lineFlags >> 24) & 0x7F);
					if ((flags & 1) != 0) {
						stream.Position = colTablePos + i * COL_ENTRY_SIZE;
						line.Column = stream.ReadUInt16();
						line.EndColumn = stream.ReadUInt16();
					}

					func.Lines.Add(line);
				}
			}
		}
	}
}
