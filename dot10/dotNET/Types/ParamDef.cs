using System.Diagnostics;

namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the Param table
	/// </summary>
	[DebuggerDisplay("{Sequence} {Name}")]
	public abstract class ParamDef : IHasConstant, IHasCustomAttribute, IHasFieldMarshal {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

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

		/// <summary>
		/// From column Param.Flags
		/// </summary>
		public abstract ParamAttributes Flags { get; set; }

		/// <summary>
		/// From column Param.Sequence
		/// </summary>
		public abstract ushort Sequence { get; set; }

		/// <summary>
		/// From column Param.Name
		/// </summary>
		public abstract UTF8String Name { get; set; }
	}

	/// <summary>
	/// A Param row created by the user and not present in the original .NET file
	/// </summary>
	public class ParamDefUser : ParamDef {
		ParamAttributes flags;
		ushort sequence;
		UTF8String name;

		/// <inheritdoc/>
		public override ParamAttributes Flags {
			get { return flags; }
			set { flags = value; }
		}

		/// <inheritdoc/>
		public override ushort Sequence {
			get { return sequence; }
			set { sequence = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name; }
			set { name = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public ParamDefUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		public ParamDefUser(UTF8String name)
			: this(name, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="sequence">Sequence</param>
		public ParamDefUser(UTF8String name, ushort sequence)
			: this(name, sequence, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="sequence">Sequence</param>
		/// <param name="flags">Flags</param>
		public ParamDefUser(UTF8String name, ushort sequence, ParamAttributes flags) {
			this.name = name;
			this.sequence = sequence;
			this.flags = flags;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		public ParamDefUser(string name)
			: this(name, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="sequence">Sequence</param>
		public ParamDefUser(string name, ushort sequence)
			: this(name, sequence, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="sequence">Sequence</param>
		/// <param name="flags">Flags</param>
		public ParamDefUser(string name, ushort sequence, ParamAttributes flags)
			: this(new UTF8String(name), sequence, flags) {
		}
	}
}
