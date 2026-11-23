// dnlib: See LICENSE.txt for more info

using System.IO;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reflection {
	/// <summary>
	/// <see cref="SigComparer"/> and <see cref="Importer"/> checked against System.Reflection for every type and
	/// member of an assembly, see <see cref="ReflectionComparison"/>
	/// </summary>
	public class ReflectionComparisonTests {
		public static TheoryData<SigComparerOptions> Options => new TheoryData<SigComparerOptions> {
			0,
			SigComparerOptions.CompareDeclaringTypes,
			SigComparerOptions.CompareDeclaringTypes | SigComparerOptions.CompareAssemblyPublicKeyToken | SigComparerOptions.CompareAssemblyVersion | SigComparerOptions.CompareAssemblyLocale,
			SigComparerOptions.DontCompareTypeScope | SigComparerOptions.CaseInsensitiveAll,
			SigComparerOptions.PrivateScopeIsComparable | SigComparerOptions.IgnoreModifiers,
		};

		[Theory, MemberData(nameof(Options))]
		public void FeaturesFixture(SigComparerOptions options) {
			var image = Fixtures.Fixtures.Features.Image;
			using (var loaded = new LoadedAssembly(image))
			using (var module = ModuleDefMD.Load(image)) {
				int checkedMembers = ReflectionComparison.Check(loaded.Assembly, module, options);

				// Make sure the members were found
				Assert.True(checkedMembers > 150, $"Only {checkedMembers} members were checked");
			}
		}

		[Theory, MemberData(nameof(Options))]
		public void CoreLibrary(SigComparerOptions options) {
			var assembly = typeof(object).Assembly;
			using (var module = ModuleDefMD.Load(File.ReadAllBytes(assembly.Location))) {
				int checkedMembers = ReflectionComparison.Check(assembly, module, options);

				Assert.True(checkedMembers > 10000, $"Only {checkedMembers} members were checked");
			}
		}
	}
}
