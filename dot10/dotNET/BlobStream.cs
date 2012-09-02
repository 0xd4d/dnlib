using dot10.IO;

namespace dot10.dotNET {
	/// <summary>
	/// Represents the #Blob stream
	/// </summary>
	public class BlobStream : DotNetStream {
		/// <inheritdoc/>
		public BlobStream(IImageStream imageStream, StreamHeader streamHeader)
			: base(imageStream, streamHeader) {
		}
	}
}
