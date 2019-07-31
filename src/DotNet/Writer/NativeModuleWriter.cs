// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using dnlib.IO;
using dnlib.PE;
using dnlib.W32Resources;
using dnlib.DotNet.MD;
using System.Diagnostics;

namespace dnlib.DotNet.Writer {
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

		internal bool OptimizeImageSize { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="optimizeImageSize">true to optimize the image size so it's as small as possible.
		/// Since the file can contain native methods and other native data, we re-use the
		/// original file when writing the new file. If <paramref name="optimizeImageSize"/> is true,
		/// we'll try to re-use the old method body locations in the original file and
		/// also try to fit the new metadata in the old metadata location.</param>
		public NativeModuleWriterOptions(ModuleDefMD module, bool optimizeImageSize) : base(module) {
			// C++ .NET mixed mode assemblies sometimes/often call Module.ResolveMethod(),
			// so method metadata tokens must be preserved.
			MetadataOptions.Flags |= MetadataFlags.PreserveAllMethodRids;

			if (optimizeImageSize) {
				OptimizeImageSize = true;

				// Prevent the #Blob heap from getting bigger. Encoded TypeDefOrRef tokens are stored there (in
				// typesigs and callingconvsigs) so we must preserve TypeDefOrRef tokens (or the #Blob heap could
				// grow in size and new MD won't fit in old location)
				MetadataOptions.Flags |= MetadataFlags.PreserveTypeRefRids | MetadataFlags.PreserveTypeDefRids | MetadataFlags.PreserveTypeSpecRids;
			}
		}
	}

	/// <summary>
	/// A module writer that supports saving mixed-mode modules (modules with native code).
	/// The original image will be re-used. See also <see cref="ModuleWriter"/>
	/// </summary>
	public sealed class NativeModuleWriter : ModuleWriterBase {
		/// <summary>The original .NET module</summary>
		readonly ModuleDefMD module;

		/// <summary>All options</summary>
		NativeModuleWriterOptions options;

		/// <summary>
		/// Any extra data found at the end of the original file. This is <c>null</c> if there's
		/// no extra data or if <see cref="NativeModuleWriterOptions.KeepExtraPEData"/> is
		/// <c>false</c>.
		/// </summary>
		DataReaderChunk extraData;

		/// <summary>The original PE sections and their data</summary>
		List<OrigSection> origSections;

		readonly struct ReusedChunkInfo {
			public IReuseChunk Chunk { get; }
			public RVA RVA { get; }
			public ReusedChunkInfo(IReuseChunk chunk, RVA rva) {
				Chunk = chunk;
				RVA = rva;
			}
		}

		List<ReusedChunkInfo> reusedChunks;

		/// <summary>Original PE image</summary>
		readonly IPEImage peImage;

		/// <summary>New sections we've added and their data</summary>
		List<PESection> sections;

		/// <summary>New .text section where we put some stuff, eg. .NET metadata</summary>
		PESection textSection;

		/// <summary>The new COR20 header</summary>
		ByteArrayChunk imageCor20Header;

		/// <summary>
		/// New .rsrc section where we put the new Win32 resources. This is <c>null</c> if there
		/// are no Win32 resources or if <see cref="NativeModuleWriterOptions.KeepWin32Resources"/>
		/// is <c>true</c>
		/// </summary>
		PESection rsrcSection;

		/// <summary>
		/// Offset in <see cref="ModuleWriterBase.destStream"/> of the PE checksum field.
		/// </summary>
		long checkSumOffset;

		/// <summary>
		/// Original PE section
		/// </summary>
		public sealed class OrigSection : IDisposable {
			/// <summary>PE section</summary>
			public ImageSectionHeader PESection;
			/// <summary>PE section data</summary>
			public DataReaderChunk Chunk;

			/// <summary>
			/// Constructor
			/// </summary>
			/// <param name="peSection">PE section</param>
			public OrigSection(ImageSectionHeader peSection) =>
				PESection = peSection;

			/// <inheritdoc/>
			public void Dispose() {
				Chunk = null;
				PESection = null;
			}

			/// <inheritdoc/>
			public override string ToString() {
				uint offs = Chunk.CreateReader().StartOffset;
				return $"{PESection.DisplayName} FO:{offs:X8} L:{Chunk.CreateReader().Length:X8}";
			}
		}

		/// <summary>
		/// Gets the module
		/// </summary>
		public ModuleDefMD ModuleDefMD => module;

		/// <inheritdoc/>
		public override ModuleDef Module => module;

		/// <inheritdoc/>
		public override ModuleWriterOptionsBase TheOptions => Options;

		/// <summary>
		/// Gets/sets the writer options. This is never <c>null</c>
		/// </summary>
		public NativeModuleWriterOptions Options {
			get => options ?? (options = new NativeModuleWriterOptions(module, optimizeImageSize: true));
			set => options = value;
		}

		/// <summary>
		/// Gets all <see cref="PESection"/>s
		/// </summary>
		public override List<PESection> Sections => sections;

		/// <summary>
		/// Gets the original PE sections and their data
		/// </summary>
		public List<OrigSection> OrigSections => origSections;

		/// <summary>
		/// Gets the <c>.text</c> section
		/// </summary>
		public override PESection TextSection => textSection;

		/// <summary>
		/// Gets the <c>.rsrc</c> section or <c>null</c> if there's none
		/// </summary>
		public override PESection RsrcSection => rsrcSection;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The module</param>
		/// <param name="options">Options or <c>null</c></param>
		public NativeModuleWriter(ModuleDefMD module, NativeModuleWriterOptions options) {
			this.module = module;
			this.options = options;
			peImage = module.Metadata.PEImage;
			reusedChunks = new List<ReusedChunkInfo>();
		}

		/// <inheritdoc/>
		protected override long WriteImpl() {
			try {
				return Write();
			}
			finally {
				if (!(origSections is null)) {
					foreach (var section in origSections)
						section.Dispose();
				}
			}
		}

		long Write() {
			Initialize();

			// It's not safe to create new Field RVAs so re-use them all. The user can override
			// this by setting field.RVA = 0 when creating a new field.InitialValue.
			metadata.KeepFieldRVA = true;

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

		void CreateSections() {
			CreatePESections();
			CreateRawSections();
			CreateExtraData();
		}

		void CreateChunks() {
			CreateMetadataChunks(module);
			methodBodies.CanReuseOldBodyLocation = Options.OptimizeImageSize;

			CreateDebugDirectory();

			imageCor20Header = new ByteArrayChunk(new byte[0x48]);
			CreateStrongNameSignature();
		}

		void AddChunksToSections() {
			textSection.Add(imageCor20Header, DEFAULT_COR20HEADER_ALIGNMENT);
			textSection.Add(strongNameSignature, DEFAULT_STRONGNAMESIG_ALIGNMENT);
			textSection.Add(constants, DEFAULT_CONSTANTS_ALIGNMENT);
			textSection.Add(methodBodies, DEFAULT_METHODBODIES_ALIGNMENT);
			textSection.Add(netResources, DEFAULT_NETRESOURCES_ALIGNMENT);
			textSection.Add(metadata, DEFAULT_METADATA_ALIGNMENT);
			textSection.Add(debugDirectory, DebugDirectory.DEFAULT_DEBUGDIRECTORY_ALIGNMENT);
			if (!(rsrcSection is null))
				rsrcSection.Add(win32Resources, DEFAULT_WIN32_RESOURCES_ALIGNMENT);
		}

		/// <inheritdoc/>
		protected override Win32Resources GetWin32Resources() {
			if (Options.KeepWin32Resources)
				return null;
			if (Options.NoWin32Resources)
				return null;
			return Options.Win32Resources ?? module.Win32Resources;
		}

		void CreatePESections() {
			sections = new List<PESection>();
			sections.Add(textSection = new PESection(".text", 0x60000020));
			if (!(GetWin32Resources() is null))
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
				newSection.Chunk = new DataReaderChunk(peImage.CreateReader(peSection.VirtualAddress, sectionSize), peSection.VirtualSize);
			}
		}

		/// <summary>
		/// Creates the PE header "section"
		/// </summary>
		DataReaderChunk CreateHeaderSection(out IChunk extraHeaderData) {
			uint afterLastSectHeader = GetOffsetAfterLastSectionHeader() + (uint)sections.Count * 0x28;
			uint firstRawOffset = Math.Min(GetFirstRawDataFileOffset(), peImage.ImageNTHeaders.OptionalHeader.SectionAlignment);
			uint headerLen = afterLastSectHeader;
			if (firstRawOffset > headerLen)
				headerLen = firstRawOffset;
			headerLen = Utils.AlignUp(headerLen, peImage.ImageNTHeaders.OptionalHeader.FileAlignment);
			if (headerLen <= peImage.ImageNTHeaders.OptionalHeader.SectionAlignment) {
				uint origSizeOfHeaders = peImage.ImageNTHeaders.OptionalHeader.SizeOfHeaders;
				uint extraLen;
				if (headerLen <= origSizeOfHeaders)
					extraLen = 0;
				else {
					extraLen = headerLen - origSizeOfHeaders;
					headerLen = origSizeOfHeaders;
				}
				if (extraLen > 0)
					extraHeaderData = new ByteArrayChunk(new byte[(int)extraLen]);
				else
					extraHeaderData = null;
				return new DataReaderChunk(peImage.CreateReader((FileOffset)0, headerLen));
			}

			//TODO: Support this too
			throw new ModuleWriterException("Could not create header");
		}

		uint GetOffsetAfterLastSectionHeader() {
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
			extraData = new DataReaderChunk(peImage.CreateReader((FileOffset)lastOffs));
			if (extraData.CreateReader().Length == 0)
				extraData = null;
		}

		uint GetLastFileSectionOffset() {
			uint rva = 0;
			foreach (var sect in origSections)
				rva = Math.Max(rva, (uint)sect.PESection.VirtualAddress + sect.PESection.SizeOfRawData);
			return (uint)peImage.ToFileOffset((RVA)(rva - 1)) + 1;
		}

		void ReuseIfPossible(PESection section, IReuseChunk chunk, RVA origRva, uint origSize, uint requiredAlignment) {
			if (origRva == 0 || origSize == 0)
				return;
			if (chunk is null)
				return;
			if (!chunk.CanReuse(origRva, origSize))
				return;
			if (((uint)origRva & (requiredAlignment - 1)) != 0)
				return;

			if (section.Remove(chunk) is null)
				throw new InvalidOperationException();
			reusedChunks.Add(new ReusedChunkInfo(chunk, origRva));
		}

		long WriteFile() {
			bool entryPointIsManagedOrNoEntryPoint = GetEntryPoint(out uint entryPointToken);

			OnWriterEvent(ModuleWriterEvent.BeginWritePdb);
			WritePdbFile();
			OnWriterEvent(ModuleWriterEvent.EndWritePdb);

			metadata.OnBeforeSetOffset();
			OnWriterEvent(ModuleWriterEvent.BeginCalculateRvasAndFileOffsets);

			if (Options.OptimizeImageSize) {
				// Check if we can reuse the old MD location for the new MD.
				// If we can't reuse it, it could be due to several reasons:
				//	- TypeDefOrRef tokens weren't preserved resulting in a new #Blob heap that's bigger than the old #Blob heap
				//	- New MD was added or existing MD was modified (eg. types were renamed) by the user so it's
				//	  now bigger and doesn't fit in the old location
				//	- The original location wasn't aligned properly
				//	- The new MD is bigger because the other MD writer was slightly better at optimizing the MD.
				//	  This should be considered a bug.
				var mdDataDir = module.Metadata.ImageCor20Header.Metadata;
				metadata.SetOffset(peImage.ToFileOffset(mdDataDir.VirtualAddress), mdDataDir.VirtualAddress);
				ReuseIfPossible(textSection, metadata, mdDataDir.VirtualAddress, mdDataDir.Size, DEFAULT_METADATA_ALIGNMENT);

				var resourceDataDir = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[2];
				if (!(win32Resources is null) && resourceDataDir.VirtualAddress != 0 && resourceDataDir.Size != 0) {
					var win32ResourcesOffset = peImage.ToFileOffset(resourceDataDir.VirtualAddress);
					if (win32Resources.CheckValidOffset(win32ResourcesOffset)) {
						win32Resources.SetOffset(win32ResourcesOffset, resourceDataDir.VirtualAddress);
						ReuseIfPossible(rsrcSection, win32Resources, resourceDataDir.VirtualAddress, resourceDataDir.Size, DEFAULT_WIN32_RESOURCES_ALIGNMENT);
					}
				}

				ReuseIfPossible(textSection, imageCor20Header, module.Metadata.PEImage.ImageNTHeaders.OptionalHeader.DataDirectories[14].VirtualAddress, module.Metadata.PEImage.ImageNTHeaders.OptionalHeader.DataDirectories[14].Size, DEFAULT_COR20HEADER_ALIGNMENT);
				if ((module.Metadata.ImageCor20Header.Flags & ComImageFlags.StrongNameSigned) != 0)
					ReuseIfPossible(textSection, strongNameSignature, module.Metadata.ImageCor20Header.StrongNameSignature.VirtualAddress, module.Metadata.ImageCor20Header.StrongNameSignature.Size, DEFAULT_STRONGNAMESIG_ALIGNMENT);
				ReuseIfPossible(textSection, netResources, module.Metadata.ImageCor20Header.Resources.VirtualAddress, module.Metadata.ImageCor20Header.Resources.Size, DEFAULT_NETRESOURCES_ALIGNMENT);
				if (methodBodies.ReusedAllMethodBodyLocations)
					textSection.Remove(methodBodies);

				var debugDataDir = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[6];
				if (debugDataDir.VirtualAddress != 0 && debugDataDir.Size != 0 && TryGetRealDebugDirectorySize(peImage, out uint realDebugDirSize))
					ReuseIfPossible(textSection, debugDirectory, debugDataDir.VirtualAddress, realDebugDirSize, DebugDirectory.DEFAULT_DEBUGDIRECTORY_ALIGNMENT);
			}

			if (constants.IsEmpty)
				textSection.Remove(constants);
			if (netResources.IsEmpty)
				textSection.Remove(netResources);
			if (textSection.IsEmpty)
				sections.Remove(textSection);
			if (!(rsrcSection is null) && rsrcSection.IsEmpty) {
				sections.Remove(rsrcSection);
				rsrcSection = null;
			}

			var headerSection = CreateHeaderSection(out var extraHeaderData);
			var chunks = new List<IChunk>();
			uint headerLen;
			if (!(extraHeaderData is null)) {
				var list = new ChunkList<IChunk>();
				list.Add(headerSection, 1);
				list.Add(extraHeaderData, 1);
				chunks.Add(list);
				headerLen = headerSection.GetVirtualSize() + extraHeaderData.GetVirtualSize();
			}
			else {
				chunks.Add(headerSection);
				headerLen = headerSection.GetVirtualSize();
			}
			foreach (var origSection in origSections)
				chunks.Add(origSection.Chunk);
			foreach (var section in sections)
				chunks.Add(section);
			if (!(extraData is null))
				chunks.Add(extraData);

			if (reusedChunks.Count > 0 || methodBodies.HasReusedMethods) {
				uint newSizeOfHeaders = SectionSizes.GetSizeOfHeaders(peImage.ImageNTHeaders.OptionalHeader.FileAlignment, headerLen);
				uint oldSizeOfHeaders = peImage.ImageNTHeaders.OptionalHeader.SizeOfHeaders;
				if (newSizeOfHeaders < oldSizeOfHeaders)
					throw new InvalidOperationException();
				uint fileOffsetDelta = newSizeOfHeaders - oldSizeOfHeaders;
				methodBodies.InitializeReusedMethodBodies(peImage, fileOffsetDelta);
				foreach (var info in reusedChunks) {
					if (fileOffsetDelta == 0 && (info.Chunk == metadata || info.Chunk == win32Resources))
						continue;
					var offset = peImage.ToFileOffset(info.RVA) + fileOffsetDelta;
					info.Chunk.SetOffset(offset, info.RVA);
				}
				metadata.UpdateMethodAndFieldRvas();
			}
			CalculateRvasAndFileOffsets(chunks, 0, 0, peImage.ImageNTHeaders.OptionalHeader.FileAlignment, peImage.ImageNTHeaders.OptionalHeader.SectionAlignment);
			metadata.UpdateMethodAndFieldRvas();
			foreach (var section in origSections) {
				if (section.Chunk.RVA != section.PESection.VirtualAddress)
					throw new ModuleWriterException("Invalid section RVA");
			}
			OnWriterEvent(ModuleWriterEvent.EndCalculateRvasAndFileOffsets);

			OnWriterEvent(ModuleWriterEvent.BeginWriteChunks);
			var writer = new DataWriter(destStream);
			WriteChunks(writer, chunks, 0, peImage.ImageNTHeaders.OptionalHeader.FileAlignment);
			long imageLength = writer.Position - destStreamBaseOffset;
			if (reusedChunks.Count > 0 || methodBodies.HasReusedMethods) {
				var pos = writer.Position;
				foreach (var info in reusedChunks) {
					Debug.Assert(info.Chunk.RVA == info.RVA);
					if (info.Chunk.RVA != info.RVA)
						throw new InvalidOperationException();
					writer.Position = destStreamBaseOffset + (uint)info.Chunk.FileOffset;
					info.Chunk.VerifyWriteTo(writer);
				}
				methodBodies.WriteReusedMethodBodies(writer, destStreamBaseOffset);
				writer.Position = pos;
			}
			var sectionSizes = new SectionSizes(peImage.ImageNTHeaders.OptionalHeader.FileAlignment, peImage.ImageNTHeaders.OptionalHeader.SectionAlignment, headerLen, GetSectionSizeInfos);
			UpdateHeaderFields(writer, entryPointIsManagedOrNoEntryPoint, entryPointToken, ref sectionSizes);
			OnWriterEvent(ModuleWriterEvent.EndWriteChunks);

			OnWriterEvent(ModuleWriterEvent.BeginStrongNameSign);
			if (!(Options.StrongNameKey is null))
				StrongNameSign((long)strongNameSignature.FileOffset);
			OnWriterEvent(ModuleWriterEvent.EndStrongNameSign);

			OnWriterEvent(ModuleWriterEvent.BeginWritePEChecksum);
			if (Options.AddCheckSum) {
				destStream.Position = destStreamBaseOffset;
				uint newCheckSum = destStream.CalculatePECheckSum(imageLength, checkSumOffset);
				writer.Position = checkSumOffset;
				writer.WriteUInt32(newCheckSum);
			}
			OnWriterEvent(ModuleWriterEvent.EndWritePEChecksum);

			return imageLength;
		}

		static bool TryGetRealDebugDirectorySize(IPEImage peImage, out uint realSize) {
			realSize = 0;
			if (peImage.ImageDebugDirectories.Count == 0)
				return false;
			var dirs = new List<ImageDebugDirectory>(peImage.ImageDebugDirectories);
			dirs.Sort((a, b) => a.AddressOfRawData.CompareTo(b.AddressOfRawData));
			var debugDataDir = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[6];
			var prevEnd = (uint)debugDataDir.VirtualAddress + debugDataDir.Size;
			for (int i = 0; i < dirs.Count; i++) {
				uint prevEndAligned = (prevEnd + 3) & ~3U;
				var dir = dirs[i];
				if (dir.AddressOfRawData == 0 || dir.SizeOfData == 0)
					continue;
				if (!(prevEnd <= (uint)dir.AddressOfRawData && (uint)dir.AddressOfRawData <= prevEndAligned))
					return false;
				prevEnd = (uint)dir.AddressOfRawData + dir.SizeOfData;
			}

			realSize = prevEnd - (uint)debugDataDir.VirtualAddress;
			return true;
		}

		/// <summary>
		/// <c>true</c> if image is 64-bit
		/// </summary>
		bool Is64Bit() => peImage.ImageNTHeaders.OptionalHeader is ImageOptionalHeader64;

		Characteristics GetCharacteristics() {
			var ch = module.Characteristics;
			if (Is64Bit())
				ch &= ~Characteristics.Bit32Machine;
			else
				ch |= Characteristics.Bit32Machine;
			if (Options.IsExeFile)
				ch &= ~Characteristics.Dll;
			else
				ch |= Characteristics.Dll;
			return ch;
		}

		/// <summary>
		/// Updates the PE header and COR20 header fields that need updating. All sections are
		/// also updated, and the new ones are added.
		/// </summary>
		void UpdateHeaderFields(DataWriter writer, bool entryPointIsManagedOrNoEntryPoint, uint entryPointToken, ref SectionSizes sectionSizes) {
			long fileHeaderOffset = destStreamBaseOffset + (long)peImage.ImageNTHeaders.FileHeader.StartOffset;
			long optionalHeaderOffset = destStreamBaseOffset + (long)peImage.ImageNTHeaders.OptionalHeader.StartOffset;
			long sectionsOffset = destStreamBaseOffset + (long)peImage.ImageSectionHeaders[0].StartOffset;
			long dataDirOffset = destStreamBaseOffset + (long)peImage.ImageNTHeaders.OptionalHeader.EndOffset - 16 * 8;
			long cor20Offset = destStreamBaseOffset + (long)imageCor20Header.FileOffset;

			// Update PE file header
			var peOptions = Options.PEHeadersOptions;
			writer.Position = fileHeaderOffset;
			writer.WriteUInt16((ushort)(peOptions.Machine ?? module.Machine));
			writer.WriteUInt16((ushort)(origSections.Count + sections.Count));
			WriteUInt32(writer, peOptions.TimeDateStamp);
			WriteUInt32(writer, peOptions.PointerToSymbolTable);
			WriteUInt32(writer, peOptions.NumberOfSymbols);
			writer.Position += 2;    // sizeof(SizeOfOptionalHeader)
			writer.WriteUInt16((ushort)(peOptions.Characteristics ?? GetCharacteristics()));

			// Update optional header
			writer.Position = optionalHeaderOffset;
			bool is32BitOptionalHeader = peImage.ImageNTHeaders.OptionalHeader is ImageOptionalHeader32;
			if (is32BitOptionalHeader) {
				writer.Position += 2;
				WriteByte(writer, peOptions.MajorLinkerVersion);
				WriteByte(writer, peOptions.MinorLinkerVersion);
				writer.WriteUInt32(sectionSizes.SizeOfCode);
				writer.WriteUInt32(sectionSizes.SizeOfInitdData);
				writer.WriteUInt32(sectionSizes.SizeOfUninitdData);
				writer.Position += 4;	// EntryPoint
				writer.WriteUInt32(sectionSizes.BaseOfCode);
				writer.WriteUInt32(sectionSizes.BaseOfData);
				WriteUInt32(writer, peOptions.ImageBase);
				writer.Position += 8;	// SectionAlignment, FileAlignment
				WriteUInt16(writer, peOptions.MajorOperatingSystemVersion);
				WriteUInt16(writer, peOptions.MinorOperatingSystemVersion);
				WriteUInt16(writer, peOptions.MajorImageVersion);
				WriteUInt16(writer, peOptions.MinorImageVersion);
				WriteUInt16(writer, peOptions.MajorSubsystemVersion);
				WriteUInt16(writer, peOptions.MinorSubsystemVersion);
				WriteUInt32(writer, peOptions.Win32VersionValue);
				writer.WriteUInt32(sectionSizes.SizeOfImage);
				writer.WriteUInt32(sectionSizes.SizeOfHeaders);
				checkSumOffset = writer.Position;
				writer.WriteInt32(0);	// CheckSum
				WriteUInt16(writer, peOptions.Subsystem);
				WriteUInt16(writer, peOptions.DllCharacteristics);
				WriteUInt32(writer, peOptions.SizeOfStackReserve);
				WriteUInt32(writer, peOptions.SizeOfStackCommit);
				WriteUInt32(writer, peOptions.SizeOfHeapReserve);
				WriteUInt32(writer, peOptions.SizeOfHeapCommit);
				WriteUInt32(writer, peOptions.LoaderFlags);
				WriteUInt32(writer, peOptions.NumberOfRvaAndSizes);
			}
			else {
				writer.Position += 2;
				WriteByte(writer, peOptions.MajorLinkerVersion);
				WriteByte(writer, peOptions.MinorLinkerVersion);
				writer.WriteUInt32(sectionSizes.SizeOfCode);
				writer.WriteUInt32(sectionSizes.SizeOfInitdData);
				writer.WriteUInt32(sectionSizes.SizeOfUninitdData);
				writer.Position += 4;	// EntryPoint
				writer.WriteUInt32(sectionSizes.BaseOfCode);
				WriteUInt64(writer, peOptions.ImageBase);
				writer.Position += 8;	// SectionAlignment, FileAlignment
				WriteUInt16(writer, peOptions.MajorOperatingSystemVersion);
				WriteUInt16(writer, peOptions.MinorOperatingSystemVersion);
				WriteUInt16(writer, peOptions.MajorImageVersion);
				WriteUInt16(writer, peOptions.MinorImageVersion);
				WriteUInt16(writer, peOptions.MajorSubsystemVersion);
				WriteUInt16(writer, peOptions.MinorSubsystemVersion);
				WriteUInt32(writer, peOptions.Win32VersionValue);
				writer.WriteUInt32(sectionSizes.SizeOfImage);
				writer.WriteUInt32(sectionSizes.SizeOfHeaders);
				checkSumOffset = writer.Position;
				writer.WriteInt32(0);	// CheckSum
				WriteUInt16(writer, peOptions.Subsystem ?? GetSubsystem());
				WriteUInt16(writer, peOptions.DllCharacteristics ?? module.DllCharacteristics);
				WriteUInt64(writer, peOptions.SizeOfStackReserve);
				WriteUInt64(writer, peOptions.SizeOfStackCommit);
				WriteUInt64(writer, peOptions.SizeOfHeapReserve);
				WriteUInt64(writer, peOptions.SizeOfHeapCommit);
				WriteUInt32(writer, peOptions.LoaderFlags);
				WriteUInt32(writer, peOptions.NumberOfRvaAndSizes);
			}

			// Update Win32 resources data directory, if we wrote a new one
			if (!(win32Resources is null)) {
				writer.Position = dataDirOffset + 2 * 8;
				writer.WriteDataDirectory(win32Resources);
			}

			// Clear the security descriptor directory
			writer.Position = dataDirOffset + 4 * 8;
			writer.WriteDataDirectory(null);

			// Write a new debug directory
			writer.Position = dataDirOffset + 6 * 8;
			writer.WriteDebugDirectory(debugDirectory);

			// Write a new Metadata data directory
			writer.Position = dataDirOffset + 14 * 8;
			writer.WriteDataDirectory(imageCor20Header);

			// Update old sections, and add new sections
			writer.Position = sectionsOffset;
			foreach (var section in origSections) {
				writer.Position += 0x14;
				writer.WriteUInt32((uint)section.Chunk.FileOffset);	// PointerToRawData
				writer.Position += 0x10;
			}
			foreach (var section in sections)
				section.WriteHeaderTo(writer, peImage.ImageNTHeaders.OptionalHeader.FileAlignment, peImage.ImageNTHeaders.OptionalHeader.SectionAlignment, (uint)section.RVA);

			// Write the .NET header
			writer.Position = cor20Offset;
			writer.WriteInt32(0x48);		// cb
			WriteUInt16(writer, Options.Cor20HeaderOptions.MajorRuntimeVersion);
			WriteUInt16(writer, Options.Cor20HeaderOptions.MinorRuntimeVersion);
			writer.WriteDataDirectory(metadata);
			writer.WriteUInt32((uint)GetComImageFlags(entryPointIsManagedOrNoEntryPoint));
			writer.WriteUInt32(entryPointToken);
			writer.WriteDataDirectory(netResources);
			writer.WriteDataDirectory(strongNameSignature);
			WriteDataDirectory(writer, module.Metadata.ImageCor20Header.CodeManagerTable);
			WriteDataDirectory(writer, module.Metadata.ImageCor20Header.VTableFixups);
			WriteDataDirectory(writer, module.Metadata.ImageCor20Header.ExportAddressTableJumps);
			WriteDataDirectory(writer, module.Metadata.ImageCor20Header.ManagedNativeHeader);

			UpdateVTableFixups(writer);
		}

		static void WriteDataDirectory(DataWriter writer, ImageDataDirectory dataDir) {
			writer.WriteUInt32((uint)dataDir.VirtualAddress);
			writer.WriteUInt32(dataDir.Size);
		}

		static void WriteByte(DataWriter writer, byte? value) {
			if (value is null)
				writer.Position++;
			else
				writer.WriteByte(value.Value);
		}

		static void WriteUInt16(DataWriter writer, ushort? value) {
			if (value is null)
				writer.Position += 2;
			else
				writer.WriteUInt16(value.Value);
		}

		static void WriteUInt16(DataWriter writer, Subsystem? value) {
			if (value is null)
				writer.Position += 2;
			else
				writer.WriteUInt16((ushort)value.Value);
		}

		static void WriteUInt16(DataWriter writer, DllCharacteristics? value) {
			if (value is null)
				writer.Position += 2;
			else
				writer.WriteUInt16((ushort)value.Value);
		}

		static void WriteUInt32(DataWriter writer, uint? value) {
			if (value is null)
				writer.Position += 4;
			else
				writer.WriteUInt32(value.Value);
		}

		static void WriteUInt32(DataWriter writer, ulong? value) {
			if (value is null)
				writer.Position += 4;
			else
				writer.WriteUInt32((uint)value.Value);
		}

		static void WriteUInt64(DataWriter writer, ulong? value) {
			if (value is null)
				writer.Position += 8;
			else
				writer.WriteUInt64(value.Value);
		}

		ComImageFlags GetComImageFlags(bool isManagedEntryPoint) {
			var flags = Options.Cor20HeaderOptions.Flags ?? module.Cor20HeaderFlags;
			if (!(Options.Cor20HeaderOptions.EntryPoint is null))
				return flags;
			if (isManagedEntryPoint)
				return flags & ~ComImageFlags.NativeEntryPoint;
			return flags | ComImageFlags.NativeEntryPoint;
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
				var section = sect.PESection;
				if (section.VirtualAddress <= rva && rva < section.VirtualAddress + Math.Max(section.VirtualSize, section.SizeOfRawData))
					return destStreamBaseOffset + (long)sect.Chunk.FileOffset + (rva - section.VirtualAddress);
			}
			return 0;
		}

		IEnumerable<SectionSizeInfo> GetSectionSizeInfos() {
			foreach (var section in origSections)
				yield return new SectionSizeInfo(section.Chunk.GetVirtualSize(), section.PESection.Characteristics);
			foreach (var section in sections)
				yield return new SectionSizeInfo(section.GetVirtualSize(), section.Characteristics);
		}

		void UpdateVTableFixups(DataWriter writer) {
			var vtableFixups = module.VTableFixups;
			if (vtableFixups is null || vtableFixups.VTables.Count == 0)
				return;

			writer.Position = ToWriterOffset(vtableFixups.RVA);
			if (writer.Position == 0) {
				Error("Could not convert RVA to file offset");
				return;
			}
			foreach (var vtable in vtableFixups) {
				if (vtable.Methods.Count > ushort.MaxValue)
					throw new ModuleWriterException("Too many methods in vtable");
				writer.WriteUInt32((uint)vtable.RVA);
				writer.WriteUInt16((ushort)vtable.Methods.Count);
				writer.WriteUInt16((ushort)vtable.Flags);

				long pos = writer.Position;
				writer.Position = ToWriterOffset(vtable.RVA);
				if (writer.Position == 0) {
					if (vtable.RVA != 0 || vtable.Methods.Count > 0)
						Error("Could not convert RVA to file offset");
				}
				else {
					var methods = vtable.Methods;
					int count = methods.Count;
					for (int i = 0; i < count; i++) {
						var method = methods[i];
						writer.WriteUInt32(GetMethodToken(method));
						if (vtable.Is64Bit)
							writer.WriteInt32(0);
					}
				}
				writer.Position = pos;
			}
		}

		uint GetMethodToken(IMethod method) {
			if (method is MethodDef md)
				return new MDToken(Table.Method, metadata.GetRid(md)).Raw;

			if (method is MemberRef mr)
				return new MDToken(Table.MemberRef, metadata.GetRid(mr)).Raw;

			if (method is MethodSpec ms)
				return new MDToken(Table.MethodSpec, metadata.GetRid(ms)).Raw;

			if (method is null)
				return 0;

			Error("Invalid VTable method type: {0}", method.GetType());
			return 0;
		}

		/// <summary>
		/// Gets the entry point
		/// </summary>
		/// <param name="ep">Updated with entry point (either a token or RVA of native method)</param>
		/// <returns><c>true</c> if it's a managed entry point or there's no entry point,
		/// <c>false</c> if it's a native entry point</returns>
		bool GetEntryPoint(out uint ep) {
			var tok = Options.Cor20HeaderOptions.EntryPoint;
			if (!(tok is null)) {
				ep = tok.Value;
				return ep == 0 || ((Options.Cor20HeaderOptions.Flags ?? 0) & ComImageFlags.NativeEntryPoint) == 0;
			}

			if (module.ManagedEntryPoint is MethodDef epMethod) {
				ep = new MDToken(Table.Method, metadata.GetRid(epMethod)).Raw;
				return true;
			}
			if (module.ManagedEntryPoint is FileDef file) {
				ep = new MDToken(Table.File, metadata.GetRid(file)).Raw;
				return true;
			}
			ep = (uint)module.NativeEntryPoint;
			return ep == 0;
		}
	}
}
