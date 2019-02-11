// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;

namespace dnlib.IO {
	/// <summary>
	/// Creates <see cref="DataReader"/>s that can read its data.
	/// 
	/// This factory class is thread safe and its data can be read by <see cref="DataReader"/> on any thread.
	/// </summary>
	public abstract class DataReaderFactory : IDisposable {
		/// <summary>
		/// The filename or null if the data is not from a file
		/// </summary>
		public abstract string Filename { get; }

		/// <summary>
		/// Gets the total length of the data
		/// </summary>
		public abstract uint Length { get; }

		/// <summary>
		/// Creates a data reader that can read all data
		/// </summary>
		/// <returns></returns>
		public DataReader CreateReader() => CreateReader(0U, Length);

		/// <summary>
		/// Creates a data reader
		/// </summary>
		/// <param name="offset">Offset of data</param>
		/// <param name="length">Length of data</param>
		/// <returns></returns>
		public abstract DataReader CreateReader(uint offset, uint length);

		static void ThrowArgumentOutOfRangeException(string paramName) =>
			throw new ArgumentOutOfRangeException(paramName);

		static void Throw_CreateReader_2(int offset, int length) {
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset));
			Debug.Assert(length < 0);
			throw new ArgumentOutOfRangeException(nameof(length));
		}

		/// <summary>
		/// Creates a data reader
		/// </summary>
		/// <param name="offset">Offset of data</param>
		/// <param name="length">Length of data</param>
		/// <returns></returns>
		public DataReader CreateReader(uint offset, int length) {
			if (length < 0)
				ThrowArgumentOutOfRangeException(nameof(length));
			return CreateReader(offset, (uint)length);
		}

		/// <summary>
		/// Creates a data reader
		/// </summary>
		/// <param name="offset">Offset of data</param>
		/// <param name="length">Length of data</param>
		/// <returns></returns>
		public DataReader CreateReader(int offset, uint length) {
			if (offset < 0)
				ThrowArgumentOutOfRangeException(nameof(offset));
			return CreateReader((uint)offset, length);
		}

		/// <summary>
		/// Creates a data reader
		/// </summary>
		/// <param name="offset">Offset of data</param>
		/// <param name="length">Length of data</param>
		/// <returns></returns>
		public DataReader CreateReader(int offset, int length) {
			if (offset < 0 || length < 0)
				Throw_CreateReader_2(offset, length);
			return CreateReader((uint)offset, (uint)length);
		}

		/// <summary>
		/// Creates a data reader
		/// </summary>
		/// <param name="stream">Stream</param>
		/// <param name="offset">Offset of data</param>
		/// <param name="length">Length of data</param>
		/// <returns></returns>
		protected DataReader CreateReader(DataStream stream, uint offset, uint length) {
			uint maxOffset = Length;
			if (offset > maxOffset)
				offset = maxOffset;
			if ((ulong)offset + length > maxOffset)
				length = maxOffset - offset;
			return new DataReader(stream, offset, length);
		}

		/// <summary>
		/// Raised when all cached <see cref="DataReader"/>s created by this instance must be recreated
		/// </summary>
		public virtual event EventHandler DataReaderInvalidated { add { } remove { } }

		/// <summary>
		/// Disposes of this instance
		/// </summary>
		public abstract void Dispose();
	}
}
