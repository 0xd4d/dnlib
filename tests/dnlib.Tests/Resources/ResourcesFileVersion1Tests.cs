// dnlib: See LICENSE.txt for more info

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnlib.IO;
using Xunit;
using SystemResourceReader = System.Resources.ResourceReader;

namespace dnlib.Tests.Resources {
	/// <summary>
	/// .NET Framework 1.x wrote version 1 of the .resources file format. It has no type codes: the file has a
	/// list of type names, and every value starts with the index of its type in that list. No compiler writes
	/// this version anymore but .NET still reads it, so the tests create such a file byte by byte and .NET's own
	/// reader checks that it's a valid file. dnlib also writes this version: a file that was read keeps it.
	/// </summary>
	public class ResourcesFileVersion1Tests {
		const string CoreLibrary = ", mscorlib, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";

		// The values of every type that version 1 stores without a serializer
		static readonly Dictionary<string, object?> values = new Dictionary<string, object?> {
			{ "String", "text" },
			{ "Int32", -123456 },
			{ "Byte", (byte)200 },
			{ "SByte", (sbyte)-100 },
			{ "Int16", (short)-12345 },
			{ "Int64", -1234567890123L },
			{ "UInt16", (ushort)54321 },
			{ "UInt32", 4000000000U },
			{ "UInt64", 18000000000000000000UL },
			{ "Single", 1.5f },
			{ "Double", -2.25 },
			{ "DateTime", new DateTime(2003, 4, 5, 6, 7, 8) },
			{ "TimeSpan", new TimeSpan(1, 2, 3, 4) },
			{ "Decimal", 12345.6789m },
			{ "Null", null },
		};

		static void Write7BitEncodedInt(BinaryWriter writer, int value) {
			uint rest = (uint)value;
			while (rest >= 0x80) {
				writer.Write((byte)(rest | 0x80));
				rest >>= 7;
			}
			writer.Write((byte)rest);
		}

		static void WriteValue(BinaryWriter writer, object value) {
			switch (value) {
			case string v: writer.Write(v); break;
			case int v: writer.Write(v); break;
			case byte v: writer.Write(v); break;
			case sbyte v: writer.Write(v); break;
			case short v: writer.Write(v); break;
			case long v: writer.Write(v); break;
			case ushort v: writer.Write(v); break;
			case uint v: writer.Write(v); break;
			case ulong v: writer.Write(v); break;
			case float v: writer.Write(v); break;
			case double v: writer.Write(v); break;
			case DateTime v: writer.Write(v.Ticks); break;
			case TimeSpan v: writer.Write(v.Ticks); break;
			case decimal v: writer.Write(v); break;
			// The data of a type that needs a serializer
			case byte[] v: writer.Write(v); break;
			default: throw new ArgumentException(value.GetType().FullName);
			}
		}

		// The hash of a resource name: the file has the names sorted by it
		static int GetHash(string name) {
			uint hash = 5381;
			foreach (char c in name)
				hash = ((hash << 5) + hash) ^ c;
			return (int)hash;
		}

		/// <summary>
		/// Creates a version 1 file. A <c>byte[]</c> value is the serialized data of an object of the type
		/// <paramref name="serializedTypeName"/>.
		/// </summary>
		static byte[] CreateVersion1File(Dictionary<string, object?> resources, string? serializedTypeName = null, int formatVersion = 1) {
			var sortedNames = resources.Keys.OrderBy(GetHash).ToList();
			var typeNames = new List<string>();
			int GetTypeIndex(object value) {
				var typeName = value is byte[] ? serializedTypeName! : value.GetType().FullName + CoreLibrary;
				if (!typeNames.Contains(typeName))
					typeNames.Add(typeName);
				return typeNames.IndexOf(typeName);
			}

			// The values: the index of the type (-1: null) and the value
			var data = new MemoryStream();
			var dataWriter = new BinaryWriter(data);
			var dataOffsets = new Dictionary<string, int>();
			foreach (var name in sortedNames) {
				dataOffsets.Add(name, (int)data.Length);
				var value = resources[name];
				Write7BitEncodedInt(dataWriter, value is null ? -1 : GetTypeIndex(value));
				if (value is not null)
					WriteValue(dataWriter, value);
			}

			// The names: the length in bytes, the UTF-16 name, and where its value is
			var names = new MemoryStream();
			var namesWriter = new BinaryWriter(names);
			var nameOffsets = new List<int>();
			foreach (var name in sortedNames) {
				nameOffsets.Add((int)names.Length);
				var nameBytes = Encoding.Unicode.GetBytes(name);
				Write7BitEncodedInt(namesWriter, nameBytes.Length);
				namesWriter.Write(nameBytes);
				namesWriter.Write(dataOffsets[name]);
			}

			var file = new MemoryStream();
			var writer = new BinaryWriter(file);
			// The header that says which class reads the file
			var readerTypes = new MemoryStream();
			var readerTypesWriter = new BinaryWriter(readerTypes);
			readerTypesWriter.Write("System.Resources.ResourceReader" + CoreLibrary);
			readerTypesWriter.Write("System.Resources.RuntimeResourceSet" + CoreLibrary);
			writer.Write(0xBEEFCACE);
			writer.Write(1);
			writer.Write((int)readerTypes.Length);
			writer.Write(readerTypes.ToArray());

			writer.Write(formatVersion);
			writer.Write(resources.Count);
			writer.Write(typeNames.Count);
			foreach (var typeName in typeNames)
				writer.Write(typeName);
			while (file.Length % 8 != 0)
				writer.Write((byte)'P');
			foreach (var name in sortedNames)
				writer.Write(GetHash(name));
			foreach (var nameOffset in nameOffsets)
				writer.Write(nameOffset);
			// Where the values start: after this number and the names
			writer.Write((int)(file.Length + 4 + names.Length));
			writer.Write(names.ToArray());
			writer.Write(data.ToArray());
			return file.ToArray();
		}

		static ResourceElementSet ReadWithDnlib(byte[] file) =>
			ResourceReader.Read(new ModuleDefUser("Test.dll"), ByteArrayDataReaderFactory.CreateReader(file));

		[Fact]
		public void DotNetReadsTheFileThatTheTestsCreate() {
			var read = new Dictionary<string, object?>();
			using (var reader = new SystemResourceReader(new MemoryStream(CreateVersion1File(values)))) {
				foreach (DictionaryEntry entry in reader)
					read.Add((string)entry.Key, entry.Value);
			}

			Assert.Equal(values.OrderBy(a => a.Key), read.OrderBy(a => a.Key));
		}

		[Fact]
		public void ValuesOfEveryTypeAreRead() {
			var set = ReadWithDnlib(CreateVersion1File(values));

			Assert.Equal(1, set.FormatVersion);
			var read = set.ResourceElements.ToDictionary(a => a.Name, a => Assert.IsType<BuiltInResourceData>(a.ResourceData).Data);
			Assert.Equal(values.OrderBy(a => a.Key), read.OrderBy(a => a.Key));
			// The value has the same type too, eg. a short isn't read as an int
			foreach (var pair in values)
				Assert.Equal(pair.Value?.GetType(), read[pair.Key]?.GetType());
		}

		// A value of any other type is the output of the BinaryFormatter: dnlib returns the bytes and the type
		[Fact]
		public void ValueOfAnotherTypeIsReadAsItsSerializedData() {
			const string typeName = "MyNamespace.MyType, MyAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
			var serialized = new byte[] { 1, 2, 3, 4, 5 };
			var resources = new Dictionary<string, object?> { { "Number", 5 }, { "Object", serialized }, { "Text", "abc" } };

			var set = ReadWithDnlib(CreateVersion1File(resources, typeName));

			var read = set.ResourceElements.ToDictionary(a => a.Name, a => a.ResourceData);
			var data = Assert.IsType<BinaryResourceData>(read["Object"]);
			Assert.Equal(serialized, data.Data);
			Assert.Equal(typeName, data.TypeName);
			Assert.Equal(5, Assert.IsType<BuiltInResourceData>(read["Number"]).Data);
			Assert.Equal("abc", Assert.IsType<BuiltInResourceData>(read["Text"]).Data);
		}

		[Fact]
		public void ValueWithATypeIndexThatDoesNotExistIsAnError() {
			var file = CreateVersion1File(new Dictionary<string, object?> { { "Number", 5 } });
			// The file ends with the value: the type index 0 and the four bytes of the number
			Assert.Equal(0, file[file.Length - 5]);
			file[file.Length - 5] = 1;

			var exception = Assert.Throws<ResourceReaderException>(() => ReadWithDnlib(file));
			Assert.Contains("Invalid resource type index", exception.Message);
		}

		[Theory, InlineData(0), InlineData(3)]
		public void OtherVersionsAreAnError(int formatVersion) {
			var file = CreateVersion1File(values, formatVersion: formatVersion);

			var exception = Assert.Throws<ResourceReaderException>(() => ReadWithDnlib(file));
			Assert.Contains("Invalid resource version", exception.Message);
		}

		static Dictionary<string, object?> ReadWithDotNet(byte[] file) {
			var read = new Dictionary<string, object?>();
			using (var reader = new SystemResourceReader(new MemoryStream(file))) {
				foreach (DictionaryEntry entry in reader)
					read.Add((string)entry.Key, entry.Value);
			}
			return read;
		}

		// The version number of a .resources file: it's after the two type names of the header
		static int GetFormatVersion(byte[] file) => ReadWithDnlib(file).FormatVersion;

		[Fact]
		public void FileThatWasReadIsWrittenAsVersion1Again() {
			var module = new ModuleDefUser("Test.dll");
			var set = ResourceReader.Read(module, ByteArrayDataReaderFactory.CreateReader(CreateVersion1File(values)));
			var stream = new MemoryStream();

			ResourceWriter.Write(module, stream, set);

			var written = stream.ToArray();
			Assert.Equal(1, GetFormatVersion(written));
			Assert.Equal(values.OrderBy(a => a.Key), ReadWithDotNet(written).OrderBy(a => a.Key));
			var readAgain = ReadWithDnlib(written).ResourceElements.ToDictionary(a => a.Name, a => Assert.IsType<BuiltInResourceData>(a.ResourceData).Data);
			Assert.Equal(values.OrderBy(a => a.Key), readAgain.OrderBy(a => a.Key));
		}

		[Fact]
		public void NewVersion1FileCanBeCreated() {
			var module = new ModuleDefUser("Test.dll");
			var factory = new ResourceDataFactory(module);
			var set = ResourceElementSet.CreateForResourceReader(new Version(1, 0, 5000, 0), formatVersion: 1);
			set.Add(new ResourceElement("Text", factory.Create("abc")));
			set.Add(new ResourceElement("Number", factory.Create(123)));
			set.Add(new ResourceElement("Nothing", factory.CreateNull()));
			var stream = new MemoryStream();

			ResourceWriter.Write(module, stream, set);

			var written = stream.ToArray();
			Assert.Equal(1, GetFormatVersion(written));
			var read = ReadWithDotNet(written);
			Assert.Equal("abc", read["Text"]);
			Assert.Equal(123, read["Number"]);
			Assert.Null(read["Nothing"]);
		}

		// Version 1 has no Boolean and Char values (a reader would need a serializer for them)
		[Fact]
		public void TypeThatVersion1DoesNotHaveCanNotBeWritten() {
			var module = new ModuleDefUser("Test.dll");
			var set = ResourceElementSet.CreateForResourceReader(new Version(1, 0, 5000, 0), formatVersion: 1);
			set.Add(new ResourceElement("Flag", new ResourceDataFactory(module).Create(true)));

			var exception = Assert.Throws<NotSupportedException>(() => ResourceWriter.Write(module, new MemoryStream(), set));

			Assert.Contains("Boolean", exception.Message);
		}

		[Theory, InlineData(0), InlineData(3)]
		public void OtherVersionsCanNotBeWritten(int formatVersion) {
			var module = new ModuleDefUser("Test.dll");
			var set = ResourceElementSet.CreateForResourceReader(new Version(4, 0, 0, 0), formatVersion);

			Assert.Throws<ArgumentException>(() => ResourceWriter.Write(module, new MemoryStream(), set));
		}
	}
}
