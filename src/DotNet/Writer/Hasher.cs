// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Security.Cryptography;

namespace dnlib.DotNet.Writer {
	static class Hasher {
		static HashAlgorithm CreateHasher(ChecksumAlgorithm checksumAlgorithm) {
			switch (checksumAlgorithm) {
			case ChecksumAlgorithm.SHA1:		return SHA1.Create();
			case ChecksumAlgorithm.SHA256:		return SHA256.Create();
			case ChecksumAlgorithm.SHA384:		return SHA384.Create();
			case ChecksumAlgorithm.SHA512:		return SHA512.Create();
			default: throw new ArgumentOutOfRangeException(nameof(checksumAlgorithm));
			}
		}

		public static byte[] Hash(ChecksumAlgorithm checksumAlgorithm, Stream stream, long length) {
			var buffer = new byte[(int)Math.Min(0x2000, length)];
			using (var hasher = CreateHasher(checksumAlgorithm)) {
				while (length > 0) {
					int len = (int)Math.Min(length, buffer.Length);
					int read = stream.Read(buffer, 0, len);
					if (read == 0)
						throw new InvalidOperationException("Couldn't read all bytes");
					hasher.TransformBlock(buffer, 0, read, buffer, 0);
					length -= read;
				}
				hasher.TransformFinalBlock(Array2.Empty<byte>(), 0, 0);
				return hasher.Hash;
			}
		}
	}
}
