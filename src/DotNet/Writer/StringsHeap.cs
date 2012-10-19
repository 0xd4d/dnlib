using System;
using System.Collections.Generic;
using System.IO;
using dot10.DotNet.MD;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// #Strings heap
	/// </summary>
	sealed class StringsHeap : HeapBase {
		Dictionary<UTF8String, uint> cachedDict = new Dictionary<UTF8String, uint>(UTF8StringEqualityComparer.Instance);
		List<UTF8String> cached = new List<UTF8String>();
		uint nextOffset = 1;

		/// <inheritdoc/>
		public override string Name {
			get { return "#Strings"; }
		}

		/// <summary>
		/// Adds a string to the #Strings heap
		/// </summary>
		/// <param name="s">The string</param>
		/// <returns>The offset of the string in the #Strings heap</returns>
		public uint Add(UTF8String s) {
			if (UTF8String.IsNullOrEmpty(s))
				return 0;

			uint offset;
			if (cachedDict.TryGetValue(s, out offset))
				return offset;

			if (Array.IndexOf(s.Data, 0) >= 0)
				throw new ArgumentException("Strings in the #Strings heap can't contain 00h bytes");

			cached.Add(s);
			cachedDict[s] = offset = nextOffset;
			nextOffset += (uint)s.Data.Length + 1;
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
				writer.Write(s.Data);
				writer.Write((byte)0);
			}
		}
	}
}
