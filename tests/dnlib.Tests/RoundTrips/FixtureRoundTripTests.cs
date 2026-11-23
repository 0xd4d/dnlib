// dnlib: See LICENSE.txt for more info

using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.RoundTrips {
	/// <summary>
	/// Reads a test assembly, writes it, reads it again and checks that it's still the same module
	/// </summary>
	public class FixtureRoundTripTests {
		[Fact]
		public void FeaturesSurvivesAWriteWithNewMetadata() =>
			RoundTrip.CheckWithNewMetadata(Fixtures.Fixtures.Features.Image);

		[Fact]
		public void FeaturesSurvivesAWriteWithPreservedMetadata() =>
			RoundTrip.CheckWithPreservedMetadata(Fixtures.Fixtures.Features.Image);

		[Fact]
		public void WrittenFileSurvivesAnotherWrite() {
			var writtenOnce = RoundTrip.CheckWithNewMetadata(Fixtures.Fixtures.Features.Image);
			RoundTrip.CheckWithNewMetadata(writtenOnce);
			RoundTrip.CheckWithPreservedMetadata(writtenOnce);
		}
	}
}
