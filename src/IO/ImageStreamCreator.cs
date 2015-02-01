// dnlib: See LICENSE.txt for more info

using System;
using System.IO;

namespace dnlib.IO {
	/// <summary>
	/// Creates a <see cref="IImageStreamCreator"/> instance
	/// </summary>
	public static class ImageStreamCreator {
		static readonly bool isUnix;

		static ImageStreamCreator() {
			// See http://mono-project.com/FAQ:_Technical#Mono_Platforms for platform detection.
			int p = (int)Environment.OSVersion.Platform;
			if (p == 4 || p == 6 || p == 128)
				isUnix = true;
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
		/// <param name="mapAsImage"><c>true</c> if we should map it as an executable. Not supported
		/// on Linux/Mac</param>
		/// <returns>A new <see cref="ImageStreamCreator"/> instance</returns>
		public static IImageStreamCreator Create(string fileName, bool mapAsImage) {
			var creator = CreateMemoryMappedFileStreamCreator(fileName, mapAsImage);
			if (creator != null)
				return creator;

			return new MemoryStreamCreator(File.ReadAllBytes(fileName)) { FileName = fileName };
		}

		static MemoryMappedFileStreamCreator CreateMemoryMappedFileStreamCreator(string fileName, bool mapAsImage) {
			if (!isUnix)
				return MemoryMappedFileStreamCreator.CreateWindows(fileName, mapAsImage);
			else
				return MemoryMappedFileStreamCreator.CreateUnix(fileName, mapAsImage);
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
		/// <param name="mapAsImage"><c>true</c> if we should map it as an executable. Not supported
		/// on Linux/Mac</param>
		/// <returns>A new <see cref="IImageStream"/> instance</returns>
		public static IImageStream CreateImageStream(string fileName, bool mapAsImage) {
			var creator = CreateMemoryMappedFileStreamCreator(fileName, mapAsImage);
			try {
				if (creator != null)
					return new UnmanagedMemoryImageStream(creator);
				return MemoryImageStream.Create(File.ReadAllBytes(fileName));
			}
			catch {
				if (creator != null)
					creator.Dispose();
				throw;
			}
		}
	}
}
