// dnlib: See LICENSE.txt for more info

﻿using dnlib.IO;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// Represents the #Blob stream
	/// </summary>
	public sealed class BlobStream : HeapStream {
		static readonly byte[] noData = new byte[0];

		/// <inheritdoc/>
		public BlobStream() {
		}

		/// <inheritdoc/>
		public BlobStream(IImageStream imageStream, StreamHeader streamHeader)
			: base(imageStream, streamHeader) {
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
				return noData;
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			IImageStream reader;
			int size = GetReader_NoLock(offset, out reader);
			if (size < 0)
				return null;
			return reader.ReadBytes(size);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads data just like <see cref="Read"/>, but returns an empty array if
		/// offset is invalid
		/// </summary>
		/// <param name="offset">Offset of data</param>
		/// <returns>The data</returns>
		public byte[] ReadNoNull(uint offset) {
			return Read(offset) ?? noData;
		}

		/// <summary>
		/// Creates a new sub stream of the #Blob stream that can access one blob
		/// </summary>
		/// <param name="offset">Offset of blob</param>
		/// <returns>A new stream</returns>
		public IImageStream CreateStream(uint offset) {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			IImageStream reader;
			int size = GetReader_NoLock(offset, out reader);
			if (size < 0)
				return MemoryImageStream.CreateEmpty();
			return reader.Create((FileOffset)reader.Position, size);
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		int GetReader_NoLock(uint offset, out IImageStream reader) {
			reader = null;
			if (!IsValidOffset(offset))
				return -1;
			reader = GetReader_NoLock(offset);
			uint length;
			if (!reader.ReadCompressedUInt32(out length))
				return -1;
			if (reader.Position + length < length || reader.Position + length > reader.Length)
				return -1;

			return (int)length;	// length <= 0x1FFFFFFF so this cast does not make it negative
		}
	}
}
