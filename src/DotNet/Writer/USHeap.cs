// dnlib: See LICENSE.txt for more info

﻿using System;
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
		public override string Name {
			get { return "#US"; }
		}

		/// <summary>
		/// Populates strings from an existing <see cref="USStream"/> (eg. to preserve
		/// string tokens)
		/// </summary>
		/// <param name="usStream">The #US stream with the original content</param>
		public void Populate(USStream usStream) {
			if (originalData != null)
				throw new InvalidOperationException("Can't call method twice");
			if (nextOffset != 1)
				throw new InvalidOperationException("Add() has already been called");
			if (usStream == null || usStream.ImageStreamLength == 0)
				return;

			using (var reader = usStream.GetClonedImageStream()) {
				originalData = reader.ReadAllBytes();
				nextOffset = (uint)originalData.Length;
				Populate(reader);
			}
		}

		void Populate(IImageStream reader) {
			var chars = new char[0x200];
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

				int stringLen = (int)len / 2;
				if (stringLen > chars.Length)
					Array.Resize(ref chars, stringLen);
				for (int i = 0; i < stringLen; i++)
					chars[i] = (char)reader.ReadUInt16();
				if ((len & 1) != 0)
					reader.ReadByte();
				var s = new string(chars, 0, stringLen);

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
			if (s == null)
				s = string.Empty;

			uint offset;
			if (cachedDict.TryGetValue(s, out offset))
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
		public override uint GetRawLength() {
			return nextOffset;
		}

		/// <inheritdoc/>
		protected override void WriteToImpl(BinaryWriter writer) {
			if (originalData != null)
				writer.Write(originalData);
			else
				writer.Write((byte)0);

			uint offset = originalData != null ? (uint)originalData.Length : 1;
			foreach (var s in cached) {
				int rawLen = GetRawDataSize(s);
				byte[] rawData;
				if (userRawData != null && userRawData.TryGetValue(offset, out rawData)) {
					if (rawData.Length != rawLen)
						throw new InvalidOperationException("Invalid length of raw data");
					writer.Write(rawData);
				}
				else
					WriteString(writer, s);
				offset += (uint)rawLen;
			}
		}

		void WriteString(BinaryWriter writer, string s) {
			writer.WriteCompressedUInt32((uint)s.Length * 2 + 1);
			byte last = 0;
			for (int i = 0; i < s.Length; i++) {
				ushort c = (ushort)s[i];
				writer.Write(c);
				if (c > 0xFF || (1 <= c && c <= 8) || (0x0E <= c && c <= 0x1F) || c == 0x27 || c == 0x2D || c == 0x7F)
					last = 1;
			}
			writer.Write(last);
		}

		/// <inheritdoc/>
		public int GetRawDataSize(string data) {
			return Utils.GetCompressedUInt32Length((uint)data.Length * 2 + 1) + data.Length * 2 + 1;
		}

		/// <inheritdoc/>
		public void SetRawData(uint offset, byte[] rawData) {
			if (rawData == null)
				throw new ArgumentNullException("rawData");
			if (userRawData == null)
				userRawData = new Dictionary<uint, byte[]>();
			userRawData[offset] = rawData;
		}

		/// <inheritdoc/>
		public IEnumerable<KeyValuePair<uint, byte[]>> GetAllRawData() {
			var memStream = new MemoryStream();
			var writer = new BinaryWriter(memStream);
			uint offset = originalData != null ? (uint)originalData.Length : 1;
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
