using System.Collections.Generic;
using System.IO;
using dot10.DotNet.MD;
using dot10.PE;
using dot10.IO;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// <see cref="ModuleWriter"/> options
	/// </summary>
	public sealed class ModuleWriterOptions {
		MetaDataOptions metaDataOptions;
		Cor20HeaderOptions cor20HeaderOptions;
		PEHeadersOptions peHeadersOptions;

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
					ModuleKind != ModuleKind.Netmodule;
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
		/// Initialize some fields from a module
		/// </summary>
		/// <param name="module">The module</param>
		public ModuleWriterOptions(ModuleDef module) {
			ShareMethodBodies = true;
			ModuleKind = module.Kind;
			PEHeadersOptions.Machine = module.Machine;
			if (module.Kind == ModuleKind.Windows)
				PEHeadersOptions.Subsystem = Subsystem.WindowsGui;
			else
				PEHeadersOptions.Subsystem = Subsystem.WindowsCui;
			Cor20HeaderOptions.Flags = module.Cor20HeaderFlags;
			MetaDataOptions.MetaDataHeaderOptions.VersionString = module.RuntimeVersion;

			var modDefMD = module as ModuleDefMD;
			if (modDefMD != null)
				AddCheckSum = modDefMD.MetaData.PEImage.ImageNTHeaders.OptionalHeader.CheckSum != 0;
		}
	}

	/// <summary>
	/// Writes a .NET PE file
	/// </summary>
	public sealed class ModuleWriter {
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
		const uint DEFAULT_RESOURCE_ALIGNMENT = 4;
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
		StrongNameSignature strongNameSignature;
		UniqueChunkList<ByteArrayChunk> constants;
		MethodBodyChunks methodBodies;
		NetResources netResources;
		MetaData metaData;
		DebugDirectory debugDirectory;
		ImportDirectory importDirectory;
		StartupStub startupStub;
		Win32Resources win32Resources;
		RelocDirectory relocDirectory;

		/// <summary>
		/// Gets/sets the writer options
		/// </summary>
		public ModuleWriterOptions Options {
			get { return options ?? (options = new ModuleWriterOptions(module)); }
			set { options = value; }
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
			Initialize();

			metaData.CreateTables();
			WriteFile(dest);
		}

		void Initialize() {
			bool hasWin32Resources = false;	//TODO:

			sections = new List<PESection>();
			sections.Add(textSection = new PESection(".text", 0x60000020));
			if (hasWin32Resources)
				sections.Add(rsrcSection = new PESection(".rsrc", 0x40000040));
			if (!Options.Is64Bit)
				sections.Add(relocSection = new PESection(".reloc", 0x42000040));
			CreateChunks();
			AddChunksToSections();
		}

		void CreateChunks() {
			bool isSn = false;	//TODO:
			bool hasWin32Resources = false;	//TODO:

			peHeaders = new PEHeaders(Options.PEHeadersOptions);

			if (!Options.Is64Bit) {
				importAddressTable = new ImportAddressTable();
				importDirectory = new ImportDirectory();
				startupStub = new StartupStub();
				relocDirectory = new RelocDirectory();
			}
			if (isSn)
				strongNameSignature = new StrongNameSignature(0x80);	//TODO: Fix size

			imageCor20Header = new ImageCor20Header(Options.Cor20HeaderOptions);
			constants = new UniqueChunkList<ByteArrayChunk>();
			methodBodies = new MethodBodyChunks(Options.ShareMethodBodies);
			netResources = new NetResources(DEFAULT_NETRESOURCES_ALIGNMENT);
			metaData = MetaData.Create(module, constants, methodBodies, netResources, Options.MetaDataOptions);
			debugDirectory = new DebugDirectory();
			if (hasWin32Resources)
				win32Resources = new Win32Resources();

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
			if (win32Resources != null)
				rsrcSection.Add(win32Resources, DEFAULT_RESOURCE_ALIGNMENT);
			if (relocSection != null)
				relocSection.Add(relocDirectory, DEFAULT_RELOC_ALIGNMENT);
		}

		void WriteFile(Stream dest) {
			var chunks = new List<IChunk>();
			chunks.Add(peHeaders);
			foreach (var section in sections)
				chunks.Add(section);

			var writer = new BinaryWriter(dest);
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

			offset = 0;
			foreach (var chunk in chunks) {
				chunk.VerifyWriteTo(writer);
				offset += chunk.GetLength();
				var newOffset = offset.AlignUp(peHeaders.FileAlignment);
				writer.WriteZeros((int)(newOffset - offset));
				offset = newOffset;
			}

			//TODO: Strong name sign the assembly
			if (Options.AddCheckSum)
				peHeaders.WriteCheckSum(writer, writer.BaseStream.Length);
		}
	}
}
