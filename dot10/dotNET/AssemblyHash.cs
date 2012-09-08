using System.Security.Cryptography;

namespace dot10.dotNET {
	/// <summary>
	/// Hashes some data according to a <see cref="AssemblyHashAlgorithm"/>
	/// </summary>
	static class AssemblyHash {
		/// <summary>
		/// Hash data
		/// </summary>
		/// <remarks>If <paramref name="hashAlgo"/> is an unsupported hash algorithm, then
		/// <see cref="AssemblyHashAlgorithm.SHA1"/> will be used as the hash algorithm.</remarks>
		/// <param name="data">The data</param>
		/// <param name="hashAlgo">The algorithm to use</param>
		/// <returns>Hashed data or null if <paramref name="data"/> was <c>null</c></returns>
		public static byte[] Hash(byte[] data, AssemblyHashAlgorithm hashAlgo) {
			if (data == null)
				return null;

			switch (hashAlgo) {
			case AssemblyHashAlgorithm.MD5:
				using (var hash = MD5.Create())
					return hash.ComputeHash(data);

			case AssemblyHashAlgorithm.None:
			case AssemblyHashAlgorithm.MD2:
			case AssemblyHashAlgorithm.MD4:
			case AssemblyHashAlgorithm.SHA1:
			case AssemblyHashAlgorithm.MAC:
			case AssemblyHashAlgorithm.SSL3_SHAMD5:
			case AssemblyHashAlgorithm.HMAC:
			case AssemblyHashAlgorithm.TLS1PRF:
			case AssemblyHashAlgorithm.HASH_REPLACE_OWF:
			default:
				using (var hash = SHA1.Create())
					return hash.ComputeHash(data);

			case AssemblyHashAlgorithm.SHA_256:
				using (var hash = SHA256.Create())
					return hash.ComputeHash(data);

			case AssemblyHashAlgorithm.SHA_384:
				using (var hash = SHA384.Create())
					return hash.ComputeHash(data);

			case AssemblyHashAlgorithm.SHA_512:
				using (var hash = SHA512.Create())
					return hash.ComputeHash(data);
			}
		}

		/// <summary>
		/// Creates a public key token from the hash of some <paramref name="publicKeyData"/>
		/// </summary>
		/// <remarks>A public key is hashed, and the last 8 bytes of the hash, in reverse
		/// order, is used as the public key token</remarks>
		/// <param name="publicKeyData">The data</param>
		/// <param name="hashAlgo">The algorithm to use</param>
		/// <returns>A new <see cref="PublicKeyToken"/> instance</returns>
		public static PublicKeyToken CreatePublicKeyToken(byte[] publicKeyData, AssemblyHashAlgorithm hashAlgo) {
			if (publicKeyData == null)
				return new PublicKeyToken();
			var hash = Hash(publicKeyData, hashAlgo);
			byte[] pkt = new byte[8];
			for (int i = 0; i < pkt.Length && i < hash.Length; i++)
				pkt[i] = hash[hash.Length - i - 1];
			return new PublicKeyToken(pkt);
		}
	}
}
