// dnlib: See LICENSE.txt for more info

using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Infrastructure {
	/// <summary>
	/// Makes sure the test run uses the dnlib build it was told to use. Without these tests, a mistake in the
	/// build files could silently test eg. the net45 build twice and never the net35 build.
	/// </summary>
	public class BuildUnderTestTests {
		[Fact]
		public void LoadedDnlibHasTheExpectedTargetFramework() =>
			Assert.Equal(BuildUnderTest.ExpectedTargetFramework, BuildUnderTest.ActualTargetFramework);

		[Fact]
		public void LoadedDnlibHasTheExpectedThreadSafety() =>
			Assert.Equal(BuildUnderTest.ExpectedThreadSafe, Settings.IsThreadSafe);
	}
}
