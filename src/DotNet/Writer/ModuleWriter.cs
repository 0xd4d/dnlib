using System.Collections.Generic;
using System.IO;
using dot10.DotNet.MD;
using dot10.PE;
using dot10.IO;
using dot10.W32Resources;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// <see cref="ModuleWriter"/> options
	/// </summary>
	public sealed class ModuleWriterOptions {
		MetaDataOptions metaDataOptions;
		Cor20HeaderOptions cor20HeaderOptions;
		PEHeadersOptions peHeadersOptions;
		IModuleWriterListener listener;
		Win32Resources win32Resources;

		/// <summary>
		/// Gets/sets the listener
		/// </summary>
		public IModuleWriterListener Listener {
			get { return listener; }
			set { listener = value; }
		}

		/// <summary>
		/// Gets/sets the <see cref="MetaData"/> options. This is never <c>null</c>.
		/// </summary>
		public MetaDataOptions MetaDataOptions {
			get { return metaDataOptions ?? (metaDataOptions = new MetaDataOptions()); }
			set { metaDataOptions = value; }
		}

		/// <summary>
		/// Gets/sets the <see cref="ImageCor20Header"/> options. This is never <c>null</c>.
		/// </summary>
		public Cor20HeaderOptions Cor20HeaderOptions {
			get { return cor20HeaderOptions ?? (cor20HeaderOptions = new Cor20HeaderOptions()); }
			set { cor20HeaderOptions = value; }
		}

		/// <summary>
		/// Gets/sets the <see cref="ImageCor20Header"/> options. This is never <c>null</c>.
		/// </summary>
		public PEHeadersOptions PEHeadersOptions {
			get { return peHeadersOptions ?? (peHeadersOptions = new PEHeadersOptions()); }
			set { peHeadersOptions = value; }
		}

		/// <summary>
		/// Gets/sets the Win32 resources. If this is <c>null</c>, use the module's
		/// Win32 resources if any.
		/// </summary>
		public Win32Resources Win32Resources {
			get { return win32Resources; }
			set { win32Resources = value; }
		}

		/// <summary>
		/// <c>true</c> if it's a 64-bit module, <c>false</c> if it's a 32-bit or AnyCPU module.
		/// </summary>
		public bool Is64Bit {
			get {
				if (!PEHeadersOptions.Machine.HasValue)
					return false;
				return PEHeadersOptions.Machine == Machine.IA64 ||
					PEHeadersOptions.Machine == Machine.AMD64;
			}
		}

		/// <summary>
		/// Gets/sets the module kind
		/// </summary>
		public ModuleKind ModuleKind { get; set; }

		/// <summary>
		/// <c>true</c> if it should be written as an EXE file, <c>false</c> if it should be
		/// written as a DLL file.
		/// </summary>
		public bool IsExeFile {
			get {
				return ModuleKind != ModuleKind.Dll &&
					ModuleKind != ModuleKind.NetModule;
			}
		}

		/// <summary>
		/// <c>true</c> if method bodies can be shared (two or more method bodies can share the
		/// same RVA), <c>false</c> if method bodies can't be shared. Don't enable it if there
		/// must be a 1:1 relationship with method bodies and their RVAs.
		/// </summary>
		public bool ShareMethodBodies { get; set; }

		/// <summary>
		/// <c>true</c> if the PE header CheckSum field should be updated, <c>false</c> if the
		/// CheckSum field should be cleared.
		/// </summary>
		public bool AddCheckSum { get; set; }

		/// <summary>
		/// Default constructor
		/// </summary>
		public ModuleWriterOptions() {
			ShareMethodBodies = true;
			ModuleKind = ModuleKind.Windows;
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
		public ModuleWriterOptions(ModuleDef module, IModuleWriterListener listener) {
			this.listener = listener;
			this.ShareMethodBodies = true;
			this.ModuleKind = module.Kind;
			this.PEHeadersOptions.Machine = module.Machine;
			if (module.Kind == ModuleKind.Windows)
				this.PEHeadersOptions.Subsystem = Subsystem.WindowsGui;
			else
				this.PEHeadersOptions.Subsystem = Subsystem.WindowsCui;
			this.Cor20HeaderOptions.Flags = module.Cor20HeaderFlags;
			this.MetaDataOptions.MetaDataHeaderOptions.VersionString = module.RuntimeVersion;

			// Some tools crash if #GUID is missing so always create it by default
			this.MetaDataOptions.Flags |= MetaDataFlags.AlwaysCreateGuidHeap;

			var modDefMD = module as ModuleDefMD;
			if (modDefMD != null) {
				var peImage = modDefMD.MetaData.PEImage;
				this.PEHeadersOptions.TimeDateStamp = peImage.ImageNTHeaders.FileHeader.TimeDateStamp;
				this.PEHeadersOptions.MajorLinkerVersion = peImage.ImageNTHeaders.OptionalHeader.MajorLinkerVersion;
				this.PEHeadersOptions.MinorLinkerVersion = peImage.ImageNTHeaders.OptionalHeader.MinorLinkerVersion;
				this.AddCheckSum = peImage.ImageNTHeaders.OptionalHeader.CheckSum != 0;
			}
		}
	}

	/// <summary>
	/// Writes a .NET PE file
	/// </summary>
	public sealed class ModuleWriter : IMetaDataListener {
		const uint DEFAULT_IAT_ALIGNMENT = 4;
		const uint DEFAULT_COR20HEADER_ALIGNMENT = 4;
		const uint DEFAULT_STRONGNAMESIG_ALIGNMENT = 16;
		internal const uint DEFAULT_CONSTANTS_ALIGNMENT = 8;
		const uint DEFAULT_METHODBODIES_ALIGNMENT = 4;
		const uint DEFAULT_NETRESOURCES_ALIGNMENT = 8;
		const uint DEFAULT_METADATA_ALIGNMENT = 4;
		const uint DEFAULT_DEBUGDIRECTORY_ALIGNMENT = 4;
		const uint DEFAULT_IMPORTDIRECTORY_ALIGNMENT = 4;
		const uint DEFAULT_STARTUPSTUB_ALIGNMENT = 1;
		internal const uint DEFAULT_WIN32_RESOURCE_ALIGNMENT = 8;
		const uint DEFAULT_RELOC_ALIGNMENT = 4;

		readonly ModuleDef module;
		ModuleWriterOptions options;
		Stream destStream;
		IModuleWriterListener listener;

		List<PESection> sections;
		PESection textSection;
		PESection rsrcSection;
		PESection relocSection;

		PEHeaders peHeaders;
		ImportAddressTable importAddressTable;
		ImageCor20Header imageCor20Header;
		StrongNameSignature strongNameSignature;
		UniqueChunkList<ByteArrayChunk> constants;
		MethodBodyChunks methodBodies;
		NetResources netResources;
		MetaData metaData;
		DebugDirectory debugDirectory;
		ImportDirectory importDirectory;
		StartupStub startupStub;
		Win32ResourcesChunk win32Resources;
		RelocDirectory relocDirectory;

		/// <summary>
		/// Gets the module
		/// </summary>
		public ModuleDef Module {
			get { return module; }
		}

		/// <summary>
		/// Gets/sets the writer options. This is never <c>null</c>
		/// </summary>
		public ModuleWriterOptions Options {
			get { return options ?? (options = new ModuleWriterOptions(module)); }
			set { options = value; }
		}

		/// <summary>
		/// Gets the destination stream
		/// </summary>
		public Stream DestinationStream {
			get { return destStream; }
		}

		/// <summary>
		/// Gets all <see cref="PESection"/>s
		/// </summary>
		public List<PESection> Sections {
			get { return sections; }
		}

		/// <summary>
		/// Gets the <c>.text</c> section
		/// </summary>
		public PESection TextSection {
			get { return textSection; }
		}

		/// <summary>
		/// Gets the <c>.rsrc</c> section or <c>null</c> if there's none
		/// </summary>
		public PESection RsrcSection {
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
		/// Gets the strong name signature or <c>null</c> if there's none
		/// </summary>
		public StrongNameSignature StrongNameSignature {
			get { return strongNameSignature; }
		}

		/// <summary>
		/// Gets the constants
		/// </summary>
		public UniqueChunkList<ByteArrayChunk> Constants {
			get { return constants; }
		}

		/// <summary>
		/// Gets the method bodies
		/// </summary>
		public MethodBodyChunks MethodBodies {
			get { return methodBodies; }
		}

		/// <summary>
		/// Gets the .NET resources
		/// </summary>
		public NetResources NetResources {
			get { return netResources; }
		}

		/// <summary>
		/// Gets the .NET metadata
		/// </summary>
		public MetaData MetaData {
			get { return metaData; }
		}

		/// <summary>
		/// Gets the debug directory or <c>null</c> if there's none
		/// </summary>
		public DebugDirectory DebugDirectory {
			get { return debugDirectory; }
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
		/// Gets the Win32 resources or <c>null</c> if there's none
		/// </summary>
		public Win32ResourcesChunk Win32Resources {
			get { return win32Resources; }
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

		/// <summary>
		/// Writes the module to a file
		/// </summary>
		/// <param name="fileName">File name. The file will be truncated if it exists.</param>
		public void Write(string fileName) {
			using (var dest = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite)) {
				dest.SetLength(0);
				try {
					Write(dest);
				}
				catch {
					// Writing failed. Delete the file since it's useless.
					dest.Close();
					DeleteFileNoThrow(fileName);
					throw;
				}
			}
		}

		static void DeleteFileNoThrow(string fileName) {
			try {
				File.Delete(fileName);
			}
			catch {
			}
		}

		/// <summary>
		/// Writes the module to a <see cref="Stream"/>
		/// </summary>
		/// <param name="dest">Destination stream</param>
		public void Write(Stream dest) {
			listener = Options.Listener ?? DummyModuleWriterListener.Instance;
			destStream = dest;

			listener.OnWriterEvent(this, ModuleWriterEvent.Begin);
			Initialize();

			metaData.CreateTables();

			WriteFile();
			listener.OnWriterEvent(this, ModuleWriterEvent.End);
		}

		void Initialize() {
			CreateSections();
			listener.OnWriterEvent(this, ModuleWriterEvent.PESectionsCreated);

			CreateChunks();
			listener.OnWriterEvent(this, ModuleWriterEvent.ChunksCreated);

			AddChunksToSections();
			listener.OnWriterEvent(this, ModuleWriterEvent.ChunksAddedToSections);
		}

		Win32Resources GetWin32Resources() {
			return module.Win32Resources ?? Options.Win32Resources;
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
			bool hasDebugDirectory = false;

			peHeaders = new PEHeaders(Options.PEHeadersOptions);

			if (!Options.Is64Bit) {
				importAddressTable = new ImportAddressTable();
				importDirectory = new ImportDirectory();
				startupStub = new StartupStub();
				relocDirectory = new RelocDirectory();
			}

			if (module.Assembly != null && !PublicKeyBase.IsNullOrEmpty2(module.Assembly.PublicKey)) {
				int len = module.Assembly.PublicKey.Data.Length - 0x20;
				strongNameSignature = new StrongNameSignature(len > 0 ? len : 0x80);
			}

			imageCor20Header = new ImageCor20Header(Options.Cor20HeaderOptions);
			constants = new UniqueChunkList<ByteArrayChunk>();
			methodBodies = new MethodBodyChunks(Options.ShareMethodBodies);
			netResources = new NetResources(DEFAULT_NETRESOURCES_ALIGNMENT);
			metaData = MetaData.Create(module, constants, methodBodies, netResources, Options.MetaDataOptions);
			metaData.Listener = this;
			if (hasDebugDirectory)
				debugDirectory = new DebugDirectory();

			var w32Resources = GetWin32Resources();
			if (w32Resources != null)
				win32Resources = new Win32ResourcesChunk(w32Resources);

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
			textSection.Add(debugDirectory, DEFAULT_DEBUGDIRECTORY_ALIGNMENT);
			textSection.Add(importDirectory, DEFAULT_IMPORTDIRECTORY_ALIGNMENT);
			textSection.Add(startupStub, DEFAULT_STARTUPSTUB_ALIGNMENT);
			if (rsrcSection != null)
				rsrcSection.Add(win32Resources, DEFAULT_WIN32_RESOURCE_ALIGNMENT);
			if (relocSection != null)
				relocSection.Add(relocDirectory, DEFAULT_RELOC_ALIGNMENT);
		}

		void WriteFile() {
			var chunks = new List<IChunk>();
			chunks.Add(peHeaders);
			foreach (var section in sections)
				chunks.Add(section);

			listener.OnWriterEvent(this, ModuleWriterEvent.BeginCalculateRvasAndFileOffsets);
			CalculateRvasAndFileOffsets(chunks);
			listener.OnWriterEvent(this, ModuleWriterEvent.EndCalculateRvasAndFileOffsets);

			InitializeChunkProperties();

			listener.OnWriterEvent(this, ModuleWriterEvent.BeginWriteChunks);
			var writer = new BinaryWriter(destStream);
			WriteChunks(writer, chunks);
			listener.OnWriterEvent(this, ModuleWriterEvent.EndWriteChunks);

			listener.OnWriterEvent(this, ModuleWriterEvent.BeginStrongNameSign);
			//TODO: Strong name sign the assembly
			listener.OnWriterEvent(this, ModuleWriterEvent.EndStrongNameSign);

			listener.OnWriterEvent(this, ModuleWriterEvent.BeginWritePEChecksum);
			if (Options.AddCheckSum)
				peHeaders.WriteCheckSum(writer, writer.BaseStream.Length);
			listener.OnWriterEvent(this, ModuleWriterEvent.EndWritePEChecksum);
		}

		void CalculateRvasAndFileOffsets(List<IChunk> chunks) {
			peHeaders.PESections = sections;
			FileOffset offset = 0;
			RVA rva = 0;
			foreach (var chunk in chunks) {
				chunk.SetOffset(offset, rva);
				uint len = chunk.GetLength();
				offset += len;
				rva += len;
				offset = offset.AlignUp(peHeaders.FileAlignment);
				rva = rva.AlignUp(peHeaders.SectionAlignment);
			}
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
			imageCor20Header.MetaData = metaData;
			imageCor20Header.NetResources = netResources;
			imageCor20Header.StrongNameSignature = strongNameSignature;
		}

		void WriteChunks(BinaryWriter writer, List<IChunk> chunks) {
			FileOffset offset = 0;
			foreach (var chunk in chunks) {
				chunk.VerifyWriteTo(writer);
				offset += chunk.GetLength();
				var newOffset = offset.AlignUp(peHeaders.FileAlignment);
				writer.WriteZeros((int)(newOffset - offset));
				offset = newOffset;
			}
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

		/// <inheritdoc/>
		void IMetaDataListener.OnMetaDataEvent(MetaData metaData, MetaDataEvent evt) {
			switch (evt) {
			case MetaDataEvent.BeginCreateTables:
				listener.OnWriterEvent(this, ModuleWriterEvent.MDBeginCreateTables);
				break;

			case MetaDataEvent.MemberDefRidsAllocated:
				listener.OnWriterEvent(this, ModuleWriterEvent.MDMemberDefRidsAllocated);
				break;

			case MetaDataEvent.MemberDefsInitialized:
				listener.OnWriterEvent(this, ModuleWriterEvent.MDMemberDefsInitialized);
				break;

			case MetaDataEvent.MostTablesSorted:
				listener.OnWriterEvent(this, ModuleWriterEvent.MDMostTablesSorted);
				break;

			case MetaDataEvent.MemberDefCustomAttributesWritten:
				listener.OnWriterEvent(this, ModuleWriterEvent.MDMemberDefCustomAttributesWritten);
				break;

			case MetaDataEvent.BeginWriteMethodBodies:
				listener.OnWriterEvent(this, ModuleWriterEvent.MDBeginWriteMethodBodies);
				break;

			case MetaDataEvent.EndWriteMethodBodies:
				listener.OnWriterEvent(this, ModuleWriterEvent.MDEndWriteMethodBodies);
				break;

			case MetaDataEvent.BeginAddResources:
				listener.OnWriterEvent(this, ModuleWriterEvent.MDBeginAddResources);
				break;

			case MetaDataEvent.EndAddResources:
				listener.OnWriterEvent(this, ModuleWriterEvent.MDEndAddResources);
				break;

			case MetaDataEvent.OnAllTablesSorted:
				listener.OnWriterEvent(this, ModuleWriterEvent.MDOnAllTablesSorted);
				break;

			case MetaDataEvent.EndCreateTables:
				listener.OnWriterEvent(this, ModuleWriterEvent.MDEndCreateTables);
				break;

			default:
				break;
			}
		}
	}
}
