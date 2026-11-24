// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
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

		const string PropertiesSource = @"
public class First { public int FirstProperty { get; set; } }
public class Second { public int SecondProperty { get; set; } }
";

		static string[] GetPropertyNames(ModuleDef module, string typeName) =>
			module.Find(typeName, isReflectionName: true)!.Properties.Select(a => a.Name.String).ToArray();

		// A PropertyMap row has a type and the first row of its properties. Its properties end where the
		// properties of the next PropertyMap row start.
		[Fact]
		public void ValidPropertyMapIsRead() {
			var image = CSharpCompiler.Compile(PropertiesSource).Image;
			var patcher = new MetadataPatcher(image);
			Assert.Equal(1U, patcher.GetCell(Table.PropertyMap, 1, "PropertyList"));
			Assert.Equal(2U, patcher.GetCell(Table.PropertyMap, 2, "PropertyList"));

			using (var module = ModuleDefMD.Load(image)) {
				Assert.Equal(new[] { "FirstProperty" }, GetPropertyNames(module, "First"));
				Assert.Equal(new[] { "SecondProperty" }, GetPropertyNames(module, "Second"));
			}
		}

		// Fixed in d4173a88: the CLR reads a property list (and an event list) that starts at row 0 as if it
		// started at row 1. Mono doesn't, the type has no properties then.
		[Fact]
		public void PropertyListThatStartsAtRowZeroStartsAtRowOneLikeInTheClr() {
			var image = new MetadataPatcher(CSharpCompiler.Compile(PropertiesSource).Image).SetCell(Table.PropertyMap, 1, "PropertyList", 0).ToArray();

			using (var module = ModuleDefMD.Load(image))
				Assert.Equal(new[] { "FirstProperty" }, GetPropertyNames(module, "First"));

			using (var module = ModuleDefMD.Load(image, new ModuleCreationOptions { Runtime = CLRRuntimeReaderKind.Mono })) {
				Assert.Empty(GetPropertyNames(module, "First"));
				Assert.Equal(new[] { "SecondProperty" }, GetPropertyNames(module, "Second"));
			}
		}

		// Fixed in a5b4b0bb: Mono ignores the highest byte of a table's row count. The CLR doesn't, but it
		// then doesn't load the file since the tables don't fit in the metadata. dnlib reads what Mono reads.
		[Fact]
		public void HighByteOfARowCountIsIgnoredIfTheTablesDoNotFit() {
			var validImage = CSharpCompiler.Compile(PropertiesSource).Image;
			var patcher = new MetadataPatcher(validImage);
			uint rowCount = patcher.GetRowCount(Table.TypeDef);
			var image = patcher.SetRowCount(Table.TypeDef, rowCount | 0xFF000000).ToArray();

			using (var module = ModuleDefMD.Load(image)) {
				Assert.Equal(rowCount, module.TablesStream.TypeDefTable.Rows);
				Assert.Equal(new[] { "FirstProperty" }, GetPropertyNames(module, "First"));
				Assert.Equal(new[] { "SecondProperty" }, GetPropertyNames(module, "Second"));
			}
		}

		// Fixed in fd386ee8: Mono doesn't check the size of a stream, the CLR doesn't load such a file. The
		// stream is read up to the end of the file.
		[Fact]
		public void StreamThatEndsAfterTheEndOfTheFileIsRead() {
			var validImage = CSharpCompiler.Compile(PropertiesSource).Image;
			var patcher = new MetadataPatcher(validImage);
			uint sizeUpToTheEndOfTheFile = (uint)(validImage.Length - patcher.GetStreamFileOffset("#Blob"));
			var image = patcher.SetStreamSize("#Blob", sizeUpToTheEndOfTheFile + 16).ToArray();

			using (var module = ModuleDefMD.Load(image)) {
				Assert.Equal(sizeUpToTheEndOfTheFile, module.BlobStream.StreamLength);
				// The signatures are in the #Blob stream
				var property = module.Find("First", isReflectionName: true)!.Properties.Single();
				Assert.Equal(ElementType.I4, property.PropertySig!.RetType!.ElementType);
			}
		}

		const string ExplicitImplementationSource = @"
public interface IInterface { void Method(); }
public class First : IInterface { void IInterface.Method() { } }
public class Second { public void Other() { } }
public class Third { public void Another() { } }
";

		// A TypeDef row has the first row of its methods, they end where the methods of the next type start.
		// Fixed in 04040886: if the lists aren't in order, a method is in the list of two types. Reading the
		// list of the second type moves the method to it. That also read which methods it overrides (the
		// MethodImpl rows of its type) while the list was locked and the method still had its old type. It
		// could read the same list again (an exception, or a stack overflow without THREAD_SAFE); in this
		// file it only returned the overrides of the old type.
		[Fact]
		public void MethodWithAnOverrideThatIsInTheMethodListOfTwoTypesIsRead() {
			var patcher = new MetadataPatcher(CSharpCompiler.Compile(ExplicitImplementationSource).Image);
			uint implementation = patcher.FindRow(Table.Method, "Name", "IInterface.Method");
			uint first = patcher.FindRow(Table.TypeDef, "Name", "First");
			uint third = patcher.FindRow(Table.TypeDef, "Name", "Third");
			Assert.Equal(implementation, patcher.GetCell(Table.TypeDef, first, "MethodList"));
			Assert.Equal(patcher.GetRowCount(Table.TypeDef), third);
			// The methods of the last type now start at the implementation, which is still a method of First
			var image = patcher.SetCell(Table.TypeDef, third, "MethodList", implementation).ToArray();

			using (var module = ModuleDefMD.Load(image)) {
				var firstType = module.Find("First", isReflectionName: true)!;
				var thirdType = module.Find("Third", isReflectionName: true)!;
				Assert.Contains(firstType.Methods, a => a.Name == "IInterface.Method");

				// The method moves to the type whose list is read last
				var method = Assert.Single(thirdType.Methods, a => a.Name == "IInterface.Method");
				Assert.Same(thirdType, method.DeclaringType);
				Assert.DoesNotContain(method, firstType.Methods);
				// The MethodImpl row is a row of First
				Assert.Empty(method.Overrides);
			}
		}
	}
}
