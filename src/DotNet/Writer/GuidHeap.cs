// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// #GUID heap
	/// </summary>
	public sealed class GuidHeap : HeapBase, IOffsetHeap<Guid> {
		readonly Dictionary<Guid, uint> guids = new Dictionary<Guid, uint>();
		Dictionary<uint, byte[]> userRawData;

		/// <inheritdoc/>
		public override string Name => "#GUID";

		/// <summary>
		/// Adds a guid to the #GUID heap
		/// </summary>
		/// <param name="guid">The guid</param>
		/// <returns>The index of the guid in the #GUID heap</returns>
		public uint Add(Guid? guid) {
			if (isReadOnly)
				throw new ModuleWriterException("Trying to modify #GUID when it's read-only");
			if (guid is null)
				return 0;

			if (guids.TryGetValue(guid.Value, out uint index))
				return index;

			index = (uint)guids.Count + 1;
			guids.Add(guid.Value, index);
			return index;
		}

		/// <inheritdoc/>
		public override uint GetRawLength() => (uint)guids.Count * 16;

		/// <inheritdoc/>
		protected override void WriteToImpl(DataWriter writer) {
			uint offset = 0;
			foreach (var kv in guids) {
				if (userRawData is null || !userRawData.TryGetValue(offset, out var rawData))
					rawData = kv.Key.ToByteArray();
				writer.WriteBytes(rawData);
				offset += 16;
			}
		}

		/// <inheritdoc/>
		public int GetRawDataSize(Guid data) => 16;

		/// <inheritdoc/>
		public void SetRawData(uint offset, byte[] rawData) {
			if (rawData is null || rawData.Length != 16)
				throw new ArgumentException("Invalid size of GUID raw data");
			if (userRawData is null)
				userRawData = new Dictionary<uint, byte[]>();
			userRawData[offset] = rawData;
		}

		/// <inheritdoc/>
		public IEnumerable<KeyValuePair<uint, byte[]>> GetAllRawData() {
			uint offset = 0;
			foreach (var kv in guids) {
				yield return new KeyValuePair<uint, byte[]>(offset, kv.Key.ToByteArray());
				offset += 16;
			}
		}
	}
}
