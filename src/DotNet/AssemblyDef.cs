// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Reflection;
using System.Threading;
using dnlib.Utils;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.Threading;
using System.Text.RegularExpressions;
using dnlib.DotNet.Pdb;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the Assembly table
	/// </summary>
	public abstract class AssemblyDef : IHasCustomAttribute, IHasDeclSecurity, IHasCustomDebugInformation, IAssembly, IListListener<ModuleDef>, ITypeDefFinder, IDnlibDef {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.Assembly, rid); }
		}

		/// <inheritdoc/>
		public uint Rid {
			get { return rid; }
			set { rid = value; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 14; }
		}

		/// <inheritdoc/>
		public int HasDeclSecurityTag {
			get { return 2; }
		}

		/// <summary>
		/// From column Assembly.HashAlgId
		/// </summary>
		public AssemblyHashAlgorithm HashAlgorithm {
			get { return hashAlgorithm; }
			set { hashAlgorithm = value; }
		}
		/// <summary/>
		protected AssemblyHashAlgorithm hashAlgorithm;

		/// <summary>
		/// From columns Assembly.MajorVersion, Assembly.MinorVersion, Assembly.BuildNumber,
		/// Assembly.RevisionNumber.
		/// </summary>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is <c>null</c></exception>
		public Version Version {
			get { return version; }
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				version = value;
			}
		}
		/// <summary/>
		protected Version version;

		/// <summary>
		/// From column Assembly.Flags
		/// </summary>
		public AssemblyAttributes Attributes {
			get { return (AssemblyAttributes)attributes; }
			set { attributes = (int)value; }
		}
		/// <summary>Attributes</summary>
		protected int attributes;

		/// <summary>
		/// From column Assembly.PublicKey
		/// </summary>
		/// <remarks>An empty <see cref="PublicKey"/> is created if the caller writes <c>null</c></remarks>
		public PublicKey PublicKey {
			get { return publicKey; }
			set { publicKey = value ?? new PublicKey(); }
		}
		/// <summary/>
		protected PublicKey publicKey;

		/// <summary>
		/// Gets the public key token which is calculated from <see cref="PublicKey"/>
		/// </summary>
		public PublicKeyToken PublicKeyToken {
			get { return publicKey.Token; }
		}

		/// <summary>
		/// From column Assembly.Name
		/// </summary>
		public UTF8String Name {
			get { return name; }
			set { name = value; }
		}
		/// <summary>Name</summary>
		protected UTF8String name;

		/// <summary>
		/// From column Assembly.Locale
		/// </summary>
		public UTF8String Culture {
			get { return culture; }
			set { culture = value; }
		}
		/// <summary>Name</summary>
		protected UTF8String culture;

		/// <inheritdoc/>
		public ThreadSafe.IList<DeclSecurity> DeclSecurities {
			get {
				if (declSecurities == null)
					InitializeDeclSecurities();
				return declSecurities;
			}
		}
		/// <summary/>
		protected ThreadSafe.IList<DeclSecurity> declSecurities;
		/// <summary>Initializes <see cref="declSecurities"/></summary>
		protected virtual void InitializeDeclSecurities() {
			Interlocked.CompareExchange(ref declSecurities, ThreadSafeListCreator.Create<DeclSecurity>(), null);
		}

		/// <inheritdoc/>
		public PublicKeyBase PublicKeyOrToken {
			get { return publicKey; }
		}

		/// <inheritdoc/>
		public string FullName {
			get { return GetFullNameWithPublicKeyToken(); }
		}

		/// <inheritdoc/>
		public string FullNameToken {
			get { return GetFullNameWithPublicKeyToken(); }
		}

		/// <summary>
		/// Gets all modules. The first module is always the <see cref="ManifestModule"/>.
		/// </summary>
		public ThreadSafe.IList<ModuleDef> Modules {
			get {
				if (modules == null)
					InitializeModules();
				return modules;
			}
		}
		/// <summary/>
		protected LazyList<ModuleDef> modules;
		/// <summary>Initializes <see cref="modules"/></summary>
		protected virtual void InitializeModules() {
			Interlocked.CompareExchange(ref modules, new LazyList<ModuleDef>(this), null);
		}

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

		/// <inheritdoc/>
		public bool HasCustomAttributes {
			get { return CustomAttributes.Count > 0; }
		}


		/// <inheritdoc/>
		public int HasCustomDebugInformationTag {
			get { return 14; }
		}

		/// <inheritdoc/>
		public bool HasCustomDebugInfos {
			get { return CustomDebugInfos.Count > 0; }
		}

		/// <summary>
		/// Gets all custom debug infos
		/// </summary>
		public ThreadSafe.IList<PdbCustomDebugInfo> CustomDebugInfos {
			get {
				if (customDebugInfos == null)
					InitializeCustomDebugInfos();
				return customDebugInfos;
			}
		}
		/// <summary/>
		protected ThreadSafe.IList<PdbCustomDebugInfo> customDebugInfos;
		/// <summary>Initializes <see cref="customDebugInfos"/></summary>
		protected virtual void InitializeCustomDebugInfos() {
			Interlocked.CompareExchange(ref customDebugInfos, ThreadSafeListCreator.Create<PdbCustomDebugInfo>(), null);
		}
		/// <inheritdoc/>
		public bool HasDeclSecurities {
			get { return DeclSecurities.Count > 0; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="Modules"/> is not empty
		/// </summary>
		public bool HasModules {
			get { return Modules.Count > 0; }
		}

		/// <summary>
		/// Gets the manifest (main) module. This is always the first module in <see cref="Modules"/>.
		/// <c>null</c> is returned if <see cref="Modules"/> is empty.
		/// </summary>
		public ModuleDef ManifestModule {
			get { return Modules.Get(0, null); }
		}

		/// <summary>
		/// Modify <see cref="attributes"/> property: <see cref="attributes"/> =
		/// (<see cref="attributes"/> &amp; <paramref name="andMask"/>) | <paramref name="orMask"/>.
		/// </summary>
		/// <param name="andMask">Value to <c>AND</c></param>
		/// <param name="orMask">Value to OR</param>
		void ModifyAttributes(AssemblyAttributes andMask, AssemblyAttributes orMask) {
#if THREAD_SAFE
			int origVal, newVal;
			do {
				origVal = attributes;
				newVal = (origVal & (int)andMask) | (int)orMask;
			} while (Interlocked.CompareExchange(ref attributes, newVal, origVal) != origVal);
#else
			attributes = (attributes & (int)andMask) | (int)orMask;
#endif
		}

		/// <summary>
		/// Set or clear flags in <see cref="attributes"/>
		/// </summary>
		/// <param name="set"><c>true</c> if flags should be set, <c>false</c> if flags should
		/// be cleared</param>
		/// <param name="flags">Flags to set or clear</param>
		void ModifyAttributes(bool set, AssemblyAttributes flags) {
#if THREAD_SAFE
			int origVal, newVal;
			do {
				origVal = attributes;
				if (set)
					newVal = origVal | (int)flags;
				else
					newVal = origVal & ~(int)flags;
			} while (Interlocked.CompareExchange(ref attributes, newVal, origVal) != origVal);
#else
			if (set)
				attributes |= (int)flags;
			else
				attributes &= ~(int)flags;
#endif
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.PublicKey"/> bit
		/// </summary>
		public bool HasPublicKey {
			get { return ((AssemblyAttributes)attributes & AssemblyAttributes.PublicKey) != 0; }
			set { ModifyAttributes(value, AssemblyAttributes.PublicKey); }
		}

		/// <summary>
		/// Gets/sets the processor architecture
		/// </summary>
		public AssemblyAttributes ProcessorArchitecture {
			get { return (AssemblyAttributes)attributes & AssemblyAttributes.PA_Mask; }
			set { ModifyAttributes(~AssemblyAttributes.PA_Mask, value & AssemblyAttributes.PA_Mask); }
		}

		/// <summary>
		/// Gets/sets the processor architecture
		/// </summary>
		public AssemblyAttributes ProcessorArchitectureFull {
			get { return (AssemblyAttributes)attributes & AssemblyAttributes.PA_FullMask; }
			set { ModifyAttributes(~AssemblyAttributes.PA_FullMask, value & AssemblyAttributes.PA_FullMask); }
		}

		/// <summary>
		/// <c>true</c> if unspecified processor architecture
		/// </summary>
		public bool IsProcessorArchitectureNone {
			get { return ((AssemblyAttributes)attributes & AssemblyAttributes.PA_Mask) == AssemblyAttributes.PA_None; }
		}

		/// <summary>
		/// <c>true</c> if neutral (PE32) architecture
		/// </summary>
		public bool IsProcessorArchitectureMSIL {
			get { return ((AssemblyAttributes)attributes & AssemblyAttributes.PA_Mask) == AssemblyAttributes.PA_MSIL; }
		}

		/// <summary>
		/// <c>true</c> if x86 (PE32) architecture
		/// </summary>
		public bool IsProcessorArchitectureX86 {
			get { return ((AssemblyAttributes)attributes & AssemblyAttributes.PA_Mask) == AssemblyAttributes.PA_x86; }
		}

		/// <summary>
		/// <c>true</c> if IA-64 (PE32+) architecture
		/// </summary>
		public bool IsProcessorArchitectureIA64 {
			get { return ((AssemblyAttributes)attributes & AssemblyAttributes.PA_Mask) == AssemblyAttributes.PA_IA64; }
		}

		/// <summary>
		/// <c>true</c> if x64 (PE32+) architecture
		/// </summary>
		public bool IsProcessorArchitectureX64 {
			get { return ((AssemblyAttributes)attributes & AssemblyAttributes.PA_Mask) == AssemblyAttributes.PA_AMD64; }
		}

		/// <summary>
		/// <c>true</c> if ARM (PE32) architecture
		/// </summary>
		public bool IsProcessorArchitectureARM {
			get { return ((AssemblyAttributes)attributes & AssemblyAttributes.PA_Mask) == AssemblyAttributes.PA_ARM; }
		}

		/// <summary>
		/// <c>true</c> if eg. reference assembly (not runnable)
		/// </summary>
		public bool IsProcessorArchitectureNoPlatform {
			get { return ((AssemblyAttributes)attributes & AssemblyAttributes.PA_Mask) == AssemblyAttributes.PA_NoPlatform; }
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.PA_Specified"/> bit
		/// </summary>
		public bool IsProcessorArchitectureSpecified {
			get { return ((AssemblyAttributes)attributes & AssemblyAttributes.PA_Specified) != 0; }
			set { ModifyAttributes(value, AssemblyAttributes.PA_Specified); }
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.EnableJITcompileTracking"/> bit
		/// </summary>
		public bool EnableJITcompileTracking {
			get { return ((AssemblyAttributes)attributes & AssemblyAttributes.EnableJITcompileTracking) != 0; }
			set { ModifyAttributes(value, AssemblyAttributes.EnableJITcompileTracking); }
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.DisableJITcompileOptimizer"/> bit
		/// </summary>
		public bool DisableJITcompileOptimizer {
			get { return ((AssemblyAttributes)attributes & AssemblyAttributes.DisableJITcompileOptimizer) != 0; }
			set { ModifyAttributes(value, AssemblyAttributes.DisableJITcompileOptimizer); }
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.Retargetable"/> bit
		/// </summary>
		public bool IsRetargetable {
			get { return ((AssemblyAttributes)attributes & AssemblyAttributes.Retargetable) != 0; }
			set { ModifyAttributes(value, AssemblyAttributes.Retargetable); }
		}

		/// <summary>
		/// Gets/sets the content type
		/// </summary>
		public AssemblyAttributes ContentType {
			get { return (AssemblyAttributes)attributes & AssemblyAttributes.ContentType_Mask; }
			set { ModifyAttributes(~AssemblyAttributes.ContentType_Mask, value & AssemblyAttributes.ContentType_Mask); }
		}

		/// <summary>
		/// <c>true</c> if content type is <c>Default</c>
		/// </summary>
		public bool IsContentTypeDefault {
			get { return ((AssemblyAttributes)attributes & AssemblyAttributes.ContentType_Mask) == AssemblyAttributes.ContentType_Default; }
		}

		/// <summary>
		/// <c>true</c> if content type is <c>WindowsRuntime</c>
		/// </summary>
		public bool IsContentTypeWindowsRuntime {
			get { return ((AssemblyAttributes)attributes & AssemblyAttributes.ContentType_Mask) == AssemblyAttributes.ContentType_WindowsRuntime; }
		}

		/// <summary>
		/// Finds a module in this assembly
		/// </summary>
		/// <param name="name">Name of module</param>
		/// <returns>A <see cref="ModuleDef"/> instance or <c>null</c> if it wasn't found.</returns>
		public ModuleDef FindModule(UTF8String name) {
			foreach (var module in Modules.GetSafeEnumerable()) {
				if (module == null)
					continue;
				if (UTF8String.CaseInsensitiveEquals(module.Name, name))
					return module;
			}
			return null;
		}

		/// <summary>
		/// Creates an <see cref="AssemblyDef"/> instance from a file
		/// </summary>
		/// <param name="fileName">File name of an existing .NET assembly</param>
		/// <param name="context">Module context or <c>null</c></param>
		/// <returns>A new <see cref="AssemblyDef"/> instance</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fileName"/> is <c>null</c></exception>
		/// <exception cref="BadImageFormatException">If it's not a .NET assembly (eg. not a .NET file or only a .NET module)</exception>
		public static AssemblyDef Load(string fileName, ModuleContext context) {
			return Load(fileName, new ModuleCreationOptions(context));
		}

		/// <summary>
		/// Creates an <see cref="AssemblyDef"/> instance from a file
		/// </summary>
		/// <param name="fileName">File name of an existing .NET assembly</param>
		/// <param name="options">Module creation options or <c>null</c></param>
		/// <returns>A new <see cref="AssemblyDef"/> instance</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fileName"/> is <c>null</c></exception>
		/// <exception cref="BadImageFormatException">If it's not a .NET assembly (eg. not a .NET file or only a .NET module)</exception>
		public static AssemblyDef Load(string fileName, ModuleCreationOptions options = null) {
			if (fileName == null)
				throw new ArgumentNullException("fileName");
			ModuleDef module = null;
			try {
				module = ModuleDefMD.Load(fileName, options);
				var asm = module.Assembly;
				if (asm == null)
					throw new BadImageFormatException(string.Format("{0} is only a .NET module, not a .NET assembly. Use ModuleDef.Load().", fileName));
				return asm;
			}
			catch {
				if (module != null)
					module.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Creates an <see cref="AssemblyDef"/> instance from a byte[]
		/// </summary>
		/// <param name="data">Contents of a .NET assembly</param>
		/// <param name="context">Module context or <c>null</c></param>
		/// <returns>A new <see cref="AssemblyDef"/> instance</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="data"/> is <c>null</c></exception>
		/// <exception cref="BadImageFormatException">If it's not a .NET assembly (eg. not a .NET file or only a .NET module)</exception>
		public static AssemblyDef Load(byte[] data, ModuleContext context) {
			return Load(data, new ModuleCreationOptions(context));
		}

		/// <summary>
		/// Creates an <see cref="AssemblyDef"/> instance from a byte[]
		/// </summary>
		/// <param name="data">Contents of a .NET assembly</param>
		/// <param name="options">Module creation options or <c>null</c></param>
		/// <returns>A new <see cref="AssemblyDef"/> instance</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="data"/> is <c>null</c></exception>
		/// <exception cref="BadImageFormatException">If it's not a .NET assembly (eg. not a .NET file or only a .NET module)</exception>
		public static AssemblyDef Load(byte[] data, ModuleCreationOptions options = null) {
			if (data == null)
				throw new ArgumentNullException("data");
			ModuleDef module = null;
			try {
				module = ModuleDefMD.Load(data, options);
				var asm = module.Assembly;
				if (asm == null)
					throw new BadImageFormatException(string.Format("{0} is only a .NET module, not a .NET assembly. Use ModuleDef.Load().", module.ToString()));
				return asm;
			}
			catch {
				if (module != null)
					module.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Creates an <see cref="AssemblyDef"/> instance from a memory location
		/// </summary>
		/// <param name="addr">Address of a .NET assembly</param>
		/// <param name="context">Module context or <c>null</c></param>
		/// <returns>A new <see cref="AssemblyDef"/> instance</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="addr"/> is <c>null</c></exception>
		/// <exception cref="BadImageFormatException">If it's not a .NET assembly (eg. not a .NET file or only a .NET module)</exception>
		public static AssemblyDef Load(IntPtr addr, ModuleContext context) {
			return Load(addr, new ModuleCreationOptions(context));
		}

		/// <summary>
		/// Creates an <see cref="AssemblyDef"/> instance from a memory location
		/// </summary>
		/// <param name="addr">Address of a .NET assembly</param>
		/// <param name="options">Module creation options or <c>null</c></param>
		/// <returns>A new <see cref="AssemblyDef"/> instance</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="addr"/> is <c>null</c></exception>
		/// <exception cref="BadImageFormatException">If it's not a .NET assembly (eg. not a .NET file or only a .NET module)</exception>
		public static AssemblyDef Load(IntPtr addr, ModuleCreationOptions options = null) {
			if (addr == IntPtr.Zero)
				throw new ArgumentNullException("addr");
			ModuleDef module = null;
			try {
				module = ModuleDefMD.Load(addr, options);
				var asm = module.Assembly;
				if (asm == null)
					throw new BadImageFormatException(string.Format("{0} (addr: {1:X8}) is only a .NET module, not a .NET assembly. Use ModuleDef.Load().", module.ToString(), addr.ToInt64()));
				return asm;
			}
			catch {
				if (module != null)
					module.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Creates an <see cref="AssemblyDef"/> instance from a stream
		/// </summary>
		/// <remarks>This will read all bytes from the stream and call <see cref="Load(byte[],ModuleContext)"/>.
		/// It's better to use one of the other Load() methods.</remarks>
		/// <param name="stream">The stream</param>
		/// <param name="context">Module context or <c>null</c></param>
		/// <returns>A new <see cref="AssemblyDef"/> instance</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="stream"/> is <c>null</c></exception>
		/// <exception cref="BadImageFormatException">If it's not a .NET assembly (eg. not a .NET file or only a .NET module)</exception>
		public static AssemblyDef Load(Stream stream, ModuleContext context) {
			return Load(stream, new ModuleCreationOptions(context));
		}

		/// <summary>
		/// Creates an <see cref="AssemblyDef"/> instance from a stream
		/// </summary>
		/// <remarks>This will read all bytes from the stream and call <see cref="Load(byte[],ModuleContext)"/>.
		/// It's better to use one of the other Load() methods.</remarks>
		/// <param name="stream">The stream</param>
		/// <param name="options">Module creation options or <c>null</c></param>
		/// <returns>A new <see cref="AssemblyDef"/> instance</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="stream"/> is <c>null</c></exception>
		/// <exception cref="BadImageFormatException">If it's not a .NET assembly (eg. not a .NET file or only a .NET module)</exception>
		public static AssemblyDef Load(Stream stream, ModuleCreationOptions options = null) {
			if (stream == null)
				throw new ArgumentNullException("stream");
			ModuleDef module = null;
			try {
				module = ModuleDefMD.Load(stream, options);
				var asm = module.Assembly;
				if (asm == null)
					throw new BadImageFormatException(string.Format("{0} is only a .NET module, not a .NET assembly. Use ModuleDef.Load().", module.ToString()));
				return asm;
			}
			catch {
				if (module != null)
					module.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Gets the assembly name with the public key
		/// </summary>
		public string GetFullNameWithPublicKey() {
			return GetFullName(publicKey);
		}

		/// <summary>
		/// Gets the assembly name with the public key token
		/// </summary>
		public string GetFullNameWithPublicKeyToken() {
			return GetFullName(publicKey.Token);
		}

		string GetFullName(PublicKeyBase pkBase) {
			return Utils.GetAssemblyNameString(name, version, culture, pkBase, Attributes);
		}

		/// <summary>
		/// Finds a <see cref="TypeDef"/>. For speed, enable <see cref="ModuleDef.EnableTypeDefFindCache"/>
		/// if possible (read the documentation first).
		/// </summary>
		/// <param name="fullName">Full name of the type (no assembly information)</param>
		/// <param name="isReflectionName"><c>true</c> if it's a reflection name, and nested
		/// type names are separated by a <c>+</c> character. If <c>false</c>, nested type names
		/// are separated by a <c>/</c> character.</param>
		/// <returns>An existing <see cref="TypeDef"/> or <c>null</c> if it wasn't found.</returns>
		public TypeDef Find(string fullName, bool isReflectionName) {
			foreach (var module in Modules.GetSafeEnumerable()) {
				if (module == null)
					continue;
				var type = module.Find(fullName, isReflectionName);
				if (type != null)
					return type;
			}
			return null;
		}

		/// <summary>
		/// Finds a <see cref="TypeDef"/>. Its scope (i.e., module or assembly) is ignored when
		/// looking up the type. For speed, enable <see cref="ModuleDef.EnableTypeDefFindCache"/>
		/// if possible (read the documentation first).
		/// </summary>
		/// <param name="typeRef">The type ref</param>
		/// <returns>An existing <see cref="TypeDef"/> or <c>null</c> if it wasn't found.</returns>
		public TypeDef Find(TypeRef typeRef) {
			foreach (var module in Modules.GetSafeEnumerable()) {
				if (module == null)
					continue;
				var type = module.Find(typeRef);
				if (type != null)
					return type;
			}
			return null;
		}

		/// <summary>
		/// Writes the assembly to a file on disk. If the file exists, it will be truncated.
		/// </summary>
		/// <param name="filename">Filename</param>
		/// <param name="options">Writer options</param>
		public void Write(string filename, ModuleWriterOptions options = null) {
			ManifestModule.Write(filename, options);
		}

		/// <summary>
		/// Writes the assembly to a stream.
		/// </summary>
		/// <param name="dest">Destination stream</param>
		/// <param name="options">Writer options</param>
		public void Write(Stream dest, ModuleWriterOptions options = null) {
			ManifestModule.Write(dest, options);
		}

		/// <summary>
		/// Checks whether this assembly is a friend assembly of <paramref name="targetAsm"/>
		/// </summary>
		/// <param name="targetAsm">Target assembly</param>
		public bool IsFriendAssemblyOf(AssemblyDef targetAsm) {
			if (targetAsm == null)
				return false;
			if (this == targetAsm)
				return true;

			// Both must be unsigned or both must be signed according to the
			// InternalsVisibleToAttribute documentation.
			if (PublicKeyBase.IsNullOrEmpty2(publicKey) != PublicKeyBase.IsNullOrEmpty2(targetAsm.PublicKey))
				return false;

			foreach (var ca in targetAsm.CustomAttributes.FindAll("System.Runtime.CompilerServices.InternalsVisibleToAttribute")) {
				if (ca.ConstructorArguments.Count != 1)
					continue;
				var arg = ca.ConstructorArguments.Get(0, default(CAArgument));
				if (arg.Type.GetElementType() != ElementType.String)
					continue;
				var asmName = arg.Value as UTF8String;
				if (UTF8String.IsNull(asmName))
					continue;

				var asmInfo = new AssemblyNameInfo(asmName);
				if (asmInfo.Name != name)
					continue;
				if (!PublicKeyBase.IsNullOrEmpty2(publicKey)) {
					if (!PublicKey.Equals(asmInfo.PublicKeyOrToken as PublicKey))
						continue;
				}
				else if (!PublicKeyBase.IsNullOrEmpty2(asmInfo.PublicKeyOrToken))
					continue;

				return true;
			}

			return false;
		}

		/// <summary>
		/// Adds or updates an existing <c>System.Reflection.AssemblySignatureKeyAttribute</c>
		/// attribute. This attribute is used in enhanced strong naming with key migration.
		/// See http://msdn.microsoft.com/en-us/library/hh415055.aspx
		/// </summary>
		/// <param name="identityPubKey">Identity public key</param>
		/// <param name="identityKey">Identity strong name key pair</param>
		/// <param name="signaturePubKey">Signature public key</param>
		public void UpdateOrCreateAssemblySignatureKeyAttribute(StrongNamePublicKey identityPubKey, StrongNameKey identityKey, StrongNamePublicKey signaturePubKey) {
			var manifestModule = ManifestModule;
			if (manifestModule == null)
				return;

			// Remove all existing attributes
			var ca = CustomAttributes.ExecuteLocked<CustomAttribute, object, CustomAttribute>(null, (tsList, arg) => {
				CustomAttribute foundCa = null;
				for (int i = 0; i < tsList.Count_NoLock(); i++) {
					var caTmp = tsList.Get_NoLock(i);
					if (caTmp.TypeFullName != "System.Reflection.AssemblySignatureKeyAttribute")
						continue;
					tsList.RemoveAt_NoLock(i);
					i--;
					if (foundCa == null)
						foundCa = caTmp;
				}
				return foundCa;
			});

			if (IsValidAssemblySignatureKeyAttribute(ca))
				ca.NamedArguments.Clear();
			else
				ca = CreateAssemblySignatureKeyAttribute();

			var counterSig = StrongNameKey.CreateCounterSignatureAsString(identityPubKey, identityKey, signaturePubKey);
			ca.ConstructorArguments[0] = new CAArgument(manifestModule.CorLibTypes.String, new UTF8String(signaturePubKey.ToString()));
			ca.ConstructorArguments[1] = new CAArgument(manifestModule.CorLibTypes.String, new UTF8String(counterSig));
			CustomAttributes.Add(ca);
		}

		bool IsValidAssemblySignatureKeyAttribute(CustomAttribute ca) {
#if THREAD_SAFE
			return false;
#else
			if (ca == null)
				return false;
			var ctor = ca.Constructor;
			if (ctor == null)
				return false;
			var sig = ctor.MethodSig;
			if (sig == null || sig.Params.Count != 2)
				return false;
			if (sig.Params[0].GetElementType() != ElementType.String)
				return false;
			if (sig.Params[1].GetElementType() != ElementType.String)
				return false;
			if (ca.ConstructorArguments.Count != 2)
				return false;
			return true;
#endif
		}

		CustomAttribute CreateAssemblySignatureKeyAttribute() {
			var manifestModule = ManifestModule;
			var owner = manifestModule.UpdateRowId(new TypeRefUser(manifestModule, "System.Reflection", "AssemblySignatureKeyAttribute", manifestModule.CorLibTypes.AssemblyRef));
			var methodSig = MethodSig.CreateInstance(manifestModule.CorLibTypes.Void, manifestModule.CorLibTypes.String, manifestModule.CorLibTypes.String);
			var ctor = manifestModule.UpdateRowId(new MemberRefUser(manifestModule, MethodDef.InstanceConstructorName, methodSig, owner));
			var ca = new CustomAttribute(ctor);
			ca.ConstructorArguments.Add(new CAArgument(manifestModule.CorLibTypes.String, UTF8String.Empty));
			ca.ConstructorArguments.Add(new CAArgument(manifestModule.CorLibTypes.String, UTF8String.Empty));
			return ca;
		}

		/// <summary>
		/// Gets the original <c>System.Runtime.Versioning.TargetFrameworkAttribute</c> custom attribute information if possible.
		/// It reads this from the original metadata and doesn't use <see cref="CustomAttributes"/>.
		/// Returns false if the custom attribute isn't present or if it is invalid.
		/// </summary>
		/// <param name="framework">Framework name</param>
		/// <param name="version">Version</param>
		/// <param name="profile">Profile</param>
		/// <returns></returns>
		public virtual bool TryGetOriginalTargetFrameworkAttribute(out string framework, out Version version, out string profile) {
			framework = null;
			version = null;
			profile = null;
			return false;
		}

		/// <inheritdoc/>
		void IListListener<ModuleDef>.OnLazyAdd(int index, ref ModuleDef module) {
			if (module == null)
				return;
#if DEBUG
			if (module.Assembly == null)
				throw new InvalidOperationException("Module.Assembly == null");
#endif
		}

		/// <inheritdoc/>
		void IListListener<ModuleDef>.OnAdd(int index, ModuleDef module) {
			if (module == null)
				return;
			if (module.Assembly != null)
				throw new InvalidOperationException("Module already has an assembly. Remove it from that assembly before adding it to this assembly.");
			module.Assembly = this;
		}

		/// <inheritdoc/>
		void IListListener<ModuleDef>.OnRemove(int index, ModuleDef module) {
			if (module != null)
				module.Assembly = null;
		}

		/// <inheritdoc/>
		void IListListener<ModuleDef>.OnResize(int index) {
		}

		/// <inheritdoc/>
		void IListListener<ModuleDef>.OnClear() {
			foreach (var module in Modules.GetEnumerable_NoLock()) {
				if (module != null)
					module.Assembly = null;
			}
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// An Assembly row created by the user and not present in the original .NET file
	/// </summary>
	public class AssemblyDefUser : AssemblyDef {
		/// <summary>
		/// Default constructor
		/// </summary>
		public AssemblyDefUser()
			: this(UTF8String.Empty, new Version(0, 0, 0, 0)) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Simple name</param>
		/// <exception cref="ArgumentNullException">If any of the args is invalid</exception>
		public AssemblyDefUser(UTF8String name)
			: this(name, new Version(0, 0, 0, 0), new PublicKey()) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Simple name</param>
		/// <param name="version">Version</param>
		/// <exception cref="ArgumentNullException">If any of the args is invalid</exception>
		public AssemblyDefUser(UTF8String name, Version version)
			: this(name, version, new PublicKey()) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Simple name</param>
		/// <param name="version">Version</param>
		/// <param name="publicKey">Public key</param>
		/// <exception cref="ArgumentNullException">If any of the args is invalid</exception>
		public AssemblyDefUser(UTF8String name, Version version, PublicKey publicKey)
			: this(name, version, publicKey, UTF8String.Empty) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Simple name</param>
		/// <param name="version">Version</param>
		/// <param name="publicKey">Public key</param>
		/// <param name="locale">Locale</param>
		/// <exception cref="ArgumentNullException">If any of the args is invalid</exception>
		public AssemblyDefUser(UTF8String name, Version version, PublicKey publicKey, UTF8String locale) {
			if ((object)name == null)
				throw new ArgumentNullException("name");
			if (version == null)
				throw new ArgumentNullException("version");
			if ((object)locale == null)
				throw new ArgumentNullException("locale");
			this.modules = new LazyList<ModuleDef>(this);
			this.name = name;
			this.version = version;
			this.publicKey = publicKey ?? new PublicKey();
			this.culture = locale;
			this.attributes = (int)AssemblyAttributes.None;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="asmName">Assembly name info</param>
		/// <exception cref="ArgumentNullException">If <paramref name="asmName"/> is <c>null</c></exception>
		public AssemblyDefUser(AssemblyName asmName)
			: this(new AssemblyNameInfo(asmName)) {
			this.hashAlgorithm = (AssemblyHashAlgorithm)asmName.HashAlgorithm;
			this.attributes = (int)asmName.Flags;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="asmName">Assembly name info</param>
		/// <exception cref="ArgumentNullException">If <paramref name="asmName"/> is <c>null</c></exception>
		public AssemblyDefUser(IAssembly asmName) {
			if (asmName == null)
				throw new ArgumentNullException("asmName");
			this.modules = new LazyList<ModuleDef>(this);
			this.name = asmName.Name;
			this.version = asmName.Version ?? new Version(0, 0, 0, 0);
			this.publicKey = asmName.PublicKeyOrToken as PublicKey ?? new PublicKey();
			this.culture = asmName.Culture;
			this.attributes = (int)AssemblyAttributes.None;
			this.hashAlgorithm = AssemblyHashAlgorithm.SHA1;
		}
	}

	/// <summary>
	/// Created from a row in the Assembly table
	/// </summary>
	sealed class AssemblyDefMD : AssemblyDef, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

		readonly uint origRid;

		/// <inheritdoc/>
		public uint OrigRid {
			get { return origRid; }
		}

		/// <inheritdoc/>
		protected override void InitializeDeclSecurities() {
			var list = readerModule.MetaData.GetDeclSecurityRidList(Table.Assembly, origRid);
			var tmp = new LazyList<DeclSecurity>((int)list.Length, list, (list2, index) => readerModule.ResolveDeclSecurity(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref declSecurities, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeModules() {
			var list = readerModule.GetModuleRidList();
			var tmp = new LazyList<ModuleDef>((int)list.Length + 1, this, list, (list2, index) => {
				ModuleDef module;
				if (index == 0)
					module = readerModule;
				else
					module = readerModule.ReadModule(((RidList)list2)[index - 1], this);
				if (module == null)
					module = new ModuleDefUser("INVALID", Guid.NewGuid());
				module.Assembly = this;
				return module;
			});
			Interlocked.CompareExchange(ref modules, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.MetaData.GetCustomAttributeRidList(Table.Assembly, origRid);
			var tmp = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeCustomDebugInfos() {
			var list = ThreadSafeListCreator.Create<PdbCustomDebugInfo>();
			readerModule.InitializeCustomDebugInfos(new MDToken(MDToken.Table, origRid), new GenericParamContext(), list);
			Interlocked.CompareExchange(ref customDebugInfos, list, null);
		}

		/// <inheritdoc/>
		public override bool TryGetOriginalTargetFrameworkAttribute(out string framework, out Version version, out string profile) {
			if (!hasInitdTFA)
				InitializeTargetFrameworkAttribute();
			framework = tfaFramework;
			version = tfaVersion;
			profile = tfaProfile;
			return tfaReturnValue;
		}
		volatile bool hasInitdTFA;
		string tfaFramework;
		Version tfaVersion;
		string tfaProfile;
		bool tfaReturnValue;

		void InitializeTargetFrameworkAttribute() {
			if (hasInitdTFA)
				return;

			var list = readerModule.MetaData.GetCustomAttributeRidList(Table.Assembly, origRid);
			var gpContext = new GenericParamContext();
			for (int i = 0; i < list.Count; i++) {
				var caRid = list[i];
				var caRow = readerModule.TablesStream.ReadCustomAttributeRow(caRid);
				if (caRow == null)
					continue;
				var caType = readerModule.ResolveCustomAttributeType(caRow.Type, gpContext);
				UTF8String ns, name;
				if (!TryGetName(caType, out ns, out name))
					continue;
				if (ns != nameSystemRuntimeVersioning || name != nameTargetFrameworkAttribute)
					continue;
				var ca = CustomAttributeReader.Read(readerModule, caType, caRow.Value, gpContext);
				if (ca == null || ca.ConstructorArguments.Count != 1)
					continue;
				var s = ca.ConstructorArguments[0].Value as UTF8String;
				if ((object)s == null)
					continue;
				string tmpFramework, tmpProfile;
				Version tmpVersion;
				if (TryCreateTargetFrameworkInfo(s, out tmpFramework, out tmpVersion, out tmpProfile)) {
					tfaFramework = tmpFramework;
					tfaVersion = tmpVersion;
					tfaProfile = tmpProfile;
					tfaReturnValue = true;
					break;
				}
			}

			hasInitdTFA = true;
		}
		static readonly UTF8String nameSystemRuntimeVersioning = new UTF8String("System.Runtime.Versioning");
		static readonly UTF8String nameTargetFrameworkAttribute = new UTF8String("TargetFrameworkAttribute");

		static bool TryGetName(ICustomAttributeType caType, out UTF8String ns, out UTF8String name) {
			ITypeDefOrRef type;
			var mr = caType as MemberRef;
			if (mr != null)
				type = mr.DeclaringType;
			else {
				var md = caType as MethodDef;
				type = md == null ? null : md.DeclaringType;
			}
			var tr = type as TypeRef;
			if (tr != null) {
				ns = tr.Namespace;
				name = tr.Name;
				return true;
			}
			var td = type as TypeDef;
			if (td != null) {
				ns = td.Namespace;
				name = td.Name;
				return true;
			}
			ns = null;
			name = null;
			return false;
		}

		static bool TryCreateTargetFrameworkInfo(string attrString, out string framework, out Version version, out string profile) {
			framework = null;
			version = null;
			profile = null;

			// See corclr/src/mscorlib/src/System/Runtime/Versioning/BinaryCompatibility.cs
			var values = attrString.Split(new char[] { ',' });
			if (values.Length < 2 || values.Length > 3)
				return false;
			var frameworkRes = values[0].Trim();
			if (frameworkRes.Length == 0)
				return false;

			Version versionRes = null;
			string profileRes = null;
			for (int i = 1; i < values.Length; i++) {
				var kvp = values[i].Split('=');
				if (kvp.Length != 2)
					return false;

				var key = kvp[0].Trim();
				var value = kvp[1].Trim();

				if (key.Equals("Version", StringComparison.OrdinalIgnoreCase)) {
					if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase))
						value = value.Substring(1);
					if (!TryParse(value, out versionRes))
						return false;
					versionRes = new Version(versionRes.Major, versionRes.Minor, versionRes.Build == -1 ? 0 : versionRes.Build, 0);
				}
				else if (key.Equals("Profile", StringComparison.OrdinalIgnoreCase)) {
					if (!string.IsNullOrEmpty(value))
						profileRes = value;
				}
			}
			if (versionRes == null)
				return false;

			framework = frameworkRes;
			version = versionRes;
			profile = profileRes;
			return true;
		}

		static int ParseInt32(string s) {
			int res;
			return int.TryParse(s, out res) ? res : 0;
		}

		static bool TryParse(string s, out Version version) {
			Match m;

			m = Regex.Match(s, @"^(\d+)\.(\d+)$");
			if (m.Groups.Count == 3) {
				version = new Version(ParseInt32(m.Groups[1].Value), ParseInt32(m.Groups[2].Value));
				return true;
			}

			m = Regex.Match(s, @"^(\d+)\.(\d+)\.(\d+)$");
			if (m.Groups.Count == 4) {
				version = new Version(ParseInt32(m.Groups[1].Value), ParseInt32(m.Groups[2].Value), ParseInt32(m.Groups[3].Value));
				return true;
			}

			m = Regex.Match(s, @"^(\d+)\.(\d+)\.(\d+)\.(\d+)$");
			if (m.Groups.Count == 5) {
				version = new Version(ParseInt32(m.Groups[1].Value), ParseInt32(m.Groups[2].Value), ParseInt32(m.Groups[3].Value), ParseInt32(m.Groups[4].Value));
				return true;
			}

			version = null;
			return false;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>Assembly</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public AssemblyDefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.AssemblyTable.IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("Assembly rid {0} does not exist", rid));
#endif
			this.origRid = rid;
			this.rid = rid;
			this.readerModule = readerModule;
			if (rid != 1)
				this.modules = new LazyList<ModuleDef>(this);
			uint publicKey, name;
			uint culture = readerModule.TablesStream.ReadAssemblyRow(origRid, out this.hashAlgorithm, out this.version, out this.attributes, out publicKey, out name);
			this.name = readerModule.StringsStream.ReadNoNull(name);
			this.culture = readerModule.StringsStream.ReadNoNull(culture);
			this.publicKey = new PublicKey(readerModule.BlobStream.Read(publicKey));
		}
	}
}
