// dnlib: See LICENSE.txt for more info

﻿using System.IO;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Stores a byte array
	/// </summary>
	public sealed class ByteArrayChunk : IChunk {
		readonly byte[] array;
		FileOffset offset;
		RVA rva;

		/// <inheritdoc/>
		public FileOffset FileOffset {
			get { return offset; }
		}

		/// <inheritdoc/>
		public RVA RVA {
			get { return rva; }
		}

		/// <summary>
		/// Gets the data
		/// </summary>
		public byte[] Data {
			get { return array; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="array">The data. It will be owned by this instance and can't be modified by
		/// other code if this instance is inserted as a <c>key</c> in a dictionary (because
		/// <see cref="GetHashCode"/> return value will be different if you modify the array). If
		/// it's never inserted as a <c>key</c> in a dictionary, then the contents can be modified,
		/// but shouldn't be resized after <see cref="SetOffset"/> has been called.</param>
		public ByteArrayChunk(byte[] array) {
			this.array = array ?? new byte[0];
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;
		}

		/// <inheritdoc/>
		public uint GetFileLength() {
			return (uint)array.Length;
		}

		/// <inheritdoc/>
		public uint GetVirtualSize() {
			return GetFileLength();
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			writer.Write(array);
		}

		/// <inheritdoc/>
		public override int GetHashCode() {
			return Utils.GetHashCode(array);
		}

		/// <inheritdoc/>
		public override bool Equals(object obj) {
			var other = obj as ByteArrayChunk;
			return other != null && Utils.Equals(array, other.array);
		}
	}
}
