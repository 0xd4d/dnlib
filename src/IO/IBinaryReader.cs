using System;
using System.IO;

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
		/// Reads bytes until byte <paramref name="b"/> is found
		/// </summary>
		/// <param name="b">The terminating byte</param>
		/// <returns>All the bytes (not including <paramref name="b"/>) or null if
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
					val = b;
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
	}
}
