namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the File table
	/// </summary>
	public class FileDef : IHasCustomAttribute, IImplementation {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// From column File.Flags
		/// </summary>
		uint flags;

		/// <summary>
		/// From column File.Name
		/// </summary>
		string name;

		/// <summary>
		/// From column File.HashValue
		/// </summary>
		byte[] hashValue;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.File, rid); }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 16; }
		}

		/// <inheritdoc/>
		public int ImplementationTag {
			get { return 0; }
		}
	}
}
