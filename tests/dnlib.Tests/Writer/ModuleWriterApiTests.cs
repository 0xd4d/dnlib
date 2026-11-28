// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// The parts of <see cref="ModuleWriterBase"/> that a listener of the writer's events uses: the chunks that
	/// the writer creates (they don't exist before <c>Write()</c>), the options, and how the ID of a portable PDB
	/// is created.
	/// </summary>
	public class ModuleWriterApiTests {
		static CreatedModule CreateModule() {
			var created = new CreatedModule();
			created.AddMethod("Method", OpCodes.Ret.ToInstruction());
			return created;
		}

		// The chunks are created by Write()
		[Fact]
		public void TheChunksDoNotExistBeforeWrite() {
			var created = CreateModule();
			var writer = new ModuleWriter(created.Module, new ModuleWriterOptions(created.Module));

			Assert.Throws<InvalidOperationException>(() => writer.Constants);
			Assert.Throws<InvalidOperationException>(() => writer.MethodBodies);
			Assert.Throws<InvalidOperationException>(() => writer.NetResources);
			Assert.Throws<InvalidOperationException>(() => writer.DestinationStream);
			Assert.Throws<InvalidOperationException>(() => writer.Metadata);
			Assert.False(writer.IsNativeWriter);
		}

		[Fact]
		public void TheChunksExistWhileTheModuleIsWritten() {
			var created = CreateModule();
			var options = new ModuleWriterOptions(created.Module);
			var stream = new MemoryStream();
			bool asked = false;
			options.WriterEvent += (sender, e) => {
				if (e.Event != ModuleWriterEvent.End)
					return;
				var writer = e.Writer;
				Assert.NotNull(writer.Constants);
				Assert.NotNull(writer.MethodBodies);
				Assert.NotNull(writer.NetResources);
				Assert.Same(stream, writer.DestinationStream);
				Assert.Contains(writer.Sections, a => a.Name == ".text");
				Assert.Null(writer.Win32Resources);
				Assert.Null(writer.StrongNameSignature);
				asked = true;
			};

			created.Module.Write(stream, options);

			Assert.True(asked);
		}

		// The options of the headers can be replaced
		[Fact]
		public void TheOptionsOfTheHeadersCanBeReplaced() {
			var created = CreateModule();
			var options = new ModuleWriterOptions(created.Module) {
				PEHeadersOptions = new PEHeadersOptions { MajorLinkerVersion = 42 },
				Cor20HeaderOptions = new Cor20HeaderOptions { MinorRuntimeVersion = 7 },
				MetadataOptions = new MetadataOptions(new MetadataHeaderOptions { VersionString = "v4.0.12345" }, MetadataFlags.PreserveAll),
			};
			var stream = new MemoryStream();

			created.Module.Write(stream, options);

			using (var peReader = new PEReader(ImmutableArray.Create(stream.ToArray()))) {
				Assert.Equal(42, peReader.PEHeaders.PEHeader!.MajorLinkerVersion);
				Assert.Equal(7, peReader.PEHeaders.CorHeader!.MinorRuntimeVersion);
			}
			using (var module = ModuleDefMD.Load(stream.ToArray()))
				Assert.Equal("v4.0.12345", module.RuntimeVersion);
		}

		// The other constructors of the metadata options
		[Fact]
		public void TheConstructorsOfTheMetadataOptions() {
			var headerOptions = new MetadataHeaderOptions();

			Assert.Equal(MetadataFlags.PreserveAll, new MetadataOptions(MetadataFlags.PreserveAll).Flags);
			Assert.Same(headerOptions, new MetadataOptions(headerOptions).MetadataHeaderOptions);
			var both = new MetadataOptions(headerOptions, MetadataFlags.PreserveTypeRefRids);
			Assert.Same(headerOptions, both.MetadataHeaderOptions);
			Assert.Equal(MetadataFlags.PreserveTypeRefRids, both.Flags);
			// The options of the PDB's metadata
			var options = new MetadataOptions();
			var debugHeaderOptions = new MetadataHeaderOptions();
			var tablesOptions = new TablesHeapOptions();
			var debugTablesOptions = new TablesHeapOptions();
			options.MetadataHeaderOptions = headerOptions;
			options.DebugMetadataHeaderOptions = debugHeaderOptions;
			options.TablesHeapOptions = tablesOptions;
			options.DebugTablesHeapOptions = debugTablesOptions;
			Assert.Same(headerOptions, options.MetadataHeaderOptions);
			Assert.Same(debugHeaderOptions, options.DebugMetadataHeaderOptions);
			Assert.Same(tablesOptions, options.TablesHeapOptions);
			Assert.Same(debugTablesOptions, options.DebugTablesHeapOptions);
		}

		// The ID of a portable PDB (a GUID and a time stamp) is normally a hash of the PDB. A listener can create it.
		[Fact]
		public void TheIdOfAPortablePdbCanBeCreatedByTheCaller() {
			var fixture = Fixtures.Fixtures.Features;
			var id = new Guid("11111111-2222-3333-4444-555555555555");
			byte[] image;
			var pdbStream = new MemoryStream();
			using (var module = RoundTrip.LoadWithPdb(fixture.Image, fixture.Pdb!)) {
				var options = new ModuleWriterOptions(module) {
					WritePdb = true,
					PdbStream = pdbStream,
					PdbFileName = "written.pdb",
					// The ID is a hash if the PDB must be deterministic or have a checksum (the defaults for this file)
					PdbOptions = PdbWriterOptions.None,
					GetPdbContentId = (stream, timestamp) => new ContentId(id, 0x12345678),
				};
				var stream = new MemoryStream();
				module.Write(stream, options);
				image = stream.ToArray();
			}

			using (var peReader = new PEReader(ImmutableArray.Create(image))) {
				var codeView = peReader.ReadDebugDirectory().Single(a => a.Type == DebugDirectoryEntryType.CodeView);
				Assert.Equal(id, peReader.ReadCodeViewDebugDirectoryData(codeView).Guid);
				Assert.Equal(0x12345678U, codeView.Stamp);
			}
		}

		[Fact]
		public void TheProgressIsBetweenZeroAndOne() {
			var created = CreateModule();
			var writer = new ModuleWriter(created.Module);

			Assert.Equal(0.5, new ModuleWriterProgressEventArgs(writer, 0.5).Progress);
			Assert.Throws<ArgumentOutOfRangeException>(() => new ModuleWriterProgressEventArgs(writer, 1.5));
			Assert.Throws<ArgumentOutOfRangeException>(() => new ModuleWriterProgressEventArgs(writer, -0.1));
		}
	}
}
