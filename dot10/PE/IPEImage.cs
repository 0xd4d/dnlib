using System;
using System.IO;
using System.Collections.Generic;
using dot10.IO;

namespace dot10.PE {
	/// <summary>
	/// Interface to access a PE image
	/// </summary>
	public interface IPEImage : IDisposable {
		/// <summary>
		/// Returns the DOS header
		/// </summary>
		ImageDosHeader ImageDosHeader { get; }

		/// <summary>
		/// Returns the NT headers
		/// </summary>
		ImageNTHeaders ImageNTHeaders { get; }

		/// <summary>
		/// Returns the section headers
		/// </summary>
		IList<ImageSectionHeader> ImageSectionHeaders { get; }

		/// <summary>
		/// Converts a <see cref="FileOffset"/> to an <see cref="RVA"/>
		/// </summary>
		/// <param name="offset">The file offset to convert</param>
		/// <returns>The RVA</returns>
		RVA ToRVA(FileOffset offset);

		/// <summary>
		/// Converts an <see cref="RVA"/> to a <see cref="FileOffset"/>
		/// </summary>
		/// <param name="rva">The RVA to convert</param>
		/// <returns>The file offset</returns>
		FileOffset ToFileOffset(RVA rva);

		/// <summary>
		/// Creates a stream to access part of the PE image from <paramref name="offset"/>
		/// to the end of the image
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <returns>A new stream</returns>
		/// <exception cref="ArgumentOutOfRangeException">If the arg is invalid</exception>
		Stream CreateStream(FileOffset offset);

		/// <summary>
		/// Creates a stream to access part of the PE image from <paramref name="offset"/>
		/// with length <paramref name="length"/>
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <param name="length">Length of data</param>
		/// <returns>A new stream</returns>
		/// <exception cref="ArgumentOutOfRangeException">If any of the arg is invalid</exception>
		Stream CreateStream(FileOffset offset, long length);

		/// <summary>
		/// Creates a stream to access part of the PE image from <paramref name="rva"/>
		/// to the end of the image
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <returns>A new stream</returns>
		/// <exception cref="ArgumentOutOfRangeException">If the arg is invalid</exception>
		Stream CreateStream(RVA rva);

		/// <summary>
		/// Creates a stream to access part of the PE image from <paramref name="rva"/>
		/// with length <paramref name="length"/>
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <param name="length">Length of data</param>
		/// <returns>A new stream</returns>
		/// <exception cref="ArgumentOutOfRangeException">If any of the arg is invalid</exception>
		Stream CreateStream(RVA rva, long length);

		/// <summary>
		/// Creates a stream to access the full PE image
		/// </summary>
		/// <returns>A new stream</returns>
		Stream CreateFullStream();
	}
}
