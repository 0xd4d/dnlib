// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.PE;
using dnlib.TestUtils;
using Xunit;
using Characteristics = dnlib.PE.Characteristics;
using DllCharacteristics = dnlib.PE.DllCharacteristics;
using Machine = dnlib.PE.Machine;
using PEHeaders = System.Reflection.PortableExecutable.PEHeaders;
using Subsystem = System.Reflection.PortableExecutable.Subsystem;

namespace dnlib.Tests.Model {
	/// <summary>
	/// The properties of <see cref="ModuleDef"/> that end up in the headers of the written file: the kind of
	/// file, the CPU, the runtime version, the .NET header flags and the entry point. The written headers are
	/// read by System.Reflection.Metadata.
	/// </summary>
	public class ModuleDefTests {
		const string Source = "public class Program { public static int Main() { return 0; } public static void Other() { } }";
		static readonly Lazy<byte[]> compiledExe = new Lazy<byte[]>(() =>
			CSharpCompiler.Compile(Source, new CSharpCompilerOptions { OutputKind = Microsoft.CodeAnalysis.OutputKind.ConsoleApplication }).Image);

		static ModuleDefMD Load() => ModuleDefMD.Load(compiledExe.Value);

		static byte[] Write(ModuleDef module, Action<ModuleWriterOptions>? changeOptions = null) {
			var options = new ModuleWriterOptions(module);
			changeOptions?.Invoke(options);
			var stream = new MemoryStream();
			module.Write(stream, options);
			return stream.ToArray();
		}

		static T ReadHeaders<T>(byte[] image, Func<PEHeaders, T> read) {
			using (var peReader = new PEReader(ImmutableArray.Create(image)))
				return read(peReader.PEHeaders);
		}

		[Fact]
		public void CompiledProgramIsAConsoleProgramForAnyCpu() {
			using (var module = Load()) {
				Assert.Equal(ModuleKind.Console, module.Kind);
				Assert.Equal(Machine.I386, module.Machine);
				Assert.True(module.IsI386);
				Assert.True(module.IsILOnly);
				Assert.False(module.Is32BitRequired);
				Assert.False(module.Is32BitPreferred);
				Assert.False(module.IsStrongNameSigned);
				Assert.False(module.HasNativeEntryPoint);
				Assert.True(module.IsManifestModule);
				Assert.Equal("Main", module.EntryPoint!.Name);
				Assert.Same(module.EntryPoint, module.ManagedEntryPoint);
				Assert.True(module.IsEntryPointValid && module.IsManagedEntryPointValid && !module.IsNativeEntryPointValid);
				Assert.Equal("<Module>", module.GlobalType!.Name);
				Assert.True(module.IsClr40 && module.IsClr40Exactly);
				Assert.False(module.IsClr20 || module.IsClr1x || module.IsEcma2002 || module.IsEcma2005);
			}
		}

		[Theory]
		[InlineData(ModuleKind.Dll, true, Subsystem.WindowsCui)]
		[InlineData(ModuleKind.NetModule, true, Subsystem.WindowsCui)]
		[InlineData(ModuleKind.Console, false, Subsystem.WindowsCui)]
		[InlineData(ModuleKind.Windows, false, Subsystem.WindowsGui)]
		public void KindOfTheModuleIsWrittenToThePEHeaders(ModuleKind kind, bool isDll, Subsystem subsystem) {
			using (var module = Load()) {
				module.Kind = kind;

				var written = Write(module);

				Assert.Equal(isDll, ReadHeaders(written, a => a.IsDll));
				Assert.Equal(subsystem, ReadHeaders(written, a => a.PEHeader!.Subsystem));
				using (var writtenModule = ModuleDefMD.Load(written))
					Assert.Equal(kind == ModuleKind.NetModule ? ModuleKind.Dll : kind, writtenModule.Kind);
			}
		}

		[Theory]
		[InlineData(Machine.I386, System.Reflection.PortableExecutable.Machine.I386, PEMagic.PE32)]
		[InlineData(Machine.AMD64, System.Reflection.PortableExecutable.Machine.Amd64, PEMagic.PE32Plus)]
		[InlineData(Machine.ARM64, System.Reflection.PortableExecutable.Machine.Arm64, PEMagic.PE32Plus)]
		[InlineData(Machine.ARMNT, System.Reflection.PortableExecutable.Machine.ArmThumb2, PEMagic.PE32)]
		public void MachineDecidesTheSizeOfThePEHeader(Machine machine, System.Reflection.PortableExecutable.Machine expectedMachine, PEMagic expectedMagic) {
			using (var module = Load()) {
				module.Machine = machine;

				var written = Write(module);

				Assert.Equal(expectedMachine, ReadHeaders(written, a => a.CoffHeader.Machine));
				Assert.Equal(expectedMagic, ReadHeaders(written, a => a.PEHeader!.Magic));
				using (var writtenModule = ModuleDefMD.Load(written)) {
					Assert.Equal(machine, writtenModule.Machine);
					Assert.Equal("Main", writtenModule.EntryPoint!.Name);
				}
			}
		}

		[Fact]
		public void DotNetHeaderFlagsAreWritten() {
			using (var module = Load()) {
				module.Is32BitRequired = true;
				module.Is32BitPreferred = true;
				Assert.Equal(ComImageFlags.ILOnly | ComImageFlags.Bit32Required | ComImageFlags.Bit32Preferred, module.Cor20HeaderFlags);

				var written = Write(module);

				Assert.Equal(CorFlags.ILOnly | CorFlags.Requires32Bit | CorFlags.Prefers32Bit, ReadHeaders(written, a => a.CorHeader!.Flags));
				using (var writtenModule = ModuleDefMD.Load(written))
					Assert.True(writtenModule.Is32BitRequired && writtenModule.Is32BitPreferred && writtenModule.IsILOnly);
			}
		}

		[Fact]
		public void CharacteristicsAreWritten() {
			using (var module = Load()) {
				module.Characteristics |= Characteristics.LargeAddressAware;
				module.DllCharacteristics = DllCharacteristics.DynamicBase | DllCharacteristics.NxCompat | DllCharacteristics.HighEntropyVA;

				var written = Write(module);

				Assert.True((ReadHeaders(written, a => a.CoffHeader.Characteristics) & System.Reflection.PortableExecutable.Characteristics.LargeAddressAware) != 0);
				Assert.Equal(
					DllCharacteristics.DynamicBase | DllCharacteristics.NxCompat | DllCharacteristics.HighEntropyVA,
					(DllCharacteristics)ReadHeaders(written, a => a.PEHeader!.DllCharacteristics));
			}
		}

		[Theory]
		[InlineData(MDHeaderRuntimeVersion.MS_CLR_20, true, false)]
		[InlineData(MDHeaderRuntimeVersion.MS_CLR_40, false, true)]
		[InlineData(MDHeaderRuntimeVersion.MS_CLR_11, false, false)]
		public void RuntimeVersionIsTheVersionStringOfTheMetadata(string runtimeVersion, bool isClr20, bool isClr40) {
			using (var module = Load()) {
				module.RuntimeVersion = runtimeVersion;
				Assert.Equal(isClr20, module.IsClr20);
				Assert.Equal(isClr40, module.IsClr40);
				Assert.Equal(runtimeVersion == MDHeaderRuntimeVersion.MS_CLR_11, module.IsClr11 && module.IsClr1x);

				var written = Write(module);

				using (var peReader = new PEReader(ImmutableArray.Create(written)))
					Assert.Equal(runtimeVersion, peReader.GetMetadataReader().MetadataVersion);
				using (var writtenModule = ModuleDefMD.Load(written))
					Assert.Equal(runtimeVersion, writtenModule.RuntimeVersion);
			}
		}

		[Fact]
		public void EntryPointCanBeChangedAndRemoved() {
			using (var module = Load()) {
				var other = module.Find("Program", isReflectionName: true)!.FindMethod("Other");
				module.EntryPoint = other;
				using (var written = ModuleDefMD.Load(Write(module)))
					Assert.Equal("Other", written.EntryPoint!.Name);

				module.EntryPoint = null;
				module.Kind = ModuleKind.Dll;
				var withoutEntryPoint = Write(module);
				Assert.Equal(0, ReadHeaders(withoutEntryPoint, a => a.CorHeader!.EntryPointTokenOrRelativeVirtualAddress));
				using (var written = ModuleDefMD.Load(withoutEntryPoint)) {
					Assert.Null(written.EntryPoint);
					Assert.False(written.IsEntryPointValid);
				}
			}
		}

		// The entry point is native code: the header has its address instead of a method token
		[Fact]
		public void NativeEntryPoint() {
			using (var module = Load()) {
				module.NativeEntryPoint = (RVA)0x2000;

				Assert.True(module.HasNativeEntryPoint);
				Assert.True(module.IsNativeEntryPointValid);
				Assert.Null(module.ManagedEntryPoint);
				Assert.Null(module.EntryPoint);
				Assert.True((module.Cor20HeaderFlags & ComImageFlags.NativeEntryPoint) != 0);

				var written = Write(module);
				Assert.Equal(0x2000, ReadHeaders(written, a => a.CorHeader!.EntryPointTokenOrRelativeVirtualAddress));
				Assert.True((ReadHeaders(written, a => a.CorHeader!.Flags) & CorFlags.NativeEntryPoint) != 0);

				module.EntryPoint = module.Find("Program", isReflectionName: true)!.FindMethod("Main");
				Assert.False(module.HasNativeEntryPoint);
				Assert.Equal((RVA)0, module.NativeEntryPoint);
			}
		}

		[Fact]
		public void ModuleIdsAreWritten() {
			using (var module = Load()) {
				var mvid = new Guid("11111111-2222-3333-4444-555555555555");
				var encId = new Guid("66666666-7777-8888-9999-aaaaaaaaaaaa");
				module.Mvid = mvid;
				module.EncId = encId;
				module.EncBaseId = null;
				module.Generation = 3;

				var written = Write(module);

				using (var peReader = new PEReader(ImmutableArray.Create(written))) {
					var reader = peReader.GetMetadataReader();
					var row = reader.GetModuleDefinition();
					Assert.Equal(mvid, reader.GetGuid(row.Mvid));
					Assert.Equal(encId, reader.GetGuid(row.GenerationId));
					Assert.True(row.BaseGenerationId.IsNil);
					Assert.Equal(3, row.Generation);
				}
				using (var writtenModule = ModuleDefMD.Load(written)) {
					Assert.Equal(mvid, writtenModule.Mvid);
					Assert.Equal(encId, writtenModule.EncId);
					Assert.Null(writtenModule.EncBaseId);
				}
			}
		}

		[Fact]
		public void NewModuleHasAGlobalTypeAndAnMvid() {
			var module = new ModuleDefUser("New.dll");

			Assert.Equal("New.dll", module.Name);
			Assert.NotNull(module.Mvid);
			Assert.NotEqual(Guid.Empty, module.Mvid);
			Assert.Equal("<Module>", Assert.Single(module.Types).Name);
			Assert.True(module.GlobalType!.IsGlobalModuleType);
			Assert.Null(module.Assembly);
			Assert.False(module.IsManifestModule);
			Assert.Equal(ModuleKind.Windows, module.Kind);
			Assert.Equal("mscorlib", module.CorLibTypes.AssemblyRef.Name);
			Assert.Equal(string.Empty, module.Location);

			new AssemblyDefUser("New").Modules.Add(module);
			Assert.True(module.IsManifestModule);
		}

		// A type is in one list: the module's or the nested types of another type
		[Fact]
		public void AddAsNonNestedTypeMovesANestedTypeToTheModule() {
			var module = new ModuleDefUser("New.dll");
			var outer = new TypeDefUser("Ns", "Outer");
			var nested = new TypeDefUser("Nested") { Attributes = TypeAttributes.NestedPublic };
			module.Types.Add(outer);
			outer.NestedTypes.Add(nested);
			Assert.Same(outer, nested.DeclaringType);
			Assert.Throws<InvalidOperationException>(() => module.Types.Add(nested));

			module.AddAsNonNestedType(nested);

			Assert.Null(nested.DeclaringType);
			Assert.Empty(outer.NestedTypes);
			Assert.Contains(nested, module.Types);
			Assert.Same(module, nested.Module);
		}
	}
}
