using System;
using System.IO;
using System.Text;

namespace dot10.IO {
	/// <summary>
	/// Reads binary data
	/// </summary>
	public interface IBinaryReader : IDisposable {
		/// <summary>
		/// Returns the length of the stream
		/// </summary>
		long Length { get; }

		/// <summary>
		/// Gets/sets the position
		/// </summary>
		long Position { get; set; }

		/// <summary>
		/// Reads <paramref name="size"/> bytes from the current <see cref="Position"/>
		/// and increments <see cref="Position"/> by <paramref name="size"/> bytes
		/// </summary>
		/// <param name="size">Number of bytes to read</param>
		/// <returns>All bytes</returns>
		/// <exception cref="IOException">An I/O error occurs</exception>
		byte[] ReadBytes(int size);

		/// <summary>
		/// Reads <paramref name="length"/> bytes to <paramref name="buffer"/>
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="offset">Offset in buffer where to place all read bytes</param>
		/// <param name="length">Number of bytes to read</param>
		/// <returns>Number of bytes read, and can be less than <paramref name="length"/> if
		/// there's no more bytes to read.</returns>
		int Read(byte[] buffer, int offset, int length);

		/// <summary>
		/// Reads bytes until byte <paramref name="b"/> is found. <see cref="Position"/> is
		/// incremented by the number of bytes read (size of return value).
		/// </summary>
		/// <param name="b">The terminating byte</param>
		/// <returns>All the bytes (not including <paramref name="b"/>) or <c>null</c> if
		/// <paramref name="b"/> wasn't found.</returns>
		byte[] ReadBytesUntilByte(byte b);

		/// <summary>
		/// Reads a <see cref="sbyte"/> from the current position and increments <see cref="Position"/> by 1
		/// </summary>
		/// <returns>The 8-bit signed byte</returns>
		/// <exception cref="IOException">An I/O error occurs</exception>
		sbyte ReadSByte();

		/// <summary>
		/// Reads a <see cref="byte"/> from the current position and increments <see cref="Position"/> by 1
		/// </summary>
		/// <returns>The 8-bit unsigned byte</returns>
		/// <exception cref="IOException">An I/O error occurs</exception>
		byte ReadByte();

		/// <summary>
		/// Reads a <see cref="Int16"/> from the current position and increments <see cref="Position"/> by 2
		/// </summary>
		/// <returns>The 16-bit signed integer</returns>
		/// <exception cref="IOException">An I/O error occurs</exception>
		short ReadInt16();

		/// <summary>
		/// Reads a <see cref="UInt16"/> from the current position and increments <see cref="Position"/> by 2
		/// </summary>
		/// <returns>The 16-bit unsigned integer</returns>
		/// <exception cref="IOException">An I/O error occurs</exception>
		ushort ReadUInt16();

		/// <summary>
		/// Reads a <see cref="Int32"/> from the current position and increments <see cref="Position"/> by 4
		/// </summary>
		/// <returns>The 32-bit signed integer</returns>
		/// <exception cref="IOException">An I/O error occurs</exception>
		int ReadInt32();

		/// <summary>
		/// Reads a <see cref="UInt32"/> from the current position and increments <see cref="Position"/> by 4
		/// </summary>
		/// <returns>The 32-bit unsigned integer</returns>
		/// <exception cref="IOException">An I/O error occurs</exception>
		uint ReadUInt32();

		/// <summary>
		/// Reads a <see cref="Int64"/> from the current position and increments <see cref="Position"/> by 8
		/// </summary>
		/// <returns>The 64-bit signed integer</returns>
		/// <exception cref="IOException">An I/O error occurs</exception>
		long ReadInt64();

		/// <summary>
		/// Reads a <see cref="UInt64"/> from the current position and increments <see cref="Position"/> by 8
		/// </summary>
		/// <returns>The 64-bit unsigned integer</returns>
		/// <exception cref="IOException">An I/O error occurs</exception>
		ulong ReadUInt64();

		/// <summary>
		/// Reads a <see cref="Single"/> from the current position and increments <see cref="Position"/> by 4
		/// </summary>
		/// <returns>The 32-bit single</returns>
		/// <exception cref="IOException">An I/O error occurs</exception>
		float ReadSingle();

		/// <summary>
		/// Reads a <see cref="Double"/> from the current position and increments <see cref="Position"/> by 8
		/// </summary>
		/// <returns>The 64-bit double</returns>
		/// <exception cref="IOException">An I/O error occurs</exception>
		double ReadDouble();

		/// <summary>
		/// Reads a <see cref="String"/> from the current position and increments <see cref="Position"/>
		/// by the number of bytes read.
		/// </summary>
		/// <param name="chars">Number of characters to read</param>
		/// <returns>The string</returns>
		/// <exception cref="IOException">An I/O error occurs</exception>
		string ReadString(int chars);
	}

	public static partial class IOExtensions {
		/// <summary>
		/// Reads all remaining bytes
		/// </summary>
		/// <param name="reader">this</param>
		/// <returns>All remaining bytes</returns>
		public static byte[] ReadRemainingBytes(this IBinaryReader reader) {
			if (reader.Position >= reader.Length)
				return new byte[0];
			return reader.ReadBytes((int)(reader.Length - reader.Position));
		}

		/// <summary>
		/// Reads all bytes
		/// </summary>
		/// <param name="reader">this</param>
		/// <returns>All bytes</returns>
		public static byte[] ReadAllBytes(this IBinaryReader reader) {
			reader.Position = 0;
			return reader.ReadBytes((int)reader.Length);
		}

		/// <summary>
		/// Reads a <see cref="Boolean"/> from the current position and increments <see cref="IBinaryReader.Position"/> by 1
		/// </summary>
		/// <param name="self">this</param>
		/// <returns>The boolean</returns>
		/// <exception cref="IOException">An I/O error occurs</exception>
		public static bool ReadBoolean(this IBinaryReader self) {
			return self.ReadByte() != 0;
		}

		/// <summary>
		/// Reads a <see cref="Char"/> from the current position and increments <see cref="IBinaryReader.Position"/> by 2
		/// </summary>
		/// <param name="self">this</param>
		/// <returns>The char</returns>
		/// <exception cref="IOException">An I/O error occurs</exception>
		public static char ReadChar(this IBinaryReader self) {
			return self.ReadChar(Encoding.UTF8);
		}

		/// <summary>
		/// Reads a <see cref="Char"/> from the current position and increments <see cref="IBinaryReader.Position"/> by 2
		/// </summary>
		/// <param name="self">this</param>
		/// <param name="encoding">Encoding</param>
		/// <returns>The char</returns>
		/// <exception cref="IOException">An I/O error occurs</exception>
		public static char ReadChar(this IBinaryReader self, Encoding encoding) {
			// This is slow but this method should rarely be called...
			var decoder = encoding.GetDecoder();
			bool twoBytes = encoding is UnicodeEncoding;
			byte[] bytes = new byte[2];
			char[] chars = new char[1];
			while (true) {
				bytes[0] = self.ReadByte();
				if (twoBytes)
					bytes[1] = self.ReadByte();
				int x = decoder.GetChars(bytes, 0, twoBytes ? 2 : 1, chars, 0);
				if (x != 0)
					break;
			}
			return chars[0];
		}

		/// <summary>
		/// Reads a UTF-8 <see cref="string"/> from the current position and increments
		/// <see cref="IBinaryReader.Position"/> by the length of the string.
		/// </summary>
		/// <param name="reader">this</param>
		/// <returns>The string</returns>
		public static string ReadString(this IBinaryReader reader) {
			return reader.ReadString(Encoding.UTF8);
		}

		/// <summary>
		/// Reads a <see cref="string"/> from the current position and increments
		/// <see cref="IBinaryReader.Position"/> by the length of the string.
		/// </summary>
		/// <param name="reader">this</param>
		/// <param name="encoding">Encoding</param>
		/// <returns>The string</returns>
		public static string ReadString(this IBinaryReader reader, Encoding encoding) {
			int len = reader.Read7BitEncodedInt32();
			return encoding.GetString(reader.ReadBytes(len));
		}

		/// <summary>
		/// Reads a <see cref="Decimal"/> from the current position and increments
		/// <see cref="IBinaryReader.Position"/> by 16
		/// </summary>
		/// <param name="reader">this</param>
		/// <returns>The decmial</returns>
		/// <exception cref="IOException">An I/O error occurs</exception>
		public static decimal ReadDecimal(this IBinaryReader reader) {
			var bits = new int[4] {
				reader.ReadInt32(),	// lo
				reader.ReadInt32(),	// mid
				reader.ReadInt32(),	// hi
				reader.ReadInt32(),	// flags
			};
			return new decimal(bits);
		}

		/// <summary>
		/// Reads chars
		/// </summary>
		/// <param name="reader">this</param>
		/// <param name="length">Number of <see cref="char"/>s to read</param>
		/// <returns>All the chars</returns>
		public static char[] ReadChars(this IBinaryReader reader, int length) {
			var chars = new char[length];
			for (int i = 0; i < length; i++)
				chars[i] = reader.ReadChar();
			return chars;
		}

		/// <summary>
		/// Reads a compressed <see cref="uint"/> from the current position in <paramref name="reader"/>
		/// </summary>
		/// <remarks>Max value it can return is <c>0x1FFFFFFF</c></remarks>
		/// <param name="reader">The reader</param>
		/// <param name="val">Decompressed value</param>
		/// <returns><c>true</c> if successful, <c>false</c> on failure</returns>
		public static bool ReadCompressedUInt32(this IBinaryReader reader, out uint val) {
			var pos = reader.Position;
			var len = reader.Length;
			if (pos >= len) {
				val = 0;
				return false;
			}

			byte b = reader.ReadByte();
			if ((b & 0x80) == 0) {
				val = b;
				return true;
			}

			if ((b & 0xC0) == 0x80) {
				if (pos + 1 < pos || pos + 1 >= len) {
					val = 0;
					return false;
				}
				val = (uint)(((b & 0x3F) << 8) | reader.ReadByte());
				return true;
			}

			// The encoding 111x isn't allowed but the CLR sometimes doesn't verify this
			// and just assumes it's 110x. Don't fail if it's 111x, just assume it's 110x.

			if (pos + 3 < pos || pos + 3 >= len) {
				val = 0;
				return false;
			}
			val = (uint)(((b & 0x1F) << 24) | (reader.ReadByte() << 16) |
					(reader.ReadByte() << 8) | reader.ReadByte());
			return true;
		}

		/// <summary>
		/// Reads a compressed <see cref="int"/> from the current position in <paramref name="reader"/>
		/// </summary>
		/// <param name="reader">The reader</param>
		/// <param name="val">Decompressed value</param>
		/// <returns><c>true</c> if successful, <c>false</c> on failure</returns>
		public static bool ReadCompressedInt32(this IBinaryReader reader, out int val) {
			var pos = reader.Position;
			var len = reader.Length;
			if (pos >= len) {
				val = 0;
				return false;
			}

			byte b = reader.ReadByte();
			if ((b & 0x80) == 0) {
				if ((b & 1) != 0)
					val = -0x40 | (b >> 1);
				else
					val = (b >> 1);
				return true;
			}

			if ((b & 0xC0) == 0x80) {
				if (pos + 1 < pos || pos + 1 >= len) {
					val = 0;
					return false;
				}
				uint tmp = (uint)(((b & 0x3F) << 8) | reader.ReadByte());
				if ((tmp & 1) != 0)
					val = -0x2000 | (int)(tmp >> 1);
				else
					val = (int)(tmp >> 1);
				return true;
			}

			if ((b & 0xE0) == 0xC0) {
				if (pos + 3 < pos || pos + 3 >= len) {
					val = 0;
					return false;
				}
				uint tmp = (uint)(((b & 0x1F) << 24) | (reader.ReadByte() << 16) |
						(reader.ReadByte() << 8) | reader.ReadByte());
				if ((tmp & 1) != 0)
					val = -0x10000000 | (int)(tmp >> 1);
				else
					val = (int)(tmp >> 1);
				return true;
			}

			val = 0;
			return false;
		}

		/// <summary>
		/// Reads a compressed <see cref="uint"/> from the current position in <paramref name="reader"/>
		/// </summary>
		/// <param name="reader">The reader</param>
		/// <returns>The value</returns>
		public static uint ReadCompressedUInt32(this IBinaryReader reader) {
			uint val;
			if (!reader.ReadCompressedUInt32(out val))
				throw new IOException("Could not read a compressed UInt32");
			return val;
		}

		/// <summary>
		/// Reads a compressed <see cref="int"/> from the current position in <paramref name="reader"/>
		/// </summary>
		/// <param name="reader">The reader</param>
		/// <returns>The value</returns>
		public static int ReadCompressedInt32(this IBinaryReader reader) {
			int val;
			if (!reader.ReadCompressedInt32(out val))
				throw new IOException("Could not read a compressed Int32");
			return val;
		}

		/// <summary>
		/// Reads a 7-bit encoded integer
		/// </summary>
		/// <param name="reader">this</param>
		/// <returns>The decoded integer</returns>
		public static uint Read7BitEncodedUInt32(this IBinaryReader reader) {
			uint val = 0;
			int bits = 0;
			for (int i = 0; i < 5; i++) {
				byte b = reader.ReadByte();
				val |= (uint)(b & 0x7F) << bits;
				if ((b & 0x80) == 0)
					return val;
				bits += 7;
			}
			throw new IOException("Invalid encoded int32");
		}

		/// <summary>
		/// Reads a 7-bit encoded integer
		/// </summary>
		/// <param name="reader">this</param>
		/// <returns>The decoded integer</returns>
		public static int Read7BitEncodedInt32(this IBinaryReader reader) {
			return (int)reader.Read7BitEncodedUInt32();
		}

		/// <summary>
		/// Creates a <see cref="Stream"/> using <paramref name="reader"/>. The created
		/// <see cref="Stream"/> doesn't own <paramref name="reader"/>, so it's not
		/// <c>Dispose()</c>'d.
		/// </summary>
		/// <param name="reader">this</param>
		/// <returns>A new <see cref="Stream"/> instance</returns>
		public static Stream CreateStream(this IBinaryReader reader) {
			return new BinaryReaderStream(reader);
		}

		/// <summary>
		/// Creates a <see cref="Stream"/> using <paramref name="reader"/>
		/// </summary>
		/// <param name="reader">this</param>
		/// <param name="ownsReader"><c>true</c> if the created <see cref="Stream"/> owns
		/// <paramref name="reader"/></param>
		/// <returns>A new <see cref="Stream"/> instance</returns>
		public static Stream CreateStream(this IBinaryReader reader, bool ownsReader) {
			return new BinaryReaderStream(reader, ownsReader);
		}

		/// <summary>
		/// Checks whether we can read <paramref name="size"/> bytes
		/// </summary>
		/// <param name="reader">Reader</param>
		/// <param name="size">Size in bytes</param>
		public static bool CanRead(this IBinaryReader reader, int size) {
			return (reader.Position + size <= reader.Length && reader.Position + size >= reader.Position) || size == 0;
		}

		/// <summary>
		/// Checks whether we can read <paramref name="size"/> bytes
		/// </summary>
		/// <param name="reader">Reader</param>
		/// <param name="size">Size in bytes</param>
		public static bool CanRead(this IBinaryReader reader, uint size) {
			return (reader.Position + size <= reader.Length && reader.Position + size >= reader.Position) || size == 0;
		}

		/// <summary>
		/// Writes <paramref name="reader"/>, starting at <paramref name="reader"/>'s current
		/// position, to <paramref name="writer"/> starting at <paramref name="writer"/>'s
		/// current position. Returns the number of bytes written.
		/// </summary>
		/// <param name="reader">Reader</param>
		/// <param name="writer">Writer</param>
		/// <returns>Number of bytes written</returns>
		/// <exception cref="IOException">Could not write all bytes or data is too big</exception>
		public static uint WriteTo(this IBinaryReader reader, BinaryWriter writer) {
			if (reader.Position >= reader.Length)
				return 0;
			return reader.WriteTo(writer, new byte[0x2000]);
		}

		/// <summary>
		/// Writes <paramref name="reader"/>, starting at <paramref name="reader"/>'s current
		/// position, to <paramref name="writer"/> starting at <paramref name="writer"/>'s
		/// current position. Returns the number of bytes written.
		/// </summary>
		/// <param name="reader">Reader</param>
		/// <param name="writer">Writer</param>
		/// <param name="dataBuffer">Temp buffer during writing</param>
		/// <returns>Number of bytes written</returns>
		/// <exception cref="IOException">Could not write all bytes or data is too big</exception>
		public static uint WriteTo(this IBinaryReader reader, BinaryWriter writer, byte[] dataBuffer) {
			if (reader.Position >= reader.Length)
				return 0;
			long longLenLeft = reader.Length - reader.Position;
			if (longLenLeft > uint.MaxValue)
				throw new IOException("Data is too big");
			uint lenLeft = (uint)longLenLeft;
			uint writtenBytes = lenLeft;
			while (lenLeft > 0) {
				int num = (int)Math.Min((uint)dataBuffer.Length, lenLeft);
				lenLeft -= (uint)num;
				if (num != reader.Read(dataBuffer, 0, num))
					throw new IOException("Could not read all reader bytes");
				writer.Write(dataBuffer, 0, num);
			}
			return writtenBytes;
		}
	}
}
