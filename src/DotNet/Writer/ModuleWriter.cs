// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.IO;
using dnlib.DotNet.MD;
using dnlib.W32Resources;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// <see cref="ModuleWriter"/> options
	/// </summary>
	public sealed class ModuleWriterOptions : ModuleWriterOptionsBase {
		/// <summary>
		/// Default constructor
		/// </summary>
		public ModuleWriterOptions() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The module</param>
		public ModuleWriterOptions(ModuleDef module)
			: this(module, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The module</param>
		/// <param name="listener">Module writer listener</param>
		public ModuleWriterOptions(ModuleDef module, IModuleWriterListener listener)
			: base(module, listener) {
		}
	}

	/// <summary>
	/// Writes a .NET PE file. See also <see cref="NativeModuleWriter"/>
	/// </summary>
	public sealed class ModuleWriter : ModuleWriterBase {
		const uint DEFAULT_IAT_ALIGNMENT = 4;
		const uint DEFAULT_IMPORTDIRECTORY_ALIGNMENT = 4;
		const uint DEFAULT_STARTUPSTUB_ALIGNMENT = 1;
		const uint DEFAULT_RELOC_ALIGNMENT = 4;

		readonly ModuleDef module;
		ModuleWriterOptions options;

		List<PESection> sections;
		PESection textSection;
		PESection rsrcSection;
		PESection relocSection;

		PEHeaders peHeaders;
		ImportAddressTable importAddressTable;
		ImageCor20Header imageCor20Header;
		ImportDirectory importDirectory;
		StartupStub startupStub;
		RelocDirectory relocDirectory;

		/// <inheritdoc/>
		public override ModuleDef Module {
			get { return module; }
		}

		/// <inheritdoc/>
		public override ModuleWriterOptionsBase TheOptions {
			get { return Options; }
		}

		/// <summary>
		/// Gets/sets the writer options. This is never <c>null</c>
		/// </summary>
		public ModuleWriterOptions Options {
			get { return options ?? (options = new ModuleWriterOptions(module)); }
			set { options = value; }
		}

		/// <summary>
		/// Gets all <see cref="PESection"/>s
		/// </summary>
		public override List<PESection> Sections {
			get { return sections; }
		}

		/// <summary>
		/// Gets the <c>.text</c> section
		/// </summary>
		public override PESection TextSection {
			get { return textSection; }
		}

		/// <summary>
		/// Gets the <c>.rsrc</c> section or <c>null</c> if there's none
		/// </summary>
		public override PESection RsrcSection {
			get { return rsrcSection; }
		}

		/// <summary>
		/// Gets the <c>.reloc</c> section or <c>null</c> if there's none
		/// </summary>
		public PESection RelocSection {
			get { return relocSection; }
		}

		/// <summary>
		/// Gets the PE headers
		/// </summary>
		public PEHeaders PEHeaders {
			get { return peHeaders; }
		}

		/// <summary>
		/// Gets the IAT or <c>null</c> if there's none
		/// </summary>
		public ImportAddressTable ImportAddressTable {
			get { return importAddressTable; }
		}

		/// <summary>
		/// Gets the .NET header
		/// </summary>
		public ImageCor20Header ImageCor20Header {
			get { return imageCor20Header; }
		}

		/// <summary>
		/// Gets the import directory or <c>null</c> if there's none
		/// </summary>
		public ImportDirectory ImportDirectory {
			get { return importDirectory; }
		}

		/// <summary>
		/// Gets the startup stub or <c>null</c> if there's none
		/// </summary>
		public StartupStub StartupStub {
			get { return startupStub; }
		}

		/// <summary>
		/// Gets the reloc directory or <c>null</c> if there's none
		/// </summary>
		public RelocDirectory RelocDirectory {
			get { return relocDirectory; }
		}

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
			metaData.CreateTables();
			return WriteFile();
		}

		void Initialize() {
			CreateSections();
			Listener.OnWriterEvent(this, ModuleWriterEvent.PESectionsCreated);

			CreateChunks();
			Listener.OnWriterEvent(this, ModuleWriterEvent.ChunksCreated);

			AddChunksToSections();
			Listener.OnWriterEvent(this, ModuleWriterEvent.ChunksAddedToSections);
		}

		/// <inheritdoc/>
		protected override Win32Resources GetWin32Resources() {
			return Options.Win32Resources ?? module.Win32Resources;
		}

		void CreateSections() {
			sections = new List<PESection>();
			sections.Add(textSection = new PESection(".text", 0x60000020));
			if (GetWin32Resources() != null)
				sections.Add(rsrcSection = new PESection(".rsrc", 0x40000040));
			if (!Options.Is64Bit)
				sections.Add(relocSection = new PESection(".reloc", 0x42000040));
		}

		void CreateChunks() {
			peHeaders = new PEHeaders(Options.PEHeadersOptions);

			if (!Options.Is64Bit) {
				importAddressTable = new ImportAddressTable();
				importDirectory = new ImportDirectory();
				startupStub = new StartupStub();
				relocDirectory = new RelocDirectory();
			}

			CreateStrongNameSignature();

			imageCor20Header = new ImageCor20Header(Options.Cor20HeaderOptions);
			CreateMetaDataChunks(module);

			CreateDebugDirectory();

			if (importDirectory != null)
				importDirectory.IsExeFile = Options.IsExeFile;

			peHeaders.IsExeFile = Options.IsExeFile;
		}

		void AddChunksToSections() {
			textSection.Add(importAddressTable, DEFAULT_IAT_ALIGNMENT);
			textSection.Add(imageCor20Header, DEFAULT_COR20HEADER_ALIGNMENT);
			textSection.Add(strongNameSignature, DEFAULT_STRONGNAMESIG_ALIGNMENT);
			textSection.Add(constants, DEFAULT_CONSTANTS_ALIGNMENT);
			textSection.Add(methodBodies, DEFAULT_METHODBODIES_ALIGNMENT);
			textSection.Add(netResources, DEFAULT_NETRESOURCES_ALIGNMENT);
			textSection.Add(metaData, DEFAULT_METADATA_ALIGNMENT);
			textSection.Add(debugDirectory, DebugDirectory.DEFAULT_DEBUGDIRECTORY_ALIGNMENT);
			textSection.Add(importDirectory, DEFAULT_IMPORTDIRECTORY_ALIGNMENT);
			textSection.Add(startupStub, DEFAULT_STARTUPSTUB_ALIGNMENT);
			if (rsrcSection != null)
				rsrcSection.Add(win32Resources, DEFAULT_WIN32_RESOURCES_ALIGNMENT);
			if (relocSection != null)
				relocSection.Add(relocDirectory, DEFAULT_RELOC_ALIGNMENT);
		}

		long WriteFile() {
			Listener.OnWriterEvent(this, ModuleWriterEvent.BeginWritePdb);
			WritePdbFile();
			Listener.OnWriterEvent(this, ModuleWriterEvent.EndWritePdb);

			Listener.OnWriterEvent(this, ModuleWriterEvent.BeginCalculateRvasAndFileOffsets);
			var chunks = new List<IChunk>();
			chunks.Add(peHeaders);
			foreach (var section in sections)
				chunks.Add(section);
			peHeaders.PESections = sections;
			CalculateRvasAndFileOffsets(chunks, 0, 0, peHeaders.FileAlignment, peHeaders.SectionAlignment);
			Listener.OnWriterEvent(this, ModuleWriterEvent.EndCalculateRvasAndFileOffsets);

			InitializeChunkProperties();

			Listener.OnWriterEvent(this, ModuleWriterEvent.BeginWriteChunks);
			var writer = new BinaryWriter(destStream);
			WriteChunks(writer, chunks, 0, peHeaders.FileAlignment);
			long imageLength = writer.BaseStream.Position - destStreamBaseOffset;
			Listener.OnWriterEvent(this, ModuleWriterEvent.EndWriteChunks);

			Listener.OnWriterEvent(this, ModuleWriterEvent.BeginStrongNameSign);
			if (Options.StrongNameKey != null)
				StrongNameSign((long)strongNameSignature.FileOffset);
			Listener.OnWriterEvent(this, ModuleWriterEvent.EndStrongNameSign);

			Listener.OnWriterEvent(this, ModuleWriterEvent.BeginWritePEChecksum);
			if (Options.AddCheckSum)
				peHeaders.WriteCheckSum(writer, imageLength);
			Listener.OnWriterEvent(this, ModuleWriterEvent.EndWritePEChecksum);

			return imageLength;
		}

		void InitializeChunkProperties() {
			Options.Cor20HeaderOptions.EntryPoint = GetEntryPoint();

			if (importAddressTable != null) {
				importAddressTable.ImportDirectory = importDirectory;
				importDirectory.ImportAddressTable = importAddressTable;
				startupStub.ImportDirectory = importDirectory;
				startupStub.PEHeaders = peHeaders;
				relocDirectory.StartupStub = startupStub;
			}
			peHeaders.StartupStub = startupStub;
			peHeaders.ImageCor20Header = imageCor20Header;
			peHeaders.ImportAddressTable = importAddressTable;
			peHeaders.ImportDirectory = importDirectory;
			peHeaders.Win32Resources = win32Resources;
			peHeaders.RelocDirectory = relocDirectory;
			peHeaders.DebugDirectory = debugDirectory;
			imageCor20Header.MetaData = metaData;
			imageCor20Header.NetResources = netResources;
			imageCor20Header.StrongNameSignature = strongNameSignature;
		}

		uint GetEntryPoint() {
			var methodEntryPoint = module.ManagedEntryPoint as MethodDef;
			if (methodEntryPoint != null)
				return new MDToken(Table.Method, metaData.GetRid(methodEntryPoint)).Raw;

			var fileEntryPoint = module.ManagedEntryPoint as FileDef;
			if (fileEntryPoint != null)
				return new MDToken(Table.File, metaData.GetRid(fileEntryPoint)).Raw;

			uint nativeEntryPoint = (uint)module.NativeEntryPoint;
			if (nativeEntryPoint != 0)
				return nativeEntryPoint;

			return 0;
		}
	}
}
