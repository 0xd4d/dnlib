// dnlib: See LICENSE.txt for more info

using System;
using System.Reflection;
using dnlib.DotNet;
using Xunit;
using AssemblyAttributes = dnlib.DotNet.AssemblyAttributes;

namespace dnlib.Tests.Importing {
	/// <summary>
	/// References to assemblies that are created from an assembly or from an <see cref="AssemblyName"/>
	/// </summary>
	public class AssemblyRefTests {
		[Fact]
		public void ReferenceToAnAssemblyHasItsNameVersionCultureAndPublicKeyToken() {
			var publicKey = new PublicKey(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });
			var assembly = new AssemblyDefUser("Name", new Version(1, 2, 3, 4), publicKey, "fr");

			var reference = assembly.ToAssemblyRef();

			Assert.NotNull(reference);
			Assert.Equal("Name", reference.Name);
			Assert.Equal(new Version(1, 2, 3, 4), reference.Version);
			Assert.Equal("fr", reference.Culture);
			// A reference normally has the short token of the key, not the key
			Assert.Equal(publicKey.Token?.Data, reference.PublicKeyOrToken?.Token?.Data);
		}

		// Fixed in f7199526 (it threw). Changed in this commit: the version was 0.0.0.0, it's now "no version",
		// which is 65535 for every number in the metadata: a reference to any version of the assembly.
		[Fact]
		public void ReferenceToAnAssemblyWithoutAVersionHasNoVersion() {
			var assembly = new AssemblyNameInfo { Name = "Name" };
			Assert.Null(assembly.Version);

			Assert.Equal(new Version(65535, 65535, 65535, 65535), assembly.ToAssemblyRef()?.Version);
		}

		// Fixed in dd7fe491: compilers write this flag in a reference to a retargetable assembly
		[Fact]
		public void ReferenceToARetargetableAssemblyIsRetargetable() {
			var assembly = new AssemblyDefUser("Name", new Version(1, 0, 0, 0));
			assembly.IsRetargetable = true;

			Assert.True(new AssemblyRefUser(assembly).IsRetargetable);
		}

		[Fact]
		public void ReferenceToANormalAssemblyIsNotRetargetable() =>
			Assert.False(new AssemblyRefUser(new AssemblyDefUser("Name", new Version(1, 0, 0, 0))).IsRetargetable);

		// The flags that describe the assembly itself (eg. its processor architecture) don't belong in a reference
		[Fact]
		public void ProcessorArchitectureIsNotCopiedToAReference() {
			var assembly = new AssemblyDefUser("Name", new Version(1, 0, 0, 0));
			assembly.Attributes |= AssemblyAttributes.PA_AMD64 | AssemblyAttributes.PA_Specified;

			var reference = new AssemblyRefUser(assembly);

			Assert.Equal((AssemblyAttributes)0, reference.Attributes & (AssemblyAttributes.PA_FullMask));
		}

		// Fixed in 1bf3a78f (#592)
		[Fact]
		public void ContentTypeOfAnAssemblyNameIsCopied() {
			var name = new AssemblyName("Name") { Version = new Version(1, 0, 0, 0), ContentType = AssemblyContentType.WindowsRuntime };

			Assert.True(new AssemblyRefUser(name).IsContentTypeWindowsRuntime);
			Assert.True(new AssemblyDefUser(name).IsContentTypeWindowsRuntime);
		}

		[Fact]
		public void ContentTypeOfAnAssemblyIsCopiedToAReference() {
			var assembly = new AssemblyDefUser("Name", new Version(1, 0, 0, 0));
			assembly.ContentType = AssemblyAttributes.ContentType_WindowsRuntime;

			Assert.True(new AssemblyRefUser(assembly).IsContentTypeWindowsRuntime);
		}

		[Fact]
		public void ReferenceCreatedFromAnAssemblyName() {
			var name = typeof(object).Assembly.GetName();

			var reference = new AssemblyRefUser(name);

			Assert.Equal(name.Name, reference.Name);
			Assert.Equal(name.Version, reference.Version);
			Assert.Equal(name.GetPublicKeyToken(), reference.PublicKeyOrToken?.Token?.Data);
		}
	}
}
