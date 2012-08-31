using dot10.IO;

namespace dot10.dotNET {
	/// <summary>
	/// Reads the #- 'edit and continue' tables stream
	/// </summary>
	class ENCTablesStream : TablesStream {
		/// <inheritdoc/>
		public ENCTablesStream(IImageStream imageStream, StreamHeader streamHeader)
			: base(imageStream, streamHeader) {
		}
	}
}
