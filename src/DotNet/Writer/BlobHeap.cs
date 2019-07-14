// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using dnlib.IO;
using dnlib.DotNet.MD;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// #Blob heap
	/// </summary>
	public sealed class BlobHeap : HeapBase, IOffsetHeap<byte[]> {
		readonly Dictionary<byte[], uint> cachedDict = new Dictionary<byte[], uint>(ByteArrayEqualityComparer.Instance);
		readonly List<byte[]> cached = new List<byte[]>();
		uint nextOffset = 1;
		byte[] originalData;
		Dictionary<uint, byte[]> userRawData;

		/// <inheritdoc/>
		public override string Name => "#Blob";

		/// <summary>
		/// Populates blobs from an existing <see cref="BlobStream"/> (eg. to preserve
		/// blob offsets)
		/// </summary>
		/// <param name="blobStream">The #Blob stream with the original content</param>
		public void Populate(BlobStream blobStream) {
			if (isReadOnly)
				throw new ModuleWriterException("Trying to modify #Blob when it's read-only");
			if (!(originalData is null))
				throw new InvalidOperationException("Can't call method twice");
			if (nextOffset != 1)
				throw new InvalidOperationException("Add() has already been called");
			if (blobStream is null || blobStream.StreamLength == 0)
				return;

			var reader = blobStream.CreateReader();
			originalData = reader.ToArray();
			nextOffset = (uint)originalData.Length;
			Populate(ref reader);
		}

		void Populate(ref DataReader reader) {
			reader.Position = 1;
			while (reader.Position < reader.Length) {
				uint offset = reader.Position;
				if (!reader.TryReadCompressedUInt32(out uint len)) {
					if (offset == reader.Position)
						reader.Position++;
					continue;
				}
				if (len == 0 || (ulong)reader.Position + len > reader.Length)
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
			if (isReadOnly)
				throw new ModuleWriterException("Trying to modify #Blob when it's read-only");
			if (data is null || data.Length == 0)
				return 0;

			if (cachedDict.TryGetValue(data, out uint offset))
				return offset;
			return AddToCache(data);
		}

		/// <summary>
		/// Adds data to the #Blob heap, but does not re-use an existing position
		/// </summary>
		/// <param name="data">The data</param>
		/// <returns>The offset of the data in the #Blob heap</returns>
		public uint Create(byte[] data) {
			if (isReadOnly)
				throw new ModuleWriterException("Trying to modify #Blob when it's read-only");
			return AddToCache(data ?? Array2.Empty<byte>());
		}

		uint AddToCache(byte[] data) {
			uint offset;
			cached.Add(data);
			cachedDict[data] = offset = nextOffset;
			nextOffset += (uint)GetRawDataSize(data);
			return offset;
		}

		/// <inheritdoc/>
		public override uint GetRawLength() => nextOffset;

		/// <inheritdoc/>
		protected override void WriteToImpl(DataWriter writer) {
			if (!(originalData is null))
				writer.WriteBytes(originalData);
			else
				writer.WriteByte(0);

			uint offset = !(originalData is null) ? (uint)originalData.Length : 1;
			foreach (var data in cached) {
				int rawLen = GetRawDataSize(data);
				if (!(userRawData is null) && userRawData.TryGetValue(offset, out var rawData)) {
					if (rawData.Length != rawLen)
						throw new InvalidOperationException("Invalid length of raw data");
					writer.WriteBytes(rawData);
				}
				else {
					writer.WriteCompressedUInt32((uint)data.Length);
					writer.WriteBytes(data);
				}
				offset += (uint)rawLen;
			}
		}

		/// <inheritdoc/>
		public int GetRawDataSize(byte[] data) => DataWriter.GetCompressedUInt32Length((uint)data.Length) + data.Length;

		/// <inheritdoc/>
		public void SetRawData(uint offset, byte[] rawData) {
			if (userRawData is null)
				userRawData = new Dictionary<uint, byte[]>();
			userRawData[offset] = rawData ?? throw new ArgumentNullException(nameof(rawData));
		}

		/// <inheritdoc/>
		public IEnumerable<KeyValuePair<uint, byte[]>> GetAllRawData() {
			var memStream = new MemoryStream();
			var writer = new DataWriter(memStream);
			uint offset = !(originalData is null) ? (uint)originalData.Length : 1;
			foreach (var data in cached) {
				memStream.Position = 0;
				memStream.SetLength(0);
				writer.WriteCompressedUInt32((uint)data.Length);
				writer.WriteBytes(data);
				yield return new KeyValuePair<uint, byte[]>(offset, memStream.ToArray());
				offset += (uint)memStream.Length;
			}
		}
	}
}
