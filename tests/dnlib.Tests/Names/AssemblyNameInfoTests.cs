// dnlib: See LICENSE.txt for more info

using System;
using AssemblyName = System.Reflection.AssemblyName;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Names {
	/// <summary>
	/// <see cref="AssemblyNameInfo"/> is the name of an assembly without a module: the simple name, the version,
	/// the culture, the public key (or its token) and the flags. It's created from the text form of a name, from
	/// System.Reflection's <see cref="AssemblyName"/> or from any other assembly or assembly reference.
	/// </summary>
	public class AssemblyNameInfoTests {
		const string FullName = "Some.Library, Version=1.2.3.4, Culture=neutral, PublicKeyToken=b77a5c561934e089";

		[Fact]
		public void NameIsParsedFromItsTextForm() {
			var info = new AssemblyNameInfo(FullName);

			Assert.Equal("Some.Library", info.Name);
			Assert.Equal(new Version(1, 2, 3, 4), info.Version);
			Assert.Equal("", info.Culture);
			Assert.Equal("b77a5c561934e089", info.PublicKeyOrToken!.ToString());
			Assert.IsType<PublicKeyToken>(info.PublicKeyOrToken);
			Assert.Equal(FullName, info.FullName);
			Assert.Equal(FullName, info.FullNameToken);
			Assert.Equal(FullName, info.ToString());
		}

		// Changed in this commit: a name without a version got version 0.0.0.0. It now has no version (every
		// number is 65535, see NameWithoutAVersionIsAnyVersion), and its full name has no version either.
		[Theory]
		[InlineData("Lib", "Lib, Culture=neutral, PublicKeyToken=null")]
		[InlineData("Lib, Version=1.0.0.0", "Lib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")]
		[InlineData("Lib, Culture=de-DE", "Lib, Culture=de-DE, PublicKeyToken=null")]
		[InlineData("Lib, PublicKeyToken=null", "Lib, Culture=neutral, PublicKeyToken=null")]
		// The parts can be in any order
		[InlineData("Lib, PublicKeyToken=b77a5c561934e089, Culture=neutral, Version=4.0.0.0", "Lib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
		public void PartsThatAreNotInTheTextHaveTheirDefaultValues(string text, string expectedFullName) =>
			Assert.Equal(expectedFullName, new AssemblyNameInfo(text).FullName);

		// Changed in this commit: 65535 is "no value" in the metadata, and the CLR takes any version of the
		// assembly for a reference without a version. System.Reflection.AssemblyName has a null version.
		[Fact]
		public void NameWithoutAVersionIsAnyVersion() {
			var noVersion = new Version(65535, 65535, 65535, 65535);

			Assert.Equal(noVersion, new AssemblyNameInfo("Lib").Version);
			Assert.Equal(noVersion, new AssemblyNameInfo("Lib, Culture=de").Version);
			Assert.Equal(noVersion, new AssemblyNameInfo(new System.Reflection.AssemblyName("Lib")).Version);
			Assert.Equal(noVersion, new AssemblyRefUser(new System.Reflection.AssemblyName("Lib")).Version);
			Assert.Equal(noVersion, new AssemblyNameInfo(new AssemblyNameInfo { Name = "Lib" }).Version);
			Assert.Equal(noVersion, new AssemblyRefUser(new AssemblyNameInfo { Name = "Lib" }).Version);
			// A version of 0.0.0.0 is a version
			Assert.Equal(new Version(0, 0, 0, 0), new AssemblyNameInfo("Lib, Version=0.0.0.0").Version);
		}

		// A name with the whole public key: the full name has its token
		[Fact]
		public void FullNameHasTheTokenOfAPublicKey() {
			var publicKey = new PublicKey(new byte[] { 0, 0x24, 0, 0, 4, 0x80, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8 });
			var info = new AssemblyNameInfo("Lib, Version=1.0.0.0, PublicKey=" + publicKey);

			Assert.IsType<PublicKey>(info.PublicKeyOrToken);
			Assert.Equal(publicKey.Data, info.PublicKeyOrToken!.Data);
			Assert.Equal("Lib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=" + publicKey.Token, info.FullName);
		}

		// The names that reflection returns for real assemblies have the same text form in dnlib
		[Theory]
		[InlineData(typeof(object))]
		[InlineData(typeof(Uri))]
		[InlineData(typeof(Xunit.Assert))]
		[InlineData(typeof(AssemblyNameInfoTests))]
		public void NameFromReflectionHasTheSameFullName(Type typeInTheAssembly) {
			var reflectionName = typeInTheAssembly.Assembly.GetName();

			var info = new AssemblyNameInfo(reflectionName);

			Assert.Equal(reflectionName.Name, info.Name);
			Assert.Equal(reflectionName.Version, info.Version);
			Assert.Equal(reflectionName.FullName, info.FullName);
			// And the text form is parsed to the same name again
			Assert.Equal(reflectionName.FullName, new AssemblyNameInfo(reflectionName.FullName).FullName);
			Assert.True(AssemblyNameComparer.CompareAll.Equals(info, new AssemblyNameInfo(reflectionName.FullName)));
		}

		[Fact]
		public void NameIsCopiedFromAnotherAssembly() {
			var assembly = new AssemblyDefUser("Lib", new Version(1, 2, 3, 4), new PublicKey(new byte[] { 1, 2, 3 }), "de") {
				HashAlgorithm = AssemblyHashAlgorithm.SHA_256,
				Attributes = AssemblyAttributes.PublicKey | AssemblyAttributes.Retargetable,
			};

			var info = new AssemblyNameInfo(assembly);

			Assert.Equal("Lib", info.Name);
			Assert.Equal(new Version(1, 2, 3, 4), info.Version);
			Assert.Equal("de", info.Culture);
			Assert.Same(assembly.PublicKey, info.PublicKeyOrToken);
			Assert.Equal(AssemblyHashAlgorithm.SHA_256, info.HashAlgId);
			Assert.Equal(AssemblyAttributes.PublicKey | AssemblyAttributes.Retargetable, info.Attributes);
			Assert.True(AssemblyNameComparer.CompareAll.Equals(assembly, info));
			// A reference has no hash algorithm
			Assert.Equal(AssemblyHashAlgorithm.None, new AssemblyNameInfo(new AssemblyRefUser(assembly)).HashAlgId);
		}

		[Fact]
		public void NoAssemblyIsAnEmptyName() {
			foreach (var info in new[] { new AssemblyNameInfo(), new AssemblyNameInfo((IAssembly?)null), new AssemblyNameInfo((AssemblyName?)null) }) {
				Assert.True(UTF8String.IsNullOrEmpty(info.Name));
				Assert.Null(info.PublicKeyOrToken);
				Assert.Equal(AssemblyAttributes.None, info.Attributes);
			}
		}

		// Every flag property reads and changes its bits of Attributes, and no other bits
		[Fact]
		public void FlagPropertiesAreTheBitsOfAttributes() {
			var info = new AssemblyNameInfo();

			info.HasPublicKey = true;
			info.IsRetargetable = true;
			info.EnableJITcompileTracking = true;
			info.DisableJITcompileOptimizer = true;
			info.IsProcessorArchitectureSpecified = true;
			Assert.Equal(AssemblyAttributes.PublicKey | AssemblyAttributes.Retargetable | AssemblyAttributes.EnableJITcompileTracking |
				AssemblyAttributes.DisableJITcompileOptimizer | AssemblyAttributes.PA_Specified, info.Attributes);
			Assert.True(info.HasPublicKey && info.IsRetargetable && info.EnableJITcompileTracking && info.DisableJITcompileOptimizer && info.IsProcessorArchitectureSpecified);

			info.HasPublicKey = false;
			info.EnableJITcompileTracking = false;
			info.DisableJITcompileOptimizer = false;
			info.IsProcessorArchitectureSpecified = false;
			Assert.Equal(AssemblyAttributes.Retargetable, info.Attributes);

			info.ContentType = AssemblyAttributes.ContentType_WindowsRuntime;
			Assert.True(info.IsContentTypeWindowsRuntime && !info.IsContentTypeDefault);
			Assert.Equal(AssemblyAttributes.Retargetable | AssemblyAttributes.ContentType_WindowsRuntime, info.Attributes);
			info.ContentType = AssemblyAttributes.ContentType_Default;
			Assert.True(info.IsContentTypeDefault && !info.IsContentTypeWindowsRuntime);
		}

		[Theory]
		[InlineData(AssemblyAttributes.PA_None, "None")]
		[InlineData(AssemblyAttributes.PA_MSIL, "MSIL")]
		[InlineData(AssemblyAttributes.PA_x86, "X86")]
		[InlineData(AssemblyAttributes.PA_IA64, "IA64")]
		[InlineData(AssemblyAttributes.PA_AMD64, "X64")]
		[InlineData(AssemblyAttributes.PA_ARM, "ARM")]
		[InlineData(AssemblyAttributes.PA_NoPlatform, "NoPlatform")]
		public void ProcessorArchitectureIsOneOfSevenValues(AssemblyAttributes architecture, string expectedProperty) {
			var info = new AssemblyNameInfo { Attributes = AssemblyAttributes.Retargetable };

			info.ProcessorArchitecture = architecture;

			Assert.Equal(architecture, info.ProcessorArchitecture);
			Assert.Equal(architecture | AssemblyAttributes.Retargetable, info.Attributes);
			// Only the property of this architecture is true
			foreach (var name in new[] { "None", "MSIL", "X86", "IA64", "X64", "ARM", "NoPlatform" }) {
				bool value = (bool)typeof(AssemblyNameInfo).GetProperty("IsProcessorArchitecture" + name)!.GetValue(info, null)!;
				Assert.True(value == (name == expectedProperty), name);
			}
			// The architecture with the bit that says that there is one
			info.ProcessorArchitectureFull = architecture | AssemblyAttributes.PA_Specified;
			Assert.True(info.IsProcessorArchitectureSpecified);
			Assert.Equal(architecture | AssemblyAttributes.PA_Specified, info.ProcessorArchitectureFull);
		}

		// Fixed in this commit: a name with a quote, or with white space at its start or end, is in double
		// quotes like in System.Reflection.AssemblyName.FullName (and the parser reads such a name), and tab,
		// carriage return and new line are \t, \r and \n
		[Theory]
		[InlineData("A B", "A B")]
		[InlineData(" A", "\" A\"")]
		[InlineData("A ", "\"A \"")]
		[InlineData("\tA", "\"\\tA\"")]
		[InlineData("A\"B", "\"A\\\"B\"")]
		[InlineData("A'B", "\"A\\'B\"")]
		[InlineData("A\tB\r\nC", "A\\tB\\r\\nC")]
		[InlineData("A,B=C\\D", "A\\,B\\=C\\\\D")]
		public void NameWithSpecialCharsIsWrittenLikeSystemReflectionDoes(string name, string expectedNameInFullName) {
			var info = new AssemblyNameInfo { Name = name, Version = new Version(1, 2, 3, 4), Culture = "" };
			string expected = expectedNameInFullName + ", Version=1.2.3.4, Culture=neutral, PublicKeyToken=null";

			Assert.Equal(expected, info.FullName);
			// .NET Framework and Mono write these names in another way
			if (typeof(object).Assembly.GetName().Name == "System.Private.CoreLib") {
				var reflectionName = new System.Reflection.AssemblyName { Name = name, Version = new Version(1, 2, 3, 4), CultureInfo = System.Globalization.CultureInfo.InvariantCulture };
				reflectionName.SetPublicKeyToken(new byte[0]);
				Assert.Equal(reflectionName.FullName, info.FullName);
			}

			// The parser reads the name again
			Assert.Equal(name, new AssemblyNameInfo(expected).Name);
			Assert.Equal(expected, new AssemblyNameInfo(expected).FullName);
		}

		// Fixed in this commit: 65535 in a version is "no value". The full name doesn't have that part and
		// the parts after it, like System.Reflection.AssemblyName.FullName.
		[Theory]
		[InlineData(1, 2, 3, 4, "Lib, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null")]
		[InlineData(1, 2, 3, 65535, "Lib, Version=1.2.3, Culture=neutral, PublicKeyToken=null")]
		[InlineData(1, 2, 65535, 4, "Lib, Version=1.2, Culture=neutral, PublicKeyToken=null")]
		[InlineData(1, 65535, 3, 4, "Lib, Version=1, Culture=neutral, PublicKeyToken=null")]
		[InlineData(65535, 2, 3, 4, "Lib, Culture=neutral, PublicKeyToken=null")]
		[InlineData(65535, 65535, 65535, 65535, "Lib, Culture=neutral, PublicKeyToken=null")]
		[InlineData(65534, 65534, 65534, 65534, "Lib, Version=65534.65534.65534.65534, Culture=neutral, PublicKeyToken=null")]
		public void VersionPartThatIs65535IsNotInTheFullName(int major, int minor, int build, int revision, string expected) {
			var info = new AssemblyNameInfo { Name = "Lib", Version = new Version(major, minor, build, revision), Culture = "" };

			Assert.Equal(expected, info.FullName);
			if (typeof(object).Assembly.GetName().Name == "System.Private.CoreLib") {
				var reflectionName = new System.Reflection.AssemblyName { Name = "Lib", Version = new Version(major, minor, build, revision), CultureInfo = System.Globalization.CultureInfo.InvariantCulture };
				reflectionName.SetPublicKeyToken(new byte[0]);
				Assert.Equal(reflectionName.FullName, info.FullName);
			}
		}

		// Fixed in this commit: the parser reads a version like System.Reflection.AssemblyName does. A third
		// or fourth number that is missing or 65535 has no value (it was 0), and a version with one number,
		// with more than four numbers or with a number that isn't 0-65535 is an invalid name.
		[Theory]
		[InlineData("1.2.3.4", "1.2.3.4")]
		[InlineData("1.2.3", "1.2.3")]
		[InlineData("1.2", "1.2")]
		[InlineData("1.2.3.65535", "1.2.3")]
		[InlineData("1.2.65535.4", "1.2")]
		[InlineData("1.2.65535", "1.2")]
		[InlineData("0.0.0.0", "0.0.0.0")]
		[InlineData("65534.65534.65534.65534", "65534.65534.65534.65534")]
		[InlineData("01.002", "1.2")]
		[InlineData("1", "invalid")]
		[InlineData("1.2.3.4.5", "invalid")]
		[InlineData("65535.2", "invalid")]
		[InlineData("1.65535", "invalid")]
		[InlineData("1.2.65536", "invalid")]
		[InlineData("1.2.-3", "invalid")]
		[InlineData("1.+2", "invalid")]
		[InlineData("1.2.x", "invalid")]
		[InlineData("1..2", "invalid")]
		public void VersionIsParsedLikeSystemReflectionDoes(string version, string expected) {
			string text = "Lib, Version=" + version;

			var assemblyRef = ReflectionTypeNameParser.ParseAssemblyRef(text);
			Assert.Equal(expected, assemblyRef is null ? "invalid" : assemblyRef.Version.ToString());

			// .NET Framework and Mono parse some of these versions in another way
			if (typeof(object).Assembly.GetName().Name == "System.Private.CoreLib") {
				string reflectionVersion;
				try {
					reflectionVersion = new AssemblyName(text).Version!.ToString();
				}
				catch (System.IO.FileLoadException) {
					reflectionVersion = "invalid";
				}
				Assert.Equal(reflectionVersion, expected);
			}

			// The full name has the parts that have a value
			if (assemblyRef is not null)
				Assert.Equal($"Lib, Version={expected}, Culture=neutral, PublicKeyToken=null", assemblyRef.FullName);
		}

		// A System.Version without its third or fourth number is written like System.Reflection does
		[Theory]
		[InlineData(1, 2, -1, -1, "Lib, Version=1.2, Culture=neutral, PublicKeyToken=null")]
		[InlineData(1, 2, 3, -1, "Lib, Version=1.2.3, Culture=neutral, PublicKeyToken=null")]
		public void VersionWithoutAllItsNumbersIsNotFilledWithZeros(int major, int minor, int build, int revision, string expected) {
			var version = build == -1 ? new Version(major, minor) : revision == -1 ? new Version(major, minor, build) : new Version(major, minor, build, revision);
			var info = new AssemblyNameInfo { Name = "Lib", Version = version, Culture = "" };

			Assert.Equal(expected, info.FullName);
			if (typeof(object).Assembly.GetName().Name == "System.Private.CoreLib") {
				var reflectionName = new AssemblyName { Name = "Lib", Version = version, CultureInfo = System.Globalization.CultureInfo.InvariantCulture };
				reflectionName.SetPublicKeyToken(new byte[0]);
				Assert.Equal(reflectionName.FullName, info.FullName);
			}
		}
	}
}
