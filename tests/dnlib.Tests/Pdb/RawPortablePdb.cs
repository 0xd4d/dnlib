// dnlib: See LICENSE.txt for more info

using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb;
using dnlib.TestUtils;
using dnlib.Tests.Writer;

namespace dnlib.Tests.Pdb {
	/// <summary>
	/// Creates portable PDB files with blobs that a test writes byte by byte, eg. sequence points with values
	/// that no compiler writes. The tables and heaps are written by System.Reflection.Metadata, which doesn't
	/// check what's in a blob.
	/// </summary>
	static class RawPortablePdb {
		/// <summary>
		/// The name of the only method of the module that <see cref="CreateImage"/> creates
		/// </summary>
		public const string MethodName = "Method";

		static readonly System.Lazy<byte[]> image = new System.Lazy<byte[]>(CreateImage);

		/// <summary>
		/// A module with one method (its rid is 1): <c>nop</c> instructions at the offsets 0, 1, 2 and a
		/// <c>ret</c> at offset 3. Its debug directory has the name and the ID of a PDB file; the PDB files of
		/// this class have that ID, or dnlib wouldn't read them.
		/// </summary>
		public static byte[] Image => image.Value;

		static byte[] CreateImage() {
			var created = new CreatedModule();
			created.AddMethod(MethodName, OpCodes.Nop.ToInstruction(), OpCodes.Nop.ToInstruction(), OpCodes.Nop.ToInstruction(), OpCodes.Ret.ToInstruction());
			created.Module.SetPdbState(new PdbState(created.Module, PdbFileKind.PortablePDB));
			return RoundTrip.WriteWithPortablePdb(created.Module).Image;
		}

		/// <summary>
		/// Returns the method of <see cref="Image"/>
		/// </summary>
		public static MethodDef GetMethod(ModuleDef module) => CreatedModule.GetType(module).FindMethod(MethodName)!;

		/// <summary>
		/// Writes the PDB file of <see cref="Image"/>. That module has one method, so the debug metadata must
		/// have one MethodDebugInformation row.
		/// </summary>
		public static byte[] Write(MetadataBuilder debugMetadata) {
			BlobContentId id;
			using (var peReader = new PEReader(ImmutableArray.Create(Image))) {
				var entry = peReader.ReadDebugDirectory().Single(a => a.Type == DebugDirectoryEntryType.CodeView);
				id = new BlobContentId(peReader.ReadCodeViewDebugDirectoryData(entry).Guid, entry.Stamp);
			}
			var rowCounts = new int[MetadataTokens.TableCount];
			rowCounts[(int)TableIndex.MethodDef] = 1;
			var builder = new PortablePdbBuilder(debugMetadata, ImmutableArray.Create(rowCounts), default, _ => id);
			var pdb = new BlobBuilder();
			builder.Serialize(pdb);
			return pdb.ToArray();
		}

		/// <summary>
		/// Creates a PDB file with one document, <c>test.cs</c>, and the sequence points of the method
		/// </summary>
		/// <param name="sequencePoints">The sequence points blob of the method</param>
		public static byte[] WriteSequencePoints(BlobBuilder sequencePoints) {
			var metadata = new MetadataBuilder();
			var document = metadata.AddDocument(metadata.GetOrAddDocumentName("test.cs"), default, default, default);
			metadata.AddMethodDebugInformation(document, metadata.GetOrAddBlob(sequencePoints));
			return Write(metadata);
		}

		/// <summary>
		/// Creates a PDB file with one document whose name is a blob that the test has written
		/// </summary>
		/// <param name="blobs">Blobs that are added to the heap first, eg. the parts of the name. The name has
		/// their heap offsets.</param>
		/// <param name="createName">Gets the heap offsets of <paramref name="blobs"/>, returns the name blob</param>
		public static byte[] WriteDocumentName(byte[][] blobs, System.Func<int[], byte[]> createName) {
			var metadata = new MetadataBuilder();
			var offsets = blobs.Select(a => MetadataTokens.GetHeapOffset(metadata.GetOrAddBlob(a))).ToArray();
			var nameOffset = MetadataTokens.GetHeapOffset(metadata.GetOrAddBlob(createName(offsets)));
			return WriteDocumentName(metadata, nameOffset);
		}

		/// <summary>
		/// Creates a PDB file with one document whose name is at an offset of the #Blob heap where no blob is:
		/// 0 or an offset after the heap's end
		/// </summary>
		public static byte[] WriteDocumentName(int nameOffset) => WriteDocumentName(new MetadataBuilder(), nameOffset);

		static byte[] WriteDocumentName(MetadataBuilder metadata, int nameOffset) {
			var document = metadata.AddDocument(MetadataTokens.DocumentNameBlobHandle(nameOffset), default, default, default);
			metadata.AddMethodDebugInformation(document, default);
			return Write(metadata);
		}
	}
}
