// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Collections.Generic;
using System.IO;
using dnlib.IO;
using dnlib.DotNet.MD;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// #Strings heap
	/// </summary>
	public sealed class StringsHeap : HeapBase, IOffsetHeap<UTF8String> {
		readonly Dictionary<UTF8String, uint> cachedDict = new Dictionary<UTF8String, uint>(UTF8StringEqualityComparer.Instance);
		readonly List<UTF8String> cached = new List<UTF8String>();
		uint nextOffset = 1;
		byte[] originalData;
		Dictionary<uint, byte[]> userRawData;

		/// <inheritdoc/>
		public override string Name {
			get { return "#Strings"; }
		}

		/// <summary>
		/// Populates strings from an existing <see cref="StringsStream"/> (eg. to preserve
		/// string offsets)
		/// </summary>
		/// <param name="stringsStream">The #Strings stream with the original content</param>
		public void Populate(StringsStream stringsStream) {
			if (isReadOnly)
				throw new ModuleWriterException("Trying to modify #Strings when it's read-only");
			if (originalData != null)
				throw new InvalidOperationException("Can't call method twice");
			if (nextOffset != 1)
				throw new InvalidOperationException("Add() has already been called");
			if (stringsStream == null || stringsStream.ImageStreamLength == 0)
				return;

			using (var reader = stringsStream.GetClonedImageStream()) {
				originalData = reader.ReadAllBytes();
				nextOffset = (uint)originalData.Length;
				Populate(reader);
			}
		}

		void Populate(IImageStream reader) {
			reader.Position = 1;
			while (reader.Position < reader.Length) {
				uint offset = (uint)reader.Position;
				var bytes = reader.ReadBytesUntilByte(0);
				if (bytes == null)
					break;

				reader.ReadByte();	// terminating zero
				if (bytes.Length == 0)
					continue;

				var s = new UTF8String(bytes);
				if (!cachedDict.ContainsKey(s))
					cachedDict[s] = offset;
			}
		}

		/// <summary>
		/// Adds a string to the #Strings heap
		/// </summary>
		/// <param name="s">The string</param>
		/// <returns>The offset of the string in the #Strings heap</returns>
		public uint Add(UTF8String s) {
			if (isReadOnly)
				throw new ModuleWriterException("Trying to modify #Strings when it's read-only");
			if (UTF8String.IsNullOrEmpty(s))
				return 0;

			uint offset;
			if (cachedDict.TryGetValue(s, out offset))
				return offset;

			return AddToCache(s);
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
			return AddToCache(s);
		}

		uint AddToCache(UTF8String s) {
			if (Array.IndexOf(s.Data, (byte)0) >= 0)
				throw new ArgumentException("Strings in the #Strings heap can't contain 00h bytes");

			uint offset;
			cached.Add(s);
			cachedDict[s] = offset = nextOffset;
			nextOffset += (uint)s.Data.Length + 1;
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
				byte[] rawData;
				if (userRawData != null && userRawData.TryGetValue(offset, out rawData)) {
					if (rawData.Length != s.Data.Length + 1)
						throw new InvalidOperationException("Invalid length of raw data");
					writer.Write(rawData);
				}
				else {
					writer.Write(s.Data);
					writer.Write((byte)0);
				}
				offset += (uint)s.Data.Length + 1;
			}
		}

		/// <inheritdoc/>
		public int GetRawDataSize(UTF8String data) {
			return data.Data.Length + 1;
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
			uint offset = originalData != null ? (uint)originalData.Length : 1;
			foreach (var s in cached) {
				var rawData = new byte[s.Data.Length + 1];
				Array.Copy(s.Data, rawData, s.Data.Length);
				yield return new KeyValuePair<uint, byte[]>(offset, rawData);
				offset += (uint)rawData.Length;
			}
		}
	}
}
