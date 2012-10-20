using System;
using System.Collections.Generic;
using System.IO;
using dot10.IO;
using dot10.DotNet.MD;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// #US heap
	/// </summary>
	sealed class USHeap : HeapBase {
		Dictionary<string, uint> cachedDict = new Dictionary<string, uint>(StringComparer.Ordinal);
		List<string> cached = new List<string>();
		uint nextOffset = 1;
		byte[] originalData;

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
			if (usStream == null || usStream.ImageStream.Length == 0)
				return;

			using (var reader = usStream.ImageStream.Create(0)) {
				originalData = reader.ReadBytes((int)reader.Length);
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
					chars[i] = reader.ReadChar();
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
			if (string.IsNullOrEmpty(s))
				return 0;

			uint offset;
			if (cachedDict.TryGetValue(s, out offset))
				return offset;

			cached.Add(s);
			cachedDict[s] = offset = nextOffset;
			nextOffset += (uint)(Utils.GetCompressedUInt32Length((uint)s.Length) + s.Length * 2 + 1);
			return offset;
		}

		/// <inheritdoc/>
		public override uint GetLength() {
			return nextOffset;
		}

		/// <inheritdoc/>
		public override void WriteTo(BinaryWriter writer) {
			if (originalData != null)
				writer.Write(originalData);
			else
				writer.Write((byte)0);
			foreach (var s in cached) {
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
		}
	}
}
