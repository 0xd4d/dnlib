using dot10.IO;

namespace dot10.dotNET {
	public class BlobStream : DotNetStream {
		/// <inheritdoc/>
		public BlobStream(IImageStream imageStream, StreamHeader streamHeader)
			: base(imageStream, streamHeader) {
		}
	}
}
