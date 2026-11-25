// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using dnlib.DotNet;
using dnlib.DotNet.Pdb;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Pdb {
	/// <summary>
	/// The documents (source files) of a <see cref="PdbState"/>. dnlib reads the debug info of a method when
	/// its body is read for the first time, and the sequence points must then get the documents that the
	/// <see cref="PdbState"/> already has, even if the user has changed them.
	/// </summary>
	public class PdbDocumentTests {
		static PdbDocument CreateDocument(string url) =>
			new PdbDocument(url, PdbDocumentConstants.LanguageCSharp, PdbDocumentConstants.LanguageVendorMicrosoft, PdbDocumentConstants.DocumentTypeText, Guid.Empty, Array.Empty<byte>());

		static IEnumerable<PdbDocument> GetDocumentsOfSequencePoints(ModuleDef module) =>
			module.GetTypes().SelectMany(a => a.Methods).SelectMany(a => a.Body!.Instructions).Select(a => a.SequencePoint?.Document).OfType<PdbDocument>().Distinct();

		static List<string> ReadDocumentNamesWithSrm(byte[] pdb) {
			using (var provider = MetadataReaderProvider.FromPortablePdbImage(ImmutableArray.Create(pdb))) {
				var reader = provider.GetMetadataReader();
				return reader.Documents.Select(a => reader.GetString(reader.GetDocument(a).Name)).ToList();
			}
		}

		[Fact]
		public void SequencePointsUseTheDocumentOfThePdbState() {
			using (var module = DebugInfoSource.Load()) {
				var document = module.PdbState!.Documents.Single();
				Assert.Equal("DebugInfo.cs", document.Url);

				Assert.Same(document, GetDocumentsOfSequencePoints(module).Single());
			}
		}

		// Fixed in 1d1885e0 (issue 566): the method bodies looked for their document by its name. If the name
		// was changed before the bodies were read, a second document with the old name was added, and the
		// writer threw.
		[Fact]
		public void DocumentCanBeRenamedBeforeTheMethodBodiesAreRead() {
			using (var module = DebugInfoSource.Load()) {
				var document = module.PdbState!.Documents.Single();
				document.Url = "Renamed.cs";

				Assert.Same(document, GetDocumentsOfSequencePoints(module).Single());
				Assert.Same(document, module.PdbState.Documents.Single());
				var written = RoundTrip.WriteWithPortablePdb(module);
				Assert.Equal(new[] { "Renamed.cs" }, ReadDocumentNamesWithSrm(written.Pdb!));
			}
		}

		// Fixed in 690f4cff: the documents were keys of a dictionary, and the key is the name, the language and
		// the checksum. A document that was changed wasn't found anymore.
		[Fact]
		public void ChangedDocumentIsStillFound() {
			using (var module = DebugInfoSource.Load()) {
				var pdbState = module.PdbState!;
				var document = pdbState.Documents.Single();
				document.Url = "Renamed.cs";
				document.Language = PdbDocumentConstants.LanguageVisualBasic;
				document.CheckSum = new byte[] { 1, 2, 3 };

				Assert.Same(document, pdbState.GetExisting(document));
				Assert.Same(document, pdbState.Add(document));
				Assert.Same(document, pdbState.Documents.Single());
				Assert.True(pdbState.Remove(document));
				Assert.False(pdbState.HasDocuments);
				Assert.False(pdbState.Remove(document));
			}
		}

		// Fixed in bd7e8856: the removed document was a new document for the PdbState when it was added again,
		// so the method bodies created another one with the old name
		[Fact]
		public void DocumentCanBeRemovedChangedAndAddedAgain() {
			using (var module = DebugInfoSource.Load()) {
				var pdbState = module.PdbState!;
				var document = pdbState.Documents.Single();
				Assert.True(pdbState.Remove(document));
				document.Url = "Renamed.cs";
				pdbState.Add(document);

				Assert.Same(document, GetDocumentsOfSequencePoints(module).Single());
				Assert.Same(document, pdbState.Documents.Single());
			}
		}

		// Fixed in 586b5d2b: only the names were compared, and the case of the letters was ignored. Two
		// documents with other checksums or languages are two rows in the PDB file.
		[Fact]
		public void DocumentsAreEqualIfNameLanguageAndChecksumAreEqual() {
			var document = CreateDocument("File.cs");

			Assert.Equal(document, CreateDocument("File.cs"));
			Assert.Equal(document.GetHashCode(), CreateDocument("File.cs").GetHashCode());
			Assert.NotEqual(document, CreateDocument("file.cs"));

			var otherLanguage = CreateDocument("File.cs");
			otherLanguage.Language = PdbDocumentConstants.LanguageVisualBasic;
			Assert.NotEqual(document, otherLanguage);

			var otherChecksum = CreateDocument("File.cs");
			otherChecksum.CheckSum = new byte[] { 1 };
			Assert.NotEqual(document, otherChecksum);

			var otherChecksumAlgorithm = CreateDocument("File.cs");
			otherChecksumAlgorithm.CheckSumAlgorithmId = PdbDocumentConstants.HashSHA256;
			Assert.NotEqual(document, otherChecksumAlgorithm);
		}

		[Fact]
		public void AddingAnEqualDocumentReturnsTheExistingOne() {
			using (var module = DebugInfoSource.Load()) {
				var pdbState = module.PdbState!;
				var document = pdbState.Add(CreateDocument("New.cs"));

				Assert.Same(document, pdbState.Add(CreateDocument("New.cs")));
				Assert.Same(document, pdbState.GetExisting(CreateDocument("New.cs")));
				Assert.Null(pdbState.GetExisting(CreateDocument("new.cs")));
				Assert.NotSame(document, pdbState.Add(CreateDocument("new.cs")));
				Assert.Equal(new[] { "DebugInfo.cs", "New.cs", "new.cs" }, pdbState.Documents.Select(a => a.Url).OrderBy(a => a, StringComparer.Ordinal));
			}
		}

		// Documents that only differ in the case of their names are all written
		[Fact]
		public void DocumentsWithNamesThatOnlyDifferInCaseAreAllWritten() {
			using (var module = DebugInfoSource.Load()) {
				var sequencePoints = DebugInfoSource.GetMethod(module, "Sum").Body!.Instructions.Select(a => a.SequencePoint).OfType<dnlib.DotNet.Pdb.SequencePoint>().ToList();
				sequencePoints[0].Document = module.PdbState!.Add(CreateDocument("debuginfo.cs"));
				sequencePoints[1].Document = module.PdbState.Add(CreateDocument("DEBUGINFO.cs"));

				var written = RoundTrip.WriteWithPortablePdb(module);

				Assert.Equal(new[] { "DEBUGINFO.cs", "DebugInfo.cs", "debuginfo.cs" }, ReadDocumentNamesWithSrm(written.Pdb!).OrderBy(a => a, StringComparer.Ordinal));
			}
		}

		// A document that a sequence point uses is written even if it was removed from the list of documents
		[Fact]
		public void AllDocumentsCanBeRemovedButTheOnesOfSequencePointsAreStillWritten() {
			using (var module = DebugInfoSource.Load()) {
				var state = module.PdbState!;
				state.Add(CreateDocument("Unused.cs"));

				var removed = state.RemoveAllDocuments(returnDocs: true)!;

				Assert.Equal(new[] { "DebugInfo.cs", "Unused.cs" }, removed.Select(a => a.Url).OrderBy(a => a, StringComparer.Ordinal));
				Assert.Empty(state.Documents);
				Assert.Null(state.RemoveAllDocuments(returnDocs: false));
				var written = RoundTrip.WriteWithPortablePdb(module);
				Assert.Equal(new[] { "DebugInfo.cs" }, ReadDocumentNamesWithSrm(written.Pdb!));
			}
		}

		[Fact]
		public void NoDocumentIsWrittenIfTheSequencePointsAreRemovedToo() {
			using (var module = DebugInfoSource.Load()) {
				foreach (var instruction in module.GetTypes().SelectMany(a => a.Methods).SelectMany(a => a.Body!.Instructions))
					instruction.SequencePoint = null;
				module.PdbState!.RemoveAllDocuments();

				var written = RoundTrip.WriteWithPortablePdb(module);

				Assert.Empty(ReadDocumentNamesWithSrm(written.Pdb!));
			}
		}
	}
}
