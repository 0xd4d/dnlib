using System.IO;
using dot10.IO;

namespace dot10.dotNET {
	class USStream : DotNetStream {
		/// <inheritdoc/>
		public USStream(IImageStream imageStream, StreamHeader streamHeader)
			: base(imageStream, streamHeader) {
		}
	}
}
