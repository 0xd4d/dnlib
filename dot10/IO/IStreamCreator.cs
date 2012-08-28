using System;
using System.IO;

namespace dot10.IO {
	/// <summary>
	/// Creates a new stream that accesses part of some data
	/// </summary>
	public interface IStreamCreator : IDisposable {
		/// <summary>
		/// The file name or null if data is not from a file
		/// </summary>
		string Filename { get; }

		/// <summary>
		/// Returns the total length of the original data
		/// </summary>
		long Length { get; }

		/// <summary>
		/// Creates a stream that can access only part of the data
		/// </summary>
		/// <param name="offset">Offset within the original data</param>
		/// <param name="length">Length of section within the original data</param>
		/// <returns>A new stream</returns>
		/// <exception cref="ArgumentOutOfRangeException">If one of the args is invalid</exception>
		Stream Create(FileOffset offset, long length);

		/// <summary>
		/// Creates a stream that can access all data
		/// </summary>
		/// <returns>A new stream</returns>
		Stream CreateFull();
	}
}
