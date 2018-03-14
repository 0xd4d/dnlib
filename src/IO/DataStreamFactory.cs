// dnlib: See LICENSE.txt for more info

using System;

namespace dnlib.IO {
	/// <summary>
	/// Creates <see cref="DataStream"/>s
	/// </summary>
	public static unsafe class DataStreamFactory {
		static bool supportsUnalignedAccesses = CalculateSupportsUnalignedAccesses();

		//TODO: ARM doesn't support unaligned accesses
		static bool CalculateSupportsUnalignedAccesses() => true;

		/// <summary>
		/// Creates a <see cref="DataStream"/> that reads from native memory
		/// </summary>
		/// <param name="data">Pointer to data</param>
		/// <returns></returns>
		public static DataStream Create(byte* data) {
			if (data == null)
				throw new ArgumentNullException(nameof(data));
			if (supportsUnalignedAccesses)
				return new UnalignedNativeMemoryDataStream(data);
			return new AlignedNativeMemoryDataStream(data);
		}

		/// <summary>
		/// Creates a <see cref="DataStream"/> that reads from a byte array
		/// </summary>
		/// <param name="data">Data</param>
		/// <returns></returns>
		public static DataStream Create(byte[] data) {
			if (data == null)
				throw new ArgumentNullException(nameof(data));
			if (supportsUnalignedAccesses)
				return new UnalignedByteArrayDataStream(data);
			return new AlignedByteArrayDataStream(data);
		}
	}
}
