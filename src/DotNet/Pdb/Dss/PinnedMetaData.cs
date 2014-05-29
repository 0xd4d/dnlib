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
using System.Runtime.InteropServices;
using dnlib.IO;

namespace dnlib.DotNet.Pdb.Dss {
	/// <summary>
	/// Pins a metadata stream in memory
	/// </summary>
	sealed class PinnedMetaData : IDisposable {
		GCHandle gcHandle;
		readonly IImageStream stream;
		readonly byte[] streamData;
		readonly IntPtr address;

		/// <summary>
		/// Gets the address
		/// </summary>
		public IntPtr Address {
			get { return address; }
		}

		/// <summary>
		/// Gets the size
		/// </summary>
		public int Size {
			get { return (int)stream.Length; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="stream">Metadata stream</param>
		public PinnedMetaData(IImageStream stream) {
			this.stream = stream;

			var umStream = stream as UnmanagedMemoryImageStream;
			if (umStream != null) {
				this.address = umStream.StartAddress;
				GC.SuppressFinalize(this);	// no GCHandle so finalizer isn't needed
			}
			else {
				var memStream = stream as MemoryImageStream;
				if (memStream != null) {
					this.streamData = memStream.DataArray;
					this.gcHandle = GCHandle.Alloc(this.streamData, GCHandleType.Pinned);
					this.address = new IntPtr(this.gcHandle.AddrOfPinnedObject().ToInt64() + memStream.DataOffset);
				}
				else {
					this.streamData = stream.ReadAllBytes();
					this.gcHandle = GCHandle.Alloc(this.streamData, GCHandleType.Pinned);
					this.address = this.gcHandle.AddrOfPinnedObject();
				}
			}
		}

		~PinnedMetaData() {
			Dispose(false);
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool disposing) {
			if (gcHandle.IsAllocated) {
				try {
					gcHandle.Free();
				}
				catch (InvalidOperationException) {
				}
			}
			if (disposing)
				stream.Dispose();
		}
	}
}
