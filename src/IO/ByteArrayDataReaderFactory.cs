// dnlib: See LICENSE.txt for more info

using System;

namespace dnlib.IO {
	/// <summary>
	/// A <see cref="DataReaderFactory"/> that reads from a byte array
	/// </summary>
	public sealed class ByteArrayDataReaderFactory : DataReaderFactory {
		/// <summary>
		/// The filename or null if the data is not from a file
		/// </summary>
		public override string Filename => filename;

		/// <summary>
		/// Gets the total length of the data
		/// </summary>
		public override uint Length => length;

		internal byte[] DataArray => data;
		internal uint DataOffset => 0;

		DataStream stream;
		string filename;
		uint length;
		byte[] data;

		ByteArrayDataReaderFactory(byte[] data, string filename) {
			this.filename = filename;
			length = (uint)data.Length;
			stream = DataStreamFactory.Create(data);
			this.data = data;
		}

		/// <summary>
		/// Creates a <see cref="ByteArrayDataReaderFactory"/> instance
		/// </summary>
		/// <param name="data">Data</param>
		/// <param name="filename">The filename or null if the data is not from a file</param>
		/// <returns></returns>
		public static ByteArrayDataReaderFactory Create(byte[] data, string filename) {
			if (data is null)
				throw new ArgumentNullException(nameof(data));
			return new ByteArrayDataReaderFactory(data, filename);
		}

		/// <summary>
		/// Creates a data reader
		/// </summary>
		/// <param name="data">Data</param>
		/// <returns></returns>
		public static DataReader CreateReader(byte[] data) => Create(data, filename: null).CreateReader();

		/// <summary>
		/// Creates a data reader
		/// </summary>
		/// <param name="offset">Offset of data</param>
		/// <param name="length">Length of data</param>
		/// <returns></returns>
		public override DataReader CreateReader(uint offset, uint length) => CreateReader(stream, offset, length);

		/// <summary>
		/// This method doesn't need to be called since a <see cref="ByteArrayDataReaderFactory"/> has nothing that must be cleaned up
		/// </summary>
		public override void Dispose() {
			stream = EmptyDataStream.Instance;
			length = 0;
			filename = null;
			data = null;
		}
	}
}
