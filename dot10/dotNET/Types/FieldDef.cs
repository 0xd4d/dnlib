namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the Field table
	/// </summary>
	public class FieldDef : IHasConstant, IHasCustomAttribute, IHasFieldMarshal, IMemberForwarded {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// From column Field.Flags
		/// </summary>
		ushort flags;

		/// <summary>
		/// From column Field.Name
		/// </summary>
		string name;

		/// <summary>
		/// From column Field.Signature
		/// </summary>
		ISignature signature;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.Field, rid); }
		}

		/// <inheritdoc/>
		public int HasConstantTag {
			get { return 0; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 1; }
		}

		/// <inheritdoc/>
		public int HasFieldMarshalTag {
			get { return 0; }
		}

		/// <inheritdoc/>
		public int MemberForwardedTag {
			get { return 0; }
		}
	}
}
