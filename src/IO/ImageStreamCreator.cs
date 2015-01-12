// dnlib: See LICENSE.txt for more info

using System;
using System.IO;

namespace dnlib.IO {
	/// <summary>
	/// Creates a <see cref="IImageStreamCreator"/> instance
	/// </summary>
	public static class ImageStreamCreator {
		static readonly bool doesNotSupportMmapFileMethods;

		static ImageStreamCreator() {
			// See http://mono-project.com/FAQ:_Technical#Mono_Platforms for platform detection.
			int p = (int)Environment.OSVersion.Platform;
			if (p == 4 || p == 6 || p == 128)
				doesNotSupportMmapFileMethods = true;	// unix OS
		}

		/// <summary>
		/// Creates a <see cref="IImageStreamCreator"/>. It will be a
		/// <see cref="MemoryMappedFileStreamCreator"/> if the operating system supports the memory
		/// mapped file methods we use, else <see cref="MemoryStreamCreator"/>.
		/// </summary>
		/// <param name="fileName">Filename</param>
		/// <returns>A new <see cref="ImageStreamCreator"/> instance</returns>
		public static IImageStreamCreator Create(string fileName) {
			return Create(fileName, false);
		}

		/// <summary>
		/// Creates a <see cref="IImageStreamCreator"/>. It will be a
		/// <see cref="MemoryMappedFileStreamCreator"/> if the operating system supports the memory
		/// mapped file methods we use, else <see cref="MemoryStreamCreator"/>.
		/// </summary>
		/// <param name="fileName">Filename</param>
		/// <param name="mapAsImage"><c>true</c> if we should map it as an executable</param>
		/// <returns>A new <see cref="ImageStreamCreator"/> instance</returns>
		public static IImageStreamCreator Create(string fileName, bool mapAsImage) {
			if (doesNotSupportMmapFileMethods)
				return new MemoryStreamCreator(File.ReadAllBytes(fileName)) { FileName = fileName };
			else
				return new MemoryMappedFileStreamCreator(fileName, mapAsImage);
		}

		/// <summary>
		/// Creates a <see cref="IImageStream"/>
		/// </summary>
		/// <param name="fileName">Filename</param>
		/// <returns>A new <see cref="IImageStream"/> instance</returns>
		public static IImageStream CreateImageStream(string fileName) {
			return CreateImageStream(fileName, false);
		}

		/// <summary>
		/// Creates a <see cref="IImageStream"/>
		/// </summary>
		/// <param name="fileName">Filename</param>
		/// <param name="mapAsImage"><c>true</c> if we should map it as an executable</param>
		/// <returns>A new <see cref="IImageStream"/> instance</returns>
		public static IImageStream CreateImageStream(string fileName, bool mapAsImage) {
			if (doesNotSupportMmapFileMethods)
				return MemoryImageStream.Create(File.ReadAllBytes(fileName));

			var creator = new MemoryMappedFileStreamCreator(fileName, mapAsImage);
			try {
				return new UnmanagedMemoryImageStream(creator);
			}
			catch {
				if (creator != null)
					creator.Dispose();
				throw;
			}
		}
	}
}
