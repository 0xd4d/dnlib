// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// A PE file can have a section <c>.mvid</c> with the 16 bytes of the module's MVID, so that a build tool
	/// can read the MVID without reading the metadata (the C# compiler adds it to reference assemblies). The
	/// writer adds the section if <see cref="ModuleWriterOptions.AddMvidSection"/> is set, which is the
	/// default for a module that was read from a file with that section.
	/// </summary>
	public class MvidSectionTests {
		static readonly Guid mvid = new Guid("0f1e2d3c-4b5a-6978-8796-a5b4c3d2e1f0");

		static byte[] Write(ModuleDef module, bool? addMvidSection) {
			var options = new ModuleWriterOptions(module);
			if (addMvidSection is not null)
				options.AddMvidSection = addMvidSection.Value;
			var stream = new MemoryStream();
			module.Write(stream, options);
			return stream.ToArray();
		}

		static byte[] CreateImage(bool addMvidSection) {
			var created = new CreatedModule();
			created.Module.Mvid = mvid;
			return Write(created.Module, addMvidSection);
		}

		// The data of the .mvid section or null if the file has no such section
		static byte[]? GetMvidSection(byte[] image) {
			using (var peReader = new PEReader(new MemoryStream(image))) {
				var sections = peReader.PEHeaders.SectionHeaders.Where(a => a.Name == ".mvid").ToArray();
				if (sections.Length == 0)
					return null;
				var section = Assert.Single(sections);
				// It's the first section, and it's read-only data that can be discarded
				Assert.Equal(".mvid", peReader.PEHeaders.SectionHeaders[0].Name);
				Assert.Equal(SectionCharacteristics.ContainsInitializedData | SectionCharacteristics.MemRead | SectionCharacteristics.MemDiscardable, section.SectionCharacteristics);
				Assert.Equal(16, section.VirtualSize);
				return image.Skip(section.PointerToRawData).Take(section.VirtualSize).ToArray();
			}
		}

		[Fact]
		public void SectionHasTheMvidOfTheModule() {
			var image = CreateImage(addMvidSection: true);

			Assert.Equal(mvid.ToByteArray(), GetMvidSection(image));
			using (var module = ModuleDefMD.Load(image))
				Assert.Equal(mvid, module.Mvid);
		}

		[Fact]
		public void NoSectionIsAddedByDefault() {
			var created = new CreatedModule();
			Assert.False(new ModuleWriterOptions(created.Module).AddMvidSection);

			Assert.Null(GetMvidSection(created.Write()));
			Assert.Null(GetMvidSection(CreateImage(addMvidSection: false)));
		}

		[Fact]
		public void SectionIsKeptIfTheFileHadOne() {
			using (var module = ModuleDefMD.Load(CreateImage(addMvidSection: true))) {
				Assert.True(new ModuleWriterOptions(module).AddMvidSection);

				Assert.Equal(mvid.ToByteArray(), GetMvidSection(Write(module, addMvidSection: null)));
				Assert.Null(GetMvidSection(Write(module, addMvidSection: false)));

				// It has the MVID that the module has when it's written
				var newMvid = Guid.NewGuid();
				module.Mvid = newMvid;
				Assert.Equal(newMvid.ToByteArray(), GetMvidSection(Write(module, addMvidSection: null)));
			}
			using (var module = ModuleDefMD.Load(CreateImage(addMvidSection: false)))
				Assert.False(new ModuleWriterOptions(module).AddMvidSection);
		}

		[Fact]
		public void ModuleWithoutAnMvidHasASectionOfZeroes() {
			var created = new CreatedModule();
			created.Module.Mvid = null;

			Assert.Equal(new byte[16], GetMvidSection(Write(created.Module, addMvidSection: true)));
		}

		// The C# compiler's reference assemblies have the section
		[Fact]
		public void SectionOfACompiledReferenceAssemblyIsKept() {
			var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("RefAssembly",
				new[] { Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText("public class C { public int M() => 1; }", cancellationToken: TestContext.Current.CancellationToken) },
				new[] { Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
				new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary));
			var stream = new MemoryStream();
			var result = compilation.Emit(stream, options: new Microsoft.CodeAnalysis.Emit.EmitOptions(metadataOnly: true, includePrivateMembers: false), cancellationToken: TestContext.Current.CancellationToken);
			Assert.True(result.Success);
			var image = stream.ToArray();

			using (var module = ModuleDefMD.Load(image)) {
				Assert.Equal(module.Mvid!.Value.ToByteArray(), GetMvidSection(image));
				Assert.True(new ModuleWriterOptions(module).AddMvidSection);
				Assert.Equal(module.Mvid.Value.ToByteArray(), GetMvidSection(Write(module, addMvidSection: null)));
			}
		}
	}
}
