// dnlib: See LICENSE.txt for more info

namespace dnlib.IO {
	/// <summary>
	/// Interface to access part of some data
	/// </summary>
	public interface IImageStream : IBinaryReader {
		/// <summary>
		/// Returns the file offset of the stream
		/// </summary>
		FileOffset FileOffset { get; }

		/// <summary>
		/// Creates a sub stream that can access parts of this stream
		/// </summary>
		/// <param name="offset">File offset relative to the start of this stream</param>
		/// <param name="length">Length</param>
		/// <returns>A new stream</returns>
		IImageStream Create(FileOffset offset, long length);
	}

	static partial class IOExtensions {
		/// <summary>
		/// Creates a stream that can access all data starting from <paramref name="offset"/>
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="offset">Offset relative to the beginning of the stream</param>
		/// <returns>A new stream</returns>
		public static IImageStream Create(this IImageStream self, FileOffset offset) {
			return self.Create(offset, long.MaxValue);
		}

		/// <summary>
		/// Clones this <see cref="IImageStream"/>
		/// </summary>
		/// <param name="self">this</param>
		/// <returns>A new <see cref="IImageStream"/> instance</returns>
		public static IImageStream Clone(this IImageStream self) {
			return self.Create(0, long.MaxValue);
		}
	}
}
