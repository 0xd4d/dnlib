// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using dnlib.DotNet;
using dnlib.PE;
using dnlib.TestUtils;
using dnlib.TestUtils.Dumping;
using Xunit;
using ImageLayout = dnlib.PE.ImageLayout;

namespace dnlib.Tests.Loading {
	/// <summary>
	/// A module can be read from a byte array, a file (it's memory mapped), a stream, or memory that isn't
	/// managed. The data is either a file as it is on disk, or a file that the OS loaded: its sections are then
	/// at their addresses in memory (<see cref="ImageLayout.Memory"/>). Every way must give the same module.
	/// </summary>
	public sealed class LoadPathTests : IDisposable {
		static byte[] Image => Fixtures.Fixtures.Features.Image;
		readonly ModuleDefMD expected = ModuleDefMD.Load(Image);

		public void Dispose() => expected.Dispose();

		void AssertIsTheExpectedModule(ModuleDefMD module) =>
			ModuleComparer.AssertEqual(expected, module, new ModuleDumpOptions { MaxStack = true });

		[Fact]
		public void FileName() {
			using (var directory = new TempDirectory()) {
				var file = directory.GetPath("Features.exe");
				File.WriteAllBytes(file, Image);

				using (var module = ModuleDefMD.Load(file)) {
					Assert.Equal(file, module.Location);
					AssertIsTheExpectedModule(module);
				}
				// The file isn't locked anymore
				File.Delete(file);
			}
		}

		[Fact]
		public void Stream() {
			using (var module = ModuleDefMD.Load(new MemoryStream(Image))) {
				Assert.Equal(string.Empty, module.Location);
				AssertIsTheExpectedModule(module);
			}
		}

		// The whole stream is read, whatever its position is
		[Fact]
		public void StreamThatIsNotAtItsStart() {
			var stream = new MemoryStream(Image);
			stream.Position = 100;

			using (var module = ModuleDefMD.Load(stream))
				AssertIsTheExpectedModule(module);
		}

		[Fact]
		public void PEImageOfAByteArray() {
			using (var module = ModuleDefMD.Load(new PEImage(Image)))
				AssertIsTheExpectedModule(module);
		}

		[Fact]
		public void UnmanagedMemoryWithTheFileLayout() {
			var memory = Marshal.AllocHGlobal(Image.Length);
			try {
				Marshal.Copy(Image, 0, memory, Image.Length);

				using (var module = ModuleDefMD.Load(memory, (ModuleCreationOptions?)null, ImageLayout.File))
					AssertIsTheExpectedModule(module);
			}
			finally {
				Marshal.FreeHGlobal(memory);
			}
		}

		// What the OS does when it loads a PE file: the headers stay at the start, and every section is copied
		// to its address (RVA)
		static byte[] CreateMemoryLayout(byte[] fileImage) {
			using (var peReader = new PEReader(ImmutableArray.Create(fileImage))) {
				var headers = peReader.PEHeaders;
				var memoryImage = new byte[headers.PEHeader!.SizeOfImage];
				Array.Copy(fileImage, memoryImage, headers.PEHeader.SizeOfHeaders);
				foreach (var section in headers.SectionHeaders)
					Array.Copy(fileImage, section.PointerToRawData, memoryImage, section.VirtualAddress, Math.Min(section.SizeOfRawData, section.VirtualSize));
				return memoryImage;
			}
		}

		[Fact]
		public void ByteArrayWithTheMemoryLayout() {
			var memoryImage = CreateMemoryLayout(Image);
			Assert.NotEqual(Image.Length, memoryImage.Length);

			using (var module = ModuleDefMD.Load(new PEImage(memoryImage, ImageLayout.Memory, verify: true)))
				AssertIsTheExpectedModule(module);
		}

		[Fact]
		public void UnmanagedMemoryWithTheMemoryLayout() {
			var memoryImage = CreateMemoryLayout(Image);
			var memory = Marshal.AllocHGlobal(memoryImage.Length);
			try {
				Marshal.Copy(memoryImage, 0, memory, memoryImage.Length);

				// The memory layout is the default for an address: it's where a loaded module is
				using (var module = ModuleDefMD.Load(memory))
					AssertIsTheExpectedModule(module);
				using (var module = ModuleDefMD.Load(memory, (ModuleCreationOptions?)null, ImageLayout.Memory))
					AssertIsTheExpectedModule(module);
			}
			finally {
				Marshal.FreeHGlobal(memory);
			}
		}

		// AssemblyDef.Load() reads the module at an address too and returns its assembly
		[Fact]
		public void AssemblyInUnmanagedMemory() {
			var memoryImage = CreateMemoryLayout(Image);
			var memory = Marshal.AllocHGlobal(memoryImage.Length);
			try {
				Marshal.Copy(memoryImage, 0, memory, memoryImage.Length);

				var assembly = AssemblyDef.Load(memory);
				using (assembly.ManifestModule) {
					AssertIsTheExpectedModule((ModuleDefMD)assembly.ManifestModule!);
					Assert.Same(assembly, assembly.ManifestModule!.Assembly);
				}
				var context = ModuleDef.CreateModuleContext();
				var assemblyWithContext = AssemblyDef.Load(memory, context);
				using (assemblyWithContext.ManifestModule)
					Assert.Same(context, assemblyWithContext.ManifestModule!.Context);
			}
			finally {
				Marshal.FreeHGlobal(memory);
			}
			Assert.Throws<ArgumentNullException>(() => AssemblyDef.Load(IntPtr.Zero));
		}

		// A module without an assembly (a .netmodule) isn't an assembly: the memory isn't kept
		[Fact]
		public void ModuleWithoutAnAssemblyInUnmanagedMemoryIsNotAnAssembly() {
			var netModule = CSharpCompiler.Compile("public class C { }", new CSharpCompilerOptions { OutputKind = Microsoft.CodeAnalysis.OutputKind.NetModule }).Image;
			var memoryImage = CreateMemoryLayout(netModule);
			var memory = Marshal.AllocHGlobal(memoryImage.Length);
			try {
				Marshal.Copy(memoryImage, 0, memory, memoryImage.Length);

				var exception = Assert.Throws<BadImageFormatException>(() => AssemblyDef.Load(memory));
				Assert.Contains("only a .NET module", exception.Message);
				using (var module = ModuleDefMD.Load(memory))
					Assert.Null(module.Assembly);
			}
			finally {
				Marshal.FreeHGlobal(memory);
			}
		}

		// A module that the runtime has loaded is read from the runtime's memory
		[Fact]
		public void ModuleOfTheRunningProgram() {
			var runtimeModule = typeof(LoadPathTests).Module;

			using (var fromRuntime = ModuleDefMD.Load(runtimeModule))
			using (var fromFile = ModuleDefMD.Load(File.ReadAllBytes(runtimeModule.FullyQualifiedName))) {
				ModuleComparer.AssertEqual(fromFile, fromRuntime, new ModuleDumpOptions { MaxStack = true });
			}
		}

		[Fact]
		public void ModuleWrittenToAFileIsReadFromTheFile() {
			using (var directory = new TempDirectory()) {
				var file = directory.GetPath("Written.exe");

				expected.Write(file);

				using (var module = ModuleDefMD.Load(file))
					ModuleComparer.AssertEqual(expected, module);
			}
		}

		[Fact]
		public void DataThatIsNotAPEFileIsRejected() {
			Assert.Throws<BadImageFormatException>(() => ModuleDefMD.Load(new byte[100]));
			Assert.Throws<BadImageFormatException>(() => ModuleDefMD.Load(new MemoryStream(new byte[] { (byte)'M', (byte)'Z', 0, 0 })));
			Assert.ThrowsAny<IOException>(() => ModuleDefMD.Load(Path.Combine(Path.GetTempPath(), "dnlib-test-file-that-does-not-exist.dll")));
		}

		// The overloads with a context or options: the module gets them
		[Fact]
		public void TheOverloadsWithAContextUseIt() {
			var context = new ModuleContext();
			var runtimeModule = typeof(LoadPathTests).Module;
			var memory = Marshal.AllocHGlobal(Image.Length);
			try {
				Marshal.Copy(Image, 0, memory, Image.Length);
				var modules = new[] {
					ModuleDefMD.Load(runtimeModule, context),
					ModuleDefMD.Load(runtimeModule, new ModuleCreationOptions(context)),
					ModuleDefMD.Load(runtimeModule, context, ImageLayout.Memory),
					ModuleDefMD.Load(new PEImage(Image), context),
					ModuleDefMD.Load(new PEImage(Image), new ModuleCreationOptions(context)),
					ModuleDefMD.Load(memory, context, ImageLayout.File),
				};
				foreach (var module in modules) {
					Assert.Same(context, module.Context);
					module.Dispose();
				}
				using (var module = ModuleDefMD.Load(new PEImage(Image)))
					AssertIsTheExpectedModule(module);
			}
			finally {
				Marshal.FreeHGlobal(memory);
			}
		}

		// A module that was created in memory (System.Reflection.Emit) has no file and no PE image.
		// Fixed in this commit: on .NET, dnlib tried to read a file with the name of the module (FileNotFoundException).
		[Fact]
		public void ADynamicModuleCanNotBeRead() {
			var assemblyBuilder = System.Reflection.Emit.AssemblyBuilder.DefineDynamicAssembly(new System.Reflection.AssemblyName("Dynamic"), System.Reflection.Emit.AssemblyBuilderAccess.Run);
			var moduleBuilder = assemblyBuilder.DefineDynamicModule("Dynamic");

			Assert.Throws<InvalidOperationException>(() => ModuleDefMD.Load(moduleBuilder));
		}
	}
}
