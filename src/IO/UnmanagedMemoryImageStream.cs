using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace dot10.IO {
	/// <summary>
	/// IImageStream for unmanaged memory
	/// </summary>
	[DebuggerDisplay("FO:{fileOffset} S:{Length} A:{startAddr}")]
	sealed unsafe class UnmanagedMemoryImageStream : IImageStream {
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
					if (IntPtr.Size == 4 && (ulong)value > int.MaxValue)
						throw new ArgumentOutOfRangeException("value", "Can't seek to a negative offset or an offset > 0x7FFFFFFF");
					byte* newAddr = startAddr + value;
					if (newAddr < startAddr)
						throw new ArgumentOutOfRangeException("value", "Invalid offset");
					currentAddr = newAddr;
				}
			}
		}

		/// <inheritdoc/>
		public unsafe IImageStream Create(FileOffset offset, long length) {
			if ((long)offset < 0 || length < 0)
				return MemoryImageStream.CreateEmpty();

			long offs = Math.Min(Length, (long)offset);
			long len = Math.Min(Length - offs, length);
			return new UnmanagedMemoryImageStream((FileOffset)((long)fileOffset + (long)offset), startAddr + (long)offs, len);
		}

		/// <inheritdoc/>
		public unsafe byte[] ReadBytes(int size) {
			if (currentAddr + size < currentAddr || currentAddr + size > endAddr) {
				if (size == 0)
					return new byte[0];
				throw new IOException("Trying to read too much");
			}
			var newData = new byte[size];
			Marshal.Copy(new IntPtr(currentAddr), newData, 0, size);
			currentAddr += size;
			return newData;
		}

		/// <inheritdoc/>
		public byte[] ReadBytesUntilByte(byte b) {
			byte* pos = GetPositionOf(b);
			if (pos == null)
				return null;
			return ReadBytes((int)(pos - currentAddr));
		}

		unsafe byte* GetPositionOf(byte b) {
			byte* pos = currentAddr;
			while (pos < endAddr) {
				if (*pos == b)
					return pos;
				pos++;
			}
			return null;
		}

		/// <inheritdoc/>
		public unsafe sbyte ReadSByte() {
			if (currentAddr >= endAddr)
				throw new IOException("Can't read one SByte");
			return (sbyte)*currentAddr++;
		}

		/// <inheritdoc/>
		public unsafe byte ReadByte() {
			if (currentAddr >= endAddr)
				throw new IOException("Can't read one Byte");
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
		public unsafe ushort ReadUInt16() {
			if (currentAddr + 1 >= endAddr)
				throw new IOException("Can't read one UInt16");
			ushort val = *(ushort*)currentAddr;
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
		public unsafe uint ReadUInt32() {
			if (currentAddr + 3 >= endAddr)
				throw new IOException("Can't read one UInt32");
			uint val = *(uint*)currentAddr;
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
		public unsafe ulong ReadUInt64() {
			if (currentAddr + 7 >= endAddr)
				throw new IOException("Can't read one UInt64");
			ulong val = *(ulong*)currentAddr;
			currentAddr += 8;
			return val;
		}

		/// <inheritdoc/>
		public unsafe float ReadSingle() {
			if (currentAddr + 3 >= endAddr)
				throw new IOException("Can't read one Single");
			var val = *(float*)currentAddr;
			currentAddr += 4;
			return val;
		}

		/// <inheritdoc/>
		public unsafe double ReadDouble() {
			if (currentAddr + 7 >= endAddr)
				throw new IOException("Can't read one Double");
			var val = *(double*)currentAddr;
			currentAddr += 8;
			return val;
		}

		/// <inheritdoc/>
		public unsafe string ReadString(int chars) {
			if (IntPtr.Size == 4 && (uint)chars > (uint)int.MaxValue)
				throw new IOException("Not enough space to read the string");
			if (currentAddr + chars * 2 < currentAddr || (chars != 0 && currentAddr + chars * 2 - 1 >= endAddr))
				throw new IOException("Not enough space to read the string");
			var s = new string((char*)currentAddr, 0, chars);
			currentAddr += chars * 2;
			return s;
		}

		/// <inheritdoc/>
		public void Dispose() {
			fileOffset = 0;
			startAddr = null;
			endAddr = null;
			currentAddr = null;
		}
	}
}
