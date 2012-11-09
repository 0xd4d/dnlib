using System;
using System.Collections.Generic;
using dot10.Utils;
using dot10.IO;
using dot10.PE;

namespace dot10.Win32Resources {
	/// <summary>
	/// A Win32 resource directory (see IMAGE_RESOURCE_DIRECTORY in the Windows SDK)
	/// </summary>
	public abstract class ResourceDirectory : ResourceDirectoryEntry, IDisposable {
		/// <summary>See <see cref="Characteristics"/></summary>
		protected uint characteristics;
		/// <summary>See <see cref="TimeDateStamp"/></summary>
		protected uint timeDateStamp;
		/// <summary>See <see cref="MajorVersion"/></summary>
		protected ushort majorVersion;
		/// <summary>See <see cref="MinorVersion"/></summary>
		protected ushort minorVersion;
		/// <summary>See <see cref="Directories"/></summary>
		protected ILazyList<ResourceDirectory> directories;
		/// <summary>See <see cref="Data"/></summary>
		protected ILazyList<ResourceData> data;

		/// <summary>
		/// Gets/sets the characteristics
		/// </summary>
		public uint Characteristics {
			get { return characteristics; }
			set { characteristics = value; }
		}

		/// <summary>
		/// Gets/sets the time date stamp
		/// </summary>
		public uint TimeDateStamp {
			get { return timeDateStamp; }
			set { timeDateStamp = value; }
		}

		/// <summary>
		/// Gets/sets the major version number
		/// </summary>
		public ushort MajorVersion {
			get { return majorVersion; }
			set { majorVersion = value; }
		}

		/// <summary>
		/// Gets/sets the minor version number
		/// </summary>
		public ushort MinorVersion {
			get { return minorVersion; }
			set { minorVersion = value; }
		}

		/// <summary>
		/// Gets all directory entries
		/// </summary>
		public IList<ResourceDirectory> Directories {
			get { return directories; }
		}

		/// <summary>
		/// Gets all resource data
		/// </summary>
		public IList<ResourceData> Data {
			get { return data; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		protected ResourceDirectory(ResourceName name)
			: base(name) {
		}

		/// <summary>
		/// Finds a <see cref="ResourceDirectory"/> by name
		/// </summary>
		/// <param name="name">Name</param>
		/// <returns>A <see cref="ResourceDirectory"/> or <c>null</c> if it wasn't found</returns>
		ResourceDirectory FindDirectory(ResourceName name) {
			foreach (var dir in directories) {
				if (dir.Name == name)
					return dir;
			}
			return null;
		}

		/// <summary>
		/// Finds a <see cref="ResourceData"/> by name
		/// </summary>
		/// <param name="name">Name</param>
		/// <returns>A <see cref="ResourceData"/> or <c>null</c> if it wasn't found</returns>
		ResourceData FindData(ResourceName name) {
			foreach (var d in data) {
				if (d.Name == name)
					return d;
			}
			return null;
		}

		/// <inheritdoc/>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose method
		/// </summary>
		/// <param name="disposing"><c>true</c> if called by <see cref="Dispose()"/></param>
		protected virtual void Dispose(bool disposing) {
			directories.DisposeAll();
			data.DisposeAll();
			directories = null;
			data = null;
		}
	}

	/// <summary>
	/// A Win32 resource directory created by the user
	/// </summary>
	public class ResourceDirectoryUser : ResourceDirectory {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		public ResourceDirectoryUser(ResourceName name)
			: base(name) {
			this.directories = new LazyList<ResourceDirectory>();
			this.data = new LazyList<ResourceData>();
		}
	}

	/// <summary>
	/// A Win32 resource directory created from a PE file
	/// </summary>
	public sealed class ResourceDirectoryPE : ResourceDirectory {
		/// <summary>
		/// To make sure we don't get stuck in an infinite loop, don't allow more than this
		/// many sub directories.
		/// </summary>
		const uint MAX_DIR_DEPTH = 10;

		readonly Win32ResourcesPE resources;
		uint depth;
		List<EntryInfo> dataInfos;
		List<EntryInfo> dirInfos;

		struct EntryInfo {
			public ResourceName name;
			public uint offset;

			public EntryInfo(ResourceName name, uint offset) {
				this.name = name;
				this.offset = offset;
			}

			public override string ToString() {
				return string.Format("{0:X8} {1}", offset, name);
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="depth">Starts from 0. If it's big enough, we'll stop reading more data.</param>
		/// <param name="name">Name</param>
		/// <param name="resources">Resources</param>
		/// <param name="reader">Reader position at the start of this resource directory</param>
		public ResourceDirectoryPE(uint depth, ResourceName name, Win32ResourcesPE resources, IBinaryReader reader)
			: base(name) {
			this.resources = resources;
			this.depth = depth;
			Initialize(reader);
		}

		void Initialize(IBinaryReader reader) {
			if (depth > MAX_DIR_DEPTH || !Win32ResourcesPE.CanRead(reader, 16)) {
				InitializeDefault();
				return;
			}

			characteristics = reader.ReadUInt32();
			timeDateStamp = reader.ReadUInt32();
			majorVersion = reader.ReadUInt16();
			minorVersion = reader.ReadUInt16();
			ushort numNamed = reader.ReadUInt16();
			ushort numIds = reader.ReadUInt16();
			int total = numNamed + numIds;
			if (!Win32ResourcesPE.CanRead(reader, total * 8)) {
				InitializeDefault();
				return;
			}

			dataInfos = new List<EntryInfo>();
			dirInfos = new List<EntryInfo>();
			long offset = reader.Position;
			for (int i = 0; i < total; i++, offset += 8) {
				reader.Position = offset;
				uint nameOrId = reader.ReadUInt32();
				uint dataOrDirectory = reader.ReadUInt32();
				ResourceName name;
				if ((nameOrId & 0x80000000) != 0) {
					var s = Win32ResourcesPE.ReadString(reader, nameOrId & 0x7FFFFFFF);
					if (s == null)
						break;
					name = new ResourceName(s);
				}
				else
					name = new ResourceName((int)nameOrId);

				if ((dataOrDirectory & 0x80000000) == 0)
					dataInfos.Add(new EntryInfo(name, dataOrDirectory));
				else
					dirInfos.Add(new EntryInfo(name, dataOrDirectory & 0x7FFFFFFF));
			}

			directories = new LazyList<ResourceDirectory>(dirInfos.Count, null, (ctx, i) => ReadResourceDirectory(i));
			data = new LazyList<ResourceData>(dataInfos.Count, null, (ctx, i) => ReadResourceData(i));
		}

		ResourceDirectory ReadResourceDirectory(uint i) {
			var info = dirInfos[(int)i];
			var reader = resources.ResourceReader;
			var oldPos = reader.Position;
			reader.Position = info.offset;

			var dir = new ResourceDirectoryPE(depth + 1, info.name, resources, reader);

			reader.Position = oldPos;
			return dir;
		}

		ResourceData ReadResourceData(uint i) {
			var info = dataInfos[(int)i];
			var reader = resources.ResourceReader;
			var oldPos = reader.Position;
			reader.Position = info.offset;

			ResourceData data;
			if (Win32ResourcesPE.CanRead(reader, 16)) {
				RVA rva = (RVA)reader.ReadUInt32();
				uint size = reader.ReadUInt32();
				uint codePage = reader.ReadUInt32();
				uint reserved = reader.ReadUInt32();
				var dataReader = resources.CreateDataReader(rva, size);
				data = new ResourceData(info.name, dataReader, codePage, reserved);
			}
			else
				data = new ResourceData(info.name, MemoryImageStream.CreateEmpty());

			reader.Position = oldPos;
			return data;
		}

		void InitializeDefault() {
			directories = new LazyList<ResourceDirectory>();
			data = new LazyList<ResourceData>();
		}
	}
}
