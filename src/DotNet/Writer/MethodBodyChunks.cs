using System;
using System.Collections.Generic;
using System.IO;
using dot10.IO;
using dot10.PE;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// Stores all method body chunks
	/// </summary>
	class MethodBodyChunks : IChunk {
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
		bool alignFatBodies;

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
				if (dict.TryGetValue(methodBody, out cached))
					return cached;
				dict[methodBody] = methodBody;
			}
			var list = methodBody.IsFat ? fatMethods : tinyMethods;
			list.Add(methodBody);
			return methodBody;
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
				uint len = mb.GetLength();
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
				uint len = mb.GetLength();
				rva2 += len;
				offset += len;
			}

			length = (uint)rva2 - (uint)rva;
		}

		/// <inheritdoc/>
		public uint GetLength() {
			return length;
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			var rva2 = rva;
			foreach (var mb in tinyMethods) {
				mb.VerifyWriteTo(writer);
				rva2 += mb.GetLength();
			}

			foreach (var mb in fatMethods) {
				if (alignFatBodies)
					writer.WriteZeros((int)rva2.AlignUp(FAT_BODY_ALIGNMENT) - (int)rva2);
				mb.VerifyWriteTo(writer);
				rva2 += mb.GetLength();
			}
		}
	}
}
