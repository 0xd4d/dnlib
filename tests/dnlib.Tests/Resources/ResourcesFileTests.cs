// dnlib: See LICENSE.txt for more info

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnlib.IO;
using Xunit;
using SystemResourceReader = System.Resources.ResourceReader;
using SystemResourceWriter = System.Resources.ResourceWriter;

namespace dnlib.Tests.Resources {
	/// <summary>
	/// dnlib's reader and writer of <c>.resources</c> files (the format of <see cref="System.Resources.ResourceManager"/>,
	/// stored as embedded resources in assemblies) checked against .NET's own
	/// <see cref="System.Resources.ResourceWriter"/> and <see cref="System.Resources.ResourceReader"/>.
	/// </summary>
	public class ResourcesFileTests {
		// One resource of every type the format has a type code for. The value of "stream" is the content of
		// the stream, and a null value is the resource "null".
		static readonly Dictionary<string, object?> values = new Dictionary<string, object?> {
			{ "string", "text € \0 end" },
			{ "empty string", "" },
			{ "true", true },
			{ "false", false },
			{ "char", '€' },
			{ "byte", (byte)0xFE },
			{ "sbyte", (sbyte)-2 },
			{ "short", (short)-12345 },
			{ "ushort", (ushort)54321 },
			{ "int", int.MinValue },
			{ "uint", uint.MaxValue },
			{ "long", long.MinValue },
			{ "ulong", ulong.MaxValue },
			{ "float", 1.5f },
			{ "double", -2.25 },
			{ "decimal", -79228162514264337593543950335m },
			{ "DateTime", new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc) },
			{ "TimeSpan", new TimeSpan(1, 2, 3, 4, 5) },
			{ "byte array", new byte[] { 1, 2, 3, 0, 255 } },
			{ "empty byte array", new byte[0] },
			{ "null", null },
		};
		static readonly byte[] streamContent = { 9, 8, 7, 6 };
		const string StreamName = "stream";

		static ModuleDef CreateModule() => new ModuleDefUser("Test.dll");

		// Written by .NET
		static byte[] WriteWithSystemWriter() {
			var stream = new MemoryStream();
			using (var writer = new SystemResourceWriter(stream)) {
				foreach (var pair in values)
					writer.AddResource(pair.Key, pair.Value);
				writer.AddResource(StreamName, new MemoryStream(streamContent));
				writer.Generate();
				return stream.ToArray();
			}
		}

		// Written by dnlib
		static byte[] WriteWithDnlib() {
			var module = CreateModule();
			var factory = new ResourceDataFactory(module);
			var set = ResourceElementSet.CreateForResourceReader(module);
			foreach (var pair in values)
				set.Add(new ResourceElement(pair.Key, CreateData(factory, pair.Value)));
			set.Add(new ResourceElement(StreamName, factory.CreateStream(streamContent)));

			var stream = new MemoryStream();
			ResourceWriter.Write(module, stream, set);
			return stream.ToArray();
		}

		static IResourceData CreateData(ResourceDataFactory factory, object? value) => value switch {
			null => factory.CreateNull(),
			string v => factory.Create(v),
			bool v => factory.Create(v),
			char v => factory.Create(v),
			byte v => factory.Create(v),
			sbyte v => factory.Create(v),
			short v => factory.Create(v),
			ushort v => factory.Create(v),
			int v => factory.Create(v),
			uint v => factory.Create(v),
			long v => factory.Create(v),
			ulong v => factory.Create(v),
			float v => factory.Create(v),
			double v => factory.Create(v),
			decimal v => factory.Create(v),
			DateTime v => factory.Create(v),
			TimeSpan v => factory.Create(v),
			byte[] v => factory.Create(v),
			_ => throw new ArgumentException("Unknown type: " + value.GetType()),
		};

		static Dictionary<string, IResourceData> ReadWithDnlib(byte[] file) {
			var set = ResourceReader.Read(CreateModule(), ByteArrayDataReaderFactory.CreateReader(file));
			return set.ResourceElements.ToDictionary(a => a.Name, a => a.ResourceData);
		}

		static Dictionary<string, object?> ReadWithSystemReader(byte[] file) {
			var result = new Dictionary<string, object?>();
			using (var reader = new SystemResourceReader(new MemoryStream(file))) {
				foreach (DictionaryEntry entry in reader) {
					var value = entry.Value;
					// A stream resource is returned as a stream
					if (value is Stream stream) {
						var content = new MemoryStream();
						stream.CopyTo(content);
						value = content.ToArray();
					}
					result.Add((string)entry.Key, value);
				}
			}
			return result;
		}

		static void AssertAllValuesWereRead(Dictionary<string, IResourceData> resources) {
			Assert.Equal(values.Count + 1, resources.Count);
			foreach (var pair in values) {
				var data = Assert.IsType<BuiltInResourceData>(resources[pair.Key]);
				Assert.Equal(pair.Value, data.Data);
				// The value has the same type too, eg. a short isn't read as an int
				Assert.Equal(pair.Value?.GetType(), data.Data?.GetType());
			}
			var streamData = Assert.IsType<BuiltInResourceData>(resources[StreamName]);
			Assert.Equal(ResourceTypeCode.Stream, streamData.Code);
			Assert.Equal(streamContent, streamData.Data);
		}

		[Fact]
		public void DnlibReadsWhatDotNetWrote() =>
			AssertAllValuesWereRead(ReadWithDnlib(WriteWithSystemWriter()));

		[Fact]
		public void DotNetReadsWhatDnlibWrote() {
			var resources = ReadWithSystemReader(WriteWithDnlib());

			Assert.Equal(values.Count + 1, resources.Count);
			foreach (var pair in values) {
				Assert.Equal(pair.Value, resources[pair.Key]);
				Assert.Equal(pair.Value?.GetType(), resources[pair.Key]?.GetType());
			}
			Assert.Equal(streamContent, resources[StreamName]);
		}

		[Fact]
		public void DnlibReadsWhatDnlibWrote() =>
			AssertAllValuesWereRead(ReadWithDnlib(WriteWithDnlib()));

		[Fact]
		public void FileWrittenByDotNetSurvivesDnlibReadAndWrite() {
			var module = CreateModule();
			var set = ResourceReader.Read(module, ByteArrayDataReaderFactory.CreateReader(WriteWithSystemWriter()));
			var stream = new MemoryStream();
			ResourceWriter.Write(module, stream, set);

			var resources = ReadWithSystemReader(stream.ToArray());
			foreach (var pair in values)
				Assert.Equal(pair.Value, resources[pair.Key]);
			Assert.Equal(streamContent, resources[StreamName]);
		}

		[Fact]
		public void ResourceTypeCodesAreTheOnesOfTheFormat() {
			var resources = ReadWithDnlib(WriteWithSystemWriter());

			Assert.Equal(ResourceTypeCode.Null, resources["null"].Code);
			Assert.Equal(ResourceTypeCode.String, resources["string"].Code);
			Assert.Equal(ResourceTypeCode.Boolean, resources["true"].Code);
			Assert.Equal(ResourceTypeCode.Char, resources["char"].Code);
			Assert.Equal(ResourceTypeCode.Byte, resources["byte"].Code);
			Assert.Equal(ResourceTypeCode.SByte, resources["sbyte"].Code);
			Assert.Equal(ResourceTypeCode.Int16, resources["short"].Code);
			Assert.Equal(ResourceTypeCode.UInt16, resources["ushort"].Code);
			Assert.Equal(ResourceTypeCode.Int32, resources["int"].Code);
			Assert.Equal(ResourceTypeCode.UInt32, resources["uint"].Code);
			Assert.Equal(ResourceTypeCode.Int64, resources["long"].Code);
			Assert.Equal(ResourceTypeCode.UInt64, resources["ulong"].Code);
			Assert.Equal(ResourceTypeCode.Single, resources["float"].Code);
			Assert.Equal(ResourceTypeCode.Double, resources["double"].Code);
			Assert.Equal(ResourceTypeCode.Decimal, resources["decimal"].Code);
			Assert.Equal(ResourceTypeCode.DateTime, resources["DateTime"].Code);
			Assert.Equal(ResourceTypeCode.TimeSpan, resources["TimeSpan"].Code);
			Assert.Equal(ResourceTypeCode.ByteArray, resources["byte array"].Code);
		}

		[Fact]
		public void EmptyFile() {
			var stream = new MemoryStream();
			using (var writer = new SystemResourceWriter(stream)) {
				writer.Generate();
				Assert.Empty(ReadWithDnlib(stream.ToArray()));
			}

			var module = CreateModule();
			var written = new MemoryStream();
			ResourceWriter.Write(module, written, ResourceElementSet.CreateForResourceReader(module));
			Assert.Empty(ReadWithSystemReader(written.ToArray()));
		}

		[Fact]
		public void CouldBeResourcesFileChecksTheSignature() {
			Assert.True(ResourceReader.CouldBeResourcesFile(ByteArrayDataReaderFactory.CreateReader(WriteWithSystemWriter())));
			Assert.False(ResourceReader.CouldBeResourcesFile(ByteArrayDataReaderFactory.CreateReader(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })));
			Assert.False(ResourceReader.CouldBeResourcesFile(ByteArrayDataReaderFactory.CreateReader(new byte[0])));
		}

		// A file that was cut off anywhere must be read, or rejected with ResourceReaderException
		[Fact]
		public void TruncatedFileThrowsOnlyResourceReaderException() {
			var file = WriteWithSystemWriter();
			for (int length = 0; length < file.Length; length++) {
				var truncated = new byte[length];
				Array.Copy(file, truncated, length);
				try {
					ReadWithDnlib(truncated);
				}
				catch (ResourceReaderException) {
				}
				catch (Exception ex) {
					Assert.Fail($"A file truncated to {length} of {file.Length} bytes threw {ex.GetType().Name}: {ex.Message}");
				}
			}
		}

		// Every byte of the file is changed, one at a time. The file is then read or rejected, as above.
		[Fact]
		public void CorruptedFileThrowsOnlyResourceReaderException() {
			var file = WriteWithSystemWriter();
			for (int offset = 0; offset < file.Length; offset++) {
				foreach (byte value in new byte[] { 0x00, 0x7F, 0x80, 0xFF }) {
					if (file[offset] == value)
						continue;
					var corrupted = (byte[])file.Clone();
					corrupted[offset] = value;
					try {
						ReadWithDnlib(corrupted);
					}
					catch (ResourceReaderException) {
					}
					catch (Exception ex) {
						Assert.Fail($"A file with byte 0x{value:X2} at offset {offset} threw {ex.GetType().Name}: {ex.Message}");
					}
				}
			}
		}

		const string UserTypeName = "MyNamespace.MyType, MyAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

		// Writes resources whose values are objects of a type that isn't built in (a "user type"). The data
		// is what a serializer wrote, dnlib doesn't read it.
		static byte[] WriteUserTypeResources(params (string name, byte[] data)[] resources) {
			var module = CreateModule();
			var factory = new ResourceDataFactory(module);
			var type = factory.CreateUserResourceType(UserTypeName);
			var set = ResourceElementSet.CreateForResourceReader(module);
			foreach (var resource in resources)
				set.Add(new ResourceElement(resource.name, factory.CreateSerialized(resource.data, SerializationFormat.BinaryFormatter, type)));

			var stream = new MemoryStream();
			ResourceWriter.Write(module, stream, set);
			return stream.ToArray();
		}

		[Fact]
		public void UserTypeResourcesAreReadAsTheirData() {
			var file = WriteUserTypeResources(("first", new byte[] { 1, 2, 3 }), ("second", new byte[] { 4, 5 }));

			var resources = ReadWithDnlib(file);

			var first = Assert.IsType<BinaryResourceData>(resources["first"]);
			Assert.Equal(new byte[] { 1, 2, 3 }, first.Data);
			Assert.Equal(UserTypeName, first.TypeName);
			Assert.Equal(new byte[] { 4, 5 }, Assert.IsType<BinaryResourceData>(resources["second"]).Data);
		}

		// Fixed in e63ec99d: the factory returned the same type twice, and the writer wrote it twice
		[Fact]
		public void UserTypeIsCreatedOnce() {
			var factory = new ResourceDataFactory(CreateModule());

			var type = factory.CreateUserResourceType(UserTypeName);

			Assert.Same(type, factory.CreateUserResourceType(UserTypeName));
			Assert.Equal(1, factory.Count);
			Assert.Same(type, Assert.Single(factory.GetSortedTypes()));
			Assert.Equal(ResourceTypeCode.UserTypes, type.Code);
			Assert.Equal(ResourceTypeCode.UserTypes + 1, factory.CreateUserResourceType("Other.Type, MyAssembly").Code);
		}

		// The file stores for every resource its name (UTF-16) followed by the offset of its data (4 bytes).
		// Returns where that offset is in the file.
		static int FindDataOffsetField(byte[] file, string resourceName) {
			var nameBytes = System.Text.Encoding.Unicode.GetBytes(resourceName);
			for (int i = 0; i + nameBytes.Length <= file.Length; i++) {
				if (file.Skip(i).Take(nameBytes.Length).SequenceEqual(nameBytes))
					return i + nameBytes.Length;
			}
			throw new InvalidOperationException("The file has no resource with the name " + resourceName);
		}

		// Fixed in 4eec887c and 5c599f17: the size of a user type's data isn't stored, it's what's between its
		// offset and the next resource's offset. If two resources had the same offset, that was 0 bytes for one
		// of them, and the whole file was rejected.
		[Fact]
		public void TwoUserTypeResourcesCanUseTheSameData() {
			var file = WriteUserTypeResources(("first", new byte[] { 1, 2, 3 }), ("second", new byte[] { 4, 5 }), ("third", new byte[] { 6 }));
			int firstOffsetField = FindDataOffsetField(file, "first");
			int secondOffsetField = FindDataOffsetField(file, "second");
			Assert.NotEqual(BitConverter.ToInt32(file, firstOffsetField), BitConverter.ToInt32(file, secondOffsetField));
			// "second" now uses the data of "first"
			Array.Copy(file, firstOffsetField, file, secondOffsetField, 4);

			var resources = ReadWithDnlib(file);

			Assert.Equal(3, resources.Count);
			var first = Assert.IsType<BinaryResourceData>(resources["first"]).Data;
			var second = Assert.IsType<BinaryResourceData>(resources["second"]).Data;
			Assert.Equal(first, second);
			// The data ends where the next resource's data starts. Nothing uses the old data of "second"
			// anymore, so it can be part of it.
			Assert.Equal(new byte[] { 1, 2, 3 }, first.Take(3));
		}
	}
}
