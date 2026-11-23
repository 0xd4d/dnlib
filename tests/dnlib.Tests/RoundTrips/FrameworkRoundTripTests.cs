// dnlib: See LICENSE.txt for more info

using System.IO;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.RoundTrips {
	/// <summary>
	/// Reads the runtime's own assemblies, writes them, reads them again and checks that they're still the
	/// same modules. See <see cref="FrameworkCorpus"/>.
	/// </summary>
	public class FrameworkRoundTripTests {
		public static TheoryData<string> SampleFiles => new TheoryData<string>(FrameworkCorpus.GetSampleFiles());
		public static TheoryData<string> AllFiles => new TheoryData<string>(FrameworkCorpus.GetAllFiles());

		[Theory, MemberData(nameof(SampleFiles))]
		public void SampleSurvivesAWriteWithNewMetadata(string file) =>
			RoundTrip.CheckWithNewMetadata(File.ReadAllBytes(file));

		[Theory, MemberData(nameof(SampleFiles))]
		public void SampleSurvivesAWriteWithPreservedMetadata(string file) =>
			RoundTrip.CheckWithPreservedMetadata(File.ReadAllBytes(file));

		[Theory, MemberData(nameof(AllFiles)), Trait(TestCategories.Name, TestCategories.Slow)]
		public void EveryAssemblySurvivesAWriteWithNewMetadata(string file) =>
			RoundTrip.CheckWithNewMetadata(File.ReadAllBytes(file));

		[Theory, MemberData(nameof(AllFiles)), Trait(TestCategories.Name, TestCategories.Slow)]
		public void EveryAssemblySurvivesAWriteWithPreservedMetadata(string file) =>
			RoundTrip.CheckWithPreservedMetadata(File.ReadAllBytes(file));

		// On .NET, most of the runtime's assemblies are precompiled (ReadyToRun): they have native code and
		// more sections than a file with only IL code, and the sections aren't where NativeModuleWriter expected
		// them (fixed in a02c5e08 and 0942f106: 67 of the 172 assemblies of .NET 10 couldn't be written).
		[Theory, MemberData(nameof(SampleFiles))]
		public void SampleSurvivesANativeWrite(string file) {
			RoundTrip.CheckWithNativeWriter(File.ReadAllBytes(file), optimizeImageSize: false);
			RoundTrip.CheckWithNativeWriter(File.ReadAllBytes(file), optimizeImageSize: true);
		}

		[Theory, MemberData(nameof(AllFiles)), Trait(TestCategories.Name, TestCategories.Slow)]
		public void EveryAssemblySurvivesANativeWrite(string file) {
			RoundTrip.CheckWithNativeWriter(File.ReadAllBytes(file), optimizeImageSize: false);
			RoundTrip.CheckWithNativeWriter(File.ReadAllBytes(file), optimizeImageSize: true);
		}

#if NET
		// The tests above only test what they say if the files have native code
		[Fact]
		public void SomeSamplesHaveNativeCode() {
			int filesWithNativeCode = 0;
			foreach (var file in FrameworkCorpus.GetSampleFiles()) {
				using (var module = dnlib.DotNet.ModuleDefMD.Load(file)) {
					if (!module.IsILOnly)
						filesWithNativeCode++;
				}
			}
			Assert.True(filesWithNativeCode > 0);
		}
#endif
	}
}
