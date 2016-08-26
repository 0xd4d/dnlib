// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Collections.Generic;
using System.IO;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Stores all method body chunks
	/// </summary>
	public sealed class MethodBodyChunks : IChunk {
		const uint FAT_BODY_ALIGNMENT = 4;
		Dictionary<MethodBody, MethodBody> tinyMethodsDict;
		Dictionary<MethodBody, MethodBody> fatMethodsDict;
		readonly List<MethodBody> tinyMethods;
		readonly List<MethodBody> fatMethods;
		readonly bool shareBodies;
		FileOffset offset;
		RVA rva;
		uint length;
		bool setOffsetCalled;
		readonly bool alignFatBodies;
		uint savedBytes;

		/// <inheritdoc/>
		public FileOffset FileOffset {
			get { return offset; }
		}

		/// <inheritdoc/>
		public RVA RVA {
			get { return rva; }
		}

		/// <summary>
		/// Gets the number of bytes saved by re-using method bodies
		/// </summary>
		public uint SavedBytes {
			get { return savedBytes; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="shareBodies"><c>true</c> if bodies can be shared</param>
		public MethodBodyChunks(bool shareBodies) {
			this.shareBodies = shareBodies;
			this.alignFatBodies = true;
			if (shareBodies) {
				tinyMethodsDict = new Dictionary<MethodBody, MethodBody>();
				fatMethodsDict = new Dictionary<MethodBody, MethodBody>();
			}
			tinyMethods = new List<MethodBody>();
			fatMethods = new List<MethodBody>();
		}

		/// <summary>
		/// Adds a <see cref="MethodBody"/> and returns the one that has been cached
		/// </summary>
		/// <param name="methodBody">The method body</param>
		/// <returns>The cached method body</returns>
		public MethodBody Add(MethodBody methodBody) {
			if (setOffsetCalled)
				throw new InvalidOperationException("SetOffset() has already been called");
			if (shareBodies) {
				var dict = methodBody.IsFat ? fatMethodsDict : tinyMethodsDict;
				MethodBody cached;
				if (dict.TryGetValue(methodBody, out cached)) {
					savedBytes += (uint)methodBody.GetSizeOfMethodBody();
					return cached;
				}
				dict[methodBody] = methodBody;
			}
			var list = methodBody.IsFat ? fatMethods : tinyMethods;
			list.Add(methodBody);
			return methodBody;
		}

		/// <summary>
		/// Removes the specified method body from this chunk
		/// </summary>
		/// <param name="methodBody">The method body</param>
		/// <returns><c>true</c> if the method body is removed</returns>
		public bool Remove(MethodBody methodBody) {
			if (setOffsetCalled)
				throw new InvalidOperationException("SetOffset() has already been called");
			var list = methodBody.IsFat ? fatMethods : tinyMethods;
			return list.Remove(methodBody);
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			setOffsetCalled = true;
			this.offset = offset;
			this.rva = rva;

			tinyMethodsDict = null;
			fatMethodsDict = null;

			var rva2 = rva;
			foreach (var mb in tinyMethods) {
				mb.SetOffset(offset, rva2);
				uint len = mb.GetFileLength();
				rva2 += len;
				offset += len;
			}

			foreach (var mb in fatMethods) {
				if (alignFatBodies) {
					uint padding = (uint)rva2.AlignUp(FAT_BODY_ALIGNMENT) - (uint)rva2;
					rva2 += padding;
					offset += padding;
				}
				mb.SetOffset(offset, rva2);
				uint len = mb.GetFileLength();
				rva2 += len;
				offset += len;
			}

			length = (uint)rva2 - (uint)rva;
		}

		/// <inheritdoc/>
		public uint GetFileLength() {
			return length;
		}

		/// <inheritdoc/>
		public uint GetVirtualSize() {
			return GetFileLength();
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			var rva2 = rva;
			foreach (var mb in tinyMethods) {
				mb.VerifyWriteTo(writer);
				rva2 += mb.GetFileLength();
			}

			foreach (var mb in fatMethods) {
				if (alignFatBodies) {
					int padding = (int)rva2.AlignUp(FAT_BODY_ALIGNMENT) - (int)rva2;
					writer.WriteZeros(padding);
					rva2 += (uint)padding;
				}
				mb.VerifyWriteTo(writer);
				rva2 += mb.GetFileLength();
			}
		}
	}
}
