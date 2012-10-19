using System;
using System.Collections.Generic;
using System.IO;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// #US heap
	/// </summary>
	sealed class USHeap : HeapBase {
		Dictionary<string, uint> cachedDict = new Dictionary<string, uint>(StringComparer.Ordinal);
		List<string> cached = new List<string>();
		uint nextOffset = 1;

		/// <inheritdoc/>
		public override string Name {
			get { return "#US"; }
		}

		/// <summary>
		/// Adds a string to the #US heap
		/// </summary>
		/// <param name="s">The string</param>
		/// <returns>The offset of the string in the #US heap</returns>
		public uint Add(string s) {
			if (s == null || s.Length == 0)
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
