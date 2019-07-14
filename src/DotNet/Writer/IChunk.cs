// dnlib: See LICENSE.txt for more info

using System.IO;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Data that gets written to the file
	/// </summary>
	public interface IChunk {
		/// <summary>
		/// Gets the file offset. This is valid only after <see cref="SetOffset"/> has been called.
		/// </summary>
		FileOffset FileOffset { get; }

		/// <summary>
		/// Gets the RVA. This is valid only after <see cref="SetOffset"/> has been called.
		/// </summary>
		RVA RVA { get; }

		/// <summary>
		/// Called when the file offset and RVA are known
		/// </summary>
		/// <param name="offset">File offset of this chunk</param>
		/// <param name="rva">RVA of this chunk</param>
		void SetOffset(FileOffset offset, RVA rva);

		/// <summary>
		/// Gets the raw file length of this chunk. Must only be called after <see cref="SetOffset"/>
		/// has been called.
		/// </summary>
		/// <returns>Length of this chunk</returns>
		uint GetFileLength();

		/// <summary>
		/// Gets the virtual size of this chunk. Must only be called after <see cref="SetOffset"/>
		/// has been called.
		/// </summary>
		/// <returns>Virtual size of this chunk</returns>
		uint GetVirtualSize();

		/// <summary>
		/// Writes all data to <paramref name="writer"/> at its current location. It's only
		/// called after <see cref="SetOffset"/> and <see cref="GetFileLength"/> have been called.
		/// You cannot assume that <paramref name="writer"/>'s file position is the same as this
		/// chunk's file position.
		/// </summary>
		/// <param name="writer">Destination</param>
		void WriteTo(DataWriter writer);
	}

	/// <summary>
	/// Implemented by <see cref="IChunk"/>s that can reuse the old data location in the original PE file
	/// </summary>
	interface IReuseChunk : IChunk {
		/// <summary>
		/// Returns true if this chunk fits in the old location
		/// </summary>
		/// <param name="origRva">Original RVA of data</param>
		/// <param name="origSize">Size of the original location</param>
		/// <returns></returns>
		bool CanReuse(RVA origRva, uint origSize);
	}

	public static partial class Extensions {
		/// <summary>
		/// Writes all data to <paramref name="writer"/> and verifies that all bytes were written
		/// </summary>
		/// <param name="chunk">this</param>
		/// <param name="writer">Destination</param>
		/// <exception cref="IOException">Not all bytes were written</exception>
		public static void VerifyWriteTo(this IChunk chunk, DataWriter writer) {
			long pos = writer.Position;
			// Uncomment this to add some debug info, useful when comparing old vs new version
			//System.Diagnostics.Debug.WriteLine($" RVA 0x{(uint)chunk.RVA:X8} OFFS 0x{(uint)chunk.FileOffset:X8} VSIZE 0x{chunk.GetVirtualSize():X8} {chunk.GetType().FullName}");
			chunk.WriteTo(writer);
			if (writer.Position - pos != chunk.GetFileLength())
				VerifyWriteToThrow(chunk);
		}

		static void VerifyWriteToThrow(IChunk chunk) =>
			throw new IOException($"Did not write all bytes: {chunk.GetType().FullName}");

		/// <summary>
		/// Writes a data directory
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="chunk">The data</param>
		internal static void WriteDataDirectory(this DataWriter writer, IChunk chunk) {
			if (chunk is null || chunk.GetVirtualSize() == 0)
				writer.WriteUInt64(0);
			else {
				writer.WriteUInt32((uint)chunk.RVA);
				writer.WriteUInt32(chunk.GetVirtualSize());
			}
		}

		internal static void WriteDebugDirectory(this DataWriter writer, DebugDirectory chunk) {
			if (chunk is null || chunk.GetVirtualSize() == 0)
				writer.WriteUInt64(0);
			else {
				writer.WriteUInt32((uint)chunk.RVA);
				writer.WriteUInt32((uint)(chunk.Count * DebugDirectory.HEADER_SIZE));
			}
		}
	}
}
