using dot10.IO;

namespace dot10.dotNET.MD {
	/// <summary>
	/// Represents the #Blob stream
	/// </summary>
	public class BlobStream : DotNetStream {
		static readonly byte[] noData = new byte[0];

		/// <inheritdoc/>
		public BlobStream(IImageStream imageStream, StreamHeader streamHeader)
			: base(imageStream, streamHeader) {
		}

		/// <summary>
		/// Reads data
		/// </summary>
		/// <param name="offset">Offset of data</param>
		/// <returns>The data or null if invalid offset</returns>
		public byte[] Read(uint offset) {
			// The CLR has a special check for offset 0. It always interprets it as
			// 0-length data, even if that first byte isn't 0 at all.
			if (offset == 0)
				return noData;
			if (!IsValidOffset(offset))
				return null;
			imageStream.Position = offset;
			uint length;
			if (!imageStream.ReadCompressedUInt32(out length))
				return null;
			if (imageStream.Position + length < length || imageStream.Position + length > imageStream.Length)
				return null;
			return imageStream.ReadBytes((int)length);	// length <= 0x1FFFFFFF so this cast does not make it negative
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
	}
}
