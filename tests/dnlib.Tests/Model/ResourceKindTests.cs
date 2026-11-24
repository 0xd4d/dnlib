// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using Xunit;

namespace dnlib.Tests.Model {
	/// <summary>
	/// A .NET resource (a row of the ManifestResource table) is one of three kinds: its data is in the file
	/// (<see cref="EmbeddedResource"/>), in another file of the assembly (<see cref="LinkedResource"/>) or in
	/// another assembly (<see cref="AssemblyLinkedResource"/>). The tests create a module with all of them,
	/// write it and read it again.
	/// </summary>
	public class ResourceKindTests {
		static readonly byte[] fileHash = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };

		static byte[] CreateImage() {
			var module = new ModuleDefUser("Test.dll");
			new AssemblyDefUser("Test", new Version(1, 0, 0, 0)).Modules.Add(module);
			module.Resources.Add(new EmbeddedResource("Embedded.Public", new byte[] { 1, 2, 3 }, ManifestResourceAttributes.Public));
			module.Resources.Add(new EmbeddedResource("Embedded.Private", new byte[] { 4, 5, 6, 7 }));
			var file = new FileDefUser("data.bin", dnlib.DotNet.FileAttributes.ContainsNoMetadata, fileHash);
			module.Resources.Add(new LinkedResource("InAFile", file, ManifestResourceAttributes.Public));
			module.Resources.Add(new AssemblyLinkedResource("InAnAssembly", new AssemblyRefUser("Other", new Version(2, 0, 0, 0)), ManifestResourceAttributes.Private));
			var stream = new MemoryStream();
			module.Write(stream);
			return stream.ToArray();
		}

		static readonly Lazy<byte[]> image = new Lazy<byte[]>(CreateImage);

		[Fact]
		public void EveryKindOfResourceIsReadWithItsValues() {
			using (var module = ModuleDefMD.Load(image.Value)) {
				var resources = module.Resources;
				Assert.Equal(new[] { "Embedded.Public", "Embedded.Private", "InAFile", "InAnAssembly" }, resources.Select(a => a.Name.String));

				var embedded = Assert.IsAssignableFrom<EmbeddedResource>(resources[0]);
				Assert.Equal(ResourceType.Embedded, embedded.ResourceType);
				Assert.True(embedded.IsPublic && !embedded.IsPrivate);
				Assert.Equal(new byte[] { 1, 2, 3 }, embedded.CreateReader().ToArray());
				Assert.Equal(3U, embedded.Length);
				Assert.Equal("Embedded.Public - size: 3", embedded.ToString());
				var privateEmbedded = Assert.IsAssignableFrom<EmbeddedResource>(resources[1]);
				Assert.True(privateEmbedded.IsPrivate && !privateEmbedded.IsPublic);
				Assert.Equal(new byte[] { 4, 5, 6, 7 }, privateEmbedded.CreateReader().ToArray());

				var linked = Assert.IsAssignableFrom<LinkedResource>(resources[2]);
				Assert.Equal(ResourceType.Linked, linked.ResourceType);
				Assert.Equal("data.bin", linked.FileName);
				Assert.Equal(fileHash, linked.Hash);
				Assert.True(linked.File.ContainsNoMetadata);
				Assert.Equal("InAFile - file: data.bin", linked.ToString());

				var inAssembly = Assert.IsAssignableFrom<AssemblyLinkedResource>(resources[3]);
				Assert.Equal(ResourceType.AssemblyLinked, inAssembly.ResourceType);
				Assert.Equal("Other", inAssembly.Assembly.Name);
				Assert.Equal(new Version(2, 0, 0, 0), inAssembly.Assembly.Version);
				Assert.StartsWith("InAnAssembly - assembly: Other, Version=2.0.0.0", inAssembly.ToString());
				Assert.True(inAssembly.IsPrivate);
			}
		}

		[Fact]
		public void ResourcesAreFoundByNameAndKind() {
			using (var module = ModuleDefMD.Load(image.Value)) {
				var resources = module.Resources;

				Assert.Same(resources[2], resources.Find("InAFile"));
				Assert.Equal(2, resources.IndexOf("InAFile"));
				Assert.Null(resources.Find("Missing"));
				Assert.Equal(-1, resources.IndexOf("Missing"));
				// Names are case sensitive
				Assert.Null(resources.Find("inafile"));

				Assert.Same(resources[1], resources.FindEmbeddedResource("Embedded.Private"));
				Assert.Equal(1, resources.IndexOfEmbeddedResource("Embedded.Private"));
				Assert.Same(resources[2], resources.FindLinkedResource("InAFile"));
				Assert.Equal(2, resources.IndexOfLinkedResource("InAFile"));
				Assert.Same(resources[3], resources.FindAssemblyLinkedResource("InAnAssembly"));
				Assert.Equal(3, resources.IndexOfAssemblyLinkedResource("InAnAssembly"));
				// A resource of another kind isn't found
				Assert.Null(resources.FindEmbeddedResource("InAFile"));
				Assert.Null(resources.FindLinkedResource("InAnAssembly"));
				Assert.Null(resources.FindAssemblyLinkedResource("Embedded.Public"));
				Assert.Equal(-1, resources.IndexOfEmbeddedResource("InAFile"));
				Assert.Equal(-1, resources.IndexOfLinkedResource("InAnAssembly"));
				Assert.Equal(-1, resources.IndexOfAssemblyLinkedResource("Embedded.Public"));
			}
		}

		// The rows of the ManifestResource table as they are in the file
		[Fact]
		public void ManifestResourceRowsHaveWhereTheDataIs() {
			using (var module = ModuleDefMD.Load(image.Value)) {
				var rows = Enumerable.Range(1, 4).Select(a => module.ResolveManifestResource((uint)a)!).ToList();

				Assert.Equal(new[] { "Embedded.Public", "Embedded.Private", "InAFile", "InAnAssembly" }, rows.Select(a => a.Name.String));
				Assert.All(rows, a => Assert.Equal(Table.ManifestResource, a.MDToken.Table));
				Assert.Equal(new uint[] { 1, 2, 3, 4 }, rows.Select(a => a.Rid));
				// The data of an embedded resource is in this file: no implementation, and the offset of its data
				// (each resource is 4 bytes of length and the data, padded to 8 bytes)
				Assert.Null(rows[0].Implementation);
				Assert.Null(rows[1].Implementation);
				Assert.Equal(0U, rows[0].Offset);
				Assert.Equal(8U, rows[1].Offset);
				Assert.Equal("data.bin", Assert.IsAssignableFrom<FileDef>(rows[2].Implementation).Name);
				Assert.Equal("Other", Assert.IsAssignableFrom<AssemblyRef>(rows[3].Implementation).Name);

				Assert.True(rows[0].IsPublic && !rows[0].IsPrivate);
				Assert.True(rows[1].IsPrivate && !rows[1].IsPublic);
				Assert.Equal(ManifestResourceAttributes.Public, rows[2].Visibility);
				Assert.Equal(ManifestResourceAttributes.Private, rows[3].Flags);
				rows[3].Visibility = ManifestResourceAttributes.Public;
				Assert.True(rows[3].IsPublic);
				Assert.Null(module.ResolveManifestResource(5));
				Assert.Null(module.ResolveManifestResource(0));
			}
		}

		[Fact]
		public void EveryKindOfResourceSurvivesAnotherWrite() {
			using (var module = ModuleDefMD.Load(image.Value)) {
				var stream = new MemoryStream();
				module.Write(stream);

				using (var written = ModuleDefMD.Load(stream.ToArray())) {
					Assert.Equal(module.Resources.Select(a => a.ToString()), written.Resources.Select(a => a.ToString()));
					Assert.Equal(module.Resources.Select(a => a.Attributes), written.Resources.Select(a => a.Attributes));
					Assert.Equal(fileHash, written.Resources.FindLinkedResource("InAFile")!.Hash);
				}
			}
		}
	}
}
