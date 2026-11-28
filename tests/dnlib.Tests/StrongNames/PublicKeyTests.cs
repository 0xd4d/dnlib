// dnlib: See LICENSE.txt for more info

using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.StrongNames {
	/// <summary>
	/// A <see cref="PublicKey"/> and a <see cref="PublicKeyToken"/> (the last 8 bytes of the SHA-1 hash of a key,
	/// reversed) are equal if their bytes are equal. <see cref="PublicKeyBase"/> has helpers that compare tokens.
	/// </summary>
	public class PublicKeyTests {
		const string KeyHex = "0024000004800000940000000602000000240000525341310004000001000100";

		[Fact]
		public void AKeyIsCreatedFromAHexString() {
			var key = new PublicKey(KeyHex);

			Assert.Equal(32, key.Data!.Length);
			Assert.Equal(0x24, key.Data[1]);
			Assert.Null(new PublicKey((string?)null).Data);
		}

		[Fact]
		public void KeysWithTheSameBytesAreEqual() {
			var key = new PublicKey(KeyHex);
			var same = new PublicKey(KeyHex);
			var other = new PublicKey("0024");

			Assert.True(key.Equals(key));
			Assert.True(key.Equals(same));
			Assert.Equal(key.GetHashCode(), same.GetHashCode());
			Assert.False(key.Equals(other));
			Assert.False(key.Equals(new PublicKeyToken(KeyHex)));
			Assert.False(key.Equals(null));
		}

		[Fact]
		public void TokensWithTheSameBytesAreEqual() {
			var token = new PublicKeyToken("b77a5c561934e089");
			var same = new PublicKeyToken("B77A5C561934E089");

			Assert.True(token.Equals(token));
			Assert.True(token.Equals(same));
			Assert.Equal(token.GetHashCode(), same.GetHashCode());
			Assert.False(token.Equals(new PublicKeyToken("b03f5f7f11d50a3a")));
			Assert.False(token.Equals((object?)null));
		}

		// A key is compared by its token
		[Fact]
		public void TokensOfKeysAreCompared() {
			var key = new PublicKey(KeyHex);
			var token = key.Token;

			Assert.True(PublicKeyBase.TokenEquals(key, token));
			Assert.True(PublicKeyBase.TokenEquals(token, new PublicKeyToken(token!.Data)));
			Assert.False(PublicKeyBase.TokenEquals(token, new PublicKeyToken("b77a5c561934e089")));
			Assert.True(PublicKeyBase.TokenEquals((PublicKeyToken?)null, null));
			Assert.False(PublicKeyBase.TokenEquals(token, null));
			Assert.Equal(PublicKeyBase.GetHashCode(token), PublicKeyBase.GetHashCodeToken(key));
			Assert.Equal(0, PublicKeyBase.GetHashCode((PublicKeyToken?)null));
			Assert.Same(key.Data, PublicKeyBase.GetRawData(key));
			Assert.Null(PublicKeyBase.GetRawData(null));
		}
	}
}
