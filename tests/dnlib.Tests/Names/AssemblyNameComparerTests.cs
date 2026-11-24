// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Names {
	/// <summary>
	/// <see cref="AssemblyNameComparer"/> compares assembly names: the simple name (without case), the version,
	/// the public key token, the culture and the content type, or only the parts that the flags select.
	/// <see cref="AssemblyNameComparer.CompareClosest"/> picks the better one of two assemblies for a reference:
	/// the assembly resolver uses it when it has no exact match.
	/// </summary>
	public class AssemblyNameComparerTests {
		const string Token = "b77a5c561934e089";
		const string OtherToken = "b03f5f7f11d50a3a";

		static AssemblyNameInfo Create(string name = "Lib", string version = "1.2.3.4", string culture = "", string? token = Token,
			AssemblyAttributes contentType = AssemblyAttributes.ContentType_Default) =>
			new AssemblyNameInfo {
				Name = name,
				Version = new Version(version),
				Culture = culture,
				PublicKeyOrToken = token is null ? null : new PublicKeyToken(token),
				Attributes = contentType,
			};

		// An assembly that differs from Create() in one part, and the flag of that part
		public static TheoryData<string, AssemblyNameComparerFlags> Parts => new TheoryData<string, AssemblyNameComparerFlags> {
			{ "name", AssemblyNameComparerFlags.Name },
			{ "version", AssemblyNameComparerFlags.Version },
			{ "token", AssemblyNameComparerFlags.PublicKeyToken },
			{ "no token", AssemblyNameComparerFlags.PublicKeyToken },
			{ "culture", AssemblyNameComparerFlags.Culture },
			{ "content type", AssemblyNameComparerFlags.ContentType },
		};

		static AssemblyNameInfo CreateWithOther(string part) => part switch {
			"name" => Create(name: "Other"),
			"version" => Create(version: "1.2.3.5"),
			"token" => Create(token: OtherToken),
			"no token" => Create(token: null),
			"culture" => Create(culture: "de"),
			"content type" => Create(contentType: AssemblyAttributes.ContentType_WindowsRuntime),
			_ => throw new ArgumentException(part),
		};

		[Theory, MemberData(nameof(Parts))]
		public void AssembliesAreOnlyDifferentIfAPartThatIsComparedIsDifferent(string part, AssemblyNameComparerFlags flagOfThePart) {
			var assembly = Create();
			var other = CreateWithOther(part);

			Assert.False(AssemblyNameComparer.CompareAll.Equals(assembly, other));
			Assert.False(new AssemblyNameComparer(flagOfThePart).Equals(assembly, other));
			Assert.NotEqual(0, new AssemblyNameComparer(flagOfThePart).CompareTo(assembly, other));
			// Every part but this one
			var comparer = new AssemblyNameComparer(AssemblyNameComparerFlags.All & ~flagOfThePart);
			Assert.True(comparer.Equals(assembly, other));
			Assert.Equal(comparer.GetHashCode(assembly), comparer.GetHashCode(other));
			Assert.Equal(0, comparer.CompareTo(assembly, other));
		}

		[Fact]
		public void EqualAssembliesHaveTheSameHashCode() {
			var comparer = AssemblyNameComparer.CompareAll;
			var assembly = Create();

			// The name is compared without case, and "neutral" is no culture. The hash code of the name was
			// case sensitive: assemblies that are equal had different hash codes.
			foreach (var same in new[] { Create(), Create(name: "LIB"), Create(culture: "neutral") }) {
				Assert.True(comparer.Equals(assembly, same));
				Assert.True(comparer.Equals(same, assembly));
				Assert.Equal(comparer.GetHashCode(assembly), comparer.GetHashCode(same));
			}
			Assert.Equal(0, comparer.GetHashCode(null));
		}

		// A public key is the same as its token
		[Fact]
		public void PublicKeyEqualsItsToken() {
			var publicKey = new PublicKey(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
			var withKey = Create();
			withKey.PublicKeyOrToken = publicKey;
			var withToken = Create();
			withToken.PublicKeyOrToken = publicKey.Token;

			Assert.True(AssemblyNameComparer.CompareAll.Equals(withKey, withToken));
			Assert.Equal(AssemblyNameComparer.CompareAll.GetHashCode(withKey), AssemblyNameComparer.CompareAll.GetHashCode(withToken));
		}

		[Fact]
		public void NullIsLessThanAnyAssembly() {
			var comparer = AssemblyNameComparer.CompareAll;

			Assert.Equal(0, comparer.CompareTo(null, null));
			Assert.True(comparer.CompareTo(null, Create()) < 0);
			Assert.True(comparer.CompareTo(Create(), null) > 0);
			Assert.True(comparer.Equals(null, null));
			Assert.False(comparer.Equals(Create(), null));
		}

		[Fact]
		public void AssembliesAreOrderedByNameThenVersion() {
			var comparer = AssemblyNameComparer.CompareAll;

			Assert.True(comparer.CompareTo(Create(name: "A", version: "9.0.0.0"), Create(name: "B", version: "1.0.0.0")) < 0);
			Assert.True(comparer.CompareTo(Create(version: "1.0.0.0"), Create(version: "2.0.0.0")) < 0);
			Assert.True(comparer.CompareTo(Create(version: "2.0.0.0"), Create(version: "1.0.0.0")) > 0);
		}

		[Fact]
		public void PredefinedComparers() {
			Assert.True(AssemblyNameComparer.NameOnly.Equals(Create(), Create(version: "9.9.9.9", token: OtherToken, culture: "de")));
			Assert.False(AssemblyNameComparer.NameOnly.Equals(Create(), Create(name: "Other")));
			Assert.True(AssemblyNameComparer.NameAndPublicKeyTokenOnly.Equals(Create(), Create(version: "9.9.9.9", culture: "de")));
			Assert.False(AssemblyNameComparer.NameAndPublicKeyTokenOnly.Equals(Create(), Create(token: OtherToken)));
		}

		// CompareClosest() returns 0 if the first assembly is the better one for the reference, 1 if the second
		// one is, and -1 if neither is better
		static int Closest(AssemblyNameInfo requested, AssemblyNameInfo? first, AssemblyNameInfo? second) =>
			AssemblyNameComparer.CompareAll.CompareClosest(requested, first, second);

		[Fact]
		public void AssemblyWithTheRequestedNameIsCloser() {
			var requested = Create();

			Assert.Equal(0, Closest(requested, Create(), Create(name: "Other")));
			Assert.Equal(1, Closest(requested, Create(name: "Other"), Create()));
			Assert.Equal(-1, Closest(requested, Create(name: "Other"), Create(name: "Another")));
			// An assembly is closer than no assembly if it has the name
			Assert.Equal(0, Closest(requested, Create(), null));
			Assert.Equal(1, Closest(requested, null, Create()));
			Assert.Equal(1, Closest(requested, Create(name: "Other"), null));
			Assert.Equal(0, Closest(requested, null, Create(name: "Other")));
		}

		[Fact]
		public void AssemblyWithTheRequestedPublicKeyTokenIsCloser() {
			var requested = Create();

			// Also if it has another version: the token is more important
			Assert.Equal(0, Closest(requested, Create(version: "9.0.0.0"), Create(token: OtherToken)));
			Assert.Equal(1, Closest(requested, Create(token: OtherToken), Create(version: "9.0.0.0")));
			// A reference without a token prefers an assembly without a token
			var requestedWithoutToken = Create(token: null);
			Assert.Equal(0, Closest(requestedWithoutToken, Create(token: null), Create()));
			Assert.Equal(1, Closest(requestedWithoutToken, Create(), Create(token: null)));
		}

		[Theory]
		[InlineData("2.0.0.0", "2.0.0.0", "3.0.0.0", 0)]    // the requested version
		[InlineData("2.0.0.0", "1.0.0.0", "2.0.0.0", 1)]
		[InlineData("2.0.0.0", "3.0.0.0", "1.0.0.0", 0)]    // a higher version is closer than a lower one
		[InlineData("2.0.0.0", "1.0.0.0", "3.0.0.0", 1)]
		[InlineData("2.0.0.0", "3.0.0.0", "4.0.0.0", 0)]    // both are higher: the lower one
		[InlineData("2.0.0.0", "4.0.0.0", "3.0.0.0", 1)]
		[InlineData("5.0.0.0", "3.0.0.0", "4.0.0.0", 1)]    // both are lower: the higher one
		[InlineData("5.0.0.0", "4.0.0.0", "3.0.0.0", 0)]
		[InlineData("0.0.0.0", "1.0.0.0", "2.0.0.0", 1)]    // no version is requested: the highest one
		[InlineData("0.0.0.0", "2.0.0.0", "1.0.0.0", 0)]
		public void AssemblyWithTheClosestVersionIsCloser(string requestedVersion, string firstVersion, string secondVersion, int expected) =>
			Assert.Equal(expected, Closest(Create(version: requestedVersion), Create(version: firstVersion), Create(version: secondVersion)));

		[Fact]
		public void CultureAndContentTypeDecideLast() {
			var requested = Create();

			Assert.Equal(0, Closest(requested, Create(), Create(culture: "de")));
			Assert.Equal(1, Closest(requested, Create(culture: "de"), Create()));
			Assert.Equal(0, Closest(requested, Create(), Create(contentType: AssemblyAttributes.ContentType_WindowsRuntime)));
			Assert.Equal(1, Closest(requested, Create(contentType: AssemblyAttributes.ContentType_WindowsRuntime), Create()));
			// The same assembly, and two assemblies that are equally close
			var same = Create();
			Assert.Equal(0, Closest(requested, same, same));
			Assert.Equal(-1, Closest(requested, Create(), Create()));
		}
	}
}
