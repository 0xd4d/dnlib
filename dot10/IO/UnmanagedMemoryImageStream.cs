using System;
using System.IO;
using System.Runtime.InteropServices;

namespace dot10.IO {
	/// <summary>
	/// IImageStream for unmanaged memory
	/// </summary>
	public unsafe class UnmanagedMemoryImageStream : IImageStream {
		FileOffset fileOffset;
		byte* startAddr;
		byte* endAddr;
		byte* currentAddr;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="fileOffset">File offset of data</param>
		/// <param name="addr">Address of data</param>
		/// <param name="length">Length of data</param>
		public unsafe UnmanagedMemoryImageStream(FileOffset fileOffset, IntPtr addr, long length)
			: this(fileOffset, (byte*)addr, length) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="fileOffset">File offset of data</param>
		/// <param name="baseAddr">Address of data</param>
		/// <param name="length">Length of data</param>
		public unsafe UnmanagedMemoryImageStream(FileOffset fileOffset, byte* baseAddr, long length) {
			this.fileOffset = fileOffset;
			this.startAddr = baseAddr;
			this.endAddr = baseAddr + length;
			this.currentAddr = this.startAddr;
		}

		/// <inheritdoc/>
		public FileOffset FileOffset {
			get { return fileOffset; }
		}

		/// <inheritdoc/>
		public long Length {
			get { unsafe { return endAddr - startAddr; } }
		}

		/// <inheritdoc/>
		public long Position {
			get { unsafe { return currentAddr - startAddr; } }
			set {
				unsafe {
					byte* newAddr = startAddr + value;
					if (newAddr < startAddr || newAddr > endAddr)
						throw new IOException("Invalid position");
					currentAddr = newAddr;
				}
			}
		}

		/// <inheritdoc/>
		public unsafe byte[] ReadBytes(int size) {
			if (currentAddr + size < currentAddr || currentAddr + size > endAddr)
				throw new IOException("Trying to read too much");
			var newData = new byte[size];
			Marshal.Copy(new IntPtr(currentAddr), newData, 0, size);
			currentAddr += size;
			return newData;
		}

		/// <inheritdoc/>
		public unsafe byte ReadByte() {
			if (currentAddr >= endAddr)
				throw new IOException("Can't read one byte");
			return *currentAddr++;
		}

		/// <inheritdoc/>
		public unsafe short ReadInt16() {
			if (currentAddr + 1 >= endAddr)
				throw new IOException("Can't read one Int16");
			short val = *(short*)currentAddr;
			currentAddr += 2;
			return val;
		}

		/// <inheritdoc/>
		public unsafe int ReadInt32() {
			if (currentAddr + 3 >= endAddr)
				throw new IOException("Can't read one Int32");
			int val = *(int*)currentAddr;
			currentAddr += 4;
			return val;
		}

		/// <inheritdoc/>
		public unsafe long ReadInt64() {
			if (currentAddr + 7 >= endAddr)
				throw new IOException("Can't read one Int64");
			long val = *(long*)currentAddr;
			currentAddr += 8;
			return val;
		}

		/// <inheritdoc/>
		public void Dispose() {
		}

		/// <inheritdoc/>
		public override string ToString() {
			return string.Format("FO:{0:X8} S:{1:X8} A:{2:X8}", fileOffset.Value, Length, new IntPtr(startAddr));
		}
	}
}
