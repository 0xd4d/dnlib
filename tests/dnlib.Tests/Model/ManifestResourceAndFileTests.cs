// dnlib: See LICENSE.txt for more info

using dnlib.DotNet;
using dnlib.DotNet.MD;
using Xunit;

namespace dnlib.Tests.Model {
	/// <summary>
	/// The rows of the ManifestResource table (<see cref="ManifestResource"/>) and of the File table
	/// (<see cref="FileDef"/>) created with the API: what the constructors and the setters store.
	/// </summary>
	public class ManifestResourceAndFileTests {
		[Fact]
		public void TheConstructorsOfAManifestResourceStoreTheirValues() {
			var file = new FileDefUser("data.bin", FileAttributes.ContainsNoMetadata, new byte[] { 1 });

			var empty = new ManifestResourceUser();
			var withName = new ManifestResourceUser("Res", file);
			var withFlags = new ManifestResourceUser("Res", file, ManifestResourceAttributes.Private);
			var withOffset = new ManifestResourceUser("Res", null, ManifestResourceAttributes.Public, 16);

			Assert.Equal(UTF8String.Empty, empty.Name);
			Assert.Null(empty.Implementation);
			Assert.Equal("Res", withName.Name);
			Assert.Same(file, withName.Implementation);
			Assert.Equal(ManifestResourceAttributes.Private, withFlags.Flags);
			Assert.Equal(16U, withOffset.Offset);
			Assert.Equal(ManifestResourceAttributes.Public, withOffset.Visibility);
			Assert.Equal(new MDToken(Table.ManifestResource, 0), empty.MDToken);
		}

		[Fact]
		public void TheValuesOfAManifestResourceAreSet() {
			var resource = new ManifestResourceUser();
			var assemblyRef = new AssemblyRefUser("Other");

			resource.Rid = 2;
			resource.Offset = 8;
			resource.Flags = ManifestResourceAttributes.Private;
			resource.Name = null;
			resource.Implementation = assemblyRef;

			Assert.Equal(2U, resource.Rid);
			Assert.Equal(8U, resource.Offset);
			Assert.True(resource.IsPrivate);
			Assert.Equal(UTF8String.Empty, resource.Name);
			Assert.Same(assemblyRef, resource.Implementation);
			Assert.Equal(18, resource.HasCustomAttributeTag);
			Assert.Equal(18, resource.HasCustomDebugInformationTag);
			Assert.False(resource.HasCustomAttributes);
			Assert.False(resource.HasCustomDebugInfos);
			Assert.Empty(resource.CustomDebugInfos);
		}

		[Fact]
		public void TheValuesOfAFileAreSet() {
			var file = new FileDefUser();

			file.Rid = 3;
			file.Flags = FileAttributes.ContainsNoMetadata;
			file.Name = null;
			file.HashValue = new byte[] { 1, 2 };

			Assert.Equal(3U, file.Rid);
			Assert.False(file.ContainsMetadata);
			Assert.Equal(UTF8String.Empty, file.Name);
			Assert.Equal("", file.FullName);
			Assert.Equal(new byte[] { 1, 2 }, file.HashValue);
			Assert.Equal(16, file.HasCustomAttributeTag);
			Assert.Equal(16, file.HasCustomDebugInformationTag);
			Assert.Equal(0, file.ImplementationTag);
			Assert.False(file.HasCustomAttributes);
			Assert.False(file.HasCustomDebugInfos);
			Assert.Empty(file.CustomDebugInfos);
		}
	}
}
