// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

namespace dnlib.TestUtils {
	/// <summary>
	/// Checks the strong name signature of an assembly without dnlib: the PE file is read with
	/// System.Reflection.Metadata and the signature is verified with <see cref="RSA"/>.
	/// Only signatures that use SHA-1 are supported (the classic strong name).
	/// </summary>
	public static class StrongNameVerifier {
		/// <summary>
		/// Returns <c>true</c> if the assembly has a strong name signature and the signature is valid for the
		/// content of the file and the public key in the assembly's metadata
		/// </summary>
		public static bool IsValid(byte[] image) => IsValid(image, signaturePublicKey: null);

		/// <summary>
		/// Returns <c>true</c> if the assembly has a strong name signature and the signature is valid for the
		/// content of the file and a public key
		/// </summary>
		/// <param name="image">The assembly</param>
		/// <param name="signaturePublicKey">The public key (in the format that's stored in metadata) of the key
		/// that signed the file, or <c>null</c> if it's the public key in the assembly's metadata. It's another
		/// key if the assembly has an AssemblySignatureKeyAttribute ("key migration").</param>
		public static bool IsValid(byte[] image, byte[]? signaturePublicKey) {
			using (var peReader = new PEReader(ImmutableArray.Create(image))) {
				var corHeader = peReader.PEHeaders.CorHeader;
				if (corHeader is null || corHeader.StrongNameSignatureDirectory.Size == 0)
					return false;
				if (!peReader.PEHeaders.TryGetDirectoryOffset(corHeader.StrongNameSignatureDirectory, out int signatureOffset))
					return false;
				int signatureSize = corHeader.StrongNameSignatureDirectory.Size;

				var metadata = peReader.GetMetadataReader();
				var publicKey = signaturePublicKey ?? metadata.GetBlobBytes(metadata.GetAssemblyDefinition().PublicKey);
				if (publicKey.Length == 0)
					return false;

				var hashAlgorithm = GetHashAlgorithm(publicKey);
				var hash = HashFile(image, peReader.PEHeaders, signatureOffset, signatureSize, hashAlgorithm);

				// The signature is stored with the least significant byte first, RSA wants it the other way
				var signature = new byte[signatureSize];
				Array.Copy(image, signatureOffset, signature, 0, signatureSize);
				Array.Reverse(signature);

				using (var rsa = RSA.Create()) {
					rsa.ImportParameters(ParsePublicKey(publicKey));
					return rsa.VerifyHash(hash, signature, hashAlgorithm, RSASignaturePadding.Pkcs1);
				}
			}
		}

		// What's hashed: the headers (DOS header, PE headers, section headers) and the data of every section,
		// without the signature itself. Two header
		// fields that are changed after signing count as zero: the PE checksum and the location of the
		// Authenticode signature.
		static byte[] HashFile(byte[] image, PEHeaders headers, int signatureOffset, int signatureSize, HashAlgorithmName hashAlgorithm) {
			var peHeader = headers.PEHeader!;
			var data = (byte[])image.Clone();

			const int checksumOffsetInOptionalHeader = 64;
			Array.Clear(data, headers.PEHeaderStartOffset + checksumOffsetInOptionalHeader, 4);
			// The data directories are at the end of the optional header: 16 entries of 8 bytes. The
			// certificate table is entry 4.
			int optionalHeaderSize = peHeader.Magic == PEMagic.PE32Plus ? 240 : 224;
			int dataDirectoriesOffset = headers.PEHeaderStartOffset + optionalHeaderSize - 16 * 8;
			Array.Clear(data, dataDirectoriesOffset + 4 * 8, 8);

			using (var hasher = CreateHashAlgorithm(hashAlgorithm)) {
				// The headers end after the last section header. The padding that follows isn't hashed.
				const int sectionHeaderSize = 40;
				int headersEnd = headers.PEHeaderStartOffset + headers.CoffHeader.SizeOfOptionalHeader + headers.SectionHeaders.Length * sectionHeaderSize;
				hasher.TransformBlock(data, 0, headersEnd, null, 0);
				foreach (var section in headers.SectionHeaders) {
					int start = section.PointerToRawData;
					int end = start + section.SizeOfRawData;
					if (signatureOffset >= start && signatureOffset < end) {
						hasher.TransformBlock(data, start, signatureOffset - start, null, 0);
						int afterSignature = signatureOffset + signatureSize;
						hasher.TransformBlock(data, afterSignature, end - afterSignature, null, 0);
					}
					else
						hasher.TransformBlock(data, start, end - start, null, 0);
				}
				hasher.TransformFinalBlock(data, 0, 0);
				return hasher.Hash!;
			}
		}

		// The second uint32 of the public key is the Windows ID of the hash algorithm. A key made by 'sn -k' has
		// SHA-1, the keys of "enhanced strong naming" have SHA-256 or more.
		static HashAlgorithmName GetHashAlgorithm(byte[] publicKey) {
			switch (LittleEndian.ToUInt32(publicKey, 4)) {
			case 0x800C: return HashAlgorithmName.SHA256;
			case 0x800D: return HashAlgorithmName.SHA384;
			case 0x800E: return HashAlgorithmName.SHA512;
			// 0x8004, and 0 means the default
			default: return HashAlgorithmName.SHA1;
			}
		}

		static HashAlgorithm CreateHashAlgorithm(HashAlgorithmName name) {
			if (name == HashAlgorithmName.SHA256)
				return SHA256.Create();
			if (name == HashAlgorithmName.SHA384)
				return SHA384.Create();
			if (name == HashAlgorithmName.SHA512)
				return SHA512.Create();
			return SHA1.Create();
		}

		/// <summary>
		/// Returns <c>true</c> if a counter signature (the second argument of an AssemblySignatureKeyAttribute)
		/// is the signature of the signature key's public key, made with the identity key
		/// </summary>
		/// <param name="identityPublicKey">The public key in the assembly's metadata</param>
		/// <param name="signaturePublicKey">The public key in the attribute: the key that signed the file</param>
		/// <param name="counterSignature">The counter signature in the attribute</param>
		public static bool IsValidCounterSignature(byte[] identityPublicKey, byte[] signaturePublicKey, byte[] counterSignature) {
			var hashAlgorithm = GetHashAlgorithm(identityPublicKey);
			byte[] hash;
			using (var algorithm = CreateHashAlgorithm(hashAlgorithm))
				hash = algorithm.ComputeHash(signaturePublicKey);
			var signature = (byte[])counterSignature.Clone();
			Array.Reverse(signature);
			using (var rsa = RSA.Create()) {
				rsa.ImportParameters(ParsePublicKey(identityPublicKey));
				return rsa.VerifyHash(hash, signature, hashAlgorithm, RSASignaturePadding.Pkcs1);
			}
		}

		// The public key in metadata: 3 uint32 (signature algorithm, hash algorithm, size of the rest), then a
		// Windows PUBLICKEYBLOB: BLOBHEADER (8 bytes), "RSA1", bit length, public exponent, modulus
		static RSAParameters ParsePublicKey(byte[] publicKey) {
			const int blobOffset = 12;
			const int rsaPubKeyOffset = blobOffset + 8;
			int bitLength = LittleEndian.ToInt32(publicKey, rsaPubKeyOffset + 4);
			var exponent = new byte[4];
			Array.Copy(publicKey, rsaPubKeyOffset + 8, exponent, 0, 4);
			var modulus = new byte[bitLength / 8];
			Array.Copy(publicKey, rsaPubKeyOffset + 12, modulus, 0, modulus.Length);
			// Windows stores the numbers with the least significant byte first, RSAParameters the other way
			Array.Reverse(exponent);
			Array.Reverse(modulus);
			return new RSAParameters { Exponent = TrimLeadingZeros(exponent), Modulus = modulus };
		}

		static byte[] TrimLeadingZeros(byte[] value) {
			int start = 0;
			while (start < value.Length - 1 && value[start] == 0)
				start++;
			var result = new byte[value.Length - start];
			Array.Copy(value, start, result, 0, result.Length);
			return result;
		}
	}
}
