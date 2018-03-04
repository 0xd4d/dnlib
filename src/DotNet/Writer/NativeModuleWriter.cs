// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Collections.Generic;
using System.IO;
using dnlib.IO;
using dnlib.PE;
using dnlib.W32Resources;
using dnlib.DotNet.MD;

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

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		public NativeModuleWriterOptions(ModuleDefMD module)
			: this(module, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="listener">Module writer listener</param>
		public NativeModuleWriterOptions(ModuleDefMD module, IModuleWriterListener listener)
			: base(module, listener) {

			// C++ .NET mixed mode assemblies sometimes/often call Module.ResolveMethod(),
			// so method metadata tokens must be preserved.
			MetaDataOptions.Flags |= MetaDataFlags.PreserveAllMethodRids;
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
		BinaryReaderChunk extraData;

		/// <summary>The original PE headers</summary>
		BinaryReaderChunk headerSection;

		/// <summary>The original PE sections and their data</summary>
		List<OrigSection> origSections;

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
			public BinaryReaderChunk Chunk;

			/// <summary>
			/// Constructor
			/// </summary>
			/// <param name="peSection">PE section</param>
			public OrigSection(ImageSectionHeader peSection) {
				this.PESection = peSection;
			}

			/// <inheritdoc/>
			public void Dispose() {
				if (Chunk != null)
					Chunk.Data.Dispose();
				Chunk = null;
				PESection = null;
			}

			/// <inheritdoc/>
			public override string ToString() {
				uint offs = Chunk.Data is IImageStream ? (uint)((IImageStream)Chunk.Data).FileOffset : 0;
				return string.Format("{0} FO:{1:X8} L:{2:X8}", PESection.DisplayName, offs, (uint)Chunk.Data.Length);
			}
		}

		/// <summary>
		/// Gets the module
		/// </summary>
		public ModuleDefMD ModuleDefMD {
			get { return module; }
		}

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
		public NativeModuleWriterOptions Options {
			get { return options ?? (options = new NativeModuleWriterOptions(module)); }
			set { options = value; }
		}

		/// <summary>
		/// Gets all <see cref="PESection"/>s
		/// </summary>
		public override List<PESection> Sections {
			get { return sections; }
		}

		/// <summary>
		/// Gets the original PE sections and their data
		/// </summary>
		public List<OrigSection> OrigSections {
			get { return origSections; }
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
		protected override long WriteImpl() {
			try {
				return Write();
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

		long Write() {
			Initialize();

			// It's not safe to create new Field RVAs so re-use them all. The user can override
			// this by setting field.RVA = 0 when creating a new field.InitialValue.
			metaData.KeepFieldRVA = true;

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

		void CreateSections() {
			CreatePESections();
			CreateRawSections();
			CreateHeaderSection();
			CreateExtraData();
		}

		void CreateChunks() {
			CreateMetaDataChunks(module);

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
			textSection.Add(metaData, DEFAULT_METADATA_ALIGNMENT);
			textSection.Add(debugDirectory, DebugDirectory.DEFAULT_DEBUGDIRECTORY_ALIGNMENT);
			if (rsrcSection != null)
				rsrcSection.Add(win32Resources, DEFAULT_WIN32_RESOURCES_ALIGNMENT);
		}

		/// <inheritdoc/>
		protected override Win32Resources GetWin32Resources() {
			if (Options.KeepWin32Resources)
				return null;
			return Options.Win32Resources ?? module.Win32Resources;
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
				newSection.Chunk = new BinaryReaderChunk(peImage.CreateStream(peSection.VirtualAddress, sectionSize), peSection.VirtualSize);
			}
		}

		/// <summary>
		/// Creates the PE header "section"
		/// </summary>
		void CreateHeaderSection() {
			uint afterLastSectHeader = GetOffsetAfterLastSectionHeader() + (uint)sections.Count * 0x28;
			uint firstRawOffset = Math.Min(GetFirstRawDataFileOffset(), peImage.ImageNTHeaders.OptionalHeader.SectionAlignment);
			uint headerLen = afterLastSectHeader;
			if (firstRawOffset > headerLen)
				headerLen = firstRawOffset;
			headerLen = Utils.AlignUp(headerLen, peImage.ImageNTHeaders.OptionalHeader.FileAlignment);
			if (headerLen <= peImage.ImageNTHeaders.OptionalHeader.SectionAlignment) {
				headerSection = new BinaryReaderChunk(peImage.CreateStream(0, headerLen));
				return;
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
			extraData = new BinaryReaderChunk(peImage.CreateStream((FileOffset)lastOffs));
			if (extraData.Data.Length == 0) {
				extraData.Data.Dispose();
				extraData = null;
			}
		}

		uint GetLastFileSectionOffset() {
			uint rva = 0;
			foreach (var sect in origSections)
				rva = Math.Max(rva, (uint)sect.PESection.VirtualAddress + sect.PESection.SizeOfRawData);
			return (uint)peImage.ToFileOffset((RVA)(rva - 1)) + 1;
		}

		long WriteFile() {
			uint entryPointToken;
			bool entryPointIsManagedOrNoEntryPoint = GetEntryPoint(out entryPointToken);

			Listener.OnWriterEvent(this, ModuleWriterEvent.BeginWritePdb);
			WritePdbFile();
			Listener.OnWriterEvent(this, ModuleWriterEvent.EndWritePdb);

			Listener.OnWriterEvent(this, ModuleWriterEvent.BeginCalculateRvasAndFileOffsets);

			var chunks = new List<IChunk>();
			chunks.Add(headerSection);
			foreach (var origSection in origSections)
				chunks.Add(origSection.Chunk);
			foreach (var section in sections)
				chunks.Add(section);
			if (extraData != null)
				chunks.Add(extraData);

			CalculateRvasAndFileOffsets(chunks, 0, 0, peImage.ImageNTHeaders.OptionalHeader.FileAlignment, peImage.ImageNTHeaders.OptionalHeader.SectionAlignment);
			foreach (var section in origSections) {
				if (section.Chunk.RVA != section.PESection.VirtualAddress)
					throw new ModuleWriterException("Invalid section RVA");
			}
			Listener.OnWriterEvent(this, ModuleWriterEvent.EndCalculateRvasAndFileOffsets);

			Listener.OnWriterEvent(this, ModuleWriterEvent.BeginWriteChunks);
			var writer = new BinaryWriter(destStream);
			WriteChunks(writer, chunks, 0, peImage.ImageNTHeaders.OptionalHeader.FileAlignment);
			long imageLength = writer.BaseStream.Position - destStreamBaseOffset;
			UpdateHeaderFields(writer, entryPointIsManagedOrNoEntryPoint, entryPointToken);
			Listener.OnWriterEvent(this, ModuleWriterEvent.EndWriteChunks);

			Listener.OnWriterEvent(this, ModuleWriterEvent.BeginStrongNameSign);
			if (Options.StrongNameKey != null)
				StrongNameSign((long)strongNameSignature.FileOffset);
			Listener.OnWriterEvent(this, ModuleWriterEvent.EndStrongNameSign);

			Listener.OnWriterEvent(this, ModuleWriterEvent.BeginWritePEChecksum);
			if (Options.AddCheckSum) {
				destStream.Position = destStreamBaseOffset;
				uint newCheckSum = new BinaryReader(destStream).CalculatePECheckSum(imageLength, checkSumOffset);
				writer.BaseStream.Position = checkSumOffset;
				writer.Write(newCheckSum);
			}
			Listener.OnWriterEvent(this, ModuleWriterEvent.EndWritePEChecksum);

			return imageLength;
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
		void UpdateHeaderFields(BinaryWriter writer, bool entryPointIsManagedOrNoEntryPoint, uint entryPointToken) {
			long fileHeaderOffset = destStreamBaseOffset + (long)peImage.ImageNTHeaders.FileHeader.StartOffset;
			long optionalHeaderOffset = destStreamBaseOffset + (long)peImage.ImageNTHeaders.OptionalHeader.StartOffset;
			long sectionsOffset = destStreamBaseOffset + (long)peImage.ImageSectionHeaders[0].StartOffset;
			long dataDirOffset = destStreamBaseOffset + (long)peImage.ImageNTHeaders.OptionalHeader.EndOffset - 16 * 8;
			long cor20Offset = destStreamBaseOffset + (long)imageCor20Header.FileOffset;

			uint fileAlignment = peImage.ImageNTHeaders.OptionalHeader.FileAlignment;
			uint sectionAlignment = peImage.ImageNTHeaders.OptionalHeader.SectionAlignment;

			// Update PE file header
			var peOptions = Options.PEHeadersOptions;
			writer.BaseStream.Position = fileHeaderOffset;
			writer.Write((ushort)(peOptions.Machine ?? module.Machine));
			writer.Write((ushort)(origSections.Count + sections.Count));
			WriteUInt32(writer, peOptions.TimeDateStamp);
			WriteUInt32(writer, peOptions.PointerToSymbolTable);
			WriteUInt32(writer, peOptions.NumberOfSymbols);
			writer.BaseStream.Position += 2;    // sizeof(SizeOfOptionalHeader)
			writer.Write((ushort)(peOptions.Characteristics ?? GetCharacteristics()));

			// Update optional header
			var sectionSizes = new SectionSizes(fileAlignment, sectionAlignment, headerSection.GetVirtualSize(), GetSectionSizeInfos);
			writer.BaseStream.Position = optionalHeaderOffset;
			bool is32BitOptionalHeader = peImage.ImageNTHeaders.OptionalHeader is ImageOptionalHeader32;
			if (is32BitOptionalHeader) {
				writer.BaseStream.Position += 2;
				WriteByte(writer, peOptions.MajorLinkerVersion);
				WriteByte(writer, peOptions.MinorLinkerVersion);
				writer.Write(sectionSizes.SizeOfCode);
				writer.Write(sectionSizes.SizeOfInitdData);
				writer.Write(sectionSizes.SizeOfUninitdData);
				writer.BaseStream.Position += 4;	// EntryPoint
				writer.Write(sectionSizes.BaseOfCode);
				writer.Write(sectionSizes.BaseOfData);
				WriteUInt32(writer, peOptions.ImageBase);
				writer.BaseStream.Position += 8;	// SectionAlignment, FileAlignment
				WriteUInt16(writer, peOptions.MajorOperatingSystemVersion);
				WriteUInt16(writer, peOptions.MinorOperatingSystemVersion);
				WriteUInt16(writer, peOptions.MajorImageVersion);
				WriteUInt16(writer, peOptions.MinorImageVersion);
				WriteUInt16(writer, peOptions.MajorSubsystemVersion);
				WriteUInt16(writer, peOptions.MinorSubsystemVersion);
				WriteUInt32(writer, peOptions.Win32VersionValue);
				writer.Write(sectionSizes.SizeOfImage);
				writer.Write(sectionSizes.SizeOfHeaders);
				checkSumOffset = writer.BaseStream.Position;
				writer.Write(0);	// CheckSum
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
				writer.BaseStream.Position += 2;
				WriteByte(writer, peOptions.MajorLinkerVersion);
				WriteByte(writer, peOptions.MinorLinkerVersion);
				writer.Write(sectionSizes.SizeOfCode);
				writer.Write(sectionSizes.SizeOfInitdData);
				writer.Write(sectionSizes.SizeOfUninitdData);
				writer.BaseStream.Position += 4;	// EntryPoint
				writer.Write(sectionSizes.BaseOfCode);
				WriteUInt64(writer, peOptions.ImageBase);
				writer.BaseStream.Position += 8;	// SectionAlignment, FileAlignment
				WriteUInt16(writer, peOptions.MajorOperatingSystemVersion);
				WriteUInt16(writer, peOptions.MinorOperatingSystemVersion);
				WriteUInt16(writer, peOptions.MajorImageVersion);
				WriteUInt16(writer, peOptions.MinorImageVersion);
				WriteUInt16(writer, peOptions.MajorSubsystemVersion);
				WriteUInt16(writer, peOptions.MinorSubsystemVersion);
				WriteUInt32(writer, peOptions.Win32VersionValue);
				writer.Write(sectionSizes.SizeOfImage);
				writer.Write(sectionSizes.SizeOfHeaders);
				checkSumOffset = writer.BaseStream.Position;
				writer.Write(0);	// CheckSum
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
			if (win32Resources != null) {
				writer.BaseStream.Position = dataDirOffset + 2 * 8;
				writer.WriteDataDirectory(win32Resources);
			}

			// Clear the security descriptor directory
			writer.BaseStream.Position = dataDirOffset + 4 * 8;
			writer.WriteDataDirectory(null);

			// Write a new debug directory
			writer.BaseStream.Position = dataDirOffset + 6 * 8;
			writer.WriteDataDirectory(debugDirectory);

			// Write a new Metadata data directory
			writer.BaseStream.Position = dataDirOffset + 14 * 8;
			writer.WriteDataDirectory(imageCor20Header);

			// Update old sections, and add new sections
			writer.BaseStream.Position = sectionsOffset;
			foreach (var section in origSections) {
				writer.BaseStream.Position += 0x14;
				writer.Write((uint)section.Chunk.FileOffset);	// PointerToRawData
				writer.BaseStream.Position += 0x10;
			}
			foreach (var section in sections)
				section.WriteHeaderTo(writer, fileAlignment, sectionAlignment, (uint)section.RVA);

			// Write the .NET header
			writer.BaseStream.Position = cor20Offset;
			writer.Write(0x48);		// cb
			WriteUInt16(writer, Options.Cor20HeaderOptions.MajorRuntimeVersion);
			WriteUInt16(writer, Options.Cor20HeaderOptions.MinorRuntimeVersion);
			writer.WriteDataDirectory(metaData);
			writer.Write((uint)GetComImageFlags(entryPointIsManagedOrNoEntryPoint));
			writer.Write(entryPointToken);
			writer.WriteDataDirectory(netResources);
			writer.WriteDataDirectory(strongNameSignature);
			WriteDataDirectory(writer, module.MetaData.ImageCor20Header.CodeManagerTable);
			WriteDataDirectory(writer, module.MetaData.ImageCor20Header.VTableFixups);
			WriteDataDirectory(writer, module.MetaData.ImageCor20Header.ExportAddressTableJumps);
			WriteDataDirectory(writer, module.MetaData.ImageCor20Header.ManagedNativeHeader);

			UpdateVTableFixups(writer);
		}

		static void WriteDataDirectory(BinaryWriter writer, ImageDataDirectory dataDir) {
			writer.Write((uint)dataDir.VirtualAddress);
			writer.Write(dataDir.Size);
		}

		static void WriteByte(BinaryWriter writer, byte? value) {
			if (value == null)
				writer.BaseStream.Position++;
			else
				writer.Write(value.Value);
		}

		static void WriteUInt16(BinaryWriter writer, ushort? value) {
			if (value == null)
				writer.BaseStream.Position += 2;
			else
				writer.Write(value.Value);
		}

		static void WriteUInt16(BinaryWriter writer, Subsystem? value) {
			if (value == null)
				writer.BaseStream.Position += 2;
			else
				writer.Write((ushort)value.Value);
		}

		static void WriteUInt16(BinaryWriter writer, DllCharacteristics? value) {
			if (value == null)
				writer.BaseStream.Position += 2;
			else
				writer.Write((ushort)value.Value);
		}

		static void WriteUInt32(BinaryWriter writer, uint? value) {
			if (value == null)
				writer.BaseStream.Position += 4;
			else
				writer.Write(value.Value);
		}

		static void WriteUInt32(BinaryWriter writer, ulong? value) {
			if (value == null)
				writer.BaseStream.Position += 4;
			else
				writer.Write((uint)value.Value);
		}

		static void WriteUInt64(BinaryWriter writer, ulong? value) {
			if (value == null)
				writer.BaseStream.Position += 8;
			else
				writer.Write(value.Value);
		}

		ComImageFlags GetComImageFlags(bool isManagedEntryPoint) {
			var flags = Options.Cor20HeaderOptions.Flags ?? module.Cor20HeaderFlags;
			if (Options.Cor20HeaderOptions.EntryPoint != null)
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
				if (writer.BaseStream.Position == 0) {
					if (vtable.RVA != 0 || vtable.Methods.Count > 0)
						Error("Could not convert RVA to file offset");
				}
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
			if (tok != null) {
				ep = tok.Value;
				return ep == 0 || ((Options.Cor20HeaderOptions.Flags ?? 0) & ComImageFlags.NativeEntryPoint) == 0;
			}

			var epMethod = module.ManagedEntryPoint as MethodDef;
			if (epMethod != null) {
				ep = new MDToken(Table.Method, metaData.GetRid(epMethod)).Raw;
				return true;
			}
			var file = module.ManagedEntryPoint as FileDef;
			if (file != null) {
				ep = new MDToken(Table.File, metaData.GetRid(file)).Raw;
				return true;
			}
			ep = (uint)module.NativeEntryPoint;
			return ep == 0;
		}
	}
}
