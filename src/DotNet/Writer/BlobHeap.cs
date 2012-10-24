using System;
using System.Collections.Generic;
using System.IO;
using dot10.IO;
using dot10.DotNet.MD;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// #Blob heap
	/// </summary>
	sealed class BlobHeap : HeapBase {
		Dictionary<byte[], uint> cachedDict = new Dictionary<byte[], uint>(ByteArrayEqualityComparer.Instance);
		List<byte[]> cached = new List<byte[]>();
		uint nextOffset = 1;
		byte[] originalData;

		/// <inheritdoc/>
		public override string Name {
			get { return "#Blob"; }
		}

		/// <summary>
		/// Populates blobs from an existing <see cref="BlobStream"/> (eg. to preserve
		/// blob offsets)
		/// </summary>
		/// <param name="blobStream">The #Blob stream with the original content</param>
		public void Populate(BlobStream blobStream) {
			if (originalData != null)
				throw new InvalidOperationException("Can't call method twice");
			if (nextOffset != 1)
				throw new InvalidOperationException("Add() has already been called");
			if (blobStream == null || blobStream.ImageStream.Length == 0)
				return;

			using (var reader = blobStream.ImageStream.Create(0)) {
				originalData = reader.ReadBytes((int)reader.Length);
				nextOffset = (uint)originalData.Length;
				Populate(reader);
			}
		}

		void Populate(IImageStream reader) {
			reader.Position = 1;
			while (reader.Position < reader.Length) {
				uint offset = (uint)reader.Position;
				uint len;
				if (!reader.ReadCompressedUInt32(out len)) {
					if (offset == reader.Position)
						reader.Position++;
					continue;
				}
				if (len == 0 || reader.Position + len > reader.Length)
					continue;

				var data = reader.ReadBytes((int)len);
				if (!cachedDict.ContainsKey(data))
					cachedDict[data] = offset;
			}
		}

		/// <summary>
		/// Adds data to the #Blob heap
		/// </summary>
		/// <param name="data">The data</param>
		/// <returns>The offset of the data in the #Blob heap</returns>
		public uint Add(byte[] data) {
			if (data == null || data.Length == 0)
				return 0;

			uint offset;
			if (cachedDict.TryGetValue(data, out offset))
				return offset;

			cached.Add(data);
			cachedDict[data] = offset = nextOffset;
			nextOffset += (uint)(Utils.GetCompressedUInt32Length((uint)data.Length) + data.Length);
			return offset;
		}

		/// <inheritdoc/>
		public override uint GetRawLength() {
			return nextOffset;
		}

		/// <inheritdoc/>
		public override void WriteToImpl(BinaryWriter writer) {
			if (originalData != null)
				writer.Write(originalData);
			else
				writer.Write((byte)0);
			foreach (var data in cached) {
				writer.WriteCompressedUInt32((uint)data.Length);
				writer.Write(data);
			}
		}
	}
}
