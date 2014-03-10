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
