// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using dnlib.DotNet.MD;
using dnlib.PE;
using dnlib.W32Resources;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// <see cref="ModuleWriter"/> options
	/// </summary>
	public sealed class ModuleWriterOptions : ModuleWriterOptionsBase {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The module</param>
		public ModuleWriterOptions(ModuleDef module) : base(module) { }
	}

	/// <summary>
	/// Writes a .NET PE file. See also <see cref="NativeModuleWriter"/>
	/// </summary>
	public sealed class ModuleWriter : ModuleWriterBase {
		const uint DEFAULT_RELOC_ALIGNMENT = 4;
		const uint MVID_ALIGNMENT = 1;

		readonly ModuleDef module;
		ModuleWriterOptions options;

		List<PESection> sections;
		PESection mvidSection;
		PESection textSection;
		PESection sdataSection;
		PESection rsrcSection;
		PESection relocSection;

		PEHeaders peHeaders;
		ImportAddressTable importAddressTable;
		ImageCor20Header imageCor20Header;
		ImportDirectory importDirectory;
		StartupStub startupStub;
		RelocDirectory relocDirectory;
		ManagedExportsWriter managedExportsWriter;
		bool needStartupStub;

		/// <inheritdoc/>
		public override ModuleDef Module => module;

		/// <inheritdoc/>
		public override ModuleWriterOptionsBase TheOptions => Options;

		/// <summary>
		/// Gets/sets the writer options. This is never <c>null</c>
		/// </summary>
		public ModuleWriterOptions Options {
			get => options ?? (options = new ModuleWriterOptions(module));
			set => options = value;
		}

		/// <summary>
		/// Gets all <see cref="PESection"/>s. The reloc section must be the last section, so use <see cref="AddSection(PESection)"/> if you need to append a section
		/// </summary>
		public override List<PESection> Sections => sections;

		/// <summary>
		/// Adds <paramref name="section"/> to the sections list, but before the reloc section which must be last
		/// </summary>
		/// <param name="section">New section to add to the list</param>
		public override void AddSection(PESection section) {
			if (sections.Count > 0 && sections[sections.Count - 1] == relocSection)
				sections.Insert(sections.Count - 1, section);
			else
				sections.Add(section);
		}

		/// <summary>
		/// Gets the <c>.text</c> section
		/// </summary>
		public override PESection TextSection => textSection;

		/// <summary>
		/// Gets the <c>.sdata</c> section
		/// </summary>
		internal PESection SdataSection => sdataSection;

		/// <summary>
		/// Gets the <c>.rsrc</c> section or null if none
		/// </summary>
		public override PESection RsrcSection => rsrcSection;

		/// <summary>
		/// Gets the <c>.reloc</c> section
		/// </summary>
		public PESection RelocSection => relocSection;

		/// <summary>
		/// Gets the PE headers
		/// </summary>
		public PEHeaders PEHeaders => peHeaders;

		/// <summary>
		/// Gets the IAT or <c>null</c> if there's none
		/// </summary>
		public ImportAddressTable ImportAddressTable => importAddressTable;

		/// <summary>
		/// Gets the .NET header
		/// </summary>
		public ImageCor20Header ImageCor20Header => imageCor20Header;

		/// <summary>
		/// Gets the import directory or <c>null</c> if there's none
		/// </summary>
		public ImportDirectory ImportDirectory => importDirectory;

		/// <summary>
		/// Gets the startup stub or <c>null</c> if there's none
		/// </summary>
		public StartupStub StartupStub => startupStub;

		/// <summary>
		/// Gets the reloc directory or <c>null</c> if there's none
		/// </summary>
		public RelocDirectory RelocDirectory => relocDirectory;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The module</param>
		public ModuleWriter(ModuleDef module)
			: this(module, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The module</param>
		/// <param name="options">Options or <c>null</c></param>
		public ModuleWriter(ModuleDef module, ModuleWriterOptions options) {
			this.module = module;
			this.options = options;
		}

		/// <inheritdoc/>
		protected override long WriteImpl() {
			Initialize();
			metadata.CreateTables();
			return WriteFile();
		}

		void Initialize() {
			CreateSections();
			OnWriterEvent(ModuleWriterEvent.PESectionsCreated);

			CreateChunks();
			OnWriterEvent(ModuleWriterEvent.ChunksCreated);

			AddChunksToSections();
			OnWriterEvent(ModuleWriterEvent.ChunksAddedToSections);
		}

		/// <inheritdoc/>
		protected override Win32Resources GetWin32Resources() {
			if (Options.NoWin32Resources)
				return null;
			return Options.Win32Resources ?? module.Win32Resources;
		}

		void CreateSections() {
			sections = new List<PESection>();
			if (TheOptions.AddMvidSection)
				sections.Add(mvidSection = new PESection(".mvid", 0x42000040));
			sections.Add(textSection = new PESection(".text", 0x60000020));
			sections.Add(sdataSection = new PESection(".sdata", 0xC0000040));
			if (!(GetWin32Resources() is null))
				sections.Add(rsrcSection = new PESection(".rsrc", 0x40000040));
			// Should be last so any data in a previous section can add relocations
			sections.Add(relocSection = new PESection(".reloc", 0x42000040));
		}

		void CreateChunks() {
			peHeaders = new PEHeaders(Options.PEHeadersOptions);

			var machine = Options.PEHeadersOptions.Machine ?? Machine.I386;
			bool is64bit = machine.Is64Bit();
			relocDirectory = new RelocDirectory(machine);
			if (machine.IsI386())
				needStartupStub = true;

			importAddressTable = new ImportAddressTable(is64bit);
			importDirectory = new ImportDirectory(is64bit);
			startupStub = new StartupStub(relocDirectory, machine, (format, args) => Error(format, args));

			CreateStrongNameSignature();

			imageCor20Header = new ImageCor20Header(Options.Cor20HeaderOptions);
			CreateMetadataChunks(module);
			managedExportsWriter = new ManagedExportsWriter(UTF8String.ToSystemStringOrEmpty(module.Name), machine, relocDirectory, metadata, peHeaders, (format, args) => Error(format, args));

			CreateDebugDirectory();

			importDirectory.IsExeFile = Options.IsExeFile;
			peHeaders.IsExeFile = Options.IsExeFile;
		}

		void AddChunksToSections() {
			var machine = Options.PEHeadersOptions.Machine ?? Machine.I386;
			bool is64bit = machine.Is64Bit();
			uint pointerAlignment = is64bit ? 8U : 4;

			if (!(mvidSection is null))
				mvidSection.Add(new ByteArrayChunk((module.Mvid ?? Guid.Empty).ToByteArray()), MVID_ALIGNMENT);
			textSection.Add(importAddressTable, pointerAlignment);
			textSection.Add(imageCor20Header, DEFAULT_COR20HEADER_ALIGNMENT);
			textSection.Add(strongNameSignature, DEFAULT_STRONGNAMESIG_ALIGNMENT);
			managedExportsWriter.AddTextChunks(textSection);
			textSection.Add(constants, DEFAULT_CONSTANTS_ALIGNMENT);
			textSection.Add(methodBodies, DEFAULT_METHODBODIES_ALIGNMENT);
			textSection.Add(netResources, DEFAULT_NETRESOURCES_ALIGNMENT);
			textSection.Add(metadata, DEFAULT_METADATA_ALIGNMENT);
			textSection.Add(debugDirectory, DebugDirectory.DEFAULT_DEBUGDIRECTORY_ALIGNMENT);
			textSection.Add(importDirectory, pointerAlignment);
			textSection.Add(startupStub, startupStub.Alignment);
			managedExportsWriter.AddSdataChunks(sdataSection);
			if (!(GetWin32Resources() is null))
				rsrcSection.Add(win32Resources, DEFAULT_WIN32_RESOURCES_ALIGNMENT);
			relocSection.Add(relocDirectory, DEFAULT_RELOC_ALIGNMENT);
		}

		long WriteFile() {
			managedExportsWriter.AddExportedMethods(metadata.ExportedMethods, GetTimeDateStamp());
			if (managedExportsWriter.HasExports)
				needStartupStub = true;

			OnWriterEvent(ModuleWriterEvent.BeginWritePdb);
			WritePdbFile();
			OnWriterEvent(ModuleWriterEvent.EndWritePdb);

			metadata.OnBeforeSetOffset();
			OnWriterEvent(ModuleWriterEvent.BeginCalculateRvasAndFileOffsets);
			var chunks = new List<IChunk>();
			chunks.Add(peHeaders);
			if (!managedExportsWriter.HasExports)
				sections.Remove(sdataSection);
			if (!(relocDirectory.NeedsRelocSection || managedExportsWriter.HasExports || needStartupStub))
				sections.Remove(relocSection);

			importAddressTable.Enable = needStartupStub;
			importDirectory.Enable = needStartupStub;
			startupStub.Enable = needStartupStub;

			foreach (var section in sections)
				chunks.Add(section);
			peHeaders.PESections = sections;
			int relocIndex = sections.IndexOf(relocSection);
			if (relocIndex >= 0 && relocIndex != sections.Count - 1)
				throw new InvalidOperationException("Reloc section must be the last section, use AddSection() to add a section");
			CalculateRvasAndFileOffsets(chunks, 0, 0, peHeaders.FileAlignment, peHeaders.SectionAlignment);
			OnWriterEvent(ModuleWriterEvent.EndCalculateRvasAndFileOffsets);

			InitializeChunkProperties();

			OnWriterEvent(ModuleWriterEvent.BeginWriteChunks);
			var writer = new DataWriter(destStream);
			WriteChunks(writer, chunks, 0, peHeaders.FileAlignment);
			long imageLength = writer.Position - destStreamBaseOffset;
			OnWriterEvent(ModuleWriterEvent.EndWriteChunks);

			OnWriterEvent(ModuleWriterEvent.BeginStrongNameSign);
			if (!(Options.StrongNameKey is null))
				StrongNameSign((long)strongNameSignature.FileOffset);
			OnWriterEvent(ModuleWriterEvent.EndStrongNameSign);

			OnWriterEvent(ModuleWriterEvent.BeginWritePEChecksum);
			if (Options.AddCheckSum)
				peHeaders.WriteCheckSum(writer, imageLength);
			OnWriterEvent(ModuleWriterEvent.EndWritePEChecksum);

			return imageLength;
		}

		void InitializeChunkProperties() {
			Options.Cor20HeaderOptions.EntryPoint = GetEntryPoint();

			importAddressTable.ImportDirectory = importDirectory;
			importDirectory.ImportAddressTable = importAddressTable;
			startupStub.ImportDirectory = importDirectory;
			startupStub.PEHeaders = peHeaders;
			peHeaders.StartupStub = startupStub;
			peHeaders.ImageCor20Header = imageCor20Header;
			peHeaders.ImportAddressTable = importAddressTable;
			peHeaders.ImportDirectory = importDirectory;
			peHeaders.Win32Resources = win32Resources;
			peHeaders.RelocDirectory = relocDirectory;
			peHeaders.DebugDirectory = debugDirectory;
			imageCor20Header.Metadata = metadata;
			imageCor20Header.NetResources = netResources;
			imageCor20Header.StrongNameSignature = strongNameSignature;
			managedExportsWriter.InitializeChunkProperties();
		}

		uint GetEntryPoint() {
			if (module.ManagedEntryPoint is MethodDef methodEntryPoint)
				return new MDToken(Table.Method, metadata.GetRid(methodEntryPoint)).Raw;

			if (module.ManagedEntryPoint is FileDef fileEntryPoint)
				return new MDToken(Table.File, metadata.GetRid(fileEntryPoint)).Raw;

			uint nativeEntryPoint = (uint)module.NativeEntryPoint;
			if (nativeEntryPoint != 0)
				return nativeEntryPoint;

			return 0;
		}
	}
}
