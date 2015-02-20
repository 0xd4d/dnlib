// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using dnlib.IO;

namespace dnlib.DotNet.Pdb.Managed {
	sealed class DbiFunction : ISymbolMethod {
		public uint Token { get; internal set; }
		public string Name { get; private set; }
		public PdbAddress Address { get; private set; }
		public DbiScope Root { get; private set; }
		public IList<DbiSourceLine> Lines { get; internal set; }

		public void Read(IImageStream stream, long recEnd) {
			stream.Position += 4;
			var end = stream.ReadUInt32();
			stream.Position += 4;
			var len = stream.ReadUInt32();
			stream.Position += 8;
			Token = stream.ReadUInt32();
			Address = PdbAddress.ReadAddress(stream);
			stream.Position += 1 + 2;
			Name = PdbReader.ReadCString(stream);

			stream.Position = recEnd;
			Root = new DbiScope("", Address.Offset, len);
			Root.Read(new RecursionCounter(), stream, end);
			FixOffsets(new RecursionCounter(), Root);
		}

		void FixOffsets(RecursionCounter counter, DbiScope scope) {
			if (!counter.Increment())
				return;

			scope.BeginOffset -= Address.Offset;
			scope.EndOffset -= Address.Offset;
			foreach (var child in scope.Children)
				FixOffsets(counter, child);

			counter.Decrement();
		}

		#region ISymbolMethod

		ISymbolScope ISymbolMethod.RootScope {
			get { return Root; }
		}

		int ISymbolMethod.SequencePointCount {
			get { return Lines == null ? 0 : Lines.Count; }
		}

		void ISymbolMethod.GetSequencePoints(int[] offsets, ISymbolDocument[] documents, int[] lines, int[] columns,
			int[] endLines, int[] endColumns) {
			int count = Lines == null ? 0 : Lines.Count;
			if (offsets != null && offsets.Length != count)
				throw new ArgumentException("Invalid array length: offsets");
			if (documents != null && documents.Length != count)
				throw new ArgumentException("Invalid array length: documents");
			if (lines != null && lines.Length != count)
				throw new ArgumentException("Invalid array length: lines");
			if (columns != null && columns.Length != count)
				throw new ArgumentException("Invalid array length: columns");
			if (endLines != null && endLines.Length != count)
				throw new ArgumentException("Invalid array length: endLines");
			if (endColumns != null && endColumns.Length != count)
				throw new ArgumentException("Invalid array length: endColumns");

			if (count <= 0)
				return;

			int i = 0;
			foreach (var line in Lines) {
				offsets[i] = (int)line.Offset;
				documents[i] = line.Document;
				lines[i] = (int)line.LineBegin;
				columns[i] = (int)line.ColumnBegin;
				endLines[i] = (int)line.LineEnd;
				endColumns[i] = (int)line.ColumnEnd;
				i++;
			}
		}

		ISymbolNamespace ISymbolMethod.GetNamespace() {
			throw new NotImplementedException();
		}

		int ISymbolMethod.GetOffset(ISymbolDocument document, int line, int column) {
			throw new NotImplementedException();
		}

		ISymbolVariable[] ISymbolMethod.GetParameters() {
			throw new NotImplementedException();
		}

		int[] ISymbolMethod.GetRanges(ISymbolDocument document, int line, int column) {
			throw new NotImplementedException();
		}

		ISymbolScope ISymbolMethod.GetScope(int offset) {
			throw new NotImplementedException();
		}

		bool ISymbolMethod.GetSourceStartEnd(ISymbolDocument[] docs, int[] lines, int[] columns) {
			throw new NotImplementedException();
		}

		SymbolToken ISymbolMethod.Token {
			get { throw new NotImplementedException(); }
		}

		#endregion
	}
}