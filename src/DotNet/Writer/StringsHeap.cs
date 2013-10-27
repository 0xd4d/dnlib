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
	/// #Strings heap
	/// </summary>
	public sealed class StringsHeap : HeapBase {
		Dictionary<UTF8String, uint> cachedDict = new Dictionary<UTF8String, uint>(UTF8StringEqualityComparer.Instance);
		List<UTF8String> cached = new List<UTF8String>();
		uint nextOffset = 1;
		byte[] originalData;

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
			if (stringsStream == null || stringsStream.ImageStream.Length == 0)
				return;

			using (var reader = stringsStream.ImageStream.Clone()) {
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
			if (Array.IndexOf(s.Data, 0) >= 0)
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
			foreach (var s in cached) {
				writer.Write(s.Data);
				writer.Write((byte)0);
			}
		}
	}
}
