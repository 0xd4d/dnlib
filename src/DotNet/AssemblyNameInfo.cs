// dnlib: See LICENSE.txt for more info

using System;
using System.Reflection;

namespace dnlib.DotNet {
	/// <summary>
	/// Stores assembly name information
	/// </summary>
	public sealed class AssemblyNameInfo : IAssembly {
		AssemblyHashAlgorithm hashAlgId;
		Version version;
		AssemblyAttributes flags;
		PublicKeyBase publicKeyOrToken;
		UTF8String name;
		UTF8String culture;

		/// <summary>
		/// Gets/sets the <see cref="AssemblyHashAlgorithm"/>
		/// </summary>
		public AssemblyHashAlgorithm HashAlgId {
			get => hashAlgId;
			set => hashAlgId = value;
		}

		/// <summary>
		/// Gets/sets the <see cref="Version"/> or <c>null</c> if none specified
		/// </summary>
		public Version Version {
			get => version;
			set => version = value;
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes"/>
		/// </summary>
		public AssemblyAttributes Attributes {
			get => flags;
			set => flags = value;
		}

		/// <summary>
		/// Gets/sets the public key or token
		/// </summary>
		public PublicKeyBase PublicKeyOrToken {
			get => publicKeyOrToken;
			set => publicKeyOrToken = value;
		}

		/// <summary>
		/// Gets/sets the name
		/// </summary>
		public UTF8String Name {
			get => name;
			set => name = value;
		}

		/// <summary>
		/// Gets/sets the culture or <c>null</c> if none specified
		/// </summary>
		public UTF8String Culture {
			get => culture;
			set => culture = value;
		}

		/// <summary>
		/// Gets the full name of the assembly
		/// </summary>
		public string FullName => FullNameToken;

		/// <summary>
		/// Gets the full name of the assembly but use a public key token
		/// </summary>
		public string FullNameToken {
			get {
				var pk = publicKeyOrToken;
				if (pk is PublicKey)
					pk = (pk as PublicKey).Token;
				return Utils.GetAssemblyNameString(name, version, culture, pk, flags);
			}
		}

		/// <summary>
		/// Modify <see cref="Attributes"/> property: <see cref="Attributes"/> =
		/// (<see cref="Attributes"/> &amp; <paramref name="andMask"/>) | <paramref name="orMask"/>.
		/// </summary>
		/// <param name="andMask">Value to <c>AND</c></param>
		/// <param name="orMask">Value to OR</param>
		void ModifyAttributes(AssemblyAttributes andMask, AssemblyAttributes orMask) => Attributes = (Attributes & andMask) | orMask;

		/// <summary>
		/// Set or clear flags in <see cref="Attributes"/>
		/// </summary>
		/// <param name="set"><c>true</c> if flags should be set, <c>false</c> if flags should
		/// be cleared</param>
		/// <param name="flags">Flags to set or clear</param>
		void ModifyAttributes(bool set, AssemblyAttributes flags) {
			if (set)
				Attributes |= flags;
			else
				Attributes &= ~flags;
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.PublicKey"/> bit
		/// </summary>
		public bool HasPublicKey {
			get => (Attributes & AssemblyAttributes.PublicKey) != 0;
			set => ModifyAttributes(value, AssemblyAttributes.PublicKey);
		}

		/// <summary>
		/// Gets/sets the processor architecture
		/// </summary>
		public AssemblyAttributes ProcessorArchitecture {
			get => Attributes & AssemblyAttributes.PA_Mask;
			set => ModifyAttributes(~AssemblyAttributes.PA_Mask, value & AssemblyAttributes.PA_Mask);
		}

		/// <summary>
		/// Gets/sets the processor architecture
		/// </summary>
		public AssemblyAttributes ProcessorArchitectureFull {
			get => Attributes & AssemblyAttributes.PA_FullMask;
			set => ModifyAttributes(~AssemblyAttributes.PA_FullMask, value & AssemblyAttributes.PA_FullMask);
		}

		/// <summary>
		/// <c>true</c> if unspecified processor architecture
		/// </summary>
		public bool IsProcessorArchitectureNone => (Attributes & AssemblyAttributes.PA_Mask) == AssemblyAttributes.PA_None;

		/// <summary>
		/// <c>true</c> if neutral (PE32) architecture
		/// </summary>
		public bool IsProcessorArchitectureMSIL => (Attributes & AssemblyAttributes.PA_Mask) == AssemblyAttributes.PA_MSIL;

		/// <summary>
		/// <c>true</c> if x86 (PE32) architecture
		/// </summary>
		public bool IsProcessorArchitectureX86 => (Attributes & AssemblyAttributes.PA_Mask) == AssemblyAttributes.PA_x86;

		/// <summary>
		/// <c>true</c> if IA-64 (PE32+) architecture
		/// </summary>
		public bool IsProcessorArchitectureIA64 => (Attributes & AssemblyAttributes.PA_Mask) == AssemblyAttributes.PA_IA64;

		/// <summary>
		/// <c>true</c> if x64 (PE32+) architecture
		/// </summary>
		public bool IsProcessorArchitectureX64 => (Attributes & AssemblyAttributes.PA_Mask) == AssemblyAttributes.PA_AMD64;

		/// <summary>
		/// <c>true</c> if ARM (PE32) architecture
		/// </summary>
		public bool IsProcessorArchitectureARM => (Attributes & AssemblyAttributes.PA_Mask) == AssemblyAttributes.PA_ARM;

		/// <summary>
		/// <c>true</c> if eg. reference assembly (not runnable)
		/// </summary>
		public bool IsProcessorArchitectureNoPlatform => (Attributes & AssemblyAttributes.PA_Mask) == AssemblyAttributes.PA_NoPlatform;

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.PA_Specified"/> bit
		/// </summary>
		public bool IsProcessorArchitectureSpecified {
			get => (Attributes & AssemblyAttributes.PA_Specified) != 0;
			set => ModifyAttributes(value, AssemblyAttributes.PA_Specified);
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.EnableJITcompileTracking"/> bit
		/// </summary>
		public bool EnableJITcompileTracking {
			get => (Attributes & AssemblyAttributes.EnableJITcompileTracking) != 0;
			set => ModifyAttributes(value, AssemblyAttributes.EnableJITcompileTracking);
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.DisableJITcompileOptimizer"/> bit
		/// </summary>
		public bool DisableJITcompileOptimizer {
			get => (Attributes & AssemblyAttributes.DisableJITcompileOptimizer) != 0;
			set => ModifyAttributes(value, AssemblyAttributes.DisableJITcompileOptimizer);
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.Retargetable"/> bit
		/// </summary>
		public bool IsRetargetable {
			get => (Attributes & AssemblyAttributes.Retargetable) != 0;
			set => ModifyAttributes(value, AssemblyAttributes.Retargetable);
		}

		/// <summary>
		/// Gets/sets the content type
		/// </summary>
		public AssemblyAttributes ContentType {
			get => Attributes & AssemblyAttributes.ContentType_Mask;
			set => ModifyAttributes(~AssemblyAttributes.ContentType_Mask, value & AssemblyAttributes.ContentType_Mask);
		}

		/// <summary>
		/// <c>true</c> if content type is <c>Default</c>
		/// </summary>
		public bool IsContentTypeDefault => (Attributes & AssemblyAttributes.ContentType_Mask) == AssemblyAttributes.ContentType_Default;

		/// <summary>
		/// <c>true</c> if content type is <c>WindowsRuntime</c>
		/// </summary>
		public bool IsContentTypeWindowsRuntime => (Attributes & AssemblyAttributes.ContentType_Mask) == AssemblyAttributes.ContentType_WindowsRuntime;

		/// <summary>
		/// Default constructor
		/// </summary>
		public AssemblyNameInfo() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="asmFullName">An assembly name</param>
		public AssemblyNameInfo(string asmFullName)
			: this(ReflectionTypeNameParser.ParseAssemblyRef(asmFullName)) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="asm">The assembly</param>
		public AssemblyNameInfo(IAssembly asm) {
			if (asm is null)
				return;
			var asmDef = asm as AssemblyDef;
			hashAlgId = asmDef is null ? 0 : asmDef.HashAlgorithm;
			version = asm.Version ?? new Version(0, 0, 0, 0);
			flags = asm.Attributes;
			publicKeyOrToken = asm.PublicKeyOrToken;
			name = UTF8String.IsNullOrEmpty(asm.Name) ? UTF8String.Empty : asm.Name;
			culture = UTF8String.IsNullOrEmpty(asm.Culture) ? UTF8String.Empty : asm.Culture;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="asmName">Assembly name info</param>
		public AssemblyNameInfo(AssemblyName asmName) {
			if (asmName is null)
				return;
			hashAlgId = (AssemblyHashAlgorithm)asmName.HashAlgorithm;
			version = asmName.Version ?? new Version(0, 0, 0, 0);
			flags = (AssemblyAttributes)asmName.Flags;
			publicKeyOrToken = (PublicKeyBase)PublicKeyBase.CreatePublicKey(asmName.GetPublicKey()) ??
							PublicKeyBase.CreatePublicKeyToken(asmName.GetPublicKeyToken());
			name = asmName.Name ?? string.Empty;
			culture = !(asmName.CultureInfo is null) && !(asmName.CultureInfo.Name is null) ? asmName.CultureInfo.Name : string.Empty;
		}

		/// <inhertidoc/>
		public override string ToString() => FullName;
	}
}
