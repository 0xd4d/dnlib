using System;
using System.Diagnostics;
using System.IO;

namespace dot10.IO {
	/// <summary>
	/// IImageStream for byte[]
	/// </summary>
	[DebuggerDisplay("FO:{fileOffset} S:{Length}")]
	sealed class MemoryImageStream : IImageStream {
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
				if (newPos < dataOffset || newPos > int.MaxValue)
					throw new ArgumentOutOfRangeException("value", "Can't seek to a negative offset or an offset > 0x7FFFFFFF");
				position = (int)newPos;
			}
		}

		/// <summary>
		/// Creates an empty <see cref="MemoryImageStream"/> instance
		/// </summary>
		public static MemoryImageStream CreateEmpty() {
			return new MemoryImageStream(0, new byte[0], 0, 0);
		}

		/// <inheritdoc/>
		public IImageStream Create(FileOffset offset, long length) {
			if ((long)offset < 0 || length < 0)
				return MemoryImageStream.CreateEmpty();

			int offs = (int)Math.Min((long)Length, (long)offset);
			int len = (int)Math.Min((long)Length - offs, length);
			return new MemoryImageStream((FileOffset)((long)fileOffset + (long)offset), data, dataOffset + offs, len);
		}

		/// <inheritdoc/>
		public byte[] ReadBytes(int size) {
			if (position + size < position || position + size > dataEnd) {
				if (size == 0)
					return new byte[0];
				throw new IOException("Trying to read too much");
			}
			var newData = new byte[size];
			Array.Copy(data, position, newData, 0, size);
			position += size;
			return newData;
		}

		/// <inheritdoc/>
		public byte[] ReadBytesUntilByte(byte b) {
			int pos = GetPositionOf(b);
			if (pos < 0)
				return null;
			return ReadBytes(pos - position);
		}

		int GetPositionOf(byte b) {
			int pos = position;
			while (pos < dataEnd) {
				if (data[pos] == b)
					return pos;
				pos++;
			}
			return -1;
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
		public float ReadSingle() {
			if (position + 3 >= dataEnd)
				throw new IOException("Can't read one Single");
			var val = BitConverter.ToSingle(data, position);
			position += 4;
			return val;
		}

		/// <inheritdoc/>
		public double ReadDouble() {
			if (position + 7 >= dataEnd)
				throw new IOException("Can't read one Double");
			var val = BitConverter.ToDouble(data, position);
			position += 8;
			return val;
		}

		/// <inheritdoc/>
		public string ReadString(int chars) {
			if ((uint)chars > (uint)int.MaxValue)
				throw new IOException("Not enough space to read the string");
			if (position + chars * 2 < position || (chars != 0 && position + chars * 2 - 1 >= dataEnd))
				throw new IOException("Not enough space to read the string");
			var array = new char[chars];
			for (int i = 0; i < chars; i++)
				array[i] = (char)(data[position++] | (data[position++] << 8));
			return new string(array);
		}

		/// <inheritdoc/>
		public void Dispose() {
			fileOffset = 0;
			data = null;
			dataOffset = 0;
			dataEnd = 0;
			position = 0;
		}
	}
}
