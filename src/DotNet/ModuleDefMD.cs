// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using dnlib.PE;
using dnlib.Utils;
using dnlib.IO;
using dnlib.DotNet.MD;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb;
using dnlib.W32Resources;

using DNW = dnlib.DotNet.Writer;
using dnlib.DotNet.Pdb.Symbols;

namespace dnlib.DotNet {
	/// <summary>
	/// Created from a row in the Module table
	/// </summary>
	public sealed class ModuleDefMD : ModuleDefMD2, IInstructionOperandResolver {
		/// <summary>The file that contains all .NET metadata</summary>
		MetaData metaData;
		IMethodDecrypter methodDecrypter;
		IStringDecrypter stringDecrypter;

		RandomRidList moduleRidList;

		SimpleLazyList<ModuleDefMD2> listModuleDefMD;
		SimpleLazyList<TypeRefMD> listTypeRefMD;
		SimpleLazyList<TypeDefMD> listTypeDefMD;
		SimpleLazyList<FieldDefMD> listFieldDefMD;
		SimpleLazyList<MethodDefMD> listMethodDefMD;
		SimpleLazyList<ParamDefMD> listParamDefMD;
		SimpleLazyList2<InterfaceImplMD> listInterfaceImplMD;
		SimpleLazyList2<MemberRefMD> listMemberRefMD;
		SimpleLazyList<ConstantMD> listConstantMD;
		SimpleLazyList<DeclSecurityMD> listDeclSecurityMD;
		SimpleLazyList<ClassLayoutMD> listClassLayoutMD;
		SimpleLazyList2<StandAloneSigMD> listStandAloneSigMD;
		SimpleLazyList<EventDefMD> listEventDefMD;
		SimpleLazyList<PropertyDefMD> listPropertyDefMD;
		SimpleLazyList<ModuleRefMD> listModuleRefMD;
		SimpleLazyList2<TypeSpecMD> listTypeSpecMD;
		SimpleLazyList<ImplMapMD> listImplMapMD;
		SimpleLazyList<AssemblyDefMD> listAssemblyDefMD;
		SimpleLazyList<AssemblyRefMD> listAssemblyRefMD;
		SimpleLazyList<FileDefMD> listFileDefMD;
		SimpleLazyList<ExportedTypeMD> listExportedTypeMD;
		SimpleLazyList<ManifestResourceMD> listManifestResourceMD;
		SimpleLazyList<GenericParamMD> listGenericParamMD;
		SimpleLazyList2<MethodSpecMD> listMethodSpecMD;
		SimpleLazyList2<GenericParamConstraintMD> listGenericParamConstraintMD;

		/// <summary>
		/// Gets/sets the method decrypter
		/// </summary>
		public IMethodDecrypter MethodDecrypter {
			get { return methodDecrypter; }
			set { methodDecrypter = value; }
		}

		/// <summary>
		/// Gets/sets the string decrypter
		/// </summary>
		public IStringDecrypter StringDecrypter {
			get { return stringDecrypter; }
			set { stringDecrypter = value; }
		}

		/// <summary>
		/// Returns the .NET metadata interface
		/// </summary>
		public IMetaData MetaData {
			get { return metaData; }
		}

		/// <summary>
		/// Returns the #~ or #- tables stream
		/// </summary>
		public TablesStream TablesStream {
			get { return metaData.TablesStream; }
		}

		/// <summary>
		/// Returns the #Strings stream
		/// </summary>
		public StringsStream StringsStream {
			get { return metaData.StringsStream; }
		}

		/// <summary>
		/// Returns the #Blob stream
		/// </summary>
		public BlobStream BlobStream {
			get { return metaData.BlobStream; }
		}

		/// <summary>
		/// Returns the #GUID stream
		/// </summary>
		public GuidStream GuidStream {
			get { return metaData.GuidStream; }
		}

		/// <summary>
		/// Returns the #US stream
		/// </summary>
		public USStream USStream {
			get { return metaData.USStream; }
		}

		/// <inheritdoc/>
		protected override void InitializeTypes() {
			var list = MetaData.GetNonNestedClassRidList();
			var tmp = new LazyList<TypeDef>((int)list.Length, this, list, (list2, index) => ResolveTypeDef(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref types, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeExportedTypes() {
			var list = MetaData.GetExportedTypeRidList();
			var tmp = new LazyList<ExportedType>((int)list.Length, list, (list2, i) => ResolveExportedType(((RidList)list2)[i]));
			Interlocked.CompareExchange(ref exportedTypes, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeResources() {
			var table = TablesStream.ManifestResourceTable;
			var tmp = new ResourceCollection((int)table.Rows, null, (ctx, i) => CreateResource(i + 1));
			Interlocked.CompareExchange(ref resources, tmp, null);
		}

		/// <inheritdoc/>
		protected override Win32Resources GetWin32Resources_NoLock() {
			return metaData.PEImage.Win32Resources;
		}

		/// <inheritdoc/>
		protected override VTableFixups GetVTableFixups_NoLock() {
			var vtableFixupsInfo = metaData.ImageCor20Header.VTableFixups;
			if (vtableFixupsInfo.VirtualAddress == 0 || vtableFixupsInfo.Size == 0)
				return null;
			return new VTableFixups(this);
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance from a file
		/// </summary>
		/// <param name="fileName">File name of an existing .NET module/assembly</param>
		/// <param name="context">Module context or <c>null</c></param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance</returns>
		public static ModuleDefMD Load(string fileName, ModuleContext context) {
			return Load(fileName, new ModuleCreationOptions(context));
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance from a file
		/// </summary>
		/// <param name="fileName">File name of an existing .NET module/assembly</param>
		/// <param name="options">Module creation options or <c>null</c></param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance</returns>
		public static ModuleDefMD Load(string fileName, ModuleCreationOptions options = null) {
			return Load(MetaDataCreator.Load(fileName), options);
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance from a byte[]
		/// </summary>
		/// <param name="data">Contents of a .NET module/assembly</param>
		/// <param name="context">Module context or <c>null</c></param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance</returns>
		public static ModuleDefMD Load(byte[] data, ModuleContext context) {
			return Load(data, new ModuleCreationOptions(context));
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance from a byte[]
		/// </summary>
		/// <param name="data">Contents of a .NET module/assembly</param>
		/// <param name="options">Module creation options or <c>null</c></param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance</returns>
		public static ModuleDefMD Load(byte[] data, ModuleCreationOptions options = null) {
			return Load(MetaDataCreator.Load(data), options);
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance from a reflection module
		/// </summary>
		/// <param name="mod">An existing reflection module</param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance</returns>
		public static ModuleDefMD Load(System.Reflection.Module mod) {
			return Load(mod, (ModuleCreationOptions)null, GetImageLayout(mod));
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance from a reflection module
		/// </summary>
		/// <param name="mod">An existing reflection module</param>
		/// <param name="context">Module context or <c>null</c></param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance</returns>
		public static ModuleDefMD Load(System.Reflection.Module mod, ModuleContext context) {
			return Load(mod, new ModuleCreationOptions(context), GetImageLayout(mod));
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance from a reflection module
		/// </summary>
		/// <param name="mod">An existing reflection module</param>
		/// <param name="options">Module creation options or <c>null</c></param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance</returns>
		public static ModuleDefMD Load(System.Reflection.Module mod, ModuleCreationOptions options) {
			return Load(mod, options, GetImageLayout(mod));
		}

		static ImageLayout GetImageLayout(System.Reflection.Module mod) {
			var fqn = mod.FullyQualifiedName;
			if (fqn.Length > 0 && fqn[0] == '<' && fqn[fqn.Length - 1] == '>')
				return ImageLayout.File;
			return ImageLayout.Memory;
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance from a reflection module
		/// </summary>
		/// <param name="mod">An existing reflection module</param>
		/// <param name="context">Module context or <c>null</c></param>
		/// <param name="imageLayout">Image layout of the module in memory</param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance</returns>
		public static ModuleDefMD Load(System.Reflection.Module mod, ModuleContext context, ImageLayout imageLayout) {
			return Load(mod, new ModuleCreationOptions(context), imageLayout);
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance from a reflection module
		/// </summary>
		/// <param name="mod">An existing reflection module</param>
		/// <param name="options">Module creation options or <c>null</c></param>
		/// <param name="imageLayout">Image layout of the module in memory</param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance</returns>
		public static ModuleDefMD Load(System.Reflection.Module mod, ModuleCreationOptions options, ImageLayout imageLayout) {
			IntPtr addr = Marshal.GetHINSTANCE(mod);
			if (addr == new IntPtr(-1))
				throw new InvalidOperationException(string.Format("Module {0} has no HINSTANCE", mod));
			return Load(addr, options, imageLayout);
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance from a memory location
		/// </summary>
		/// <param name="addr">Address of a .NET module/assembly</param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance</returns>
		public static ModuleDefMD Load(IntPtr addr) {
			return Load(MetaDataCreator.Load(addr), (ModuleCreationOptions)null);
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance from a memory location
		/// </summary>
		/// <param name="addr">Address of a .NET module/assembly</param>
		/// <param name="context">Module context or <c>null</c></param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance</returns>
		public static ModuleDefMD Load(IntPtr addr, ModuleContext context) {
			return Load(MetaDataCreator.Load(addr), new ModuleCreationOptions(context));
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance from a memory location
		/// </summary>
		/// <param name="addr">Address of a .NET module/assembly</param>
		/// <param name="options">Module creation options or <c>null</c></param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance</returns>
		public static ModuleDefMD Load(IntPtr addr, ModuleCreationOptions options) {
			return Load(MetaDataCreator.Load(addr), options);
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance
		/// </summary>
		/// <param name="peImage">PE image</param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance</returns>
		public static ModuleDefMD Load(IPEImage peImage) {
			return Load(MetaDataCreator.Load(peImage), (ModuleCreationOptions)null);
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance
		/// </summary>
		/// <param name="peImage">PE image</param>
		/// <param name="context">Module context or <c>null</c></param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance</returns>
		public static ModuleDefMD Load(IPEImage peImage, ModuleContext context) {
			return Load(MetaDataCreator.Load(peImage), new ModuleCreationOptions(context));
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance
		/// </summary>
		/// <param name="peImage">PE image</param>
		/// <param name="options">Module creation options or <c>null</c></param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance</returns>
		public static ModuleDefMD Load(IPEImage peImage, ModuleCreationOptions options) {
			return Load(MetaDataCreator.Load(peImage), options);
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance from a memory location
		/// </summary>
		/// <param name="addr">Address of a .NET module/assembly</param>
		/// <param name="context">Module context or <c>null</c></param>
		/// <param name="imageLayout">Image layout of the file in memory</param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance</returns>
		public static ModuleDefMD Load(IntPtr addr, ModuleContext context, ImageLayout imageLayout) {
			return Load(MetaDataCreator.Load(addr, imageLayout), new ModuleCreationOptions(context));
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance from a memory location
		/// </summary>
		/// <param name="addr">Address of a .NET module/assembly</param>
		/// <param name="options">Module creation options or <c>null</c></param>
		/// <param name="imageLayout">Image layout of the file in memory</param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance</returns>
		public static ModuleDefMD Load(IntPtr addr, ModuleCreationOptions options, ImageLayout imageLayout) {
			return Load(MetaDataCreator.Load(addr, imageLayout), options);
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance from a stream
		/// </summary>
		/// <remarks>This will read all bytes from the stream and call <see cref="Load(byte[],ModuleCreationOptions)"/>.
		/// It's better to use one of the other Load() methods.</remarks>
		/// <param name="stream">The stream (owned by caller)</param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="stream"/> is <c>null</c></exception>
		public static ModuleDefMD Load(Stream stream) {
			return Load(stream, (ModuleCreationOptions)null);
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance from a stream
		/// </summary>
		/// <remarks>This will read all bytes from the stream and call <see cref="Load(byte[],ModuleContext)"/>.
		/// It's better to use one of the other Load() methods.</remarks>
		/// <param name="stream">The stream (owned by caller)</param>
		/// <param name="context">Module context or <c>null</c></param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="stream"/> is <c>null</c></exception>
		public static ModuleDefMD Load(Stream stream, ModuleContext context) {
			return Load(stream, new ModuleCreationOptions(context));
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance from a stream
		/// </summary>
		/// <remarks>This will read all bytes from the stream and call <see cref="Load(byte[],ModuleContext)"/>.
		/// It's better to use one of the other Load() methods.</remarks>
		/// <param name="stream">The stream (owned by caller)</param>
		/// <param name="options">Module creation options or <c>null</c></param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="stream"/> is <c>null</c></exception>
		public static ModuleDefMD Load(Stream stream, ModuleCreationOptions options) {
			if (stream == null)
				throw new ArgumentNullException("stream");
			if (stream.Length > int.MaxValue)
				throw new ArgumentException("Stream is too big");
			var data = new byte[(int)stream.Length];
			stream.Position = 0;
			if (stream.Read(data, 0, data.Length) != data.Length)
				throw new IOException("Could not read all bytes from the stream");
			return Load(data, options);
		}

		/// <summary>
		/// Creates a <see cref="ModuleDefMD"/> instance from a <see cref="MetaData"/>
		/// </summary>
		/// <param name="metaData">The metadata</param>
		/// <param name="options">Module creation options or <c>null</c></param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance that now owns <paramref name="metaData"/></returns>
		internal static ModuleDefMD Load(MetaData metaData, ModuleCreationOptions options) {
			return new ModuleDefMD(metaData, options);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="metaData">The metadata</param>
		/// <param name="options">Module creation options or <c>null</c></param>
		/// <exception cref="ArgumentNullException">If <paramref name="metaData"/> is <c>null</c></exception>
		ModuleDefMD(MetaData metaData, ModuleCreationOptions options)
			: base(null, 1) {
#if DEBUG
			if (metaData == null)
				throw new ArgumentNullException("metaData");
#endif
			if (options == null)
				options = ModuleCreationOptions.Default;
			this.metaData = metaData;
			this.context = options.Context;
			Initialize();
			InitializeFromRawRow();
			location = metaData.PEImage.FileName ?? string.Empty;

			this.Kind = GetKind();
			this.Characteristics = MetaData.PEImage.ImageNTHeaders.FileHeader.Characteristics;
			this.DllCharacteristics = MetaData.PEImage.ImageNTHeaders.OptionalHeader.DllCharacteristics;
			this.RuntimeVersion = MetaData.VersionString;
			this.Machine = MetaData.PEImage.ImageNTHeaders.FileHeader.Machine;
			this.Cor20HeaderFlags = MetaData.ImageCor20Header.Flags;
			this.Cor20HeaderRuntimeVersion = (uint)(MetaData.ImageCor20Header.MajorRuntimeVersion << 16) | MetaData.ImageCor20Header.MinorRuntimeVersion;
			this.TablesHeaderVersion = MetaData.TablesStream.Version;
			corLibTypes = new CorLibTypes(this, options.CorLibAssemblyRef ?? FindCorLibAssemblyRef() ?? CreateDefaultCorLibAssemblyRef());
			InitializePdb(options);
		}

		void InitializePdb(ModuleCreationOptions options) {
			if (options == null)
				return;
			LoadPdb(CreateSymbolReader(options));
		}

		SymbolReader CreateSymbolReader(ModuleCreationOptions options) {
			if (options.CreateSymbolReader != null) {
				var symReader = options.CreateSymbolReader(this);
				if (symReader != null)
					return symReader;
			}

			if (options.PdbFileOrData != null) {
				var pdbFileName = options.PdbFileOrData as string;
				if (!string.IsNullOrEmpty(pdbFileName)) {
					var symReader = SymbolReaderCreator.Create(options.PdbImplementation, metaData, pdbFileName);
					if (symReader != null)
						return symReader;
				}

				var pdbData = options.PdbFileOrData as byte[];
				if (pdbData != null)
					return SymbolReaderCreator.Create(options.PdbImplementation, metaData, pdbData);

				var pdbStream = options.PdbFileOrData as IImageStream;
				if (pdbStream != null)
					return SymbolReaderCreator.Create(options.PdbImplementation, metaData, pdbStream);
			}

			if (options.TryToLoadPdbFromDisk) {
				if (!string.IsNullOrEmpty(location))
					return SymbolReaderCreator.CreateFromAssemblyFile(options.PdbImplementation, metaData, location);
				else
					return SymbolReaderCreator.Create(options.PdbImplementation, metaData);
			}

			return null;
		}

		/// <summary>
		/// Loads symbols using <paramref name="symbolReader"/>
		/// </summary>
		/// <param name="symbolReader">PDB symbol reader</param>
		public void LoadPdb(SymbolReader symbolReader) {
			if (symbolReader == null)
				return;
			if (pdbState != null)
				throw new InvalidOperationException("PDB file has already been initialized");

			var orig = Interlocked.CompareExchange(ref pdbState, new PdbState(symbolReader, this), null);
			if (orig != null)
				throw new InvalidOperationException("PDB file has already been initialized");
		}

		/// <summary>
		/// Loads symbols from a PDB file
		/// </summary>
		/// <param name="pdbFileName">PDB file name</param>
		public void LoadPdb(string pdbFileName) {
			LoadPdb(PdbImplType.Default, pdbFileName);
		}

		/// <summary>
		/// Loads symbols from a PDB file
		/// </summary>
		/// <param name="pdbImpl">PDB implementation to use</param>
		/// <param name="pdbFileName">PDB file name</param>
		public void LoadPdb(PdbImplType pdbImpl, string pdbFileName) {
			LoadPdb(SymbolReaderCreator.Create(pdbImpl, metaData, pdbFileName));
		}

		/// <summary>
		/// Loads symbols from a byte array
		/// </summary>
		/// <param name="pdbData">PDB data</param>
		public void LoadPdb(byte[] pdbData) {
			LoadPdb(PdbImplType.Default, pdbData);
		}

		/// <summary>
		/// Loads symbols from a byte array
		/// </summary>
		/// <param name="pdbImpl">PDB implementation to use</param>
		/// <param name="pdbData">PDB data</param>
		public void LoadPdb(PdbImplType pdbImpl, byte[] pdbData) {
			LoadPdb(SymbolReaderCreator.Create(pdbImpl, metaData, pdbData));
		}

		/// <summary>
		/// Loads symbols from a stream
		/// </summary>
		/// <param name="pdbStream">PDB file stream which is now owned by us</param>
		public void LoadPdb(IImageStream pdbStream) {
			LoadPdb(PdbImplType.Default, pdbStream);
		}

		/// <summary>
		/// Loads symbols from a stream
		/// </summary>
		/// <param name="pdbImpl">PDB implementation to use</param>
		/// <param name="pdbStream">PDB file stream which is now owned by us</param>
		public void LoadPdb(PdbImplType pdbImpl, IImageStream pdbStream) {
			LoadPdb(SymbolReaderCreator.Create(pdbImpl, metaData, pdbStream));
		}

		/// <summary>
		/// Loads symbols if a PDB file is available
		/// </summary>
		public void LoadPdb() {
			LoadPdb(PdbImplType.Default);
		}

		/// <summary>
		/// Loads symbols if a PDB file is available
		/// </summary>
		/// <param name="pdbImpl">PDB implementation to use</param>
		public void LoadPdb(PdbImplType pdbImpl) {
			var loc = location;
			if (string.IsNullOrEmpty(loc))
				return;
			LoadPdb(SymbolReaderCreator.Create(pdbImpl, metaData, loc));
		}

		internal void InitializeCustomDebugInfos(MDToken token, GenericParamContext gpContext, IList<PdbCustomDebugInfo> result) {
			var ps = pdbState;
			if (ps == null)
				return;
			ps.InitializeCustomDebugInfos(token, gpContext, result);
		}

		ModuleKind GetKind() {
			if (TablesStream.AssemblyTable.Rows < 1)
				return ModuleKind.NetModule;

			var peImage = MetaData.PEImage;
			if ((peImage.ImageNTHeaders.FileHeader.Characteristics & Characteristics.Dll) != 0)
				return ModuleKind.Dll;

			switch (peImage.ImageNTHeaders.OptionalHeader.Subsystem) {
			default:
			case Subsystem.WindowsGui:
				return ModuleKind.Windows;

			case Subsystem.WindowsCui:
				return ModuleKind.Console;
			}
		}

		void Initialize() {
			var ts = metaData.TablesStream;

			listModuleDefMD = new SimpleLazyList<ModuleDefMD2>(ts.ModuleTable.Rows, rid2 => rid2 == 1 ? this : new ModuleDefMD2(this, rid2));
			listTypeRefMD = new SimpleLazyList<TypeRefMD>(ts.TypeRefTable.Rows, rid2 => new TypeRefMD(this, rid2));
			listTypeDefMD = new SimpleLazyList<TypeDefMD>(ts.TypeDefTable.Rows, rid2 => new TypeDefMD(this, rid2));
			listFieldDefMD = new SimpleLazyList<FieldDefMD>(ts.FieldTable.Rows, rid2 => new FieldDefMD(this, rid2));
			listMethodDefMD = new SimpleLazyList<MethodDefMD>(ts.MethodTable.Rows, rid2 => new MethodDefMD(this, rid2));
			listParamDefMD = new SimpleLazyList<ParamDefMD>(ts.ParamTable.Rows, rid2 => new ParamDefMD(this, rid2));
			listInterfaceImplMD = new SimpleLazyList2<InterfaceImplMD>(ts.InterfaceImplTable.Rows, (rid2, gpContext) => new InterfaceImplMD(this, rid2, gpContext));
			listMemberRefMD = new SimpleLazyList2<MemberRefMD>(ts.MemberRefTable.Rows, (rid2, gpContext) => new MemberRefMD(this, rid2, gpContext));
			listConstantMD = new SimpleLazyList<ConstantMD>(ts.ConstantTable.Rows, rid2 => new ConstantMD(this, rid2));
			listDeclSecurityMD = new SimpleLazyList<DeclSecurityMD>(ts.DeclSecurityTable.Rows, rid2 => new DeclSecurityMD(this, rid2));
			listClassLayoutMD = new SimpleLazyList<ClassLayoutMD>(ts.ClassLayoutTable.Rows, rid2 => new ClassLayoutMD(this, rid2));
			listStandAloneSigMD = new SimpleLazyList2<StandAloneSigMD>(ts.StandAloneSigTable.Rows, (rid2, gpContext) => new StandAloneSigMD(this, rid2, gpContext));
			listEventDefMD = new SimpleLazyList<EventDefMD>(ts.EventTable.Rows, rid2 => new EventDefMD(this, rid2));
			listPropertyDefMD = new SimpleLazyList<PropertyDefMD>(ts.PropertyTable.Rows, rid2 => new PropertyDefMD(this, rid2));
			listModuleRefMD = new SimpleLazyList<ModuleRefMD>(ts.ModuleRefTable.Rows, rid2 => new ModuleRefMD(this, rid2));
			listTypeSpecMD = new SimpleLazyList2<TypeSpecMD>(ts.TypeSpecTable.Rows, (rid2, gpContext) => new TypeSpecMD(this, rid2, gpContext));
			listImplMapMD = new SimpleLazyList<ImplMapMD>(ts.ImplMapTable.Rows, rid2 => new ImplMapMD(this, rid2));
			listAssemblyDefMD = new SimpleLazyList<AssemblyDefMD>(ts.AssemblyTable.Rows, rid2 => new AssemblyDefMD(this, rid2));
			listFileDefMD = new SimpleLazyList<FileDefMD>(ts.FileTable.Rows, rid2 => new FileDefMD(this, rid2));
			listAssemblyRefMD = new SimpleLazyList<AssemblyRefMD>(ts.AssemblyRefTable.Rows, rid2 => new AssemblyRefMD(this, rid2));
			listExportedTypeMD = new SimpleLazyList<ExportedTypeMD>(ts.ExportedTypeTable.Rows, rid2 => new ExportedTypeMD(this, rid2));
			listManifestResourceMD = new SimpleLazyList<ManifestResourceMD>(ts.ManifestResourceTable.Rows, rid2 => new ManifestResourceMD(this, rid2));
			listGenericParamMD = new SimpleLazyList<GenericParamMD>(ts.GenericParamTable.Rows, rid2 => new GenericParamMD(this, rid2));
			listMethodSpecMD = new SimpleLazyList2<MethodSpecMD>(ts.MethodSpecTable.Rows, (rid2, gpContext) => new MethodSpecMD(this, rid2, gpContext));
			listGenericParamConstraintMD = new SimpleLazyList2<GenericParamConstraintMD>(ts.GenericParamConstraintTable.Rows, (rid2, gpContext) => new GenericParamConstraintMD(this, rid2, gpContext));

			for (int i = 0; i < 64; i++) {
				var tbl = TablesStream.Get((Table)i);
				lastUsedRids[i] = tbl == null ? 0 : (int)tbl.Rows;
			}
		}

		static readonly Dictionary<string, int> preferredCorLibs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) {
			// .NET Framework
			{ "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", 100 },
			{ "mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", 90 },
			{ "mscorlib, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", 60 },
			{ "mscorlib, Version=1.0.3300.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", 50 },

			// Silverlight
			{ "mscorlib, Version=5.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e", 80 },
			{ "mscorlib, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e", 70 },

			// Zune
			{ "mscorlib, Version=3.5.0.0, Culture=neutral, PublicKeyToken=e92a8b81eba7ceb7", 60 },

			// Compact Framework
			{ "mscorlib, Version=3.5.0.0, Culture=neutral, PublicKeyToken=969db8053d3322ac", 60 },
			{ "mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=969db8053d3322ac", 50 },
		};

		/// <summary>
		/// Finds a mscorlib <see cref="AssemblyRef"/>
		/// </summary>
		/// <returns>An existing <see cref="AssemblyRef"/> instance or <c>null</c> if it wasn't found</returns>
		AssemblyRef FindCorLibAssemblyRef() {
			var numAsmRefs = TablesStream.AssemblyRefTable.Rows;
			AssemblyRef corLibAsmRef = null;

			int currentPriority = int.MinValue;
			for (uint i = 1; i <= numAsmRefs; i++) {
				var asmRef = ResolveAssemblyRef(i);
				int priority;
				if (!preferredCorLibs.TryGetValue(asmRef.FullName, out priority))
					continue;
				if (priority > currentPriority) {
					currentPriority = priority;
					corLibAsmRef = asmRef;
				}
			}
			if (corLibAsmRef != null)
				return corLibAsmRef;

			for (uint i = 1; i <= numAsmRefs; i++) {
				var asmRef = ResolveAssemblyRef(i);
				if (!UTF8String.ToSystemStringOrEmpty(asmRef.Name).Equals("mscorlib", StringComparison.OrdinalIgnoreCase))
					continue;
				if (IsGreaterAssemblyRefVersion(corLibAsmRef, asmRef))
					corLibAsmRef = asmRef;
			}
			if (corLibAsmRef != null)
				return corLibAsmRef;

			// If we've loaded mscorlib itself, it won't have any AssemblyRefs to itself.
			var asm = Assembly;
			if (asm != null && (asm.IsCorLib() || Find("System.Object", false) != null)) {
				IsCoreLibraryModule = true;
				return UpdateRowId(new AssemblyRefUser(asm));
			}

			return corLibAsmRef;
		}

		/// <summary>
		/// Called when no corlib assembly reference was found
		/// </summary>
		/// <returns></returns>
		AssemblyRef CreateDefaultCorLibAssemblyRef() {
			var asmRef = GetAlternativeCorLibReference();
			if (asmRef != null)
				return UpdateRowId(asmRef);

			if (this.IsClr40)
				return UpdateRowId(AssemblyRefUser.CreateMscorlibReferenceCLR40());
			if (this.IsClr20)
				return UpdateRowId(AssemblyRefUser.CreateMscorlibReferenceCLR20());
			if (this.IsClr11)
				return UpdateRowId(AssemblyRefUser.CreateMscorlibReferenceCLR11());
			if (this.IsClr10)
				return UpdateRowId(AssemblyRefUser.CreateMscorlibReferenceCLR10());
			return UpdateRowId(AssemblyRefUser.CreateMscorlibReferenceCLR40());
		}

		AssemblyRef GetAlternativeCorLibReference() {
			foreach (var asmRef in GetAssemblyRefs()) {
				if (IsAssemblyRef(asmRef, systemRuntimeName, contractsPublicKeyToken))
					return asmRef;
			}
			foreach (var asmRef in GetAssemblyRefs()) {
				if (IsAssemblyRef(asmRef, corefxName, contractsPublicKeyToken))
					return asmRef;
			}
			return null;
		}

		static bool IsAssemblyRef(AssemblyRef asmRef, UTF8String name, PublicKeyToken token) {
			if (asmRef.Name != name)
				return false;
			var pkot = asmRef.PublicKeyOrToken;
			if (pkot == null)
				return false;
			return token.Equals(pkot.Token);
		}
		static readonly UTF8String systemRuntimeName = new UTF8String("System.Runtime");
		static readonly UTF8String corefxName = new UTF8String("corefx");
		static readonly PublicKeyToken contractsPublicKeyToken = new PublicKeyToken("b03f5f7f11d50a3a");

		/// <inheritdoc/>
		protected override void Dispose(bool disposing) {
			// Call base first since it will dispose of all the resources, which will
			// eventually use metaData that we will dispose
			base.Dispose(disposing);
			if (disposing) {
				var md = metaData;
				if (md != null)
					md.Dispose();
				metaData = null;
			}
		}

		/// <summary>
		/// Resolves a token
		/// </summary>
		/// <param name="token">The metadata token</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A <see cref="IMDTokenProvider"/> or <c>null</c> if <paramref name="token"/> is invalid</returns>
		public override IMDTokenProvider ResolveToken(uint token, GenericParamContext gpContext) {
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.Module:			return ResolveModule(rid);
			case Table.TypeRef:			return ResolveTypeRef(rid);
			case Table.TypeDef:			return ResolveTypeDef(rid);
			case Table.Field:			return ResolveField(rid);
			case Table.Method:			return ResolveMethod(rid);
			case Table.Param:			return ResolveParam(rid);
			case Table.InterfaceImpl:	return ResolveInterfaceImpl(rid, gpContext);
			case Table.MemberRef:		return ResolveMemberRef(rid, gpContext);
			case Table.Constant:		return ResolveConstant(rid);
			case Table.DeclSecurity:	return ResolveDeclSecurity(rid);
			case Table.ClassLayout:		return ResolveClassLayout(rid);
			case Table.StandAloneSig:	return ResolveStandAloneSig(rid, gpContext);
			case Table.Event:			return ResolveEvent(rid);
			case Table.Property:		return ResolveProperty(rid);
			case Table.ModuleRef:		return ResolveModuleRef(rid);
			case Table.TypeSpec:		return ResolveTypeSpec(rid, gpContext);
			case Table.ImplMap:			return ResolveImplMap(rid);
			case Table.Assembly:		return ResolveAssembly(rid);
			case Table.AssemblyRef:		return ResolveAssemblyRef(rid);
			case Table.File:			return ResolveFile(rid);
			case Table.ExportedType:	return ResolveExportedType(rid);
			case Table.ManifestResource:return ResolveManifestResource(rid);
			case Table.GenericParam:	return ResolveGenericParam(rid);
			case Table.MethodSpec:		return ResolveMethodSpec(rid, gpContext);
			case Table.GenericParamConstraint: return ResolveGenericParamConstraint(rid, gpContext);
			}
			return null;
		}

		/// <summary>
		/// Resolves a <see cref="ModuleDef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="ModuleDef"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public ModuleDef ResolveModule(uint rid) {
			return listModuleDefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="TypeRef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="TypeRef"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public TypeRef ResolveTypeRef(uint rid) {
			return listTypeRefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="TypeDef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="TypeDef"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public TypeDef ResolveTypeDef(uint rid) {
			return listTypeDefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="FieldDef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="FieldDef"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public FieldDef ResolveField(uint rid) {
			return listFieldDefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="MethodDef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="MethodDef"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public MethodDef ResolveMethod(uint rid) {
			return listMethodDefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="ParamDef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="ParamDef"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public ParamDef ResolveParam(uint rid) {
			return listParamDefMD[rid - 1];
		}

		/// <summary>
		/// Resolves an <see cref="InterfaceImpl"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="InterfaceImpl"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public InterfaceImpl ResolveInterfaceImpl(uint rid) {
			return listInterfaceImplMD[rid - 1, new GenericParamContext()];
		}

		/// <summary>
		/// Resolves an <see cref="InterfaceImpl"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A <see cref="InterfaceImpl"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public InterfaceImpl ResolveInterfaceImpl(uint rid, GenericParamContext gpContext) {
			return listInterfaceImplMD[rid - 1, gpContext];
		}

		/// <summary>
		/// Resolves a <see cref="MemberRef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="MemberRef"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public MemberRef ResolveMemberRef(uint rid) {
			return listMemberRefMD[rid - 1, new GenericParamContext()];
		}

		/// <summary>
		/// Resolves a <see cref="MemberRef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A <see cref="MemberRef"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public MemberRef ResolveMemberRef(uint rid, GenericParamContext gpContext) {
			return listMemberRefMD[rid - 1, gpContext];
		}

		/// <summary>
		/// Resolves a <see cref="Constant"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="Constant"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public Constant ResolveConstant(uint rid) {
			return listConstantMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="DeclSecurity"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="DeclSecurity"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public DeclSecurity ResolveDeclSecurity(uint rid) {
			return listDeclSecurityMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="ClassLayout"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="ClassLayout"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public ClassLayout ResolveClassLayout(uint rid) {
			return listClassLayoutMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="StandAloneSig"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="StandAloneSig"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public StandAloneSig ResolveStandAloneSig(uint rid) {
			return listStandAloneSigMD[rid - 1, new GenericParamContext()];
		}

		/// <summary>
		/// Resolves a <see cref="StandAloneSig"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A <see cref="StandAloneSig"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public StandAloneSig ResolveStandAloneSig(uint rid, GenericParamContext gpContext) {
			return listStandAloneSigMD[rid - 1, gpContext];
		}

		/// <summary>
		/// Resolves an <see cref="EventDef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="EventDef"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public EventDef ResolveEvent(uint rid) {
			return listEventDefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="PropertyDef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="PropertyDef"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public PropertyDef ResolveProperty(uint rid) {
			return listPropertyDefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="ModuleRef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="ModuleRef"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public ModuleRef ResolveModuleRef(uint rid) {
			return listModuleRefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="TypeSpec"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="TypeSpec"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public TypeSpec ResolveTypeSpec(uint rid) {
			return listTypeSpecMD[rid - 1, new GenericParamContext()];
		}

		/// <summary>
		/// Resolves a <see cref="TypeSpec"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A <see cref="TypeSpec"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public TypeSpec ResolveTypeSpec(uint rid, GenericParamContext gpContext) {
			return listTypeSpecMD[rid - 1, gpContext];
		}

		/// <summary>
		/// Resolves an <see cref="ImplMap"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="ImplMap"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public ImplMap ResolveImplMap(uint rid) {
			return listImplMapMD[rid - 1];
		}

		/// <summary>
		/// Resolves an <see cref="AssemblyDef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="AssemblyDef"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public AssemblyDef ResolveAssembly(uint rid) {
			return listAssemblyDefMD[rid - 1];
		}

		/// <summary>
		/// Resolves an <see cref="AssemblyRef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="AssemblyRef"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public AssemblyRef ResolveAssemblyRef(uint rid) {
			return listAssemblyRefMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="FileDef"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="FileDef"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public FileDef ResolveFile(uint rid) {
			return listFileDefMD[rid - 1];
		}

		/// <summary>
		/// Resolves an <see cref="ExportedType"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="ExportedType"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public ExportedType ResolveExportedType(uint rid) {
			return listExportedTypeMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="ManifestResource"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="ManifestResource"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public ManifestResource ResolveManifestResource(uint rid) {
			return listManifestResourceMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="GenericParam"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="GenericParam"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public GenericParam ResolveGenericParam(uint rid) {
			return listGenericParamMD[rid - 1];
		}

		/// <summary>
		/// Resolves a <see cref="MethodSpec"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="MethodSpec"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public MethodSpec ResolveMethodSpec(uint rid) {
			return listMethodSpecMD[rid - 1, new GenericParamContext()];
		}

		/// <summary>
		/// Resolves a <see cref="MethodSpec"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A <see cref="MethodSpec"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public MethodSpec ResolveMethodSpec(uint rid, GenericParamContext gpContext) {
			return listMethodSpecMD[rid - 1, gpContext];
		}

		/// <summary>
		/// Resolves a <see cref="GenericParamConstraint"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <returns>A <see cref="GenericParamConstraint"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public GenericParamConstraint ResolveGenericParamConstraint(uint rid) {
			return listGenericParamConstraintMD[rid - 1, new GenericParamContext()];
		}

		/// <summary>
		/// Resolves a <see cref="GenericParamConstraint"/>
		/// </summary>
		/// <param name="rid">The row ID</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A <see cref="GenericParamConstraint"/> instance or <c>null</c> if <paramref name="rid"/> is invalid</returns>
		public GenericParamConstraint ResolveGenericParamConstraint(uint rid, GenericParamContext gpContext) {
			return listGenericParamConstraintMD[rid - 1, gpContext];
		}

		/// <summary>
		/// Resolves a <see cref="ITypeDefOrRef"/>
		/// </summary>
		/// <param name="codedToken">A <c>TypeDefOrRef</c> coded token</param>
		/// <returns>A <see cref="ITypeDefOrRef"/> or <c>null</c> if <paramref name="codedToken"/> is invalid</returns>
		public ITypeDefOrRef ResolveTypeDefOrRef(uint codedToken) {
			return ResolveTypeDefOrRef(codedToken, new GenericParamContext());
		}

		/// <summary>
		/// Resolves a <see cref="ITypeDefOrRef"/>
		/// </summary>
		/// <param name="codedToken">A <c>TypeDefOrRef</c> coded token</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A <see cref="ITypeDefOrRef"/> or <c>null</c> if <paramref name="codedToken"/> is invalid</returns>
		public ITypeDefOrRef ResolveTypeDefOrRef(uint codedToken, GenericParamContext gpContext) {
			uint token;
			if (!CodedToken.TypeDefOrRef.Decode(codedToken, out token))
				return null;
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.TypeDef:		return ResolveTypeDef(rid);
			case Table.TypeRef:		return ResolveTypeRef(rid);
			case Table.TypeSpec:	return ResolveTypeSpec(rid, gpContext);
			}
			return null;
		}

		/// <summary>
		/// Resolves a <see cref="IHasConstant"/>
		/// </summary>
		/// <param name="codedToken">A <c>HasConstant</c> coded token</param>
		/// <returns>A <see cref="IHasConstant"/> or <c>null</c> if <paramref name="codedToken"/> is invalid</returns>
		public IHasConstant ResolveHasConstant(uint codedToken) {
			uint token;
			if (!CodedToken.HasConstant.Decode(codedToken, out token))
				return null;
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.Field:	return ResolveField(rid);
			case Table.Param:	return ResolveParam(rid);
			case Table.Property:return ResolveProperty(rid);
			}
			return null;
		}

		/// <summary>
		/// Resolves a <see cref="IHasCustomAttribute"/>
		/// </summary>
		/// <param name="codedToken">A <c>HasCustomAttribute</c> coded token</param>
		/// <returns>A <see cref="IHasCustomAttribute"/> or <c>null</c> if <paramref name="codedToken"/> is invalid</returns>
		public IHasCustomAttribute ResolveHasCustomAttribute(uint codedToken) {
			return ResolveHasCustomAttribute(codedToken, new GenericParamContext());
		}

		/// <summary>
		/// Resolves a <see cref="IHasCustomAttribute"/>
		/// </summary>
		/// <param name="codedToken">A <c>HasCustomAttribute</c> coded token</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A <see cref="IHasCustomAttribute"/> or <c>null</c> if <paramref name="codedToken"/> is invalid</returns>
		public IHasCustomAttribute ResolveHasCustomAttribute(uint codedToken, GenericParamContext gpContext) {
			uint token;
			if (!CodedToken.HasCustomAttribute.Decode(codedToken, out token))
				return null;
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.Method:		return ResolveMethod(rid);
			case Table.Field:		return ResolveField(rid);
			case Table.TypeRef:		return ResolveTypeRef(rid);
			case Table.TypeDef:		return ResolveTypeDef(rid);
			case Table.Param:		return ResolveParam(rid);
			case Table.InterfaceImpl: return ResolveInterfaceImpl(rid, gpContext);
			case Table.MemberRef:	return ResolveMemberRef(rid, gpContext);
			case Table.Module:		return ResolveModule(rid);
			case Table.DeclSecurity:return ResolveDeclSecurity(rid);
			case Table.Property:	return ResolveProperty(rid);
			case Table.Event:		return ResolveEvent(rid);
			case Table.StandAloneSig: return ResolveStandAloneSig(rid, gpContext);
			case Table.ModuleRef:	return ResolveModuleRef(rid);
			case Table.TypeSpec:	return ResolveTypeSpec(rid, gpContext);
			case Table.Assembly:	return ResolveAssembly(rid);
			case Table.AssemblyRef:	return ResolveAssemblyRef(rid);
			case Table.File:		return ResolveFile(rid);
			case Table.ExportedType:return ResolveExportedType(rid);
			case Table.ManifestResource: return ResolveManifestResource(rid);
			case Table.GenericParam:return ResolveGenericParam(rid);
			case Table.MethodSpec:	return ResolveMethodSpec(rid, gpContext);
			case Table.GenericParamConstraint: return ResolveGenericParamConstraint(rid, gpContext);
			}
			return null;
		}

		/// <summary>
		/// Resolves a <see cref="IHasFieldMarshal"/>
		/// </summary>
		/// <param name="codedToken">A <c>HasFieldMarshal</c> coded token</param>
		/// <returns>A <see cref="IHasFieldMarshal"/> or <c>null</c> if <paramref name="codedToken"/> is invalid</returns>
		public IHasFieldMarshal ResolveHasFieldMarshal(uint codedToken) {
			uint token;
			if (!CodedToken.HasFieldMarshal.Decode(codedToken, out token))
				return null;
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.Field:	return ResolveField(rid);
			case Table.Param:	return ResolveParam(rid);
			}
			return null;
		}

		/// <summary>
		/// Resolves a <see cref="IHasDeclSecurity"/>
		/// </summary>
		/// <param name="codedToken">A <c>HasDeclSecurity</c> coded token</param>
		/// <returns>A <see cref="IHasDeclSecurity"/> or <c>null</c> if <paramref name="codedToken"/> is invalid</returns>
		public IHasDeclSecurity ResolveHasDeclSecurity(uint codedToken) {
			uint token;
			if (!CodedToken.HasDeclSecurity.Decode(codedToken, out token))
				return null;
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.TypeDef:		return ResolveTypeDef(rid);
			case Table.Method:		return ResolveMethod(rid);
			case Table.Assembly:	return ResolveAssembly(rid);
			}
			return null;
		}

		/// <summary>
		/// Resolves a <see cref="IMemberRefParent"/>
		/// </summary>
		/// <param name="codedToken">A <c>MemberRefParent</c> coded token</param>
		/// <returns>A <see cref="IMemberRefParent"/> or <c>null</c> if <paramref name="codedToken"/> is invalid</returns>
		public IMemberRefParent ResolveMemberRefParent(uint codedToken) {
			return ResolveMemberRefParent(codedToken, new GenericParamContext());
		}

		/// <summary>
		/// Resolves a <see cref="IMemberRefParent"/>
		/// </summary>
		/// <param name="codedToken">A <c>MemberRefParent</c> coded token</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A <see cref="IMemberRefParent"/> or <c>null</c> if <paramref name="codedToken"/> is invalid</returns>
		public IMemberRefParent ResolveMemberRefParent(uint codedToken, GenericParamContext gpContext) {
			uint token;
			if (!CodedToken.MemberRefParent.Decode(codedToken, out token))
				return null;
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.TypeDef:		return ResolveTypeDef(rid);
			case Table.TypeRef:		return ResolveTypeRef(rid);
			case Table.ModuleRef:	return ResolveModuleRef(rid);
			case Table.Method:		return ResolveMethod(rid);
			case Table.TypeSpec:	return ResolveTypeSpec(rid, gpContext);
			}
			return null;
		}

		/// <summary>
		/// Resolves a <see cref="IHasSemantic"/>
		/// </summary>
		/// <param name="codedToken">A <c>HasSemantic</c> coded token</param>
		/// <returns>A <see cref="IHasSemantic"/> or <c>null</c> if <paramref name="codedToken"/> is invalid</returns>
		public IHasSemantic ResolveHasSemantic(uint codedToken) {
			uint token;
			if (!CodedToken.HasSemantic.Decode(codedToken, out token))
				return null;
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.Event:		return ResolveEvent(rid);
			case Table.Property:	return ResolveProperty(rid);
			}
			return null;
		}

		/// <summary>
		/// Resolves a <see cref="IMethodDefOrRef"/>
		/// </summary>
		/// <param name="codedToken">A <c>MethodDefOrRef</c> coded token</param>
		/// <returns>A <see cref="IMethodDefOrRef"/> or <c>null</c> if <paramref name="codedToken"/> is invalid</returns>
		public IMethodDefOrRef ResolveMethodDefOrRef(uint codedToken) {
			return ResolveMethodDefOrRef(codedToken, new GenericParamContext());
		}

		/// <summary>
		/// Resolves a <see cref="IMethodDefOrRef"/>
		/// </summary>
		/// <param name="codedToken">A <c>MethodDefOrRef</c> coded token</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A <see cref="IMethodDefOrRef"/> or <c>null</c> if <paramref name="codedToken"/> is invalid</returns>
		public IMethodDefOrRef ResolveMethodDefOrRef(uint codedToken, GenericParamContext gpContext) {
			uint token;
			if (!CodedToken.MethodDefOrRef.Decode(codedToken, out token))
				return null;
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.Method:		return ResolveMethod(rid);
			case Table.MemberRef:	return ResolveMemberRef(rid, gpContext);
			}
			return null;
		}

		/// <summary>
		/// Resolves a <see cref="IMemberForwarded"/>
		/// </summary>
		/// <param name="codedToken">A <c>MemberForwarded</c> coded token</param>
		/// <returns>A <see cref="IMemberForwarded"/> or <c>null</c> if <paramref name="codedToken"/> is invalid</returns>
		public IMemberForwarded ResolveMemberForwarded(uint codedToken) {
			uint token;
			if (!CodedToken.MemberForwarded.Decode(codedToken, out token))
				return null;
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.Field:	return ResolveField(rid);
			case Table.Method:	return ResolveMethod(rid);
			}
			return null;
		}

		/// <summary>
		/// Resolves an <see cref="IImplementation"/>
		/// </summary>
		/// <param name="codedToken">An <c>Implementation</c> coded token</param>
		/// <returns>A <see cref="IImplementation"/> or <c>null</c> if <paramref name="codedToken"/> is invalid</returns>
		public IImplementation ResolveImplementation(uint codedToken) {
			uint token;
			if (!CodedToken.Implementation.Decode(codedToken, out token))
				return null;
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.File:			return ResolveFile(rid);
			case Table.AssemblyRef:		return ResolveAssemblyRef(rid);
			case Table.ExportedType:	return ResolveExportedType(rid);
			}
			return null;
		}

		/// <summary>
		/// Resolves a <see cref="ICustomAttributeType"/>
		/// </summary>
		/// <param name="codedToken">A <c>CustomAttributeType</c> coded token</param>
		/// <returns>A <see cref="ICustomAttributeType"/> or <c>null</c> if <paramref name="codedToken"/> is invalid</returns>
		public ICustomAttributeType ResolveCustomAttributeType(uint codedToken) {
			return ResolveCustomAttributeType(codedToken, new GenericParamContext());
		}

		/// <summary>
		/// Resolves a <see cref="ICustomAttributeType"/>
		/// </summary>
		/// <param name="codedToken">A <c>CustomAttributeType</c> coded token</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A <see cref="ICustomAttributeType"/> or <c>null</c> if <paramref name="codedToken"/> is invalid</returns>
		public ICustomAttributeType ResolveCustomAttributeType(uint codedToken, GenericParamContext gpContext) {
			uint token;
			if (!CodedToken.CustomAttributeType.Decode(codedToken, out token))
				return null;
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.Method:		return ResolveMethod(rid);
			case Table.MemberRef:	return ResolveMemberRef(rid, gpContext);
			}
			return null;
		}

		/// <summary>
		/// Resolves a <see cref="IResolutionScope"/>
		/// </summary>
		/// <param name="codedToken">A <c>ResolutionScope</c> coded token</param>
		/// <returns>A <see cref="IResolutionScope"/> or <c>null</c> if <paramref name="codedToken"/> is invalid</returns>
		public IResolutionScope ResolveResolutionScope(uint codedToken) {
			uint token;
			if (!CodedToken.ResolutionScope.Decode(codedToken, out token))
				return null;
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.Module:		return ResolveModule(rid);
			case Table.ModuleRef:	return ResolveModuleRef(rid);
			case Table.AssemblyRef:	return ResolveAssemblyRef(rid);
			case Table.TypeRef:		return ResolveTypeRef(rid);
			}
			return null;
		}

		/// <summary>
		/// Resolves a <see cref="ITypeOrMethodDef"/>
		/// </summary>
		/// <param name="codedToken">A <c>TypeOrMethodDef</c>> coded token</param>
		/// <returns>A <see cref="ITypeOrMethodDef"/> or <c>null</c> if <paramref name="codedToken"/> is invalid</returns>
		public ITypeOrMethodDef ResolveTypeOrMethodDef(uint codedToken) {
			uint token;
			if (!CodedToken.TypeOrMethodDef.Decode(codedToken, out token))
				return null;
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.TypeDef:	return ResolveTypeDef(rid);
			case Table.Method:	return ResolveMethod(rid);
			}
			return null;
		}

		/// <summary>
		/// Reads a signature from the #Blob stream
		/// </summary>
		/// <param name="sig">#Blob stream offset of signature</param>
		/// <returns>A new <see cref="CallingConventionSig"/> instance or <c>null</c> if
		/// <paramref name="sig"/> is invalid.</returns>
		public CallingConventionSig ReadSignature(uint sig) {
			return SignatureReader.ReadSig(this, sig, new GenericParamContext());
		}

		/// <summary>
		/// Reads a signature from the #Blob stream
		/// </summary>
		/// <param name="sig">#Blob stream offset of signature</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A new <see cref="CallingConventionSig"/> instance or <c>null</c> if
		/// <paramref name="sig"/> is invalid.</returns>
		public CallingConventionSig ReadSignature(uint sig, GenericParamContext gpContext) {
			return SignatureReader.ReadSig(this, sig, gpContext);
		}

		/// <summary>
		/// Reads a type signature from the #Blob stream
		/// </summary>
		/// <param name="sig">#Blob stream offset of signature</param>
		/// <returns>A new <see cref="TypeSig"/> instance or <c>null</c> if
		/// <paramref name="sig"/> is invalid.</returns>
		public TypeSig ReadTypeSignature(uint sig) {
			return SignatureReader.ReadTypeSig(this, sig, new GenericParamContext());
		}

		/// <summary>
		/// Reads a type signature from the #Blob stream
		/// </summary>
		/// <param name="sig">#Blob stream offset of signature</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A new <see cref="TypeSig"/> instance or <c>null</c> if
		/// <paramref name="sig"/> is invalid.</returns>
		public TypeSig ReadTypeSignature(uint sig, GenericParamContext gpContext) {
			return SignatureReader.ReadTypeSig(this, sig, gpContext);
		}

		/// <summary>
		/// Reads a type signature from the #Blob stream
		/// </summary>
		/// <param name="sig">#Blob stream offset of signature</param>
		/// <param name="extraData">If there's any extra data after the signature, it's saved
		/// here, else this will be <c>null</c></param>
		/// <returns>A new <see cref="TypeSig"/> instance or <c>null</c> if
		/// <paramref name="sig"/> is invalid.</returns>
		public TypeSig ReadTypeSignature(uint sig, out byte[] extraData) {
			return SignatureReader.ReadTypeSig(this, sig, new GenericParamContext(), out extraData);
		}

		/// <summary>
		/// Reads a type signature from the #Blob stream
		/// </summary>
		/// <param name="sig">#Blob stream offset of signature</param>
		/// <param name="extraData">If there's any extra data after the signature, it's saved
		/// here, else this will be <c>null</c></param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A new <see cref="TypeSig"/> instance or <c>null</c> if
		/// <paramref name="sig"/> is invalid.</returns>
		public TypeSig ReadTypeSignature(uint sig, GenericParamContext gpContext, out byte[] extraData) {
			return SignatureReader.ReadTypeSig(this, sig, gpContext, out extraData);
		}

		/// <summary>
		/// Reads a <see cref="MarshalType"/> from the blob
		/// </summary>
		/// <param name="table">Table of owner</param>
		/// <param name="rid">Row ID of owner</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A new <see cref="MarshalType"/> instance or <c>null</c> if there's no field
		/// marshal for this owner.</returns>
		internal MarshalType ReadMarshalType(Table table, uint rid, GenericParamContext gpContext) {
			var row = TablesStream.ReadFieldMarshalRow(MetaData.GetFieldMarshalRid(table, rid));
			if (row == null)
				return null;
			return MarshalBlobReader.Read(this, row.NativeType, gpContext);
		}

		/// <summary>
		/// Reads a CIL method body
		/// </summary>
		/// <param name="parameters">Method parameters</param>
		/// <param name="rva">RVA</param>
		/// <returns>A new <see cref="CilBody"/> instance. It's empty if RVA is invalid (eg. 0 or
		/// it doesn't point to a CIL method body)</returns>
		public CilBody ReadCilBody(IList<Parameter> parameters, RVA rva) {
			return ReadCilBody(parameters, rva, new GenericParamContext());
		}

		/// <summary>
		/// Reads a CIL method body
		/// </summary>
		/// <param name="parameters">Method parameters</param>
		/// <param name="rva">RVA</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A new <see cref="CilBody"/> instance. It's empty if RVA is invalid (eg. 0 or
		/// it doesn't point to a CIL method body)</returns>
		public CilBody ReadCilBody(IList<Parameter> parameters, RVA rva, GenericParamContext gpContext) {
			if (rva == 0)
				return new CilBody();

			// Create a full stream so position will be the real position in the file. This
			// is important when reading exception handlers since those must be 4-byte aligned.
			// If we create a partial stream starting from rva, then position will be 0 and always
			// 4-byte aligned. All fat method bodies should be 4-byte aligned, but the CLR doesn't
			// seem to verify it. We must parse the method exactly the way the CLR parses it.
			using (var reader = metaData.PEImage.CreateFullStream()) {
				reader.Position = (long)metaData.PEImage.ToFileOffset(rva);
				return MethodBodyReader.CreateCilBody(this, reader, parameters, gpContext);
			}
		}

		/// <summary>
		/// Returns the owner type of a field
		/// </summary>
		/// <param name="field">The field</param>
		/// <returns>The owner type or <c>null</c> if none</returns>
		internal TypeDef GetOwnerType(FieldDefMD field) {
			return ResolveTypeDef(MetaData.GetOwnerTypeOfField(field.OrigRid));
		}

		/// <summary>
		/// Returns the owner type of a method
		/// </summary>
		/// <param name="method">The method</param>
		/// <returns>The owner type or <c>null</c> if none</returns>
		internal TypeDef GetOwnerType(MethodDefMD method) {
			return ResolveTypeDef(MetaData.GetOwnerTypeOfMethod(method.OrigRid));
		}

		/// <summary>
		/// Returns the owner type of an event
		/// </summary>
		/// <param name="evt">The event</param>
		/// <returns>The owner type or <c>null</c> if none</returns>
		internal TypeDef GetOwnerType(EventDefMD evt) {
			return ResolveTypeDef(MetaData.GetOwnerTypeOfEvent(evt.OrigRid));
		}

		/// <summary>
		/// Returns the owner type of a property
		/// </summary>
		/// <param name="property">The property</param>
		/// <returns>The owner type or <c>null</c> if none</returns>
		internal TypeDef GetOwnerType(PropertyDefMD property) {
			return ResolveTypeDef(MetaData.GetOwnerTypeOfProperty(property.OrigRid));
		}

		/// <summary>
		/// Returns the owner type/method of a generic param
		/// </summary>
		/// <param name="gp">The generic param</param>
		/// <returns>The owner type/method or <c>null</c> if none</returns>
		internal ITypeOrMethodDef GetOwner(GenericParamMD gp) {
			return ResolveTypeOrMethodDef(MetaData.GetOwnerOfGenericParam(gp.OrigRid));
		}

		/// <summary>
		/// Returns the owner generic param of a generic param constraint
		/// </summary>
		/// <param name="gpc">The generic param constraint</param>
		/// <returns>The owner generic param or <c>null</c> if none</returns>
		internal GenericParam GetOwner(GenericParamConstraintMD gpc) {
			return ResolveGenericParam(MetaData.GetOwnerOfGenericParamConstraint(gpc.OrigRid));
		}

		/// <summary>
		/// Returns the owner method of a param
		/// </summary>
		/// <param name="pd">The param</param>
		/// <returns>The owner method or <c>null</c> if none</returns>
		internal MethodDef GetOwner(ParamDefMD pd) {
			return ResolveMethod(MetaData.GetOwnerOfParam(pd.OrigRid));
		}

		/// <summary>
		/// Reads a module
		/// </summary>
		/// <param name="fileRid">File rid</param>
		/// <param name="owner">The assembly owning the module we should read</param>
		/// <returns>A new <see cref="ModuleDefMD"/> instance or <c>null</c> if <paramref name="fileRid"/>
		/// is invalid or if it's not a .NET module.</returns>
		internal ModuleDefMD ReadModule(uint fileRid, AssemblyDef owner) {
			var fileDef = ResolveFile(fileRid);
			if (fileDef == null)
				return null;
			if (!fileDef.ContainsMetaData)
				return null;
			var fileName = GetValidFilename(GetBaseDirectoryOfImage(), UTF8String.ToSystemString(fileDef.Name));
			if (fileName == null)
				return null;
			ModuleDefMD module;
			try {
				module = ModuleDefMD.Load(fileName);
			}
			catch {
				module = null;
			}
			if (module != null) {
				// share context
				module.context = context;

				var asm = module.Assembly;
				if (asm != null && asm != owner)
					asm.Modules.Remove(module);
			}
			return module;
		}

		/// <summary>
		/// Gets a list of all <c>File</c> rids that are .NET modules. Call <see cref="ReadModule(uint,AssemblyDef)"/>
		/// to read one of these modules.
		/// </summary>
		/// <returns>A new <see cref="RidList"/> instance</returns>
		internal RidList GetModuleRidList() {
			if (moduleRidList == null)
				InitializeModuleList();
			return moduleRidList;
		}

		void InitializeModuleList() {
			if (moduleRidList != null)
				return;
			uint rows = TablesStream.FileTable.Rows;
			var newModuleRidList = new RandomRidList((int)rows);

			var baseDir = GetBaseDirectoryOfImage();
			for (uint fileRid = 1; fileRid <= rows; fileRid++) {
				var fileDef = ResolveFile(fileRid);
				if (fileDef == null)
					continue;	// Should never happen
				if (!fileDef.ContainsMetaData)
					continue;
				var pathName = GetValidFilename(baseDir, UTF8String.ToSystemString(fileDef.Name));
				if (pathName != null)
					newModuleRidList.Add(fileRid);
			}
			Interlocked.CompareExchange(ref moduleRidList, newModuleRidList, null);
		}

		/// <summary>
		/// Concatenates the inputs and returns the result if it's a valid path
		/// </summary>
		/// <param name="baseDir">Base dir</param>
		/// <param name="name">File name</param>
		/// <returns>Full path to the file or <c>null</c> if one of the inputs is invalid</returns>
		static string GetValidFilename(string baseDir, string name) {
			if (baseDir == null)
				return null;

			string pathName;
			try {
				if (name.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
					return null;
				pathName = Path.Combine(baseDir, name);
				if (pathName != Path.GetFullPath(pathName))
					return null;
				if (!File.Exists(pathName))
					return null;
			}
			catch {
				return null;
			}

			return pathName;
		}

		/// <summary>
		/// Gets the base directory where this .NET module is located on disk
		/// </summary>
		/// <returns>Base directory or <c>null</c> if unknown or if an error occurred</returns>
		string GetBaseDirectoryOfImage() {
			var imageFileName = Location;
			if (string.IsNullOrEmpty(imageFileName))
				return null;
			try {
				return Path.GetDirectoryName(imageFileName);
			}
			catch (IOException) {
			}
			catch (ArgumentException) {
			}
			return null;
		}

		/// <summary>
		/// Creates a <see cref="Resource"/> instance
		/// </summary>
		/// <param name="rid"><c>ManifestResource</c> rid</param>
		/// <returns>A new <see cref="Resource"/> instance</returns>
		Resource CreateResource(uint rid) {
			var row = TablesStream.ReadManifestResourceRow(rid);
			if (row == null)
				return new EmbeddedResource(UTF8String.Empty, MemoryImageStream.CreateEmpty(), 0) { Rid = rid };

			MDToken token;
			if (!CodedToken.Implementation.Decode(row.Implementation, out token))
				return new EmbeddedResource(UTF8String.Empty, MemoryImageStream.CreateEmpty(), 0) { Rid = rid };

			var mr = ResolveManifestResource(rid);
			if (mr == null)
				return new EmbeddedResource(UTF8String.Empty, MemoryImageStream.CreateEmpty(), 0) { Rid = rid };

			if (token.Rid == 0)
				return new EmbeddedResource(mr.Name, CreateResourceStream(mr.Offset), mr.Flags) { Rid = rid, Offset = mr.Offset };

			var file = mr.Implementation as FileDef;
			if (file != null)
				return new LinkedResource(mr.Name, file, mr.Flags) { Rid = rid, Offset = mr.Offset };

			var asmRef = mr.Implementation as AssemblyRef;
			if (asmRef != null)
				return new AssemblyLinkedResource(mr.Name, asmRef, mr.Flags) { Rid = rid, Offset = mr.Offset };

			return new EmbeddedResource(mr.Name, MemoryImageStream.CreateEmpty(), mr.Flags) { Rid = rid, Offset = mr.Offset };
		}

		/// <summary>
		/// Creates a resource stream that can access part of the resource section of this module
		/// </summary>
		/// <param name="offset">Offset of resource relative to the .NET resources section</param>
		/// <returns>A stream the size of the resource</returns>
		[HandleProcessCorruptedStateExceptions, SecurityCritical]	// Req'd on .NET 4.0
		IImageStream CreateResourceStream(uint offset) {
			IImageStream fs = null, imageStream = null;
			try {
				var peImage = metaData.PEImage;
				var cor20Header = metaData.ImageCor20Header;
				var resources = cor20Header.Resources;
				if (resources.VirtualAddress == 0 || resources.Size == 0)
					return MemoryImageStream.CreateEmpty();
				fs = peImage.CreateFullStream();

				var resourceOffset = (long)peImage.ToFileOffset(resources.VirtualAddress);
				if (resourceOffset <= 0 || resourceOffset + offset < resourceOffset)
					return MemoryImageStream.CreateEmpty();
				if (offset + 3 <= offset || offset + 3 >= resources.Size)
					return MemoryImageStream.CreateEmpty();
				if (resourceOffset + offset + 3 < resourceOffset || resourceOffset + offset + 3 >= fs.Length)
					return MemoryImageStream.CreateEmpty();
				fs.Position = resourceOffset + offset;
				uint length = fs.ReadUInt32();	// Could throw
				if (length == 0 || fs.Position + length - 1 < fs.Position || fs.Position + length - 1 >= fs.Length)
					return MemoryImageStream.CreateEmpty();
				if (fs.Position - resourceOffset + length - 1 >= resources.Size)
					return MemoryImageStream.CreateEmpty();

				imageStream = peImage.CreateStream((FileOffset)fs.Position, length);
				if (peImage.MayHaveInvalidAddresses) {
					for (; imageStream.Position < imageStream.Length; imageStream.Position += 0x1000)
						imageStream.ReadByte();	// Could throw
					imageStream.Position = imageStream.Length - 1;	// length is never 0 if we're here
					imageStream.ReadByte();	// Could throw
					imageStream.Position = 0;
				}
			}
			catch (AccessViolationException) {
				if (imageStream != null)
					imageStream.Dispose();
				return MemoryImageStream.CreateEmpty();
			}
			finally {
				if (fs != null)
					fs.Dispose();
			}
			return imageStream;
		}

		/// <summary>
		/// Reads a <see cref="CustomAttribute"/>
		/// </summary>
		/// <param name="caRid">Custom attribute rid</param>
		/// <returns>A new <see cref="CustomAttribute"/> instance or <c>null</c> if
		/// <paramref name="caRid"/> is invalid</returns>
		public CustomAttribute ReadCustomAttribute(uint caRid) {
			return ReadCustomAttribute(caRid, new GenericParamContext());
		}

		/// <summary>
		/// Reads a <see cref="CustomAttribute"/>
		/// </summary>
		/// <param name="caRid">Custom attribute rid</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A new <see cref="CustomAttribute"/> instance or <c>null</c> if
		/// <paramref name="caRid"/> is invalid</returns>
		public CustomAttribute ReadCustomAttribute(uint caRid, GenericParamContext gpContext) {
			var caRow = TablesStream.ReadCustomAttributeRow(caRid);
			if (caRow == null)
				return null;
			return CustomAttributeReader.Read(this, ResolveCustomAttributeType(caRow.Type, gpContext), caRow.Value, gpContext);
		}

		/// <summary>
		/// Reads data somewhere in the address space of the image
		/// </summary>
		/// <param name="rva">RVA of data</param>
		/// <param name="size">Size of data</param>
		/// <returns>All the data or <c>null</c> if <paramref name="rva"/> or <paramref name="size"/>
		/// is invalid</returns>
		public byte[] ReadDataAt(RVA rva, int size) {
			if (size < 0)
				return null;
			var peImage = MetaData.PEImage;
			using (var reader = peImage.CreateStream(rva, size)) {
				if (reader.Length < size)
					return null;
				return reader.ReadBytes(size);
			}
		}

		/// <summary>
		/// Gets the native entry point or 0 if none
		/// </summary>
		public RVA GetNativeEntryPoint() {
			var cor20Header = MetaData.ImageCor20Header;
			if ((cor20Header.Flags & ComImageFlags.NativeEntryPoint) == 0)
				return 0;
			return (RVA)cor20Header.EntryPointToken_or_RVA;
		}

		/// <summary>
		/// Gets the managed entry point (a Method or a File) or null if none
		/// </summary>
		public IManagedEntryPoint GetManagedEntryPoint() {
			var cor20Header = MetaData.ImageCor20Header;
			if ((cor20Header.Flags & ComImageFlags.NativeEntryPoint) != 0)
				return null;
			return ResolveToken(cor20Header.EntryPointToken_or_RVA) as IManagedEntryPoint;
		}

		/// <summary>
		/// Reads a new <see cref="FieldDefMD"/> instance. This one is not cached.
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>A new <see cref="FieldDefMD"/> instance</returns>
		internal FieldDefMD ReadField(uint rid) {
			return new FieldDefMD(this, rid);
		}

		/// <summary>
		/// Reads a new <see cref="MethodDefMD"/> instance. This one is not cached.
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>A new <see cref="MethodDefMD"/> instance</returns>
		internal MethodDefMD ReadMethod(uint rid) {
			return new MethodDefMD(this, rid);
		}

		/// <summary>
		/// Reads a new <see cref="EventDefMD"/> instance. This one is not cached.
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>A new <see cref="EventDefMD"/> instance</returns>
		internal EventDefMD ReadEvent(uint rid) {
			return new EventDefMD(this, rid);
		}

		/// <summary>
		/// Reads a new <see cref="PropertyDefMD"/> instance. This one is not cached.
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>A new <see cref="PropertyDefMD"/> instance</returns>
		internal PropertyDefMD ReadProperty(uint rid) {
			return new PropertyDefMD(this, rid);
		}

		/// <summary>
		/// Reads a new <see cref="ParamDefMD"/> instance. This one is not cached.
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>A new <see cref="ParamDefMD"/> instance</returns>
		internal ParamDefMD ReadParam(uint rid) {
			return new ParamDefMD(this, rid);
		}

		/// <summary>
		/// Reads a new <see cref="GenericParamMD"/> instance. This one is not cached.
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>A new <see cref="GenericParamMD"/> instance</returns>
		internal GenericParamMD ReadGenericParam(uint rid) {
			return new GenericParamMD(this, rid);
		}

		/// <summary>
		/// Reads a new <see cref="GenericParamConstraintMD"/> instance. This one is not cached.
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <returns>A new <see cref="GenericParamConstraintMD"/> instance</returns>
		internal GenericParamConstraintMD ReadGenericParamConstraint(uint rid) {
			return new GenericParamConstraintMD(this, rid, new GenericParamContext());
		}

		/// <summary>
		/// Reads a new <see cref="GenericParamConstraintMD"/> instance. This one is not cached.
		/// </summary>
		/// <param name="rid">Row ID</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A new <see cref="GenericParamConstraintMD"/> instance</returns>
		internal GenericParamConstraintMD ReadGenericParamConstraint(uint rid, GenericParamContext gpContext) {
			return new GenericParamConstraintMD(this, rid, gpContext);
		}

		/// <summary>
		/// Reads a method body
		/// </summary>
		/// <param name="method">Method</param>
		/// <param name="rva">Method RVA</param>
		/// <param name="implAttrs">Method impl attrs</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A <see cref="MethodBody"/> or <c>null</c> if none</returns>
		internal MethodBody ReadMethodBody(MethodDefMD method, RVA rva, MethodImplAttributes implAttrs, GenericParamContext gpContext) {
			MethodBody mb;
			var mDec = methodDecrypter;
			if (mDec != null && mDec.GetMethodBody(method.OrigRid, rva, method.Parameters, gpContext, out mb)) {
				var cilBody = mb as CilBody;
				if (cilBody != null)
					return InitializeBodyFromPdb(method, cilBody);
				return mb;
			}

			if (rva == 0)
				return null;
			var codeType = implAttrs & MethodImplAttributes.CodeTypeMask;
			if (codeType == MethodImplAttributes.IL)
				return InitializeBodyFromPdb(method, ReadCilBody(method.Parameters, rva, gpContext));
			if (codeType == MethodImplAttributes.Native)
				return new NativeMethodBody(rva);
			return null;
		}

		/// <summary>
		/// Updates <paramref name="body"/> with the PDB info (if any)
		/// </summary>
		/// <param name="method">Owner method</param>
		/// <param name="body">Method body</param>
		/// <returns>Returns originak <paramref name="body"/> value</returns>
		CilBody InitializeBodyFromPdb(MethodDefMD method, CilBody body) {
			var ps = pdbState;
			if (ps != null)
				ps.InitializeMethodBody(this, method, body);
			return body;
		}

		internal void InitializeCustomDebugInfos(MethodDefMD method, CilBody body, IList<PdbCustomDebugInfo> customDebugInfos) {
			if (body == null)
				return;

			var ps = pdbState;
			if (ps != null)
				ps.InitializeCustomDebugInfos(method, body, customDebugInfos);
		}

		/// <summary>
		/// Reads a string from the #US heap
		/// </summary>
		/// <param name="token">String token</param>
		/// <returns>A non-null string</returns>
		public string ReadUserString(uint token) {
			var sDec = stringDecrypter;
			if (sDec != null) {
				var s = sDec.ReadUserString(token);
				if (s != null)
					return s;
			}
			return USStream.ReadNoNull(token & 0x00FFFFFF);
		}

		/// <summary>
		/// Writes the mixed-mode module to a file on disk. If the file exists, it will be overwritten.
		/// </summary>
		/// <param name="filename">Filename</param>
		public void NativeWrite(string filename) {
			NativeWrite(filename, null);
		}

		/// <summary>
		/// Writes the mixed-mode module to a file on disk. If the file exists, it will be overwritten.
		/// </summary>
		/// <param name="filename">Filename</param>
		/// <param name="options">Writer options</param>
		public void NativeWrite(string filename, DNW.NativeModuleWriterOptions options) {
			var writer = new DNW.NativeModuleWriter(this, options ?? new DNW.NativeModuleWriterOptions(this));
			writer.Write(filename);
		}

		/// <summary>
		/// Writes the mixed-mode module to a stream.
		/// </summary>
		/// <param name="dest">Destination stream</param>
		public void NativeWrite(Stream dest) {
			NativeWrite(dest, null);
		}

		/// <summary>
		/// Writes the mixed-mode module to a stream.
		/// </summary>
		/// <param name="dest">Destination stream</param>
		/// <param name="options">Writer options</param>
		public void NativeWrite(Stream dest, DNW.NativeModuleWriterOptions options) {
			var writer = new DNW.NativeModuleWriter(this, options ?? new DNW.NativeModuleWriterOptions(this));
			writer.Write(dest);
		}

		/// <summary>
		/// Reads data from the #Blob. The following columns are returned:
		/// Field.Signature
		/// Method.Signature
		/// MemberRef.Signature
		/// Constant.Value
		/// CustomAttribute.Value
		/// FieldMarshal.NativeType
		/// DeclSecurity.PermissionSet
		/// StandAloneSig.Signature
		/// Property.Type
		/// TypeSpec.Signature
		/// Assembly.PublicKey
		/// AssemblyRef.PublicKeyOrToken
		/// File.HashValue
		/// MethodSpec.Instantiation
		/// </summary>
		/// <param name="token">A token</param>
		/// <returns>The value in the #Blob or <c>null</c> if <paramref name="token"/> is invalid</returns>
		public byte[] ReadBlob(uint token) {
			uint rid = MDToken.ToRID(token);
			switch (MDToken.ToTable(token)) {
			case Table.Field:
				var fieldRow = TablesStream.ReadFieldRow(rid);
				if (fieldRow == null)
					break;
				return BlobStream.Read(fieldRow.Signature);

			case Table.Method:
				var methodRow = TablesStream.ReadMethodRow(rid);
				if (methodRow == null)
					break;
				return BlobStream.Read(methodRow.Signature);

			case Table.MemberRef:
				var mrRow = TablesStream.ReadMemberRefRow(rid);
				if (mrRow == null)
					break;
				return BlobStream.Read(mrRow.Signature);

			case Table.Constant:
				var constRow = TablesStream.ReadConstantRow(rid);
				if (constRow == null)
					break;
				return BlobStream.Read(constRow.Value);

			case Table.CustomAttribute:
				var caRow = TablesStream.ReadCustomAttributeRow(rid);
				if (caRow == null)
					break;
				return BlobStream.Read(caRow.Value);

			case Table.FieldMarshal:
				var fmRow = TablesStream.ReadFieldMarshalRow(rid);
				if (fmRow == null)
					break;
				return BlobStream.Read(fmRow.NativeType);

			case Table.DeclSecurity:
				var dsRow = TablesStream.ReadDeclSecurityRow(rid);
				if (dsRow == null)
					break;
				return BlobStream.Read(dsRow.PermissionSet);

			case Table.StandAloneSig:
				var sasRow = TablesStream.ReadStandAloneSigRow(rid);
				if (sasRow == null)
					break;
				return BlobStream.Read(sasRow.Signature);

			case Table.Property:
				var propRow = TablesStream.ReadPropertyRow(rid);
				if (propRow == null)
					break;
				return BlobStream.Read(propRow.Type);

			case Table.TypeSpec:
				var tsRow = TablesStream.ReadTypeSpecRow(rid);
				if (tsRow == null)
					break;
				return BlobStream.Read(tsRow.Signature);

			case Table.Assembly:
				var asmRow = TablesStream.ReadAssemblyRow(rid);
				if (asmRow == null)
					break;
				return BlobStream.Read(asmRow.PublicKey);

			case Table.AssemblyRef:
				// HashValue is also in the #Blob but the user has to read it some other way
				var asmRefRow = TablesStream.ReadAssemblyRefRow(rid);
				if (asmRefRow == null)
					break;
				return BlobStream.Read(asmRefRow.PublicKeyOrToken);

			case Table.File:
				var fileRow = TablesStream.ReadFileRow(rid);
				if (fileRow == null)
					break;
				return BlobStream.Read(fileRow.HashValue);

			case Table.MethodSpec:
				var msRow = TablesStream.ReadMethodSpecRow(rid);
				if (msRow == null)
					break;
				return BlobStream.Read(msRow.Instantiation);
			}

			return null;
		}
	}
}
