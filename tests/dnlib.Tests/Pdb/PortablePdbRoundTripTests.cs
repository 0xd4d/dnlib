// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Pdb;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Pdb {
	/// <summary>
	/// Reads a test assembly with the portable PDB that the C# compiler created, writes both, reads them again
	/// and checks that the debug info is still the same
	/// </summary>
	public class PortablePdbRoundTripTests {
		static CompiledAssembly Features => Fixtures.Fixtures.Features;

		[Theory, InlineData(false), InlineData(true)]
		public void DebugInfoSurvivesAWrite(bool preserveMetadata) =>
			RoundTrip.CheckWithPortablePdb(Features.Image, Features.Pdb!, preserveMetadata);

		[Theory, InlineData(false), InlineData(true)]
		public void DebugInfoSurvivesTwoWrites(bool preserveMetadata) {
			var writtenOnce = RoundTrip.CheckWithPortablePdb(Features.Image, Features.Pdb!, preserveMetadata);
			RoundTrip.CheckWithPortablePdb(writtenOnce.Image, writtenOnce.Pdb!, preserveMetadata);
		}

		[Fact]
		public void WrittenAssemblyStillRuns() {
			var written = RoundTrip.CheckWithPortablePdb(Features.Image, Features.Pdb!, preserveMetadata: false);

			Assert.Equal(0, AssemblyRunner.RunEntryPoint(written.Image));
		}

		static IEnumerable<PdbScope> GetNestedScopes(PdbScope scope) {
			foreach (var child in scope.Scopes) {
				yield return child;
				foreach (var nested in GetNestedScopes(child))
					yield return nested;
			}
		}

		// The tests above compare what dnlib read before and after a write. If the reader read nothing, they'd
		// pass too, so check that the things the fixture has were read.
		[Fact]
		public void TheFixturesDebugInfoIsRead() {
			using (var module = RoundTrip.LoadWithPdb(Features.Image, Features.Pdb!)) {
				Assert.Equal(PdbFileKind.PortablePDB, module.PdbState!.PdbFileKind);
				var document = Assert.Single(module.PdbState.Documents);
				Assert.Equal("Features.cs", document.Url);
				Assert.Equal(PdbDocumentConstants.LanguageCSharp, document.Language);
				Assert.NotEmpty(document.CheckSum);

				var locals = module.Find("Features.Code", isReflectionName: true)!.FindMethod("Locals")!;
				var body = locals.Body!;
				Assert.Contains(body.Instructions, a => a.SequencePoint is not null);
				Assert.Contains(body.Variables, a => a.Name == "buffer");

				var rootScope = body.PdbMethod!.Scope!;
				Assert.Same(body.Instructions[0], rootScope.Start);
				Assert.NotNull(rootScope.ImportScope);
				var constants = rootScope.Constants.ToDictionary(a => a.Name!, a => a.Value);
				Assert.Equal(3, constants["Factor"]);
				Assert.Equal("label", constants["Label"]);
				Assert.Equal(0.5, constants["Half"]);
				Assert.Equal((byte)2, constants["Favorite"]);
				Assert.Null(constants["NoObject"]);
				// 'money' is declared in the fixed statement's block, which has its own scope
				Assert.DoesNotContain(rootScope.Variables, a => a.Name == "money");
				Assert.Contains(GetNestedScopes(rootScope).SelectMany(a => a.Variables), a => a.Name == "money");

				// The compiler stores extra info for async methods and iterators
				var customDebugInfos = module.GetTypes().SelectMany(a => a.Methods).SelectMany(a => a.CustomDebugInfos).ToList();
				Assert.Contains(customDebugInfos, a => a is PdbAsyncMethodCustomDebugInfo);
				Assert.Contains(customDebugInfos, a => a is PdbStateMachineHoistedLocalScopesCustomDebugInfo);
				Assert.Contains(customDebugInfos, a => a is PdbIteratorMethodCustomDebugInfo);
			}
		}
	}
}
