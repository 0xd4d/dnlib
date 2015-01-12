// dnlib: See LICENSE.txt for more info

﻿using System;
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
		public USStream(IImageStream imageStream, StreamHeader streamHeader)
			: base(imageStream, streamHeader) {
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
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			var reader = GetReader_NoLock(offset);
			uint length;
			if (!reader.ReadCompressedUInt32(out length))
				return null;
			if (reader.Position + length < length || reader.Position + length > reader.Length)
				return null;
			try {
				return reader.ReadString((int)(length / 2));
			}
			catch (OutOfMemoryException) {
				throw;
			}
			catch {
				// It's possible that an exception is thrown when converting a char* to
				// a string. If so, return an empty string.
				return string.Empty;
			}
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Reads data just like <see cref="Read"/>, but returns an empty string if
		/// offset is invalid
		/// </summary>
		/// <param name="offset">Offset of unicode string</param>
		/// <returns>The string</returns>
		public string ReadNoNull(uint offset) {
			return Read(offset) ?? string.Empty;
		}
	}
}
