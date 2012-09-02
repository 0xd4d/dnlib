using dot10.IO;

namespace dot10.dotNET {
	class GuidStream : DotNetStream {
		/// <inheritdoc/>
		public GuidStream(IImageStream imageStream, StreamHeader streamHeader)
			: base(imageStream, streamHeader) {
		}
	}
}
