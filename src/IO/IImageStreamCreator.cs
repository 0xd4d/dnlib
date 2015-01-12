// dnlib: See LICENSE.txt for more info

﻿using System;

namespace dnlib.IO {
	/// <summary>
	/// Creates a new stream that accesses part of some data
	/// </summary>
	public interface IImageStreamCreator : IDisposable {
		/// <summary>
		/// The file name or <c>null</c> if data is not from a file
		/// </summary>
		string FileName { get; }

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
		IImageStream Create(FileOffset offset, long length);

		/// <summary>
		/// Creates a stream that can access all data
		/// </summary>
		/// <returns>A new stream</returns>
		IImageStream CreateFull();
	}

	static partial class IOExtensions {
		/// <summary>
		/// Creates a stream that can access all data starting from <paramref name="offset"/>
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="offset">Offset within the original data</param>
		/// <returns>A new stream</returns>
		public static IImageStream Create(this IImageStreamCreator self, FileOffset offset) {
			return self.Create(offset, long.MaxValue);
		}
	}
}
