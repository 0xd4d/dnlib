namespace dot10.DotNet {
	/// <summary>
	/// Represents a public key
	/// </summary>
	public sealed class PublicKey : PublicKeyBase {
		const AssemblyHashAlgorithm DEFAULT_ALGORITHM = AssemblyHashAlgorithm.SHA1;
		PublicKeyToken publicKeyToken;

		/// <summary>
		/// Gets the <see cref="PublicKeyToken"/>
		/// </summary>
		public PublicKeyToken Token {
			get {
				if (publicKeyToken == null && !IsNullOrEmpty)
					publicKeyToken = AssemblyHash.CreatePublicKeyToken(data);
				return publicKeyToken;
			}
		}

		/// <inheritdoc/>
		public override byte[] Data {
			get { return data; }
			set {
				if (data == value)
					return;
				data = value;
				publicKeyToken = null;
			}
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public PublicKey() {
		}

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
			var other = obj as PublicKey;
			if (other == null)
				return false;
			return Utils.Equals(data, other.data);
		}

		/// <inheritdoc/>
		public override int GetHashCode() {
			return Utils.GetHashCode(data);
		}
	}
}
