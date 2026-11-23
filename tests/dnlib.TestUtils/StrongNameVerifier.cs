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
		public static bool IsValid(byte[] image) {
			using (var peReader = new PEReader(ImmutableArray.Create(image))) {
				var corHeader = peReader.PEHeaders.CorHeader;
				if (corHeader is null || corHeader.StrongNameSignatureDirectory.Size == 0)
					return false;
				if (!peReader.PEHeaders.TryGetDirectoryOffset(corHeader.StrongNameSignatureDirectory, out int signatureOffset))
					return false;
				int signatureSize = corHeader.StrongNameSignatureDirectory.Size;

				var metadata = peReader.GetMetadataReader();
				var publicKey = metadata.GetBlobBytes(metadata.GetAssemblyDefinition().PublicKey);
				if (publicKey.Length == 0)
					return false;

				var hash = HashFile(image, peReader.PEHeaders, signatureOffset, signatureSize);

				// The signature is stored with the least significant byte first, RSA wants it the other way
				var signature = new byte[signatureSize];
				Array.Copy(image, signatureOffset, signature, 0, signatureSize);
				Array.Reverse(signature);

				using (var rsa = RSA.Create()) {
					rsa.ImportParameters(ParsePublicKey(publicKey));
					return rsa.VerifyHash(hash, signature, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
				}
			}
		}

		// What's hashed: the headers (DOS header, PE headers, section headers) and the data of every section,
		// without the signature itself. Two header
		// fields that are changed after signing count as zero: the PE checksum and the location of the
		// Authenticode signature.
		static byte[] HashFile(byte[] image, PEHeaders headers, int signatureOffset, int signatureSize) {
			var peHeader = headers.PEHeader!;
			var data = (byte[])image.Clone();

			const int checksumOffsetInOptionalHeader = 64;
			Array.Clear(data, headers.PEHeaderStartOffset + checksumOffsetInOptionalHeader, 4);
			// The data directories are at the end of the optional header: 16 entries of 8 bytes. The
			// certificate table is entry 4.
			int optionalHeaderSize = peHeader.Magic == PEMagic.PE32Plus ? 240 : 224;
			int dataDirectoriesOffset = headers.PEHeaderStartOffset + optionalHeaderSize - 16 * 8;
			Array.Clear(data, dataDirectoriesOffset + 4 * 8, 8);

			using (var sha1 = SHA1.Create()) {
				// The headers end after the last section header. The padding that follows isn't hashed.
				const int sectionHeaderSize = 40;
				int headersEnd = headers.PEHeaderStartOffset + headers.CoffHeader.SizeOfOptionalHeader + headers.SectionHeaders.Length * sectionHeaderSize;
				sha1.TransformBlock(data, 0, headersEnd, null, 0);
				foreach (var section in headers.SectionHeaders) {
					int start = section.PointerToRawData;
					int end = start + section.SizeOfRawData;
					if (signatureOffset >= start && signatureOffset < end) {
						sha1.TransformBlock(data, start, signatureOffset - start, null, 0);
						int afterSignature = signatureOffset + signatureSize;
						sha1.TransformBlock(data, afterSignature, end - afterSignature, null, 0);
					}
					else
						sha1.TransformBlock(data, start, end - start, null, 0);
				}
				sha1.TransformFinalBlock(data, 0, 0);
				return sha1.Hash!;
			}
		}

		// The public key in metadata: 3 uint32 (signature algorithm, hash algorithm, size of the rest), then a
		// Windows PUBLICKEYBLOB: BLOBHEADER (8 bytes), "RSA1", bit length, public exponent, modulus
		static RSAParameters ParsePublicKey(byte[] publicKey) {
			const int blobOffset = 12;
			const int rsaPubKeyOffset = blobOffset + 8;
			int bitLength = BitConverter.ToInt32(publicKey, rsaPubKeyOffset + 4);
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
