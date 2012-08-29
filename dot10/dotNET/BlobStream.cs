using System.IO;

namespace dot10.dotNET {
	class BlobStream : DotNetStream {
		/// <inheritdoc/>
		public BlobStream(Stream data, StreamHeader streamHeader)
			: base(data, streamHeader) {
		}
	}
}
