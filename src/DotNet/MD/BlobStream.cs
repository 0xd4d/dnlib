// dnlib: See LICENSE.txt for more info

using System;
using dnlib.IO;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// Represents the #Blob stream
	/// </summary>
	public sealed class BlobStream : HeapStream {
		/// <inheritdoc/>
		public BlobStream() {
		}

		/// <inheritdoc/>
		public BlobStream(DataReaderFactory mdReaderFactory, uint metadataBaseOffset, StreamHeader streamHeader)
			: base(mdReaderFactory, metadataBaseOffset, streamHeader) {
		}

		/// <summary>
		/// Reads data
		/// </summary>
		/// <param name="offset">Offset of data</param>
		/// <returns>The data or <c>null</c> if invalid offset</returns>
		public byte[] Read(uint offset) {
			// The CLR has a special check for offset 0. It always interprets it as
			// 0-length data, even if that first byte isn't 0 at all.
			if (offset == 0)
				return Array2.Empty<byte>();
			if (!TryCreateReader(offset, out var reader))
				return null;
			return reader.ToArray();
		}

		/// <summary>
		/// Reads data just like <see cref="Read"/>, but returns an empty array if
		/// offset is invalid
		/// </summary>
		/// <param name="offset">Offset of data</param>
		/// <returns>The data</returns>
		public byte[] ReadNoNull(uint offset) => Read(offset) ?? Array2.Empty<byte>();

		/// <summary>
		/// Creates a reader that can access a blob
		/// </summary>
		/// <param name="offset">Offset of blob</param>
		/// <returns>A new stream</returns>
		public DataReader CreateReader(uint offset) {
			TryCreateReader(offset, out var reader);
			return reader;
		}

		/// <summary>
		/// Creates a reader that can access a blob or returns false on failure
		/// </summary>
		/// <param name="offset">Offset of blob</param>
		/// <param name="reader">Updated with the reader</param>
		/// <returns></returns>
		public bool TryCreateReader(uint offset, out DataReader reader) {
			reader = dataReader;
			if (!IsValidOffset(offset))
				return false;
			reader.Position = offset;
			if (!reader.TryReadCompressedUInt32(out uint length))
				return false;
			if (!reader.CanRead(length))
				return false;
			reader = reader.Slice(reader.Position, length);
			return true;
		}
	}
}
