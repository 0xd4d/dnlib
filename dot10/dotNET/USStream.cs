using System.IO;

namespace dot10.dotNET {
	class USStream : DotNetStream {
		/// <inheritdoc/>
		public USStream(Stream data, StreamHeader streamHeader)
			: base(data, streamHeader) {
		}
	}
}
