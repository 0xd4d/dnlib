// dnlib: See LICENSE.txt for more info

﻿using System.IO;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// A <see cref="IBinaryReader"/> chunk
	/// </summary>
	public class BinaryReaderChunk : IChunk {
		FileOffset offset;
		RVA rva;
		readonly IBinaryReader data;
		readonly uint virtualSize;

		/// <summary>
		/// Gets the data
		/// </summary>
		public IBinaryReader Data {
			get { return data; }
		}

		/// <inheritdoc/>
		public FileOffset FileOffset {
			get { return offset; }
		}

		/// <inheritdoc/>
		public RVA RVA {
			get { return rva; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">The data</param>
		public BinaryReaderChunk(IBinaryReader data)
			: this(data, (uint)data.Length) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">The data</param>
		/// <param name="virtualSize">Virtual size of <paramref name="data"/></param>
		public BinaryReaderChunk(IBinaryReader data, uint virtualSize) {
			this.data = data;
			this.virtualSize = virtualSize;
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;
		}

		/// <inheritdoc/>
		public uint GetFileLength() {
			return (uint)data.Length;
		}

		/// <inheritdoc/>
		public uint GetVirtualSize() {
			return virtualSize;
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			data.Position = 0;
			data.WriteTo(writer);
		}
	}
}
