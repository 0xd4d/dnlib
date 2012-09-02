#pragma warning disable 0169	//TODO:

namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the StandAloneSig table
	/// </summary>
	public class StandAloneSig : IHasCustomAttribute {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// From column StandAloneSig.Signature
		/// </summary>
		ISignature signature;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.StandAloneSig, rid); }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 11; }
		}
	}
}
