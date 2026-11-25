// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Pdb {
	/// <summary>
	/// The name of a document in a portable PDB is a blob: a separator char and the offsets of other blobs
	/// that are the parts of the name, eg. <c>/</c> and the blobs <c>src</c> and <c>a.cs</c>. A compiler writes
	/// the parts of a path. These tests write names that no compiler writes: empty parts, no separator, a
	/// separator that is a letter. They're the tests of System.Reflection.Metadata, which must read the same.
	/// </summary>
	public class DocumentNameBlobTests {
		static byte[] Utf8(string text) => System.Text.Encoding.UTF8.GetBytes(text);

		static string ReadWithDnlib(byte[] pdb) {
			using (var module = RoundTrip.LoadWithPdb(RawPortablePdb.Image, pdb))
				return module.PdbState!.Documents.Single().Url;
		}

		static string ReadWithSrm(byte[] pdb) {
			using (var provider = MetadataReaderProvider.FromPortablePdbImage(ImmutableArray.Create(pdb))) {
				var reader = provider.GetMetadataReader();
				return reader.GetString(reader.GetDocument(reader.Documents.Single()).Name);
			}
		}

		// separator: the first byte of the name. parts: the texts of the part blobs. partIndexes: the parts of
		// the name in their order, -1 is the offset 0 (the empty blob that every #Blob heap starts with).
		[Theory]
		[InlineData('\\', new[] { "ab", "xyz" }, new[] { 0, 1 }, @"ab\xyz")]
		[InlineData('/', new[] { "src", "a.cs" }, new[] { 0, 1 }, "src/a.cs")]
		[InlineData('\\', new[] { "a" }, new[] { -1, 0, -1, 0, -1 }, @"\a\\a\")]        // empty parts
		[InlineData('\0', new[] { "a" }, new[] { -1, 0, -1, 0, -1 }, "aa")]             // no separator
		[InlineData('\0', new[] { "a" }, new[] { -1, -1, -1, -1, -1 }, "")]             // and only empty parts
		[InlineData('\0', new[] { "a" }, new int[0], "")]                               // no parts
		[InlineData('/', new[] { "a" }, new int[0], "")]
		[InlineData('a', new[] { "b" }, new[] { 0, 0 }, "bab")]                         // the separator is a letter
		[InlineData('a', new[] { "ሴ", "b" }, new[] { 1, 0, 1 }, "baሴab")]     // a part that isn't ASCII
		[InlineData('/', new[] { "a/b", "c" }, new[] { 0, 1 }, "a/b/c")]                // a separator in a part
		public void NameIsItsPartsWithTheSeparatorBetweenThem(char separator, string[] parts, int[] partIndexes, string expected) {
			var pdb = RawPortablePdb.WriteDocumentName(parts.Select(Utf8).ToArray(), offsets => {
				var name = new BlobBuilder();
				name.WriteByte((byte)separator);
				foreach (int index in partIndexes)
					name.WriteCompressedInteger(index < 0 ? 0 : offsets[index]);
				return name.ToArray();
			});

			Assert.Equal(expected, ReadWithDnlib(pdb));
			Assert.Equal(expected, ReadWithSrm(pdb));
		}

		// Fixed in the commit that added this test: a document without a name has the offset 0, the empty blob. dnlib
		// tried to read its separator, and then didn't read the PDB file at all.
		[Fact]
		public void DocumentWithoutANameHasAnEmptyName() {
			var pdb = RawPortablePdb.WriteDocumentName(0);

			Assert.Equal("", ReadWithDnlib(pdb));
		}

		// A name whose offset is after the end of the heap. System.Reflection.Metadata throws.
		[Fact]
		public void NameThatIsNotInTheHeapIsEmpty() {
			var pdb = RawPortablePdb.WriteDocumentName(0x1000);

			Assert.Equal("", ReadWithDnlib(pdb));
			Assert.Throws<BadImageFormatException>(() => ReadWithSrm(pdb));
		}

		// A part whose offset is after the end of the heap is an empty part. System.Reflection.Metadata throws.
		[Fact]
		public void PartThatIsNotInTheHeapIsEmpty() {
			var pdb = RawPortablePdb.WriteDocumentName(new[] { Utf8("a") }, offsets => {
				var name = new BlobBuilder();
				name.WriteByte((byte)'/');
				name.WriteCompressedInteger(offsets[0]);
				name.WriteCompressedInteger(0x1000);
				name.WriteCompressedInteger(offsets[0]);
				return name.ToArray();
			});

			Assert.Equal("a//a", ReadWithDnlib(pdb));
			Assert.Throws<BadImageFormatException>(() => ReadWithSrm(pdb));
		}

		// The separator is one UTF-8 char. System.Reflection.Metadata only reads an ASCII char and throws for
		// every other first byte; dnlib also reads a char of several bytes.
		[Fact]
		public void SeparatorCanBeACharOfSeveralBytes() {
			var pdb = RawPortablePdb.WriteDocumentName(new[] { Utf8("a"), Utf8("b") }, offsets => {
				var name = new BlobBuilder();
				name.WriteBytes(Utf8("∕"));
				name.WriteCompressedInteger(offsets[0]);
				name.WriteCompressedInteger(offsets[1]);
				return name.ToArray();
			});

			Assert.Equal("a∕b", ReadWithDnlib(pdb));
			Assert.Throws<BadImageFormatException>(() => ReadWithSrm(pdb));
		}

		// A separator that isn't valid UTF-8 is read as the replacement char U+FFFD, eg. a byte that can only be
		// the second byte of a char
		[Fact]
		public void SeparatorThatIsNotValidUtf8IsTheReplacementChar() {
			var pdb = RawPortablePdb.WriteDocumentName(new[] { Utf8("a"), Utf8("b") }, offsets => {
				var name = new BlobBuilder();
				name.WriteByte(0x80);
				name.WriteCompressedInteger(offsets[0]);
				name.WriteCompressedInteger(offsets[1]);
				return name.ToArray();
			});

			Assert.Equal("a\uFFFDb", ReadWithDnlib(pdb));
			Assert.Throws<BadImageFormatException>(() => ReadWithSrm(pdb));
		}

		// Fixed in the commit that added this test: dnlib read after the end of these names and then didn't
		// read the PDB file at all. The name now ends where nothing more can be read.
		[Theory]
		[InlineData(new byte[] { 0xE2 }, "")]                   // the first byte of a separator of three bytes
		[InlineData(new byte[] { 0xE2, 0x88 }, "")]             // its first two bytes
		[InlineData(new byte[] { (byte)'/', 1, 0x80 }, "a")]    // the first byte of an offset of two bytes
		[InlineData(new byte[] { (byte)'/', 1, 1, 0xC0, 0, 0 }, "a/a")]  // three bytes of an offset of four bytes
		public void NameThatIsCutOffEndsThere(byte[] name, string expected) {
			// The first blob that is added to a heap has the offset 1
			var pdb = RawPortablePdb.WriteDocumentName(new[] { Utf8("a") }, offsets => {
				Assert.Equal(1, offsets[0]);
				return name;
			});

			Assert.Equal(expected, ReadWithDnlib(pdb));
			Assert.Throws<BadImageFormatException>(() => ReadWithSrm(pdb));
		}

		// Two documents: the second name starts with the bytes that the first, cut off name has
		[Fact]
		public void CutOffSeparatorIsNotUsedForTheNextName() {
			var metadata = new MetadataBuilder();
			var partOffset = MetadataTokens.GetHeapOffset(metadata.GetOrAddBlob(Utf8("a")));
			var cutOff = metadata.GetOrAddBlob(new byte[] { 0xE2 });
			var name = new BlobBuilder();
			name.WriteBytes(Utf8("\u2215"));
			name.WriteCompressedInteger(partOffset);
			name.WriteCompressedInteger(partOffset);
			var valid = metadata.GetOrAddBlob(name);
			var document = metadata.AddDocument(MetadataTokens.DocumentNameBlobHandle(MetadataTokens.GetHeapOffset(cutOff)), default, default, default);
			metadata.AddDocument(MetadataTokens.DocumentNameBlobHandle(MetadataTokens.GetHeapOffset(valid)), default, default, default);
			metadata.AddMethodDebugInformation(document, default);
			var pdb = RawPortablePdb.Write(metadata);

			using (var module = RoundTrip.LoadWithPdb(RawPortablePdb.Image, pdb))
				Assert.Equal(new[] { "", "a\u2215a" }, module.PdbState!.Documents.Select(a => a.Url));
		}
	}
}
