// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Collections.Generic;
using System.IO;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// #GUID heap
	/// </summary>
	public sealed class GuidHeap : HeapBase, IOffsetHeap<Guid> {
		readonly List<Guid> guids = new List<Guid>();
		Dictionary<uint, byte[]> userRawData;

		/// <inheritdoc/>
		public override string Name {
			get { return "#GUID"; }
		}

		/// <summary>
		/// Adds a guid to the #GUID heap
		/// </summary>
		/// <param name="guid">The guid</param>
		/// <returns>The index of the guid in the #GUID heap</returns>
		public uint Add(Guid? guid) {
			if (isReadOnly)
				throw new ModuleWriterException("Trying to modify #GUID when it's read-only");
			if (guid == null)
				return 0;

			// The number of GUIDs will almost always be 1 so there's no need for a dictionary.
			// The only table that contains GUIDs is the Module table, and it has three GUID
			// columns. Only one of them (Mvid) is normally set and the others are null.
			int index = guids.IndexOf(guid.Value);
			if (index >= 0)
				return (uint)index + 1;

			guids.Add(guid.Value);
			return (uint)guids.Count;
		}

		/// <inheritdoc/>
		public override uint GetRawLength() {
			return (uint)guids.Count * 16;
		}

		/// <inheritdoc/>
		protected override void WriteToImpl(BinaryWriter writer) {
			uint offset = 0;
			foreach (var guid in guids) {
				byte[] rawData;
				if (userRawData == null || !userRawData.TryGetValue(offset, out rawData))
					rawData = guid.ToByteArray();
				writer.Write(rawData);
				offset += 16;
			}
		}

		/// <inheritdoc/>
		public int GetRawDataSize(Guid data) {
			return 16;
		}

		/// <inheritdoc/>
		public void SetRawData(uint offset, byte[] rawData) {
			if (rawData == null || rawData.Length != 16)
				throw new ArgumentException("Invalid size of GUID raw data");
			if (userRawData == null)
				userRawData = new Dictionary<uint, byte[]>();
			userRawData[offset] = rawData;
		}

		/// <inheritdoc/>
		public IEnumerable<KeyValuePair<uint, byte[]>> GetAllRawData() {
			uint offset = 0;
			foreach (var guid in guids) {
				yield return new KeyValuePair<uint, byte[]>(offset, guid.ToByteArray());
				offset += 16;
			}
		}
	}
}
