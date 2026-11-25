// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Security.Cryptography;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.StrongNames {
	/// <summary>
	/// <see cref="StrongNameKey"/> is a key pair (the content of a .snk file) and <see cref="StrongNamePublicKey"/>
	/// is the public key that's stored in an assembly. Both are Windows "key blobs": a small header with the
	/// kind of key, then the numbers of the RSA key. The tests read and create them without signing anything
	/// (see <see cref="StrongNameTests"/> for that).
	/// </summary>
	public class StrongNameKeyTests {
		// A new key pair as a .snk file would have it
		static byte[] CreateKeyPair() {
			using (var rsa = new RSACryptoServiceProvider(2048)) {
				var blob = rsa.ExportCspBlob(includePrivateParameters: true);
				// .NET exports a "key exchange" key (0xA400), strong name keys are signature keys (0x2400)
				blob[4] = 0x00;
				blob[5] = 0x24;
				return blob;
			}
		}

		static readonly byte[] keyPair = CreateKeyPair();

		[Fact]
		public void KeyPairIsWrittenAsTheBytesItWasReadFrom() {
			var key = new StrongNameKey(keyPair);

			Assert.Equal(keyPair, key.CreateStrongName());
			Assert.Equal(2048 / 8, key.SignatureSize);
			// A key pair has no hash algorithm (a public key has): None means SHA-1
			Assert.Equal(AssemblyHashAlgorithm.None, key.HashAlgorithm);
			Assert.Equal(AssemblyHashAlgorithm.SHA1, new StrongNamePublicKey(key.PublicKey).HashAlgorithm);
			// The numbers of the key: the two primes are half as long as the modulus
			Assert.Equal(256, key.Modulus.Length);
			Assert.Equal(128, key.Prime1.Length);
			Assert.Equal(128, key.Prime2.Length);
			Assert.Equal(128, key.Exponent1.Length);
			Assert.Equal(128, key.Exponent2.Length);
			Assert.Equal(128, key.Coefficient.Length);
			Assert.Equal(256, key.PrivateExponent.Length);
			Assert.Equal(new byte[] { 0, 1, 0, 1 }, key.PublicExponent);
		}

		[Fact]
		public void KeyPairIsReadFromAStreamAndAFile() {
			using (var directory = new TempDirectory()) {
				var file = directory.GetPath("key.snk");
				File.WriteAllBytes(file, keyPair);

				Assert.Equal(keyPair, new StrongNameKey(file).CreateStrongName());
				Assert.Equal(keyPair, new StrongNameKey(new MemoryStream(keyPair)).CreateStrongName());
				Assert.Equal(keyPair, new StrongNameKey(new BinaryReader(new MemoryStream(keyPair))).CreateStrongName());
			}
		}

		// The RSA object that signs with the key has the key's numbers
		[Fact]
		public void RsaObjectHasTheKey() {
			var key = new StrongNameKey(keyPair);

			using (var rsa = key.CreateRSA()) {
				var parameters = rsa.ExportParameters(includePrivateParameters: true);
				Assert.Equal(key.Modulus, parameters.Modulus);
				Assert.Equal(key.PrivateExponent, parameters.D);
				Assert.Equal(key.Prime1, parameters.P);
			}
		}

		[Fact]
		public void KeyWithAnotherHashAlgorithmIsANewKeyWithTheSameNumbers() {
			var key = new StrongNameKey(keyPair);

			var sha256Key = key.WithHashAlgorithm(AssemblyHashAlgorithm.SHA_256);

			Assert.NotSame(key, sha256Key);
			Assert.Equal(AssemblyHashAlgorithm.SHA_256, sha256Key.HashAlgorithm);
			Assert.Equal(AssemblyHashAlgorithm.None, key.HashAlgorithm);
			Assert.Equal(key.Modulus, sha256Key.Modulus);
			// The public key blob has the hash algorithm, so it's another public key
			Assert.NotEqual(key.PublicKey, sha256Key.PublicKey);
			Assert.Equal(AssemblyHashAlgorithm.SHA_256, new StrongNamePublicKey(sha256Key.PublicKey).HashAlgorithm);
			// The same algorithm: the same key. SHA-1 is the default, so the public key is the same too.
			Assert.Same(sha256Key, sha256Key.WithHashAlgorithm(AssemblyHashAlgorithm.SHA_256));
			Assert.Same(key, key.WithHashAlgorithm(AssemblyHashAlgorithm.None));
			Assert.Equal(key.PublicKey, key.WithHashAlgorithm(AssemblyHashAlgorithm.SHA1).PublicKey);
		}

		// The first 12 bytes of a key pair: the kind of blob (7: key pair), its version (2), two reserved bytes,
		// the algorithm (0x2400: RSA signature) and the text "RSA2"
		[Theory]
		[InlineData(0, 6, "Not a public/private key pair")]
		[InlineData(1, 3, "Invalid version")]
		[InlineData(5, 0xA4, "Not RSA sign")]
		[InlineData(8, (byte)'X', "Invalid RSA2 magic")]
		public void KeyPairWithAnInvalidHeaderIsRejected(int offset, byte value, string expectedMessage) {
			var invalid = (byte[])keyPair.Clone();
			invalid[offset] = value;

			var exception = Assert.Throws<InvalidKeyException>(() => new StrongNameKey(invalid));

			Assert.Equal(expectedMessage, exception.Message);
		}

		[Fact]
		public void PublicKeyIsCreatedFromItsNumbersAndReadAgain() {
			var key = new StrongNameKey(keyPair);
			var publicKey = new StrongNamePublicKey(key.Modulus, key.PublicExponent);

			var blob = publicKey.CreatePublicKey();

			Assert.Equal(key.PublicKey, blob);
			Assert.Equal(AssemblyHashAlgorithm.SHA1, publicKey.HashAlgorithm);
			Assert.Equal(SignatureAlgorithm.CALG_RSA_SIGN, publicKey.SignatureAlgorithm);
			// Its text form is the hex digits of the blob, as "sn -tp" prints them
			Assert.Equal(new PublicKey(blob).ToString(), publicKey.ToString(), ignoreCase: true);

			foreach (var read in new[] {
				new StrongNamePublicKey(blob),
				new StrongNamePublicKey(new PublicKey(blob)),
				new StrongNamePublicKey(new MemoryStream(blob)),
				new StrongNamePublicKey(new BinaryReader(new MemoryStream(blob))),
			}) {
				Assert.Equal(key.Modulus, read.Modulus);
				Assert.Equal(key.PublicExponent, read.PublicExponent);
				Assert.Equal(blob, read.CreatePublicKey());
			}
		}

		[Fact]
		public void PublicKeyWithAnotherHashAlgorithm() {
			var key = new StrongNameKey(keyPair);

			var publicKey = new StrongNamePublicKey(key.Modulus, key.PublicExponent, AssemblyHashAlgorithm.SHA_512);
			var withBoth = new StrongNamePublicKey(key.Modulus, key.PublicExponent, AssemblyHashAlgorithm.SHA_384, SignatureAlgorithm.CALG_RSA_SIGN);

			Assert.Equal(AssemblyHashAlgorithm.SHA_512, new StrongNamePublicKey(publicKey.CreatePublicKey()).HashAlgorithm);
			Assert.Equal(AssemblyHashAlgorithm.SHA_384, new StrongNamePublicKey(withBoth.CreatePublicKey()).HashAlgorithm);
			Assert.Equal(key.WithHashAlgorithm(AssemblyHashAlgorithm.SHA_512).PublicKey, publicKey.CreatePublicKey());
		}

		[Fact]
		public void PublicKeyIsReadFromAFile() {
			var blob = new StrongNameKey(keyPair).PublicKey;
			using (var directory = new TempDirectory()) {
				var file = directory.GetPath("public.snk");
				File.WriteAllBytes(file, blob);

				Assert.Equal(blob, new StrongNamePublicKey(file).CreatePublicKey());
			}
		}

		[Fact]
		public void EmptyPublicKeyHasNoNumbers() {
			var publicKey = new StrongNamePublicKey();

			Assert.Empty(publicKey.Modulus);
			Assert.Empty(publicKey.PublicExponent);
		}

		// A public key blob has 12 bytes before the key (the two algorithms and the size of the rest), then the
		// kind of blob (6: public key), its version (2), the algorithm and the text "RSA1"
		[Theory]
		[InlineData(12, 7, "Not a public key")]
		[InlineData(13, 3, "Invalid version")]
		[InlineData(17, 0xA4, "Not RSA sign")]
		[InlineData(20, (byte)'X', "Invalid RSA1 magic")]
		public void PublicKeyWithAnInvalidHeaderIsRejected(int offset, byte value, string expectedMessage) {
			var invalid = new StrongNameKey(keyPair).PublicKey;
			invalid = (byte[])invalid.Clone();
			invalid[offset] = value;

			var exception = Assert.Throws<InvalidKeyException>(() => new StrongNamePublicKey(invalid));

			Assert.Equal(expectedMessage, exception.Message);
		}

		[Theory, InlineData(0), InlineData(11), InlineData(20), InlineData(40)]
		public void TruncatedPublicKeyIsRejected(int length) {
			var blob = new StrongNameKey(keyPair).PublicKey;
			var truncated = new byte[length];
			Array.Copy(blob, truncated, length);

			Assert.Throws<InvalidKeyException>(() => new StrongNamePublicKey(truncated));
		}

		[Fact]
		public void ExceptionConstructors() {
			var inner = new IOException("inner");

			Assert.NotNull(new InvalidKeyException().Message);
			Assert.Equal("message", new InvalidKeyException("message").Message);
			Assert.Same(inner, new InvalidKeyException("message", inner).InnerException);
		}
	}
}
