using System.IO;

namespace dot10.dotNET {
	/// <summary>
	/// .NET metadata stream
	/// </summary>
	public class DotNetStream {
		BinaryReader reader;
		StreamHeader streamHeader;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">Stream data</param>
		/// <param name="streamHeader">The stream header</param>
		public DotNetStream(Stream data, StreamHeader streamHeader) {
			this.reader = new BinaryReader(data);
			this.streamHeader = streamHeader;
		}

		/// <inheritdoc/>
		public override string ToString() {
			return string.Format("{0:X8} {1}", reader.BaseStream.Length, streamHeader.Name);
		}
	}
}
