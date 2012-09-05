using dot10.IO;

namespace dot10.dotNET.MD {
	/// <summary>
	/// Reads the #~ compressed tables stream
	/// </summary>
	public class CompressedTablesStream : TablesStream {
		/// <inheritdoc/>
		public CompressedTablesStream(IImageStream imageStream, StreamHeader streamHeader)
			: base(imageStream, streamHeader) {
		}
	}
}
