// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.IO;
using dnlib.DotNet.Pdb;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Debug directory entry
	/// </summary>
	public sealed class DebugDirectoryEntry {
		/// <summary>
		/// Gets the header
		/// </summary>
		public IMAGE_DEBUG_DIRECTORY DebugDirectory;

		/// <summary>
		/// Gets the data
		/// </summary>
		public readonly IChunk Chunk;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="chunk">Data</param>
		public DebugDirectoryEntry(IChunk chunk) {
			Chunk = chunk;
		}
	}

	/// <summary>
	/// Debug directory chunk
	/// </summary>
	public sealed class DebugDirectory : IChunk {
		/// <summary>Default debug directory alignment</summary>
		public const uint DEFAULT_DEBUGDIRECTORY_ALIGNMENT = 4;
		const int HEADER_SIZE = 28;

		FileOffset offset;
		RVA rva;
		uint length;
		readonly List<DebugDirectoryEntry> entries;

		/// <inheritdoc/>
		public FileOffset FileOffset {
			get { return offset; }
		}

		/// <inheritdoc/>
		public RVA RVA {
			get { return rva; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public DebugDirectory() {
			entries = new List<DebugDirectoryEntry>();
		}

		/// <summary>
		/// Adds data
		/// </summary>
		/// <param name="data">Data</param>
		/// <returns></returns>
		public DebugDirectoryEntry Add(byte[] data) {
			return Add(new ByteArrayChunk(data));
		}

		/// <summary>
		/// Adds data
		/// </summary>
		/// <param name="chunk">Data</param>
		/// <returns></returns>
		public DebugDirectoryEntry Add(IChunk chunk) {
			var entry = new DebugDirectoryEntry(chunk);
			entries.Add(entry);
			return entry;
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;

			length = HEADER_SIZE * (uint)entries.Count;
			foreach (var entry in entries) {
				length = Utils.AlignUp(length, DEFAULT_DEBUGDIRECTORY_ALIGNMENT);
				entry.Chunk.SetOffset(offset + length, rva + length);
				length += entry.Chunk.GetFileLength();
			}
		}

		/// <inheritdoc/>
		public uint GetFileLength() {
			return length;
		}

		/// <inheritdoc/>
		public uint GetVirtualSize() {
			return GetFileLength();
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			uint offset = 0;
			foreach (var entry in entries) {
				writer.Write(entry.DebugDirectory.Characteristics);
				writer.Write(entry.DebugDirectory.TimeDateStamp);
				writer.Write(entry.DebugDirectory.MajorVersion);
				writer.Write(entry.DebugDirectory.MinorVersion);
				writer.Write((uint)entry.DebugDirectory.Type);
				writer.Write(entry.Chunk.GetFileLength());
				writer.Write((uint)entry.Chunk.RVA);
				writer.Write((uint)entry.Chunk.FileOffset);
				offset += HEADER_SIZE;
			}

			foreach (var entry in entries) {
				WriteAlign(writer, ref offset);
				entry.Chunk.VerifyWriteTo(writer);
				offset += entry.Chunk.GetFileLength();
			}
		}

		static void WriteAlign(BinaryWriter writer, ref uint offs) {
			uint align = Utils.AlignUp(offs, DEFAULT_DEBUGDIRECTORY_ALIGNMENT) - offs;
			offs += align;
			writer.WriteZeros((int)align);
		}
	}
}
