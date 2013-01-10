/*
    Copyright (C) 2012-2013 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

﻿using System;
using System.Collections.Generic;
using System.IO;
using dnlib.IO;
using dnlib.DotNet.MD;

namespace dnlib.DotNet.Writer {
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

			using (var reader = usStream.ImageStream.Clone()) {
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
			nextOffset += (uint)(Utils.GetCompressedUInt32Length((uint)s.Length * 2 + 1) + s.Length * 2 + 1);
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
