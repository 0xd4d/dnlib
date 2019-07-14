// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using dnlib.IO;
using dnlib.DotNet.MD;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// #US heap
	/// </summary>
	public sealed class USHeap : HeapBase, IOffsetHeap<string> {
		readonly Dictionary<string, uint> cachedDict = new Dictionary<string, uint>(StringComparer.Ordinal);
		readonly List<string> cached = new List<string>();
		uint nextOffset = 1;
		byte[] originalData;
		Dictionary<uint, byte[]> userRawData;

		/// <inheritdoc/>
		public override string Name => "#US";

		/// <summary>
		/// Populates strings from an existing <see cref="USStream"/> (eg. to preserve
		/// string tokens)
		/// </summary>
		/// <param name="usStream">The #US stream with the original content</param>
		public void Populate(USStream usStream) {
			if (!(originalData is null))
				throw new InvalidOperationException("Can't call method twice");
			if (nextOffset != 1)
				throw new InvalidOperationException("Add() has already been called");
			if (usStream is null || usStream.StreamLength == 0)
				return;

			var reader = usStream.CreateReader();
			originalData = reader.ToArray();
			nextOffset = (uint)originalData.Length;
			Populate(ref reader);
		}

		void Populate(ref DataReader reader) {
			reader.Position = 1;
			while (reader.Position < reader.Length) {
				uint offset = (uint)reader.Position;
				if (!reader.TryReadCompressedUInt32(out uint len)) {
					if (offset == reader.Position)
						reader.Position++;
					continue;
				}
				if (len == 0 || (ulong)reader.Position + len > reader.Length)
					continue;

				int stringLen = (int)len / 2;
				var s = reader.ReadUtf16String(stringLen);
				if ((len & 1) != 0)
					reader.ReadByte();

				if (!cachedDict.ContainsKey(s))
					cachedDict[s] = offset;
			}
		}

		/// <summary>
		/// Adds a string to the #US heap
		/// </summary>
		/// <param name="s">The string</param>
		/// <returns>The offset of the string in the #US heap</returns>
		public uint Add(string s) {
			if (isReadOnly)
				throw new ModuleWriterException("Trying to modify #US when it's read-only");
			if (s is null)
				s = string.Empty;

			if (cachedDict.TryGetValue(s, out uint offset))
				return offset;
			return AddToCache(s);
		}

		/// <summary>
		/// Adds a string to the #US heap
		/// </summary>
		/// <param name="s">The string</param>
		/// <returns>The offset of the string in the #US heap</returns>
		public uint Create(string s) {
			if (isReadOnly)
				throw new ModuleWriterException("Trying to modify #US when it's read-only");
			return AddToCache(s ?? string.Empty);
		}

		uint AddToCache(string s) {
			uint offset;
			cached.Add(s);
			cachedDict[s] = offset = nextOffset;
			nextOffset += (uint)GetRawDataSize(s);
			if (offset > 0x00FFFFFF)
				throw new ModuleWriterException("#US heap is too big");
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
			foreach (var s in cached) {
				int rawLen = GetRawDataSize(s);
				if (!(userRawData is null) && userRawData.TryGetValue(offset, out var rawData)) {
					if (rawData.Length != rawLen)
						throw new InvalidOperationException("Invalid length of raw data");
					writer.WriteBytes(rawData);
				}
				else
					WriteString(writer, s);
				offset += (uint)rawLen;
			}
		}

		void WriteString(DataWriter writer, string s) {
			writer.WriteCompressedUInt32((uint)s.Length * 2 + 1);
			byte last = 0;
			for (int i = 0; i < s.Length; i++) {
				ushort c = (ushort)s[i];
				writer.WriteUInt16(c);
				if (c > 0xFF || (1 <= c && c <= 8) || (0x0E <= c && c <= 0x1F) || c == 0x27 || c == 0x2D || c == 0x7F)
					last = 1;
			}
			writer.WriteByte(last);
		}

		/// <inheritdoc/>
		public int GetRawDataSize(string data) => DataWriter.GetCompressedUInt32Length((uint)data.Length * 2 + 1) + data.Length * 2 + 1;

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
			foreach (var s in cached) {
				memStream.Position = 0;
				memStream.SetLength(0);
				WriteString(writer, s);
				yield return new KeyValuePair<uint, byte[]>(offset, memStream.ToArray());
				offset += (uint)memStream.Length;
			}
		}
	}
}
