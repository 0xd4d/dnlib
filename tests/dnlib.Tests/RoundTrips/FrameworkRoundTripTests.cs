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
	}
}
