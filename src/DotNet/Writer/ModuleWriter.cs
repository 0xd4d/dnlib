/*
    Copyright (C) 2012-2013 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

﻿using System.Collections.Generic;
using System.IO;
using dnlib.DotNet.MD;
using dnlib.PE;
using dnlib.IO;
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
		const uint DEFAULT_COR20HEADER_ALIGNMENT = 4;
		const uint DEFAULT_DEBUGDIRECTORY_ALIGNMENT = 4;
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
		StrongNameSignature strongNameSignature;
		DebugDirectory debugDirectory;
		ImportDirectory importDirectory;
		StartupStub startupStub;
		RelocDirectory relocDirectory;

		/// <summary>
		/// Gets the module
		/// </summary>
		public ModuleDef Module {
			get { return module; }
		}

		/// <inheritdoc/>
		protected override ModuleDef TheModule {
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

			if (Options.StrongNameKey != null)
				strongNameSignature = new StrongNameSignature(Options.StrongNameKey.SignatureSize);
			else if (module.Assembly != null && !PublicKeyBase.IsNullOrEmpty2(module.Assembly.PublicKey)) {
				int len = module.Assembly.PublicKey.Data.Length - 0x20;
				strongNameSignature = new StrongNameSignature(len > 0 ? len : 0x80);
			}
			else if (((Options.Cor20HeaderOptions.Flags ?? module.Cor20HeaderFlags) & ComImageFlags.StrongNameSigned) != 0)
				strongNameSignature = new StrongNameSignature(0x80);

			imageCor20Header = new ImageCor20Header(Options.Cor20HeaderOptions);
			CreateMetaDataChunks(module);

			if (hasDebugDirectory)
				debugDirectory = new DebugDirectory();

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
				rsrcSection.Add(win32Resources, DEFAULT_WIN32_RESOURCES_ALIGNMENT);
			if (relocSection != null)
				relocSection.Add(relocDirectory, DEFAULT_RELOC_ALIGNMENT);
		}

		long WriteFile() {
			var chunks = new List<IChunk>();
			chunks.Add(peHeaders);
			foreach (var section in sections)
				chunks.Add(section);

			Listener.OnWriterEvent(this, ModuleWriterEvent.BeginCalculateRvasAndFileOffsets);
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
