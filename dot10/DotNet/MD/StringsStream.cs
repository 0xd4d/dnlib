using dot10.IO;

namespace dot10.DotNet.MD {
	/// <summary>
	/// Represents the #Strings stream
	/// </summary>
	public class StringsStream : DotNetStream {
		/// <inheritdoc/>
		public StringsStream(IImageStream imageStream, StreamHeader streamHeader)
			: base(imageStream, streamHeader) {
		}

		/// <summary>
		/// Read a <see cref="UTF8String"/>
		/// </summary>
		/// <param name="offset">Offset of string</param>
		/// <returns>A <see cref="UTF8String"/> instance or null if invalid offset</returns>
		public UTF8String Read(uint offset) {
			if (offset >= imageStream.Length)
				return null;
			imageStream.Position = offset;
			var data = imageStream.ReadBytesUntilByte(0);
			if (data == null)
				return null;
			return new UTF8String(data);
		}
	}
}
