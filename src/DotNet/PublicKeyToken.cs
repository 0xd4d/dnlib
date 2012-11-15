namespace dot10.DotNet {
	/// <summary>
	/// Represents a public key token
	/// </summary>
	public sealed class PublicKeyToken : PublicKeyBase {
		/// <inheritdoc/>
		public PublicKeyToken()
			: base() {
		}

		/// <inheritdoc/>
		public PublicKeyToken(byte[] data)
			: base(data) {
		}

		/// <inheritdoc/>
		public PublicKeyToken(string hexString)
			: base(hexString) {
		}

		/// <inheritdoc/>
		public override bool Equals(object obj) {
			var other = obj as PublicKeyToken;
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
