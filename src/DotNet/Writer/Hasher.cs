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

		public static string GetChecksumName(ChecksumAlgorithm checksumAlgorithm) {
			// https://github.com/dotnet/corefx/blob/master/src/System.Reflection.Metadata/specs/PE-COFF.md#pdb-checksum-debug-directory-entry-type-19
			switch (checksumAlgorithm) {
			case ChecksumAlgorithm.SHA1:		return "SHA1";
			case ChecksumAlgorithm.SHA256:		return "SHA256";
			case ChecksumAlgorithm.SHA384:		return "SHA384";
			case ChecksumAlgorithm.SHA512:		return "SHA512";
			default: throw new ArgumentOutOfRangeException(nameof(checksumAlgorithm));
			}
		}

		public static bool TryGetChecksumAlgorithm(string checksumName, out ChecksumAlgorithm pdbChecksumAlgorithm, out int checksumSize) {
			switch (checksumName) {
			case "SHA1":
				pdbChecksumAlgorithm = ChecksumAlgorithm.SHA1;
				checksumSize = 20;
				return true;

			case "SHA256":
				pdbChecksumAlgorithm = ChecksumAlgorithm.SHA256;
				checksumSize = 32;
				return true;

			case "SHA384":
				pdbChecksumAlgorithm = ChecksumAlgorithm.SHA384;
				checksumSize = 48;
				return true;

			case "SHA512":
				pdbChecksumAlgorithm = ChecksumAlgorithm.SHA512;
				checksumSize = 64;
				return true;

			default:
				pdbChecksumAlgorithm = 0;
				checksumSize = -1;
				return false;
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
