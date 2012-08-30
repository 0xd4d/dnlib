using System.IO;
using dot10.IO;

namespace dot10.dotNET {
	class StringsStream : DotNetStream {
		/// <inheritdoc/>
		public StringsStream(IImageStream imageStream, StreamHeader streamHeader)
			: base(imageStream, streamHeader) {
		}
	}
}
