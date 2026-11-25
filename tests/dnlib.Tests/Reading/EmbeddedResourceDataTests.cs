// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// The data of all embedded resources is in one block of the file (the "Resources" directory of the .NET
	/// header). The Offset column of a ManifestResource row is the offset of the resource in that block: a
	/// 32-bit length and then the data. A resource whose offset or length isn't in the block has no data: dnlib
	/// returns an empty resource, and the CLR returns nothing or fails.
	/// </summary>
	public class EmbeddedResourceDataTests {
		const string ResourceName = "My.Resource";
		static readonly byte[] resourceContent = { 1, 2, 3, 4, 5 };

		static readonly byte[] validImage = CompileWithResource();

		static byte[] CompileWithResource() {
			var options = new CSharpCompilerOptions();
			options.Resources.Add(ResourceName, resourceContent);
			return CSharpCompiler.Compile("public class C { }", options).Image;
		}

		// The size of the block with all resources, and where it is in the file
		static void GetResourcesBlock(out uint fileOffset, out uint size) {
			using (var module = ModuleDefMD.Load(validImage)) {
				var directory = module.Metadata.ImageCor20Header.Resources;
				fileOffset = (uint)module.Metadata.PEImage.ToFileOffset(directory.VirtualAddress);
				size = directory.Size;
			}
		}

		static byte[] ReadWithDnlib(byte[] image) {
			using (var module = ModuleDefMD.Load(image)) {
				var resource = Assert.IsAssignableFrom<EmbeddedResource>(Assert.Single(module.Resources));
				Assert.Equal(ResourceName, resource.Name);
				Assert.Equal(resource.CreateReader().ToArray().Length, (int)resource.Length);
				return resource.CreateReader().ToArray();
			}
		}

		// What the runtime returns (an empty stream if the length is 0), or null if it returns no stream or throws
		static byte[]? ReadWithTheRuntime(byte[] image) {
			using (var loaded = new LoadedAssembly(image)) {
				try {
					using (var stream = loaded.Assembly.GetManifestResourceStream(ResourceName)) {
						if (stream is null)
							return null;
						var data = new MemoryStream();
						stream.CopyTo(data);
						return data.ToArray();
					}
				}
				catch (Exception ex) when (ex is BadImageFormatException || ex is IOException || ex is ArgumentException || ex is NotSupportedException) {
					return null;
				}
			}
		}

		static void AssertHasNoData(byte[] image) {
			Assert.Empty(ReadWithDnlib(image));
			// Checked with .NET only: Mono doesn't check the length
			if (typeof(object).Assembly.GetName().Name == "System.Private.CoreLib")
				Assert.Empty(ReadWithTheRuntime(image) ?? Array.Empty<byte>());

			// The module can be written: the resource is then an empty resource
			var written = RoundTrip.CheckWithNewMetadata(image);
			Assert.Empty(ReadWithDnlib(written));
		}

		[Fact]
		public void ValidResourceIsReadByDnlibAndTheRuntime() {
			GetResourcesBlock(out _, out uint size);

			Assert.Equal(resourceContent, ReadWithDnlib(validImage));
			Assert.Equal(resourceContent, ReadWithTheRuntime(validImage));
			Assert.InRange(size, 4U + (uint)resourceContent.Length, 16U);
		}

		public static TheoryData<string> InvalidOffsets => new TheoryData<string> {
			"its length is the last 2 bytes of the block", "the end of the block", "after the block", "after the file", "0xFFFFFFFF",
		};

		[Theory, MemberData(nameof(InvalidOffsets))]
		public void ResourceWithAnOffsetThatIsNotInTheBlockHasNoData(string where) {
			GetResourcesBlock(out _, out uint size);
			uint offset = where switch {
				"its length is the last 2 bytes of the block" => size - 2,
				"the end of the block" => size,
				"after the block" => size + 0x100,
				"after the file" => (uint)validImage.Length,
				"0xFFFFFFFF" => uint.MaxValue,
				_ => throw new ArgumentException(where),
			};
			var image = new MetadataPatcher(validImage).SetCell(Table.ManifestResource, 1, "Offset", offset).ToArray();

			AssertHasNoData(image);
		}

		static byte[] WithLength(uint length) {
			GetResourcesBlock(out uint fileOffset, out _);
			var image = (byte[])validImage.Clone();
			Assert.Equal((uint)resourceContent.Length, BitConverter.ToUInt32(image, (int)fileOffset));
			BitConverter.GetBytes(length).CopyTo(image, (int)fileOffset);
			return image;
		}

		[Theory]
		[InlineData(0U)]
		[InlineData(0x1000U)]
		[InlineData(0x7FFFFFFFU)]
		[InlineData(0xFFFFFFFCU)]
		[InlineData(0xFFFFFFFFU)]
		public void ResourceWithALengthThatIsNotInTheBlockHasNoData(uint length) =>
			AssertHasNoData(WithLength(length));

		[Fact]
		public void ResourceThatEndsOneByteAfterTheBlockHasNoData() {
			GetResourcesBlock(out _, out uint size);

			AssertHasNoData(WithLength(size - 4 + 1));
		}

		// A shorter resource, and one that uses all bytes of the block
		[Fact]
		public void ResourceWithAnotherLengthInTheBlockIsRead() {
			GetResourcesBlock(out _, out uint size);

			var shorter = WithLength(2);
			Assert.Equal(new byte[] { 1, 2 }, ReadWithDnlib(shorter));
			Assert.Equal(new byte[] { 1, 2 }, ReadWithTheRuntime(shorter));

			var whole = WithLength(size - 4);
			var data = ReadWithDnlib(whole);
			Assert.Equal((int)size - 4, data.Length);
			Assert.Equal(data, ReadWithTheRuntime(whole));
			RoundTrip.CheckWithNewMetadata(whole);
		}
	}
}
