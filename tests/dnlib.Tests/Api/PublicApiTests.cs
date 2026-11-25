// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using dnlib.DotNet;
using dnlib.TestUtils;
using PublicApiGenerator;
using Xunit;

namespace dnlib.Tests.Api {
	/// <summary>
	/// The public API of dnlib (every public and protected type and member, with nullable annotations) is
	/// compared with the text files in <c>Api/Baselines</c>, one for every target framework of dnlib. A change
	/// of the public API fails the test, so it can't happen by accident: removing or changing a member breaks
	/// the programs that use it.
	///
	/// If the change is intended, update the files: run the tests with the environment variable
	/// <c>DNLIB_UPDATE_API_BASELINE=1</c> (every leg of tests/test.sh updates its own file) and commit them.
	/// The diff of the files then shows the API change in the pull request.
	/// </summary>
	public class PublicApiTests {
		const string UpdateVariable = "DNLIB_UPDATE_API_BASELINE";

		// THREAD_SAFE doesn't change the public API, so both builds of a target framework use the same file
		static string BaselineFileName => $"dnlib.{BuildUnderTest.ExpectedTargetFramework}.txt";

		static string GeneratePublicApi() {
			var options = new ApiGeneratorOptions { IncludeAssemblyAttributes = false };
			return Normalize(typeof(ModuleDef).Assembly.GeneratePublicApi(options));
		}

		// The same text whatever the OS and git's line ending settings are
		static string Normalize(string text) => text.Replace("\r\n", "\n").TrimEnd('\n') + "\n";

		static string? ReadBaseline() {
			using (var stream = typeof(PublicApiTests).Assembly.GetManifestResourceStream("Api/Baselines/" + BaselineFileName)) {
				if (stream is null)
					return null;
				using (var reader = new StreamReader(stream))
					return Normalize(reader.ReadToEnd());
			}
		}

		// The directory of this source file, to update the baseline on a developer's computer
		static string GetSourceDirectory([CallerFilePath] string sourceFile = "") => Path.GetDirectoryName(sourceFile)!;

		[Fact]
		public void PublicApiIsTheBaseline() {
			var actual = GeneratePublicApi();

			if (Environment.GetEnvironmentVariable(UpdateVariable) == "1") {
				File.WriteAllText(Path.Combine(GetSourceDirectory(), "Baselines", BaselineFileName), actual);
				return;
			}

			var expected = ReadBaseline();
			Assert.True(expected is not null, $"There's no file Api/Baselines/{BaselineFileName}. Create it: run the tests with {UpdateVariable}=1.");
			if (expected == actual)
				return;

			var expectedLines = expected.Split('\n');
			var actualLines = actual.Split('\n');
			var removed = expectedLines.Except(actualLines).Take(10).Select(a => "  - " + a.Trim());
			var added = actualLines.Except(expectedLines).Take(10).Select(a => "  + " + a.Trim());
			Assert.Fail($"The public API of dnlib ({BuildUnderTest.ExpectedTargetFramework}) isn't the API in Api/Baselines/{BaselineFileName}." + Environment.NewLine +
				"The first lines that were removed (-) and added (+):" + Environment.NewLine +
				string.Join(Environment.NewLine, removed.Concat(added)) + Environment.NewLine +
				$"If the change is intended, run the tests with {UpdateVariable}=1 and commit the changed files.");
		}

		// The file is a list of C# declarations. If the generator found nothing, the test above would compare
		// two empty texts.
		[Fact]
		public void BaselineHasTheMainTypes() {
			var api = GeneratePublicApi();

			Assert.Contains("namespace dnlib.DotNet", api);
			Assert.Contains("public abstract class ModuleDef", api);
			Assert.Contains("public sealed class ModuleDefMD : ", api);
			Assert.Contains("namespace dnlib.DotNet.Writer", api);
			Assert.True(api.Split('\n').Length > 5000);
		}
	}
}
