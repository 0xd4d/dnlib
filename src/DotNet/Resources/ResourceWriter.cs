// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace dnlib.DotNet.Resources {
	/// <summary>
	/// Writes .NET resources
	/// </summary>
	public sealed class ResourceWriter {
		ModuleDef module;
		BinaryWriter writer;
		ResourceElementSet resources;
		ResourceDataFactory typeCreator;
		Dictionary<IResourceData, UserResourceType> dataToNewType = new Dictionary<IResourceData, UserResourceType>();

		ResourceWriter(ModuleDef module, ResourceDataFactory typeCreator, Stream stream, ResourceElementSet resources) {
			this.module = module;
			this.typeCreator = typeCreator;
			writer = new BinaryWriter(stream);
			this.resources = resources;
		}

		/// <summary>
		/// Write .NET resources
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="stream">Output stream</param>
		/// <param name="resources">.NET resources</param>
		public static void Write(ModuleDef module, Stream stream, ResourceElementSet resources) =>
			new ResourceWriter(module, new ResourceDataFactory(module), stream, resources).Write();

		/// <summary>
		/// Write .NET resources
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="typeCreator">User type factory</param>
		/// <param name="stream">Output stream</param>
		/// <param name="resources">.NET resources</param>
		public static void Write(ModuleDef module, ResourceDataFactory typeCreator, Stream stream, ResourceElementSet resources) =>
			new ResourceWriter(module, typeCreator, stream, resources).Write();

		void Write() {
			if (resources.FormatVersion != 1 && resources.FormatVersion != 2)
				throw new ArgumentException($"Invalid format version: {resources.FormatVersion}", nameof(resources));

			InitializeUserTypes(resources.FormatVersion);

			writer.Write(0xBEEFCACE);
			writer.Write(1);
			WriteReaderType();
			writer.Write(resources.FormatVersion);
			writer.Write(resources.Count);
			writer.Write(typeCreator.Count);
			foreach (var userType in typeCreator.GetSortedTypes())
				writer.Write(userType.Name);
			int extraBytes = 8 - ((int)writer.BaseStream.Position & 7);
			if (extraBytes != 8) {
				for (int i = 0; i < extraBytes; i++)
					writer.Write((byte)'X');
			}

			var nameOffsetStream = new MemoryStream();
			var nameOffsetWriter = new BinaryWriter(nameOffsetStream, Encoding.Unicode);
			var dataStream = new MemoryStream();
			var dataWriter = new ResourceBinaryWriter(dataStream) {
				FormatVersion = resources.FormatVersion,
				ReaderType = resources.ReaderType,
			};
			var hashes = new int[resources.Count];
			var offsets = new int[resources.Count];
			var formatter = new BinaryFormatter(null, new StreamingContext(StreamingContextStates.File | StreamingContextStates.Persistence));
			int index = 0;
			foreach (var info in resources.ResourceElements) {
				offsets[index] = (int)nameOffsetWriter.BaseStream.Position;
				hashes[index] = (int)Hash(info.Name);
				index++;
				nameOffsetWriter.Write(info.Name);
				nameOffsetWriter.Write((int)dataWriter.BaseStream.Position);
				WriteData(dataWriter, info, formatter);
			}

			Array.Sort(hashes, offsets);
			foreach (var hash in hashes)
				writer.Write(hash);
			foreach (var offset in offsets)
				writer.Write(offset);
			writer.Write((int)writer.BaseStream.Position + (int)nameOffsetStream.Length + 4);
			writer.Write(nameOffsetStream.ToArray());
			writer.Write(dataStream.ToArray());
		}

		void WriteData(ResourceBinaryWriter writer, ResourceElement info, IFormatter formatter) {
			var code = GetResourceType(info.ResourceData, writer.FormatVersion);
			writer.Write7BitEncodedInt((int)code);
			info.ResourceData.WriteData(writer, formatter);
		}

		ResourceTypeCode GetResourceType(IResourceData data, int formatVersion) {
			if (formatVersion == 1) {
				if (data.Code == ResourceTypeCode.Null)
					return (ResourceTypeCode)(-1);
				return (ResourceTypeCode)(dataToNewType[data].Code - ResourceTypeCode.UserTypes);
			}

			if (data is BuiltInResourceData)
				return data.Code;

			return dataToNewType[data].Code;
		}

		static uint Hash(string key) {
			uint val = 0x1505;
			foreach (var c in key)
				val = ((val << 5) + val) ^ (uint)c;
			return val;
		}

		void InitializeUserTypes(int formatVersion) {
			foreach (var resource in resources.ResourceElements) {
				UserResourceType newType;
				if (formatVersion == 1 && resource.ResourceData is BuiltInResourceData builtinData) {
					newType = typeCreator.CreateBuiltinResourceType(builtinData.Code);
					if (newType is null)
						throw new NotSupportedException($"Unsupported resource type: {builtinData.Code} in format version 1 resource");
				}
				else if (resource.ResourceData is UserResourceData userData)
					newType = typeCreator.CreateUserResourceType(userData.TypeName);
				else
					continue;
				dataToNewType[resource.ResourceData] = newType;
			}
		}

		void WriteReaderType() {
			var memStream = new MemoryStream();
			var headerWriter = new BinaryWriter(memStream);
			if (resources.ResourceReaderTypeName is not null && resources.ResourceSetTypeName is not null) {
				headerWriter.Write(resources.ResourceReaderTypeName);
				headerWriter.Write(resources.ResourceSetTypeName);
			}
			else {
				var mscorlibFullName = GetMscorlibFullname();
				headerWriter.Write($"System.Resources.ResourceReader, {mscorlibFullName}");
				headerWriter.Write("System.Resources.RuntimeResourceSet");
			}
			writer.Write((int)memStream.Position);
			writer.Write(memStream.ToArray());
		}

		string GetMscorlibFullname() {
			if (module.CorLibTypes.AssemblyRef.Name == "mscorlib")
				return module.CorLibTypes.AssemblyRef.FullName;
			return "mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
		}
	}
}
