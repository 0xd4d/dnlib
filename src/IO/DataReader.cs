// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using dnlib.DotNet.Writer;

namespace dnlib.IO {
	/// <summary>
	/// Thrown by a <see cref="DataReader"/> when it can't read data or if the caller tries to set an invalid offset
	/// </summary>
	[Serializable]
	public sealed class DataReaderException : IOException {
		internal DataReaderException(string message) : base(message) { }
		internal DataReaderException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}

	/// <summary>
	/// Reads data
	/// </summary>
	[DebuggerDisplay("{StartOffset,h}-{EndOffset,h} Length={Length} BytesLeft={BytesLeft}")]
	public struct DataReader {
		/// <summary>
		/// Gets the start offset of the data
		/// </summary>
		public readonly uint StartOffset => startOffset;

		/// <summary>
		/// Gets the end offset of the data, not inclusive
		/// </summary>
		public readonly uint EndOffset => endOffset;

		/// <summary>
		/// Gets the total length of the data
		/// </summary>
		public readonly uint Length => endOffset - startOffset;

		/// <summary>
		/// Gets the current offset. This is between <see cref="StartOffset"/> and <see cref="EndOffset"/> (inclusive)
		/// </summary>
		public uint CurrentOffset {
			readonly get => currentOffset;
			set {
				VerifyState();
				if (value < startOffset || value > endOffset) {
					// Invalid offsets should be an IOException and not an ArgumentException
					ThrowDataReaderException("Invalid new " + nameof(CurrentOffset));
				}
				currentOffset = value;
				VerifyState();
			}
		}

		/// <summary>
		/// Gets/sets the position relative to <see cref="StartOffset"/>
		/// </summary>
		public uint Position {
			readonly get => currentOffset - startOffset;
			set {
				VerifyState();
				if (value > Length) {
					// Invalid positions should be an IOException and not an ArgumentException
					ThrowDataReaderException("Invalid new " + nameof(Position));
				}
				currentOffset = startOffset + value;
				VerifyState();
			}
		}

		/// <summary>
		/// Gets the number of bytes that can be read without throwing an exception
		/// </summary>
		public readonly uint BytesLeft => endOffset - currentOffset;

		readonly DataStream stream;
		readonly uint startOffset;
		readonly uint endOffset;
		uint currentOffset;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="stream">Stream</param>
		/// <param name="offset">Start offset of data</param>
		/// <param name="length">Length of data</param>
		public DataReader(DataStream stream, uint offset, uint length) {
			Debug.Assert(!(stream is null) || (offset == 0 && length == 0));
			Debug.Assert(offset + length >= offset);
			this.stream = stream;
			startOffset = offset;
			endOffset = offset + length;
			currentOffset = offset;
			VerifyState();
		}

		[Conditional("DEBUG")]
		readonly void VerifyState() {
			Debug.Assert(startOffset <= currentOffset);
			Debug.Assert(currentOffset <= endOffset);
		}

		static void ThrowNoMoreBytesLeft() => throw new DataReaderException("There's not enough bytes left to read");
		static void ThrowDataReaderException(string message) => throw new DataReaderException(message);
		static void ThrowInvalidOperationException() => throw new InvalidOperationException();
		static void ThrowArgumentNullException(string paramName) => throw new ArgumentNullException(paramName);
		static void ThrowInvalidArgument(string paramName) => throw new DataReaderException("Invalid argument value");

		/// <summary>
		/// Resets the reader so it points to the start of the data
		/// </summary>
		public void Reset() => currentOffset = startOffset;

		/// <summary>
		/// Creates a new reader that can access a smaller part of this reader
		/// </summary>
		/// <param name="start">Start position relative to <see cref="StartOffset"/></param>
		/// <param name="length">Length of data</param>
		/// <returns></returns>
		public readonly DataReader Slice(uint start, uint length) {
			if ((ulong)start + length > Length)
				ThrowInvalidArgument(nameof(length));
			return new DataReader(stream, startOffset + start, length);
		}

		/// <summary>
		/// Creates a new reader that can access everything from <paramref name="start"/> to the end of the data
		/// </summary>
		/// <param name="start">Start position relative to <see cref="StartOffset"/></param>
		/// <returns></returns>
		public readonly DataReader Slice(uint start) {
			if (start > Length)
				ThrowInvalidArgument(nameof(start));
			return Slice(start, Length - start);
		}

		/// <summary>
		/// Creates a new reader that can access a smaller part of this reader
		/// </summary>
		/// <param name="start">Start position relative to <see cref="StartOffset"/></param>
		/// <param name="length">Length of data</param>
		/// <returns></returns>
		public readonly DataReader Slice(int start, int length) {
			if (start < 0)
				ThrowInvalidArgument(nameof(start));
			if (length < 0)
				ThrowInvalidArgument(nameof(length));
			return Slice((uint)start, (uint)length);
		}

		/// <summary>
		/// Creates a new reader that can access everything from <paramref name="start"/> to the end of the data
		/// </summary>
		/// <param name="start">Start position relative to <see cref="StartOffset"/></param>
		/// <returns></returns>
		public readonly DataReader Slice(int start) {
			if (start < 0)
				ThrowInvalidArgument(nameof(start));
			if ((uint)start > Length)
				ThrowInvalidArgument(nameof(start));
			return Slice((uint)start, Length - (uint)start);
		}

		/// <summary>
		/// Checks if it's possible to read <paramref name="length"/> bytes
		/// </summary>
		/// <param name="length">Length of data</param>
		/// <returns></returns>
		public readonly bool CanRead(int length) => length >= 0 && (uint)length <= BytesLeft;

		/// <summary>
		/// Checks if it's possible to read <paramref name="length"/> bytes
		/// </summary>
		/// <param name="length">Length of data</param>
		/// <returns></returns>
		public readonly bool CanRead(uint length) => length <= BytesLeft;

		/// <summary>
		/// Reads a <see cref="bool"/>
		/// </summary>
		/// <returns></returns>
		public bool ReadBoolean() => ReadByte() != 0;

		/// <summary>
		/// Reads a <see cref="char"/>
		/// </summary>
		/// <returns></returns>
		public char ReadChar() => (char)ReadUInt16();

		/// <summary>
		/// Reads a <see cref="sbyte"/>
		/// </summary>
		/// <returns></returns>
		public sbyte ReadSByte() => (sbyte)ReadByte();

		/// <summary>
		/// Reads a <see cref="byte"/>
		/// </summary>
		/// <returns></returns>
		public byte ReadByte() {
			VerifyState();
			const uint SIZE = 1;
			var currentOffset = this.currentOffset;
			if (currentOffset == endOffset)
				ThrowNoMoreBytesLeft();
			var value = stream.ReadByte(currentOffset);
			this.currentOffset = currentOffset + SIZE;
			VerifyState();
			return value;
		}

		/// <summary>
		/// Reads a <see cref="short"/>
		/// </summary>
		/// <returns></returns>
		public short ReadInt16() => (short)ReadUInt16();

		/// <summary>
		/// Reads a <see cref="ushort"/>
		/// </summary>
		/// <returns></returns>
		public ushort ReadUInt16() {
			VerifyState();
			const uint SIZE = 2;
			var currentOffset = this.currentOffset;
			if (endOffset - currentOffset < SIZE)
				ThrowNoMoreBytesLeft();
			var value = stream.ReadUInt16(currentOffset);
			this.currentOffset = currentOffset + SIZE;
			VerifyState();
			return value;
		}

		/// <summary>
		/// Reads a <see cref="int"/>
		/// </summary>
		/// <returns></returns>
		public int ReadInt32() => (int)ReadUInt32();

		/// <summary>
		/// Reads a <see cref="uint"/>
		/// </summary>
		/// <returns></returns>
		public uint ReadUInt32() {
			VerifyState();
			const uint SIZE = 4;
			var currentOffset = this.currentOffset;
			if (endOffset - currentOffset < SIZE)
				ThrowNoMoreBytesLeft();
			var value = stream.ReadUInt32(currentOffset);
			this.currentOffset = currentOffset + SIZE;
			VerifyState();
			return value;
		}

		internal byte Unsafe_ReadByte() {
			VerifyState();
			const uint SIZE = 1;
			var currentOffset = this.currentOffset;
			Debug.Assert(currentOffset != endOffset);
			var value = stream.ReadByte(currentOffset);
			this.currentOffset = currentOffset + SIZE;
			VerifyState();
			return value;
		}

		internal ushort Unsafe_ReadUInt16() {
			VerifyState();
			const uint SIZE = 2;
			var currentOffset = this.currentOffset;
			Debug.Assert(endOffset - currentOffset >= SIZE);
			var value = stream.ReadUInt16(currentOffset);
			this.currentOffset = currentOffset + SIZE;
			VerifyState();
			return value;
		}

		internal uint Unsafe_ReadUInt32() {
			VerifyState();
			const uint SIZE = 4;
			var currentOffset = this.currentOffset;
			Debug.Assert(endOffset - currentOffset >= SIZE);
			var value = stream.ReadUInt32(currentOffset);
			this.currentOffset = currentOffset + SIZE;
			VerifyState();
			return value;
		}

		/// <summary>
		/// Reads a <see cref="long"/>
		/// </summary>
		/// <returns></returns>
		public long ReadInt64() => (long)ReadUInt64();

		/// <summary>
		/// Reads a <see cref="ulong"/>
		/// </summary>
		/// <returns></returns>
		public ulong ReadUInt64() {
			VerifyState();
			const uint SIZE = 8;
			var currentOffset = this.currentOffset;
			if (endOffset - currentOffset < SIZE)
				ThrowNoMoreBytesLeft();
			var value = stream.ReadUInt64(currentOffset);
			this.currentOffset = currentOffset + SIZE;
			VerifyState();
			return value;
		}

		/// <summary>
		/// Reads a <see cref="float"/>
		/// </summary>
		/// <returns></returns>
		public float ReadSingle() {
			VerifyState();
			const uint SIZE = 4;
			var currentOffset = this.currentOffset;
			if (endOffset - currentOffset < SIZE)
				ThrowNoMoreBytesLeft();
			var value = stream.ReadSingle(currentOffset);
			this.currentOffset = currentOffset + SIZE;
			VerifyState();
			return value;
		}

		/// <summary>
		/// Reads a <see cref="double"/>
		/// </summary>
		/// <returns></returns>
		public double ReadDouble() {
			VerifyState();
			const uint SIZE = 8;
			var currentOffset = this.currentOffset;
			if (endOffset - currentOffset < SIZE)
				ThrowNoMoreBytesLeft();
			var value = stream.ReadDouble(currentOffset);
			this.currentOffset = currentOffset + SIZE;
			VerifyState();
			return value;
		}

		/// <summary>
		/// Reads a <see cref="Guid"/>
		/// </summary>
		/// <returns></returns>
		public Guid ReadGuid() {
			VerifyState();
			const uint SIZE = 16;
			var currentOffset = this.currentOffset;
			if (endOffset - currentOffset < SIZE)
				ThrowNoMoreBytesLeft();
			var value = stream.ReadGuid(currentOffset);
			this.currentOffset = currentOffset + SIZE;
			VerifyState();
			return value;
		}

		/// <summary>
		/// Reads a <see cref="decimal"/>
		/// </summary>
		/// <returns></returns>
		public decimal ReadDecimal() {
			var bits = new int[4] {
				ReadInt32(),	// lo
				ReadInt32(),	// mid
				ReadInt32(),	// hi
				ReadInt32(),	// flags
			};
			return new decimal(bits);
		}

		/// <summary>
		/// Reads a UTF-16 encoded <see cref="string"/>
		/// </summary>
		/// <param name="chars">Number of characters to read</param>
		/// <returns></returns>
		public string ReadUtf16String(int chars) {
			if (chars < 0)
				ThrowInvalidArgument(nameof(chars));
			if (chars == 0)
				return string.Empty;
			VerifyState();
			uint length = (uint)chars * 2;
			var currentOffset = this.currentOffset;
			if (endOffset - currentOffset < length)
				ThrowNoMoreBytesLeft();
			var s = length == 0 ? string.Empty : stream.ReadUtf16String(currentOffset, chars);
			this.currentOffset = currentOffset + length;
			VerifyState();
			return s;
		}

		/// <summary>
		/// Reads bytes
		/// </summary>
		/// <param name="destination">Destination pointer</param>
		/// <param name="length">Number of bytes to read</param>
		public unsafe void ReadBytes(void* destination, int length) {
			if (destination is null && length != 0)
				ThrowArgumentNullException(nameof(destination));
			if (length < 0)
				ThrowInvalidArgument(nameof(length));
			// This is also true if 'this' is the 'default' instance ('stream' is null)
			if (length == 0)
				return;
			VerifyState();
			var currentOffset = this.currentOffset;
			if (endOffset - currentOffset < (uint)length)
				ThrowNoMoreBytesLeft();
			stream.ReadBytes(currentOffset, destination, length);
			this.currentOffset = currentOffset + (uint)length;
			VerifyState();
		}

		/// <summary>
		/// Reads bytes
		/// </summary>
		/// <param name="destination">Destination array</param>
		/// <param name="destinationIndex">Destination index</param>
		/// <param name="length">Number of bytes to read</param>
		public void ReadBytes(byte[] destination, int destinationIndex, int length) {
			if (destination is null)
				ThrowArgumentNullException(nameof(destination));
			if (destinationIndex < 0)
				ThrowInvalidArgument(nameof(destinationIndex));
			if (length < 0)
				ThrowInvalidArgument(nameof(length));
			// This is also true if 'this' is the 'default' instance ('stream' is null)
			if (length == 0)
				return;
			VerifyState();
			var currentOffset = this.currentOffset;
			if (endOffset - currentOffset < (uint)length)
				ThrowNoMoreBytesLeft();
			stream.ReadBytes(currentOffset, destination, destinationIndex, length);
			this.currentOffset = currentOffset + (uint)length;
			VerifyState();
		}

		/// <summary>
		/// Reads bytes
		/// </summary>
		/// <param name="length">Number of bytes to read</param>
		/// <returns></returns>
		public byte[] ReadBytes(int length) {
			if (length < 0)
				ThrowInvalidArgument(nameof(length));
			if (length == 0)
				return Array2.Empty<byte>();
			var data = new byte[length];
			ReadBytes(data, 0, length);
			return data;
		}

		/// <summary>
		/// Reads a compressed <see cref="uint"/>
		/// </summary>
		/// <param name="value">Uncompressed <see cref="uint"/></param>
		/// <returns></returns>
		public bool TryReadCompressedUInt32(out uint value) {
			VerifyState();
			var currentOffset = this.currentOffset;
			var bytesLeft = endOffset - currentOffset;
			if (bytesLeft == 0) {
				value = 0;
				VerifyState();
				return false;
			}

			var stream = this.stream;
			byte b = stream.ReadByte(currentOffset++);
			if ((b & 0x80) == 0) {
				value = b;
				this.currentOffset = currentOffset;
				VerifyState();
				return true;
			}

			if ((b & 0xC0) == 0x80) {
				if (bytesLeft < 2) {
					value = 0;
					VerifyState();
					return false;
				}
				value = (uint)(((b & 0x3F) << 8) | stream.ReadByte(currentOffset++));
				this.currentOffset = currentOffset;
				VerifyState();
				return true;
			}

			// The encoding 111x isn't allowed but the CLR sometimes doesn't verify this
			// and just assumes it's 110x. Don't fail if it's 111x, just assume it's 110x.

			if (bytesLeft < 4) {
				value = 0;
				VerifyState();
				return false;
			}
			value = (uint)(((b & 0x1F) << 24) | (stream.ReadByte(currentOffset++) << 16) |
					(stream.ReadByte(currentOffset++) << 8) | stream.ReadByte(currentOffset++));
			this.currentOffset = currentOffset;
			VerifyState();
			return true;
		}

		/// <summary>
		/// Reads a compressed <see cref="uint"/>
		/// </summary>
		/// <returns></returns>
		public uint ReadCompressedUInt32() {
			if (!TryReadCompressedUInt32(out uint value))
				ThrowNoMoreBytesLeft();
			return value;
		}

		/// <summary>
		/// Reads a compressed <see cref="int"/>
		/// </summary>
		/// <param name="value">Uncompressed <see cref="int"/></param>
		/// <returns></returns>
		public bool TryReadCompressedInt32(out int value) {
			VerifyState();
			var currentOffset = this.currentOffset;
			var bytesLeft = endOffset - currentOffset;
			if (bytesLeft == 0) {
				value = 0;
				VerifyState();
				return false;
			}

			var stream = this.stream;
			byte b = stream.ReadByte(currentOffset++);
			if ((b & 0x80) == 0) {
				if ((b & 1) != 0)
					value = -0x40 | (b >> 1);
				else
					value = (b >> 1);
				this.currentOffset = currentOffset;
				VerifyState();
				return true;
			}

			if ((b & 0xC0) == 0x80) {
				if (bytesLeft < 2) {
					value = 0;
					VerifyState();
					return false;
				}
				uint tmp = (uint)(((b & 0x3F) << 8) | stream.ReadByte(currentOffset++));
				if ((tmp & 1) != 0)
					value = -0x2000 | (int)(tmp >> 1);
				else
					value = (int)(tmp >> 1);
				this.currentOffset = currentOffset;
				VerifyState();
				return true;
			}

			if ((b & 0xE0) == 0xC0) {
				if (bytesLeft < 4) {
					value = 0;
					VerifyState();
					return false;
				}
				uint tmp = (uint)(((b & 0x1F) << 24) | (stream.ReadByte(currentOffset++) << 16) |
						(stream.ReadByte(currentOffset++) << 8) | stream.ReadByte(currentOffset++));
				if ((tmp & 1) != 0)
					value = -0x10000000 | (int)(tmp >> 1);
				else
					value = (int)(tmp >> 1);
				this.currentOffset = currentOffset;
				VerifyState();
				return true;
			}

			value = 0;
			VerifyState();
			return false;
		}

		/// <summary>
		/// Reads a compressed <see cref="int"/>
		/// </summary>
		/// <returns></returns>
		public int ReadCompressedInt32() {
			if (!TryReadCompressedInt32(out int value))
				ThrowNoMoreBytesLeft();
			return value;
		}

		/// <summary>
		/// Reads a 7-bit encoded integer
		/// </summary>
		/// <returns></returns>
		public uint Read7BitEncodedUInt32() {
			uint val = 0;
			int bits = 0;
			for (int i = 0; i < 5; i++) {
				byte b = ReadByte();
				val |= (uint)(b & 0x7F) << bits;
				if ((b & 0x80) == 0)
					return val;
				bits += 7;
			}
			ThrowDataReaderException("Invalid encoded UInt32");
			return 0;
		}

		/// <summary>
		/// Reads a 7-bit encoded integer
		/// </summary>
		/// <returns></returns>
		public int Read7BitEncodedInt32() => (int)Read7BitEncodedUInt32();

		/// <summary>
		/// Reads a serialized UTF-8 string
		/// </summary>
		/// <returns></returns>
		public string ReadSerializedString() => ReadSerializedString(Encoding.UTF8);

		/// <summary>
		/// Reads a serialized string
		/// </summary>
		/// <param name="encoding">Encoding</param>
		/// <returns></returns>
		public string ReadSerializedString(Encoding encoding) {
			if (encoding is null)
				ThrowArgumentNullException(nameof(encoding));
			int length = Read7BitEncodedInt32();
			if (length < 0)
				ThrowNoMoreBytesLeft();
			if (length == 0)
				return string.Empty;
			return ReadString(length, encoding);
		}

		/// <summary>
		/// Returns all data without updating the current position
		/// </summary>
		/// <returns></returns>
		public readonly byte[] ToArray() {
			int length = (int)Length;
			if (length < 0)
				ThrowInvalidOperationException();
			// This is also true if 'this' is the 'default' instance ('stream' is null)
			if (length == 0)
				return Array2.Empty<byte>();
			var data = new byte[length];
			stream.ReadBytes(startOffset, data, 0, data.Length);
			return data;
		}

		/// <summary>
		/// Returns the remaining data
		/// </summary>
		/// <returns></returns>
		public byte[] ReadRemainingBytes() {
			int length = (int)BytesLeft;
			if (length < 0)
				ThrowInvalidOperationException();
			return ReadBytes(length);
		}

		/// <summary>
		/// Reads all bytes until a terminating byte or returns null if <paramref name="value"/> wasn't found.
		/// If found, the current offset is incremented by the length of the returned data
		/// </summary>
		/// <param name="value">Terminating byte value</param>
		/// <returns></returns>
		public byte[] TryReadBytesUntil(byte value) {
			var currentOffset = this.currentOffset;
			var endOffset = this.endOffset;
			// This is also true if 'this' is the 'default' instance ('stream' is null)
			if (currentOffset == endOffset)
				return null;
			if (!stream.TryGetOffsetOf(currentOffset, endOffset, value, out var valueOffset))
				return null;
			int length = (int)(valueOffset - currentOffset);
			if (length < 0)
				return null;
			return ReadBytes(length);
		}

		/// <summary>
		/// Reads a zero-terminated UTF-8 string or returns null if the string couldn't be read.
		/// If successful, the current offset is incremented past the terminating zero.
		/// </summary>
		/// <returns></returns>
		public string TryReadZeroTerminatedUtf8String() => TryReadZeroTerminatedString(Encoding.UTF8);

		/// <summary>
		/// Reads a zero-terminated string or returns null if the string couldn't be read.
		/// If successful, the current offset is incremented past the terminating zero.
		/// </summary>
		/// <param name="encoding">Encoding</param>
		/// <returns></returns>
		public string TryReadZeroTerminatedString(Encoding encoding) {
			if (encoding is null)
				ThrowArgumentNullException(nameof(encoding));
			VerifyState();
			var currentOffset = this.currentOffset;
			var endOffset = this.endOffset;
			// This is also true if 'this' is the 'default' instance ('stream' is null)
			if (currentOffset == endOffset)
				return null;
			if (!stream.TryGetOffsetOf(currentOffset, endOffset, 0, out var valueOffset))
				return null;
			int length = (int)(valueOffset - currentOffset);
			if (length < 0)
				return null;
			var value = length == 0 ? string.Empty : stream.ReadString(currentOffset, length, encoding);
			this.currentOffset = valueOffset + 1;
			VerifyState();
			return value;
		}

		/// <summary>
		/// Reads a UTF-8 encoded string
		/// </summary>
		/// <param name="byteCount">Number of bytes to read (not characters)</param>
		/// <returns></returns>
		public string ReadUtf8String(int byteCount) => ReadString(byteCount, Encoding.UTF8);

		/// <summary>
		/// Reads a string
		/// </summary>
		/// <param name="byteCount">Number of bytes to read (not characters)</param>
		/// <param name="encoding">Encoding</param>
		/// <returns></returns>
		public string ReadString(int byteCount, Encoding encoding) {
			if (byteCount < 0)
				ThrowInvalidArgument(nameof(byteCount));
			if (encoding is null)
				ThrowArgumentNullException(nameof(encoding));
			if (byteCount == 0)
				return string.Empty;
			if ((uint)byteCount > Length)
				ThrowInvalidArgument(nameof(byteCount));
			VerifyState();
			var currentOffset = this.currentOffset;
			var value = stream.ReadString(currentOffset, byteCount, encoding);
			this.currentOffset = currentOffset + (uint)byteCount;
			VerifyState();
			return value;
		}

		/// <summary>
		/// Creates a <see cref="Stream"/> that can access this content. The caller doesn't have to dispose of the returned stream.
		/// </summary>
		/// <returns></returns>
		public readonly Stream AsStream() => new DataReaderStream(this);

		readonly byte[] AllocTempBuffer() => new byte[(int)Math.Min(0x2000, BytesLeft)];

		/// <summary>
		/// Copies the data, starting from <see cref="Position"/>, to <paramref name="destination"/>
		/// </summary>
		/// <param name="destination">Destination</param>
		/// <returns>Number of bytes written</returns>
		public void CopyTo(DataWriter destination) {
			if (destination is null)
				ThrowArgumentNullException(nameof(destination));
			if (Position >= Length)
				return;
			CopyTo(destination.InternalStream, AllocTempBuffer());
		}

		/// <summary>
		/// Copies the data, starting from <see cref="Position"/>, to <paramref name="destination"/>
		/// </summary>
		/// <param name="destination">Destination</param>
		/// <param name="dataBuffer">Temp buffer during writing</param>
		/// <returns>Number of bytes written</returns>
		public void CopyTo(DataWriter destination, byte[] dataBuffer) {
			if (destination is null)
				ThrowArgumentNullException(nameof(destination));
			CopyTo(destination.InternalStream, dataBuffer);
		}

		/// <summary>
		/// Copies the data, starting from <see cref="Position"/>, to <paramref name="destination"/>
		/// </summary>
		/// <param name="destination">Destination</param>
		/// <returns>Number of bytes written</returns>
		public void CopyTo(BinaryWriter destination) {
			if (destination is null)
				ThrowArgumentNullException(nameof(destination));
			if (Position >= Length)
				return;
			CopyTo(destination.BaseStream, AllocTempBuffer());
		}

		/// <summary>
		/// Copies the data, starting from <see cref="Position"/>, to <paramref name="destination"/>
		/// </summary>
		/// <param name="destination">Destination</param>
		/// <param name="dataBuffer">Temp buffer during writing</param>
		/// <returns>Number of bytes written</returns>
		public void CopyTo(BinaryWriter destination, byte[] dataBuffer) {
			if (destination is null)
				ThrowArgumentNullException(nameof(destination));
			CopyTo(destination.BaseStream, dataBuffer);
		}

		/// <summary>
		/// Copies the data, starting from <see cref="Position"/>, to <paramref name="destination"/>
		/// </summary>
		/// <param name="destination">Destination</param>
		/// <returns>Number of bytes written</returns>
		public void CopyTo(Stream destination) {
			if (destination is null)
				ThrowArgumentNullException(nameof(destination));
			if (Position >= Length)
				return;
			CopyTo(destination, AllocTempBuffer());
		}

		/// <summary>
		/// Copies the data, starting from <see cref="Position"/>, to <paramref name="destination"/>
		/// </summary>
		/// <param name="destination">Destination</param>
		/// <param name="dataBuffer">Temp buffer during writing</param>
		/// <returns>Number of bytes written</returns>
		public void CopyTo(Stream destination, byte[] dataBuffer) {
			if (destination is null)
				ThrowArgumentNullException(nameof(destination));
			if (dataBuffer is null)
				ThrowArgumentNullException(nameof(dataBuffer));
			if (Position >= Length)
				return;
			if (dataBuffer.Length == 0)
				ThrowInvalidArgument(nameof(dataBuffer));
			uint lenLeft = BytesLeft;
			while (lenLeft > 0) {
				int num = (int)Math.Min((uint)dataBuffer.Length, lenLeft);
				lenLeft -= (uint)num;
				ReadBytes(dataBuffer, 0, num);
				destination.Write(dataBuffer, 0, num);
			}
		}
	}
}
