using System.Collections.Generic;
using System.IO;
using dot10.DotNet.MD;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// Writes a .NET PE file
	/// </summary>
	public class ModuleWriter {
		//TODO: These should not be constants
		const uint DEFAULT_FILE_ALIGNMENT = 0x200;
		const uint DEFAULT_SECTION_ALIGNMENT = 0x2000;
		const uint DEFAULT_IAT_ALIGNMENT = 4;
		const uint DEFAULT_COR20HEADER_ALIGNMENT = 4;
		const uint DEFAULT_STRONGNAMESIG_ALIGNMENT = 16;
		const uint DEFAULT_CONSTANTS_ALIGNMENT = 8;
		const uint DEFAULT_METHODBODIES_ALIGNMENT = 4;
		const uint DEFAULT_NETRESOURCES_ALIGNMENT = 8;
		const uint DEFAULT_METADATA_ALIGNMENT = 4;
		const uint DEFAULT_DEBUGDIRECTORY_ALIGNMENT = 4;
		const uint DEFAULT_IMPORTDIRECTORY_ALIGNMENT = 4;
		const uint DEFAULT_NATIVEEP_ALIGNMENT = 1;

		readonly ModuleDef module;

		List<PESection> sections;
		PESection textSection;
		PESection rsrcSection;
		PESection relocSection;

		ImportAddressTable importAddressTable;
		ImageCor20Header imageCor20Header;
		StrongNameSignature strongNameSignature;
		UniqueChunkList<IChunk> constants;
		MethodBodyChunks methodBodies;
		ChunkList<IChunk> netResources;
		MetaData metaData;
		DebugDirectory debugDirectory;
		ImportDirectory importDirectory;
		NativeEntryPoint nativeEntryPoint;

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
			using (var dest = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite)) {
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
			//TODO:
		}

		void Initialize() {
			sections = new List<PESection>();
			sections.Add(textSection = new PESection(".text", 0x60000020));
			sections.Add(rsrcSection = new PESection(".rsrc", 0x40000040));
			sections.Add(relocSection = new PESection(".reloc", 0x42000040));	//TODO: Only add if 32-bit
			CreateChunks();
			AddChunksToSections();
		}

		void CreateChunks() {
			bool isSn = false;	//TODO:
			bool is64bit = false;	//TODO:
			bool shareBodies = true;	//TODO:

			if (!is64bit) {
				importAddressTable = new ImportAddressTable();
				importDirectory = new ImportDirectory();
				nativeEntryPoint = new NativeEntryPoint();
			}
			if (isSn)
				strongNameSignature = new StrongNameSignature(0x80);	//TODO: Fix size

			imageCor20Header = new ImageCor20Header(new Cor20HeaderOptions(ComImageFlags.ILOnly));
			constants = new UniqueChunkList<IChunk>();
			methodBodies = new MethodBodyChunks(shareBodies);
			netResources = new ChunkList<IChunk>();
			metaData = new MetaData(module, constants, methodBodies, netResources);
			debugDirectory = new DebugDirectory();
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
			textSection.Add(nativeEntryPoint, DEFAULT_NATIVEEP_ALIGNMENT);
		}
	}
}
