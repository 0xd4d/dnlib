namespace dot10.dotNET.Hi {
	/// <summary>
	/// All signatures implement this interface
	/// </summary>
	public interface ISignature {
	}

	/// <summary>
	/// A field signature
	/// </summary>
	public class FieldSig : ISignature {
		/// <summary>
		/// The field type
		/// </summary>
		public ITypeSig Type { get; set; }
	}
}
