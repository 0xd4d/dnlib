/*
    Copyright (C) 2012-2014 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

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
