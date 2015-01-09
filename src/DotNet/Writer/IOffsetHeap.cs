// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Interface to get and set raw heap data. Implemented by the offset heaps: #Strings,
	/// #GUID, #Blob, and #US.
	/// </summary>
	/// <typeparam name="TValue">Type of cooked data</typeparam>
	public interface IOffsetHeap<TValue> {
		/// <summary>
		/// Gets the size of the data as raw data when written to the heap
		/// </summary>
		/// <param name="data">The data</param>
		/// <returns>Size of the data as raw data when written to the heap</returns>
		int GetRawDataSize(TValue data);

		/// <summary>
		/// Overrides what value should be written to the heap.
		/// </summary>
		/// <param name="offset">Offset of value. Must match an offset returned by
		/// <see cref="GetAllRawData()"/></param>
		/// <param name="rawData">The new raw data. The size must match the raw size exactly.</param>
		void SetRawData(uint offset, byte[] rawData);

		/// <summary>
		/// Gets all inserted raw data and their offsets. The returned <see cref="byte"/> array
		/// is owned by the caller.
		/// </summary>
		/// <returns>An enumerable of all raw data and their offsets</returns>
		IEnumerable<KeyValuePair<uint, byte[]>> GetAllRawData();
	}
}
