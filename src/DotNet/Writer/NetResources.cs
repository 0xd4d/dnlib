// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// .NET resources
	/// </summary>
	public sealed class NetResources : IReuseChunk {
		readonly List<DataReaderChunk> resources = new List<DataReaderChunk>();
		readonly uint alignment;
		uint length;
		bool setOffsetCalled;
		FileOffset offset;
		RVA rva;

		internal bool IsEmpty => resources.Count == 0;

		/// <inheritdoc/>
		public FileOffset FileOffset => offset;

		/// <inheritdoc/>
		public RVA RVA => rva;

		/// <summary>
		/// Gets offset of next resource. This offset is relative to the start of
		/// the .NET resources and is always aligned.
		/// </summary>
		public uint NextOffset => Utils.AlignUp(length, alignment);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="alignment">Alignment of all resources</param>
		public NetResources(uint alignment) => this.alignment = alignment;

		/// <summary>
		/// Adds a resource
		/// </summary>
		/// <param name="reader">The resource data</param>
		/// <returns>The resource data</returns>
		public DataReaderChunk Add(DataReader reader) {
			if (setOffsetCalled)
				throw new InvalidOperationException("SetOffset() has already been called");
			length = NextOffset + 4 + reader.Length;
			var data = new DataReaderChunk(ref reader);
			resources.Add(data);
			return data;
		}

		bool IReuseChunk.CanReuse(RVA origRva, uint origSize) => length <= origSize;

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			setOffsetCalled = true;
			this.offset = offset;
			this.rva = rva;
			foreach (var resource in resources) {
				offset = offset.AlignUp(alignment);
				rva = rva.AlignUp(alignment);
				resource.SetOffset(offset + 4, rva + 4);
				uint len = 4 + resource.GetFileLength();
				offset += len;
				rva += len;
			}
		}

		/// <inheritdoc/>
		public uint GetFileLength() => length;

		/// <inheritdoc/>
		public uint GetVirtualSize() => GetFileLength();

		/// <inheritdoc/>
		public void WriteTo(DataWriter writer) {
			var rva2 = rva;
			foreach (var resourceData in resources) {
				int padding = (int)rva2.AlignUp(alignment) - (int)rva2;
				writer.WriteZeroes(padding);
				rva2 += (uint)padding;
				writer.WriteUInt32(resourceData.GetFileLength());
				resourceData.VerifyWriteTo(writer);
				rva2 += 4 + resourceData.GetFileLength();
			}
		}
	}
}
