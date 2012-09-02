using dot10.IO;

namespace dot10.dotNET {
	/// <summary>
	/// Represents the #US stream
	/// </summary>
	public class USStream : DotNetStream {
		/// <inheritdoc/>
		public USStream(IImageStream imageStream, StreamHeader streamHeader)
			: base(imageStream, streamHeader) {
		}
	}
}
