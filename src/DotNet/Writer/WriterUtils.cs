// dnlib: See LICENSE.txt for more info

using System.Text;

namespace dnlib.DotNet.Writer {
	static class WriterUtils {
		/// <summary>
		/// Encodes a string as UTF-8, but unlike <see cref="Encoding.UTF8"/>, an unpaired surrogate is encoded like
		/// any other char (3 bytes) instead of being replaced with U+FFFD. Roslyn (System.Reflection.Metadata) writes
		/// portable PDB blob strings this way.
		/// </summary>
		public static byte[] GetUTF8BytesKeepUnpairedSurrogates(string s) {
			if (!HasUnpairedSurrogate(s))
				return Encoding.UTF8.GetBytes(s);
			var bytes = new byte[s.Length * 3];
			int w = 0;
			for (int i = 0; i < s.Length; i++) {
				int c = s[i];
				if (char.IsHighSurrogate((char)c) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1])) {
					int codePoint = char.ConvertToUtf32((char)c, s[++i]);
					bytes[w++] = (byte)(0xF0 | (codePoint >> 18));
					bytes[w++] = (byte)(0x80 | ((codePoint >> 12) & 0x3F));
					bytes[w++] = (byte)(0x80 | ((codePoint >> 6) & 0x3F));
					bytes[w++] = (byte)(0x80 | (codePoint & 0x3F));
				}
				else if (c < 0x80)
					bytes[w++] = (byte)c;
				else if (c < 0x800) {
					bytes[w++] = (byte)(0xC0 | (c >> 6));
					bytes[w++] = (byte)(0x80 | (c & 0x3F));
				}
				else {
					bytes[w++] = (byte)(0xE0 | (c >> 12));
					bytes[w++] = (byte)(0x80 | ((c >> 6) & 0x3F));
					bytes[w++] = (byte)(0x80 | (c & 0x3F));
				}
			}
			var res = new byte[w];
			System.Array.Copy(bytes, res, w);
			return res;
		}

		static bool HasUnpairedSurrogate(string s) {
			for (int i = 0; i < s.Length; i++) {
				char c = s[i];
				if (char.IsHighSurrogate(c) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
					i++;
				else if (char.IsSurrogate(c))
					return true;
			}
			return false;
		}

		public static uint WriteCompressedUInt32(this DataWriter writer, IWriterError helper, uint value) {
			if (value > 0x1FFFFFFF) {
				helper.Error("UInt32 value is too big and can't be compressed");
				value = 0x1FFFFFFF;
			}
			writer.WriteCompressedUInt32(value);
			return value;
		}

		public static int WriteCompressedInt32(this DataWriter writer, IWriterError helper, int value) {
			if (value < -0x10000000) {
				helper.Error("Int32 value is too small and can't be compressed.");
				value = -0x10000000;
			}
			else if (value > 0x0FFFFFFF) {
				helper.Error("Int32 value is too big and can't be compressed.");
				value = 0x0FFFFFFF;
			}
			writer.WriteCompressedInt32(value);
			return value;
		}

		public static void Write(this DataWriter writer, IWriterError helper, UTF8String? s) {
			if (UTF8String.IsNull(s)) {
				helper.Error("UTF8String is null");
				s = UTF8String.Empty;
			}

			writer.WriteCompressedUInt32(helper, (uint)s.DataLength);
			writer.WriteBytes(s.Data);
		}
	}
}
