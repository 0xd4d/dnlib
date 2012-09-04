namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the File table
	/// </summary>
	public abstract class FileDef : IHasCustomAttribute, IImplementation {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

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

		/// <summary>
		/// From column File.Flags
		/// </summary>
		public abstract FileFlags Flags { get; set; }

		/// <summary>
		/// From column File.Name
		/// </summary>
		public abstract UTF8String Name { get; set; }

		/// <summary>
		/// From column File.HashValue
		/// </summary>
		public abstract byte[] HashValue { get; set; }

		/// <inheritdoc/>
		public override string ToString() {
			return Name.String;
		}
	}

	/// <summary>
	/// A File row created by the user and not present in the original .NET file
	/// </summary>
	public class FileDefUser : FileDef {
		FileFlags flags;
		UTF8String name;
		byte[] hashValue;

		/// <inheritdoc/>
		public override FileFlags Flags {
			get { return flags; }
			set { flags = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name; }
			set { name = value; }
		}

		/// <inheritdoc/>
		public override byte[] HashValue {
			get { return hashValue; }
			set { hashValue = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public FileDefUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name of file</param>
		/// <param name="flags">Flags</param>
		/// <param name="hashValue">File hash</param>
		public FileDefUser(string name, FileFlags flags, byte[] hashValue)
			: this(new UTF8String(name), flags, hashValue) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name of file</param>
		/// <param name="flags">Flags</param>
		/// <param name="hashValue">File hash</param>
		public FileDefUser(UTF8String name, FileFlags flags, byte[] hashValue) {
			this.name = name;
			this.flags = flags;
			this.hashValue = hashValue;
		}
	}
}
