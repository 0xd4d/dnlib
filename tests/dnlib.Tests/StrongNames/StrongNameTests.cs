// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Security.Cryptography;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.StrongNames {
	/// <summary>
	/// Strong name keys and signing an assembly when it's written. The signature of a written file is checked
	/// without dnlib, see <see cref="StrongNameVerifier"/>.
	/// </summary>
	public class StrongNameTests {
		// A new key pair in the format of an .snk file
		static byte[] CreateKeyPair(int bits = 1024) {
			using (var rsa = new RSACryptoServiceProvider(bits)) {
				var blob = rsa.ExportCspBlob(includePrivateParameters: true);
				// The key's algorithm id is at offset 4. .NET exports a "key exchange" key (CALG_RSA_KEYX, 0xA400),
				// strong name keys (sn.exe -k) are signature keys (CALG_RSA_SIGN, 0x2400). The key is the same.
				blob[4] = 0x00;
				blob[5] = 0x24;
				return blob;
			}
		}

		static readonly byte[] keyPair = CreateKeyPair();
		const string Source = "public class C { public static int M() => 123; }";

		static byte[] WriteSigned(byte[] image, StrongNameKey key) {
			using (var module = ModuleDefMD.Load(image)) {
				var options = new ModuleWriterOptions(module);
				options.InitializeStrongNameSigning(module, key);
				var stream = new MemoryStream();
				module.Write(stream, options);
				return stream.ToArray();
			}
		}

		[Fact]
		public void KeyPairHasAPublicKey() {
			var key = new StrongNameKey(keyPair);

			// The public key blob: 12 bytes header + 20 bytes key header + the modulus
			Assert.Equal(12 + 20 + 1024 / 8, key.PublicKey.Length);
			var publicKey = new StrongNamePublicKey(key.PublicKey);
			Assert.Equal(key.Modulus, publicKey.Modulus);
			Assert.Equal(key.PublicExponent, publicKey.PublicExponent);
		}

		[Theory, InlineData(1024), InlineData(2048), InlineData(4096)]
		public void SignedAssemblyHasAValidSignature(int bits) {
			var key = new StrongNameKey(CreateKeyPair(bits));

			var signed = WriteSigned(CSharpCompiler.Compile(Source).Image, key);

			Assert.True(StrongNameVerifier.IsValid(signed));
			using (var module = ModuleDefMD.Load(signed)) {
				Assert.Equal(key.PublicKey, module.Assembly!.PublicKey.Data);
				Assert.True(module.Assembly.HasPublicKey);
				Assert.True((module.Cor20HeaderFlags & ComImageFlags.StrongNameSigned) != 0);
			}
		}

		// Makes sure the verifier isn't the reason the tests above pass
		[Fact]
		public void ChangedFileHasAnInvalidSignature() {
			var signed = WriteSigned(CSharpCompiler.Compile(Source).Image, new StrongNameKey(keyPair));
			// The MVID is somewhere in the metadata: change one byte of it
			Guid mvid;
			using (var module = ModuleDefMD.Load(signed))
				mvid = module.Mvid!.Value;
			int offset = IndexOf(signed, mvid.ToByteArray());
			Assert.True(offset > 0);
			signed[offset] ^= 0xFF;

			Assert.False(StrongNameVerifier.IsValid(signed));
		}

		static int IndexOf(byte[] data, byte[] pattern) {
			for (int i = 0; i + pattern.Length <= data.Length; i++) {
				int j = 0;
				while (j < pattern.Length && data[i + j] == pattern[j])
					j++;
				if (j == pattern.Length)
					return i;
			}
			return -1;
		}

		[Fact]
		public void SignedAssemblyStillHasAValidSignatureAfterSigningItAgain() {
			var key = new StrongNameKey(keyPair);
			var signedOnce = WriteSigned(CSharpCompiler.Compile(Source).Image, key);

			var signedTwice = WriteSigned(signedOnce, key);

			Assert.True(StrongNameVerifier.IsValid(signedTwice));
		}

		// Fixed in 1de7f4c6: the flag says the file has a signature
		[Fact]
		public void AssemblyWithAPublicKeyThatIsNotSignedDoesNotHaveTheSignedFlag() {
			using (var module = ModuleDefMD.Load(CSharpCompiler.Compile(Source).Image)) {
				module.Assembly!.PublicKey = new PublicKey(new StrongNameKey(keyPair).PublicKey);
				var stream = new MemoryStream();
				module.Write(stream);

				using (var written = ModuleDefMD.Load(stream.ToArray())) {
					Assert.False(PublicKeyBase.IsNullOrEmpty2(written.Assembly!.PublicKey));
					Assert.Equal((ComImageFlags)0, written.Cor20HeaderFlags & ComImageFlags.StrongNameSigned);
				}
				Assert.False(StrongNameVerifier.IsValid(stream.ToArray()));
			}
		}

		[Fact]
		public void UnsignedAssemblyHasNoValidSignature() =>
			Assert.False(StrongNameVerifier.IsValid(CSharpCompiler.Compile(Source).Image));

		// Fixed in efc6384a: the bit length in the key was used as the size of an array before it was checked
		[Fact]
		public void KeyWithAnInvalidBitLengthIsRejectedWithoutAllocatingItsSize() {
			var invalidKey = (byte[])keyPair.Clone();
			// PRIVATEKEYBLOB: BLOBHEADER (8 bytes), "RSA2", bit length
			const int bitLengthOffset = 12;
			invalidKey[bitLengthOffset] = 0xFF;
			invalidKey[bitLengthOffset + 1] = 0xFF;
			invalidKey[bitLengthOffset + 2] = 0xFF;
			invalidKey[bitLengthOffset + 3] = 0xFF;

			Assert.Throws<InvalidKeyException>(() => new StrongNameKey(invalidKey));
		}

		[Theory, InlineData(0), InlineData(7), InlineData(20), InlineData(100)]
		public void TruncatedKeyIsRejected(int length) {
			var truncated = new byte[length];
			Array.Copy(keyPair, truncated, length);

			Assert.Throws<InvalidKeyException>(() => new StrongNameKey(truncated));
		}

		[Fact]
		public void PublicKeyIsNotAKeyPair() =>
			Assert.Throws<InvalidKeyException>(() => new StrongNameKey(new StrongNameKey(keyPair).PublicKey));

		// Fixed in 14ea7853
		[Fact]
		public void PublicKeyWithoutDataIsRejected() =>
			Assert.Throws<ArgumentNullException>(() => new StrongNamePublicKey(new PublicKey((byte[]?)null)));

		// Writes an assembly with the delay sign option: it gets the public key and room for the signature,
		// and someone who has the private key signs it later
		static byte[] WriteDelaySigned(Action<ModuleWriterOptions> changeOptions, ILogger? logger = null) {
			using (var module = ModuleDefMD.Load(CSharpCompiler.Compile(Source).Image)) {
				var options = new ModuleWriterOptions(module) { DelaySign = true, Logger = logger };
				changeOptions(options);
				var stream = new MemoryStream();
				module.Write(stream, options);
				return stream.ToArray();
			}
		}

		static StrongNamePublicKey CreatePublicKey() => new StrongNamePublicKey(new StrongNameKey(keyPair).PublicKey);

		[Fact]
		public void DelaySignedAssemblyHasThePublicKeyAndRoomForTheSignature() {
			var written = WriteDelaySigned(options => options.StrongNamePublicKey = CreatePublicKey());

			using (var module = ModuleDefMD.Load(written)) {
				Assert.Equal(new StrongNameKey(keyPair).PublicKey, module.Assembly!.PublicKey.Data);
				Assert.True(module.Assembly.HasPublicKey);
				Assert.Equal((ComImageFlags)0, module.Cor20HeaderFlags & ComImageFlags.StrongNameSigned);
				// A 1024-bit key has a signature of 128 bytes
				Assert.Equal(128U, module.Metadata.ImageCor20Header.StrongNameSignature.Size);
			}
			Assert.False(StrongNameVerifier.IsValid(written));
		}

		// Fixed in 1c9eec70: these were only checked by Debug.Assert()
		[Fact]
		public void DelaySigningWithoutAPublicKeyIsAnError() =>
			Assert.Throws<ModuleWriterException>(() => WriteDelaySigned(options => { }));

		[Fact]
		public void DelaySigningWithAKeyPairIsAnError() =>
			Assert.Throws<ModuleWriterException>(() => WriteDelaySigned(options => {
				options.StrongNamePublicKey = CreatePublicKey();
				options.StrongNameKey = new StrongNameKey(keyPair);
			}));

		// Fixed in 1c9eec70: the file got a signature, but the header said it has none
		[Fact]
		public void AssemblyIsSignedIfDelaySigningWithAKeyPairIsNotStoppedByTheLogger() {
			var written = WriteDelaySigned(options => {
				options.StrongNamePublicKey = CreatePublicKey();
				options.StrongNameKey = new StrongNameKey(keyPair);
			}, DummyLogger.NoThrowInstance);

			Assert.True(StrongNameVerifier.IsValid(written));
			using (var module = ModuleDefMD.Load(written))
				Assert.Equal(ComImageFlags.StrongNameSigned, module.Cor20HeaderFlags & ComImageFlags.StrongNameSigned);
		}
	}
}
