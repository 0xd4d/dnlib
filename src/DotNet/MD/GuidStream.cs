// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics.CodeAnalysis;
using dnlib.IO;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// Represents the #GUID stream
	/// </summary>
	public sealed class GuidStream : HeapStream {
		DataReaderFactory? mdReaderFactory;
		readonly uint streamOffset;

		/// <inheritdoc/>
		public GuidStream() {
		}

		/// <inheritdoc/>
		public GuidStream(DataReaderFactory mdReaderFactory, uint metadataBaseOffset, StreamHeader streamHeader)
			: base(mdReaderFactory, metadataBaseOffset, streamHeader) {
			this.mdReaderFactory = mdReaderFactory;
			streamOffset = metadataBaseOffset + streamHeader.Offset;
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing) {
			if (disposing)
				mdReaderFactory = null;
			base.Dispose(disposing);
		}

		/// <inheritdoc/>
		public override bool IsValidIndex(uint index) => index == 0 || (index <= 0x10000000 && (IsValidOffset((index - 1) * 16, 16) || CanReadPastEnd((index - 1) * 16)));

		// The CLR only checks that the GUID starts in the heap (StgPoolReadOnly::GetGuid()), so if the size of the heap
		// isn't a multiple of 16, it reads the bytes that follow the heap.
		[MemberNotNullWhen(true, nameof(mdReaderFactory))]
		bool CanReadPastEnd(uint offset) =>
			offset < dataReader.Length && mdReaderFactory is DataReaderFactory factory &&
			(ulong)streamOffset + offset + 16 <= factory.Length;

		/// <summary>
		/// Read a <see cref="Guid"/>
		/// </summary>
		/// <param name="index">Index into this stream</param>
		/// <returns>A <see cref="Guid"/> or <c>null</c> if <paramref name="index"/> is 0 or invalid</returns>
		public Guid? Read(uint index) {
			if (index == 0 || index > 0x10000000)
				return null;
			uint offset = (index - 1) * 16;
			if (IsValidOffset(offset, 16)) {
				var reader = dataReader;
				reader.Position = offset;
				return reader.ReadGuid();
			}
			if (CanReadPastEnd(offset))
				return mdReaderFactory.CreateReader(streamOffset + offset, 16U).ReadGuid();
			return null;
		}
	}
}
