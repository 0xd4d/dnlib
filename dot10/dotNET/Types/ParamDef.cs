#pragma warning disable 0169	//TODO:

namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the Param table
	/// </summary>
	public abstract class ParamDef : IHasConstant, IHasCustomAttribute, IHasFieldMarshal {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// From column Param.Flags
		/// </summary>
		ushort flags;

		/// <summary>
		/// From column Param.Sequence
		/// </summary>
		ushort sequence;

		/// <summary>
		/// From column Param.Name
		/// </summary>
		string name;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.Param, rid); }
		}

		/// <inheritdoc/>
		public int HasConstantTag {
			get { return 1; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 4; }
		}

		/// <inheritdoc/>
		public int HasFieldMarshalTag {
			get { return 1; }
		}
	}

	/// <summary>
	/// A Param row created by the user and not present in the original .NET file
	/// </summary>
	public class ParamDefUser : ParamDef {
	}
}
