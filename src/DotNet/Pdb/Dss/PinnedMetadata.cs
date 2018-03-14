// dnlib: See LICENSE.txt for more info

using System;
using System.Runtime.InteropServices;
using dnlib.DotNet.MD;
using dnlib.IO;

namespace dnlib.DotNet.Pdb.Dss {
	/// <summary>
	/// Pins a metadata stream in memory
	/// </summary>
	sealed class PinnedMetadata : IDisposable {
		GCHandle gcHandle;
		readonly byte[] streamData;
		readonly IntPtr address;

		/// <summary>
		/// Gets the address
		/// </summary>
		public IntPtr Address => address;

		/// <summary>
		/// Gets the size
		/// </summary>
		public int Size { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="metadata">.NET metadata</param>
		public unsafe PinnedMetadata(Metadata metadata) {
			var peImage = metadata.PEImage;
			var mdDataDir = metadata.ImageCor20Header.Metadata;
			var mdReader = peImage.CreateReader(mdDataDir.VirtualAddress, mdDataDir.Size);
			Size = (int)mdReader.Length;

			var realDataReaderFactory = peImage.DataReaderFactory;
			if (realDataReaderFactory is NativeMemoryDataReaderFactory nativeMemFactory) {
				address = (IntPtr)((byte*)nativeMemFactory.GetUnsafeUseAddress() + mdReader.StartOffset);
				GC.SuppressFinalize(this);	// no GCHandle so finalizer isn't needed
			}
			else if (realDataReaderFactory is MemoryMappedDataReaderFactory mmapMemFactory) {
				address = (IntPtr)((byte*)mmapMemFactory.GetUnsafeUseAddress() + mdReader.StartOffset);
				GC.SuppressFinalize(this);	// no GCHandle so finalizer isn't needed
			}
			else if (realDataReaderFactory is ByteArrayDataReaderFactory memFactory) {
				streamData = memFactory.DataArray;
				gcHandle = GCHandle.Alloc(streamData, GCHandleType.Pinned);
				address = new IntPtr(gcHandle.AddrOfPinnedObject().ToInt64() + memFactory.DataOffset + mdReader.StartOffset);
			}
			else {
				streamData = mdReader.ToArray();
				gcHandle = GCHandle.Alloc(streamData, GCHandleType.Pinned);
				address = gcHandle.AddrOfPinnedObject();
			}
		}

		~PinnedMetadata() => Dispose(false);

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
		}
	}
}
