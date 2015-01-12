// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Collections.Generic;
using System.IO;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// .NET resources
	/// </summary>
	public sealed class NetResources : IChunk {
		readonly List<ByteArrayChunk> resources = new List<ByteArrayChunk>();
		readonly uint alignment;
		uint length;
		bool setOffsetCalled;
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
		/// Gets offset of next resource. This offset is relative to the start of
		/// the .NET resources and is always aligned.
		/// </summary>
		public uint NextOffset {
			get { return length; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="alignment">Alignment of all resources</param>
		public NetResources(uint alignment) {
			this.alignment = alignment;
		}

		/// <summary>
		/// Adds a resource
		/// </summary>
		/// <param name="stream">The resource data</param>
		/// <returns>The resource data</returns>
		public ByteArrayChunk Add(IImageStream stream) {
			if (setOffsetCalled)
				throw new InvalidOperationException("SetOffset() has already been called");
			var rawData = stream.ReadAllBytes();
			length = Utils.AlignUp(length + 4 + (uint)rawData.Length, alignment);
			var data = new ByteArrayChunk(rawData);
			resources.Add(data);
			return data;
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			setOffsetCalled = true;
			this.offset = offset;
			this.rva = rva;
			foreach (var resource in resources) {
				resource.SetOffset(offset + 4, rva + 4);
				uint len = 4 + resource.GetFileLength();
				offset = (offset + len).AlignUp(alignment);
				rva = (rva + len).AlignUp(alignment);
			}
		}

		/// <inheritdoc/>
		public uint GetFileLength() {
			return length;
		}

		/// <inheritdoc/>
		public uint GetVirtualSize() {
			return GetFileLength();
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			RVA rva2 = rva;
			foreach (var resourceData in resources) {
				writer.Write(resourceData.GetFileLength());
				resourceData.VerifyWriteTo(writer);
				rva2 += 4 + resourceData.GetFileLength();
				int padding = (int)rva2.AlignUp(alignment) - (int)rva2;
				writer.WriteZeros(padding);
				rva2 += (uint)padding;
			}
		}
	}
}
