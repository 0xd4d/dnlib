// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Runtime.InteropServices;
using dnlib.DotNet;
using dnlib.DotNet.Pdb;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Pdb {
	/// <summary>
	/// The custom debug info of a method in a Windows PDB is one blob of records, each with a kind. Each kind
	/// that dnlib knows must be the class of that kind (eg. <see cref="PdbUsingGroupsCustomDebugInfo"/>), any
	/// other kind must be a <see cref="PdbUnknownCustomDebugInfo"/>, and a kind is a byte. Anything else is a
	/// writer error.
	/// </summary>
	public class WindowsPdbCustomDebugInfoErrorTests {
		static void SkipUnlessWindows() =>
			Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "A Windows PDB can only be created on Windows");

		// Another class than the one of its kind
		sealed class OtherInfo : PdbCustomDebugInfo {
			readonly PdbCustomDebugInfoKind kind;
			public OtherInfo(PdbCustomDebugInfoKind kind) => this.kind = kind;
			public override PdbCustomDebugInfoKind Kind => kind;
			public override Guid Guid => Guid.Empty;
		}

		static CompiledAssembly Write(Action<MethodDef> addInfo, ILogger? logger = null) {
			using (var module = ModuleDefMD.Load(DebugInfoSource.CompiledWithWindowsPdb.Image, new ModuleCreationOptions {
				PdbFileOrData = DebugInfoSource.CompiledWithWindowsPdb.Pdb,
				TryToLoadPdbFromDisk = false,
			})) {
				var method = DebugInfoSource.GetMethod(module, "Sum");
				method.CustomDebugInfos.Clear();
				addInfo(method);
				var options = new ModuleWriterOptions(module) { WritePdb = true, PdbStream = new MemoryStream(), PdbFileName = "written.pdb" };
				if (logger is not null)
					options.Logger = logger;
				var image = new MemoryStream();
				module.Write(image, options);
				return new CompiledAssembly(image.ToArray(), ((MemoryStream)options.PdbStream).ToArray());
			}
		}

		static void AssertIsAnError(Action<MethodDef> addInfo, string expectedError) {
			var exception = Assert.Throws<ModuleWriterException>(() => Write(addInfo));
			Assert.Contains(expectedError, exception.Message);

			// If the error is ignored, the rest of the PDB is written
			var written = Write(addInfo, DummyLogger.NoThrowInstance);
			using (var module = RoundTrip.LoadWithPdb(written.Image, written.Pdb!))
				Assert.NotNull(module.PdbState);
		}

		[Theory]
		[InlineData(PdbCustomDebugInfoKind.UsingGroups)]
		[InlineData(PdbCustomDebugInfoKind.ForwardMethodInfo)]
		[InlineData(PdbCustomDebugInfoKind.ForwardModuleInfo)]
		[InlineData(PdbCustomDebugInfoKind.StateMachineHoistedLocalScopes)]
		[InlineData(PdbCustomDebugInfoKind.StateMachineTypeName)]
		[InlineData(PdbCustomDebugInfoKind.DynamicLocals)]
		[InlineData(PdbCustomDebugInfoKind.EditAndContinueLocalSlotMap)]
		[InlineData(PdbCustomDebugInfoKind.EditAndContinueLambdaMap)]
		[InlineData(PdbCustomDebugInfoKind.TupleElementNames)]
		[InlineData(PdbCustomDebugInfoKind.EditAndContinueStateMachineStateMap)]
		public void AKnownKindMustHaveItsClass(PdbCustomDebugInfoKind kind) {
			SkipUnlessWindows();
			AssertIsAnError(method => method.CustomDebugInfos.Add(new OtherInfo(kind)), "Unsupported custom debug info type");
		}

		// A kind that dnlib doesn't know can only be written with its bytes
		[Fact]
		public void AnUnknownKindMustBeAnUnknownInfo() {
			SkipUnlessWindows();
			AssertIsAnError(method => method.CustomDebugInfos.Add(new OtherInfo((PdbCustomDebugInfoKind)200)), "Unsupported custom debug info class");
		}

		// A kind that isn't a byte can't be the kind of a Windows PDB record
		[Fact]
		public void AKindThatIsNotAByteIsNotSupported() {
			SkipUnlessWindows();
			AssertIsAnError(method => method.CustomDebugInfos.Add(new OtherInfo((PdbCustomDebugInfoKind)0x1234)), "isn't supported by Windows");
		}

		// An unknown kind is written with its bytes and read again
		[Fact]
		public void AnUnknownKindIsWrittenWithItsBytes() {
			SkipUnlessWindows();
			var written = Write(method => method.CustomDebugInfos.Add(new PdbUnknownCustomDebugInfo((PdbCustomDebugInfoKind)200, new byte[] { 1, 2, 3, 4 })));

			using (var module = RoundTrip.LoadWithPdb(written.Image, written.Pdb!)) {
				var info = Assert.Single(DebugInfoSource.GetMethod(module, "Sum").CustomDebugInfos, a => a.Kind == (PdbCustomDebugInfoKind)200);
				Assert.Equal(new byte[] { 1, 2, 3, 4 }, Assert.IsType<PdbUnknownCustomDebugInfo>(info).Data);
			}
		}

		// A Windows PDB can have at most 255 custom debug infos per method
		[Fact]
		public void AMethodCanHaveAtMost255Infos() {
			SkipUnlessWindows();
			AssertIsAnError(method => {
				for (int i = 0; i < 256; i++)
					method.CustomDebugInfos.Add(new PdbUnknownCustomDebugInfo((PdbCustomDebugInfoKind)200, new byte[4]));
			}, "Too many custom debug infos");
		}

		// The names of the dynamic locals and the tuple element names are strings without a zero char
		[Fact]
		public void ANameWithAZeroCharIsAnError() {
			SkipUnlessWindows();
			AssertIsAnError(method => {
				var dynamicLocals = new PdbDynamicLocalsCustomDebugInfo();
				var local = new PdbDynamicLocal { Name = "bad\0name" };
				local.Flags.Add(1);
				dynamicLocals.Locals.Add(local);
				method.CustomDebugInfos.Add(dynamicLocals);
			}, "contains a NUL char");
		}
	}
}
