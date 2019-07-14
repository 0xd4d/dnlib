// dnlib: See LICENSE.txt for more info

using System.Threading;

namespace dnlib.DotNet {
	/// <summary>
	/// Represents a public key
	/// </summary>
	public sealed class PublicKey : PublicKeyBase {
		const AssemblyHashAlgorithm DEFAULT_ALGORITHM = AssemblyHashAlgorithm.SHA1;
		PublicKeyToken publicKeyToken;

		/// <summary>
		/// Gets the <see cref="PublicKeyToken"/>
		/// </summary>
		public override PublicKeyToken Token {
			get {
				if (publicKeyToken is null && !IsNullOrEmpty)
					Interlocked.CompareExchange(ref publicKeyToken, AssemblyHash.CreatePublicKeyToken(data), null);
				return publicKeyToken;
			}
		}

		/// <inheritdoc/>
		public override byte[] Data => data;

		/// <summary>
		/// Constructor
		/// </summary>
		public PublicKey() : base((byte[])null) { }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">Public key data</param>
		public PublicKey(byte[] data)
			: base(data) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="hexString">Public key data as a hex string or the string <c>"null"</c>
		/// to set public key data to <c>null</c></param>
		public PublicKey(string hexString)
			: base(hexString) {
		}

		/// <inheritdoc/>
		public override bool Equals(object obj) {
			if ((object)this == obj)
				return true;
			var other = obj as PublicKey;
			if (other is null)
				return false;
			return Utils.Equals(Data, other.Data);
		}

		/// <inheritdoc/>
		public override int GetHashCode() => Utils.GetHashCode(Data);
	}
}
