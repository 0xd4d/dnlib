// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using dnlib.IO;
using dnlib.DotNet.Pdb;
using dnlib.PE;
using dnlib.W32Resources;
using dnlib.DotNet.MD;
using System.Diagnostics;
using dnlib.DotNet.Pdb.WindowsPdb;
using System.Text;
using System.IO.Compression;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Module writer event args
	/// </summary>
	public sealed class ModuleWriterEventArgs : EventArgs {
		/// <summary>
		/// Gets the writer (<see cref="ModuleWriter"/> or <see cref="NativeModuleWriter"/>)
		/// </summary>
		public ModuleWriterBase Writer { get; }

		/// <summary>
		/// Gets the event
		/// </summary>
		public ModuleWriterEvent Event { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="event">Event</param>
		public ModuleWriterEventArgs(ModuleWriterBase writer, ModuleWriterEvent @event) {
			Writer = writer ?? throw new ArgumentNullException(nameof(writer));
			Event = @event;
		}
	}

	/// <summary>
	/// Module writer progress event args
	/// </summary>
	public sealed class ModuleWriterProgressEventArgs : EventArgs {
		/// <summary>
		/// Gets the writer (<see cref="ModuleWriter"/> or <see cref="NativeModuleWriter"/>)
		/// </summary>
		public ModuleWriterBase Writer { get; }

		/// <summary>
		/// Gets the progress, 0.0 - 1.0
		/// </summary>
		public double Progress { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="progress">Progress, 0.0 - 1.0</param>
		public ModuleWriterProgressEventArgs(ModuleWriterBase writer, double progress) {
			if (progress < 0 || progress > 1)
				throw new ArgumentOutOfRangeException(nameof(progress));
			Writer = writer ?? throw new ArgumentNullException(nameof(writer));
			Progress = progress;
		}
	}

	/// <summary>
	/// Common module writer options base class
	/// </summary>
	public class ModuleWriterOptionsBase {
		IModuleWriterListener listener;
		PEHeadersOptions peHeadersOptions;
		Cor20HeaderOptions cor20HeaderOptions;
		MetadataOptions metadataOptions;
		ILogger logger;
		ILogger metadataLogger;
		Win32Resources win32Resources;
		StrongNameKey strongNameKey;
		StrongNamePublicKey strongNamePublicKey;
		bool delaySign;

		/// <summary>
		/// Gets/sets the listener
		/// </summary>
		[Obsolete("Use event " + nameof(WriterEvent) + " instead of " + nameof(IModuleWriterListener), error: false)]
		public IModuleWriterListener Listener {
			get => listener;
			set => listener = value;
		}

		/// <summary>
		/// Raised at various times when writing the file. The listener has a chance to modify
		/// the file, eg. add extra metadata, encrypt methods, etc.
		/// </summary>
		public event EventHandler<ModuleWriterEventArgs> WriterEvent;
		internal void RaiseEvent(object sender, ModuleWriterEventArgs e) => WriterEvent?.Invoke(sender, e);

		/// <summary>
		/// Raised when the progress is updated
		/// </summary>
		public event EventHandler<ModuleWriterProgressEventArgs> ProgressUpdated;
		internal void RaiseEvent(object sender, ModuleWriterProgressEventArgs e) => ProgressUpdated?.Invoke(sender, e);

		/// <summary>
		/// Gets/sets the logger. If this is <c>null</c>, any errors result in a
		/// <see cref="ModuleWriterException"/> being thrown. To disable this behavior, either
		/// create your own logger or use <see cref="DummyLogger.NoThrowInstance"/>.
		/// </summary>
		public ILogger Logger {
			get => logger;
			set => logger = value;
		}

		/// <summary>
		/// Gets/sets the <see cref="Metadata"/> writer logger. If this is <c>null</c>, use
		/// <see cref="Logger"/>.
		/// </summary>
		public ILogger MetadataLogger {
			get => metadataLogger;
			set => metadataLogger = value;
		}

		/// <summary>
		/// Gets/sets the <see cref="PEHeaders"/> options. This is never <c>null</c>.
		/// </summary>
		public PEHeadersOptions PEHeadersOptions {
			get => peHeadersOptions ?? (peHeadersOptions = new PEHeadersOptions());
			set => peHeadersOptions = value;
		}

		/// <summary>
		/// Gets/sets the <see cref="ImageCor20Header"/> options. This is never <c>null</c>.
		/// </summary>
		public Cor20HeaderOptions Cor20HeaderOptions {
			get => cor20HeaderOptions ?? (cor20HeaderOptions = new Cor20HeaderOptions());
			set => cor20HeaderOptions = value;
		}

		/// <summary>
		/// Gets/sets the <see cref="Metadata"/> options. This is never <c>null</c>.
		/// </summary>
		public MetadataOptions MetadataOptions {
			get => metadataOptions ?? (metadataOptions = new MetadataOptions());
			set => metadataOptions = value;
		}

		/// <summary>
		/// Gets/sets the Win32 resources. If this is <c>null</c>, use the module's
		/// Win32 resources if any.
		/// </summary>
		public Win32Resources Win32Resources {
			get => win32Resources;
			set => win32Resources = value;
		}

		/// <summary>
		/// true to delay sign the assembly. Initialize <see cref="StrongNamePublicKey"/> to the
		/// public key to use, and don't initialize <see cref="StrongNameKey"/>. To generate the
		/// public key from your strong name key file, execute <c>sn -p mykey.snk mypublickey.snk</c>
		/// </summary>
		public bool DelaySign {
			get => delaySign;
			set => delaySign = value;
		}

		/// <summary>
		/// Gets/sets the strong name key. When you enhance strong name sign an assembly,
		/// this instance's HashAlgorithm must be initialized to its public key's HashAlgorithm.
		/// You should call <see cref="InitializeStrongNameSigning(ModuleDef,StrongNameKey)"/>
		/// to initialize this property if you use normal strong name signing.
		/// You should call <see cref="InitializeEnhancedStrongNameSigning(ModuleDef,StrongNameKey,StrongNamePublicKey)"/>
		/// or <see cref="InitializeEnhancedStrongNameSigning(ModuleDef,StrongNameKey,StrongNamePublicKey,StrongNameKey,StrongNamePublicKey)"/>
		/// to initialize this property if you use enhanced strong name signing.
		/// </summary>
		public StrongNameKey StrongNameKey {
			get => strongNameKey;
			set => strongNameKey = value;
		}

		/// <summary>
		/// Gets/sets the new public key that should be used. If this is <c>null</c>, use
		/// the public key generated from <see cref="StrongNameKey"/>. If it is also <c>null</c>,
		/// use the module's Assembly's public key.
		/// You should call <see cref="InitializeEnhancedStrongNameSigning(ModuleDef,StrongNameKey,StrongNamePublicKey)"/>
		/// or <see cref="InitializeEnhancedStrongNameSigning(ModuleDef,StrongNameKey,StrongNamePublicKey,StrongNameKey,StrongNamePublicKey)"/>
		/// to initialize this property if you use enhanced strong name signing.
		/// </summary>
		public StrongNamePublicKey StrongNamePublicKey {
			get => strongNamePublicKey;
			set => strongNamePublicKey = value;
		}

		/// <summary>
		/// <c>true</c> if method bodies can be shared (two or more method bodies can share the
		/// same RVA), <c>false</c> if method bodies can't be shared. Don't enable it if there
		/// must be a 1:1 relationship with method bodies and their RVAs.
		/// </summary>
		public bool ShareMethodBodies { get; set; }

		/// <summary>
		/// <c>true</c> if the PE header CheckSum field should be updated, <c>false</c> if the
		/// CheckSum field isn't updated.
		/// </summary>
		public bool AddCheckSum { get; set; }

		/// <summary>
		/// <c>true</c> if it's a 64-bit module, <c>false</c> if it's a 32-bit or AnyCPU module.
		/// </summary>
		public bool Is64Bit {
			get {
				if (!PEHeadersOptions.Machine.HasValue)
					return false;
				return PEHeadersOptions.Machine == Machine.IA64 ||
					PEHeadersOptions.Machine == Machine.AMD64 ||
					PEHeadersOptions.Machine == Machine.ARM64;
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
		public bool IsExeFile => ModuleKind != ModuleKind.Dll && ModuleKind != ModuleKind.NetModule;

		/// <summary>
		/// Set it to <c>true</c> to enable writing a PDB file. Default is <c>false</c> (a PDB file
		/// won't be written to disk).
		/// </summary>
		public bool WritePdb { get; set; }

		/// <summary>
		/// PDB file name. If it's <c>null</c> a PDB file with the same name as the output assembly
		/// will be created but with a PDB extension. <see cref="WritePdb"/> must be <c>true</c> or
		/// this property is ignored.
		/// </summary>
		public string PdbFileName { get; set; }

		/// <summary>
		/// PDB stream. If this is initialized, then you should also set <see cref="PdbFileName"/>
		/// to the name of the PDB file since the file name must be written to the PE debug directory.
		/// <see cref="WritePdb"/> must be <c>true</c> or this property is ignored.
		/// </summary>
		public Stream PdbStream { get; set; }

		/// <summary>
		/// GUID used by some PDB writers, eg. portable PDB writer. It's initialized to a random GUID.
		/// </summary>
		public Guid PdbGuid { get; set; }

		/// <summary>
		/// true if an <c>.mvid</c> section should be added to the assembly. Not used by native module writer.
		/// </summary>
		public bool AddMvidSection { get; set; }

		/// <summary>
		/// Default constructor
		/// </summary>
		protected ModuleWriterOptionsBase() {
			ShareMethodBodies = true;
			ModuleKind = ModuleKind.Windows;
			PdbGuid = Guid.NewGuid();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">The module</param>
		protected ModuleWriterOptionsBase(ModuleDef module) {
			PdbGuid = Guid.NewGuid();
			ShareMethodBodies = true;
			MetadataOptions.MetadataHeaderOptions.VersionString = module.RuntimeVersion;
			ModuleKind = module.Kind;
			PEHeadersOptions.Machine = module.Machine;
			PEHeadersOptions.Characteristics = module.Characteristics;
			PEHeadersOptions.DllCharacteristics = module.DllCharacteristics;
			if (module.Kind == ModuleKind.Windows)
				PEHeadersOptions.Subsystem = Subsystem.WindowsGui;
			else
				PEHeadersOptions.Subsystem = Subsystem.WindowsCui;
			PEHeadersOptions.NumberOfRvaAndSizes = 0x10;
			Cor20HeaderOptions.Flags = module.Cor20HeaderFlags;

			if (module.Assembly != null && !PublicKeyBase.IsNullOrEmpty2(module.Assembly.PublicKey))
				Cor20HeaderOptions.Flags |= ComImageFlags.StrongNameSigned;

			if (module.Cor20HeaderRuntimeVersion != null) {
				Cor20HeaderOptions.MajorRuntimeVersion = (ushort)(module.Cor20HeaderRuntimeVersion.Value >> 16);
				Cor20HeaderOptions.MinorRuntimeVersion = (ushort)module.Cor20HeaderRuntimeVersion.Value;
			}
			else if (module.IsClr1x) {
				Cor20HeaderOptions.MajorRuntimeVersion = 2;
				Cor20HeaderOptions.MinorRuntimeVersion = 0;
			}
			else {
				Cor20HeaderOptions.MajorRuntimeVersion = 2;
				Cor20HeaderOptions.MinorRuntimeVersion = 5;
			}

			if (module.TablesHeaderVersion != null) {
				MetadataOptions.TablesHeapOptions.MajorVersion = (byte)(module.TablesHeaderVersion.Value >> 8);
				MetadataOptions.TablesHeapOptions.MinorVersion = (byte)module.TablesHeaderVersion.Value;
			}
			else if (module.IsClr1x) {
				// Generics aren't supported
				MetadataOptions.TablesHeapOptions.MajorVersion = 1;
				MetadataOptions.TablesHeapOptions.MinorVersion = 0;
			}
			else {
				// Generics are supported
				MetadataOptions.TablesHeapOptions.MajorVersion = 2;
				MetadataOptions.TablesHeapOptions.MinorVersion = 0;
			}

			// Some tools crash if #GUID is missing so always create it by default
			MetadataOptions.Flags |= MetadataFlags.AlwaysCreateGuidHeap;

			var modDefMD = module as ModuleDefMD;
			if (modDefMD != null) {
				var ntHeaders = modDefMD.Metadata.PEImage.ImageNTHeaders;
				PEHeadersOptions.TimeDateStamp = ntHeaders.FileHeader.TimeDateStamp;
				PEHeadersOptions.MajorLinkerVersion = ntHeaders.OptionalHeader.MajorLinkerVersion;
				PEHeadersOptions.MinorLinkerVersion = ntHeaders.OptionalHeader.MinorLinkerVersion;
				PEHeadersOptions.ImageBase = ntHeaders.OptionalHeader.ImageBase;
				PEHeadersOptions.MajorOperatingSystemVersion = ntHeaders.OptionalHeader.MajorOperatingSystemVersion;
				PEHeadersOptions.MinorOperatingSystemVersion = ntHeaders.OptionalHeader.MinorOperatingSystemVersion;
				PEHeadersOptions.MajorImageVersion = ntHeaders.OptionalHeader.MajorImageVersion;
				PEHeadersOptions.MinorImageVersion = ntHeaders.OptionalHeader.MinorImageVersion;
				PEHeadersOptions.MajorSubsystemVersion = ntHeaders.OptionalHeader.MajorSubsystemVersion;
				PEHeadersOptions.MinorSubsystemVersion = ntHeaders.OptionalHeader.MinorSubsystemVersion;
				PEHeadersOptions.Win32VersionValue = ntHeaders.OptionalHeader.Win32VersionValue;
				AddCheckSum = ntHeaders.OptionalHeader.CheckSum != 0;
				AddMvidSection = HasMvidSection(modDefMD.Metadata.PEImage.ImageSectionHeaders);
			}

			if (Is64Bit) {
				PEHeadersOptions.Characteristics &= ~Characteristics.Bit32Machine;
				PEHeadersOptions.Characteristics |= Characteristics.LargeAddressAware;
			}
			else if (modDefMD == null)
				PEHeadersOptions.Characteristics |= Characteristics.Bit32Machine;
		}

		static bool HasMvidSection(IList<ImageSectionHeader> sections) {
			foreach (var section in sections) {
				if (section.VirtualSize != 16)
					continue;
				var name = section.Name;
				// Roslyn ignores the last 2 bytes
				if (name[0] == '.' && name[1] == 'm' && name[2] == 'v' && name[3] == 'i' && name[4] == 'd' && name[5] == 0)
					return true;
			}
			return false;
		}

		/// <summary>
		/// Initializes <see cref="StrongNameKey"/> and <see cref="StrongNamePublicKey"/>
		/// for normal strong name signing.
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="signatureKey">Signature strong name key pair</param>
		public void InitializeStrongNameSigning(ModuleDef module, StrongNameKey signatureKey) {
			StrongNameKey = signatureKey;
			StrongNamePublicKey = null;
			if (module.Assembly != null)
				module.Assembly.CustomAttributes.RemoveAll("System.Reflection.AssemblySignatureKeyAttribute");
		}

		/// <summary>
		/// Initializes <see cref="StrongNameKey"/> and <see cref="StrongNamePublicKey"/>
		/// for enhanced strong name signing (without key migration). See
		/// http://msdn.microsoft.com/en-us/library/hh415055.aspx
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="signatureKey">Signature strong name key pair</param>
		/// <param name="signaturePubKey">Signature public key</param>
		public void InitializeEnhancedStrongNameSigning(ModuleDef module, StrongNameKey signatureKey, StrongNamePublicKey signaturePubKey) {
			InitializeStrongNameSigning(module, signatureKey);
			StrongNameKey = StrongNameKey.WithHashAlgorithm(signaturePubKey.HashAlgorithm);
		}

		/// <summary>
		/// Initializes <see cref="StrongNameKey"/> and <see cref="StrongNamePublicKey"/>
		/// for enhanced strong name signing (with key migration). See
		/// http://msdn.microsoft.com/en-us/library/hh415055.aspx
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="signatureKey">Signature strong name key pair</param>
		/// <param name="signaturePubKey">Signature public key</param>
		/// <param name="identityKey">Identity strong name key pair</param>
		/// <param name="identityPubKey">Identity public key</param>
		public void InitializeEnhancedStrongNameSigning(ModuleDef module, StrongNameKey signatureKey, StrongNamePublicKey signaturePubKey, StrongNameKey identityKey, StrongNamePublicKey identityPubKey) {
			StrongNameKey = signatureKey.WithHashAlgorithm(signaturePubKey.HashAlgorithm);
			StrongNamePublicKey = identityPubKey;
			if (module.Assembly != null)
				module.Assembly.UpdateOrCreateAssemblySignatureKeyAttribute(identityPubKey, identityKey, signaturePubKey);
		}
	}

	/// <summary>
	/// Module writer base class
	/// </summary>
	public abstract class ModuleWriterBase : ILogger {
		/// <summary>Default alignment of all constants</summary>
		protected internal const uint DEFAULT_CONSTANTS_ALIGNMENT = 8;
		/// <summary>Default alignment of all method bodies</summary>
		protected const uint DEFAULT_METHODBODIES_ALIGNMENT = 4;
		/// <summary>Default alignment of all .NET resources</summary>
		protected const uint DEFAULT_NETRESOURCES_ALIGNMENT = 4;
		/// <summary>Default alignment of the .NET metadata</summary>
		protected const uint DEFAULT_METADATA_ALIGNMENT = 4;
		/// <summary>Default Win32 resources alignment</summary>
		protected internal const uint DEFAULT_WIN32_RESOURCES_ALIGNMENT = 8;
		/// <summary>Default strong name signature alignment</summary>
		protected const uint DEFAULT_STRONGNAMESIG_ALIGNMENT = 4;
		/// <summary>Default COR20 header alignment</summary>
		protected const uint DEFAULT_COR20HEADER_ALIGNMENT = 4;

		/// <summary>See <see cref="DestinationStream"/></summary>
		protected Stream destStream;
		/// <summary>See <see cref="Constants"/></summary>
		protected UniqueChunkList<ByteArrayChunk> constants;
		/// <summary>See <see cref="MethodBodies"/></summary>
		protected MethodBodyChunks methodBodies;
		/// <summary>See <see cref="NetResources"/></summary>
		protected NetResources netResources;
		/// <summary>See <see cref="Metadata"/></summary>
		protected Metadata metadata;
		/// <summary>See <see cref="Win32Resources"/></summary>
		protected Win32ResourcesChunk win32Resources;
		/// <summary>Offset where the module is written. Usually 0.</summary>
		protected long destStreamBaseOffset;
		/// <summary>Debug directory</summary>
		protected DebugDirectory debugDirectory;

		string createdPdbFileName;

		/// <summary>
		/// Strong name signature
		/// </summary>
		protected StrongNameSignature strongNameSignature;

		/// <summary>
		/// Returns the module writer options
		/// </summary>
		public abstract ModuleWriterOptionsBase TheOptions { get; }

		/// <summary>
		/// Gets the destination stream
		/// </summary>
		public Stream DestinationStream => destStream;

		/// <summary>
		/// Gets the constants
		/// </summary>
		public UniqueChunkList<ByteArrayChunk> Constants => constants;

		/// <summary>
		/// Gets the method bodies
		/// </summary>
		public MethodBodyChunks MethodBodies => methodBodies;

		/// <summary>
		/// Gets the .NET resources
		/// </summary>
		public NetResources NetResources => netResources;

		/// <summary>
		/// Gets the .NET metadata
		/// </summary>
		public Metadata Metadata => metadata;

		/// <summary>
		/// Gets the Win32 resources or <c>null</c> if there's none
		/// </summary>
		public Win32ResourcesChunk Win32Resources => win32Resources;

		/// <summary>
		/// Gets the strong name signature or <c>null</c> if there's none
		/// </summary>
		public StrongNameSignature StrongNameSignature => strongNameSignature;

		/// <summary>
		/// Gets all <see cref="PESection"/>s
		/// </summary>
		public abstract List<PESection> Sections { get; }

		/// <summary>
		/// Gets the <c>.text</c> section
		/// </summary>
		public abstract PESection TextSection { get; }

		/// <summary>
		/// Gets the <c>.rsrc</c> section or <c>null</c> if there's none
		/// </summary>
		public abstract PESection RsrcSection { get; }

		/// <summary>
		/// Gets the debug directory or <c>null</c> if there's none
		/// </summary>
		public DebugDirectory DebugDirectory => debugDirectory;

		/// <summary>
		/// <c>true</c> if <c>this</c> is a <see cref="NativeModuleWriter"/>, <c>false</c> if
		/// <c>this</c> is a <see cref="ModuleWriter"/>.
		/// </summary>
		public bool IsNativeWriter => this is NativeModuleWriter;

		/// <summary>
		/// null if we're not writing a PDB
		/// </summary>
		PdbState pdbState;

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
			if (string.IsNullOrEmpty(fileName))
				return;
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
			pdbState = TheOptions.WritePdb && Module.PdbState != null ? Module.PdbState : null;
			if (TheOptions.DelaySign) {
				Debug.Assert(TheOptions.StrongNamePublicKey != null, "Options.StrongNamePublicKey must be initialized when delay signing the assembly");
				Debug.Assert(TheOptions.StrongNameKey == null, "Options.StrongNameKey must be null when delay signing the assembly");
				TheOptions.Cor20HeaderOptions.Flags &= ~ComImageFlags.StrongNameSigned;
			}
			else if (TheOptions.StrongNameKey != null || TheOptions.StrongNamePublicKey != null)
				TheOptions.Cor20HeaderOptions.Flags |= ComImageFlags.StrongNameSigned;

			AddLegacyListener();
			destStream = dest;
			destStreamBaseOffset = destStream.Position;
			OnWriterEvent(ModuleWriterEvent.Begin);
			var imageLength = WriteImpl();
			destStream.Position = destStreamBaseOffset + imageLength;
			OnWriterEvent(ModuleWriterEvent.End);
		}

		void AddLegacyListener() {
#pragma warning disable 0618 // Type or member is obsolete
			var listener = TheOptions.Listener;
#pragma warning restore 0618 // Type or member is obsolete
			if (listener != null)
				TheOptions.WriterEvent += (s, e) => listener.OnWriterEvent(e.Writer, e.Event);
		}

		/// <summary>
		/// Returns the module that is written
		/// </summary>
		public abstract ModuleDef Module { get; }

		/// <summary>
		/// Writes the module to <see cref="destStream"/>. Event listeners and
		/// <see cref="destStream"/> have been initialized when this method is called.
		/// </summary>
		/// <returns>Number of bytes written</returns>
		protected abstract long WriteImpl();

		/// <summary>
		/// Creates the strong name signature if the module has one of the strong name flags
		/// set or wants to sign the assembly.
		/// </summary>
		protected void CreateStrongNameSignature() {
			if (TheOptions.DelaySign && TheOptions.StrongNamePublicKey != null) {
				int len = TheOptions.StrongNamePublicKey.CreatePublicKey().Length - 0x20;
				strongNameSignature = new StrongNameSignature(len > 0 ? len : 0x80);
			}
			else if (TheOptions.StrongNameKey != null)
				strongNameSignature = new StrongNameSignature(TheOptions.StrongNameKey.SignatureSize);
			else if (Module.Assembly != null && !PublicKeyBase.IsNullOrEmpty2(Module.Assembly.PublicKey)) {
				int len = Module.Assembly.PublicKey.Data.Length - 0x20;
				strongNameSignature = new StrongNameSignature(len > 0 ? len : 0x80);
			}
			else if (((TheOptions.Cor20HeaderOptions.Flags ?? Module.Cor20HeaderFlags) & ComImageFlags.StrongNameSigned) != 0)
				strongNameSignature = new StrongNameSignature(0x80);
		}

		/// <summary>
		/// Creates the .NET metadata chunks (constants, method bodies, .NET resources,
		/// the metadata, and Win32 resources)
		/// </summary>
		/// <param name="module"></param>
		protected void CreateMetadataChunks(ModuleDef module) {
			constants = new UniqueChunkList<ByteArrayChunk>();
			methodBodies = new MethodBodyChunks(TheOptions.ShareMethodBodies);
			netResources = new NetResources(DEFAULT_NETRESOURCES_ALIGNMENT);

			DebugMetadataKind debugKind;
			if (pdbState != null && (pdbState.PdbFileKind == PdbFileKind.PortablePDB || pdbState.PdbFileKind == PdbFileKind.EmbeddedPortablePDB))
				debugKind = DebugMetadataKind.Standalone;
			else
				debugKind = DebugMetadataKind.None;
			metadata = Metadata.Create(module, constants, methodBodies, netResources, TheOptions.MetadataOptions, debugKind);
			metadata.Logger = TheOptions.MetadataLogger ?? this;
			metadata.MetadataEvent += Metadata_MetadataEvent;
			metadata.ProgressUpdated += Metadata_ProgressUpdated;

			// StrongNamePublicKey is used if the user wants to override the assembly's
			// public key or when enhanced strong naming the assembly.
			var pk = TheOptions.StrongNamePublicKey;
			if (pk != null)
				metadata.AssemblyPublicKey = pk.CreatePublicKey();
			else if (TheOptions.StrongNameKey != null)
				metadata.AssemblyPublicKey = TheOptions.StrongNameKey.PublicKey;

			var w32Resources = GetWin32Resources();
			if (w32Resources != null)
				win32Resources = new Win32ResourcesChunk(w32Resources);
		}

		/// <summary>
		/// Gets the Win32 resources that should be written to the new image or <c>null</c> if none
		/// </summary>
		protected abstract Win32Resources GetWin32Resources();

		/// <summary>
		/// Calculates <see cref="RVA"/> and <see cref="FileOffset"/> of all <see cref="IChunk"/>s
		/// </summary>
		/// <param name="chunks">All chunks</param>
		/// <param name="offset">Starting file offset</param>
		/// <param name="rva">Starting RVA</param>
		/// <param name="fileAlignment">File alignment</param>
		/// <param name="sectionAlignment">Section alignment</param>
		protected void CalculateRvasAndFileOffsets(List<IChunk> chunks, FileOffset offset, RVA rva, uint fileAlignment, uint sectionAlignment) {
			foreach (var chunk in chunks) {
				chunk.SetOffset(offset, rva);
				// If it has zero size, it's not present in the file (eg. a section that wasn't needed)
				if (chunk.GetVirtualSize() != 0) {
					offset += chunk.GetFileLength();
					rva += chunk.GetVirtualSize();
					offset = offset.AlignUp(fileAlignment);
					rva = rva.AlignUp(sectionAlignment);
				}
			}
		}

		/// <summary>
		/// Writes all chunks to <paramref name="writer"/>
		/// </summary>
		/// <param name="writer">The writer</param>
		/// <param name="chunks">All chunks</param>
		/// <param name="offset">File offset of first chunk</param>
		/// <param name="fileAlignment">File alignment</param>
		protected void WriteChunks(BinaryWriter writer, List<IChunk> chunks, FileOffset offset, uint fileAlignment) {
			foreach (var chunk in chunks) {
				chunk.VerifyWriteTo(writer);
				// If it has zero size, it's not present in the file (eg. a section that wasn't needed)
				if (chunk.GetVirtualSize() != 0) {
					offset += chunk.GetFileLength();
					var newOffset = offset.AlignUp(fileAlignment);
					writer.WriteZeros((int)(newOffset - offset));
					offset = newOffset;
				}
			}
		}

		/// <summary>
		/// Strong name sign the assembly
		/// </summary>
		/// <param name="snSigOffset">Strong name signature offset</param>
		protected void StrongNameSign(long snSigOffset) {
			var snSigner = new StrongNameSigner(destStream, destStreamBaseOffset);
			snSigner.WriteSignature(TheOptions.StrongNameKey, snSigOffset);
		}

		bool CanWritePdb() => pdbState != null;

		/// <summary>
		/// Creates the debug directory if a PDB file should be written
		/// </summary>
		protected void CreateDebugDirectory() {
			if (CanWritePdb())
				debugDirectory = new DebugDirectory();
		}

		/// <summary>
		/// Write the PDB file. The caller should send the PDB events before and after calling this
		/// method.
		/// </summary>
		protected void WritePdbFile() {
			if (!CanWritePdb())
				return;
			if (debugDirectory == null)
				throw new InvalidOperationException("debugDirectory is null but WritePdb is true");

			if (pdbState == null) {
				Error("TheOptions.WritePdb is true but module has no PdbState");
				return;
			}

			try {
				switch (pdbState.PdbFileKind) {
				case PdbFileKind.WindowsPDB:
					WriteWindowsPdb(pdbState);
					break;

				case PdbFileKind.PortablePDB:
					WritePortablePdb(pdbState, false);
					break;

				case PdbFileKind.EmbeddedPortablePDB:
					WritePortablePdb(pdbState, true);
					break;

				default:
					Error("Invalid PDB file kind {0}", pdbState.PdbFileKind);
					break;
				}
			}
			catch {
				DeleteFileNoThrow(createdPdbFileName);
				throw;
			}
		}

		void WriteWindowsPdb(PdbState pdbState) {
			var symWriter = GetWindowsPdbSymbolWriter();
			if (symWriter == null) {
				Error("Could not create a PDB symbol writer. A Windows OS might be required.");
				return;
			}

			using (var pdbWriter = new WindowsPdbWriter(symWriter, pdbState, metadata)) {
				pdbWriter.Logger = TheOptions.Logger;
				pdbWriter.Write();

				var data = pdbWriter.GetDebugInfo(out var idd);
				var entry = debugDirectory.Add(data);
				entry.DebugDirectory = idd;
				entry.DebugDirectory.TimeDateStamp = GetTimeDateStamp();
			}
		}

		/// <summary>
		/// Gets the timestamp stored in the PE header
		/// </summary>
		/// <returns></returns>
		protected uint GetTimeDateStamp() {
			var td = TheOptions.PEHeadersOptions.TimeDateStamp;
			if (td.HasValue)
				return (uint)td;
			TheOptions.PEHeadersOptions.TimeDateStamp = PEHeadersOptions.CreateNewTimeDateStamp();
			return (uint)TheOptions.PEHeadersOptions.TimeDateStamp;
		}

		ISymbolWriter2 GetWindowsPdbSymbolWriter() {
			if (TheOptions.PdbStream != null) {
				return SymbolWriterCreator.Create(TheOptions.PdbStream,
							TheOptions.PdbFileName ??
							GetStreamName(TheOptions.PdbStream) ??
							GetDefaultPdbFileName());
			}

			if (!string.IsNullOrEmpty(TheOptions.PdbFileName)) {
				createdPdbFileName = TheOptions.PdbFileName;
				return SymbolWriterCreator.Create(createdPdbFileName);
			}

			createdPdbFileName = GetDefaultPdbFileName();
			if (createdPdbFileName == null)
				return null;
			return SymbolWriterCreator.Create(createdPdbFileName);
		}

		static string GetStreamName(Stream stream) => (stream as FileStream)?.Name;

		static string GetModuleName(ModuleDef module) {
			var name = module.Name ?? string.Empty;
			if (string.IsNullOrEmpty(name))
				return null;
			if (name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".netmodule", StringComparison.OrdinalIgnoreCase))
				return name;
			return name + ".pdb";
		}

		string GetDefaultPdbFileName() {
			var destFileName = GetStreamName(destStream) ?? GetModuleName(Module);
			if (string.IsNullOrEmpty(destFileName)) {
				Error("TheOptions.WritePdb is true but it's not possible to guess the default PDB file name. Set PdbFileName to the name of the PDB file.");
				return null;
			}

			return Path.ChangeExtension(destFileName, "pdb");
		}

		void WritePortablePdb(PdbState pdbState, bool isEmbeddedPortablePdb) {
			bool ownsStream = false;
			Stream pdbStream = null;
			try {
				MemoryStream embeddedMemoryStream = null;
				if (isEmbeddedPortablePdb) {
					pdbStream = embeddedMemoryStream = new MemoryStream();
					ownsStream = true;
				}
				else
					pdbStream = GetStandalonePortablePdbStream(out ownsStream);

				var pdbFilename = TheOptions.PdbFileName ?? GetStreamName(pdbStream) ?? GetDefaultPdbFileName();
				if (isEmbeddedPortablePdb)
					pdbFilename = Path.GetFileName(pdbFilename);

				uint entryPointToken;
				if (pdbState.UserEntryPoint == null)
					entryPointToken = 0;
				else
					entryPointToken = new MDToken(Table.Method, metadata.GetRid(pdbState.UserEntryPoint)).Raw;

				var pdbId = new byte[20];
				var pdbIdWriter = new BinaryWriter(new MemoryStream(pdbId));
				var pdbGuid = TheOptions.PdbGuid;
				pdbIdWriter.Write(pdbGuid.ToByteArray());
				pdbIdWriter.Write(GetTimeDateStamp());
				Debug.Assert(pdbIdWriter.BaseStream.Position == pdbId.Length);

				metadata.WritePortablePdb(pdbStream, entryPointToken, pdbId);

				const uint age = 1;
				var cvEntry = debugDirectory.Add(GetCodeViewData(pdbGuid, age, pdbFilename));
				cvEntry.DebugDirectory.TimeDateStamp = GetTimeDateStamp();
				cvEntry.DebugDirectory.MajorVersion = PortablePdbConstants.FormatVersion;
				cvEntry.DebugDirectory.MinorVersion = PortablePdbConstants.PortableCodeViewVersionMagic;
				cvEntry.DebugDirectory.Type = ImageDebugType.CodeView;

				if (isEmbeddedPortablePdb) {
					Debug.Assert(embeddedMemoryStream != null);
					var embedEntry = debugDirectory.Add(CreateEmbeddedPortablePdbBlob(embeddedMemoryStream));
					embedEntry.DebugDirectory.TimeDateStamp = 0;
					embedEntry.DebugDirectory.MajorVersion = PortablePdbConstants.FormatVersion;
					embedEntry.DebugDirectory.MinorVersion = PortablePdbConstants.EmbeddedVersion;
					embedEntry.DebugDirectory.Type = ImageDebugType.EmbeddedPortablePdb;
				}
			}
			finally {
				if (ownsStream && pdbStream != null)
					pdbStream.Dispose();
			}
		}

		static byte[] CreateEmbeddedPortablePdbBlob(MemoryStream portablePdbStream) {
			var compressedData = Compress(portablePdbStream);
			var data = new byte[4 + 4 + compressedData.Length];
			var stream = new MemoryStream(data);
			var writer = new BinaryWriter(stream);
			writer.Write(0x4244504D);//"MPDB"
			writer.Write((uint)portablePdbStream.Length);
			writer.Write(compressedData);
			Debug.Assert(stream.Position == data.Length);
			return data;
		}

		static byte[] Compress(MemoryStream sourceStream) {
			sourceStream.Position = 0;
			var destStream = new MemoryStream();
			using (var deflate = new DeflateStream(destStream, CompressionMode.Compress)) {
				var source = sourceStream.ToArray();
				deflate.Write(source, 0, source.Length);
			}
			return destStream.ToArray();
		}

		static byte[] GetCodeViewData(Guid guid, uint age, string filename) {
			var stream = new MemoryStream();
			var writer = new BinaryWriter(stream);
			writer.Write(0x53445352);
			writer.Write(guid.ToByteArray());
			writer.Write(age);
			writer.Write(Encoding.UTF8.GetBytes(filename));
			writer.Write((byte)0);
			return stream.ToArray();
		}

		Stream GetStandalonePortablePdbStream(out bool ownsStream) {
			if (TheOptions.PdbStream != null) {
				ownsStream = false;
				return TheOptions.PdbStream;
			}

			if (!string.IsNullOrEmpty(TheOptions.PdbFileName))
				createdPdbFileName = TheOptions.PdbFileName;
			else
				createdPdbFileName = GetDefaultPdbFileName();
			if (createdPdbFileName == null) {
				ownsStream = false;
				return null;
			}
			ownsStream = true;
			return File.Create(createdPdbFileName);
		}

		void Metadata_MetadataEvent(object sender, MetadataWriterEventArgs e) {
			switch (e.Event) {
			case MetadataEvent.BeginCreateTables:
				OnWriterEvent(ModuleWriterEvent.MDBeginCreateTables);
				break;

			case MetadataEvent.AllocateTypeDefRids:
				OnWriterEvent(ModuleWriterEvent.MDAllocateTypeDefRids);
				break;

			case MetadataEvent.AllocateMemberDefRids:
				OnWriterEvent(ModuleWriterEvent.MDAllocateMemberDefRids);
				break;

			case MetadataEvent.MemberDefRidsAllocated:
				OnWriterEvent(ModuleWriterEvent.MDMemberDefRidsAllocated);
				break;

			case MetadataEvent.MemberDefsInitialized:
				OnWriterEvent(ModuleWriterEvent.MDMemberDefsInitialized);
				break;

			case MetadataEvent.BeforeSortTables:
				OnWriterEvent(ModuleWriterEvent.MDBeforeSortTables);
				break;

			case MetadataEvent.MostTablesSorted:
				OnWriterEvent(ModuleWriterEvent.MDMostTablesSorted);
				break;

			case MetadataEvent.MemberDefCustomAttributesWritten:
				OnWriterEvent(ModuleWriterEvent.MDMemberDefCustomAttributesWritten);
				break;

			case MetadataEvent.BeginAddResources:
				OnWriterEvent(ModuleWriterEvent.MDBeginAddResources);
				break;

			case MetadataEvent.EndAddResources:
				OnWriterEvent(ModuleWriterEvent.MDEndAddResources);
				break;

			case MetadataEvent.BeginWriteMethodBodies:
				OnWriterEvent(ModuleWriterEvent.MDBeginWriteMethodBodies);
				break;

			case MetadataEvent.EndWriteMethodBodies:
				OnWriterEvent(ModuleWriterEvent.MDEndWriteMethodBodies);
				break;

			case MetadataEvent.OnAllTablesSorted:
				OnWriterEvent(ModuleWriterEvent.MDOnAllTablesSorted);
				break;

			case MetadataEvent.EndCreateTables:
				OnWriterEvent(ModuleWriterEvent.MDEndCreateTables);
				break;

			default:
				Debug.Fail($"Unknown MD event: {e.Event}");
				break;
			}
		}

		void Metadata_ProgressUpdated(object sender, MetadataProgressEventArgs e) =>
			RaiseProgress(ModuleWriterEvent.MDBeginCreateTables, ModuleWriterEvent.MDEndCreateTables + 1, e.Progress);

		/// <summary>
		/// Raises a writer event
		/// </summary>
		/// <param name="evt">Event</param>
		protected void OnWriterEvent(ModuleWriterEvent evt) {
			RaiseProgress(evt, 0);
			TheOptions.RaiseEvent(this, new ModuleWriterEventArgs(this, evt));
		}

		static readonly double[] eventToProgress = new double[(int)ModuleWriterEvent.End - (int)ModuleWriterEvent.Begin + 1 + 1] {
			0,					// Begin
			0.00128048488389907,// PESectionsCreated
			0.0524625293056615,	// ChunksCreated
			0.0531036610555682,	// ChunksAddedToSections
			0.0535679983835939,	// MDBeginCreateTables
			0.0547784058004697,	// MDAllocateTypeDefRids
			0.0558606342971218,	// MDAllocateMemberDefRids
			0.120553993799033,	// MDMemberDefRidsAllocated
			0.226210300699921,	// MDMemberDefsInitialized
			0.236002648477671,	// MDBeforeSortTables
			0.291089703426468,	// MDMostTablesSorted
			0.449919748849947,	// MDMemberDefCustomAttributesWritten
			0.449919985998736,	// MDBeginAddResources
			0.452716444513587,	// MDEndAddResources
			0.452716681662375,	// MDBeginWriteMethodBodies
			0.924922132195272,	// MDEndWriteMethodBodies
			0.931410404476231,	// MDOnAllTablesSorted
			0.931425463424305,	// MDEndCreateTables
			0.932072998191503,	// BeginWritePdb
			0.932175327893773,	// EndWritePdb
			0.932175446468167,	// BeginCalculateRvasAndFileOffsets
			0.954646479929387,	// EndCalculateRvasAndFileOffsets
			0.95492263969368,	// BeginWriteChunks
			0.980563166714175,	// EndWriteChunks
			0.980563403862964,	// BeginStrongNameSign
			0.980563403862964,	// EndStrongNameSign
			0.980563522437358,	// BeginWritePEChecksum
			0.999975573674777,	// EndWritePEChecksum
			1,					// End
			1,// An extra one so we can get the next base progress without checking the index
		};

		void RaiseProgress(ModuleWriterEvent evt, double subProgress) => RaiseProgress(evt, evt + 1, subProgress);

		void RaiseProgress(ModuleWriterEvent evt, ModuleWriterEvent nextEvt, double subProgress) {
			subProgress = Math.Min(1, Math.Max(0, subProgress));
			var baseProgress = eventToProgress[(int)evt];
			var nextProgress = eventToProgress[(int)nextEvt];
			var progress = baseProgress + (nextProgress - baseProgress) * subProgress;
			progress = Math.Min(1, Math.Max(0, progress));
			TheOptions.RaiseEvent(this, new ModuleWriterProgressEventArgs(this, progress));
		}

		ILogger GetLogger() => TheOptions.Logger ?? DummyLogger.ThrowModuleWriterExceptionOnErrorInstance;

		/// <inheritdoc/>
		void ILogger.Log(object sender, LoggerEvent loggerEvent, string format, params object[] args) =>
			GetLogger().Log(this, loggerEvent, format, args);

		/// <inheritdoc/>
		bool ILogger.IgnoresEvent(LoggerEvent loggerEvent) => GetLogger().IgnoresEvent(loggerEvent);

		/// <summary>
		/// Logs an error message
		/// </summary>
		/// <param name="format">Format</param>
		/// <param name="args">Format args</param>
		protected void Error(string format, params object[] args) =>
			GetLogger().Log(this, LoggerEvent.Error, format, args);

		/// <summary>
		/// Logs a warning message
		/// </summary>
		/// <param name="format">Format</param>
		/// <param name="args">Format args</param>
		protected void Warning(string format, params object[] args) =>
			GetLogger().Log(this, LoggerEvent.Warning, format, args);
	}
}
