// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Reflection;
using System.Threading;
using dnlib.Utils;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using System.Text.RegularExpressions;
using dnlib.DotNet.Pdb;
using System.Collections.Generic;
using System.Diagnostics;

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
		public MDToken MDToken => new MDToken(Table.Assembly, rid);

		/// <inheritdoc/>
		public uint Rid {
			get => rid;
			set => rid = value;
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag => 14;

		/// <inheritdoc/>
		public int HasDeclSecurityTag => 2;

		/// <summary>
		/// From column Assembly.HashAlgId
		/// </summary>
		public AssemblyHashAlgorithm HashAlgorithm {
			get => hashAlgorithm;
			set => hashAlgorithm = value;
		}
		/// <summary/>
		protected AssemblyHashAlgorithm hashAlgorithm;

		/// <summary>
		/// From columns Assembly.MajorVersion, Assembly.MinorVersion, Assembly.BuildNumber,
		/// Assembly.RevisionNumber.
		/// </summary>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is <c>null</c></exception>
		public Version Version {
			get => version;
			set => version = value ?? throw new ArgumentNullException(nameof(value));
		}
		/// <summary/>
		protected Version version;

		/// <summary>
		/// From column Assembly.Flags
		/// </summary>
		public AssemblyAttributes Attributes {
			get => (AssemblyAttributes)attributes;
			set => attributes = (int)value;
		}
		/// <summary>Attributes</summary>
		protected int attributes;

		/// <summary>
		/// From column Assembly.PublicKey
		/// </summary>
		/// <remarks>An empty <see cref="PublicKey"/> is created if the caller writes <c>null</c></remarks>
		public PublicKey PublicKey {
			get => publicKey;
			set => publicKey = value ?? new PublicKey();
		}
		/// <summary/>
		protected PublicKey publicKey;

		/// <summary>
		/// Gets the public key token which is calculated from <see cref="PublicKey"/>
		/// </summary>
		public PublicKeyToken PublicKeyToken => publicKey.Token;

		/// <summary>
		/// From column Assembly.Name
		/// </summary>
		public UTF8String Name {
			get => name;
			set => name = value;
		}
		/// <summary>Name</summary>
		protected UTF8String name;

		/// <summary>
		/// From column Assembly.Locale
		/// </summary>
		public UTF8String Culture {
			get => culture;
			set => culture = value;
		}
		/// <summary>Name</summary>
		protected UTF8String culture;

		/// <inheritdoc/>
		public IList<DeclSecurity> DeclSecurities {
			get {
				if (declSecurities is null)
					InitializeDeclSecurities();
				return declSecurities;
			}
		}
		/// <summary/>
		protected IList<DeclSecurity> declSecurities;
		/// <summary>Initializes <see cref="declSecurities"/></summary>
		protected virtual void InitializeDeclSecurities() =>
			Interlocked.CompareExchange(ref declSecurities, new List<DeclSecurity>(), null);

		/// <inheritdoc/>
		public PublicKeyBase PublicKeyOrToken => publicKey;

		/// <inheritdoc/>
		public string FullName => GetFullNameWithPublicKeyToken();

		/// <inheritdoc/>
		public string FullNameToken => GetFullNameWithPublicKeyToken();

		/// <summary>
		/// Gets all modules. The first module is always the <see cref="ManifestModule"/>.
		/// </summary>
		public IList<ModuleDef> Modules {
			get {
				if (modules is null)
					InitializeModules();
				return modules;
			}
		}
		/// <summary/>
		protected LazyList<ModuleDef> modules;
		/// <summary>Initializes <see cref="modules"/></summary>
		protected virtual void InitializeModules() =>
			Interlocked.CompareExchange(ref modules, new LazyList<ModuleDef>(this), null);

		/// <summary>
		/// Gets all custom attributes
		/// </summary>
		public CustomAttributeCollection CustomAttributes {
			get {
				if (customAttributes is null)
					InitializeCustomAttributes();
				return customAttributes;
			}
		}
		/// <summary/>
		protected CustomAttributeCollection customAttributes;
		/// <summary>Initializes <see cref="customAttributes"/></summary>
		protected virtual void InitializeCustomAttributes() =>
			Interlocked.CompareExchange(ref customAttributes, new CustomAttributeCollection(), null);

		/// <inheritdoc/>
		public bool HasCustomAttributes => CustomAttributes.Count > 0;


		/// <inheritdoc/>
		public int HasCustomDebugInformationTag => 14;

		/// <inheritdoc/>
		public bool HasCustomDebugInfos => CustomDebugInfos.Count > 0;

		/// <summary>
		/// Gets all custom debug infos
		/// </summary>
		public IList<PdbCustomDebugInfo> CustomDebugInfos {
			get {
				if (customDebugInfos is null)
					InitializeCustomDebugInfos();
				return customDebugInfos;
			}
		}
		/// <summary/>
		protected IList<PdbCustomDebugInfo> customDebugInfos;
		/// <summary>Initializes <see cref="customDebugInfos"/></summary>
		protected virtual void InitializeCustomDebugInfos() =>
			Interlocked.CompareExchange(ref customDebugInfos, new List<PdbCustomDebugInfo>(), null);
		/// <inheritdoc/>
		public bool HasDeclSecurities => DeclSecurities.Count > 0;

		/// <summary>
		/// <c>true</c> if <see cref="Modules"/> is not empty
		/// </summary>
		public bool HasModules => Modules.Count > 0;

		/// <summary>
		/// Gets the manifest (main) module. This is always the first module in <see cref="Modules"/>.
		/// <c>null</c> is returned if <see cref="Modules"/> is empty.
		/// </summary>
		public ModuleDef ManifestModule => Modules.Count == 0 ? null : Modules[0];

		/// <summary>
		/// Modify <see cref="attributes"/> property: <see cref="attributes"/> =
		/// (<see cref="attributes"/> &amp; <paramref name="andMask"/>) | <paramref name="orMask"/>.
		/// </summary>
		/// <param name="andMask">Value to <c>AND</c></param>
		/// <param name="orMask">Value to OR</param>
		void ModifyAttributes(AssemblyAttributes andMask, AssemblyAttributes orMask) =>
			attributes = (attributes & (int)andMask) | (int)orMask;

		/// <summary>
		/// Set or clear flags in <see cref="attributes"/>
		/// </summary>
		/// <param name="set"><c>true</c> if flags should be set, <c>false</c> if flags should
		/// be cleared</param>
		/// <param name="flags">Flags to set or clear</param>
		void ModifyAttributes(bool set, AssemblyAttributes flags) {
			if (set)
				attributes |= (int)flags;
			else
				attributes &= ~(int)flags;
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.PublicKey"/> bit
		/// </summary>
		public bool HasPublicKey {
			get => ((AssemblyAttributes)attributes & AssemblyAttributes.PublicKey) != 0;
			set => ModifyAttributes(value, AssemblyAttributes.PublicKey);
		}

		/// <summary>
		/// Gets/sets the processor architecture
		/// </summary>
		public AssemblyAttributes ProcessorArchitecture {
			get => (AssemblyAttributes)attributes & AssemblyAttributes.PA_Mask;
			set => ModifyAttributes(~AssemblyAttributes.PA_Mask, value & AssemblyAttributes.PA_Mask);
		}

		/// <summary>
		/// Gets/sets the processor architecture
		/// </summary>
		public AssemblyAttributes ProcessorArchitectureFull {
			get => (AssemblyAttributes)attributes & AssemblyAttributes.PA_FullMask;
			set => ModifyAttributes(~AssemblyAttributes.PA_FullMask, value & AssemblyAttributes.PA_FullMask);
		}

		/// <summary>
		/// <c>true</c> if unspecified processor architecture
		/// </summary>
		public bool IsProcessorArchitectureNone => ((AssemblyAttributes)attributes & AssemblyAttributes.PA_Mask) == AssemblyAttributes.PA_None;

		/// <summary>
		/// <c>true</c> if neutral (PE32) architecture
		/// </summary>
		public bool IsProcessorArchitectureMSIL => ((AssemblyAttributes)attributes & AssemblyAttributes.PA_Mask) == AssemblyAttributes.PA_MSIL;

		/// <summary>
		/// <c>true</c> if x86 (PE32) architecture
		/// </summary>
		public bool IsProcessorArchitectureX86 => ((AssemblyAttributes)attributes & AssemblyAttributes.PA_Mask) == AssemblyAttributes.PA_x86;

		/// <summary>
		/// <c>true</c> if IA-64 (PE32+) architecture
		/// </summary>
		public bool IsProcessorArchitectureIA64 => ((AssemblyAttributes)attributes & AssemblyAttributes.PA_Mask) == AssemblyAttributes.PA_IA64;

		/// <summary>
		/// <c>true</c> if x64 (PE32+) architecture
		/// </summary>
		public bool IsProcessorArchitectureX64 => ((AssemblyAttributes)attributes & AssemblyAttributes.PA_Mask) == AssemblyAttributes.PA_AMD64;

		/// <summary>
		/// <c>true</c> if ARM (PE32) architecture
		/// </summary>
		public bool IsProcessorArchitectureARM => ((AssemblyAttributes)attributes & AssemblyAttributes.PA_Mask) == AssemblyAttributes.PA_ARM;

		/// <summary>
		/// <c>true</c> if eg. reference assembly (not runnable)
		/// </summary>
		public bool IsProcessorArchitectureNoPlatform => ((AssemblyAttributes)attributes & AssemblyAttributes.PA_Mask) == AssemblyAttributes.PA_NoPlatform;

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.PA_Specified"/> bit
		/// </summary>
		public bool IsProcessorArchitectureSpecified {
			get => ((AssemblyAttributes)attributes & AssemblyAttributes.PA_Specified) != 0;
			set => ModifyAttributes(value, AssemblyAttributes.PA_Specified);
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.EnableJITcompileTracking"/> bit
		/// </summary>
		public bool EnableJITcompileTracking {
			get => ((AssemblyAttributes)attributes & AssemblyAttributes.EnableJITcompileTracking) != 0;
			set => ModifyAttributes(value, AssemblyAttributes.EnableJITcompileTracking);
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.DisableJITcompileOptimizer"/> bit
		/// </summary>
		public bool DisableJITcompileOptimizer {
			get => ((AssemblyAttributes)attributes & AssemblyAttributes.DisableJITcompileOptimizer) != 0;
			set => ModifyAttributes(value, AssemblyAttributes.DisableJITcompileOptimizer);
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.Retargetable"/> bit
		/// </summary>
		public bool IsRetargetable {
			get => ((AssemblyAttributes)attributes & AssemblyAttributes.Retargetable) != 0;
			set => ModifyAttributes(value, AssemblyAttributes.Retargetable);
		}

		/// <summary>
		/// Gets/sets the content type
		/// </summary>
		public AssemblyAttributes ContentType {
			get => (AssemblyAttributes)attributes & AssemblyAttributes.ContentType_Mask;
			set => ModifyAttributes(~AssemblyAttributes.ContentType_Mask, value & AssemblyAttributes.ContentType_Mask);
		}

		/// <summary>
		/// <c>true</c> if content type is <c>Default</c>
		/// </summary>
		public bool IsContentTypeDefault => ((AssemblyAttributes)attributes & AssemblyAttributes.ContentType_Mask) == AssemblyAttributes.ContentType_Default;

		/// <summary>
		/// <c>true</c> if content type is <c>WindowsRuntime</c>
		/// </summary>
		public bool IsContentTypeWindowsRuntime => ((AssemblyAttributes)attributes & AssemblyAttributes.ContentType_Mask) == AssemblyAttributes.ContentType_WindowsRuntime;

		/// <summary>
		/// Finds a module in this assembly
		/// </summary>
		/// <param name="name">Name of module</param>
		/// <returns>A <see cref="ModuleDef"/> instance or <c>null</c> if it wasn't found.</returns>
		public ModuleDef FindModule(UTF8String name) {
			var modules = Modules;
			int count = modules.Count;
			for (int i = 0; i < count; i++) {
				var module = modules[i];
				if (module is null)
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
		public static AssemblyDef Load(string fileName, ModuleContext context) =>
			Load(fileName, new ModuleCreationOptions(context));

		/// <summary>
		/// Creates an <see cref="AssemblyDef"/> instance from a file
		/// </summary>
		/// <param name="fileName">File name of an existing .NET assembly</param>
		/// <param name="options">Module creation options or <c>null</c></param>
		/// <returns>A new <see cref="AssemblyDef"/> instance</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fileName"/> is <c>null</c></exception>
		/// <exception cref="BadImageFormatException">If it's not a .NET assembly (eg. not a .NET file or only a .NET module)</exception>
		public static AssemblyDef Load(string fileName, ModuleCreationOptions options = null) {
			if (fileName is null)
				throw new ArgumentNullException(nameof(fileName));
			ModuleDef module = null;
			try {
				module = ModuleDefMD.Load(fileName, options);
				var asm = module.Assembly;
				if (asm is null)
					throw new BadImageFormatException($"{fileName} is only a .NET module, not a .NET assembly. Use ModuleDef.Load().");
				return asm;
			}
			catch {
				if (!(module is null))
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
		public static AssemblyDef Load(byte[] data, ModuleContext context) =>
			Load(data, new ModuleCreationOptions(context));

		/// <summary>
		/// Creates an <see cref="AssemblyDef"/> instance from a byte[]
		/// </summary>
		/// <param name="data">Contents of a .NET assembly</param>
		/// <param name="options">Module creation options or <c>null</c></param>
		/// <returns>A new <see cref="AssemblyDef"/> instance</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="data"/> is <c>null</c></exception>
		/// <exception cref="BadImageFormatException">If it's not a .NET assembly (eg. not a .NET file or only a .NET module)</exception>
		public static AssemblyDef Load(byte[] data, ModuleCreationOptions options = null) {
			if (data is null)
				throw new ArgumentNullException(nameof(data));
			ModuleDef module = null;
			try {
				module = ModuleDefMD.Load(data, options);
				var asm = module.Assembly;
				if (asm is null)
					throw new BadImageFormatException($"{module.ToString()} is only a .NET module, not a .NET assembly. Use ModuleDef.Load().");
				return asm;
			}
			catch {
				if (!(module is null))
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
		public static AssemblyDef Load(IntPtr addr, ModuleContext context) =>
			Load(addr, new ModuleCreationOptions(context));

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
				throw new ArgumentNullException(nameof(addr));
			ModuleDef module = null;
			try {
				module = ModuleDefMD.Load(addr, options);
				var asm = module.Assembly;
				if (asm is null)
					throw new BadImageFormatException($"{module.ToString()} (addr: {addr.ToInt64():X8}) is only a .NET module, not a .NET assembly. Use ModuleDef.Load().");
				return asm;
			}
			catch {
				if (!(module is null))
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
		public static AssemblyDef Load(Stream stream, ModuleContext context) =>
			Load(stream, new ModuleCreationOptions(context));

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
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));
			ModuleDef module = null;
			try {
				module = ModuleDefMD.Load(stream, options);
				var asm = module.Assembly;
				if (asm is null)
					throw new BadImageFormatException($"{module.ToString()} is only a .NET module, not a .NET assembly. Use ModuleDef.Load().");
				return asm;
			}
			catch {
				if (!(module is null))
					module.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Gets the assembly name with the public key
		/// </summary>
		public string GetFullNameWithPublicKey() => GetFullName(publicKey);

		/// <summary>
		/// Gets the assembly name with the public key token
		/// </summary>
		public string GetFullNameWithPublicKeyToken() => GetFullName(publicKey.Token);

		string GetFullName(PublicKeyBase pkBase) => Utils.GetAssemblyNameString(name, version, culture, pkBase, Attributes);

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
			var modules = Modules;
			int count = modules.Count;
			for (int i = 0; i < count; i++) {
				var module = modules[i];
				if (module is null)
					continue;
				var type = module.Find(fullName, isReflectionName);
				if (!(type is null))
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
			var modules = Modules;
			int count = modules.Count;
			for (int i = 0; i < count; i++) {
				var module = modules[i];
				if (module is null)
					continue;
				var type = module.Find(typeRef);
				if (!(type is null))
					return type;
			}
			return null;
		}

		/// <summary>
		/// Writes the assembly to a file on disk. If the file exists, it will be truncated.
		/// </summary>
		/// <param name="filename">Filename</param>
		/// <param name="options">Writer options</param>
		public void Write(string filename, ModuleWriterOptions options = null) =>
			ManifestModule.Write(filename, options);

		/// <summary>
		/// Writes the assembly to a stream.
		/// </summary>
		/// <param name="dest">Destination stream</param>
		/// <param name="options">Writer options</param>
		public void Write(Stream dest, ModuleWriterOptions options = null) =>
			ManifestModule.Write(dest, options);

		/// <summary>
		/// Checks whether this assembly is a friend assembly of <paramref name="targetAsm"/>
		/// </summary>
		/// <param name="targetAsm">Target assembly</param>
		public bool IsFriendAssemblyOf(AssemblyDef targetAsm) {
			if (targetAsm is null)
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
				var arg = ca.ConstructorArguments.Count == 0 ? default : ca.ConstructorArguments[0];
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
			if (manifestModule is null)
				return;

			// Remove all existing attributes
			CustomAttribute ca = null;
			for (int i = 0; i < CustomAttributes.Count; i++) {
				var caTmp = CustomAttributes[i];
				if (caTmp.TypeFullName != "System.Reflection.AssemblySignatureKeyAttribute")
					continue;
				CustomAttributes.RemoveAt(i);
				i--;
				if (ca is null)
					ca = caTmp;
			}

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
			if (dnlib.Settings.IsThreadSafe)
				return false;
			if (ca is null)
				return false;
			var ctor = ca.Constructor;
			if (ctor is null)
				return false;
			var sig = ctor.MethodSig;
			if (sig is null || sig.Params.Count != 2)
				return false;
			if (sig.Params[0].GetElementType() != ElementType.String)
				return false;
			if (sig.Params[1].GetElementType() != ElementType.String)
				return false;
			if (ca.ConstructorArguments.Count != 2)
				return false;
			return true;
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
			if (module is null)
				return;
#if DEBUG
			if (module.Assembly is null)
				throw new InvalidOperationException("Module.Assembly is null");
#endif
		}

		/// <inheritdoc/>
		void IListListener<ModuleDef>.OnAdd(int index, ModuleDef module) {
			if (module is null)
				return;
			if (!(module.Assembly is null))
				throw new InvalidOperationException("Module already has an assembly. Remove it from that assembly before adding it to this assembly.");
			module.Assembly = this;
		}

		/// <inheritdoc/>
		void IListListener<ModuleDef>.OnRemove(int index, ModuleDef module) {
			if (!(module is null))
				module.Assembly = null;
		}

		/// <inheritdoc/>
		void IListListener<ModuleDef>.OnResize(int index) {
		}

		/// <inheritdoc/>
		void IListListener<ModuleDef>.OnClear() {
			foreach (var module in modules.GetEnumerable_NoLock()) {
				if (!(module is null))
					module.Assembly = null;
			}
		}

		/// <inheritdoc/>
		public override string ToString() => FullName;
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
			if (name is null)
				throw new ArgumentNullException(nameof(name));
			if (locale is null)
				throw new ArgumentNullException(nameof(locale));
			modules = new LazyList<ModuleDef>(this);
			this.name = name;
			this.version = version ?? throw new ArgumentNullException(nameof(version));
			this.publicKey = publicKey ?? new PublicKey();
			culture = locale;
			attributes = (int)AssemblyAttributes.None;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="asmName">Assembly name info</param>
		/// <exception cref="ArgumentNullException">If <paramref name="asmName"/> is <c>null</c></exception>
		public AssemblyDefUser(AssemblyName asmName)
			: this(new AssemblyNameInfo(asmName)) {
			hashAlgorithm = (AssemblyHashAlgorithm)asmName.HashAlgorithm;
			attributes = (int)asmName.Flags;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="asmName">Assembly name info</param>
		/// <exception cref="ArgumentNullException">If <paramref name="asmName"/> is <c>null</c></exception>
		public AssemblyDefUser(IAssembly asmName) {
			if (asmName is null)
				throw new ArgumentNullException(nameof(asmName));
			modules = new LazyList<ModuleDef>(this);
			name = asmName.Name;
			version = asmName.Version ?? new Version(0, 0, 0, 0);
			publicKey = asmName.PublicKeyOrToken as PublicKey ?? new PublicKey();
			culture = asmName.Culture;
			attributes = (int)AssemblyAttributes.None;
			hashAlgorithm = AssemblyHashAlgorithm.SHA1;
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
		public uint OrigRid => origRid;

		/// <inheritdoc/>
		protected override void InitializeDeclSecurities() {
			var list = readerModule.Metadata.GetDeclSecurityRidList(Table.Assembly, origRid);
			var tmp = new LazyList<DeclSecurity, RidList>(list.Count, list, (list2, index) => readerModule.ResolveDeclSecurity(list2[index]));
			Interlocked.CompareExchange(ref declSecurities, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeModules() {
			var list = readerModule.GetModuleRidList();
			var tmp = new LazyList<ModuleDef, RidList>(list.Count + 1, this, list, (list2, index) => {
				ModuleDef module;
				if (index == 0)
					module = readerModule;
				else
					module = readerModule.ReadModule(list2[index - 1], this);
				if (module is null)
					module = new ModuleDefUser("INVALID", Guid.NewGuid());
				module.Assembly = this;
				return module;
			});
			Interlocked.CompareExchange(ref modules, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.Metadata.GetCustomAttributeRidList(Table.Assembly, origRid);
			var tmp = new CustomAttributeCollection(list.Count, list, (list2, index) => readerModule.ReadCustomAttribute(list[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeCustomDebugInfos() {
			var list = new List<PdbCustomDebugInfo>();
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

			var list = readerModule.Metadata.GetCustomAttributeRidList(Table.Assembly, origRid);
			var gpContext = new GenericParamContext();
			for (int i = 0; i < list.Count; i++) {
				var caRid = list[i];
				if (!readerModule.TablesStream.TryReadCustomAttributeRow(caRid, out var caRow))
					continue;
				var caType = readerModule.ResolveCustomAttributeType(caRow.Type, gpContext);
				if (!TryGetName(caType, out var ns, out var name))
					continue;
				if (ns != nameSystemRuntimeVersioning || name != nameTargetFrameworkAttribute)
					continue;
				var ca = CustomAttributeReader.Read(readerModule, caType, caRow.Value, gpContext);
				if (ca is null || ca.ConstructorArguments.Count != 1)
					continue;
				var s = ca.ConstructorArguments[0].Value as UTF8String;
				if (s is null)
					continue;
				if (TryCreateTargetFrameworkInfo(s, out var tmpFramework, out var tmpVersion, out var tmpProfile)) {
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
			if (caType is MemberRef mr)
				type = mr.DeclaringType;
			else
				type = (caType as MethodDef)?.DeclaringType;
			if (type is TypeRef tr) {
				ns = tr.Namespace;
				name = tr.Name;
				return true;
			}
			if (type is TypeDef td) {
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
			if (versionRes is null)
				return false;

			framework = frameworkRes;
			version = versionRes;
			profile = profileRes;
			return true;
		}

		static int ParseInt32(string s) => int.TryParse(s, out int res) ? res : 0;

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
			if (readerModule is null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.AssemblyTable.IsInvalidRID(rid))
				throw new BadImageFormatException($"Assembly rid {rid} does not exist");
#endif
			origRid = rid;
			this.rid = rid;
			this.readerModule = readerModule;
			if (rid != 1)
				modules = new LazyList<ModuleDef>(this);
			bool b = readerModule.TablesStream.TryReadAssemblyRow(origRid, out var row);
			Debug.Assert(b);
			hashAlgorithm = (AssemblyHashAlgorithm)row.HashAlgId;
			version = new Version(row.MajorVersion, row.MinorVersion, row.BuildNumber, row.RevisionNumber);
			attributes = (int)row.Flags;
			name = readerModule.StringsStream.ReadNoNull(row.Name);
			culture = readerModule.StringsStream.ReadNoNull(row.Locale);
			publicKey = new PublicKey(readerModule.BlobStream.Read(row.PublicKey));
		}
	}
}
