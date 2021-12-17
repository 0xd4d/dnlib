// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using dnlib.IO;
using dnlib.DotNet.MD;
using System.Diagnostics;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// #Strings heap
	/// </summary>
	public sealed class StringsHeap : HeapBase, IOffsetHeap<UTF8String> {
		readonly Dictionary<UTF8String, uint> cachedDict = new Dictionary<UTF8String, uint>(UTF8StringEqualityComparer.Instance);
		readonly List<UTF8String> cached = new List<UTF8String>();
		uint nextOffset = 1;
		byte[]? originalData;
		Dictionary<uint, byte[]>? userRawData;
		readonly Dictionary<UTF8String, StringsOffsetInfo> toStringsOffsetInfo = new Dictionary<UTF8String, StringsOffsetInfo>(UTF8StringEqualityComparer.Instance);
		readonly Dictionary<uint, StringsOffsetInfo> offsetIdToInfo = new Dictionary<uint, StringsOffsetInfo>();
		readonly List<StringsOffsetInfo> stringsOffsetInfos = new List<StringsOffsetInfo>();
		const uint STRINGS_ID_FLAG = 0x80000000;
		uint stringsId = STRINGS_ID_FLAG | 0;

		sealed class StringsOffsetInfo {
			public StringsOffsetInfo(UTF8String value, uint stringsId) {
				Value = value;
				StringsId = stringsId;
				Debug.Assert((stringsId & STRINGS_ID_FLAG) != 0);
			}
			public readonly UTF8String Value;
			public readonly uint StringsId;
			public uint StringsOffset;
			public override string ToString() => $"{StringsId:X8} {StringsOffset:X4} {Value.String}";
		}

		/// <inheritdoc/>
		public override string Name => "#Strings";

		/// <summary>
		/// Populates strings from an existing <see cref="StringsStream"/> (eg. to preserve
		/// string offsets)
		/// </summary>
		/// <param name="stringsStream">The #Strings stream with the original content</param>
		public void Populate(StringsStream stringsStream) {
			if (isReadOnly)
				throw new ModuleWriterException("Trying to modify #Strings when it's read-only");
			if (originalData is not null)
				throw new InvalidOperationException("Can't call method twice");
			if (nextOffset != 1)
				throw new InvalidOperationException("Add() has already been called");
			if (stringsStream is null || stringsStream.StreamLength == 0)
				return;

			var reader = stringsStream.CreateReader();
			originalData = reader.ToArray();
			nextOffset = (uint)originalData.Length;
			Populate(ref reader);
		}

		void Populate(ref DataReader reader) {
			reader.Position = 1;
			while (reader.Position < reader.Length) {
				uint offset = (uint)reader.Position;
				var bytes = reader.TryReadBytesUntil(0);
				if (bytes is null)
					break;

				reader.ReadByte();	// terminating zero
				if (bytes.Length == 0)
					continue;

				var s = new UTF8String(bytes);
				if (!cachedDict.ContainsKey(s))
					cachedDict[s] = offset;
			}
		}

		internal void AddOptimizedStringsAndSetReadOnly() {
			if (isReadOnly)
				throw new ModuleWriterException("Trying to modify #Strings when it's read-only");
			SetReadOnly();

			stringsOffsetInfos.Sort(Comparison_StringsOffsetInfoSorter);

			StringsOffsetInfo? prevInfo = null;
			foreach (var info in stringsOffsetInfos) {
				if (prevInfo is not null && EndsWith(prevInfo.Value, info.Value))
					info.StringsOffset = prevInfo.StringsOffset + (uint)(prevInfo.Value.Data.Length - info.Value.Data.Length);
				else
					info.StringsOffset = AddToCache(info.Value);
				prevInfo = info;
			}
		}

		static bool EndsWith(UTF8String s, UTF8String value) {
			var d = s.Data;
			var vd = value.Data;
			int i = d.Length - vd.Length;
			if (i < 0)
				return false;
			for (int vi = 0; vi < vd.Length; vi++) {
				if (d[i] != vd[vi])
					return false;
				i++;
			}
			return true;
		}

		static readonly Comparison<StringsOffsetInfo> Comparison_StringsOffsetInfoSorter = StringsOffsetInfoSorter;
		static int StringsOffsetInfoSorter(StringsOffsetInfo a, StringsOffsetInfo b) {
			var da = a.Value.Data;
			var db = b.Value.Data;
			int ai = da.Length - 1;
			int bi = db.Length - 1;
			int len = Math.Min(da.Length, db.Length);
			while (len > 0) {
				int c = da[ai] - db[bi];
				if (c != 0)
					return c;
				ai--;
				bi--;
				len--;
			}
			return db.Length - da.Length;
		}

		/// <summary>
		/// Adds a string to the #Strings heap. The returned value is not necessarily an offset in
		/// the #Strings heap. Call <see cref="GetOffset(uint)"/> to get the offset.
		/// </summary>
		/// <param name="s">The string</param>
		/// <returns>The offset id. This is not a #Strings offset. Call <see cref="GetOffset(uint)"/> to get the #Strings offset</returns>
		public uint Add(UTF8String s) {
			if (isReadOnly)
				throw new ModuleWriterException("Trying to modify #Strings when it's read-only");
			if (UTF8String.IsNullOrEmpty(s))
				return 0;

			if (toStringsOffsetInfo.TryGetValue(s, out var info))
				return info.StringsId;
			if (cachedDict.TryGetValue(s, out uint offset))
				return offset;

			if (Array.IndexOf(s.Data, (byte)0) >= 0)
				throw new ArgumentException("Strings in the #Strings heap can't contain NUL bytes");
			info = new StringsOffsetInfo(s, stringsId++);
			Debug.Assert(!toStringsOffsetInfo.ContainsKey(s));
			Debug.Assert(!offsetIdToInfo.ContainsKey(info.StringsId));
			toStringsOffsetInfo[s] = info;
			offsetIdToInfo[info.StringsId] = info;
			stringsOffsetInfos.Add(info);
			return info.StringsId;
		}

		/// <summary>
		/// Gets the offset of a string in the #Strings heap. This method can only be called after
		/// all strings have been added.
		/// </summary>
		/// <param name="offsetId">Offset id returned by <see cref="Add(UTF8String)"/></param>
		/// <returns></returns>
		public uint GetOffset(uint offsetId) {
			if (!isReadOnly)
				throw new ModuleWriterException("This method can only be called after all strings have been added and this heap is read-only");
			if ((offsetId & STRINGS_ID_FLAG) == 0)
				return offsetId;
			if (offsetIdToInfo.TryGetValue(offsetId, out var info)) {
				Debug.Assert(info.StringsOffset != 0);
				return info.StringsOffset;
			}
			throw new ArgumentOutOfRangeException(nameof(offsetId));
		}

		/// <summary>
		/// Adds a string to the #Strings heap, but does not re-use an existing position
		/// </summary>
		/// <param name="s">The string</param>
		/// <returns>The offset of the string in the #Strings heap</returns>
		public uint Create(UTF8String s) {
			if (isReadOnly)
				throw new ModuleWriterException("Trying to modify #Strings when it's read-only");
			if (UTF8String.IsNullOrEmpty(s))
				s = UTF8String.Empty;
			if (Array.IndexOf(s.Data, (byte)0) >= 0)
				throw new ArgumentException("Strings in the #Strings heap can't contain NUL bytes");
			return AddToCache(s);
		}

		uint AddToCache(UTF8String s) {
			uint offset;
			cached.Add(s);
			cachedDict[s] = offset = nextOffset;
			nextOffset += (uint)s.Data.Length + 1;
			return offset;
		}

		/// <inheritdoc/>
		public override uint GetRawLength() => nextOffset;

		/// <inheritdoc/>
		protected override void WriteToImpl(DataWriter writer) {
			if (originalData is not null)
				writer.WriteBytes(originalData);
			else
				writer.WriteByte(0);

			uint offset = originalData is not null ? (uint)originalData.Length : 1;
			foreach (var s in cached) {
				if (userRawData is not null && userRawData.TryGetValue(offset, out var rawData)) {
					if (rawData.Length != s.Data.Length + 1)
						throw new InvalidOperationException("Invalid length of raw data");
					writer.WriteBytes(rawData);
				}
				else {
					writer.WriteBytes(s.Data);
					writer.WriteByte(0);
				}
				offset += (uint)s.Data.Length + 1;
			}
		}

		/// <inheritdoc/>
		public int GetRawDataSize(UTF8String data) => data.Data.Length + 1;

		/// <inheritdoc/>
		public void SetRawData(uint offset, byte[] rawData) {
			if (userRawData is null)
				userRawData = new Dictionary<uint, byte[]>();
			userRawData[offset] = rawData ?? throw new ArgumentNullException(nameof(rawData));
		}

		/// <inheritdoc/>
		public IEnumerable<KeyValuePair<uint, byte[]>> GetAllRawData() {
			uint offset = originalData is not null ? (uint)originalData.Length : 1;
			foreach (var s in cached) {
				var rawData = new byte[s.Data.Length + 1];
				Array.Copy(s.Data, rawData, s.Data.Length);
				yield return new KeyValuePair<uint, byte[]>(offset, rawData);
				offset += (uint)rawData.Length;
			}
		}
	}
}
