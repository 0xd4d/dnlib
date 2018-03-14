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
		/// The filename or null if the data is not from a file
		/// </summary>
		string Filename { get; }

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
		/// Returns the debug directories
		/// </summary>
		IList<ImageDebugDirectory> ImageDebugDirectories { get; }

		/// <summary>
		/// Gets/sets the Win32 resources. This is <c>null</c> if there are no Win32 resources.
		/// </summary>
		Win32Resources Win32Resources { get; set; }

		/// <summary>
		/// Gets the <see cref="DataReader"/> factory
		/// </summary>
		DataReaderFactory DataReaderFactory { get; }

		/// <summary>
		/// Creates a <see cref="DataReader"/> from <paramref name="offset"/> to the end of the image
		/// </summary>
		/// <param name="offset">Offset of data</param>
		/// <returns></returns>
		DataReader CreateReader(FileOffset offset);

		/// <summary>
		/// Creates a <see cref="DataReader"/>
		/// </summary>
		/// <param name="offset">Offset of data</param>
		/// <param name="length">Length of data</param>
		/// <returns></returns>
		DataReader CreateReader(FileOffset offset, uint length);

		/// <summary>
		/// Creates a <see cref="DataReader"/> from <paramref name="rva"/> to the end of the image
		/// </summary>
		/// <param name="rva">RVA of data</param>
		/// <returns></returns>
		DataReader CreateReader(RVA rva);

		/// <summary>
		/// Creates a <see cref="DataReader"/>
		/// </summary>
		/// <param name="rva">RVA of data</param>
		/// <param name="length">Length of data</param>
		/// <returns></returns>
		DataReader CreateReader(RVA rva, uint length);

		/// <summary>
		/// Creates a <see cref="DataReader"/> that can read the whole image
		/// </summary>
		/// <returns></returns>
		DataReader CreateReader();
	}

	/// <summary>
	/// Interface to access a PE image
	/// </summary>
	public interface IInternalPEImage : IPEImage {
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
		/// Finds a <see cref="ResourceData"/>
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="type">Type</param>
		/// <param name="name">Name</param>
		/// <param name="langId">Language ID</param>
		/// <returns>The <see cref="ResourceData"/> or <c>null</c> if none found</returns>
		public static ResourceData FindWin32ResourceData(this IPEImage self, ResourceName type, ResourceName name, ResourceName langId) =>
			self.Win32Resources?.Find(type, name, langId);
	}
}
