// dnlib: See LICENSE.txt for more info

using System.Diagnostics;

namespace dnlib.DotNet.Writer {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
	/// <summary>
	/// Writes data
	/// </summary>
	public unsafe struct DataWriter {
		public int Position => position;

		readonly byte[] data;
		int position;

		public DataWriter(byte[] data) {
			this.data = data;
			position = 0;
		}

		public void Write(sbyte value) => data[position++] = (byte)value;
		public void Write(byte value) => data[position++] = value;

		public void Write(short value) {
			data[position++] = (byte)value;
			data[position++] = (byte)(value >> 8);
		}

		public void Write(ushort value) {
			data[position++] = (byte)value;
			data[position++] = (byte)(value >> 8);
		}

		public void Write(int value) {
			Debug.Assert(this.position + 4 <= data.Length);
			var position = this.position;
			fixed (byte* p = data)
				*(int*)(p + position) = value;
			this.position = position + 4;
		}

		public void Write(uint value) {
			Debug.Assert(this.position + 4 <= data.Length);
			var position = this.position;
			fixed (byte* p = data)
				*(uint*)(p + position) = value;
			this.position = position + 4;
		}

		public void Write(long value) {
			Debug.Assert(this.position + 8 <= data.Length);
			var position = this.position;
			fixed (byte* p = data)
				*(long*)(p + position) = value;
			this.position = position + 8;
		}

		public void Write(ulong value) {
			Debug.Assert(this.position + 8 <= data.Length);
			var position = this.position;
			fixed (byte* p = data)
				*(ulong*)(p + position) = value;
			this.position = position + 8;
		}

		public void Write(float value) {
			Debug.Assert(this.position + 4 <= data.Length);
			var position = this.position;
			fixed (byte* p = data)
				*(float*)(p + position) = value;
			this.position = position + 4;
		}

		public void Write(double value) {
			Debug.Assert(this.position + 8 <= data.Length);
			var position = this.position;
			fixed (byte* p = data)
				*(double*)(p + position) = value;
			this.position = position + 8;
		}
	}
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
}
