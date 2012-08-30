using System;
using System.IO;

namespace dot10.IO {
	/// <summary>
	/// Interface to access part of some data
	/// </summary>
	public interface IImageStream : IBinaryReader {
		/// <summary>
		/// Returns the file offset of the stream
		/// </summary>
		FileOffset FileOffset { get; }
	}
}
