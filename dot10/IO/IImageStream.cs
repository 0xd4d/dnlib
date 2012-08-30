using System;
using System.IO;

namespace dot10.IO {
	/// <summary>
	/// Interface to access part of some data
	/// </summary>
	public interface IImageStream : IDisposable {
		/// <summary>
		/// Returns the file offset of the stream
		/// </summary>
		FileOffset FileOffset { get; }

		/// <summary>
		/// Returns the length of the stream
		/// </summary>
		long Length { get; }

		/// <summary>
		/// Gets/sets the position relative to <see cref="FileOffset"/>
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
		/// Reads a <see cref="Int32"/> from the current position and increments <see cref="Position"/> by 4
		/// </summary>
		/// <returns>The 32-bit signed integer</returns>
		/// <exception cref="IOException">An I/O error occurs</exception>
		int ReadInt32();

		/// <summary>
		/// Reads a <see cref="Int64"/> from the current position and increments <see cref="Position"/> by 8
		/// </summary>
		/// <returns>The 64-bit signed integer</returns>
		/// <exception cref="IOException">An I/O error occurs</exception>
		long ReadInt64();
	}

	public static partial class IOExtensions {
		/// <summary>
		/// Reads a <see cref="sbyte"/> from the current position and increments <see cref="IImageStream.Position"/> by 1
		/// </summary>
		/// <returns>The 8-bit signed byte</returns>
		/// <exception cref="IOException">An I/O error occurs</exception>
		public static sbyte ReadSByte(this IImageStream self) {
			return (sbyte)self.ReadByte();
		}

		/// <summary>
		/// Reads a <see cref="UInt16"/> from the current position and increments <see cref="IImageStream.Position"/> by 2
		/// </summary>
		/// <returns>The 16-bit unsigned integer</returns>
		/// <exception cref="IOException">An I/O error occurs</exception>
		public static ushort ReadUInt16(this IImageStream self) {
			return (ushort)self.ReadInt16();
		}

		/// <summary>
		/// Reads a <see cref="UInt32"/> from the current position and increments <see cref="IImageStream.Position"/> by 4
		/// </summary>
		/// <returns>The 32-bit unsigned integer</returns>
		/// <exception cref="IOException">An I/O error occurs</exception>
		public static uint ReadUInt32(this IImageStream self) {
			return (uint)self.ReadInt32();
		}

		/// <summary>
		/// Reads a <see cref="UInt64"/> from the current position and increments <see cref="IImageStream.Position"/> by 8
		/// </summary>
		/// <returns>The 64-bit unsigned integer</returns>
		/// <exception cref="IOException">An I/O error occurs</exception>
		public static ulong ReadUInt64(this IImageStream self) {
			return (ulong)self.ReadInt64();
		}
	}
}
