// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Pdb;
using dnlib.TestUtils;
using dnlib.TestUtils.Dumping;
using Xunit;

namespace dnlib.Tests.RoundTrips {
	/// <summary>
	/// A program that the Visual Basic compiler created (<c>Fixtures/Sources/VbFeatures.vb</c>): its metadata
	/// and debug info have things the C# compiler never writes
	/// </summary>
	public class VisualBasicTests {
		static CompiledAssembly Compiled => Fixtures.Fixtures.VbFeatures;

		[Fact]
		public void OriginalProgramRuns() => Assert.Equal(0, AssemblyRunner.RunEntryPoint(Compiled.Image));

		[Fact]
		public void ProgramRunsAfterAWriteWithNewMetadata() =>
			Assert.Equal(0, AssemblyRunner.RunEntryPoint(RoundTrip.CheckWithNewMetadata(Compiled.Image)));

		[Fact]
		public void ProgramRunsAfterAWriteWithPreservedMetadata() =>
			Assert.Equal(0, AssemblyRunner.RunEntryPoint(RoundTrip.CheckWithPreservedMetadata(Compiled.Image)));

		[Theory, InlineData(false), InlineData(true)]
		public void DebugInfoSurvivesAWrite(bool preserveMetadata) {
			var written = RoundTrip.CheckWithPortablePdb(Compiled.Image, Compiled.Pdb!, preserveMetadata);

			Assert.Equal(0, AssemblyRunner.RunEntryPoint(written.Image));
		}

		[Fact]
		public void WrittenTablesAreTheCompilersTables() {
			CompiledAssembly written;
			using (var module = RoundTrip.LoadWithPdb(Compiled.Image, Compiled.Pdb!))
				written = RoundTrip.WriteWithPortablePdb(module, preserveMetadata: true);

			Assert.Equal(MetadataTableDumper.Dump(Compiled.Image), MetadataTableDumper.Dump(written.Image));
			Assert.Equal(
				PortablePdbTableDumper.Dump(Compiled.Pdb!, skipEmptyLocalScopes: true),
				PortablePdbTableDumper.Dump(written.Pdb!, skipEmptyLocalScopes: true));
		}

		[Fact]
		public void ModuleIsASealedClassWithTheStandardModuleAttribute() {
			using (var module = ModuleDefMD.Load(Compiled.Image)) {
				var helpers = module.Find("VbFeatures.Helpers", isReflectionName: true)!;

				Assert.True(helpers.IsSealed);
				Assert.Contains(helpers.CustomAttributes, a => a.TypeFullName == "Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute");
				// The attribute's class is in this assembly: the VB runtime is embedded
				Assert.NotNull(module.Find("Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute", isReflectionName: true));
				Assert.DoesNotContain(module.GetAssemblyRefs(), a => a.Name.String.StartsWith("Microsoft.VisualBasic", StringComparison.Ordinal));
			}
		}

		// A Date default value can't be stored as a constant, it's in an attribute
		[Fact]
		public void DateDefaultValueIsInAnAttribute() {
			using (var module = ModuleDefMD.Load(Compiled.Image)) {
				var describe = module.Find("VbFeatures.Helpers", isReflectionName: true)!.FindMethod("Describe")!;
				var name = describe.ParamDefs.Single(a => a.Name == "name");
				var day = describe.ParamDefs.Single(a => a.Name == "day");

				Assert.True(name.IsOptional);
				Assert.Equal("nobody", name.Constant!.Value);
				Assert.True(day.IsOptional);
				Assert.Null(day.Constant);
				var attribute = day.CustomAttributes.Single(a => a.TypeFullName == "System.Runtime.CompilerServices.DateTimeConstantAttribute");
				Assert.Equal(new DateTime(2004, 2, 3).Ticks, attribute.ConstructorArguments[0].Value);
			}
		}

		[Fact]
		public void DateAndDecimalLocalConstantsAreReadFromThePdb() {
			using (var module = RoundTrip.LoadWithPdb(Compiled.Image, Compiled.Pdb!)) {
				var method = module.Find("VbFeatures.Helpers", isReflectionName: true)!.FindMethod("Constants")!;
				var constants = method.Body!.PdbMethod!.Scope!.Constants.ToDictionary(a => a.Name!, a => a.Value);

				Assert.Equal(new DateTime(2007, 5, 6, 8, 9, 10), constants["Day"]);
				Assert.Equal(12.5m, constants["Price"]);
				Assert.Equal("title", constants["Title"]);
				Assert.Null(constants["Nothing1"]);
			}
		}

		[Fact]
		public void PdbSaysTheLanguageIsVisualBasic() {
			using (var module = RoundTrip.LoadWithPdb(Compiled.Image, Compiled.Pdb!)) {
				var document = module.PdbState!.Documents.Single();

				Assert.Equal("VbFeatures.vb", document.Url);
				Assert.Equal(PdbDocumentConstants.LanguageVisualBasic, document.Language);
			}
		}
	}
}
