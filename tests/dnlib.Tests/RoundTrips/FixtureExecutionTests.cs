// dnlib: See LICENSE.txt for more info

using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.RoundTrips {
	/// <summary>
	/// Runs a test assembly after dnlib wrote it. Its <c>Main()</c> checks that its own code works and returns
	/// the number of failed checks. The runtime, not dnlib, reads the written file here.
	/// </summary>
	public class FixtureExecutionTests {
		[Fact]
		public void OriginalFeaturesRuns() =>
			Assert.Equal(0, AssemblyRunner.RunEntryPoint(Fixtures.Fixtures.Features.Image));

		[Fact]
		public void FeaturesRunsAfterAWriteWithNewMetadata() {
			var written = RoundTrip.CheckWithNewMetadata(Fixtures.Fixtures.Features.Image);

			Assert.Equal(0, AssemblyRunner.RunEntryPoint(written));
		}

		[Fact]
		public void FeaturesRunsAfterAWriteWithPreservedMetadata() {
			var written = RoundTrip.CheckWithPreservedMetadata(Fixtures.Fixtures.Features.Image);

			Assert.Equal(0, AssemblyRunner.RunEntryPoint(written));
		}

		[Fact]
		public void FeaturesRunsAfterTwoWrites() {
			var writtenOnce = RoundTrip.CheckWithNewMetadata(Fixtures.Fixtures.Features.Image);
			var writtenTwice = RoundTrip.CheckWithNewMetadata(writtenOnce);

			Assert.Equal(0, AssemblyRunner.RunEntryPoint(writtenTwice));
		}
	}
}
