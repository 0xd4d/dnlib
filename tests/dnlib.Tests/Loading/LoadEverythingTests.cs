// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.TestUtils;
using dnlib.TestUtils.Dumping;
using dnlib.Threading;
using Xunit;

namespace dnlib.Tests.Loading {
	/// <summary>
	/// dnlib reads most things when they're first used. <see cref="ModuleDef.LoadEverything"/> reads everything
	/// at once, eg. before the file is closed.
	/// </summary>
	public class LoadEverythingTests {
		public static TheoryData<string> SampleFiles => new TheoryData<string>(FrameworkCorpus.GetSampleFiles());

		static readonly ModuleDumpOptions allOptions = new ModuleDumpOptions { MaxStack = true, Pdb = true };

		[Fact]
		public void ModuleIsTheSameAsWhenItIsReadOnDemand() {
			var features = Fixtures.Fixtures.Features;
			using (var onDemand = RoundTrip.LoadWithPdb(features.Image, features.Pdb!))
			using (var everything = RoundTrip.LoadWithPdb(features.Image, features.Pdb!)) {
				everything.LoadEverything();

				ModuleComparer.AssertEqual(onDemand, everything, allOptions);
			}
		}

		// The Exports fixture has things the other fixture doesn't have: a table of exported methods (vtable
		// fixups) and a field with data in the file
		[Fact]
		public void ModuleWithExportedMethodsIsTheSameAsWhenItIsReadOnDemand() {
			var image = Fixtures.Fixtures.Exports.Image;
			using (var onDemand = ModuleDefMD.Load(image))
			using (var everything = ModuleDefMD.Load(image)) {
				everything.LoadEverything();

				ModuleComparer.AssertEqual(onDemand, everything, allOptions);
				Assert.Equal("Add", Assert.Single(Assert.Single(everything.VTableFixups!.VTables).Methods)!.Name);
			}
		}

		// A module with a resource in another file, a resource in another assembly, and a file entry
		[Fact]
		public void LinkedResourcesAndFilesAreLoaded() {
			byte[] image;
			using (var module = ModuleDefMD.Load(Fixtures.Fixtures.Features.Image)) {
				var file = new FileDefUser("Linked.bin", dnlib.DotNet.FileAttributes.ContainsNoMetadata, new byte[] { 1, 2, 3 });
				module.Resources.Add(new LinkedResource("Linked", file, ManifestResourceAttributes.Public));
				module.Resources.Add(new AssemblyLinkedResource("InOtherAssembly", new AssemblyRefUser("Other", new System.Version(1, 0, 0, 0)), ManifestResourceAttributes.Private));
				var stream = new MemoryStream();
				module.Write(stream);
				image = stream.ToArray();
			}

			using (var module = ModuleDefMD.Load(image)) {
				module.LoadEverything();

				var linked = Assert.Single(module.Resources.OfType<LinkedResource>());
				Assert.Equal("Linked.bin", linked.File.Name);
				Assert.Equal(new byte[] { 1, 2, 3 }, linked.File.HashValue);
				Assert.False(linked.File.ContainsMetadata);
				Assert.Equal("Other", Assert.Single(module.Resources.OfType<AssemblyLinkedResource>()).Assembly.Name);
			}
		}

		[Theory, MemberData(nameof(SampleFiles))]
		public void FrameworkAssemblyIsTheSameAsWhenItIsReadOnDemand(string file) {
			var image = File.ReadAllBytes(file);
			using (var onDemand = ModuleDefMD.Load(image))
			using (var everything = ModuleDefMD.Load(image)) {
				everything.LoadEverything();

				ModuleComparer.AssertEqual(onDemand, everything, allOptions);
			}
		}

		[Fact]
		public void ModuleCanBeUsedAfterItsFileWasClosed() {
			using (var directory = new TempDirectory()) {
				var file = directory.GetPath("Features.exe");
				File.WriteAllBytes(file, Fixtures.Fixtures.Features.Image);
				using (var expected = ModuleDefMD.Load(Fixtures.Fixtures.Features.Image))
				using (var module = ModuleDefMD.Load(file)) {
					module.LoadEverything();
					// Closes the memory mapped file. The module then reads the rest of the file into memory.
					((dnlib.PE.IInternalPEImage)module.Metadata.PEImage).UnsafeDisableMemoryMappedIO();
					File.Delete(file);

					ModuleComparer.AssertEqual(expected, module, new ModuleDumpOptions { MaxStack = true });
				}
			}
		}

		sealed class CancelAfter : ICancellationToken {
			int callsLeft;
			public CancelAfter(int calls) => callsLeft = calls;
			public void ThrowIfCancellationRequested() {
				if (callsLeft-- <= 0)
					throw new OperationCanceledException();
			}
		}

		[Theory, InlineData(0), InlineData(1), InlineData(100)]
		public void LoadingCanBeCancelled(int callsBeforeCancel) {
			using (var module = ModuleDefMD.Load(Fixtures.Fixtures.Features.Image)) {
				Assert.Throws<OperationCanceledException>(() => module.LoadEverything(new CancelAfter(callsBeforeCancel)));

				// The module can still be used
				using (var expected = ModuleDefMD.Load(Fixtures.Fixtures.Features.Image))
					ModuleComparer.AssertEqual(expected, module);
			}
		}

		// A module that was created with the API can have members without a signature, a custom attribute
		// without a constructor, lists with missing parts, ...: LoadEverything() skips what isn't there
		[Fact]
		public void ModuleWithMissingPartsIsLoaded() {
			var module = dnlib.Tests.Model.ModuleWithMissingParts.Create();
			var type = module.Find("Ns.Type", isReflectionName: true)!;

			module.LoadEverything();

			// Nothing was changed
			Assert.Equal(2, type.Fields.Count);
			Assert.Equal(3, type.Methods.Count);
			Assert.Equal(2, type.CustomAttributes.Count);
			Assert.Equal(3, module.Resources.Count);
			Assert.Null(type.FindMethod("NoSignature")!.Signature);
		}
	}
}
