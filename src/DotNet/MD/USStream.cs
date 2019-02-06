// dnlib: See LICENSE.txt for more info

using System;
using dnlib.IO;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// Represents the #US stream
	/// </summary>
	public sealed class USStream : HeapStream {
		/// <inheritdoc/>
		public USStream() {
		}

		/// <inheritdoc/>
		public USStream(DataReaderFactory mdReaderFactory, uint metadataBaseOffset, StreamHeader streamHeader)
			: base(mdReaderFactory, metadataBaseOffset, streamHeader) {
		}

		/// <summary>
		/// Reads a unicode string
		/// </summary>
		/// <param name="offset">Offset of unicode string</param>
		/// <returns>A string or <c>null</c> if <paramref name="offset"/> is invalid</returns>
		public string Read(uint offset) {
			if (offset == 0)
				return string.Empty;
			if (!IsValidOffset(offset))
				return null;
			var reader = dataReader;
			reader.Position = offset;
			if (!reader.TryReadCompressedUInt32(out uint length))
				return null;
			if (!reader.CanRead(length))
				return null;
			try {
				return reader.ReadUtf16String((int)(length / 2));
			}
			catch (OutOfMemoryException) {
				throw;
			}
			catch {
				// It's possible that an exception is thrown when converting a char* to
				// a string. If so, return an empty string.
				return string.Empty;
			}
		}

		/// <summary>
		/// Reads data just like <see cref="Read"/>, but returns an empty string if
		/// offset is invalid
		/// </summary>
		/// <param name="offset">Offset of unicode string</param>
		/// <returns>The string</returns>
		public string ReadNoNull(uint offset) => Read(offset) ?? string.Empty;
	}
}
