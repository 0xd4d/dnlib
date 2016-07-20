// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using dnlib.Utils;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb;
using dnlib.DotNet.Writer;
using dnlib.PE;
using dnlib.Threading;
using dnlib.W32Resources;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the Module table
	/// </summary>
	public abstract class ModuleDef : IHasCustomAttribute, IResolutionScope, IDisposable, IListListener<TypeDef>, IModule, ITypeDefFinder, IDnlibDef, ITokenResolver {
		/// <summary>Default characteristics</summary>
		protected const Characteristics DefaultCharacteristics = Characteristics.ExecutableImage | Characteristics._32BitMachine;

		/// <summary>Default DLL characteristics</summary>
		protected const DllCharacteristics DefaultDllCharacteristics = DllCharacteristics.TerminalServerAware | DllCharacteristics.NoSeh | DllCharacteristics.NxCompat | DllCharacteristics.DynamicBase;

		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		/// <summary>
		/// Initialize this in the ctor
		/// </summary>
		protected ICorLibTypes corLibTypes;

		/// <summary>
		/// PDB state
		/// </summary>
		protected PdbState pdbState;

		TypeDefFinder typeDefFinder;

		/// <summary>
		/// Array of last used rid in each table. I.e., next free rid is value + 1
		/// </summary>
		protected readonly int[] lastUsedRids = new int[64];

		/// <summary>Module context</summary>
		protected ModuleContext context;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.Module, rid); }
		}

		/// <inheritdoc/>
		public uint Rid {
			get { return rid; }
			set { rid = value; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 7; }
		}

		/// <inheritdoc/>
		public int ResolutionScopeTag {
			get { return 0; }
		}

		/// <summary>
		/// Gets/sets a user value. This is never used by dnlib. This property isn't thread safe.
		/// </summary>
		public object Tag {
			get { return tag; }
			set { tag = value; }
		}
		object tag;

		/// <inheritdoc/>
		public ScopeType ScopeType {
			get { return ScopeType.ModuleDef; }
		}

		/// <inheritdoc/>
		public string ScopeName {
			get { return FullName; }
		}

		/// <summary>
		/// Gets/sets Module.Generation column
		/// </summary>
		public ushort Generation {
			get { return generation; }
			set { generation = value; }
		}
		/// <summary/>
		protected ushort generation;

		/// <summary>
		/// Gets/sets Module.Name column
		/// </summary>
		public UTF8String Name {
			get { return name; }
			set { name = value; }
		}
		/// <summary>Name</summary>
		protected UTF8String name;

		/// <summary>
		/// Gets/sets Module.Mvid column
		/// </summary>
		public Guid? Mvid {
			get { return mvid; }
			set { mvid = value; }
		}
		/// <summary/>
		protected Guid? mvid;

		/// <summary>
		/// Gets/sets Module.EncId column
		/// </summary>
		public Guid? EncId {
			get { return encId; }
			set { encId = value; }
		}
		/// <summary/>
		protected Guid? encId;

		/// <summary>
		/// Gets/sets Module.EncBaseId column
		/// </summary>
		public Guid? EncBaseId {
			get { return encBaseId; }
			set { encBaseId = value; }
		}
		/// <summary/>
		protected Guid? encBaseId;

		/// <summary>
		/// Gets all custom attributes
		/// </summary>
		public CustomAttributeCollection CustomAttributes {
			get {
				if (customAttributes == null)
					InitializeCustomAttributes();
				return customAttributes;
			}
		}
		/// <summary/>
		protected CustomAttributeCollection customAttributes;
		/// <summary>Initializes <see cref="customAttributes"/></summary>
		protected virtual void InitializeCustomAttributes() {
			Interlocked.CompareExchange(ref customAttributes, new CustomAttributeCollection(), null);
		}

		/// <summary>
		/// Gets the module's assembly. To set this value, add this <see cref="ModuleDef"/>
		/// to <see cref="AssemblyDef.Modules"/>.
		/// </summary>
		public AssemblyDef Assembly {
			get { return assembly; }
			internal set { assembly = value; }
		}
		/// <summary/>
		protected AssemblyDef assembly;

		/// <summary>
		/// Gets a list of all non-nested <see cref="TypeDef"/>s. See also <see cref="GetTypes()"/>
		/// </summary>
		public ThreadSafe.IList<TypeDef> Types {
			get {
				if (types == null)
					InitializeTypes();
				return types;
			}
		}
		/// <summary/>
		protected LazyList<TypeDef> types;
		/// <summary>Initializes <see cref="types"/></summary>
		protected virtual void InitializeTypes() {
			Interlocked.CompareExchange(ref types, new LazyList<TypeDef>(this), null);
		}

		/// <summary>
		/// Gets a list of all <see cref="ExportedType"/>s
		/// </summary>
		public ThreadSafe.IList<ExportedType> ExportedTypes {
			get {
				if (exportedTypes == null)
					InitializeExportedTypes();
				return exportedTypes;
			}
		}
		/// <summary/>
		protected ThreadSafe.IList<ExportedType> exportedTypes;
		/// <summary>Initializes <see cref="exportedTypes"/></summary>
		protected virtual void InitializeExportedTypes() {
			Interlocked.CompareExchange(ref exportedTypes, ThreadSafeListCreator.Create<ExportedType>(), null);
		}

		/// <summary>
		/// Gets/sets the native entry point. Only one of <see cref="NativeEntryPoint"/> and
		/// <see cref="ManagedEntryPoint"/> can be set. You write to one and the other one gets cleared.
		/// </summary>
		public RVA NativeEntryPoint {
			get {
				if (!nativeAndManagedEntryPoint_initialized)
					InitializeNativeAndManagedEntryPoint();
				return nativeEntryPoint;
			}
			set {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				nativeEntryPoint = value;
				managedEntryPoint = null;
				nativeAndManagedEntryPoint_initialized = true;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}
		/// <summary>
		/// Gets/sets the managed entry point. Only one of <see cref="NativeEntryPoint"/> and
		/// <see cref="ManagedEntryPoint"/> can be set. You write to one and the other one gets cleared.
		/// </summary>
		public IManagedEntryPoint ManagedEntryPoint {
			get {
				if (!nativeAndManagedEntryPoint_initialized)
					InitializeNativeAndManagedEntryPoint();
				return managedEntryPoint;
			}
			set {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				nativeEntryPoint = 0;
				managedEntryPoint = value;
				nativeAndManagedEntryPoint_initialized = true;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}
		/// <summary/>
		protected RVA nativeEntryPoint;
		/// <summary/>
		protected IManagedEntryPoint managedEntryPoint;
		/// <summary/>
		protected bool nativeAndManagedEntryPoint_initialized;

		void InitializeNativeAndManagedEntryPoint() {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (nativeAndManagedEntryPoint_initialized)
				return;
			nativeEntryPoint = GetNativeEntryPoint_NoLock();
			managedEntryPoint = GetManagedEntryPoint_NoLock();
			nativeAndManagedEntryPoint_initialized = true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}
		/// <summary>Called to initialize <see cref="nativeEntryPoint"/></summary>
		protected virtual RVA GetNativeEntryPoint_NoLock() {
			return 0;
		}
		/// <summary>Called to initialize <see cref="managedEntryPoint"/></summary>
		protected virtual IManagedEntryPoint GetManagedEntryPoint_NoLock() {
			return null;
		}

		/// <inheritdoc/>
		public bool HasCustomAttributes {
			get { return CustomAttributes.Count > 0; }
		}

		/// <summary>
		/// Gets/sets the entry point method
		/// </summary>
		public MethodDef EntryPoint {
			get { return ManagedEntryPoint as MethodDef; }
			set { ManagedEntryPoint = value; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="NativeEntryPoint"/> is non-zero
		/// </summary>
		public bool IsNativeEntryPointValid {
			get { return NativeEntryPoint != 0; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="ManagedEntryPoint"/> is non-null
		/// </summary>
		public bool IsManagedEntryPointValid {
			get { return ManagedEntryPoint != null; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="EntryPoint"/> is non-null
		/// </summary>
		public bool IsEntryPointValid {
			get { return EntryPoint != null; }
		}

		/// <summary>
		/// Gets a list of all <see cref="Resource"/>s
		/// </summary>
		public ResourceCollection Resources {
			get {
				if (resources == null)
					InitializeResources();
				return resources;
			}
		}
		/// <summary/>
		protected ResourceCollection resources;
		/// <summary>Initializes <see cref="resources"/></summary>
		protected virtual void InitializeResources() {
			Interlocked.CompareExchange(ref resources, new ResourceCollection(), null);
		}

		/// <summary>
		/// Gets/sets the <see cref="VTableFixups"/>. This is <c>null</c> if there are no
		/// vtable fixups.
		/// </summary>
		public VTableFixups VTableFixups {
			get {
				if (!vtableFixups_isInitialized)
					InitializeVTableFixups();
				return vtableFixups;
			}
			set {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				vtableFixups = value;
				vtableFixups_isInitialized = true;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}
		/// <summary/>
		protected VTableFixups vtableFixups;
		/// <summary/>
		protected bool vtableFixups_isInitialized;

		void InitializeVTableFixups() {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (vtableFixups_isInitialized)
				return;
			vtableFixups = GetVTableFixups_NoLock();
			vtableFixups_isInitialized = true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>Called to initialize <see cref="vtableFixups"/></summary>
		protected virtual VTableFixups GetVTableFixups_NoLock() {
			return null;
		}

		/// <summary>
		/// <c>true</c> if there's at least one <see cref="TypeDef"/> in <see cref="Types"/>
		/// </summary>
		public bool HasTypes {
			get { return Types.Count > 0; }
		}

		/// <summary>
		/// <c>true</c> if there's at least one <see cref="ExportedType"/> in <see cref="ExportedTypes"/>
		/// </summary>
		public bool HasExportedTypes {
			get { return ExportedTypes.Count > 0; }
		}

		/// <summary>
		/// <c>true</c> if there's at least one <see cref="Resource"/> in <see cref="Resources"/>
		/// </summary>
		public bool HasResources {
			get { return Resources.Count > 0; }
		}

		/// <inheritdoc/>
		public string FullName {
			get { return UTF8String.ToSystemStringOrEmpty(name); }
		}

		/// <summary>
		/// Gets/sets the path of the module or an empty string if it wasn't loaded from disk
		/// </summary>
		public string Location {
			get { return location; }
			set { location = value; }
		}
		/// <summary/>
		protected string location;

		/// <summary>
		/// Gets the <see cref="ICorLibTypes"/>
		/// </summary>
		public ICorLibTypes CorLibTypes {
			get { return corLibTypes; }
		}

		/// <summary>
		/// Gets the <see cref="TypeDefFinder"/> instance
		/// </summary>
		TypeDefFinder TypeDefFinder {
			get {
				if (typeDefFinder == null)
					Interlocked.CompareExchange(ref typeDefFinder, new TypeDefFinder(Types), null);
				return typeDefFinder;
			}
		}

		/// <summary>
		/// Gets/sets the module context. This is never <c>null</c>.
		/// </summary>
		public ModuleContext Context {
			get {
				if (context == null)
					Interlocked.CompareExchange(ref context, new ModuleContext(), null);
				return context;
			}
			set { context = value ?? new ModuleContext(); }
		}

		/// <summary>
		/// If <c>true</c>, the <see cref="TypeDef"/> cache is enabled. The cache is used by
		/// <see cref="Find(string,bool)"/> and <see cref="Find(TypeRef)"/> to find types.
		/// <br/><br/>
		/// <c>IMPORTANT:</c> Only enable the cache if this module's types keep their exact
		/// name, namespace, and declaring type and if <c>no</c> type is either added or
		/// removed from <see cref="Types"/> or from any type that is reachable from the
		/// top-level types in <see cref="Types"/> (i.e., any type owned by this module).
		/// This is disabled by default. When disabled, all calls to <see cref="Find(string,bool)"/>
		/// and <see cref="Find(TypeRef)"/> will result in a slow <c>O(n)</c> (linear) search.
		/// </summary>
		/// <seealso cref="ResetTypeDefFindCache()"/>
		public bool EnableTypeDefFindCache {
			get { return TypeDefFinder.IsCacheEnabled; }
			set { TypeDefFinder.IsCacheEnabled = value; }
		}

		/// <summary>
		/// <c>true</c> if this is the manifest (main) module
		/// </summary>
		public bool IsManifestModule {
			get {
				var asm = assembly;
				return asm != null && asm.ManifestModule == this;
			}
		}

		/// <summary>
		/// Gets the global (aka. &lt;Module&gt;) type or <c>null</c> if there are no types
		/// </summary>
		public TypeDef GlobalType {
			get { return Types.Get(0, null); }
		}

		/// <summary>
		/// Gets/sets the Win32 resources
		/// </summary>
		public Win32Resources Win32Resources {
			get {
				if (!win32Resources_isInitialized)
					InitializeWin32Resources();
				return win32Resources;
			}
			set {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				win32Resources = value;
				win32Resources_isInitialized = true;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}
		/// <summary/>
		protected Win32Resources win32Resources;
		/// <summary/>
		protected bool win32Resources_isInitialized;

		void InitializeWin32Resources() {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
			if (win32Resources_isInitialized)
				return;
			win32Resources = GetWin32Resources_NoLock();
			win32Resources_isInitialized = true;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>Called to initialize <see cref="win32Resources"/></summary>
		protected virtual Win32Resources GetWin32Resources_NoLock() {
			return null;
		}

		/// <summary>
		/// Gets the <see cref="dnlib.DotNet.Pdb.PdbState"/>. This is <c>null</c> if no PDB file
		/// has been loaded or if no PDB file could be found.
		/// </summary>
		public PdbState PdbState {
			get { return pdbState; }
		}

		/// <summary>
		/// Module kind
		/// </summary>
		public ModuleKind Kind { get; set; }

		/// <summary>
		/// Gets/sets the characteristics (from PE file header)
		/// </summary>
		public Characteristics Characteristics { get; set; }

		/// <summary>
		/// Gets/sets the DLL characteristics (from PE optional header)
		/// </summary>
		public DllCharacteristics DllCharacteristics { get; set; }

		/// <summary>
		/// Gets/sets the runtime version which is stored in the MetaData header.
		/// See <see cref="MDHeaderRuntimeVersion"/>.
		/// </summary>
		/// <remarks>Not thread safe</remarks>
		public string RuntimeVersion {
			get { return runtimeVersion; }
			set {
				if (runtimeVersion != value) {
					runtimeVersion = value;
					cachedWinMDStatus = null;
					runtimeVersionWinMD = null;
					winMDVersion = null;
				}
			}
		}
		string runtimeVersion;

		/// <summary>
		/// Gets the WinMD status
		/// </summary>
		/// <remarks>Not thread safe</remarks>
		public WinMDStatus WinMDStatus {
			get {
				var cval = cachedWinMDStatus;
				if (cval != null)
					return cval.Value;
				cachedWinMDStatus = cval = CalculateWinMDStatus(RuntimeVersion);
				return cval.Value;
			}
		}
		WinMDStatus? cachedWinMDStatus;

		/// <summary>
		/// <c>true</c> if this is a WinMD file
		/// </summary>
		public bool IsWinMD {
			get { return WinMDStatus != WinMDStatus.None; }
		}

		/// <summary>
		/// <c>true</c> if this is a managed WinMD file
		/// </summary>
		public bool IsManagedWinMD {
			get { return WinMDStatus == WinMDStatus.Managed; }
		}

		/// <summary>
		/// <c>true</c> if this is a pure (non-managed) WinMD file
		/// </summary>
		public bool IsPureWinMD {
			get { return WinMDStatus == WinMDStatus.Pure; }
		}

		/// <summary>
		/// Gets the CLR runtime version of the managed WinMD file or <c>null</c> if none. This is
		/// similar to <see cref="RuntimeVersion"/> for normal non-WinMD files.
		/// </summary>
		/// <remarks>Not thread safe</remarks>
		public string RuntimeVersionWinMD {
			get {
				var rtver = runtimeVersionWinMD;
				if (rtver != null)
					return rtver;
				runtimeVersionWinMD = rtver = CalculateRuntimeVersionWinMD(RuntimeVersion);
				return rtver;
			}
		}
		string runtimeVersionWinMD;

		/// <summary>
		/// Gets the WinMD version or <c>null</c> if none
		/// </summary>
		/// <remarks>Not thread safe</remarks>
		public string WinMDVersion {
			get {
				var ver = winMDVersion;
				if (ver != null)
					return ver;
				winMDVersion = ver = CalculateWinMDVersion(RuntimeVersion);
				return ver;
			}
		}
		string winMDVersion;

		static WinMDStatus CalculateWinMDStatus(string version) {
			if (version == null)
				return WinMDStatus.None;
			if (!version.StartsWith("WindowsRuntime ", StringComparison.Ordinal))
				return WinMDStatus.None;

			return version.IndexOf(';') < 0 ? WinMDStatus.Pure : WinMDStatus.Managed;
		}

		static string CalculateRuntimeVersionWinMD(string version) {
			// Original parser code:
			// CoreCLR file: src/md/winmd/adapter.cpp
			// Func: WinMDAdapter::Create(IMDCommon *pRawMDCommon, /*[out]*/ WinMDAdapter **ppAdapter)
			if (version == null)
				return null;
			if (!version.StartsWith("WindowsRuntime ", StringComparison.Ordinal))
				return null;
			int index = version.IndexOf(';');
			if (index < 0)
				return null;
			var s = version.Substring(index + 1);
			if (s.StartsWith("CLR", StringComparison.OrdinalIgnoreCase))
				s = s.Substring(3);
			s = s.TrimStart(' ');

			return s;
		}

		static string CalculateWinMDVersion(string version) {
			if (version == null)
				return null;
			if (!version.StartsWith("WindowsRuntime ", StringComparison.Ordinal))
				return null;
			int index = version.IndexOf(';');
			if (index < 0)
				return version;
			return version.Substring(0, index);
		}

		/// <summary>
		/// <c>true</c> if <see cref="RuntimeVersion"/> is the CLR v1.0 string (only the major
		/// and minor version numbers are checked)
		/// </summary>
		public bool IsClr10 {
			get {
				var ver = RuntimeVersion ?? string.Empty;
				return ver.StartsWith(MDHeaderRuntimeVersion.MS_CLR_10_PREFIX) ||
					ver.StartsWith(MDHeaderRuntimeVersion.MS_CLR_10_PREFIX_X86RETAIL) ||
					ver == MDHeaderRuntimeVersion.MS_CLR_10_RETAIL ||
					ver == MDHeaderRuntimeVersion.MS_CLR_10_COMPLUS;
			}
		}

		/// <summary>
		/// <c>true</c> if <see cref="RuntimeVersion"/> is the CLR v1.0 string
		/// </summary>
		public bool IsClr10Exactly {
			get {
				return RuntimeVersion == MDHeaderRuntimeVersion.MS_CLR_10 ||
					RuntimeVersion == MDHeaderRuntimeVersion.MS_CLR_10_X86RETAIL ||
					RuntimeVersion == MDHeaderRuntimeVersion.MS_CLR_10_RETAIL ||
					RuntimeVersion == MDHeaderRuntimeVersion.MS_CLR_10_COMPLUS;
			}
		}

		/// <summary>
		/// <c>true</c> if <see cref="RuntimeVersion"/> is the CLR v1.1 string (only the major
		/// and minor version numbers are checked)
		/// </summary>
		public bool IsClr11 {
			get { return (RuntimeVersion ?? string.Empty).StartsWith(MDHeaderRuntimeVersion.MS_CLR_11_PREFIX); }
		}

		/// <summary>
		/// <c>true</c> if <see cref="RuntimeVersion"/> is the CLR v1.1 string
		/// </summary>
		public bool IsClr11Exactly {
			get { return RuntimeVersion == MDHeaderRuntimeVersion.MS_CLR_11; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="RuntimeVersion"/> is the CLR v1.0 or v1.1 string (only the
		/// major and minor version numbers are checked)
		/// </summary>
		public bool IsClr1x {
			get { return IsClr10 || IsClr11; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="RuntimeVersion"/> is the CLR v1.0 or v1.1 string
		/// </summary>
		public bool IsClr1xExactly {
			get { return IsClr10Exactly || IsClr11Exactly; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="RuntimeVersion"/> is the CLR v2.0 string (only the major
		/// and minor version numbers are checked)
		/// </summary>
		public bool IsClr20 {
			get { return (RuntimeVersion ?? string.Empty).StartsWith(MDHeaderRuntimeVersion.MS_CLR_20_PREFIX); }
		}

		/// <summary>
		/// <c>true</c> if <see cref="RuntimeVersion"/> is the CLR v2.0 string
		/// </summary>
		public bool IsClr20Exactly {
			get { return RuntimeVersion == MDHeaderRuntimeVersion.MS_CLR_20; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="RuntimeVersion"/> is the CLR v4.0 string (only the major
		/// and minor version numbers are checked)
		/// </summary>
		public bool IsClr40 {
			get { return (RuntimeVersion ?? string.Empty).StartsWith(MDHeaderRuntimeVersion.MS_CLR_40_PREFIX); }
		}

		/// <summary>
		/// <c>true</c> if <see cref="RuntimeVersion"/> is the CLR v4.0 string
		/// </summary>
		public bool IsClr40Exactly {
			get { return RuntimeVersion == MDHeaderRuntimeVersion.MS_CLR_40; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="RuntimeVersion"/> is the ECMA 2002 string
		/// </summary>
		public bool IsEcma2002 {
			get { return RuntimeVersion == MDHeaderRuntimeVersion.ECMA_2002; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="RuntimeVersion"/> is the ECMA 2005 string
		/// </summary>
		public bool IsEcma2005 {
			get { return RuntimeVersion == MDHeaderRuntimeVersion.ECMA_2005; }
		}

		/// <summary>
		/// Gets/sets the <see cref="Machine"/> (from PE header)
		/// </summary>
		public Machine Machine { get; set; }

		/// <summary>
		/// <c>true</c> if <see cref="Machine"/> is <see cref="dnlib.PE.Machine.I386"/>
		/// </summary>
		public bool IsI386 {
			get { return Machine == Machine.I386; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="Machine"/> is <see cref="dnlib.PE.Machine.IA64"/>
		/// </summary>
		public bool IsIA64 {
			get { return Machine == Machine.IA64; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="Machine"/> is <see cref="dnlib.PE.Machine.AMD64"/>
		/// </summary>
		public bool IsAMD64 {
			get { return Machine == Machine.AMD64; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="Machine"/> is <see cref="dnlib.PE.Machine.ARM64"/>
		/// </summary>
		public bool IsARM64 {
			get { return Machine == Machine.ARM64; }
		}

		/// <summary>
		/// Gets/sets the <see cref="Cor20HeaderFlags"/> (from .NET header)
		/// </summary>
		public ComImageFlags Cor20HeaderFlags {
			get { return (ComImageFlags)cor20HeaderFlags; }
			set { cor20HeaderFlags = (int)value; }
		}
		/// <summary/>
		protected int cor20HeaderFlags;

		/// <summary>
		/// Gets/sets the runtime version number in the COR20 header. The major version is
		/// in the high 16 bits. The minor version is in the low 16 bits. This is normally 2.5
		/// (0x00020005), but if it's .NET 1.x, it should be 2.0 (0x00020000). If this is
		/// <c>null</c>, the default value will be used when saving the module (2.0 if CLR 1.x,
		/// and 2.5 if not CLR 1.x).
		/// </summary>
		public uint? Cor20HeaderRuntimeVersion { get; set; }

		/// <summary>
		/// Gets the tables header version. The major version is in the upper 8 bits and the
		/// minor version is in the lower 8 bits. .NET 1.0/1.1 use version 1.0 (0x0100) and
		/// .NET 2.x and later use version 2.0 (0x0200). 1.0 has no support for generics,
		/// 1.1 has support for generics (GenericParam rows have an extra Kind column),
		/// and 2.0 has support for generics (GenericParam rows have the standard 4 columns).
		/// No other version is supported. If this is <c>null</c>, the default version is
		/// used (1.0 if .NET 1.x, else 2.0).
		/// </summary>
		public ushort? TablesHeaderVersion { get; set; }

		/// <summary>
		/// Set or clear flags in <see cref="cor20HeaderFlags"/>
		/// </summary>
		/// <param name="set"><c>true</c> if flags should be set, <c>false</c> if flags should
		/// be cleared</param>
		/// <param name="flags">Flags to set or clear</param>
		void ModifyComImageFlags(bool set, ComImageFlags flags) {
#if THREAD_SAFE
			int origVal, newVal;
			do {
				origVal = cor20HeaderFlags;
				if (set)
					newVal = origVal | (int)flags;
				else
					newVal = origVal & ~(int)flags;
			} while (Interlocked.CompareExchange(ref cor20HeaderFlags, newVal, origVal) != origVal);
#else
			if (set)
				cor20HeaderFlags |= (int)flags;
			else
				cor20HeaderFlags &= ~(int)flags;
#endif
		}

		/// <summary>
		/// Gets/sets the <see cref="ComImageFlags.ILOnly"/> bit
		/// </summary>
		public bool IsILOnly {
			get { return ((ComImageFlags)cor20HeaderFlags & ComImageFlags.ILOnly) != 0; }
			set { ModifyComImageFlags(value, ComImageFlags.ILOnly); }
		}

		/// <summary>
		/// Gets/sets the <see cref="ComImageFlags._32BitRequired"/> bit
		/// </summary>
		public bool Is32BitRequired {
			get { return ((ComImageFlags)cor20HeaderFlags & ComImageFlags._32BitRequired) != 0; }
			set { ModifyComImageFlags(value, ComImageFlags._32BitRequired); }
		}

		/// <summary>
		/// Gets/sets the <see cref="ComImageFlags.StrongNameSigned"/> bit
		/// </summary>
		public bool IsStrongNameSigned {
			get { return ((ComImageFlags)cor20HeaderFlags & ComImageFlags.StrongNameSigned) != 0; }
			set { ModifyComImageFlags(value, ComImageFlags.StrongNameSigned); }
		}

		/// <summary>
		/// Gets/sets the <see cref="ComImageFlags.NativeEntryPoint"/> bit
		/// </summary>
		public bool HasNativeEntryPoint {
			get { return ((ComImageFlags)cor20HeaderFlags & ComImageFlags.NativeEntryPoint) != 0; }
			set { ModifyComImageFlags(value, ComImageFlags.NativeEntryPoint); }
		}

		/// <summary>
		/// Gets/sets the <see cref="ComImageFlags._32BitPreferred"/> bit
		/// </summary>
		public bool Is32BitPreferred {
			get { return ((ComImageFlags)cor20HeaderFlags & ComImageFlags._32BitPreferred) != 0; }
			set { ModifyComImageFlags(value, ComImageFlags._32BitPreferred); }
		}

		/// <inheritdoc/>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose method
		/// </summary>
		/// <param name="disposing"><c>true</c> if called by <see cref="Dispose()"/></param>
		protected virtual void Dispose(bool disposing) {
			if (!disposing)
				return;
			foreach (var resource in Resources.GetInitializedElements(true)) {
				if (resource != null)
					resource.Dispose();
			}
			var tdf = typeDefFinder;
			if (tdf != null) {
				tdf.Dispose();
				typeDefFinder = null;
			}
		}

		/// <summary>
		/// Gets all the types (including nested types) present in this module
		/// </summary>
		public IEnumerable<TypeDef> GetTypes() {
			return AllTypesHelper.Types(Types);
		}

		/// <summary>
		/// Adds <paramref name="typeDef"/> as a non-nested type. If it's already nested, its
		/// <see cref="TypeDef.DeclaringType"/> will be set to <c>null</c>.
		/// </summary>
		/// <param name="typeDef">The <see cref="TypeDef"/> to insert</param>
		public void AddAsNonNestedType(TypeDef typeDef) {
			if (typeDef == null)
				return;
			typeDef.DeclaringType = null;
			Types.Add(typeDef);
		}

		/// <summary>
		/// Updates the <c>rid</c> to the next free <c>rid</c> available. It's only updated if
		/// the original <c>rid</c> is 0.
		/// </summary>
		/// <typeparam name="T">IMDTokenProvider</typeparam>
		/// <param name="tableRow">The row that should be updated</param>
		/// <returns>Returns the input</returns>
		public T UpdateRowId<T>(T tableRow) where T : IMDTokenProvider {
			if (tableRow != null && tableRow.Rid == 0)
				tableRow.Rid = GetNextFreeRid(tableRow.MDToken.Table);
			return tableRow;
		}

		/// <summary>
		/// Updates the <c>rid</c> to the next free <c>rid</c> available.
		/// </summary>
		/// <typeparam name="T">IMDTokenProvider</typeparam>
		/// <param name="tableRow">The row that should be updated</param>
		/// <returns>Returns the input</returns>
		public T ForceUpdateRowId<T>(T tableRow) where T : IMDTokenProvider {
			if (tableRow != null)
				tableRow.Rid = GetNextFreeRid(tableRow.MDToken.Table);
			return tableRow;
		}

		uint GetNextFreeRid(Table table) {
			if ((uint)table >= lastUsedRids.Length)
				return 0;
			return (uint)Interlocked.Increment(ref lastUsedRids[(int)table]);
		}

		/// <summary>
		/// Imports a <see cref="Type"/> as a <see cref="ITypeDefOrRef"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type or <c>null</c> if <paramref name="type"/> is invalid</returns>
		public ITypeDefOrRef Import(Type type) {
			return new Importer(this).Import(type);
		}

		/// <summary>
		/// Imports a <see cref="Type"/> as a <see cref="TypeSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type or <c>null</c> if <paramref name="type"/> is invalid</returns>
		public TypeSig ImportAsTypeSig(Type type) {
			return new Importer(this).ImportAsTypeSig(type);
		}

		/// <summary>
		/// Imports a <see cref="FieldInfo"/> as a <see cref="MemberRef"/>
		/// </summary>
		/// <param name="fieldInfo">The field</param>
		/// <returns>The imported field or <c>null</c> if <paramref name="fieldInfo"/> is invalid
		/// or if we failed to import the field</returns>
		public MemberRef Import(FieldInfo fieldInfo) {
			return (MemberRef)new Importer(this).Import(fieldInfo);
		}

		/// <summary>
		/// Imports a <see cref="MethodBase"/> as a <see cref="IMethod"/>. This will be either
		/// a <see cref="MemberRef"/> or a <see cref="MethodSpec"/>.
		/// </summary>
		/// <param name="methodBase">The method</param>
		/// <returns>The imported method or <c>null</c> if <paramref name="methodBase"/> is invalid
		/// or if we failed to import the method</returns>
		public IMethod Import(MethodBase methodBase) {
			return new Importer(this).Import(methodBase);
		}

		/// <summary>
		/// Imports a <see cref="IType"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type or <c>null</c></returns>
		public IType Import(IType type) {
			return new Importer(this).Import(type);
		}

		/// <summary>
		/// Imports a <see cref="TypeDef"/> as a <see cref="TypeRef"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type or <c>null</c></returns>
		public TypeRef Import(TypeDef type) {
			return (TypeRef)new Importer(this).Import(type);
		}

		/// <summary>
		/// Imports a <see cref="TypeRef"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type or <c>null</c></returns>
		public TypeRef Import(TypeRef type) {
			return (TypeRef)new Importer(this).Import(type);
		}

		/// <summary>
		/// Imports a <see cref="TypeSpec"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type or <c>null</c></returns>
		public TypeSpec Import(TypeSpec type) {
			return new Importer(this).Import(type);
		}

		/// <summary>
		/// Imports a <see cref="TypeSig"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type or <c>null</c></returns>
		public TypeSig Import(TypeSig type) {
			return new Importer(this).Import(type);
		}

		/// <summary>
		/// Imports a <see cref="IField"/>
		/// </summary>
		/// <param name="field">The field</param>
		/// <returns>The imported type or <c>null</c> if <paramref name="field"/> is invalid</returns>
		public IField Import(IField field) {
			return (MemberRef)new Importer(this).Import(field);
		}

		/// <summary>
		/// Imports a <see cref="FieldDef"/> as a <see cref="MemberRef"/>
		/// </summary>
		/// <param name="field">The field</param>
		/// <returns>The imported type or <c>null</c> if <paramref name="field"/> is invalid</returns>
		public IField Import(FieldDef field) {
			return (MemberRef)new Importer(this).Import(field);
		}

		/// <summary>
		/// Imports a <see cref="IMethod"/>
		/// </summary>
		/// <param name="method">The method</param>
		/// <returns>The imported method or <c>null</c> if <paramref name="method"/> is invalid</returns>
		public IMethod Import(IMethod method) {
			return new Importer(this).Import(method);
		}

		/// <summary>
		/// Imports a <see cref="MethodDef"/> as a <see cref="MemberRef"/>
		/// </summary>
		/// <param name="method">The method</param>
		/// <returns>The imported method or <c>null</c> if <paramref name="method"/> is invalid</returns>
		public IMethod Import(MethodDef method) {
			return (MemberRef)new Importer(this).Import(method);
		}

		/// <summary>
		/// Imports a <see cref="MethodSpec"/>
		/// </summary>
		/// <param name="method">The method</param>
		/// <returns>The imported method or <c>null</c> if <paramref name="method"/> is invalid</returns>
		public MethodSpec Import(MethodSpec method) {
			return new Importer(this).Import(method);
		}

		/// <summary>
		/// Imports a <see cref="MemberRef"/>
		/// </summary>
		/// <param name="memberRef">The member ref</param>
		/// <returns>The imported member ref or <c>null</c> if <paramref name="memberRef"/> is invalid</returns>
		public MemberRef Import(MemberRef memberRef) {
			return new Importer(this).Import(memberRef);
		}

		/// <summary>
		/// Writes the module to a file on disk. If the file exists, it will be overwritten.
		/// </summary>
		/// <param name="filename">Filename</param>
		public void Write(string filename) {
			Write(filename, null);
		}

		/// <summary>
		/// Writes the module to a file on disk. If the file exists, it will be overwritten.
		/// </summary>
		/// <param name="filename">Filename</param>
		/// <param name="options">Writer options</param>
		public void Write(string filename, ModuleWriterOptions options) {
			var writer = new ModuleWriter(this, options ?? new ModuleWriterOptions(this));
			writer.Write(filename);
		}

		/// <summary>
		/// Writes the module to a stream.
		/// </summary>
		/// <param name="dest">Destination stream</param>
		public void Write(Stream dest) {
			Write(dest, null);
		}

		/// <summary>
		/// Writes the module to a stream.
		/// </summary>
		/// <param name="dest">Destination stream</param>
		/// <param name="options">Writer options</param>
		public void Write(Stream dest, ModuleWriterOptions options) {
			var writer = new ModuleWriter(this, options ?? new ModuleWriterOptions(this));
			writer.Write(dest);
		}

		/// <summary>
		/// Resets the <see cref="TypeDef"/> cache which can be enabled by setting
		/// <see cref="EnableTypeDefFindCache"/> to <c>true</c>. Use this method if the cache is
		/// enabled but some of the types have been modified (eg. removed, added, renamed).
		/// </summary>
		public void ResetTypeDefFindCache() {
			TypeDefFinder.ResetCache();
		}

		/// <summary>
		/// Finds a <see cref="ResourceData"/>
		/// </summary>
		/// <param name="type">Type</param>
		/// <param name="name">Name</param>
		/// <param name="langId">Language ID</param>
		/// <returns>The <see cref="ResourceData"/> or <c>null</c> if none found</returns>
		public ResourceData FindWin32ResourceData(ResourceName type, ResourceName name, ResourceName langId) {
			var w32Resources = Win32Resources;
			return w32Resources == null ? null : w32Resources.Find(type, name, langId);
		}

		/// <summary>
		/// Creates a new <see cref="dnlib.DotNet.Pdb.PdbState"/>
		/// </summary>
		public void CreatePdbState() {
			SetPdbState(new PdbState());
		}

		/// <summary>
		/// Sets a <see cref="dnlib.DotNet.Pdb.PdbState"/>
		/// </summary>
		/// <param name="pdbState">New <see cref="dnlib.DotNet.Pdb.PdbState"/></param>
		public void SetPdbState(PdbState pdbState) {
			if (pdbState == null)
				throw new ArgumentNullException("pdbState");
			var orig = Interlocked.CompareExchange(ref this.pdbState, pdbState, null);
			if (orig != null)
				throw new InvalidOperationException("PDB file has already been initialized");
		}

		uint GetCor20RuntimeVersion() {
			var rtVer = Cor20HeaderRuntimeVersion;
			if (rtVer != null)
				return rtVer.Value;
			return IsClr1x ? 0x00020000U : 0x00020005;
		}

		/// <summary>
		/// Returns the size of a pointer. Assumes it's 32-bit if pointer size is unknown or
		/// if it can be 32-bit or 64-bit.
		/// </summary>
		/// <returns>Size of a pointer (4 or 8)</returns>
		public int GetPointerSize() {
			return GetPointerSize(4);
		}

		/// <summary>
		/// Returns the size of a pointer
		/// </summary>
		/// <param name="defaultPointerSize">Default pointer size if it's not known or if it
		/// can be 32-bit or 64-bit</param>
		/// <returns>Size of a pointer (4 or 8)</returns>
		public int GetPointerSize(int defaultPointerSize) {
			var machine = Machine;
			if (machine == Machine.AMD64 || machine == Machine.IA64 || machine == Machine.ARM64)
				return 8;
			if (machine != Machine.I386)
				return 4;

			// Machine is I386 so it's either x86 or platform neutral

			// If the runtime version is < 2.5, then it's always loaded as a 32-bit process.
			if (GetCor20RuntimeVersion() < 0x00020005)
				return 4;

			// If it's a 32-bit PE header, and ILOnly is cleared, it's always loaded as a
			// 32-bit process.
			var flags = (ComImageFlags)cor20HeaderFlags;
			if ((flags & ComImageFlags.ILOnly) == 0)
				return 4;

			// 32-bit Preferred flag is new in .NET 4.5. See CorHdr.h in Windows SDK for more info
			switch (flags & (ComImageFlags._32BitRequired | ComImageFlags._32BitPreferred)) {
			case 0:
				// Machine and ILOnly flag should be checked
				break;

			case ComImageFlags._32BitPreferred:
				// Illegal
				break;

			case ComImageFlags._32BitRequired:
				// x86 image (32-bit process)
				return 4;

			case ComImageFlags._32BitRequired | ComImageFlags._32BitPreferred:
				// Platform neutral but prefers to be 32-bit
				return defaultPointerSize;
			}

			return defaultPointerSize;
		}

		/// <inheritdoc/>
		void IListListener<TypeDef>.OnLazyAdd(int index, ref TypeDef value) {
#if DEBUG
			if (value.DeclaringType != null)
				throw new InvalidOperationException("Added type's DeclaringType != null");
#endif
			value.Module2 = this;
		}

		/// <inheritdoc/>
		void IListListener<TypeDef>.OnAdd(int index, TypeDef value) {
			if (value.DeclaringType != null)
				throw new InvalidOperationException("Nested type is already owned by another type. Set DeclaringType to null first.");
			if (value.Module != null)
				throw new InvalidOperationException("Type is already owned by another module. Remove it from that module's type list.");
			value.Module2 = this;
		}

		/// <inheritdoc/>
		void IListListener<TypeDef>.OnRemove(int index, TypeDef value) {
			value.Module2 = null;
		}

		/// <inheritdoc/>
		void IListListener<TypeDef>.OnResize(int index) {
		}

		/// <inheritdoc/>
		void IListListener<TypeDef>.OnClear() {
			foreach (var type in Types.GetEnumerable_NoLock())
				type.Module2 = null;
		}

		/// <summary>
		/// Finds a <see cref="TypeDef"/>. For speed, enable <see cref="EnableTypeDefFindCache"/>
		/// if possible (read the documentation first).
		/// </summary>
		/// <param name="fullName">Full name of the type (no assembly information)</param>
		/// <param name="isReflectionName"><c>true</c> if it's a reflection name, and nested
		/// type names are separated by a <c>+</c> character. If <c>false</c>, nested type names
		/// are separated by a <c>/</c> character.</param>
		/// <returns>An existing <see cref="TypeDef"/> or <c>null</c> if it wasn't found.</returns>
		public TypeDef Find(string fullName, bool isReflectionName) {
			return TypeDefFinder.Find(fullName, isReflectionName);
		}

		/// <summary>
		/// Finds a <see cref="TypeDef"/>. Its scope (i.e., module or assembly) is ignored when
		/// looking up the type. For speed, enable <see cref="EnableTypeDefFindCache"/> if possible
		/// (read the documentation first).
		/// </summary>
		/// <param name="typeRef">The type ref</param>
		/// <returns>An existing <see cref="TypeDef"/> or <c>null</c> if it wasn't found.</returns>
		public TypeDef Find(TypeRef typeRef) {
			return TypeDefFinder.Find(typeRef);
		}

		/// <summary>
		/// Finds a <see cref="TypeDef"/>
		/// </summary>
		/// <param name="typeRef">The type</param>
		/// <returns>A <see cref="TypeDef"/> or <c>null</c> if it wasn't found</returns>
		public TypeDef Find(ITypeDefOrRef typeRef) {
			var td = typeRef as TypeDef;
			if (td != null)
				return td.Module == this ? td : null;

			var tr = typeRef as TypeRef;
			if (tr != null)
				return Find(tr);

			var ts = typeRef as TypeSpec;
			if (ts == null)
				return null;
			var sig = ts.TypeSig as TypeDefOrRefSig;
			if (sig == null)
				return null;

			td = sig.TypeDef;
			if (td != null)
				return td.Module == this ? td : null;

			tr = sig.TypeRef;
			if (tr != null)
				return Find(tr);

			return null;
		}

		/// <summary>
		/// Creates a new <see cref="ModuleContext"/> instance. There should normally only be one
		/// instance shared by all <see cref="ModuleDef"/>s.
		/// </summary>
		/// <returns>A new <see cref="ModuleContext"/> instance</returns>
		public static ModuleContext CreateModuleContext() {
			return CreateModuleContext(true);
		}

		/// <summary>
		/// Creates a new <see cref="ModuleContext"/> instance. There should normally only be one
		/// instance shared by all <see cref="ModuleDef"/>s.
		/// </summary>
		/// <param name="addOtherSearchPaths">If <c>true</c>, add other common assembly search
		/// paths, not just the module search paths and the GAC.</param>
		/// <returns>A new <see cref="ModuleContext"/> instance</returns>
		public static ModuleContext CreateModuleContext(bool addOtherSearchPaths) {
			var ctx = new ModuleContext();
			var asmRes = new AssemblyResolver(ctx, addOtherSearchPaths);
			var res = new Resolver(asmRes);
			ctx.AssemblyResolver = asmRes;
			ctx.Resolver = res;
			return ctx;
		}

		/// <summary>
		/// Load everything in this module. All types, fields, asm refs, etc are loaded, all their
		/// properties are read to make sure everything is cached.
		/// </summary>
		public void LoadEverything() {
			LoadEverything(null);
		}

		/// <summary>
		/// Load everything in this module. All types, fields, asm refs, etc are loaded, all their
		/// properties are read to make sure everything is cached.
		/// </summary>
		/// <param name="cancellationToken">Cancellation token or <c>null</c></param>
		public virtual void LoadEverything(ICancellationToken cancellationToken) {
			ModuleLoader.LoadAll(this, cancellationToken);
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}

		/// <summary>
		/// Resolves a token
		/// </summary>
		/// <param name="mdToken">The metadata token</param>
		/// <returns>A <see cref="IMDTokenProvider"/> or <c>null</c> if <paramref name="mdToken"/> is invalid</returns>
		public IMDTokenProvider ResolveToken(MDToken mdToken) {
			return ResolveToken(mdToken.Raw, new GenericParamContext());
		}

		/// <summary>
		/// Resolves a token
		/// </summary>
		/// <param name="mdToken">The metadata token</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A <see cref="IMDTokenProvider"/> or <c>null</c> if <paramref name="mdToken"/> is invalid</returns>
		public IMDTokenProvider ResolveToken(MDToken mdToken, GenericParamContext gpContext) {
			return ResolveToken(mdToken.Raw, gpContext);
		}

		/// <summary>
		/// Resolves a token
		/// </summary>
		/// <param name="token">The metadata token</param>
		/// <returns>A <see cref="IMDTokenProvider"/> or <c>null</c> if <paramref name="token"/> is invalid</returns>
		public IMDTokenProvider ResolveToken(uint token) {
			return ResolveToken(token, new GenericParamContext());
		}

		/// <summary>
		/// Resolves a token
		/// </summary>
		/// <param name="token">The metadata token</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A <see cref="IMDTokenProvider"/> or <c>null</c> if <paramref name="token"/> is invalid</returns>
		public virtual IMDTokenProvider ResolveToken(uint token, GenericParamContext gpContext) {
			return null;
		}

		/// <summary>
		/// Gets all <see cref="AssemblyRef"/>s
		/// </summary>
		public IEnumerable<AssemblyRef> GetAssemblyRefs() {
			for (uint rid = 1; ; rid++) {
				var asmRef = ResolveToken(new MDToken(Table.AssemblyRef, rid).Raw) as AssemblyRef;
				if (asmRef == null)
					break;
				yield return asmRef;
			}
		}

		/// <summary>
		/// Gets all <see cref="ModuleRef"/>s
		/// </summary>
		public IEnumerable<ModuleRef> GetModuleRefs() {
			for (uint rid = 1; ; rid++) {
				var modRef = ResolveToken(new MDToken(Table.ModuleRef, rid).Raw) as ModuleRef;
				if (modRef == null)
					break;
				yield return modRef;
			}
		}

		/// <summary>
		/// Gets all <see cref="MemberRef"/>s. <see cref="MemberRef"/>s with generic parameters
		/// aren't cached and a new copy is always returned.
		/// </summary>
		public IEnumerable<MemberRef> GetMemberRefs() {
			return GetMemberRefs(new GenericParamContext());
		}

		/// <summary>
		/// Gets all <see cref="MemberRef"/>s. <see cref="MemberRef"/>s with generic parameters
		/// aren't cached and a new copy is always returned.
		/// </summary>
		/// <param name="gpContext">Generic parameter context</param>
		public IEnumerable<MemberRef> GetMemberRefs(GenericParamContext gpContext) {
			for (uint rid = 1; ; rid++) {
				var mr = ResolveToken(new MDToken(Table.MemberRef, rid).Raw, gpContext) as MemberRef;
				if (mr == null)
					break;
				yield return mr;
			}
		}

		/// <summary>
		/// Gets all <see cref="TypeRef"/>s
		/// </summary>
		public IEnumerable<TypeRef> GetTypeRefs() {
			for (uint rid = 1; ; rid++) {
				var mr = ResolveToken(new MDToken(Table.TypeRef, rid).Raw) as TypeRef;
				if (mr == null)
					break;
				yield return mr;
			}
		}

		/// <summary>
		/// Finds an assembly reference by name. If there's more than one, pick the one with
		/// the greatest version number.
		/// </summary>
		/// <param name="simpleName">Simple name of assembly (eg. "mscorlib")</param>
		/// <returns>The found <see cref="AssemblyRef"/> or <c>null</c> if there's no such
		/// assembly reference.</returns>
		public AssemblyRef GetAssemblyRef(UTF8String simpleName) {
			AssemblyRef found = null;
			foreach (var asmRef in GetAssemblyRefs()) {
				if (asmRef.Name != simpleName)
					continue;
				if (IsGreaterAssemblyRefVersion(found, asmRef))
					found = asmRef;
			}
			return found;
		}

		/// <summary>
		/// Compare asm refs' version
		/// </summary>
		/// <param name="found">First asm ref</param>
		/// <param name="newOne">New asm ref</param>
		/// <returns></returns>
		protected static bool IsGreaterAssemblyRefVersion(AssemblyRef found, AssemblyRef newOne) {
			if (found == null)
				return true;
			var foundVer = found.Version;
			var newVer = newOne.Version;
			return foundVer == null || (newVer != null && newVer >= foundVer);
		}
	}

	/// <summary>
	/// A Module row created by the user and not present in the original .NET file
	/// </summary>
	public class ModuleDefUser : ModuleDef {
		/// <summary>
		/// Default constructor
		/// </summary>
		public ModuleDefUser()
			: this(null, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <remarks><see cref="ModuleDef.Mvid"/> is initialized to a random <see cref="Guid"/></remarks>
		/// <param name="name">Module nam</param>
		public ModuleDefUser(UTF8String name)
			: this(name, Guid.NewGuid()) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Module name</param>
		/// <param name="mvid">Module version ID</param>
		public ModuleDefUser(UTF8String name, Guid? mvid)
			: this(name, mvid, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Module name</param>
		/// <param name="mvid">Module version ID</param>
		/// <param name="corLibAssemblyRef">Corlib assembly ref or <c>null</c></param>
		public ModuleDefUser(UTF8String name, Guid? mvid, AssemblyRef corLibAssemblyRef) {
			this.Kind = ModuleKind.Windows;
			this.Characteristics = DefaultCharacteristics;
			this.DllCharacteristics = DefaultDllCharacteristics;
			this.RuntimeVersion = MDHeaderRuntimeVersion.MS_CLR_20;
			this.Machine = Machine.I386;
			this.cor20HeaderFlags = (int)ComImageFlags.ILOnly;
			this.Cor20HeaderRuntimeVersion = 0x00020005;	// .NET 2.0 or later should use 2.5
			this.TablesHeaderVersion = 0x0200;				// .NET 2.0 or later should use 2.0
			this.types = new LazyList<TypeDef>(this);
			this.exportedTypes = new LazyList<ExportedType>();
			this.resources = new ResourceCollection();
			this.corLibTypes = new CorLibTypes(this, corLibAssemblyRef);
			this.types = new LazyList<TypeDef>(this);
			this.name = name;
			this.mvid = mvid;
			types.Add(CreateModuleType());
			UpdateRowId(this);
		}

		TypeDef CreateModuleType() {
			var type = UpdateRowId(new TypeDefUser(UTF8String.Empty, "<Module>", null));
			type.Attributes = TypeAttributes.NotPublic | TypeAttributes.AutoLayout | TypeAttributes.Class | TypeAttributes.AnsiClass;
			return type;
		}
	}

	/// <summary>
	/// Created from a row in the Module table
	/// </summary>
	public class ModuleDefMD2 : ModuleDef, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

		readonly uint origRid;

		/// <inheritdoc/>
		public uint OrigRid {
			get { return origRid; }
		}

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.MetaData.GetCustomAttributeRidList(Table.Module, origRid);
			var tmp = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <inheritdoc/>
		protected override RVA GetNativeEntryPoint_NoLock() {
			return readerModule.GetNativeEntryPoint();
		}

		/// <inheritdoc/>
		protected override IManagedEntryPoint GetManagedEntryPoint_NoLock() {
			return readerModule.GetManagedEntryPoint();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>Module</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		internal ModuleDefMD2(ModuleDefMD readerModule, uint rid) {
			if (rid == 1 && readerModule == null)
				readerModule = (ModuleDefMD)this;
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid != 1 && readerModule.TablesStream.ModuleTable.IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("Module rid {0} does not exist", rid));
#endif
			this.origRid = rid;
			this.rid = rid;
			this.readerModule = readerModule;
			if (rid != 1) {
				this.Kind = ModuleKind.Windows;
				this.Characteristics = DefaultCharacteristics;
				this.DllCharacteristics = DefaultDllCharacteristics;
				this.RuntimeVersion = MDHeaderRuntimeVersion.MS_CLR_20;
				this.Machine = Machine.I386;
				this.cor20HeaderFlags = (int)ComImageFlags.ILOnly;
				this.Cor20HeaderRuntimeVersion = 0x00020005;	// .NET 2.0 or later should use 2.5
				this.TablesHeaderVersion = 0x0200;				// .NET 2.0 or later should use 2.0
				this.corLibTypes = new CorLibTypes(this);
				this.location = string.Empty;
				InitializeFromRawRow();
			}
		}

		/// <summary>
		/// Initialize fields from the raw <c>Module</c> row
		/// </summary>
		protected void InitializeFromRawRow() {
			uint name, mvid, encId;
			uint encBaseId = readerModule.TablesStream.ReadModuleRow(origRid, out generation, out name, out mvid, out encId);
			this.mvid = readerModule.GuidStream.Read(mvid);
			this.encId = readerModule.GuidStream.Read(encId);
			this.encBaseId = readerModule.GuidStream.Read(encBaseId);
			this.name = readerModule.StringsStream.ReadNoNull(name);
			if (origRid == 1)
				assembly = readerModule.ResolveAssembly(origRid);
		}
	}
}
