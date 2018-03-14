// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using dnlib.Utils;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.W32Resources {
	/// <summary>
	/// A Win32 resource directory (see IMAGE_RESOURCE_DIRECTORY in the Windows SDK)
	/// </summary>
	public abstract class ResourceDirectory : ResourceDirectoryEntry {
		/// <summary>See <see cref="Characteristics"/></summary>
		protected uint characteristics;
		/// <summary>See <see cref="TimeDateStamp"/></summary>
		protected uint timeDateStamp;
		/// <summary>See <see cref="MajorVersion"/></summary>
		protected ushort majorVersion;
		/// <summary>See <see cref="MinorVersion"/></summary>
		protected ushort minorVersion;
		/// <summary>See <see cref="Directories"/></summary>
		private protected LazyList<ResourceDirectory> directories;
		/// <summary>See <see cref="Data"/></summary>
		private protected LazyList<ResourceData> data;

		/// <summary>
		/// Gets/sets the characteristics
		/// </summary>
		public uint Characteristics {
			get => characteristics;
			set => characteristics = value;
		}

		/// <summary>
		/// Gets/sets the time date stamp
		/// </summary>
		public uint TimeDateStamp {
			get => timeDateStamp;
			set => timeDateStamp = value;
		}

		/// <summary>
		/// Gets/sets the major version number
		/// </summary>
		public ushort MajorVersion {
			get => majorVersion;
			set => majorVersion = value;
		}

		/// <summary>
		/// Gets/sets the minor version number
		/// </summary>
		public ushort MinorVersion {
			get => minorVersion;
			set => minorVersion = value;
		}

		/// <summary>
		/// Gets all directory entries
		/// </summary>
		public IList<ResourceDirectory> Directories => directories;

		/// <summary>
		/// Gets all resource data
		/// </summary>
		public IList<ResourceData> Data => data;

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
		public ResourceDirectory FindDirectory(ResourceName name) {
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
		public ResourceData FindData(ResourceName name) {
			foreach (var d in data) {
				if (d.Name == name)
					return d;
			}
			return null;
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
			directories = new LazyList<ResourceDirectory>();
			data = new LazyList<ResourceData>();
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

		/// <summary>Owner</summary>
		readonly Win32ResourcesPE resources;
		/// <summary>Directory depth. When creating more <see cref="ResourceDirectoryPE"/>'s,
		/// the instances get this value + 1</summary>
		uint depth;

		/// <summary>
		/// Info about all <see cref="ResourceData"/>'s we haven't created yet
		/// </summary>
		List<EntryInfo> dataInfos;

		/// <summary>
		/// Info about all <see cref="ResourceDirectory"/>'s we haven't created yet
		/// </summary>
		List<EntryInfo> dirInfos;

		readonly struct EntryInfo {
			public readonly ResourceName name;

			/// <summary>Offset of resource directory / data</summary>
			public readonly uint offset;

			public EntryInfo(ResourceName name, uint offset) {
				this.name = name;
				this.offset = offset;
			}

			public override string ToString() => $"{offset:X8} {name}";
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="depth">Starts from 0. If it's big enough, we'll stop reading more data.</param>
		/// <param name="name">Name</param>
		/// <param name="resources">Resources</param>
		/// <param name="reader">Reader positioned at the start of this resource directory</param>
		public ResourceDirectoryPE(uint depth, ResourceName name, Win32ResourcesPE resources, ref DataReader reader)
			: base(name) {
			this.resources = resources;
			this.depth = depth;
			Initialize(ref reader);
		}

		/// <summary>
		/// Reads the directory header and initializes <see cref="ResourceDirectory.directories"/> and
		/// <see cref="ResourceDirectory.data"/>.
		/// </summary>
		/// <param name="reader"></param>
		void Initialize(ref DataReader reader) {
			if (depth > MAX_DIR_DEPTH || !reader.CanRead(16U)) {
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
			if (!reader.CanRead((uint)total * 8)) {
				InitializeDefault();
				return;
			}

			dataInfos = new List<EntryInfo>();
			dirInfos = new List<EntryInfo>();
			uint offset = reader.Position;
			for (int i = 0; i < total; i++, offset += 8) {
				reader.Position = offset;
				uint nameOrId = reader.ReadUInt32();
				uint dataOrDirectory = reader.ReadUInt32();
				ResourceName name;
				if ((nameOrId & 0x80000000) != 0)
					name = new ResourceName(ReadString(ref reader, nameOrId & 0x7FFFFFFF) ?? string.Empty);
				else
					name = new ResourceName((int)nameOrId);

				if ((dataOrDirectory & 0x80000000) == 0)
					dataInfos.Add(new EntryInfo(name, dataOrDirectory));
				else
					dirInfos.Add(new EntryInfo(name, dataOrDirectory & 0x7FFFFFFF));
			}

			directories = new LazyList<ResourceDirectory, object>(dirInfos.Count, null, (ctx, i) => ReadResourceDirectory(i));
			data = new LazyList<ResourceData, object>(dataInfos.Count, null, (ctx, i) => ReadResourceData(i));
		}

		/// <summary>
		/// Reads a string
		/// </summary>
		/// <param name="reader">Reader</param>
		/// <param name="offset">Offset of string</param>
		/// <returns>The string or <c>null</c> if we could not read it</returns>
		static string ReadString(ref DataReader reader, uint offset) {
			reader.Position = offset;
			if (!reader.CanRead(2U))
				return null;
			int size = reader.ReadUInt16();
			int sizeInBytes = size * 2;
			if (!reader.CanRead((uint)sizeInBytes))
				return null;
			try {
				return reader.ReadUtf16String(sizeInBytes / 2);
			}
			catch {
				return null;
			}
		}

		ResourceDirectory ReadResourceDirectory(int i) {
			var info = dirInfos[i];
			var reader = resources.GetResourceReader();
			reader.Position = Math.Min(reader.Length, info.offset);
			return new ResourceDirectoryPE(depth + 1, info.name, resources, ref reader);
		}

		ResourceData ReadResourceData(int i) {
			var info = dataInfos[i];
			var reader = resources.GetResourceReader();
			reader.Position = Math.Min(reader.Length, info.offset);

			ResourceData data;
			if (reader.CanRead(16U)) {
				var rva = (RVA)reader.ReadUInt32();
				uint size = reader.ReadUInt32();
				uint codePage = reader.ReadUInt32();
				uint reserved = reader.ReadUInt32();
				resources.GetDataReaderInfo(rva, size, out var dataReaderFactory, out uint dataOffset, out uint dataLength);
				data = new ResourceData(info.name, dataReaderFactory, dataOffset, dataLength, codePage, reserved);
			}
			else
				data = new ResourceData(info.name);

			return data;
		}

		void InitializeDefault() {
			directories = new LazyList<ResourceDirectory>();
			data = new LazyList<ResourceData>();
		}
	}
}
