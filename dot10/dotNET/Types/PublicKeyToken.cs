namespace dot10.dotNET.Types {
	/// <summary>
	/// Represents a public key token
	/// </summary>
	public class PublicKeyToken : PublicKeyBase {
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
	}
}
