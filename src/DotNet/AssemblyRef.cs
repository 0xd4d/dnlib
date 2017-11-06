// dnlib: See LICENSE.txt for more info

using System;
using System.Reflection;
using System.Threading;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb;
using dnlib.Threading;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the AssemblyRef table
	/// </summary>
	public abstract class AssemblyRef : IHasCustomAttribute, IImplementation, IResolutionScope, IHasCustomDebugInformation, IAssembly, IScope {
		/// <summary>
		/// An assembly ref that can be used to indicate that it references the current assembly
		/// when the current assembly is not known (eg. a type string without any assembly info
		/// when it references a type in the current assembly).
		/// </summary>
		public static readonly AssemblyRef CurrentAssembly = new AssemblyRefUser("<<<CURRENT_ASSEMBLY>>>");

		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.AssemblyRef, rid); }
		}

		/// <inheritdoc/>
		public uint Rid {
			get { return rid; }
			set { rid = value; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 15; }
		}

		/// <inheritdoc/>
		public int ImplementationTag {
			get { return 1; }
		}

		/// <inheritdoc/>
		public int ResolutionScopeTag {
			get { return 2; }
		}

		/// <inheritdoc/>
		public ScopeType ScopeType {
			get { return ScopeType.AssemblyRef; }
		}

		/// <inheritdoc/>
		public string ScopeName {
			get { return FullName; }
		}

		/// <summary>
		/// From columns AssemblyRef.MajorVersion, AssemblyRef.MinorVersion,
		/// AssemblyRef.BuildNumber, AssemblyRef.RevisionNumber
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
		/// From column AssemblyRef.Flags
		/// </summary>
		public AssemblyAttributes Attributes {
			get { return (AssemblyAttributes)attributes; }
			set { attributes = (int)value; }
		}
		/// <summary>Attributes</summary>
		protected int attributes;

		/// <summary>
		/// From column AssemblyRef.PublicKeyOrToken
		/// </summary>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is <c>null</c></exception>
		public PublicKeyBase PublicKeyOrToken {
			get { return publicKeyOrToken; }
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				publicKeyOrToken = value;
			}
		}
		/// <summary/>
		protected PublicKeyBase publicKeyOrToken;

		/// <summary>
		/// From column AssemblyRef.Name
		/// </summary>
		public UTF8String Name {
			get { return name; }
			set { name = value; }
		}
		/// <summary>Name</summary>
		protected UTF8String name;

		/// <summary>
		/// From column AssemblyRef.Locale
		/// </summary>
		public UTF8String Culture {
			get { return culture; }
			set { culture = value; }
		}
		/// <summary>Culture</summary>
		protected UTF8String culture;

		/// <summary>
		/// From column AssemblyRef.HashValue
		/// </summary>
		public byte[] Hash {
			get { return hashValue; }
			set { hashValue = value; }
		}
		/// <summary/>
		protected byte[] hashValue;

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
			get { return 15; }
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
		public string FullName {
			get { return FullNameToken; }
		}

		/// <summary>
		/// Same as <see cref="FullName"/>, except that it uses the <c>PublicKey</c> if available.
		/// </summary>
		public string RealFullName {
			get { return Utils.GetAssemblyNameString(name, version, culture, publicKeyOrToken, Attributes); }
		}

		/// <summary>
		/// Gets the full name of the assembly but use a public key token
		/// </summary>
		public string FullNameToken {
			get { return Utils.GetAssemblyNameString(name, version, culture, PublicKeyBase.ToPublicKeyToken(publicKeyOrToken), Attributes); }
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

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// An AssemblyRef row created by the user and not present in the original .NET file
	/// </summary>
	public class AssemblyRefUser : AssemblyRef {
		/// <summary>
		/// Creates a reference to CLR 1.0's mscorlib
		/// </summary>
		public static AssemblyRefUser CreateMscorlibReferenceCLR10() {
			return new AssemblyRefUser("mscorlib", new Version(1, 0, 3300, 0), new PublicKeyToken("b77a5c561934e089"));
		}

		/// <summary>
		/// Creates a reference to CLR 1.1's mscorlib
		/// </summary>
		public static AssemblyRefUser CreateMscorlibReferenceCLR11() {
			return new AssemblyRefUser("mscorlib", new Version(1, 0, 5000, 0), new PublicKeyToken("b77a5c561934e089"));
		}

		/// <summary>
		/// Creates a reference to CLR 2.0's mscorlib
		/// </summary>
		public static AssemblyRefUser CreateMscorlibReferenceCLR20() {
			return new AssemblyRefUser("mscorlib", new Version(2, 0, 0, 0), new PublicKeyToken("b77a5c561934e089"));
		}

		/// <summary>
		/// Creates a reference to CLR 4.0's mscorlib
		/// </summary>
		public static AssemblyRefUser CreateMscorlibReferenceCLR40() {
			return new AssemblyRefUser("mscorlib", new Version(4, 0, 0, 0), new PublicKeyToken("b77a5c561934e089"));
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public AssemblyRefUser()
			: this(UTF8String.Empty) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Simple name</param>
		/// <exception cref="ArgumentNullException">If any of the args is invalid</exception>
		public AssemblyRefUser(UTF8String name)
			: this(name, new Version(0, 0, 0, 0)) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Simple name</param>
		/// <param name="version">Version</param>
		/// <exception cref="ArgumentNullException">If any of the args is invalid</exception>
		public AssemblyRefUser(UTF8String name, Version version)
			: this(name, version, new PublicKey()) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Simple name</param>
		/// <param name="version">Version</param>
		/// <param name="publicKey">Public key or public key token</param>
		/// <exception cref="ArgumentNullException">If any of the args is invalid</exception>
		public AssemblyRefUser(UTF8String name, Version version, PublicKeyBase publicKey)
			: this(name, version, publicKey, UTF8String.Empty) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Simple name</param>
		/// <param name="version">Version</param>
		/// <param name="publicKey">Public key or public key token</param>
		/// <param name="locale">Locale</param>
		/// <exception cref="ArgumentNullException">If any of the args is invalid</exception>
		public AssemblyRefUser(UTF8String name, Version version, PublicKeyBase publicKey, UTF8String locale) {
			if ((object)name == null)
				throw new ArgumentNullException("name");
			if (version == null)
				throw new ArgumentNullException("version");
			if ((object)locale == null)
				throw new ArgumentNullException("locale");
			this.name = name;
			this.version = version;
			this.publicKeyOrToken = publicKey;
			this.culture = locale;
			this.attributes = (int)(publicKey is PublicKey ? AssemblyAttributes.PublicKey : AssemblyAttributes.None);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="asmName">Assembly name info</param>
		/// <exception cref="ArgumentNullException">If <paramref name="asmName"/> is <c>null</c></exception>
		public AssemblyRefUser(AssemblyName asmName)
			: this(new AssemblyNameInfo(asmName)) {
			this.attributes = (int)asmName.Flags;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="assembly">Assembly</param>
		public AssemblyRefUser(IAssembly assembly) {
			if (assembly == null)
				throw new ArgumentNullException("asmName");

			this.version = assembly.Version ?? new Version(0, 0, 0, 0);
			this.publicKeyOrToken = assembly.PublicKeyOrToken;
			this.name = UTF8String.IsNullOrEmpty(assembly.Name) ? UTF8String.Empty : assembly.Name;
			this.culture = assembly.Culture;
			this.attributes = (int)((publicKeyOrToken is PublicKey ? AssemblyAttributes.PublicKey : AssemblyAttributes.None) | assembly.ContentType);
		}
	}

	/// <summary>
	/// Created from a row in the AssemblyRef table
	/// </summary>
	sealed class AssemblyRefMD : AssemblyRef, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

		readonly uint origRid;

		/// <inheritdoc/>
		public uint OrigRid {
			get { return origRid; }
		}

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.MetaData.GetCustomAttributeRidList(Table.AssemblyRef, origRid);
			var tmp = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
			Interlocked.CompareExchange(ref customAttributes, tmp, null);
		}

		/// <inheritdoc/>
		protected override void InitializeCustomDebugInfos() {
			var list = ThreadSafeListCreator.Create<PdbCustomDebugInfo>();
			readerModule.InitializeCustomDebugInfos(new MDToken(MDToken.Table, origRid), new GenericParamContext(), list);
			Interlocked.CompareExchange(ref customDebugInfos, list, null);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>AssemblyRef</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public AssemblyRefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.AssemblyRefTable.IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("AssemblyRef rid {0} does not exist", rid));
#endif
			this.origRid = rid;
			this.rid = rid;
			this.readerModule = readerModule;
			uint publicKeyOrToken, name, culture;
			uint hashValue = readerModule.TablesStream.ReadAssemblyRefRow(origRid, out this.version, out this.attributes, out publicKeyOrToken, out name, out culture);
			var pkData = readerModule.BlobStream.Read(publicKeyOrToken);
			if ((this.attributes & (uint)AssemblyAttributes.PublicKey) != 0)
				this.publicKeyOrToken = new PublicKey(pkData);
			else
				this.publicKeyOrToken = new PublicKeyToken(pkData);
			this.name = readerModule.StringsStream.ReadNoNull(name);
			this.culture = readerModule.StringsStream.ReadNoNull(culture);
			this.hashValue = readerModule.BlobStream.Read(hashValue);
		}
	}
}
