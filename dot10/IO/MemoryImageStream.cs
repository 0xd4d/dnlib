using System;
using System.IO;

namespace dot10.IO {
	/// <summary>
	/// IImageStream for byte[]
	/// </summary>
	public class MemoryImageStream : IImageStream {
		FileOffset fileOffset;
		byte[] data;
		int dataOffset;
		int dataEnd;
		int position;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="fileOffset">File offset of data</param>
		/// <param name="data">The data</param>
		/// <param name="dataOffset">Start offset in <paramref name="data"/></param>
		/// <param name="dataLength">Length of data</param>
		public MemoryImageStream(FileOffset fileOffset, byte[] data, int dataOffset, int dataLength) {
			this.fileOffset = fileOffset;
			this.data = data;
			this.dataOffset = dataOffset;
			this.dataEnd = dataOffset + dataLength;
			this.position = dataOffset;
		}

		/// <inheritdoc/>
		public FileOffset FileOffset {
			get { return fileOffset; }
		}

		/// <inheritdoc/>
		public long Length {
			get { return dataEnd - dataOffset; }
		}

		/// <inheritdoc/>
		public long Position {
			get { return position - dataOffset; }
			set {
				long newPos = dataOffset + value;
				if (newPos < dataOffset || newPos > dataEnd)
					throw new IOException("Invalid position");
				position = (int)newPos;
			}
		}

		/// <inheritdoc/>
		public byte[] ReadBytes(int size) {
			if (position + size < position || position + size > dataEnd)
				throw new IOException("Trying to read too much");
			var newData = new byte[size];
			Array.Copy(data, position, newData, 0, size);
			position += size;
			return newData;
		}

		/// <inheritdoc/>
		public sbyte ReadSByte() {
			if (position >= dataEnd)
				throw new IOException("Can't read one SByte");
			return (sbyte)data[position++];
		}

		/// <inheritdoc/>
		public byte ReadByte() {
			if (position >= dataEnd)
				throw new IOException("Can't read one Byte");
			return data[position++];
		}

		/// <inheritdoc/>
		public short ReadInt16() {
			if (position + 1 >= dataEnd)
				throw new IOException("Can't read one Int16");
			return (short)(data[position++] | (data[position++] << 8));
		}

		/// <inheritdoc/>
		public ushort ReadUInt16() {
			if (position + 1 >= dataEnd)
				throw new IOException("Can't read one UInt16");
			return (ushort)(data[position++] | (data[position++] << 8));
		}

		/// <inheritdoc/>
		public int ReadInt32() {
			if (position + 3 >= dataEnd)
				throw new IOException("Can't read one Int32");
			return data[position++] |
					(data[position++] << 8) |
					(data[position++] << 16) |
					(data[position++] << 24);
		}

		/// <inheritdoc/>
		public uint ReadUInt32() {
			if (position + 3 >= dataEnd)
				throw new IOException("Can't read one UInt32");
			return (uint)(data[position++] |
					(data[position++] << 8) |
					(data[position++] << 16) |
					(data[position++] << 24));
		}

		/// <inheritdoc/>
		public long ReadInt64() {
			if (position + 7 >= dataEnd)
				throw new IOException("Can't read one Int64");
			return (long)data[position++] |
					((long)data[position++] << 8) |
					((long)data[position++] << 16) |
					((long)data[position++] << 24) |
					((long)data[position++] << 32) |
					((long)data[position++] << 40) |
					((long)data[position++] << 48) |
					((long)data[position++] << 56);
		}

		/// <inheritdoc/>
		public ulong ReadUInt64() {
			if (position + 7 >= dataEnd)
				throw new IOException("Can't read one UInt64");
			return (ulong)data[position++] |
					((ulong)data[position++] << 8) |
					((ulong)data[position++] << 16) |
					((ulong)data[position++] << 24) |
					((ulong)data[position++] << 32) |
					((ulong)data[position++] << 40) |
					((ulong)data[position++] << 48) |
					((ulong)data[position++] << 56);
		}

		/// <inheritdoc/>
		public void Dispose() {
		}

		/// <inheritdoc/>
		public override string ToString() {
			return string.Format("FO:{0:X8} S:{1:X8} A:{2:X8}", fileOffset.Value, Length);
		}
	}
}
