// dnlib: See LICENSE.txt for more info

using System;
using System.Text;

namespace dnlib.IO {
	/// <summary>
	/// This class is used by a <see cref="DataReader"/>. The <see cref="DataReader"/> instance
	/// verifies that all input are valid before calling any methods in this class.
	/// This class is thread safe.
	/// </summary>
	public abstract class DataStream {
		/// <summary>
		/// Reads bytes
		/// </summary>
		/// <param name="offset">Offset of data</param>
		/// <param name="destination">Destination pointer</param>
		/// <param name="length">Number of bytes to read</param>
		public unsafe abstract void ReadBytes(uint offset, void* destination, int length);

		/// <summary>
		/// Reads bytes
		/// </summary>
		/// <param name="offset">Offset of data</param>
		/// <param name="destination">Destination array</param>
		/// <param name="destinationIndex">Destination index</param>
		/// <param name="length">Number of bytes to read</param>
		public abstract void ReadBytes(uint offset, byte[] destination, int destinationIndex, int length);

		/// <summary>
		/// Reads a <see cref="byte"/>
		/// </summary>
		/// <param name="offset">Offset of data</param>
		/// <returns></returns>
		public abstract byte ReadByte(uint offset);

		/// <summary>
		/// Reads a <see cref="ushort"/>
		/// </summary>
		/// <param name="offset">Offset of data</param>
		/// <returns></returns>
		public abstract ushort ReadUInt16(uint offset);

		/// <summary>
		/// Reads a <see cref="uint"/>
		/// </summary>
		/// <param name="offset">Offset of data</param>
		/// <returns></returns>
		public abstract uint ReadUInt32(uint offset);

		/// <summary>
		/// Reads a <see cref="ulong"/>
		/// </summary>
		/// <param name="offset">Offset of data</param>
		/// <returns></returns>
		public abstract ulong ReadUInt64(uint offset);

		/// <summary>
		/// Reads a <see cref="float"/>
		/// </summary>
		/// <param name="offset">Offset of data</param>
		/// <returns></returns>
		public abstract float ReadSingle(uint offset);

		/// <summary>
		/// Reads a <see cref="double"/>
		/// </summary>
		/// <param name="offset">Offset of data</param>
		/// <returns></returns>
		public abstract double ReadDouble(uint offset);

		/// <summary>
		/// Reads a <see cref="Guid"/>
		/// </summary>
		/// <param name="offset">Offset of data</param>
		/// <returns></returns>
		public virtual Guid ReadGuid(uint offset) =>
			new Guid(ReadUInt32(offset), ReadUInt16(offset + 4), ReadUInt16(offset + 6),
				ReadByte(offset + 8), ReadByte(offset + 9), ReadByte(offset + 10), ReadByte(offset + 11),
				ReadByte(offset + 12), ReadByte(offset + 13), ReadByte(offset + 14), ReadByte(offset + 15));

		/// <summary>
		/// Reads a UTF-16 encoded <see cref="string"/>
		/// </summary>
		/// <param name="offset">Offset of data</param>
		/// <param name="chars">Number of characters to read</param>
		/// <returns></returns>
		public abstract string ReadUtf16String(uint offset, int chars);

		/// <summary>
		/// Reads a string
		/// </summary>
		/// <param name="offset">Offset of data</param>
		/// <param name="length">Length of string in bytes</param>
		/// <param name="encoding">Encoding</param>
		/// <returns></returns>
		public abstract string ReadString(uint offset, int length, Encoding encoding);

		/// <summary>
		/// Gets the data offset of a byte or returns false if the byte wasn't found
		/// </summary>
		/// <param name="offset">Offset of data</param>
		/// <param name="endOffset">End offset of data (not inclusive)</param>
		/// <param name="value">Byte value to search for</param>
		/// <param name="valueOffset">Offset of the byte if found</param>
		/// <returns></returns>
		public abstract bool TryGetOffsetOf(uint offset, uint endOffset, byte value, out uint valueOffset);
	}
}
