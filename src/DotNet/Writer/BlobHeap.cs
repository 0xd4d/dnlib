using System.Collections.Generic;
using System.IO;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// #Blob heap
	/// </summary>
	sealed class BlobHeap : HeapBase {
		Dictionary<byte[], uint> cachedDict = new Dictionary<byte[], uint>(ByteArrayEqualityComparer.Instance);
		List<byte[]> cached = new List<byte[]>();
		uint nextOffset = 1;

		/// <inheritdoc/>
		public override string Name {
			get { return "#Blob"; }
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
		public override uint GetLength() {
			return nextOffset;
		}

		/// <inheritdoc/>
		public override void WriteTo(BinaryWriter writer) {
			writer.Write((byte)0);
			foreach (var data in cached) {
				writer.WriteCompressedUInt32((uint)data.Length);
				writer.Write(data);
			}
		}
	}
}
