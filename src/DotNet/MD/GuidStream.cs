// dnlib: See LICENSE.txt for more info

using System;
using dnlib.IO;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// Represents the #GUID stream
	/// </summary>
	public sealed class GuidStream : HeapStream {
		/// <inheritdoc/>
		public GuidStream() {
		}

		/// <inheritdoc/>
		public GuidStream(DataReaderFactory mdReaderFactory, uint metadataBaseOffset, StreamHeader streamHeader)
			: base(mdReaderFactory, metadataBaseOffset, streamHeader) {
		}

		/// <inheritdoc/>
		public override bool IsValidIndex(uint index) => index == 0 || (index <= 0x10000000 && IsValidOffset((index - 1) * 16, 16));

		/// <summary>
		/// Read a <see cref="Guid"/>
		/// </summary>
		/// <param name="index">Index into this stream</param>
		/// <returns>A <see cref="Guid"/> or <c>null</c> if <paramref name="index"/> is 0 or invalid</returns>
		public Guid? Read(uint index) {
			if (index == 0 || !IsValidIndex(index))
				return null;
			var reader = dataReader;
			reader.Position = (index - 1) * 16;
			return reader.ReadGuid();
		}
	}
}
