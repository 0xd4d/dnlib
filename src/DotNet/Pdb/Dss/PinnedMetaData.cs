// dnlib: See LICENSE.txt for more info

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
		public IntPtr Address => address;

		/// <summary>
		/// Gets the size
		/// </summary>
		public int Size => (int)stream.Length;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="stream">Metadata stream</param>
		public PinnedMetaData(IImageStream stream) {
			this.stream = stream;

			if (stream is UnmanagedMemoryImageStream umStream) {
				address = umStream.StartAddress;
				GC.SuppressFinalize(this);	// no GCHandle so finalizer isn't needed
			}
			else {
				if (stream is MemoryImageStream memStream) {
					streamData = memStream.DataArray;
					gcHandle = GCHandle.Alloc(streamData, GCHandleType.Pinned);
					address = new IntPtr(gcHandle.AddrOfPinnedObject().ToInt64() + memStream.DataOffset);
				}
				else {
					streamData = stream.ReadAllBytes();
					gcHandle = GCHandle.Alloc(streamData, GCHandleType.Pinned);
					address = gcHandle.AddrOfPinnedObject();
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
