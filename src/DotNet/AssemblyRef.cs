/*
    Copyright (C) 2012-2014 de4dot@gmail.com

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

﻿using System;
using System.Reflection;
using System.Threading;
using dnlib.Utils;
using dnlib.DotNet.MD;
using dnlib.Threading;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the AssemblyRef table
	/// </summary>
	public abstract class AssemblyRef : IHasCustomAttribute, IImplementation, IResolutionScope, IAssembly, IScope {
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

#if THREAD_SAFE
		/// <summary>
		/// The lock
		/// </summary>
		internal readonly Lock theLock = Lock.Create();
#endif

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
		public abstract Version Version { get; set; }

		/// <summary>
		/// From column AssemblyRef.Flags
		/// </summary>
		public AssemblyAttributes Attributes {
#if THREAD_SAFE
			get {
				theLock.EnterWriteLock();
				try {
					return Attributes_NoLock;
				}
				finally { theLock.ExitWriteLock(); }
			}
#else
			get { return Attributes_NoLock; }
#endif
			set {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				Attributes_NoLock = (value & ~AssemblyAttributes.PublicKey) | (Attributes_NoLock & AssemblyAttributes.PublicKey);
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}

		/// <summary>
		/// From column AssemblyRef.Flags
		/// </summary>
		protected abstract AssemblyAttributes Attributes_NoLock { get; set; }

		/// <summary>
		/// From column AssemblyRef.PublicKeyOrToken
		/// </summary>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is <c>null</c></exception>
		public PublicKeyBase PublicKeyOrToken {
#if THREAD_SAFE
			get {
				theLock.EnterWriteLock();
				try {
					return PublicKeyOrToken_NoLock;
				}
				finally { theLock.ExitWriteLock(); }
			}
#else
			get { return PublicKeyOrToken_NoLock; }
#endif
			set {
				if (value == null)
					throw new ArgumentNullException("value");
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				if (value is PublicKey)
					Attributes_NoLock |= AssemblyAttributes.PublicKey;
				else
					Attributes_NoLock &= ~AssemblyAttributes.PublicKey;
				PublicKeyOrToken_NoLock = value;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}

		/// <summary>
		/// From column AssemblyRef.PublicKeyOrToken
		/// </summary>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is <c>null</c></exception>
		protected abstract PublicKeyBase PublicKeyOrToken_NoLock { get; set; }

		/// <summary>
		/// From column AssemblyRef.Name
		/// </summary>
		public abstract UTF8String Name { get; set; }

		/// <summary>
		/// From column AssemblyRef.Locale
		/// </summary>
		public abstract UTF8String Culture { get; set; }

		/// <summary>
		/// From column AssemblyRef.HashValue
		/// </summary>
		public abstract byte[] Hash { get; set; }

		/// <summary>
		/// Gets all custom attributes
		/// </summary>
		public abstract CustomAttributeCollection CustomAttributes { get; }

		/// <inheritdoc/>
		public bool HasCustomAttributes {
			get { return CustomAttributes.Count > 0; }
		}

		/// <inheritdoc/>
		public string FullName {
			get { return Utils.GetAssemblyNameString(Name, Version, Culture, PublicKeyBase.ToPublicKeyToken(PublicKeyOrToken)); }
		}

		/// <summary>
		/// Same as <see cref="FullName"/>, except that it uses the <c>PublicKey</c> if available.
		/// </summary>
		public string RealFullName {
			get { return Utils.GetAssemblyNameString(Name, Version, Culture, PublicKeyOrToken); }
		}

		/// <summary>
		/// Modify <see cref="Attributes_NoLock"/> property: <see cref="Attributes_NoLock"/> =
		/// (<see cref="Attributes_NoLock"/> &amp; <paramref name="andMask"/>) | <paramref name="orMask"/>.
		/// </summary>
		/// <param name="andMask">Value to <c>AND</c></param>
		/// <param name="orMask">Value to OR</param>
		void ModifyAttributes(AssemblyAttributes andMask, AssemblyAttributes orMask) {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
				Attributes_NoLock = (Attributes_NoLock & andMask) | orMask;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Set or clear flags in <see cref="Attributes_NoLock"/>
		/// </summary>
		/// <param name="set"><c>true</c> if flags should be set, <c>false</c> if flags should
		/// be cleared</param>
		/// <param name="flags">Flags to set or clear</param>
		void ModifyAttributes(bool set, AssemblyAttributes flags) {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
				if (set)
					Attributes_NoLock |= flags;
				else
					Attributes_NoLock &= ~flags;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.PublicKey"/> bit
		/// </summary>
		public bool HasPublicKey {
			get { return (Attributes & AssemblyAttributes.PublicKey) != 0; }
			set { ModifyAttributes(value, AssemblyAttributes.PublicKey); }
		}

		/// <summary>
		/// Gets/sets the processor architecture
		/// </summary>
		public AssemblyAttributes ProcessorArchitecture {
			get { return Attributes & AssemblyAttributes.PA_Mask; }
			set { ModifyAttributes(~AssemblyAttributes.PA_Mask, value & AssemblyAttributes.PA_Mask); }
		}

		/// <summary>
		/// Gets/sets the processor architecture
		/// </summary>
		public AssemblyAttributes ProcessorArchitectureFull {
			get { return Attributes & AssemblyAttributes.PA_FullMask; }
			set { ModifyAttributes(~AssemblyAttributes.PA_FullMask, value & AssemblyAttributes.PA_FullMask); }
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
			set { ModifyAttributes(value, AssemblyAttributes.PA_Specified); }
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.EnableJITcompileTracking"/> bit
		/// </summary>
		public bool EnableJITcompileTracking {
			get { return (Attributes & AssemblyAttributes.EnableJITcompileTracking) != 0; }
			set { ModifyAttributes(value, AssemblyAttributes.EnableJITcompileTracking); }
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.DisableJITcompileOptimizer"/> bit
		/// </summary>
		public bool DisableJITcompileOptimizer {
			get { return (Attributes & AssemblyAttributes.DisableJITcompileOptimizer) != 0; }
			set { ModifyAttributes(value, AssemblyAttributes.DisableJITcompileOptimizer); }
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyAttributes.Retargetable"/> bit
		/// </summary>
		public bool IsRetargetable {
			get { return (Attributes & AssemblyAttributes.Retargetable) != 0; }
			set { ModifyAttributes(value, AssemblyAttributes.Retargetable); }
		}

		/// <summary>
		/// Gets/sets the content type
		/// </summary>
		public AssemblyAttributes ContentType {
			get { return Attributes & AssemblyAttributes.ContentType_Mask; }
			set { ModifyAttributes(~AssemblyAttributes.ContentType_Mask, value & AssemblyAttributes.ContentType_Mask); }
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

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// An AssemblyRef row created by the user and not present in the original .NET file
	/// </summary>
	public class AssemblyRefUser : AssemblyRef {
		Version version;
		AssemblyAttributes flags;
		PublicKeyBase publicKeyOrToken;
		UTF8String name;
		UTF8String locale;
		byte[] hashValue;
		readonly CustomAttributeCollection customAttributeCollection = new CustomAttributeCollection();

		/// <inheritdoc/>
		public override CustomAttributeCollection CustomAttributes {
			get { return customAttributeCollection; }
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
		protected override AssemblyAttributes Attributes_NoLock {
			get { return flags; }
			set { flags = value; }
		}

		/// <inheritdoc/>
		protected override PublicKeyBase PublicKeyOrToken_NoLock {
			get { return publicKeyOrToken; }
			set { publicKeyOrToken = value; }
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
		public override byte[] Hash {
			get { return hashValue; }
			set { hashValue = value; }
		}

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
			this.locale = locale;
			this.flags = publicKey is PublicKey ? AssemblyAttributes.PublicKey : AssemblyAttributes.None;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="asmName">Assembly name info</param>
		/// <exception cref="ArgumentNullException">If <paramref name="asmName"/> is <c>null</c></exception>
		public AssemblyRefUser(AssemblyName asmName)
			: this(new AssemblyNameInfo(asmName)) {
			this.flags = (AssemblyAttributes)asmName.Flags;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="asmName">Assembly name info</param>
		/// <exception cref="ArgumentNullException">If <paramref name="asmName"/> is <c>null</c></exception>
		public AssemblyRefUser(AssemblyNameInfo asmName) {
			if (asmName == null)
				throw new ArgumentNullException("asmName");

			this.version = asmName.Version ?? new Version(0, 0, 0, 0);
			this.publicKeyOrToken = asmName.PublicKeyOrToken;
			this.name = UTF8String.IsNullOrEmpty(asmName.Name) ? UTF8String.Empty : asmName.Name;
			this.locale = asmName.Locale;
			this.flags = publicKeyOrToken is PublicKey ? AssemblyAttributes.PublicKey : AssemblyAttributes.None;
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
			this.locale = assembly.Culture;
			this.flags = publicKeyOrToken is PublicKey ? AssemblyAttributes.PublicKey : AssemblyAttributes.None;
		}
	}

	/// <summary>
	/// Created from a row in the AssemblyRef table
	/// </summary>
	sealed class AssemblyRefMD : AssemblyRef {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow_NoLock"/> is called</summary>
		RawAssemblyRefRow rawRow;

		CustomAttributeCollection customAttributeCollection;
		UserValue<Version> version;
		UserValue<AssemblyAttributes> flags;
		UserValue<PublicKeyBase> publicKeyOrToken;
		UserValue<UTF8String> name;
		UserValue<UTF8String> locale;
		UserValue<byte[]> hashValue;

		/// <inheritdoc/>
		public override CustomAttributeCollection CustomAttributes {
			get {
				if (customAttributeCollection == null) {
					var list = readerModule.MetaData.GetCustomAttributeRidList(Table.AssemblyRef, rid);
					var tmp = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
					Interlocked.CompareExchange(ref customAttributeCollection, tmp, null);
				}
				return customAttributeCollection;
			}
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
		protected override AssemblyAttributes Attributes_NoLock {
			get { return flags.Value; }
			set { flags.Value = value; }
		}

		/// <inheritdoc/>
		protected override PublicKeyBase PublicKeyOrToken_NoLock {
			get { return publicKeyOrToken.Value; }
			set { publicKeyOrToken.Value = value; }
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
		public override byte[] Hash {
			get { return hashValue.Value; }
			set { hashValue.Value = value; }
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
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			version.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				return new Version(rawRow.MajorVersion, rawRow.MinorVersion, rawRow.BuildNumber, rawRow.RevisionNumber);
			};
			flags.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				return (AssemblyAttributes)rawRow.Flags;
			};
			publicKeyOrToken.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				var pkData = readerModule.BlobStream.Read(rawRow.PublicKeyOrToken);
				if ((rawRow.Flags & (uint)AssemblyAttributes.PublicKey) != 0)
					return new PublicKey(pkData);
				return new PublicKeyToken(pkData);
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				return readerModule.StringsStream.ReadNoNull(rawRow.Name);
			};
			locale.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				return readerModule.StringsStream.ReadNoNull(rawRow.Locale);
			};
			hashValue.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				return readerModule.BlobStream.Read(rawRow.HashValue);
			};
#if THREAD_SAFE
			version.Lock = theLock;
			// flags.Lock = theLock;				No lock for this one
			// publicKeyOrToken.Lock = theLock;		No lock for this one
			name.Lock = theLock;
			locale.Lock = theLock;
			hashValue.Lock = theLock;
#endif
		}

		void InitializeRawRow_NoLock() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadAssemblyRefRow(rid);
		}
	}
}
