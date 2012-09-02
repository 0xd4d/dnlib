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
	}
}
