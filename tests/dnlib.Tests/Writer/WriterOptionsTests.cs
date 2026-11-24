// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using Xunit;
using PEHeaders = System.Reflection.PortableExecutable.PEHeaders;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// The options of the writer that set fields of the PE headers and of the .NET header, and the events it
	/// raises while it writes. The written headers are read by System.Reflection.Metadata.
	/// </summary>
	public class WriterOptionsTests {
		const string Source = "public class Program { public static int Main() { return 0; } }";
		static readonly Lazy<byte[]> compiledExe = new Lazy<byte[]>(() =>
			CSharpCompiler.Compile(Source, new CSharpCompilerOptions { OutputKind = Microsoft.CodeAnalysis.OutputKind.ConsoleApplication }).Image);

		static byte[] Write(Action<ModuleWriterOptions> changeOptions) {
			using (var module = ModuleDefMD.Load(compiledExe.Value)) {
				var options = new ModuleWriterOptions(module);
				changeOptions(options);
				var stream = new MemoryStream();
				module.Write(stream, options);
				return stream.ToArray();
			}
		}

		static T ReadHeaders<T>(byte[] image, Func<PEHeaders, T> read) {
			using (var peReader = new PEReader(ImmutableArray.Create(image)))
				return read(peReader.PEHeaders);
		}

		[Fact]
		public void OptionalHeaderFieldsAreWritten() {
			var written = Write(options => {
				var headers = options.PEHeadersOptions;
				headers.ImageBase = 0x10000000;
				headers.SectionAlignment = 0x4000;
				headers.FileAlignment = 0x1000;
				headers.MajorLinkerVersion = 12;
				headers.MinorLinkerVersion = 34;
				headers.MajorOperatingSystemVersion = 6;
				headers.MinorOperatingSystemVersion = 1;
				headers.MajorImageVersion = 7;
				headers.MinorImageVersion = 8;
				headers.MajorSubsystemVersion = 6;
				headers.MinorSubsystemVersion = 2;
				headers.SizeOfStackReserve = 0x200000;
				headers.SizeOfStackCommit = 0x2000;
				headers.SizeOfHeapReserve = 0x300000;
				headers.SizeOfHeapCommit = 0x3000;
				headers.Subsystem = dnlib.PE.Subsystem.WindowsGui;
				headers.DllCharacteristics = dnlib.PE.DllCharacteristics.NxCompat | dnlib.PE.DllCharacteristics.TerminalServerAware;
			});

			var header = ReadHeaders(written, a => a.PEHeader!);
			Assert.Equal(0x10000000UL, header.ImageBase);
			Assert.Equal(0x4000, header.SectionAlignment);
			Assert.Equal(0x1000, header.FileAlignment);
			Assert.Equal((12, 34), (header.MajorLinkerVersion, header.MinorLinkerVersion));
			Assert.Equal((6, 1), (header.MajorOperatingSystemVersion, header.MinorOperatingSystemVersion));
			Assert.Equal((7, 8), (header.MajorImageVersion, header.MinorImageVersion));
			Assert.Equal((6, 2), (header.MajorSubsystemVersion, header.MinorSubsystemVersion));
			Assert.Equal((0x200000UL, 0x2000UL), (header.SizeOfStackReserve, header.SizeOfStackCommit));
			Assert.Equal((0x300000UL, 0x3000UL), (header.SizeOfHeapReserve, header.SizeOfHeapCommit));
			Assert.Equal(Subsystem.WindowsGui, header.Subsystem);
			Assert.Equal(DllCharacteristics.NxCompatible | DllCharacteristics.TerminalServerAware, header.DllCharacteristics);

			// The sections are where the alignments say
			var sections = ReadHeaders(written, a => a.SectionHeaders);
			Assert.All(sections, a => Assert.Equal(0, a.VirtualAddress % 0x4000));
			Assert.All(sections, a => Assert.Equal(0, a.PointerToRawData % 0x1000));
			Assert.Equal(0, written.Length % 0x1000);
			using (var module = ModuleDefMD.Load(written))
				Assert.Equal("Main", module.EntryPoint!.Name);
		}

		[Fact]
		public void FileHeaderFieldsAreWritten() {
			var written = Write(options => {
				options.PEHeadersOptions.TimeDateStamp = 0x12345678;
				// The Dll flag isn't an option: the kind of the module decides it, see ModuleDefTests
				options.PEHeadersOptions.Characteristics = dnlib.PE.Characteristics.ExecutableImage | dnlib.PE.Characteristics.LargeAddressAware | dnlib.PE.Characteristics.Dll;
				// The compiler's file is deterministic: its timestamp is calculated, see DeterministicWriteTests
				options.PdbOptions &= ~PdbWriterOptions.Deterministic;
			});

			var header = ReadHeaders(written, a => a.CoffHeader);
			Assert.Equal(0x12345678, header.TimeDateStamp);
			Assert.Equal(Characteristics.ExecutableImage | Characteristics.LargeAddressAware, header.Characteristics);
		}

		[Fact]
		public void DotNetHeaderFieldsAreWritten() {
			var written = Write(options => {
				options.Cor20HeaderOptions.MajorRuntimeVersion = 2;
				options.Cor20HeaderOptions.MinorRuntimeVersion = 5;
				options.Cor20HeaderOptions.Flags = dnlib.DotNet.MD.ComImageFlags.ILOnly | dnlib.DotNet.MD.ComImageFlags.Bit32Required;
			});

			var header = ReadHeaders(written, a => a.CorHeader!);
			Assert.Equal((2, 5), (header.MajorRuntimeVersion, header.MinorRuntimeVersion));
			Assert.Equal(CorFlags.ILOnly | CorFlags.Requires32Bit, header.Flags);
		}

		[Fact]
		public void MetadataHeaderFieldsAreWritten() {
			var written = Write(options => {
				options.MetadataOptions.MetadataHeaderOptions.VersionString = "v9.8.7";
				options.MetadataOptions.TablesHeapOptions.MajorVersion = 2;
				options.MetadataOptions.TablesHeapOptions.MinorVersion = 0;
			});

			using (var peReader = new PEReader(ImmutableArray.Create(written)))
				Assert.Equal("v9.8.7", System.Reflection.Metadata.PEReaderExtensions.GetMetadataReader(peReader).MetadataVersion);
			using (var module = ModuleDefMD.Load(written)) {
				Assert.Equal("v9.8.7", module.RuntimeVersion);
				Assert.Equal((ushort)0x0200, module.TablesHeaderVersion);
			}
		}

		// The writer tells listeners what it's doing: the events are raised in the order of the enum
		[Fact]
		public void WriterEventsAreRaisedInOrder() {
			var events = new List<ModuleWriterEvent>();
			Write(options => options.WriterEvent += (sender, e) => {
				Assert.NotNull(e.Writer);
				events.Add(e.Event);
			});

			Assert.Equal(ModuleWriterEvent.Begin, events[0]);
			Assert.Equal(ModuleWriterEvent.End, events[events.Count - 1]);
			Assert.Equal(events.OrderBy(a => (int)a), events);
			Assert.Equal(events.Count, events.Distinct().Count());
			Assert.Contains(ModuleWriterEvent.MDEndCreateTables, events);
			Assert.Contains(ModuleWriterEvent.BeginWriteChunks, events);
			Assert.Contains(ModuleWriterEvent.EndWritePEChecksum, events);
			// Every event of the enum is raised
			Assert.Equal(Enum.GetValues(typeof(ModuleWriterEvent)).Length, events.Count);
		}

		[Fact]
		public void ProgressGoesFromZeroToOne() {
			var progress = new List<double>();
			Write(options => options.ProgressUpdated += (sender, e) => progress.Add(e.Progress));

			Assert.True(progress.Count > 5);
			Assert.Equal(1.0, progress[progress.Count - 1]);
			Assert.All(progress, a => Assert.InRange(a, 0.0, 1.0));
			// It never goes back (it did, by a tiny amount, between two steps of the metadata writer)
			Assert.Equal(progress.OrderBy(a => a), progress);
		}

		// A listener can change the file before it's written, eg. add a section
		[Fact]
		public void ListenerCanAddASection() {
			var written = Write(options => options.WriterEvent += (sender, e) => {
				if (e.Event == ModuleWriterEvent.PESectionsCreated) {
					var section = new PESection(".extra", 0x40000040);
					section.Add(new ByteArrayChunk(new byte[] { 1, 2, 3, 4, 5 }), 4);
					e.Writer.AddSection(section);
				}
			});

			var extra = ReadHeaders(written, a => a.SectionHeaders).Single(a => a.Name == ".extra");
			Assert.Equal(5, extra.VirtualSize);
			Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, written.Skip(extra.PointerToRawData).Take(5));
			Assert.Equal(0, AssemblyRunner.RunEntryPoint(written));
		}
	}
}
