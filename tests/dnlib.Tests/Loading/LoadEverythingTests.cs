// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
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
	}
}
