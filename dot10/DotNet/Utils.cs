using System;
using System.Text;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	static class Utils {
		/// <summary>
		/// Returns an assembly name string
		/// </summary>
		/// <param name="name">Simple assembly name</param>
		/// <param name="version">Version or null</param>
		/// <param name="culture">Culture or null</param>
		/// <param name="publicKey">Public key / public key token or null</param>
		/// <returns>An assembly name string</returns>
		internal static string GetAssemblyNameString(UTF8String name, Version version, UTF8String culture, PublicKeyBase publicKey) {
			var sb = new StringBuilder();
			sb.Append(UTF8String.IsNullOrEmpty(name) ? "" : FullNameHelper.GetReflectionNamespace(name.String));

			if (version != null) {
				sb.Append(", Version=");
				sb.Append(version.ToString());
			}

			if ((object)culture != null) {
				sb.Append(", Culture=");
				sb.Append(UTF8String.IsNullOrEmpty(culture) ? "neutral" : FullNameHelper.GetReflectionName(culture.String));
			}

			sb.Append(", ");
			sb.Append(publicKey == null || publicKey is PublicKeyToken ? "PublicKeyToken=" : "PublicKey=");
			sb.Append(publicKey == null ? "null" : publicKey.ToString());

			return sb.ToString();
		}

		/// <summary>
		/// Convert a byte[] to a <see cref="string"/>
		/// </summary>
		/// <param name="bytes">All bytes</param>
		/// <param name="upper"><c>true</c> if output should be in upper case hex</param>
		/// <returns><paramref name="bytes"/> as a hex string</returns>
		internal static string ToHex(byte[] bytes, bool upper) {
			if (bytes == null)
				return "";
			var sb = new StringBuilder(bytes.Length / 2);
			foreach (var b in bytes) {
				sb.Append(ToHexChar(b >> 4, upper));
				sb.Append(ToHexChar(b & 0x0F, upper));
			}
			return sb.ToString();
		}

		static char ToHexChar(int val, bool upper) {
			if (0 <= val && val <= 9)
				return (char)(val + (int)'0');
			return (char)(val - 10 + (upper ? (int)'A' : (int)'a'));
		}

		/// <summary>
		/// Converts a hex string to a byte[]
		/// </summary>
		/// <param name="hexString">A string with an even number of hex characters</param>
		/// <returns><paramref name="hexString"/> converted to a byte[] or null
		/// if <paramref name="hexString"/> is invalid</returns>
		internal static byte[] ParseBytes(string hexString) {
			try {
				if (hexString.Length % 2 != 0)
					return null;
				var bytes = new byte[hexString.Length / 2];
				for (int i = 0; i < hexString.Length; i += 2) {
					int upper = TryParseHexChar(hexString[i]);
					int lower = TryParseHexChar(hexString[i + 1]);
					if (upper < 0 || lower < 0)
						return null;
					bytes[i / 2] = (byte)((upper << 4) | lower);
				}
				return bytes;
			}
			catch {
				return null;
			}
		}

		/// <summary>
		/// Converts a character to a hex digit
		/// </summary>
		/// <param name="c">Hex character</param>
		/// <returns><c>0x00</c>-<c>0x0F</c> if successful, <c>-1</c> if <paramref name="c"/> is not
		/// a valid hex digit</returns>
		static int TryParseHexChar(char c) {
			if ('0' <= c && c <= '9')
				return (ushort)c - (ushort)'0';
			if ('a' <= c && c <= 'f')
				return 10 + (ushort)c - (ushort)'a';
			if ('A' <= c && c <= 'F')
				return 10 + (ushort)c - (ushort)'A';
			return -1;
		}
	}
}
