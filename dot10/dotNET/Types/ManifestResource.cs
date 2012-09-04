using System.Diagnostics;

namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the ManifestResource table
	/// </summary>
	[DebuggerDisplay("{Offset} {Name.String} {Implementation}")]
	public abstract class ManifestResource : IHasCustomAttribute {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.ManifestResource, rid); }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 18; }
		}

		/// <summary>
		/// From column ManifestResource.Offset
		/// </summary>
		public abstract uint Offset { get; set; }

		/// <summary>
		/// From column ManifestResource.Flags
		/// </summary>
		public abstract ManifestResourceAttributes Flags { get; set; }

		/// <summary>
		/// From column ManifestResource.Name
		/// </summary>
		public abstract UTF8String Name { get; set; }

		/// <summary>
		/// From column ManifestResource.Implementation
		/// </summary>
		public abstract IImplementation Implementation { get; set; }
	}

	/// <summary>
	/// A ManifestResource row created by the user and not present in the original .NET file
	/// </summary>
	public class ManifestResourceUser : ManifestResource {
		uint offset;
		ManifestResourceAttributes flags;
		UTF8String name;
		IImplementation implementation;

		/// <inheritdoc/>
		public override uint Offset {
			get { return offset; }
			set { offset = value; }
		}

		/// <inheritdoc/>
		public override ManifestResourceAttributes Flags {
			get { return flags; }
			set { flags = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name; }
			set { name = value; }
		}

		/// <inheritdoc/>
		public override IImplementation Implementation {
			get { return implementation; }
			set { implementation = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public ManifestResourceUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="implementation">Implementation</param>
		public ManifestResourceUser(UTF8String name, IImplementation implementation)
			: this(name, implementation, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="implementation">Implementation</param>
		/// <param name="flags">Flags</param>
		public ManifestResourceUser(UTF8String name, IImplementation implementation, ManifestResourceAttributes flags)
			: this(name, implementation, flags, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="implementation">Implementation</param>
		/// <param name="flags">Flags</param>
		/// <param name="offset">Offset</param>
		public ManifestResourceUser(UTF8String name, IImplementation implementation, ManifestResourceAttributes flags, uint offset) {
			this.name = name;
			this.implementation = implementation;
			this.flags = flags;
			this.offset = offset;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="implementation">Implementation</param>
		public ManifestResourceUser(string name, IImplementation implementation)
			: this(name, implementation, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="implementation">Implementation</param>
		/// <param name="flags">Flags</param>
		public ManifestResourceUser(string name, IImplementation implementation, ManifestResourceAttributes flags)
			: this(name, implementation, flags, 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="implementation">Implementation</param>
		/// <param name="flags">Flags</param>
		/// <param name="offset">Offset</param>
		public ManifestResourceUser(string name, IImplementation implementation, ManifestResourceAttributes flags, uint offset)
			: this(new UTF8String(name), implementation, flags, offset) {
		}
	}
}
