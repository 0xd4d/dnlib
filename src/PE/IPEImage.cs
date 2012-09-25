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
		/// The file name or <c>null</c> if data is not from a file
		/// </summary>
		string FileName { get; }

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
		IImageStream CreateStream(FileOffset offset);

		/// <summary>
		/// Creates a stream to access part of the PE image from <paramref name="offset"/>
		/// with length <paramref name="length"/>
		/// </summary>
		/// <param name="offset">File offset</param>
		/// <param name="length">Length of data</param>
		/// <returns>A new stream</returns>
		/// <exception cref="ArgumentOutOfRangeException">If any arg is invalid</exception>
		IImageStream CreateStream(FileOffset offset, long length);

		/// <summary>
		/// Creates a stream to access the full PE image
		/// </summary>
		/// <returns>A new stream</returns>
		IImageStream CreateFullStream();
	}

	public static partial class PEExtensions {
		/// <summary>
		/// Creates a stream to access part of the PE image from <paramref name="rva"/>
		/// to the end of the image
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="rva">RVA</param>
		/// <returns>A new stream</returns>
		/// <exception cref="ArgumentOutOfRangeException">If the arg is invalid</exception>
		public static IImageStream CreateStream(this IPEImage self, RVA rva) {
			return self.CreateStream(self.ToFileOffset(rva));
		}

		/// <summary>
		/// Creates a stream to access part of the PE image from <paramref name="rva"/>
		/// with length <paramref name="length"/>
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="rva">RVA</param>
		/// <param name="length">Length of data</param>
		/// <returns>A new stream</returns>
		/// <exception cref="ArgumentOutOfRangeException">If any arg is invalid</exception>
		public static IImageStream CreateStream(this IPEImage self, RVA rva, long length) {
			return self.CreateStream(self.ToFileOffset(rva), length);
		}
	}
}
