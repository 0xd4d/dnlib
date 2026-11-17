// dnlib: See LICENSE.txt for more info

using dnlib.IO;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// A custom .NET metadata stream
	/// </summary>
	public class CustomDotNetStream : DotNetStream {
		/// <summary>
		/// Constructor
		/// </summary>
		public CustomDotNetStream() { }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="mdReaderFactory">Data reader factory</param>
		/// <param name="metadataBaseOffset">Offset of metadata</param>
		/// <param name="streamHeader">The stream header</param>
		public CustomDotNetStream(DataReaderFactory mdReaderFactory, uint metadataBaseOffset, StreamHeader streamHeader)
			: base(mdReaderFactory, metadataBaseOffset, streamHeader) {
		}
	}
}
