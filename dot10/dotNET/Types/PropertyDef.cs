namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the Property table
	/// </summary>
	public class PropertyDef : IHasConstant, IHasCustomAttribute, IHasSemantic {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// From column Property.PropFlags
		/// </summary>
		ushort propFlags;

		/// <summary>
		/// From column Property.Name
		/// </summary>
		string name;

		/// <summary>
		/// From column Property.Type
		/// </summary>
		ISignature type;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.Property, rid); }
		}

		/// <inheritdoc/>
		public int HasConstantTag {
			get { return 2; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 9; }
		}

		/// <inheritdoc/>
		public int HasSemanticTag {
			get { return 1; }
		}
	}
}
