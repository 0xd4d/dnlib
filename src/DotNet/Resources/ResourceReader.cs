// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
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
		public ResourceReaderException() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="msg">Message</param>
		public ResourceReaderException(string msg)
			: base(msg) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		public ResourceReaderException(SerializationInfo info, StreamingContext context)
			: base(info, context) {
		}
	}

	/// <summary>
	/// Gets called to create a <see cref="IResourceData"/> from serialized data. Returns <c>null</c>
	/// if a default <see cref="IResourceData"/> instance should be created.
	/// </summary>
	/// <param name="resourceDataFactory">ResourceDataFactory</param>
	/// <param name="type">Serialized type</param>
	/// <param name="serializedData">Serialized data</param>
	/// <returns></returns>
	public delegate IResourceData CreateResourceDataDelegate(ResourceDataFactory resourceDataFactory, UserResourceType type, byte[] serializedData);

	/// <summary>
	/// Reads .NET resources
	/// </summary>
	public struct ResourceReader {
		DataReader reader;
		readonly uint baseFileOffset;
		readonly ResourceDataFactory resourceDataFactory;
		readonly CreateResourceDataDelegate createResourceDataDelegate;

		ResourceReader(ModuleDef module, ref DataReader reader, CreateResourceDataDelegate createResourceDataDelegate) {
			this.reader = reader;
			resourceDataFactory = new ResourceDataFactory(module);
			this.createResourceDataDelegate = createResourceDataDelegate;
			baseFileOffset = reader.StartOffset;
		}

		/// <summary>
		/// Returns true if it's possibly resources file data
		/// </summary>
		/// <param name="reader">Reader</param>
		/// <returns></returns>
		public static bool CouldBeResourcesFile(DataReader reader) =>
			reader.CanRead(4U) && reader.ReadUInt32() == 0xBEEFCACE;

		/// <summary>
		/// Reads a .NET resource
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="reader">Data of resource</param>
		/// <returns></returns>
		public static ResourceElementSet Read(ModuleDef module, DataReader reader) => Read(module, reader, null);

		/// <summary>
		/// Reads a .NET resource
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="reader">Data of resource</param>
		/// <param name="createResourceDataDelegate">Call back that gets called to create a <see cref="IResourceData"/> instance. Can be null.</param>
		/// <returns></returns>
		public static ResourceElementSet Read(ModuleDef module, DataReader reader, CreateResourceDataDelegate createResourceDataDelegate) =>
			new ResourceReader(module, ref reader, createResourceDataDelegate).Read();

		ResourceElementSet Read() {
			var resources = new ResourceElementSet();

			uint sig = reader.ReadUInt32();
			if (sig != 0xBEEFCACE)
				throw new ResourceReaderException($"Invalid resource sig: {sig:X8}");
			if (!CheckReaders())
				throw new ResourceReaderException("Invalid resource reader");
			int version = reader.ReadInt32();
			if (version != 2)//TODO: Support version 1
				throw new ResourceReaderException($"Invalid resource version: {version}");
			int numResources = reader.ReadInt32();
			if (numResources < 0)
				throw new ResourceReaderException($"Invalid number of resources: {numResources}");
			int numUserTypes = reader.ReadInt32();
			if (numUserTypes < 0)
				throw new ResourceReaderException($"Invalid number of user types: {numUserTypes}");

			var userTypes = new List<UserResourceType>();
			for (int i = 0; i < numUserTypes; i++)
				userTypes.Add(new UserResourceType(reader.ReadSerializedString(), ResourceTypeCode.UserTypes + i));
			reader.Position = (reader.Position + 7) & ~7U;

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
				reader.Position = (uint)(nameBaseOffset + offsets[i]);
				var name = reader.ReadSerializedString(Encoding.Unicode);
				long offset = dataBaseOffset + reader.ReadInt32();
				infos.Add(new ResourceInfo(name, offset));
			}

			infos.Sort((a, b) => a.offset.CompareTo(b.offset));
			for (int i = 0; i < infos.Count; i++) {
				var info = infos[i];
				var element = new ResourceElement();
				element.Name = info.name;
				reader.Position = (uint)info.offset;
				long nextDataOffset = i == infos.Count - 1 ? end : infos[i + 1].offset;
				int size = (int)(nextDataOffset - info.offset);
				element.ResourceData = ReadResourceData(userTypes, size);
				element.ResourceData.StartOffset = baseFileOffset + (FileOffset)info.offset;
				element.ResourceData.EndOffset = baseFileOffset + (FileOffset)reader.Position;

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
			public override string ToString() => $"{offset:X8} - {name}";
		}

		IResourceData ReadResourceData(List<UserResourceType> userTypes, int size) {
			uint endPos = reader.Position + (uint)size;
			uint code = ReadUInt32(ref reader);
			switch ((ResourceTypeCode)code) {
			case ResourceTypeCode.Null:		return resourceDataFactory.CreateNull();
			case ResourceTypeCode.String:	return resourceDataFactory.Create(reader.ReadSerializedString());
			case ResourceTypeCode.Boolean:	return resourceDataFactory.Create(reader.ReadBoolean());
			case ResourceTypeCode.Char:		return resourceDataFactory.Create(reader.ReadChar());
			case ResourceTypeCode.Byte:		return resourceDataFactory.Create(reader.ReadByte());
			case ResourceTypeCode.SByte:	return resourceDataFactory.Create(reader.ReadSByte());
			case ResourceTypeCode.Int16:	return resourceDataFactory.Create(reader.ReadInt16());
			case ResourceTypeCode.UInt16:	return resourceDataFactory.Create(reader.ReadUInt16());
			case ResourceTypeCode.Int32:	return resourceDataFactory.Create(reader.ReadInt32());
			case ResourceTypeCode.UInt32:	return resourceDataFactory.Create(reader.ReadUInt32());
			case ResourceTypeCode.Int64:	return resourceDataFactory.Create(reader.ReadInt64());
			case ResourceTypeCode.UInt64:	return resourceDataFactory.Create(reader.ReadUInt64());
			case ResourceTypeCode.Single:	return resourceDataFactory.Create(reader.ReadSingle());
			case ResourceTypeCode.Double:	return resourceDataFactory.Create(reader.ReadDouble());
			case ResourceTypeCode.Decimal:	return resourceDataFactory.Create(reader.ReadDecimal());
			case ResourceTypeCode.DateTime: return resourceDataFactory.Create(DateTime.FromBinary(reader.ReadInt64()));
			case ResourceTypeCode.TimeSpan:	return resourceDataFactory.Create(new TimeSpan(reader.ReadInt64()));
			case ResourceTypeCode.ByteArray:return resourceDataFactory.Create(reader.ReadBytes(reader.ReadInt32()));
			case ResourceTypeCode.Stream:	return resourceDataFactory.CreateStream(reader.ReadBytes(reader.ReadInt32()));
			default:
				int userTypeIndex = (int)(code - (uint)ResourceTypeCode.UserTypes);
				if (userTypeIndex < 0 || userTypeIndex >= userTypes.Count)
					throw new ResourceReaderException($"Invalid resource data code: {code}");
				var userType = userTypes[userTypeIndex];
				var serializedData = reader.ReadBytes((int)(endPos - reader.Position));
				if (!(createResourceDataDelegate is null)) {
					var res = createResourceDataDelegate(resourceDataFactory, userType, serializedData);
					if (!(res is null))
						return res;
				}
				return resourceDataFactory.CreateSerialized(serializedData, userType);
			}
		}

		static uint ReadUInt32(ref DataReader reader) {
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
				throw new ResourceReaderException($"Invalid number of readers: {numReaders}");
			int readersSize = reader.ReadInt32();
			if (readersSize < 0)
				throw new ResourceReaderException($"Invalid readers size: {readersSize:X8}");

			for (int i = 0; i < numReaders; i++) {
				var resourceReaderFullName = reader.ReadSerializedString();
				/*var resourceSetFullName = */reader.ReadSerializedString();
				if (Regex.IsMatch(resourceReaderFullName, @"^System\.Resources\.ResourceReader,\s*mscorlib,"))
					validReader = true;
			}

			return validReader;
		}
	}
}
