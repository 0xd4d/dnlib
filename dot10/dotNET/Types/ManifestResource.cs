using System;
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

	/// <summary>
	/// Created from a row in the ManifestResource table
	/// </summary>
	sealed class ManifestResourceMD : ManifestResource {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawManifestResourceRow rawRow;

		UserValue<uint> offset;
		UserValue<ManifestResourceAttributes> flags;
		UserValue<UTF8String> name;
		UserValue<IImplementation> implementation;

		/// <inheritdoc/>
		public override uint Offset {
			get { return offset.Value; }
			set { offset.Value = value; }
		}

		/// <inheritdoc/>
		public override ManifestResourceAttributes Flags {
			get { return flags.Value; }
			set { flags.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <inheritdoc/>
		public override IImplementation Implementation {
			get { return implementation.Value; }
			set { implementation.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>ManifestResource</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public ManifestResourceMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			if (readerModule.TablesStream.Get(Table.ManifestResource).Rows < rid)
				throw new BadImageFormatException(string.Format("ManifestResource rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			offset.ReadOriginalValue = () => {
				InitializeRawRow();
				return rawRow.Offset;
			};
			flags.ReadOriginalValue = () => {
				InitializeRawRow();
				return (ManifestResourceAttributes)rawRow.Flags;
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.Name);
			};
			implementation.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveImplementation(rawRow.Implementation);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadManifestResourceRow(rid);
		}
	}
}
