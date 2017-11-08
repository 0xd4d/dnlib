// dnlib: See LICENSE.txt for more info

﻿using System.IO;
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
		void WriteTo(BinaryWriter writer);
	}

	public static partial class Extensions {
		/// <summary>
		/// Writes all data to <paramref name="writer"/> and verifies that all bytes were written
		/// </summary>
		/// <param name="chunk">this</param>
		/// <param name="writer">Destination</param>
		/// <exception cref="IOException">Not all bytes were written</exception>
		public static void VerifyWriteTo(this IChunk chunk, BinaryWriter writer) {
			long pos = writer.BaseStream.Position;
			chunk.WriteTo(writer);
			if (writer.BaseStream.Position - pos != chunk.GetFileLength())
				throw new IOException("Did not write all bytes");
		}

		/// <summary>
		/// Writes a data directory
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="chunk">The data</param>
		internal static void WriteDataDirectory(this BinaryWriter writer, IChunk chunk) {
			if (chunk == null || chunk.GetVirtualSize() == 0)
				writer.Write(0UL);
			else {
				writer.Write((uint)chunk.RVA);
				writer.Write(chunk.GetVirtualSize());
			}
		}
	}
}
