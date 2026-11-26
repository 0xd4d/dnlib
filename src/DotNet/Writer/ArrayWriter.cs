// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Writes data
	/// </summary>
	public unsafe struct ArrayWriter {
		/// <summary>
		/// Gets the current position
		/// </summary>
		public int Position {
			get => position;
			set => position = value;
		}

		readonly byte[] data;
		int position;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">Destination array</param>
		public ArrayWriter(byte[] data) {
			this.data = data;
			position = 0;
		}

		/// <summary>
		/// Writes a <see cref="sbyte"/>
		/// </summary>
		/// <param name="value">Value</param>
		public void WriteSByte(sbyte value) => data[position++] = (byte)value;

		/// <summary>
		/// Writes a <see cref="byte"/>
		/// </summary>
		/// <param name="value">Value</param>
		public void WriteByte(byte value) => data[position++] = value;

		/// <summary>
		/// Writes a <see cref="short"/>
		/// </summary>
		/// <param name="value">Value</param>
		public void WriteInt16(short value) {
			data[position++] = (byte)value;
			data[position++] = (byte)(value >> 8);
		}

		/// <summary>
		/// Writes a <see cref="ushort"/>
		/// </summary>
		/// <param name="value">Value</param>
		public void WriteUInt16(ushort value) {
			data[position++] = (byte)value;
			data[position++] = (byte)(value >> 8);
		}

		/// <summary>
		/// Writes a <see cref="int"/>
		/// </summary>
		/// <param name="value">Value</param>
		public void WriteInt32(int value) {
			Debug2.Assert(this.position + 4 <= this.data.Length);
			var data = this.data;
			int position = this.position;
			data[position] = (byte)value;
			data[position + 1] = (byte)(value >> 8);
			data[position + 2] = (byte)(value >> 16);
			data[position + 3] = (byte)(value >> 24);
			this.position = position + 4;
		}

		/// <summary>
		/// Writes a <see cref="uint"/>
		/// </summary>
		/// <param name="value">Value</param>
		public void WriteUInt32(uint value) {
			Debug2.Assert(this.position + 4 <= this.data.Length);
			var data = this.data;
			int position = this.position;
			data[position] = (byte)value;
			data[position + 1] = (byte)(value >> 8);
			data[position + 2] = (byte)(value >> 16);
			data[position + 3] = (byte)(value >> 24);
			this.position = position + 4;
		}

		/// <summary>
		/// Writes a <see cref="long"/>
		/// </summary>
		/// <param name="value">Value</param>
		public void WriteInt64(long value) {
			Debug2.Assert(this.position + 8 <= this.data.Length);
			var data = this.data;
			int position = this.position;
			data[position] = (byte)value;
			data[position + 1] = (byte)(value >> 8);
			data[position + 2] = (byte)(value >> 16);
			data[position + 3] = (byte)(value >> 24);
			data[position + 4] = (byte)(value >> 32);
			data[position + 5] = (byte)(value >> 40);
			data[position + 6] = (byte)(value >> 48);
			data[position + 7] = (byte)(value >> 56);
			this.position = position + 8;
		}

		/// <summary>
		/// Writes a <see cref="ulong"/>
		/// </summary>
		/// <param name="value">Value</param>
		public void WriteUInt64(ulong value) {
			Debug2.Assert(this.position + 8 <= this.data.Length);
			var data = this.data;
			int position = this.position;
			data[position] = (byte)value;
			data[position + 1] = (byte)(value >> 8);
			data[position + 2] = (byte)(value >> 16);
			data[position + 3] = (byte)(value >> 24);
			data[position + 4] = (byte)(value >> 32);
			data[position + 5] = (byte)(value >> 40);
			data[position + 6] = (byte)(value >> 48);
			data[position + 7] = (byte)(value >> 56);
			this.position = position + 8;
		}

		/// <summary>
		/// Writes a <see cref="float"/>
		/// </summary>
		/// <param name="value">Value</param>
		public void WriteSingle(float value) {
			Debug2.Assert(this.position + 4 <= this.data.Length);
			uint tmp = *(uint*)&value;
			var data = this.data;
			int position = this.position;
			data[position] = (byte)tmp;
			data[position + 1] = (byte)(tmp >> 8);
			data[position + 2] = (byte)(tmp >> 16);
			data[position + 3] = (byte)(tmp >> 24);
			this.position = position + 4;
		}

		/// <summary>
		/// Writes a <see cref="double"/>
		/// </summary>
		/// <param name="value">Value</param>
		public void WriteDouble(double value) {
			Debug2.Assert(this.position + 8 <= this.data.Length);
			ulong tmp = *(ulong*)&value;
			var data = this.data;
			int position = this.position;
			data[position] = (byte)tmp;
			data[position + 1] = (byte)(tmp >> 8);
			data[position + 2] = (byte)(tmp >> 16);
			data[position + 3] = (byte)(tmp >> 24);
			data[position + 4] = (byte)(tmp >> 32);
			data[position + 5] = (byte)(tmp >> 40);
			data[position + 6] = (byte)(tmp >> 48);
			data[position + 7] = (byte)(tmp >> 56);
			this.position = position + 8;
		}

		/// <summary>
		/// Writes bytes
		/// </summary>
		/// <param name="source">Bytes</param>
		public void WriteBytes(byte[] source) => WriteBytes(source, 0, source.Length);

		/// <summary>
		/// Writes bytes
		/// </summary>
		/// <param name="source">Bytes</param>
		/// <param name="index">Source index</param>
		/// <param name="length">Number of bytes to write</param>
		public void WriteBytes(byte[] source, int index, int length) {
			Array.Copy(source, index, data, position, length);
			position += length;
		}
	}
}
