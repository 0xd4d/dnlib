// dnlib: See LICENSE.txt for more info

using System;

namespace dnlib.DotNet.Writer {
	static class RoslynContentIdProvider {
		public static void GetContentId(byte[] hash, out Guid guid, out uint timestamp) {
			if (hash.Length < 20)
				throw new InvalidOperationException();
			var guidBytes = new byte[16];
			Array.Copy(hash, 0, guidBytes, 0, guidBytes.Length);
			guidBytes[7] = (byte)((guidBytes[7] & 0x0F) | 0x40);
			guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);
			guid = new Guid(guidBytes);
			timestamp = 0x80000000 | (uint)((hash[19] << 24) | (hash[18] << 16) | (hash[17] << 8) | hash[16]);
		}
	}
}
