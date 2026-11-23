// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// Invalid or unusual metadata that the CLR accepts, so dnlib must read it the same way. Each test changes
	/// one thing in a valid file with <see cref="MetadataPatcher"/>.
	/// </summary>
	public class PatchedMetadataTests {
		static readonly byte[] resourceContent = { 1, 2, 3, 4, 5 };

		static byte[] CompileWithResource() {
			var options = new CSharpCompilerOptions();
			options.Resources.Add("My.Resource", resourceContent);
			return CSharpCompiler.Compile("public class C { }", options).Image;
		}

		[Fact]
		public void EmbeddedResourceIsRead() {
			using (var module = ModuleDefMD.Load(CompileWithResource())) {
				var resource = Assert.IsAssignableFrom<EmbeddedResource>(Assert.Single(module.Resources));

				Assert.Equal("My.Resource", resource.Name);
				Assert.Equal(resourceContent, resource.CreateReader().ToArray());
				Assert.True(resource.IsPublic);
			}
		}

		// Fixed in 4ce8f4e5: the Implementation column says where the resource is (a file, another assembly).
		// The CLR decodes an invalid value as "no token", which means the resource is in this file.
		[Fact]
		public void ResourceWithAnInvalidImplementationIsAnEmbeddedResource() {
			// The coded token has 2 tag bits, tag 3 doesn't exist
			var image = new MetadataPatcher(CompileWithResource()).SetCell(Table.ManifestResource, 1, "Implementation", 3).ToArray();

			using (var module = ModuleDefMD.Load(image)) {
				var resource = Assert.IsAssignableFrom<EmbeddedResource>(Assert.Single(module.Resources));

				Assert.Equal("My.Resource", resource.Name);
				Assert.Equal(resourceContent, resource.CreateReader().ToArray());
			}
		}

		[Fact]
		public void ResourceInAnotherAssemblyIsAnAssemblyLinkedResource() {
			var validImage = CompileWithResource();
			// Tag 1 = AssemblyRef, rid 1 = the reference to the core library
			Assert.True(CodedToken.Implementation.Encode(new MDToken(Table.AssemblyRef, 1), out uint implementation));
			var image = new MetadataPatcher(validImage).SetCell(Table.ManifestResource, 1, "Implementation", implementation).ToArray();

			using (var module = ModuleDefMD.Load(image)) {
				var resource = Assert.IsAssignableFrom<AssemblyLinkedResource>(Assert.Single(module.Resources));

				Assert.Equal("My.Resource", resource.Name);
				Assert.Same(module.ResolveAssemblyRef(1), resource.Assembly);
			}
		}

		// Fixed in f367ac3f: the CLR only checks that a GUID starts in the #GUID heap
		[Theory, InlineData(15), InlineData(1)]
		public void GuidThatEndsAfterTheGuidHeapIsRead(uint heapSize) {
			var validImage = CSharpCompiler.Compile("public class C { }").Image;
			Guid? expected;
			using (var module = ModuleDefMD.Load(validImage))
				expected = module.Mvid;
			Assert.NotNull(expected);
			// The heap has one GUID (16 bytes), the module's MVID
			var image = new MetadataPatcher(validImage).SetStreamSize("#GUID", heapSize).ToArray();

			using (var module = ModuleDefMD.Load(image))
				Assert.Equal(expected, module.Mvid);
		}

		[Fact]
		public void GuidOutsideTheGuidHeapIsNull() {
			var validImage = CSharpCompiler.Compile("public class C { }").Image;
			var image = new MetadataPatcher(validImage).SetStreamSize("#GUID", 0).ToArray();

			using (var module = ModuleDefMD.Load(image))
				Assert.Null(module.Mvid);
		}

		// Fixed in b16ec6c5: offset 0 of the #Blob heap is the empty blob
		[Fact]
		public void BlobAtOffsetZeroIsEmpty() {
			using (var module = ModuleDefMD.Load(CSharpCompiler.Compile("public class C { }").Image)) {
				Assert.Equal(0U, module.BlobStream.CreateReader(0).Length);
				Assert.True(module.BlobStream.TryCreateReader(0, out var reader));
				Assert.Equal(0U, reader.Length);
				Assert.Empty(module.BlobStream.ReadNoNull(0));
			}
		}

		// Fixed in a3fbb019 (#598): the size of the data is the size of the field's type, and the type of an
		// enum is its underlying type
		[Fact]
		public void InitialValueOfAnEnumFieldIsRead() {
			const string source = @"
.assembly extern mscorlib { }
.assembly Test { }
.module Test.dll
.class public auto ansi sealed MyEnum extends [mscorlib]System.Enum {
	.field public specialname rtspecialname int16 value__
}
.class public abstract sealed auto ansi C extends [mscorlib]System.Object {
	.field public static valuetype MyEnum EnumField at EnumData
	.field public static int32 IntField at IntData
}
.data EnumData = bytearray (34 12)
.data IntData = bytearray (78 56 34 12)
";
			using (var module = ModuleDefMD.Load(IlAssembler.Assemble(source).Image)) {
				var type = module.Find("C", isReflectionName: true)!;

				Assert.Equal(new byte[] { 0x34, 0x12 }, type.FindField("EnumField")!.InitialValue);
				Assert.Equal(new byte[] { 0x78, 0x56, 0x34, 0x12 }, type.FindField("IntField")!.InitialValue);
			}
		}
	}
}
