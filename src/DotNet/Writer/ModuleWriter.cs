using System.Collections.Generic;
using System.IO;
using dot10.DotNet.MD;
using dot10.PE;
using dot10.IO;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// Writes a .NET PE file
	/// </summary>
	public sealed class ModuleWriter {
		//TODO: These should not be constants
		const uint DEFAULT_FILE_ALIGNMENT = 0x200;
		const uint DEFAULT_SECTION_ALIGNMENT = 0x2000;
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
		/// Constructor
		/// </summary>
		/// <param name="module">The module</param>
		public ModuleWriter(ModuleDef module) {
			this.module = module;
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
			sections.Add(relocSection = new PESection(".reloc", 0x42000040));	//TODO: Only add if 32-bit
			CreateChunks();
			AddChunksToSections();
		}

		void CreateChunks() {
			bool isSn = false;	//TODO:
			bool is64bit = false;	//TODO:
			bool shareBodies = true;	//TODO:
			bool isExe = false;	//TODO:
			bool hasWin32Resources = false;	//TODO:

			peHeaders = new PEHeaders();

			if (!is64bit) {
				importAddressTable = new ImportAddressTable();
				importDirectory = new ImportDirectory();
				startupStub = new StartupStub();
				relocDirectory = new RelocDirectory();
			}
			if (isSn)
				strongNameSignature = new StrongNameSignature(0x80);	//TODO: Fix size

			imageCor20Header = new ImageCor20Header(new Cor20HeaderOptions(ComImageFlags.ILOnly));
			constants = new UniqueChunkList<ByteArrayChunk>();
			methodBodies = new MethodBodyChunks(shareBodies);
			netResources = new NetResources(DEFAULT_NETRESOURCES_ALIGNMENT);
			var mdOptions = new MetaDataOptions();	//TODO: Use the options the user wants
			metaData = MetaData.Create(module, constants, methodBodies, netResources, mdOptions);
			debugDirectory = new DebugDirectory();
			if (hasWin32Resources)
				win32Resources = new Win32Resources();

			importDirectory.IsExeFile = isExe;
			peHeaders.IsExeFile = isExe;
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
			peHeaders.WriteCheckSum(writer, writer.BaseStream.Length);	//TODO: Option to disable this
		}
	}
}
