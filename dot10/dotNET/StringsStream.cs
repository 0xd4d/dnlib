using System.IO;

namespace dot10.dotNET {
	class StringsStream : DotNetStream {
		/// <inheritdoc/>
		public StringsStream(Stream data, StreamHeader streamHeader)
			: base(data, streamHeader) {
		}
	}
}
