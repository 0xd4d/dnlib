// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace dnlib.TestUtils.Dumping {
	/// <summary>
	/// Writes the rows of all tables in a portable PDB file as text. It reads the file with
	/// System.Reflection.Metadata and not with dnlib, so the tests can check what dnlib wrote without using
	/// dnlib's reader, and compare it with what the C# compiler wrote.
	/// </summary>
	/// <remarks>
	/// Offsets in the heaps aren't written, the strings, GUIDs and bytes at those offsets are. Two files with
	/// the same rows in the same order have the same text, even if their heaps are in another order.
	///
	/// Not in the text: the PDB ID, and the row counts of the module's tables that are stored in the PDB.
	/// </remarks>
	public sealed class PortablePdbTableDumper {
		readonly MetadataReader reader;
		readonly StringBuilder text = new StringBuilder();
		// The number of each LocalScope row in the text, 0 if the row isn't in the text. Index 0 is row 1.
		readonly int[] localScopeNumbers;

		PortablePdbTableDumper(MetadataReader reader, bool skipEmptyLocalScopes) {
			this.reader = reader;
			localScopeNumbers = new int[reader.LocalScopes.Count];
			int number = 0;
			foreach (var handle in reader.LocalScopes) {
				if (!skipEmptyLocalScopes || !IsEmpty(reader.GetLocalScope(handle)))
					localScopeNumbers[MetadataTokens.GetRowNumber(handle) - 1] = ++number;
			}
		}

		static bool IsEmpty(LocalScope scope) =>
			scope.ImportScope.IsNil && scope.GetLocalVariables().Count == 0 && scope.GetLocalConstants().Count == 0;

		/// <summary>
		/// Gets the text
		/// </summary>
		/// <param name="pdb">A portable PDB file</param>
		/// <param name="skipEmptyLocalScopes"><c>true</c> to leave out every LocalScope row without local variables,
		/// constants and an import scope, and to number the rows without them. The C# compiler writes such rows.
		/// dnlib doesn't write one if it's the method's only scope: it has no information.</param>
		public static string Dump(byte[] pdb, bool skipEmptyLocalScopes = false) {
			using (var provider = MetadataReaderProvider.FromPortablePdbImage(ImmutableArray.Create(pdb)))
				return new PortablePdbTableDumper(provider.GetMetadataReader(), skipEmptyLocalScopes).Dump();
		}

		string Dump() {
			text.AppendLine($"Entry point: {RowText(reader.DebugMetadataHeader!.EntryPoint)}");
			DumpDocuments();
			DumpMethodDebugInformation();
			DumpLocalScopes();
			DumpLocalVariables();
			DumpLocalConstants();
			DumpImportScopes();
			// Every row of the StateMachineMethod table is a 'state machine kickoff method' line above
			text.AppendLine($"StateMachineMethod rows: {reader.GetTableRowCount(TableIndex.StateMachineMethod)}");
			DumpCustomDebugInformation();
			return text.ToString();
		}

		// Eg. "MethodDef 12", or "nil" if it doesn't reference a row
		string RowText(Handle handle) {
			if (handle.IsNil)
				return "nil";
			int row = MetadataTokens.GetToken(reader, handle) & 0x00FFFFFF;
			if (handle.Kind == HandleKind.LocalScope)
				row = localScopeNumbers[row - 1];
			return $"{handle.Kind} {row}";
		}

		string GuidText(GuidHandle handle) =>
			handle.IsNil ? "nil" : reader.GetGuid(handle).ToString();

		string BlobText(BlobHandle handle) =>
			handle.IsNil ? "nil" : HexText(reader.GetBlobBytes(handle));

		static string HexText(byte[] bytes) => bytes.Length == 0 ? "empty" : BitConverter.ToString(bytes);

		void DumpDocuments() {
			foreach (var handle in reader.Documents) {
				var document = reader.GetDocument(handle);
				text.AppendLine($"Document {MetadataTokens.GetRowNumber(handle)}: {reader.GetString(document.Name)}");
				// A name is stored as a separator and the parts between the separators
				var nameBlob = reader.GetBlobBytes(document.Name);
				text.AppendLine($"  separator: 0x{nameBlob[0]:X2}");
				text.AppendLine($"  language: {GuidText(document.Language)}");
				text.AppendLine($"  hash algorithm: {GuidText(document.HashAlgorithm)}");
				text.AppendLine($"  hash: {BlobText(document.Hash)}");
			}
		}

		void DumpMethodDebugInformation() {
			foreach (var handle in reader.MethodDebugInformation) {
				var info = reader.GetMethodDebugInformation(handle);
				text.AppendLine($"MethodDebugInformation {MetadataTokens.GetRowNumber(handle)}:");
				text.AppendLine($"  document: {RowText(info.Document)}");
				text.AppendLine($"  local signature: {RowText(info.LocalSignature)}");
				text.AppendLine($"  state machine kickoff method: {RowText(info.GetStateMachineKickoffMethod())}");
				if (info.SequencePointsBlob.IsNil)
					continue;
				foreach (var sequencePoint in info.GetSequencePoints()) {
					var where = sequencePoint.IsHidden ? "hidden" :
						$"{sequencePoint.StartLine},{sequencePoint.StartColumn}-{sequencePoint.EndLine},{sequencePoint.EndColumn}";
					text.AppendLine($"  IL_{sequencePoint.Offset:X4}: {RowText(sequencePoint.Document)} {where}");
				}
			}
		}

		void DumpLocalScopes() {
			foreach (var handle in reader.LocalScopes) {
				var scope = reader.GetLocalScope(handle);
				if (localScopeNumbers[MetadataTokens.GetRowNumber(handle) - 1] == 0)
					continue;
				text.AppendLine($"{RowText(handle)}: {RowText(scope.Method)} IL_{scope.StartOffset:X4}-IL_{scope.EndOffset:X4}");
				text.AppendLine($"  import scope: {RowText(scope.ImportScope)}");
				foreach (var variable in scope.GetLocalVariables())
					text.AppendLine($"  {RowText(variable)}");
				foreach (var constant in scope.GetLocalConstants())
					text.AppendLine($"  {RowText(constant)}");
			}
		}

		void DumpLocalVariables() {
			foreach (var handle in reader.LocalVariables) {
				var variable = reader.GetLocalVariable(handle);
				text.AppendLine($"LocalVariable {MetadataTokens.GetRowNumber(handle)}: {reader.GetString(variable.Name)}, index {variable.Index}, {variable.Attributes}");
			}
		}

		void DumpLocalConstants() {
			foreach (var handle in reader.LocalConstants) {
				var constant = reader.GetLocalConstant(handle);
				text.AppendLine($"LocalConstant {MetadataTokens.GetRowNumber(handle)}: {reader.GetString(constant.Name)}, signature {BlobText(constant.Signature)}");
			}
		}

		void DumpImportScopes() {
			foreach (var handle in reader.ImportScopes) {
				var scope = reader.GetImportScope(handle);
				text.AppendLine($"ImportScope {MetadataTokens.GetRowNumber(handle)}: parent {RowText(scope.Parent)}");
				foreach (var import in scope.GetImports()) {
					text.Append($"  {import.Kind}");
					if (!import.Alias.IsNil)
						text.Append($", alias {Utf8Text(import.Alias)}");
					if (!import.TargetAssembly.IsNil)
						text.Append($", assembly {RowText(import.TargetAssembly)}");
					// An import has a namespace or a type, and reading the wrong one throws
					bool hasType = import.Kind == ImportDefinitionKind.ImportType || import.Kind == ImportDefinitionKind.AliasType;
					if (hasType)
						text.Append($", type {RowText(import.TargetType)}");
					else if (!import.TargetNamespace.IsNil)
						text.Append($", namespace {Utf8Text(import.TargetNamespace)}");
					text.AppendLine();
				}
			}
		}

		string Utf8Text(BlobHandle handle) => Encoding.UTF8.GetString(reader.GetBlobBytes(handle));

		// The table is sorted by parent. The rows of one parent are written sorted by kind: their order in the
		// file has no meaning, and dnlib writes some kinds in another order than the compiler does (eg. the
		// async method info after the other custom debug info of the method).
		void DumpCustomDebugInformation() {
			var rowsOfParent = new System.Collections.Generic.List<string>();
			string? currentParent = null;
			foreach (var handle in reader.CustomDebugInformation) {
				var info = reader.GetCustomDebugInformation(handle);
				string parent = RowText(info.Parent);
				if (parent != currentParent) {
					AppendSorted(rowsOfParent);
					currentParent = parent;
				}
				rowsOfParent.Add($"CustomDebugInformation: {parent}, kind {GuidText(info.Kind)}{Environment.NewLine}  {BlobText(info.Value)}");
			}
			AppendSorted(rowsOfParent);
		}

		void AppendSorted(System.Collections.Generic.List<string> lines) {
			lines.Sort(StringComparer.Ordinal);
			foreach (var line in lines)
				text.AppendLine(line);
			lines.Clear();
		}
	}
}
