using System.IO;

namespace dot10.dotNET {
	class GuidStream : DotNetStream {
		/// <inheritdoc/>
		public GuidStream(Stream data, StreamHeader streamHeader)
			: base(data, streamHeader) {
		}
	}
}
