// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using dnlib.IO;
using dnlib.W32Resources;

namespace dnlib.PE {
	/// <summary>
	/// Converts <see cref="RVA"/>s to/from <see cref="FileOffset"/>s
	/// </summary>
	public interface IRvaFileOffsetConverter {
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
	}

	/// <summary>
	/// Interface to access a PE image
	/// </summary>
	public interface IPEImage : IRvaFileOffsetConverter, IDisposable {
		/// <summary>
		/// <c>true</c> if image layout is the same as the raw PE image layout, <c>false</c>
		/// if it's the same layout as a PE image loaded by the OS PE loader.
		/// </summary>
		bool IsFileImageLayout { get; }

		/// <summary>
		/// <c>true</c> if some of the memory where the image is located could be unavailable.
		/// This could happen if it's been loaded by the OS loader.
		/// </summary>
		bool MayHaveInvalidAddresses { get; }

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
		/// Gets/sets the Win32 resources. This is <c>null</c> if there are no Win32 resources.
		/// </summary>
		Win32Resources Win32Resources { get; set; }

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

		/// <summary>
		/// Call this to disable memory mapped I/O if it was used to open the file. This must only
		/// be called if no other code is trying to access the memory since that could lead to an
		/// exception.
		/// </summary>
		void UnsafeDisableMemoryMappedIO();

		/// <summary>
		/// <c>true</c> if memory mapped I/O is enabled
		/// </summary>
		bool IsMemoryMappedIO { get; }
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

		/// <summary>
		/// Reads all bytes from the PE image. This may fail if the PE image has been loaded
		/// by the OS loader since there may be memory holes.
		/// </summary>
		/// <param name="self">this</param>
		/// <returns>All bytes of the PE image</returns>
		public static byte[] GetImageAsByteArray(this IPEImage self) {
			using (var reader = self.CreateFullStream())
				return reader.ReadAllBytes();
		}

		/// <summary>
		/// Finds a <see cref="ResourceData"/>
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="type">Type</param>
		/// <param name="name">Name</param>
		/// <param name="langId">Language ID</param>
		/// <returns>The <see cref="ResourceData"/> or <c>null</c> if none found</returns>
		public static ResourceData FindWin32ResourceData(this IPEImage self, ResourceName type, ResourceName name, ResourceName langId) {
			var w32Resources = self.Win32Resources;
			return w32Resources == null ? null : w32Resources.Find(type, name, langId);
		}
	}
}
