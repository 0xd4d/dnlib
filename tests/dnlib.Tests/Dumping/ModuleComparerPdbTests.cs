// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Pdb;
using dnlib.TestUtils;
using dnlib.TestUtils.Dumping;
using Xunit;

namespace dnlib.Tests.Dumping {
	/// <summary>
	/// Like <see cref="ModuleComparerTests"/>, for the debug info that's compared if
	/// <see cref="ModuleDumpOptions.Pdb"/> is set: each test changes one thing and checks that it's reported.
	/// </summary>
	public class ModuleComparerPdbTests {
		static readonly ModuleDumpOptions pdbOptions = new ModuleDumpOptions { Pdb = true };

		static ModuleDefMD LoadFeatures() => RoundTrip.LoadWithPdb(Fixtures.Fixtures.Features.Image, Fixtures.Fixtures.Features.Pdb!);

		static string GetDifference(Action<ModuleDefMD> change) {
			using (var original = LoadFeatures())
			using (var changed = LoadFeatures()) {
				change(changed);
				var exception = Assert.Throws<ModuleDifferenceException>(() => ModuleComparer.AssertEqual(original, changed, pdbOptions));
				// The same change isn't reported if debug info isn't compared
				ModuleComparer.AssertEqual(original, changed);
				return exception.Message;
			}
		}

		static MethodDef FindMethod(ModuleDef module, string name) =>
			module.Find("Features.Code", isReflectionName: true)!.FindMethod(name) ?? throw new ArgumentException($"Method {name} doesn't exist");

		static PdbScope GetRootScope(ModuleDef module, string methodName) => FindMethod(module, methodName).Body!.PdbMethod!.Scope!;

		[Fact]
		public void SameModuleLoadedTwiceIsEqual() {
			using (var first = LoadFeatures())
			using (var second = LoadFeatures())
				ModuleComparer.AssertEqual(first, second, pdbOptions);
		}

		[Fact]
		public void DocumentIsCompared() =>
			Assert.Contains("document", GetDifference(module => module.PdbState!.Documents.First().Url = "Other.cs"));

		[Fact]
		public void SequencePointIsCompared() =>
			Assert.Contains(" @ ", GetDifference(module => FindMethod(module, "Locals").Body!.Instructions.First(a => a.SequencePoint is not null).SequencePoint!.StartLine++));

		[Fact]
		public void RemovedSequencePointIsReported() =>
			GetDifference(module => FindMethod(module, "Locals").Body!.Instructions.First(a => a.SequencePoint is not null).SequencePoint = null);

		[Fact]
		public void LocalNameIsCompared() =>
			Assert.Contains("\"renamed\"", GetDifference(module => FindMethod(module, "Locals").Body!.Variables.First(a => a.Name == "buffer").Name = "renamed"));

		[Fact]
		public void ScopeRangeIsCompared() =>
			Assert.Contains("scope", GetDifference(module => {
				var scope = GetRootScope(module, "Locals").Scopes[0];
				scope.Start = FindMethod(module, "Locals").Body!.Instructions[0];
			}));

		[Fact]
		public void RemovedScopeIsReported() =>
			GetDifference(module => GetRootScope(module, "Locals").Scopes.Clear());

		[Fact]
		public void ConstantValueIsCompared() =>
			Assert.Contains("constant \"Factor\"", GetDifference(module => GetRootScope(module, "Locals").Constants.First(a => a.Name == "Factor").Value = 4));

		[Fact]
		public void ImportScopeIsCompared() =>
			Assert.Contains("import", GetDifference(module => {
				// The imports ('using' directives) are in the method's import scope or in one of its parents
				var importScope = GetRootScope(module, "Locals").ImportScope;
				while (importScope!.Imports.Count == 0)
					importScope = importScope.Parent;
				importScope.Imports.RemoveAt(0);
			}));

		[Fact]
		public void CustomDebugInfoIsCompared() =>
			Assert.Contains("customdebuginfo", GetDifference(module => {
				var method = module.GetTypes().SelectMany(a => a.Methods).First(a => a.CustomDebugInfos.Any(b => b is PdbAsyncMethodCustomDebugInfo));
				method.CustomDebugInfos.Clear();
			}));
	}
}
