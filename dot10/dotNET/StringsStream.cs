using System.Text;
using dot10.IO;

namespace dot10.dotNET {
	/// <summary>
	/// Represents the #Strings stream
	/// </summary>
	public class StringsStream : DotNetStream {
		/// <inheritdoc/>
		public StringsStream(IImageStream imageStream, StreamHeader streamHeader)
			: base(imageStream, streamHeader) {
		}

		/// <summary>
		/// Read a <see cref="string"/>
		/// </summary>
		/// <param name="offset">Offset of string</param>
		/// <returns>The UTF-8 decoded string or null if invalid offset</returns>
		public string Read(uint offset) {
			var data = imageStream.ReadBytesUntilByte(0);
			if (data == null)
				return null;
			return Encoding.UTF8.GetString(data);
		}
	}
}
