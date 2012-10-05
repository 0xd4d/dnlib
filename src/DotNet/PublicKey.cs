namespace dot10.DotNet {
	/// <summary>
	/// Represents a public key
	/// </summary>
	public sealed class PublicKey : PublicKeyBase {
		const AssemblyHashAlgorithm DEFAULT_ALGORITHM = AssemblyHashAlgorithm.SHA1;
		AssemblyHashAlgorithm hashAlgo;
		PublicKeyToken publicKeyToken;

		/// <summary>
		/// Gets the <see cref="PublicKeyToken"/>
		/// </summary>
		public PublicKeyToken Token {
			get {
				if (publicKeyToken == null && !IsNullOrEmpty)
					publicKeyToken = AssemblyHash.CreatePublicKeyToken(data, hashAlgo);
				return publicKeyToken;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyHashAlgorithm"/>
		/// </summary>
		public AssemblyHashAlgorithm HashAlgorithm {
			get { return hashAlgo; }
			set {
				if (hashAlgo == value)
					return;
				hashAlgo = value;
				publicKeyToken = null;
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

		/// <inheritdoc/>
		public PublicKey()
			: this(DEFAULT_ALGORITHM) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="hashAlgo">Hash algorithm</param>
		public PublicKey(AssemblyHashAlgorithm hashAlgo)
			: base() {
			this.hashAlgo = hashAlgo;
		}

		/// <inheritdoc/>
		public PublicKey(byte[] data)
			: this(data, DEFAULT_ALGORITHM) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">Public key data</param>
		/// <param name="hashAlgo">Hash algorithm</param>
		public PublicKey(byte[] data, AssemblyHashAlgorithm hashAlgo)
			: base(data) {
			this.hashAlgo = hashAlgo;
		}

		/// <inheritdoc/>
		public PublicKey(string hexString)
			: this(hexString, DEFAULT_ALGORITHM) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="hexString">Public key data as a hex string or the string <c>"null"</c>
		/// to set public key data to <c>null</c></param>
		/// <param name="hashAlgo">Hash algorithm</param>
		public PublicKey(string hexString, AssemblyHashAlgorithm hashAlgo)
			: base(hexString) {
			this.hashAlgo = hashAlgo;
		}
	}
}
