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
	/// #Blob heap
	/// </summary>
	sealed class BlobHeap : HeapBase {
		Dictionary<byte[], uint> cachedDict = new Dictionary<byte[], uint>(ByteArrayEqualityComparer.Instance);
		List<byte[]> cached = new List<byte[]>();
		uint nextOffset = 1;
		byte[] originalData;

		/// <inheritdoc/>
		public override string Name {
			get { return "#Blob"; }
		}

		/// <summary>
		/// Populates blobs from an existing <see cref="BlobStream"/> (eg. to preserve
		/// blob offsets)
		/// </summary>
		/// <param name="blobStream">The #Blob stream with the original content</param>
		public void Populate(BlobStream blobStream) {
			if (isReadOnly)
				throw new ModuleWriterException("Trying to modify #Blob when it's read-only");
			if (originalData != null)
				throw new InvalidOperationException("Can't call method twice");
			if (nextOffset != 1)
				throw new InvalidOperationException("Add() has already been called");
			if (blobStream == null || blobStream.ImageStream.Length == 0)
				return;

			using (var reader = blobStream.ImageStream.Clone()) {
				originalData = reader.ReadAllBytes();
				nextOffset = (uint)originalData.Length;
				Populate(reader);
			}
		}

		void Populate(IImageStream reader) {
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

				var data = reader.ReadBytes((int)len);
				if (!cachedDict.ContainsKey(data))
					cachedDict[data] = offset;
			}
		}

		/// <summary>
		/// Adds data to the #Blob heap
		/// </summary>
		/// <param name="data">The data</param>
		/// <returns>The offset of the data in the #Blob heap</returns>
		public uint Add(byte[] data) {
			if (isReadOnly)
				throw new ModuleWriterException("Trying to modify #Blob when it's read-only");
			if (data == null || data.Length == 0)
				return 0;

			uint offset;
			if (cachedDict.TryGetValue(data, out offset))
				return offset;
			return AddToCache(data);
		}

		/// <summary>
		/// Adds data to the #Blob heap, but does not re-use an existing position
		/// </summary>
		/// <param name="data">The data</param>
		/// <returns>The offset of the data in the #Blob heap</returns>
		public uint Create(byte[] data) {
			if (isReadOnly)
				throw new ModuleWriterException("Trying to modify #Blob when it's read-only");
			return AddToCache(data ?? new byte[0]);
		}

		uint AddToCache(byte[] data) {
			uint offset;
			cached.Add(data);
			cachedDict[data] = offset = nextOffset;
			nextOffset += (uint)(Utils.GetCompressedUInt32Length((uint)data.Length) + data.Length);
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
			foreach (var data in cached) {
				writer.WriteCompressedUInt32((uint)data.Length);
				writer.Write(data);
			}
		}
	}
}
