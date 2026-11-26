// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using Xunit;
using dnlib.TestUtils;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// The metadata starts with a header (a signature, the version of the runtime as text, and the name,
	/// offset and size of every stream), and the <c>#~</c> stream starts with the header of the tables (which
	/// tables exist, how many rows they have, which are sorted, and whether an index into a heap has 2 or 4
	/// bytes). The fields that dnlib reads are compared with the bytes of the file and with what
	/// System.Reflection.Metadata reads.
	/// </summary>
	public class MetadataHeaderFieldTests {
		static readonly byte[] image = Fixtures.Fixtures.Features.Image;
		static readonly ModuleDefMD module = ModuleDefMD.Load(image);

		static uint MetadataOffset => (uint)module.Metadata.PEImage.ToFileOffset(module.Metadata.ImageCor20Header.Metadata.VirtualAddress);

		[Fact]
		public void MetadataHeaderHasTheValuesOfTheFile() {
			var header = module.Metadata.MetadataHeader;
			int offset = (int)MetadataOffset;

			Assert.Equal(0x424A5342U, header.Signature);
			Assert.Equal(LittleEndian.ToUInt32(image, offset), header.Signature);
			Assert.Equal(LittleEndian.ToUInt16(image, offset + 4), header.MajorVersion);
			Assert.Equal(LittleEndian.ToUInt16(image, offset + 6), header.MinorVersion);
			Assert.Equal((1, 1), (header.MajorVersion, header.MinorVersion));
			Assert.Equal(0U, header.Reserved1);
			Assert.Equal(LittleEndian.ToUInt32(image, offset + 12), header.StringLength);
			// The length is a multiple of 4: there are zero bytes after the text
			Assert.Equal(0U, header.StringLength % 4);
			string versionInFile = Encoding.UTF8.GetString(image, offset + 16, (int)header.StringLength).TrimEnd('\0');
			Assert.Equal(versionInFile, header.VersionString);
			Assert.Equal(module.RuntimeVersion, header.VersionString);

			int storageHeader = offset + 16 + (int)header.StringLength;
			Assert.Equal((uint)storageHeader, (uint)header.StorageHeaderOffset);
			Assert.Equal(image[storageHeader], (byte)header.Flags);
			Assert.Equal(StorageFlags.Normal, header.Flags);
			Assert.Equal(image[storageHeader + 1], header.Reserved2);
			Assert.Equal(LittleEndian.ToUInt16(image, storageHeader + 2), header.Streams);
			Assert.Equal(header.Streams, header.StreamHeaders.Count);

			using (var peReader = new PEReader(new MemoryStream(image)))
				Assert.Equal(peReader.GetMetadataReader().MetadataVersion, header.VersionString);
		}

		// A stream header is the offset of the stream in the metadata, its size, and its name with zero
		// bytes up to the next multiple of 4
		[Fact]
		public void StreamHeadersHaveTheValuesOfTheFile() {
			var header = module.Metadata.MetadataHeader;
			int position = (int)header.StorageHeaderOffset + 4;

			foreach (var stream in header.StreamHeaders) {
				Assert.Equal(LittleEndian.ToUInt32(image, position), stream.Offset);
				Assert.Equal(LittleEndian.ToUInt32(image, position + 4), stream.StreamSize);
				int nameLength = Array.IndexOf(image, (byte)0, position + 8) - (position + 8);
				Assert.Equal(Encoding.ASCII.GetString(image, position + 8, nameLength), stream.Name);
				position += 8 + (nameLength + 4) / 4 * 4;
			}
			Assert.Equal(new[] { "#~", "#Strings", "#US", "#GUID", "#Blob" }, header.StreamHeaders.Select(a => a.Name).ToArray());
		}

		[Fact]
		public void StreamsAreWhereTheirHeadersSay() {
			var metadata = module.Metadata;
			var streams = new Dictionary<string, DotNetStream> {
				{ "#~", metadata.TablesStream }, { "#Strings", metadata.StringsStream }, { "#US", metadata.USStream },
				{ "#GUID", metadata.GuidStream }, { "#Blob", metadata.BlobStream },
			};

			Assert.Equal(metadata.MetadataHeader.StreamHeaders.Count, metadata.AllStreams.Count);
			foreach (var streamHeader in metadata.MetadataHeader.StreamHeaders) {
				var stream = streams[streamHeader.Name];
				Assert.Same(streamHeader, stream.StreamHeader);
				Assert.Equal(streamHeader.Name, stream.Name);
				Assert.Equal(MetadataOffset + streamHeader.Offset, (uint)stream.StartOffset);
				Assert.Equal(streamHeader.StreamSize, stream.StreamLength);
				Assert.Equal((uint)stream.StartOffset + streamHeader.StreamSize, (uint)stream.EndOffset);
				Assert.Contains(stream, metadata.AllStreams);
			}

			using (var peReader = new PEReader(new MemoryStream(image))) {
				var reader = peReader.GetMetadataReader();
				Assert.Equal(MetadataOffset + (uint)reader.GetHeapMetadataOffset(HeapIndex.String), (uint)metadata.StringsStream.StartOffset);
				Assert.Equal(MetadataOffset + (uint)reader.GetHeapMetadataOffset(HeapIndex.UserString), (uint)metadata.USStream.StartOffset);
				Assert.Equal(MetadataOffset + (uint)reader.GetHeapMetadataOffset(HeapIndex.Guid), (uint)metadata.GuidStream.StartOffset);
				Assert.Equal(MetadataOffset + (uint)reader.GetHeapMetadataOffset(HeapIndex.Blob), (uint)metadata.BlobStream.StartOffset);
			}
		}

		[Fact]
		public void TablesHeaderHasTheValuesOfTheFile() {
			var tables = module.TablesStream;
			int offset = (int)tables.StartOffset;

			Assert.Equal(LittleEndian.ToUInt32(image, offset), tables.Reserved1);
			Assert.Equal(0U, tables.Reserved1);
			// The major version is the upper byte
			Assert.Equal((ushort)((image[offset + 4] << 8) | image[offset + 5]), tables.Version);
			Assert.Equal(0x0200, tables.Version);
			Assert.Equal(image[offset + 6], (byte)tables.Flags);
			Assert.Equal(image[offset + 7], tables.Log2Rid);
			Assert.Equal(LittleEndian.ToUInt64(image, offset + 8), tables.ValidMask);
			Assert.Equal(LittleEndian.ToUInt64(image, offset + 16), tables.SortedMask);

			// A small file: every index into a heap has 2 bytes, and there's nothing else in the header
			Assert.Equal((MDStreamFlags)0, tables.Flags);
			Assert.False(tables.HasBigStrings);
			Assert.False(tables.HasBigGUID);
			Assert.False(tables.HasBigBlob);
			Assert.False(tables.HasPadding);
			Assert.False(tables.HasDeltaOnly);
			Assert.False(tables.HasExtraData);
			Assert.False(tables.HasDelete);
			Assert.Equal(0U, tables.ExtraData);
		}

		// The bit of a table in the "valid" mask is set if the file has the table: then its row count is in
		// the header, in the order of the tables
		[Fact]
		public void TablesHaveTheRowCountsOfTheHeader() {
			var tables = module.TablesStream;
			int position = (int)tables.StartOffset + 24;

			// The mask has 64 bits, but there are no tables after the ones of a portable PDB
			Assert.Equal((int)Table.CustomDebugInformation + 1, tables.MDTables.Length);
			Assert.Equal(0UL, tables.ValidMask >> tables.MDTables.Length);
			for (int i = 0; i < tables.MDTables.Length; i++) {
				var table = tables.MDTables[i];
				Assert.Equal((Table)i, table.Table);
				Assert.Same(table, tables.Get((Table)i));
				bool exists = (tables.ValidMask & (1UL << i)) != 0;
				// HasTable() only says that it's a table dnlib knows
				Assert.True(tables.HasTable((Table)i));
				uint rows = 0;
				if (exists) {
					rows = LittleEndian.ToUInt32(image, position);
					position += 4;
				}
				Assert.Equal(rows, table.Rows);
				Assert.Equal(rows == 0, table.IsEmpty);
			}
			Assert.Null(tables.Get((Table)64));
			Assert.False(tables.HasTable((Table)64));
			// The first row of the first table is right after the row counts
			var firstTable = tables.MDTables.First(a => a.Rows != 0);
			Assert.Equal((uint)position, (uint)firstTable.StartOffset);
		}

		[Fact]
		public void TablesHaveTheRowsAndSizesThatSystemReflectionMetadataReads() {
			var tables = module.TablesStream;
			using (var peReader = new PEReader(new MemoryStream(image))) {
				var reader = peReader.GetMetadataReader();
				uint nextOffset = 0;
				foreach (var table in tables.MDTables) {
					// SRM doesn't know the tables after the last one of the specification
					if ((int)table.Table > (int)TableIndex.CustomDebugInformation || !Enum.IsDefined(typeof(Table), table.Table))
						continue;
					var index = (TableIndex)(int)table.Table;
					Assert.Equal((uint)reader.GetTableRowCount(index), table.Rows);
					Assert.Equal(table.Table.ToString(), table.Name);
					Assert.Equal((uint)table.StartOffset + table.Rows * table.RowSize, (uint)table.EndOffset);
					if (table.Rows == 0)
						continue;
					Assert.True((uint)reader.GetTableRowSize(index) == table.RowSize, $"The row size of {table.Table}");
					Assert.Equal(MetadataOffset + (uint)reader.GetTableMetadataOffset(index), (uint)table.StartOffset);
					// The tables follow each other
					if (nextOffset != 0)
						Assert.Equal(nextOffset, (uint)table.StartOffset);
					nextOffset = (uint)table.EndOffset;
				}
			}
		}

		// The columns of a table follow each other in a row
		[Fact]
		public void ColumnsFillTheRowOfTheirTable() {
			foreach (var table in module.TablesStream.MDTables) {
				if (!Enum.IsDefined(typeof(Table), table.Table))
					continue;
				int offset = 0;
				for (int i = 0; i < table.Columns.Count; i++) {
					var column = table.Columns[i];
					Assert.Equal(i, column.Index);
					Assert.Equal(offset, column.Offset);
					Assert.Contains(column.Size, new[] { 1, 2, 4 });
					Assert.False(string.IsNullOrEmpty(column.Name));
					offset += column.Size;
				}
				Assert.Equal((uint)offset, table.RowSize);
				Assert.Same(table.TableInfo.Columns, table.Columns);
				Assert.Equal(table.Table, table.TableInfo.Table);
			}
		}
	}
}
