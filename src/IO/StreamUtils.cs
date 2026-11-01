// dnlib: See LICENSE.txt for more info

using System.IO;

namespace dnlib.IO {
	static class StreamUtils {
		/// <summary>
		/// Reads exactly <paramref name="count"/> bytes. <see cref="Stream.Read(byte[], int, int)"/>
		/// can return fewer bytes than requested even if it's not at the end of the stream.
		/// </summary>
		/// <exception cref="IOException">If the stream ends before all bytes have been read</exception>
		public static void ReadAll(Stream stream, byte[] buffer, int offset, int count) {
			while (count > 0) {
				int read = stream.Read(buffer, offset, count);
				if (read <= 0)
					throw new IOException("Could not read data");
				offset += read;
				count -= read;
			}
		}
	}
}
