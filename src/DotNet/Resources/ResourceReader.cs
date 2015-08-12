// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using dnlib.IO;

namespace dnlib.DotNet.Resources {
	/// <summary>
	/// Thrown by <see cref="ResourceReader"/>
	/// </summary>
	[Serializable]
	public sealed class ResourceReaderException : Exception {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="msg">Message</param>
		public ResourceReaderException(string msg)
			: base(msg) {
		}
	}

	/// <summary>
	/// Gets called to create a <see cref="IResourceData"/> from serialized data. Returns <c>null</c>
	/// if a default <see cref="IResourceData"/> instance should be created.
	/// </summary>
	/// <param name="resourceDataCreator">ResourceDataCreator</param>
	/// <param name="type">Serialized type</param>
	/// <param name="serializedData">Serialized data</param>
	/// <returns></returns>
	public delegate IResourceData CreateResourceDataDelegate(ResourceDataCreator resourceDataCreator, UserResourceType type, byte[] serializedData);

	/// <summary>
	/// Reads .NET resources
	/// </summary>
	public struct ResourceReader {
		readonly IBinaryReader reader;
		readonly long baseFileOffset;
		readonly ResourceDataCreator resourceDataCreator;
		readonly CreateResourceDataDelegate createResourceDataDelegate;

		ResourceReader(ModuleDef module, IBinaryReader reader, CreateResourceDataDelegate createResourceDataDelegate) {
			this.reader = reader;
			this.resourceDataCreator = new ResourceDataCreator(module);
			this.createResourceDataDelegate = createResourceDataDelegate;

			var stream = reader as IImageStream;
			this.baseFileOffset = stream == null ? 0 : (long)stream.FileOffset;
		}

		/// <summary>
		/// Returns true if it's possibly resources file data
		/// </summary>
		/// <param name="reader">Reader</param>
		/// <returns></returns>
		public static bool CouldBeResourcesFile(IBinaryReader reader) {
			return reader.CanRead(4) && reader.ReadUInt32() == 0xBEEFCACE;
		}

		/// <summary>
		/// Reads a .NET resource
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="reader">Data of resource</param>
		/// <returns></returns>
		public static ResourceElementSet Read(ModuleDef module, IBinaryReader reader) {
			return Read(module, reader, null);
		}

		/// <summary>
		/// Reads a .NET resource
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="reader">Data of resource</param>
		/// <param name="createResourceDataDelegate">Call back that gets called to create a <see cref="IResourceData"/> instance. Can be null.</param>
		/// <returns></returns>
		public static ResourceElementSet Read(ModuleDef module, IBinaryReader reader, CreateResourceDataDelegate createResourceDataDelegate) {
			return new ResourceReader(module, reader, createResourceDataDelegate).Read();
		}

		ResourceElementSet Read() {
			ResourceElementSet resources = new ResourceElementSet();

			uint sig = reader.ReadUInt32();
			if (sig != 0xBEEFCACE)
				throw new ResourceReaderException(string.Format("Invalid resource sig: {0:X8}", sig));
			if (!CheckReaders())
				throw new ResourceReaderException("Invalid resource reader");
			int version = reader.ReadInt32();
			if (version != 2)//TODO: Support version 1
				throw new ResourceReaderException(string.Format("Invalid resource version: {0}", version));
			int numResources = reader.ReadInt32();
			if (numResources < 0)
				throw new ResourceReaderException(string.Format("Invalid number of resources: {0}", numResources));
			int numUserTypes = reader.ReadInt32();
			if (numUserTypes < 0)
				throw new ResourceReaderException(string.Format("Invalid number of user types: {0}", numUserTypes));

			var userTypes = new List<UserResourceType>();
			for (int i = 0; i < numUserTypes; i++)
				userTypes.Add(new UserResourceType(reader.ReadString(), ResourceTypeCode.UserTypes + i));
			reader.Position = (reader.Position + 7) & ~7;

			var hashes = new int[numResources];
			for (int i = 0; i < numResources; i++)
				hashes[i] = reader.ReadInt32();
			var offsets = new int[numResources];
			for (int i = 0; i < numResources; i++)
				offsets[i] = reader.ReadInt32();

			long baseOffset = reader.Position;
			long dataBaseOffset = reader.ReadInt32();
			long nameBaseOffset = reader.Position;
			long end = reader.Length;

			var infos = new List<ResourceInfo>(numResources);

			for (int i = 0; i < numResources; i++) {
				reader.Position = nameBaseOffset + offsets[i];
				var name = reader.ReadString(Encoding.Unicode);
				long offset = dataBaseOffset + reader.ReadInt32();
				infos.Add(new ResourceInfo(name, offset));
			}

			infos.Sort((a, b) => a.offset.CompareTo(b.offset));
			for (int i = 0; i < infos.Count; i++) {
				var info = infos[i];
				var element = new ResourceElement();
				element.Name = info.name;
				reader.Position = info.offset;
				long nextDataOffset = i == infos.Count - 1 ? end : infos[i + 1].offset;
				int size = (int)(nextDataOffset - info.offset);
				element.ResourceData = ReadResourceData(userTypes, size);
				element.ResourceData.StartOffset = this.baseFileOffset + (FileOffset)info.offset;
				element.ResourceData.EndOffset = this.baseFileOffset + (FileOffset)reader.Position;

				resources.Add(element);
			}

			return resources;
		}

		sealed class ResourceInfo {
			public string name;
			public long offset;
			public ResourceInfo(string name, long offset) {
				this.name = name;
				this.offset = offset;
			}
			public override string ToString() {
				return string.Format("{0:X8} - {1}", offset, name);
			}
		}

		IResourceData ReadResourceData(List<UserResourceType> userTypes, int size) {
			uint code = ReadUInt32(reader);
			switch ((ResourceTypeCode)code) {
			case ResourceTypeCode.Null:		return resourceDataCreator.CreateNull();
			case ResourceTypeCode.String:	return resourceDataCreator.Create(reader.ReadString());
			case ResourceTypeCode.Boolean:	return resourceDataCreator.Create(reader.ReadBoolean());
			case ResourceTypeCode.Char:		return resourceDataCreator.Create((char)reader.ReadUInt16());
			case ResourceTypeCode.Byte:		return resourceDataCreator.Create(reader.ReadByte());
			case ResourceTypeCode.SByte:	return resourceDataCreator.Create(reader.ReadSByte());
			case ResourceTypeCode.Int16:	return resourceDataCreator.Create(reader.ReadInt16());
			case ResourceTypeCode.UInt16:	return resourceDataCreator.Create(reader.ReadUInt16());
			case ResourceTypeCode.Int32:	return resourceDataCreator.Create(reader.ReadInt32());
			case ResourceTypeCode.UInt32:	return resourceDataCreator.Create(reader.ReadUInt32());
			case ResourceTypeCode.Int64:	return resourceDataCreator.Create(reader.ReadInt64());
			case ResourceTypeCode.UInt64:	return resourceDataCreator.Create(reader.ReadUInt64());
			case ResourceTypeCode.Single:	return resourceDataCreator.Create(reader.ReadSingle());
			case ResourceTypeCode.Double:	return resourceDataCreator.Create(reader.ReadDouble());
			case ResourceTypeCode.Decimal:	return resourceDataCreator.Create(reader.ReadDecimal());
			case ResourceTypeCode.DateTime: return resourceDataCreator.Create(DateTime.FromBinary(reader.ReadInt64()));
			case ResourceTypeCode.TimeSpan:	return resourceDataCreator.Create(new TimeSpan(reader.ReadInt64()));
			case ResourceTypeCode.ByteArray:return resourceDataCreator.Create(reader.ReadBytes(reader.ReadInt32()));
			case ResourceTypeCode.Stream:	return resourceDataCreator.CreateStream(reader.ReadBytes(reader.ReadInt32()));
			default:
				int userTypeIndex = (int)(code - (uint)ResourceTypeCode.UserTypes);
				if (userTypeIndex < 0 || userTypeIndex >= userTypes.Count)
					throw new ResourceReaderException(string.Format("Invalid resource data code: {0}", code));
				var userType = userTypes[userTypeIndex];
				var serializedData = reader.ReadBytes(size);
				if (createResourceDataDelegate != null) {
					var res = createResourceDataDelegate(resourceDataCreator, userType, serializedData);
					if (res != null)
						return res;
				}
				return resourceDataCreator.CreateSerialized(serializedData, userType);
			}
		}

		static uint ReadUInt32(IBinaryReader reader) {
			try {
				return reader.Read7BitEncodedUInt32();
			}
			catch {
				throw new ResourceReaderException("Invalid encoded int32");
			}
		}

		bool CheckReaders() {
			bool validReader = false;

			int numReaders = reader.ReadInt32();
			if (numReaders < 0)
				throw new ResourceReaderException(string.Format("Invalid number of readers: {0}", numReaders));
			int readersSize = reader.ReadInt32();
			if (readersSize < 0)
				throw new ResourceReaderException(string.Format("Invalid readers size: {0:X8}", readersSize));

			for (int i = 0; i < numReaders; i++) {
				var resourceReaderFullName = reader.ReadString();
				var resourceSetFullName = reader.ReadString();
				if (Regex.IsMatch(resourceReaderFullName, @"^System\.Resources\.ResourceReader,\s*mscorlib,"))
					validReader = true;
			}

			return validReader;
		}
	}
}
