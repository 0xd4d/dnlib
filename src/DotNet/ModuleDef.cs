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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using dnlib.Utils;
using dnlib.DotNet.MD;
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
	public abstract class ModuleDef : IHasCustomAttribute, IResolutionScope, IDisposable, IListListener<TypeDef>, IModule, ITypeDefFinder {
		/// <summary>Default characteristics</summary>
		protected const Characteristics DefaultCharacteristics = Characteristics.ExecutableImage | Characteristics._32BitMachine;

		/// <summary>Default DLL characteristics</summary>
		protected const DllCharacteristics DefaultDllCharacteristics = DllCharacteristics.TerminalServerAware | DllCharacteristics.NoSeh | DllCharacteristics.NxCompat | DllCharacteristics.DynamicBase;

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

		/// <summary>
		/// Initialize this in the ctor
		/// </summary>
		protected ICorLibTypes corLibTypes;

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
		public abstract ushort Generation { get; set; }

		/// <summary>
		/// Gets/sets Module.Name column
		/// </summary>
		public abstract UTF8String Name { get; set; }

		/// <summary>
		/// Gets/sets Module.Mvid column
		/// </summary>
		public abstract Guid? Mvid { get; set; }

		/// <summary>
		/// Gets/sets Module.EncId column
		/// </summary>
		public abstract Guid? EncId { get; set; }

		/// <summary>
		/// Gets/sets Module.EncBaseId column
		/// </summary>
		public abstract Guid? EncBaseId { get; set; }

		/// <summary>
		/// Gets all custom attributes
		/// </summary>
		public abstract CustomAttributeCollection CustomAttributes { get; }

		/// <summary>
		/// Gets the module's assembly. To set this value, add this <see cref="ModuleDef"/>
		/// to <see cref="AssemblyDef.Modules"/>.
		/// </summary>
		public abstract AssemblyDef Assembly { get; internal set; }

		/// <summary>
		/// Gets a list of all non-nested <see cref="TypeDef"/>s
		/// </summary>
		public abstract ThreadSafe.IList<TypeDef> Types { get; }

		/// <summary>
		/// Gets a list of all <see cref="ExportedType"/>s
		/// </summary>
		public abstract ThreadSafe.IList<ExportedType> ExportedTypes { get; }

		/// <summary>
		/// Gets/sets the native entry point. Only one of <see cref="NativeEntryPoint"/> and
		/// <see cref="ManagedEntryPoint"/> can be set. You write to one and the other one gets cleared.
		/// </summary>
		public RVA NativeEntryPoint {
			get {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				return NativeEntryPoint_NoLock;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
			set {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				NativeEntryPoint_NoLock = value;
				ManagedEntryPoint_NoLock = null;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}

		/// <summary>
		/// No-lock version of <see cref="NativeEntryPoint"/>
		/// </summary>
		protected abstract RVA NativeEntryPoint_NoLock { get; set; }

		/// <summary>
		/// Gets/sets the managed entry point. Only one of <see cref="NativeEntryPoint"/> and
		/// <see cref="ManagedEntryPoint"/> can be set. You write to one and the other one gets cleared.
		/// </summary>
		public IManagedEntryPoint ManagedEntryPoint {
			get {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				return ManagedEntryPoint_NoLock;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
			set {
#if THREAD_SAFE
				theLock.EnterWriteLock(); try {
#endif
				NativeEntryPoint_NoLock = 0;
				ManagedEntryPoint_NoLock = value;
#if THREAD_SAFE
				} finally { theLock.ExitWriteLock(); }
#endif
			}
		}

		/// <summary>
		/// No-lock version of <see cref="ManagedEntryPoint"/>
		/// </summary>
		protected abstract IManagedEntryPoint ManagedEntryPoint_NoLock { get; set; }

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
		public abstract ResourceCollection Resources { get; }

		/// <summary>
		/// Gets/sets the <see cref="VTableFixups"/>. This is <c>null</c> if there are no
		/// vtable fixups.
		/// </summary>
		public abstract VTableFixups VTableFixups { get; set; }

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
			get { return UTF8String.ToSystemStringOrEmpty(Name); }
		}

		/// <summary>
		/// Gets/sets the path of the module or an empty string if it wasn't loaded from disk
		/// </summary>
		public abstract string Location { get; set; }

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
			set { context = value; }
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
				var asm = Assembly;
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
		public abstract Win32Resources Win32Resources { get; set; }

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
		public string RuntimeVersion { get; set; }

		/// <summary>
		/// <c>true</c> if <see cref="RuntimeVersion"/> is the CLR v1.0 string (only the major
		/// and minor version numbers are checked)
		/// </summary>
		public bool IsClr10 {
			get { return (RuntimeVersion ?? string.Empty).StartsWith(MDHeaderRuntimeVersion.MS_CLR_10_PREFIX); }
		}

		/// <summary>
		/// <c>true</c> if <see cref="RuntimeVersion"/> is the CLR v1.0 string
		/// </summary>
		public bool IsClr10Exactly {
			get { return RuntimeVersion == MDHeaderRuntimeVersion.MS_CLR_10; }
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
		/// Gets/sets the <see cref="Cor20HeaderFlags"/> (from .NET header)
		/// </summary>
		public ComImageFlags Cor20HeaderFlags {
#if THREAD_SAFE
			get {
				theLock.EnterReadLock(); try {
					return Cor20HeaderFlags_NoLock;
				} finally { theLock.ExitReadLock(); }
			}
			set {
				theLock.EnterWriteLock(); try {
					Cor20HeaderFlags_NoLock = value;
				} finally { theLock.ExitWriteLock(); }
			}
#else
			get { return Cor20HeaderFlags_NoLock; }
			set { Cor20HeaderFlags_NoLock = value; }
#endif
		}
		ComImageFlags Cor20HeaderFlags_NoLock;

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
		/// Set or clear flags in <see cref="Cor20HeaderFlags_NoLock"/>
		/// </summary>
		/// <param name="set"><c>true</c> if flags should be set, <c>false</c> if flags should
		/// be cleared</param>
		/// <param name="flags">Flags to set or clear</param>
		void ModifyComImageFlags(bool set, ComImageFlags flags) {
#if THREAD_SAFE
			theLock.EnterWriteLock(); try {
#endif
				if (set)
					Cor20HeaderFlags_NoLock |= flags;
				else
					Cor20HeaderFlags_NoLock &= ~flags;
#if THREAD_SAFE
			} finally { theLock.ExitWriteLock(); }
#endif
		}

		/// <summary>
		/// Gets/sets the <see cref="ComImageFlags.ILOnly"/> bit
		/// </summary>
		public bool IsILOnly {
			get { return (Cor20HeaderFlags & ComImageFlags.ILOnly) != 0; }
			set { ModifyComImageFlags(value, ComImageFlags.ILOnly); }
		}

		/// <summary>
		/// Gets/sets the <see cref="ComImageFlags._32BitRequired"/> bit
		/// </summary>
		public bool Is32BitRequired {
			get { return (Cor20HeaderFlags & ComImageFlags._32BitRequired) != 0; }
			set { ModifyComImageFlags(value, ComImageFlags._32BitRequired); }
		}

		/// <summary>
		/// Gets/sets the <see cref="ComImageFlags.StrongNameSigned"/> bit
		/// </summary>
		public bool IsStrongNameSigned {
			get { return (Cor20HeaderFlags & ComImageFlags.StrongNameSigned) != 0; }
			set { ModifyComImageFlags(value, ComImageFlags.StrongNameSigned); }
		}

		/// <summary>
		/// Gets/sets the <see cref="ComImageFlags.NativeEntryPoint"/> bit
		/// </summary>
		public bool HasNativeEntryPoint {
			get { return (Cor20HeaderFlags & ComImageFlags.NativeEntryPoint) != 0; }
			set { ModifyComImageFlags(value, ComImageFlags.NativeEntryPoint); }
		}

		/// <summary>
		/// Gets/sets the <see cref="ComImageFlags._32BitPreferred"/> bit
		/// </summary>
		public bool Is32BitPreferred {
			get { return (Cor20HeaderFlags & ComImageFlags._32BitPreferred) != 0; }
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
			return new Importer(this).Import(fieldInfo) as MemberRef;
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
		public ITypeDefOrRef Import(TypeDef type) {
			return new Importer(this).Import(type);
		}

		/// <summary>
		/// Imports a <see cref="TypeRef"/>
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>The imported type or <c>null</c></returns>
		public ITypeDefOrRef Import(TypeRef type) {
			return new Importer(this).Import(type);
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
			return new Importer(this).Import(field);
		}

		/// <summary>
		/// Imports a <see cref="FieldDef"/> as a <see cref="MemberRef"/>
		/// </summary>
		/// <param name="field">The field</param>
		/// <returns>The imported type or <c>null</c> if <paramref name="field"/> is invalid</returns>
		public MemberRef Import(FieldDef field) {
			return new Importer(this).Import(field);
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
		public MemberRef Import(MethodDef method) {
			return new Importer(this).Import(method);
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
			if (machine == Machine.AMD64 || machine == Machine.IA64)
				return 8;
			if (machine != Machine.I386)
				return 4;

			// Machine is I386 so it's either x86 or platform neutral

			// If the runtime version is < 2.5, then it's always loaded as a 32-bit process.
			if (GetCor20RuntimeVersion() < 0x00020005)
				return 4;

			// If it's a 32-bit PE header, and ILOnly is cleared, it's always loaded as a
			// 32-bit process.
			var flags = Cor20HeaderFlags;
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

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// A Module row created by the user and not present in the original .NET file
	/// </summary>
	public class ModuleDefUser : ModuleDef {
		ushort generation;
		UTF8String name;
		Guid? mvid;
		Guid? encId;
		Guid? encBaseId;
		readonly CustomAttributeCollection customAttributeCollection = new CustomAttributeCollection();
		AssemblyDef assembly;
		LazyList<TypeDef> types;
		readonly ThreadSafe.IList<ExportedType> exportedTypes = ThreadSafeListCreator.Create<ExportedType>();
		readonly ResourceCollection resources = new ResourceCollection();
		RVA nativeEntryPoint;
		IManagedEntryPoint managedEntryPoint;
		Win32Resources win32Resources;
		VTableFixups vtableFixups;
		string location = string.Empty;

		/// <inheritdoc/>
		public override ushort Generation {
			get { return generation; }
			set { generation = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name; }
			set { name = value; }
		}

		/// <inheritdoc/>
		public override Guid? Mvid {
			get { return mvid; }
			set { mvid = value; }
		}

		/// <inheritdoc/>
		public override Guid? EncId {
			get { return encId; }
			set { encId = value; }
		}

		/// <inheritdoc/>
		public override Guid? EncBaseId {
			get { return encBaseId; }
			set { encBaseId = value; }
		}

		/// <inheritdoc/>
		public override CustomAttributeCollection CustomAttributes {
			get { return customAttributeCollection; }
		}

		/// <inheritdoc/>
		public override AssemblyDef Assembly {
			get { return assembly; }
			internal set { assembly = value; }
		}

		/// <inheritdoc/>
		public override ThreadSafe.IList<TypeDef> Types {
			get { return types; }
		}

		/// <inheritdoc/>
		public override ThreadSafe.IList<ExportedType> ExportedTypes {
			get { return exportedTypes; }
		}

		/// <inheritdoc/>
		public override ResourceCollection Resources {
			get { return resources; }
		}

		/// <inheritdoc/>
		protected override RVA NativeEntryPoint_NoLock {
			get { return nativeEntryPoint; }
			set { nativeEntryPoint = value; }
		}

		/// <inheritdoc/>
		protected override IManagedEntryPoint ManagedEntryPoint_NoLock {
			get { return managedEntryPoint; }
			set { managedEntryPoint = value; }
		}

		/// <inheritdoc/>
		public override string Location {
			get { return location; }
			set { location = value ?? string.Empty; }
		}

		/// <inheritdoc/>
		public override Win32Resources Win32Resources {
			get { return win32Resources; }
			set { win32Resources = value; }
		}

		/// <inheritdoc/>
		public override VTableFixups VTableFixups {
			get { return vtableFixups; }
			set { vtableFixups = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public ModuleDefUser()
			: this(null, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <remarks><see cref="Mvid"/> is initialized to a random <see cref="Guid"/></remarks>
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
			this.Cor20HeaderFlags = ComImageFlags.ILOnly;
			this.Cor20HeaderRuntimeVersion = 0x00020005;	// .NET 2.0 or later should use 2.5
			this.TablesHeaderVersion = 0x0200;				// .NET 2.0 or later should use 2.0
			this.corLibTypes = new CorLibTypes(this, corLibAssemblyRef);
			this.types = new LazyList<TypeDef>(this);
			this.name = name;
			this.mvid = mvid;
			types.Add(CreateModuleType());
			UpdateRowId(this);
		}

		TypeDef CreateModuleType() {
			var type = UpdateRowId(new TypeDefUser(null, "<Module>", null));
			type.Attributes = TypeAttributes.NotPublic | TypeAttributes.AutoLayout | TypeAttributes.Class | TypeAttributes.AnsiClass;
			return type;
		}
	}

	/// <summary>
	/// Created from a row in the Module table
	/// </summary>
	public class ModuleDefMD2 : ModuleDef {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow_NoLock"/> is called</summary>
		RawModuleRow rawRow;

		UserValue<ushort> generation;
		UserValue<UTF8String> name;
		UserValue<Guid?> mvid;
		UserValue<Guid?> encId;
		UserValue<Guid?> encBaseId;
		CustomAttributeCollection customAttributeCollection;
		UserValue<AssemblyDef> assembly;
		/// <summary/>
		protected LazyList<TypeDef> types;
		/// <summary/>
		protected LazyList<ExportedType> exportedTypes;
		/// <summary/>
		protected ResourceCollection resources;
		UserValue<RVA> nativeEntryPoint;
		UserValue<IManagedEntryPoint> managedEntryPoint;
		Win32Resources win32Resources;
		VTableFixups vtableFixups;
		string location;

		/// <inheritdoc/>
		public override ushort Generation {
			get { return generation.Value; }
			set { generation.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <inheritdoc/>
		public override Guid? Mvid {
			get { return mvid.Value; }
			set { mvid.Value = value; }
		}

		/// <inheritdoc/>
		public override Guid? EncId {
			get { return encId.Value; }
			set { encId.Value = value; }
		}

		/// <inheritdoc/>
		public override CustomAttributeCollection CustomAttributes {
			get {
				if (customAttributeCollection == null) {
					var list = readerModule.MetaData.GetCustomAttributeRidList(Table.Module, rid);
					var tmp = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
					Interlocked.CompareExchange(ref customAttributeCollection, tmp, null);
				}
				return customAttributeCollection;
			}
		}

		/// <inheritdoc/>
		public override Guid? EncBaseId {
			get { return encBaseId.Value; }
			set { encBaseId.Value = value; }
		}

		/// <inheritdoc/>
		public override AssemblyDef Assembly {
			get { return assembly.Value; }
			internal set { assembly.Value = value; }
		}

		/// <inheritdoc/>
		public override ThreadSafe.IList<TypeDef> Types {
			get { return types; }
		}

		/// <inheritdoc/>
		public override ThreadSafe.IList<ExportedType> ExportedTypes {
			get { return exportedTypes; }
		}

		/// <inheritdoc/>
		public override ResourceCollection Resources {
			get { return resources; }
		}

		/// <inheritdoc/>
		protected override RVA NativeEntryPoint_NoLock {
			get { return nativeEntryPoint.Value; }
			set { nativeEntryPoint.Value = value; }
		}

		/// <inheritdoc/>
		protected override IManagedEntryPoint ManagedEntryPoint_NoLock {
			get { return managedEntryPoint.Value; }
			set { managedEntryPoint.Value = value; }
		}

		/// <inheritdoc/>
		public override string Location {
			get { return location; }
			set { location = value ?? string.Empty; }
		}

		/// <inheritdoc/>
		public override Win32Resources Win32Resources {
			get { return win32Resources; }
			set { win32Resources = value; }
		}

		/// <inheritdoc/>
		public override VTableFixups VTableFixups {
			get { return vtableFixups; }
			set { vtableFixups = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>Module</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public ModuleDefMD2(ModuleDefMD readerModule, uint rid) {
			if (rid == 1 && readerModule == null)
				readerModule = (ModuleDefMD)this;
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid != 1 && readerModule.TablesStream.ModuleTable.IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("Module rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			if (rid != 1) {
				this.Kind = ModuleKind.Windows;
				this.Characteristics = DefaultCharacteristics;
				this.DllCharacteristics = DefaultDllCharacteristics;
				this.RuntimeVersion = MDHeaderRuntimeVersion.MS_CLR_20;
				this.Machine = Machine.I386;
				this.Cor20HeaderFlags = ComImageFlags.ILOnly;
				this.Cor20HeaderRuntimeVersion = 0x00020005;	// .NET 2.0 or later should use 2.5
				this.TablesHeaderVersion = 0x0200;				// .NET 2.0 or later should use 2.0
				this.types = new LazyList<TypeDef>(this);
				this.exportedTypes = new LazyList<ExportedType>();
				this.resources = new ResourceCollection();
				this.corLibTypes = new CorLibTypes(this);
				this.location = string.Empty;
			}
			Initialize();
		}

		void Initialize() {
			generation.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				return rawRow.Generation;
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				return readerModule.StringsStream.ReadNoNull(rawRow.Name);
			};
			mvid.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				return readerModule.GuidStream.Read(rawRow.Mvid);
			};
			encId.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				return readerModule.GuidStream.Read(rawRow.EncId);
			};
			encBaseId.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				return readerModule.GuidStream.Read(rawRow.EncBaseId);
			};
			assembly.ReadOriginalValue = () => {
				if (rid != 1)
					return null;
				return readerModule.ResolveAssembly(1);
			};
			nativeEntryPoint.ReadOriginalValue = () => {
				return readerModule.GetNativeEntryPoint();
			};
			managedEntryPoint.ReadOriginalValue = () => {
				return readerModule.GetManagedEntryPoint();
			};
#if THREAD_SAFE
			generation.Lock = theLock;
			name.Lock = theLock;
			mvid.Lock = theLock;
			encId.Lock = theLock;
			encBaseId.Lock = theLock;
			assembly.Lock = theLock;
			// nativeEntryPoint.Lock = theLock;		No lock for this one
			// managedEntryPoint.Lock = theLock;	No lock for this one
#endif
		}

		void InitializeRawRow_NoLock() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadModuleRow(rid) ?? new RawModuleRow();
		}
	}
}
