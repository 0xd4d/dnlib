using System;
using System.Collections.Generic;
using System.IO;
using dot10.IO;
using dot10.PE;
using dot10.W32Resources;
using dot10.DotNet.MD;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// <see cref="NativeModuleWriter"/> options
	/// </summary>
	public sealed class NativeModuleWriterOptions : ModuleWriterOptionsBase {
		/// <summary>
		/// If <c>true</c>, any extra data after the PE data in the original file is also saved
		/// at the end of the new file. Enable this option if some protector has written data to
		/// the end of the file and uses it at runtime.
		/// </summary>
		public bool KeepExtraPEData { get; set; }

		/// <summary>
		/// If <c>true</c>, keep the original Win32 resources
		/// </summary>
		public bool KeepWin32Resources { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		public NativeModuleWriterOptions(ModuleDefMD module)
			: base(module) {
			this.AddCheckSum = module.MetaData.PEImage.ImageNTHeaders.OptionalHeader.CheckSum != 0;
		}
	}

	/// <summary>
	/// A module writer that supports saving mixed-mode modules (modules with native code).
	/// The original image will be re-used. See also <see cref="ModuleWriter"/>
	/// </summary>
	public sealed class NativeModuleWriter : ModuleWriterBase {
		readonly ModuleDefMD module;
		NativeModuleWriterOptions options;
		BinaryReaderChunk extraData;
		BinaryReaderChunk headerSection;
		List<OrigSection> origSections;
		IPEImage peImage;
		List<PESection> sections;
		PESection textSection;
		PESection rsrcSection;
		long checkSumOffset;

		class OrigSection : IDisposable {
			public ImageSectionHeader peSection;
			public BinaryReaderChunk chunk;

			public OrigSection(ImageSectionHeader peSection) {
				this.peSection = peSection;
			}

			public void Dispose() {
				if (chunk != null)
					chunk.Data.Dispose();
				chunk = null;
				peSection = null;
			}

			public override string ToString() {
				uint offs = chunk.Data is IImageStream ? (uint)((IImageStream)chunk.Data).FileOffset : 0;
				return string.Format("{0} FO:{1:X8} L:{2:X8}", peSection.DisplayName, offs, (uint)chunk.Data.Length);
			}
		}

		/// <summary>
		/// Gets the module
		/// </summary>
		public ModuleDefMD Module {
			get { return module; }
		}

		/// <inheritdoc/>
		protected override ModuleWriterOptionsBase TheOptions {
			get { return Options; }
		}

		/// <summary>
		/// Gets/sets the writer options. This is never <c>null</c>
		/// </summary>
		public NativeModuleWriterOptions Options {
			get { return options ?? (options = new NativeModuleWriterOptions(module)); }
			set { options = value; }
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
		/// Constructor
		/// </summary>
		/// <param name="module">The module</param>
		/// <param name="options">Options or <c>null</c></param>
		public NativeModuleWriter(ModuleDefMD module, NativeModuleWriterOptions options) {
			this.module = module;
			this.options = options;
			this.peImage = module.MetaData.PEImage;
		}

		/// <inheritdoc/>
		protected override void WriteImpl() {
			try {
				Write();
			}
			finally {
				if (origSections != null) {
					foreach (var section in origSections)
						section.Dispose();
				}
				if (headerSection != null)
					headerSection.Data.Dispose();
				if (extraData != null)
					extraData.Data.Dispose();
			}
		}

		void Write() {
			Initialize();
			metaData.CreateTables();
			WriteFile();
		}

		void Initialize() {
			CreateSections();
			Listener.OnWriterEvent(this, ModuleWriterEvent.PESectionsCreated);

			CreateMetaDataChunks(module);
			Listener.OnWriterEvent(this, ModuleWriterEvent.ChunksCreated);

			AddChunksToSections();
			Listener.OnWriterEvent(this, ModuleWriterEvent.ChunksAddedToSections);
		}

		void CreateSections() {
			CreatePESections();
			CreateRawSections();
			CreateHeaderSection();
			CreateExtraData();
		}

		void AddChunksToSections() {
			textSection.Add(constants, DEFAULT_CONSTANTS_ALIGNMENT);
			textSection.Add(methodBodies, DEFAULT_METHODBODIES_ALIGNMENT);
			textSection.Add(netResources, DEFAULT_NETRESOURCES_ALIGNMENT);
			textSection.Add(metaData, DEFAULT_METADATA_ALIGNMENT);
			if (rsrcSection != null)
				rsrcSection.Add(win32Resources, DEFAULT_WIN32_RESOURCES_ALIGNMENT);
		}

		/// <inheritdoc/>
		protected override Win32Resources GetWin32Resources() {
			if (Options.KeepWin32Resources)
				return null;
			return module.Win32Resources ?? Options.Win32Resources;
		}

		void CreatePESections() {
			sections = new List<PESection>();
			sections.Add(textSection = new PESection(".text", 0x60000020));
			if (GetWin32Resources() != null)
				sections.Add(rsrcSection = new PESection(".rsrc", 0x40000040));
		}

		/// <summary>
		/// Gets the raw section data of the image. The sections are saved in
		/// <see cref="origSections"/>.
		/// </summary>
		void CreateRawSections() {
			var fileAlignment = peImage.ImageNTHeaders.OptionalHeader.FileAlignment;
			origSections = new List<OrigSection>(peImage.ImageSectionHeaders.Count);

			foreach (var peSection in peImage.ImageSectionHeaders) {
				var newSection = new OrigSection(peSection);
				origSections.Add(newSection);
				uint sectionSize = Utils.AlignUp(peSection.SizeOfRawData, fileAlignment);
				newSection.chunk = new BinaryReaderChunk(peImage.CreateStream(peSection.VirtualAddress, sectionSize), peSection.VirtualSize);
			}
		}

		/// <summary>
		/// Creates the PE header "section"
		/// </summary>
		void CreateHeaderSection() {
			uint minHeaderLen = GetOffsetAfterLastSection() + (uint)sections.Count * 0x28;
			uint maxHeaderLen = GetFirstRawDataFileOffset();
			uint headerLen = minHeaderLen;
			if (maxHeaderLen > headerLen)
				headerLen = maxHeaderLen;
			headerLen = Utils.AlignUp(headerLen, peImage.ImageNTHeaders.OptionalHeader.FileAlignment);
			if (headerLen < peImage.ImageNTHeaders.OptionalHeader.SectionAlignment) {
				headerSection = new BinaryReaderChunk(peImage.CreateStream(0, headerLen));
				return;
			}

			//TODO: Support this too
			throw new ModuleWriterException("Could not create header");
		}

		uint GetOffsetAfterLastSection() {
			var lastSect = peImage.ImageSectionHeaders[peImage.ImageSectionHeaders.Count - 1];
			return (uint)lastSect.EndOffset;
		}

		uint GetFirstRawDataFileOffset() {
			uint len = uint.MaxValue;
			foreach (var section in peImage.ImageSectionHeaders)
				len = Math.Min(len, section.PointerToRawData);
			return len;
		}

		/// <summary>
		/// Saves any data that is appended to the original PE file
		/// </summary>
		void CreateExtraData() {
			if (!Options.KeepExtraPEData)
				return;
			var lastOffs = GetLastFileSectionOffset();
			extraData = new BinaryReaderChunk(peImage.CreateStream((FileOffset)lastOffs));
			if (extraData.Data.Length == 0) {
				extraData.Data.Dispose();
				extraData = null;
			}
		}

		uint GetLastFileSectionOffset() {
			uint rva = 0;
			foreach (var sect in origSections)
				rva = Math.Max(rva, (uint)sect.peSection.VirtualAddress + sect.peSection.SizeOfRawData);
			return (uint)peImage.ToFileOffset((RVA)(rva - 1)) + 1;
		}

		void WriteFile() {
			var chunks = new List<IChunk>();
			chunks.Add(headerSection);
			foreach (var origSection in origSections)
				chunks.Add(origSection.chunk);
			foreach (var section in sections)
				chunks.Add(section);
			if (extraData != null)
				chunks.Add(extraData);

			Listener.OnWriterEvent(this, ModuleWriterEvent.BeginCalculateRvasAndFileOffsets);
			CalculateRvasAndFileOffsets(chunks, 0, 0, peImage.ImageNTHeaders.OptionalHeader.FileAlignment, peImage.ImageNTHeaders.OptionalHeader.SectionAlignment);
			foreach (var section in origSections) {
				if (section.chunk.RVA != section.peSection.VirtualAddress)
					throw new ModuleWriterException("Invalid section RVA");
			}
			Listener.OnWriterEvent(this, ModuleWriterEvent.EndCalculateRvasAndFileOffsets);

			Listener.OnWriterEvent(this, ModuleWriterEvent.BeginWriteChunks);
			var writer = new BinaryWriter(destStream);
			WriteChunks(writer, chunks, 0, peImage.ImageNTHeaders.OptionalHeader.FileAlignment);
			long imageLength = writer.BaseStream.Position - destStreamBaseOffset;
			UpdateHeaderFields(writer);
			Listener.OnWriterEvent(this, ModuleWriterEvent.EndWriteChunks);

			Listener.OnWriterEvent(this, ModuleWriterEvent.BeginStrongNameSign);
			//TODO: Strong name sign the assembly
			Listener.OnWriterEvent(this, ModuleWriterEvent.EndStrongNameSign);

			Listener.OnWriterEvent(this, ModuleWriterEvent.BeginWritePEChecksum);
			if (Options.AddCheckSum) {
				destStream.Position = destStreamBaseOffset;
				var newCheckSum = new BinaryReader(destStream).CalculateCheckSum(imageLength, checkSumOffset);
				writer.BaseStream.Position = checkSumOffset;
				writer.Write(newCheckSum);
			}
			Listener.OnWriterEvent(this, ModuleWriterEvent.EndWritePEChecksum);
		}

		/// <summary>
		/// <c>true</c> if image is 64-bit
		/// </summary>
		bool Is64Bit() {
			return peImage.ImageNTHeaders.OptionalHeader is ImageOptionalHeader64;
		}

		Characteristics GetCharacteristics() {
			var ch = module.Characteristics;
			if (Is64Bit())
				ch &= ~Characteristics._32BitMachine;
			else
				ch |= Characteristics._32BitMachine;
			return ch;
		}

		/// <summary>
		/// Updates the PE header and COR20 header fields that need updating. All sections are
		/// also updated, and the new ones are added.
		/// </summary>
		void UpdateHeaderFields(BinaryWriter writer) {
			long fileHeaderOffset = destStreamBaseOffset + (long)peImage.ImageNTHeaders.FileHeader.StartOffset;
			long optionalHeaderOffset = destStreamBaseOffset + (long)peImage.ImageNTHeaders.OptionalHeader.StartOffset;
			long sectionsOffset = destStreamBaseOffset + (long)peImage.ImageSectionHeaders[0].StartOffset;
			long dataDirOffset = destStreamBaseOffset + (long)peImage.ImageNTHeaders.OptionalHeader.EndOffset - 16 * 8;
			long cor20Offset = destStreamBaseOffset + (long)module.MetaData.ImageCor20Header.StartOffset;

			uint fileAlignment = peImage.ImageNTHeaders.OptionalHeader.FileAlignment;
			uint sectionAlignment = peImage.ImageNTHeaders.OptionalHeader.SectionAlignment;

			// Update PE file header
			writer.BaseStream.Position = fileHeaderOffset;
			writer.Write((ushort)module.Machine);
			writer.Write((ushort)(origSections.Count + sections.Count));
			writer.BaseStream.Position += 14;
			writer.Write((ushort)GetCharacteristics());

			// Update optional header
			var sectionSizes = new SectionSizes(fileAlignment, sectionAlignment, headerSection.GetVirtualSize(), () => GetSectionSizeInfos());
			writer.BaseStream.Position = optionalHeaderOffset;
			bool is32BitOptionalHeader = peImage.ImageNTHeaders.OptionalHeader is ImageOptionalHeader32;
			if (is32BitOptionalHeader) {
				writer.BaseStream.Position += 4;
				writer.Write(sectionSizes.sizeOfCode);
				writer.Write(sectionSizes.sizeOfInitdData);
				writer.Write(sectionSizes.sizeOfUninitdData);
				writer.BaseStream.Position += 4;	// EntryPoint
				writer.Write(sectionSizes.baseOfCode);
				writer.Write(sectionSizes.baseOfData);
				writer.BaseStream.Position += 0x1C;
				writer.Write(sectionSizes.sizeOfImage);
				writer.Write(sectionSizes.sizeOfHeaders);
			}
			else {
				writer.BaseStream.Position += 4;
				writer.Write(sectionSizes.sizeOfCode);
				writer.Write(sectionSizes.sizeOfInitdData);
				writer.Write(sectionSizes.sizeOfUninitdData);
				writer.BaseStream.Position += 4;	// EntryPoint
				writer.Write(sectionSizes.baseOfCode);
				writer.BaseStream.Position += 0x20;
				writer.Write(sectionSizes.sizeOfImage);
				writer.Write(sectionSizes.sizeOfHeaders);
			}
			checkSumOffset = writer.BaseStream.Position;
			writer.BaseStream.Position += 4;	// CheckSum
			writer.Write((ushort)GetSubsystem());
			writer.Write((ushort)module.DllCharacteristics);
			if (is32BitOptionalHeader)
				writer.BaseStream.Position += 0x14;
			else
				writer.BaseStream.Position += 0x24;
			writer.Write(0x10);		// NumberOfRvaAndSizes

			// Update Win32 resources data directory, if we wrote a new one
			if (win32Resources != null) {
				writer.BaseStream.Position = dataDirOffset + 2 * 8;
				writer.WriteDataDirectory(win32Resources);
			}

			// Update old sections, and add new sections
			writer.BaseStream.Position = sectionsOffset;
			foreach (var section in origSections) {
				writer.BaseStream.Position += 0x14;
				writer.Write((uint)section.chunk.FileOffset);	// PointerToRawData
				writer.BaseStream.Position += 0x10;
			}
			foreach (var section in sections)
				section.WriteHeaderTo(writer, fileAlignment, sectionAlignment, (uint)section.RVA);

			// Update .NET header
			writer.BaseStream.Position = cor20Offset + 8;
			writer.WriteDataDirectory(metaData);
			writer.Write((uint)module.Cor20HeaderFlags);
			writer.Write(GetEntryPoint());
			writer.WriteDataDirectory(netResources);
			//TODO: Write new strong name signature if we resigned it

			UpdateVTableFixups(writer);
		}

		Subsystem GetSubsystem() {
			if (module.Kind == ModuleKind.Windows)
				return Subsystem.WindowsGui;
			return Subsystem.WindowsCui;
		}

		/// <summary>
		/// Converts <paramref name="rva"/> to a file offset in the destination stream
		/// </summary>
		/// <param name="rva">RVA</param>
		long ToWriterOffset(RVA rva) {
			if (rva == 0)
				return 0;
			foreach (var sect in origSections) {
				var section = sect.peSection;
				if (section.VirtualAddress <= rva && rva < section.VirtualAddress + Math.Max(section.VirtualSize, section.SizeOfRawData))
					return destStreamBaseOffset + (long)sect.chunk.FileOffset + (rva - section.VirtualAddress);
			}
			return 0;
		}

		IEnumerable<SectionSizeInfo> GetSectionSizeInfos() {
			foreach (var section in origSections)
				yield return new SectionSizeInfo(section.chunk.GetVirtualSize(), section.peSection.Characteristics);
			foreach (var section in sections)
				yield return new SectionSizeInfo(section.GetVirtualSize(), section.Characteristics);
		}

		void UpdateVTableFixups(BinaryWriter writer) {
			var vtableFixups = module.VTableFixups;
			if (vtableFixups == null || vtableFixups.VTables.Count == 0)
				return;

			writer.BaseStream.Position = ToWriterOffset(vtableFixups.RVA);
			if (writer.BaseStream.Position == 0) {
				Error("Could not convert RVA to file offset");
				return;
			}
			foreach (var vtable in vtableFixups) {
				if (vtable.Methods.Count > ushort.MaxValue)
					throw new ModuleWriterException("Too many methods in vtable");
				writer.Write((uint)vtable.RVA);
				writer.Write((ushort)vtable.Methods.Count);
				writer.Write((ushort)vtable.Flags);

				long pos = writer.BaseStream.Position;
				writer.BaseStream.Position = ToWriterOffset(vtable.RVA);
				if (writer.BaseStream.Position == 0)
					Error("Could not convert RVA to file offset");
				else {
					foreach (var method in vtable.Methods) {
						writer.Write(GetMethodToken(method));
						if (vtable.Is64Bit)
							writer.Write(0);
					}
				}
				writer.BaseStream.Position = pos;
			}
		}

		uint GetMethodToken(IMethod method) {
			var md = method as MethodDef;
			if (md != null)
				return new MDToken(Table.Method, metaData.GetRid(md)).Raw;

			var mr = method as MemberRef;
			if (mr != null)
				return new MDToken(Table.MemberRef, metaData.GetRid(mr)).Raw;

			var ms = method as MethodSpec;
			if (ms != null)
				return new MDToken(Table.MethodSpec, metaData.GetRid(ms)).Raw;

			if (method == null)
				Error("VTable method is null");
			else
				Error("Invalid VTable method type: {0}", method.GetType());
			return 0;
		}

		uint GetEntryPoint() {
			var ep = module.ManagedEntryPoint as MethodDef;
			if (ep != null)
				return new MDToken(Table.Method, metaData.GetRid(ep)).Raw;
			var file = module.ManagedEntryPoint as FileDef;
			if (file != null)
				return new MDToken(Table.File, metaData.GetRid(file)).Raw;
			return (uint)module.NativeEntryPoint;
		}
	}
}
