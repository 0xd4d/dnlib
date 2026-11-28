// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnlib.IO;
using Xunit;

namespace dnlib.Tests.Resources {
	/// <summary>
	/// The parts of dnlib's <c>.resources</c> API that aren't about the values: the reader the file is for
	/// (<c>System.Resources.ResourceReader</c>, or <c>DeserializingResourceReader</c> of System.Resources.Extensions,
	/// which also stores how a value was serialized), and the text of the resources.
	/// </summary>
	public class ResourceSetApiTests {
		const string UserTypeName = "MyNamespace.MyType, MyAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

		static ModuleDef CreateModule() => new ModuleDefUser("Test.dll");

		static ResourceElementSet WriteAndRead(ModuleDef module, ResourceElementSet set, ResourceDataFactory factory) {
			var stream = new MemoryStream();
			ResourceWriter.Write(module, factory, stream, set);
			return ResourceReader.Read(CreateModule(), ByteArrayDataReaderFactory.CreateReader(stream.ToArray()));
		}

		// A DeserializingResourceReader file stores the format of every serialized value: the reader gets it again
		[Theory]
		[InlineData(SerializationFormat.BinaryFormatter)]
		[InlineData(SerializationFormat.TypeConverterByteArray)]
		[InlineData(SerializationFormat.TypeConverterString)]
		[InlineData(SerializationFormat.ActivatorStream)]
		public void TheDeserializingReadersFileHasTheFormatOfEachValue(SerializationFormat format) {
			var module = CreateModule();
			var factory = new ResourceDataFactory(module);
			var set = ResourceElementSet.CreateForDeserializingResourceReader(new Version(8, 0, 0, 0));
			set.Add(new ResourceElement("value", factory.CreateSerialized(new byte[] { 1, 2, 3 }, format, factory.CreateUserResourceType(UserTypeName))));
			set.Add(new ResourceElement("text", factory.Create("text")));

			var read = WriteAndRead(module, set, factory);

			Assert.Equal(ResourceReaderType.DeserializingResourceReader, read.ReaderType);
			Assert.StartsWith("System.Resources.Extensions.DeserializingResourceReader, System.Resources.Extensions, Version=8.0.0.0", read.ResourceReaderTypeName);
			var value = Assert.IsType<BinaryResourceData>(read.ResourceElements.Single(a => a.Name == "value").ResourceData);
			Assert.Equal(format, value.Format);
			Assert.Equal(new byte[] { 1, 2, 3 }, value.Data);
			Assert.Equal(UserTypeName, value.TypeName);
			Assert.Equal(ResourceTypeCode.UserTypes, value.Code);
		}

		// The normal reader only knows values that BinaryFormatter serialized
		[Fact]
		public void TheNormalReaderOnlyHasBinaryFormatterValues() {
			var module = CreateModule();
			var factory = new ResourceDataFactory(module);
			var set = ResourceElementSet.CreateForResourceReader(module);
			set.Add(new ResourceElement("value", factory.CreateSerialized(new byte[] { 1 }, SerializationFormat.TypeConverterString, factory.CreateUserResourceType(UserTypeName))));

			Assert.Throws<NotSupportedException>(() => ResourceWriter.Write(module, new MemoryStream(), set));
		}

		[Fact]
		public void ASetIsForTheReaderOfItsMscorlibVersion() {
			var set = ResourceElementSet.CreateForResourceReader(new Version(2, 0, 0, 0), formatVersion: 1);

			Assert.Equal("System.Resources.ResourceReader, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", set.ResourceReaderTypeName);
			Assert.Equal("System.Resources.RuntimeResourceSet", set.ResourceSetTypeName);
			Assert.Equal(1, set.FormatVersion);
			Assert.Equal(ResourceReaderType.ResourceReader, set.ReaderType);
			// A module whose core library isn't mscorlib gets the mscorlib of .NET Framework 4
			var coreModule = new ModuleDefUser("Test.dll", null, new AssemblyRefUser("System.Private.CoreLib", new Version(8, 0, 0, 0)));
			Assert.Contains("mscorlib, Version=4.0.0.0", ResourceElementSet.CreateForResourceReader(coreModule).ResourceReaderTypeName);
		}

		// A clone has the same reader and format, but not the resources
		[Fact]
		public void ACloneHasTheReaderOfTheSet() {
			var module = CreateModule();
			var set = ResourceElementSet.CreateForDeserializingResourceReader(new Version(8, 0, 0, 0), formatVersion: 2);
			set.Add(new ResourceElement("text", new ResourceDataFactory(module).Create("text")));

			var clone = set.Clone();

			Assert.Equal(set.ResourceReaderTypeName, clone.ResourceReaderTypeName);
			Assert.Equal(set.ResourceSetTypeName, clone.ResourceSetTypeName);
			Assert.Equal(set.ReaderType, clone.ReaderType);
			Assert.Equal(set.FormatVersion, clone.FormatVersion);
			Assert.Equal(0, clone.Count);
		}

		// The text of a resource shows its type and its value
		[Fact]
		public void ResourcesAreShownWithTheirValues() {
			var factory = new ResourceDataFactory(CreateModule());
			var userType = factory.CreateUserResourceType(UserTypeName);

			Assert.Equal("null", factory.CreateNull().ToString());
			Assert.Equal("String: 'text'", factory.Create("text").ToString());
			Assert.Equal("Int32: '5'", factory.Create(5).ToString());
			Assert.Equal("ByteArray: Length: 3", factory.Create(new byte[3]).ToString());
			Assert.Equal("Stream: Length: 2", factory.CreateStream(new byte[2]).ToString());
			Assert.Equal("Binary: Length: 2 Format: BinaryFormatter", factory.CreateSerialized(new byte[2], SerializationFormat.BinaryFormatter, userType).ToString());
			Assert.Equal($"40 {UserTypeName}", userType.ToString());
			Assert.Equal("N: name, V: Int32: '5'", new ResourceElement("name", factory.Create(5)).ToString());
		}

		// The type of a value that BinaryFormatter serialized is in the data. .NET has no BinaryFormatter anymore,
		// so the type can't be found there.
		[Fact]
		public void TheTypeOfABinaryFormatterValueIsInItsData() {
			var factory = new ResourceDataFactory(CreateModule());
			byte[]? serialized = null;
#pragma warning disable SYSLIB0011 // BinaryFormatter is obsolete
			try {
				var stream = new MemoryStream();
				new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter().Serialize(stream, new Version(1, 2));
				serialized = stream.ToArray();
			}
			catch (NotSupportedException) {
			}
#pragma warning restore SYSLIB0011

			if (serialized is null) {
				Assert.Throws<ApplicationException>(() => factory.CreateBinaryFormatterSerialized(new byte[] { 0 }));
				return;
			}
			var data = factory.CreateBinaryFormatterSerialized(serialized);
			Assert.StartsWith("System.Version, mscorlib", data.TypeName);
			Assert.Equal(SerializationFormat.BinaryFormatter, data.Format);
		}
	}
}
