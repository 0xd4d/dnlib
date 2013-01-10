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

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using dnlib.Utils;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the Assembly table
	/// </summary>
	public abstract class AssemblyDef : IHasCustomAttribute, IHasDeclSecurity, IAssembly, IListListener<ModuleDef>, ITypeDefFinder {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// The manifest module
		/// </summary>
		protected ModuleDef manifestModule;

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
		public abstract AssemblyHashAlgorithm HashAlgorithm { get; set; }

		/// <summary>
		/// From columns Assembly.MajorVersion, Assembly.MinorVersion, Assembly.BuildNumber,
		/// Assembly.RevisionNumber.
		/// </summary>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is <c>null</c></exception>
		public abstract Version Version { get; set; }

		/// <summary>
		/// From column Assembly.Flags
		/// </summary>
		public abstract AssemblyAttributes Attributes { get; set; }

		/// <summary>
		/// From column Assembly.PublicKey
		/// </summary>
		/// <remarks>An empty <see cref="PublicKey"/> is created if the caller writes <c>null</c></remarks>
		public abstract PublicKey PublicKey { get; set; }

		/// <summary>
		/// Gets the public key token which is calculated from <see cref="PublicKey"/>
		/// </summary>
		public PublicKeyToken PublicKeyToken {
			get { return PublicKey.Token; }
		}

		/// <summary>
		/// From column Assembly.Name
		/// </summary>
		public abstract UTF8String Name { get; set; }

		/// <summary>
		/// From column Assembly.Locale
		/// </summary>
		public abstract UTF8String Culture { get; set; }

		/// <inheritdoc/>
		public abstract IList<DeclSecurity> DeclSecurities { get; }

		/// <inheritdoc/>
		public PublicKeyBase PublicKeyOrToken {
			get { return PublicKey; }
		}

		/// <inheritdoc/>
		public string FullName {
			get { return GetFullNameWithPublicKeyToken(); }
		}

		/// <summary>
		/// Gets all modules. The first module is always the <see cref="ManifestModule"/>.
		/// </summary>
		public abstract IList<ModuleDef> Modules { get; }

		/// <summary>
		/// Gets all custom attributes
		/// </summary>
		public abstract CustomAttributeCollection CustomAttributes { get; }

		/// <inheritdoc/>
		public bool HasCustomAttributes {
			get { return CustomAttributes.Count > 0; }
		}

		/// <summary>
		/// Gets the manifest (main) module. This is always the first module in <see cref="Modules"/>.
		/// </summary>
		public ModuleDef ManifestModule {
			get { return Modules.Count == 0 ? null : Modules[0]; }
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.PublicKey"/> bit
		/// </summary>
		public bool HasPublicKey {
			get { return (Attributes & AssemblyAttributes.PublicKey) != 0; }
			set {
				if (value)
					Attributes |= AssemblyAttributes.PublicKey;
				else
					Attributes &= ~AssemblyAttributes.PublicKey;
			}
		}

		/// <summary>
		/// Gets/sets the processor architecture
		/// </summary>
		public AssemblyAttributes ProcessorArchitecture {
			get { return Attributes & AssemblyAttributes.PA_Mask; }
			set { Attributes = (Attributes & ~AssemblyAttributes.PA_Mask) | (value & AssemblyAttributes.PA_Mask); }
		}

		/// <summary>
		/// Gets/sets the processor architecture
		/// </summary>
		public AssemblyAttributes ProcessorArchitectureFull {
			get { return Attributes & AssemblyAttributes.PA_FullMask; }
			set { Attributes = (Attributes & ~AssemblyAttributes.PA_FullMask) | (value & AssemblyAttributes.PA_FullMask); }
		}

		/// <summary>
		/// <c>true</c> if unspecified processor architecture
		/// </summary>
		public bool IsProcessorArchitectureNone {
			get { return (Attributes & AssemblyAttributes.PA_Mask) == AssemblyAttributes.PA_None; }
		}

		/// <summary>
		/// <c>true</c> if neutral (PE32) architecture
		/// </summary>
		public bool IsProcessorArchitectureMSIL {
			get { return (Attributes & AssemblyAttributes.PA_Mask) == AssemblyAttributes.PA_MSIL; }
		}

		/// <summary>
		/// <c>true</c> if x86 (PE32) architecture
		/// </summary>
		public bool IsProcessorArchitectureX86 {
			get { return (Attributes & AssemblyAttributes.PA_Mask) == AssemblyAttributes.PA_x86; }
		}

		/// <summary>
		/// <c>true</c> if IA-64 (PE32+) architecture
		/// </summary>
		public bool IsProcessorArchitectureIA64 {
			get { return (Attributes & AssemblyAttributes.PA_Mask) == AssemblyAttributes.PA_IA64; }
		}

		/// <summary>
		/// <c>true</c> if x64 (PE32+) architecture
		/// </summary>
		public bool IsProcessorArchitectureX64 {
			get { return (Attributes & AssemblyAttributes.PA_Mask) == AssemblyAttributes.PA_AMD64; }
		}

		/// <summary>
		/// <c>true</c> if ARM (PE32) architecture
		/// </summary>
		public bool IsProcessorArchitectureARM {
			get { return (Attributes & AssemblyAttributes.PA_Mask) == AssemblyAttributes.PA_ARM; }
		}

		/// <summary>
		/// <c>true</c> if eg. reference assembly (not runnable)
		/// </summary>
		public bool IsProcessorArchitectureNoPlatform {
			get { return (Attributes & AssemblyAttributes.PA_Mask) == AssemblyAttributes.PA_NoPlatform; }
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.PA_Specified"/> bit
		/// </summary>
		public bool IsProcessorArchitectureSpecified {
			get { return (Attributes & AssemblyAttributes.PA_Specified) != 0; }
			set {
				if (value)
					Attributes |= AssemblyAttributes.PA_Specified;
				else
					Attributes &= ~AssemblyAttributes.PA_Specified;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.EnableJITcompileTracking"/> bit
		/// </summary>
		public bool EnableJITcompileTracking {
			get { return (Attributes & AssemblyAttributes.EnableJITcompileTracking) != 0; }
			set {
				if (value)
					Attributes |= AssemblyAttributes.EnableJITcompileTracking;
				else
					Attributes &= ~AssemblyAttributes.EnableJITcompileTracking;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.DisableJITcompileOptimizer"/> bit
		/// </summary>
		public bool DisableJITcompileOptimizer {
			get { return (Attributes & AssemblyAttributes.DisableJITcompileOptimizer) != 0; }
			set {
				if (value)
					Attributes |= AssemblyAttributes.DisableJITcompileOptimizer;
				else
					Attributes &= ~AssemblyAttributes.DisableJITcompileOptimizer;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.Retargetable"/> bit
		/// </summary>
		public bool IsRetargetable {
			get { return (Attributes & AssemblyAttributes.Retargetable) != 0; }
			set {
				if (value)
					Attributes |= AssemblyAttributes.Retargetable;
				else
					Attributes &= ~AssemblyAttributes.Retargetable;
			}
		}

		/// <summary>
		/// Gets/sets the content type
		/// </summary>
		public AssemblyAttributes ContentType {
			get { return Attributes & AssemblyAttributes.ContentType_Mask; }
			set { Attributes = (Attributes & ~AssemblyAttributes.ContentType_Mask) | (value & AssemblyAttributes.ContentType_Mask); }
		}

		/// <summary>
		/// <c>true</c> if content type is <c>Default</c>
		/// </summary>
		public bool IsContentTypeDefault {
			get { return (Attributes & AssemblyAttributes.ContentType_Mask) == AssemblyAttributes.ContentType_Default; }
		}

		/// <summary>
		/// <c>true</c> if content type is <c>WindowsRuntime</c>
		/// </summary>
		public bool IsContentTypeWindowsRuntime {
			get { return (Attributes & AssemblyAttributes.ContentType_Mask) == AssemblyAttributes.ContentType_WindowsRuntime; }
		}

		/// <summary>
		/// Finds a module in this assembly
		/// </summary>
		/// <param name="name">Name of module</param>
		/// <returns>A <see cref="ModuleDef"/> instance or <c>null</c> if it wasn't found.</returns>
		public ModuleDef FindModule(UTF8String name) {
			foreach (var module in Modules) {
				if (UTF8String.CaseInsensitiveEquals(module.Name, name))
					return module;
			}
			return null;
		}

		/// <summary>
		/// Creates an <see cref="AssemblyDef"/> instance from a file
		/// </summary>
		/// <param name="fileName">File name of an existing .NET assembly</param>
		/// <returns>A new <see cref="AssemblyDef"/> instance</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="fileName"/> is <c>null</c></exception>
		/// <exception cref="BadImageFormatException">If it's not a .NET assembly (eg. not a .NET file or only a .NET module)</exception>
		public static AssemblyDef Load(string fileName) {
			return Load(fileName, null);
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
			if (fileName == null)
				throw new ArgumentNullException("fileName");
			ModuleDef module = null;
			try {
				module = ModuleDefMD.Load(fileName, context);
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
		/// <returns>A new <see cref="AssemblyDef"/> instance</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="data"/> is <c>null</c></exception>
		/// <exception cref="BadImageFormatException">If it's not a .NET assembly (eg. not a .NET file or only a .NET module)</exception>
		public static AssemblyDef Load(byte[] data) {
			return Load(data, null);
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
			if (data == null)
				throw new ArgumentNullException("data");
			ModuleDef module = null;
			try {
				module = ModuleDefMD.Load(data, context);
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
		/// <returns>A new <see cref="AssemblyDef"/> instance</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="addr"/> is <c>null</c></exception>
		/// <exception cref="BadImageFormatException">If it's not a .NET assembly (eg. not a .NET file or only a .NET module)</exception>
		public static AssemblyDef Load(IntPtr addr) {
			return Load(addr, null);
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
			if (addr == IntPtr.Zero)
				throw new ArgumentNullException("addr");
			ModuleDef module = null;
			try {
				module = ModuleDefMD.Load(addr, context);
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
		/// <remarks>This will read all bytes from the stream and call <see cref="Load(byte[])"/>.
		/// It's better to use one of the other Load() methods.</remarks>
		/// <param name="stream">The stream</param>
		/// <returns>A new <see cref="AssemblyDef"/> instance</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="stream"/> is <c>null</c></exception>
		/// <exception cref="BadImageFormatException">If it's not a .NET assembly (eg. not a .NET file or only a .NET module)</exception>
		public static AssemblyDef Load(Stream stream) {
			return Load(stream, null);
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
			if (stream == null)
				throw new ArgumentNullException("stream");
			ModuleDef module = null;
			try {
				module = ModuleDefMD.Load(stream, context);
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
		/// Creates an <see cref="AssemblyDef"/> instance from a <see cref="DotNetFile"/>
		/// </summary>
		/// <param name="dnFile">The loaded .NET file</param>
		/// <returns>A new <see cref="AssemblyDef"/> instance that now owns <paramref name="dnFile"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="dnFile"/> is <c>null</c></exception>
		/// <exception cref="BadImageFormatException">If it's not a .NET assembly (eg. not a .NET file or only a .NET module)</exception>
		public static AssemblyDef Load(DotNetFile dnFile) {
			return Load(dnFile, null);
		}

		/// <summary>
		/// Creates an <see cref="AssemblyDef"/> instance from a <see cref="DotNetFile"/>
		/// </summary>
		/// <param name="dnFile">The loaded .NET file</param>
		/// <param name="context">Module context or <c>null</c></param>
		/// <returns>A new <see cref="AssemblyDef"/> instance that now owns <paramref name="dnFile"/></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="dnFile"/> is <c>null</c></exception>
		/// <exception cref="BadImageFormatException">If it's not a .NET assembly (eg. not a .NET file or only a .NET module)</exception>
		public static AssemblyDef Load(DotNetFile dnFile, ModuleContext context) {
			if (dnFile == null)
				throw new ArgumentNullException("dnFile");
			ModuleDef module = null;
			try {
				module = ModuleDefMD.Load(dnFile, context);
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
			return GetFullName(PublicKey);
		}

		/// <summary>
		/// Gets the assembly name with the public key token
		/// </summary>
		public string GetFullNameWithPublicKeyToken() {
			return GetFullName(PublicKeyToken);
		}

		string GetFullName(PublicKeyBase pkBase) {
			return Utils.GetAssemblyNameString(Name, Version, Culture, pkBase);
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
			foreach (var module in Modules) {
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
			foreach (var module in Modules) {
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
		public void Write(string filename) {
			Write(filename, null);
		}

		/// <summary>
		/// Writes the assembly to a file on disk. If the file exists, it will be truncated.
		/// </summary>
		/// <param name="filename">Filename</param>
		/// <param name="options">Writer options</param>
		public void Write(string filename, ModuleWriterOptions options) {
			ManifestModule.Write(filename, options);
		}

		/// <summary>
		/// Writes the assembly to a stream.
		/// </summary>
		/// <param name="dest">Destination stream</param>
		public void Write(Stream dest) {
			Write(dest, null);
		}

		/// <summary>
		/// Writes the assembly to a stream.
		/// </summary>
		/// <param name="dest">Destination stream</param>
		/// <param name="options">Writer options</param>
		public void Write(Stream dest, ModuleWriterOptions options) {
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
			if (PublicKeyBase.IsNullOrEmpty2(PublicKey) != PublicKeyBase.IsNullOrEmpty2(targetAsm.PublicKey))
				return false;

			foreach (var ca in targetAsm.CustomAttributes.FindAll("System.Runtime.CompilerServices.InternalsVisibleToAttribute")) {
				if (ca.ConstructorArguments.Count != 1)
					continue;
				var arg = ca.ConstructorArguments[0];
				if (arg.Type.GetElementType() != ElementType.String)
					continue;
				var asmName = arg.Value as UTF8String;
				if (UTF8String.IsNull(asmName))
					continue;

				var asmInfo = new AssemblyNameInfo(asmName.String);
				if (asmInfo.Name != Name)
					continue;
				if (!PublicKeyBase.IsNullOrEmpty2(PublicKey)) {
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
			if (ManifestModule == null)
				return;

			// Remove all existing attributes
			CustomAttribute ca = null;
			for (int i = 0; i < CustomAttributes.Count; i++) {
				var caTmp = CustomAttributes[i];
				if (caTmp.TypeFullName != "System.Reflection.AssemblySignatureKeyAttribute")
					continue;
				CustomAttributes.RemoveAt(i);
				i--;
				if (ca == null)
					ca = caTmp;
			}

			if (IsValidAssemblySignatureKeyAttribute(ca))
				ca.NamedArguments.Clear();
			else
				ca = CreateAssemblySignatureKeyAttribute();

			var counterSig = StrongNameKey.CreateCounterSignatureAsString(identityPubKey, identityKey, signaturePubKey);
			ca.ConstructorArguments[0] = new CAArgument(ManifestModule.CorLibTypes.String, new UTF8String(signaturePubKey.ToString()));
			ca.ConstructorArguments[1] = new CAArgument(ManifestModule.CorLibTypes.String, new UTF8String(counterSig));
			CustomAttributes.Add(ca);
		}

		bool IsValidAssemblySignatureKeyAttribute(CustomAttribute ca) {
			if (ca == null)
				return false;
			var ctor = ca.Constructor as IMethodDefOrRef;
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
		}

		CustomAttribute CreateAssemblySignatureKeyAttribute() {
			var owner = ManifestModule.UpdateRowId(new TypeRefUser(ManifestModule, "System.Reflection", "AssemblySignatureKeyAttribute", ManifestModule.CorLibTypes.AssemblyRef));
			var methodSig = MethodSig.CreateInstance(ManifestModule.CorLibTypes.Void, ManifestModule.CorLibTypes.String, ManifestModule.CorLibTypes.String);
			var ctor = ManifestModule.UpdateRowId(new MemberRefUser(ManifestModule, ".ctor", methodSig, owner));
			var ca = new CustomAttribute(ctor);
			ca.ConstructorArguments.Add(new CAArgument(ManifestModule.CorLibTypes.String, UTF8String.Empty));
			ca.ConstructorArguments.Add(new CAArgument(ManifestModule.CorLibTypes.String, UTF8String.Empty));
			return ca;
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
			foreach (var module in Modules) {
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
		AssemblyHashAlgorithm hashAlgId;
		Version version;
		AssemblyAttributes flags;
		PublicKey publicKey;
		UTF8String name;
		UTF8String locale;
		IList<DeclSecurity> declSecurities = new List<DeclSecurity>();
		LazyList<ModuleDef> modules;
		CustomAttributeCollection customAttributeCollection = new CustomAttributeCollection();

		/// <inheritdoc/>
		public override AssemblyHashAlgorithm HashAlgorithm {
			get { return hashAlgId; }
			set { hashAlgId = value; }
		}

		/// <inheritdoc/>
		public override Version Version {
			get { return version; }
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				version = value;
			}
		}

		/// <inheritdoc/>
		public override AssemblyAttributes Attributes {
			get { return flags; }
			set { flags = value; }
		}

		/// <inheritdoc/>
		public override PublicKey PublicKey {
			get { return publicKey; }
			set { publicKey = value ?? new PublicKey(); }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name; }
			set { name = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Culture {
			get { return locale; }
			set { locale = value; }
		}

		/// <inheritdoc/>
		public override IList<DeclSecurity> DeclSecurities {
			get { return declSecurities; }
		}

		/// <inheritdoc/>
		public override IList<ModuleDef> Modules {
			get { return modules; }
		}

		/// <inheritdoc/>
		public override CustomAttributeCollection CustomAttributes {
			get { return customAttributeCollection; }
		}

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
			this.locale = locale;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="asmName">Assembly name info</param>
		/// <exception cref="ArgumentNullException">If <paramref name="asmName"/> is <c>null</c></exception>
		public AssemblyDefUser(AssemblyName asmName)
			: this(new AssemblyNameInfo(asmName)) {
			this.hashAlgId = (AssemblyHashAlgorithm)asmName.HashAlgorithm;
			this.flags = (AssemblyAttributes)asmName.Flags;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="asmName">Assembly name info</param>
		/// <exception cref="ArgumentNullException">If <paramref name="asmName"/> is <c>null</c></exception>
		public AssemblyDefUser(AssemblyNameInfo asmName) {
			if (asmName == null)
				throw new ArgumentNullException("asmName");
			this.modules = new LazyList<ModuleDef>(this);
			this.name = asmName.Name;
			this.version = asmName.Version ?? new Version(0, 0, 0, 0);
			this.publicKey = asmName.PublicKeyOrToken as PublicKey ?? new PublicKey();
			this.locale = asmName.Locale;
			this.flags = AssemblyAttributes.None;
			this.hashAlgId = AssemblyHashAlgorithm.SHA1;
		}
	}

	/// <summary>
	/// Created from a row in the Assembly table
	/// </summary>
	sealed class AssemblyDefMD : AssemblyDef {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow"/> is called</summary>
		RawAssemblyRow rawRow;

		UserValue<AssemblyHashAlgorithm> hashAlgId;
		UserValue<Version> version;
		UserValue<AssemblyAttributes> flags;
		UserValue<PublicKey> publicKey;
		UserValue<UTF8String> name;
		UserValue<UTF8String> locale;
		LazyList<DeclSecurity> declSecurities;
		LazyList<ModuleDef> modules;
		CustomAttributeCollection customAttributeCollection;

		/// <inheritdoc/>
		public override AssemblyHashAlgorithm HashAlgorithm {
			get { return hashAlgId.Value; }
			set { hashAlgId.Value = value; }
		}

		/// <inheritdoc/>
		public override Version Version {
			get { return version.Value; }
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				version.Value = value;
			}
		}

		/// <inheritdoc/>
		public override AssemblyAttributes Attributes {
			get { return flags.Value; }
			set { flags.Value = value; }
		}

		/// <inheritdoc/>
		public override PublicKey PublicKey {
			get { return publicKey.Value; }
			set { publicKey.Value = value ?? new PublicKey(); }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Culture {
			get { return locale.Value; }
			set { locale.Value = value; }
		}

		/// <inheritdoc/>
		public override IList<DeclSecurity> DeclSecurities {
			get {
				if (declSecurities == null) {
					var list = readerModule.MetaData.GetDeclSecurityRidList(Table.Assembly, rid);
					declSecurities = new LazyList<DeclSecurity>((int)list.Length, list, (list2, index) => readerModule.ResolveDeclSecurity(((RidList)list2)[index]));
				}
				return declSecurities;
			}
		}

		/// <inheritdoc/>
		public override IList<ModuleDef> Modules {
			get {
				if (modules == null) {
					var list = readerModule.GetModuleRidList();
					modules = new LazyList<ModuleDef>((int)list.Length + 1, this, list, (list2, index) => {
						ModuleDef module;
						if (index == 0)
							module = readerModule;
						else
							module = readerModule.ReadModule(((RidList)list2)[index - 1]);
						if (module == null)
							module = new ModuleDefUser("INVALID", Guid.NewGuid());
						module.Assembly = this;
						return module;
					});
				}
				return modules;
			}
		}

		/// <inheritdoc/>
		public override CustomAttributeCollection CustomAttributes {
			get {
				if (customAttributeCollection == null) {
					var list = readerModule.MetaData.GetCustomAttributeRidList(Table.Assembly, rid);
					customAttributeCollection = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
				}
				return customAttributeCollection;
			}
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
			this.rid = rid;
			this.readerModule = readerModule;
			if (rid != 1)
				this.modules = new LazyList<ModuleDef>(this);
			Initialize();
		}

		void Initialize() {
			hashAlgId.ReadOriginalValue = () => {
				InitializeRawRow();
				return (AssemblyHashAlgorithm)rawRow.HashAlgId;
			};
			version.ReadOriginalValue = () => {
				InitializeRawRow();
				return new Version(rawRow.MajorVersion, rawRow.MinorVersion, rawRow.BuildNumber, rawRow.RevisionNumber);
			};
			flags.ReadOriginalValue = () => {
				InitializeRawRow();
				return (AssemblyAttributes)rawRow.Flags;
			};
			publicKey.ReadOriginalValue = () => {
				InitializeRawRow();
				return new PublicKey(readerModule.BlobStream.Read(rawRow.PublicKey));
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.ReadNoNull(rawRow.Name);
			};
			locale.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.ReadNoNull(rawRow.Locale);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadAssemblyRow(rid);
		}
	}
}
