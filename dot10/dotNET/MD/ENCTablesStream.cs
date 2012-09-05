using dot10.IO;

namespace dot10.dotNET.MD {
	/// <summary>
	/// Reads the #- 'edit and continue' tables stream
	/// </summary>
	public class ENCTablesStream : TablesStream {
		/// <inheritdoc/>
		public ENCTablesStream(IImageStream imageStream, StreamHeader streamHeader)
			: base(imageStream, streamHeader) {
		}
	}
}
