using System;
using dot10.DotNet.MD;

namespace dot10.DotNet {
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

		/// <summary>
		/// Gets/sets the <see cref="FileFlags.ContainsMetaData"/> bit
		/// </summary>
		public bool ContainsMetaData {
			get { return (Flags & FileFlags.ContainsNoMetaData) == 0; }
			set {
				if (value)
					Flags &= ~FileFlags.ContainsNoMetaData;
				else
					Flags |= FileFlags.ContainsNoMetaData;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="FileFlags.ContainsNoMetaData"/> bit
		/// </summary>
		public bool ContainsNoMetaData {
			get { return (Flags & FileFlags.ContainsNoMetaData) != 0; }
			set {
				if (value)
					Flags |= FileFlags.ContainsNoMetaData;
				else
					Flags &= ~FileFlags.ContainsNoMetaData;
			}
		}

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

	/// <summary>
	/// Created from a row in the File table
	/// </summary>
	sealed class FileDefMD : FileDef {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawFileRow rawRow;

		UserValue<FileFlags> flags;
		UserValue<UTF8String> name;
		UserValue<byte[]> hashValue;

		/// <inheritdoc/>
		public override FileFlags Flags {
			get { return flags.Value; }
			set { flags.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <inheritdoc/>
		public override byte[] HashValue {
			get { return hashValue.Value; }
			set { hashValue.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>File</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public FileDefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.Get(Table.File).IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("File rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			flags.ReadOriginalValue = () => {
				InitializeRawRow();
				return (FileFlags)rawRow.Flags;
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.Name);
			};
			hashValue.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.BlobStream.Read(rawRow.HashValue);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadFileRow(rid);
		}
	}
}
