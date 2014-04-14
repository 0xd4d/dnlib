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
using System.Threading;
using dnlib.Utils;
using dnlib.DotNet.MD;
using dnlib.Threading;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the ExportedType table
	/// </summary>
	public abstract class ExportedType : IHasCustomAttribute, IImplementation, IType {
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
		/// The owner module
		/// </summary>
		protected ModuleDef module;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.ExportedType, rid); }
		}

		/// <inheritdoc/>
		public uint Rid {
			get { return rid; }
			set { rid = value; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 17; }
		}

		/// <inheritdoc/>
		public int ImplementationTag {
			get { return 2; }
		}

		/// <summary>
		/// Gets all custom attributes
		/// </summary>
		public abstract CustomAttributeCollection CustomAttributes { get; }

		/// <inheritdoc/>
		public bool HasCustomAttributes {
			get { return CustomAttributes.Count > 0; }
		}

		/// <inheritdoc/>
		public bool IsValueType {
			get {
				var td = Resolve();
				return td != null && td.IsValueType;
			}
		}

		/// <inheritdoc/>
		string IType.TypeName {
			get { return FullNameCreator.Name(this, false); }
		}

		/// <inheritdoc/>
		public string ReflectionName {
			get { return FullNameCreator.Name(this, true); }
		}

		/// <inheritdoc/>
		public string Namespace {
			get { return FullNameCreator.Namespace(this, false); }
		}

		/// <inheritdoc/>
		public string ReflectionNamespace {
			get { return FullNameCreator.Namespace(this, true); }
		}

		/// <inheritdoc/>
		public string FullName {
			get { return FullNameCreator.FullName(this, false); }
		}

		/// <inheritdoc/>
		public string ReflectionFullName {
			get { return FullNameCreator.FullName(this, true); }
		}

		/// <inheritdoc/>
		public string AssemblyQualifiedName {
			get { return FullNameCreator.AssemblyQualifiedName(this); }
		}

		/// <inheritdoc/>
		public IAssembly DefinitionAssembly {
			get { return FullNameCreator.DefinitionAssembly(this); }
		}

		/// <inheritdoc/>
		public IScope Scope {
			get { return FullNameCreator.Scope(this); }
		}

		/// <inheritdoc/>
		public ITypeDefOrRef ScopeType {
			get { return FullNameCreator.ScopeType(this); }
		}

		/// <inheritdoc/>
		public ModuleDef Module {
			get { return module; }
		}

		/// <inheritdoc/>
		bool IGenericParameterProvider.IsMethod {
			get { return false; }
		}

		/// <inheritdoc/>
		bool IGenericParameterProvider.IsType {
			get { return true; }
		}

		/// <inheritdoc/>
		int IGenericParameterProvider.NumberOfGenericParameters {
			get { return 0; }
		}

		/// <summary>
		/// From column ExportedType.Flags
		/// </summary>
		public TypeAttributes Attributes {
#if THREAD_SAFE
			get {
				theLock.EnterWriteLock();
				try {
					return Attributes_NoLock;
				}
				finally { theLock.ExitWriteLock(); }
			}
			set {
				theLock.EnterWriteLock();
				try {
					Attributes_NoLock = value;
				}
				finally { theLock.ExitWriteLock(); }
			}
#else
			get { return Attributes_NoLock; }
			set { Attributes_NoLock = value; }
#endif
		}

		/// <summary>
		/// From column ExportedType.Flags
		/// </summary>
		protected abstract TypeAttributes Attributes_NoLock { get; set; }

		/// <summary>
		/// From column ExportedType.TypeDefId
		/// </summary>
		public abstract uint TypeDefId { get; set; }

		/// <summary>
		/// From column ExportedType.TypeName
		/// </summary>
		public abstract UTF8String TypeName { get; set; }

		/// <summary>
		/// From column ExportedType.TypeNamespace
		/// </summary>
		public abstract UTF8String TypeNamespace { get; set; }

		/// <summary>
		/// From column ExportedType.Implementation
		/// </summary>
		public abstract IImplementation Implementation { get; set; }

		/// <summary>
		/// <c>true</c> if it's nested within another <see cref="ExportedType"/>
		/// </summary>
		public bool IsNested {
			get { return DeclaringType != null; }
		}

		/// <summary>
		/// Gets the declaring type, if any
		/// </summary>
		public ExportedType DeclaringType {
			get { return Implementation as ExportedType; }
		}

		/// <summary>
		/// Modify <see cref="Attributes_NoLock"/> property: <see cref="Attributes_NoLock"/> =
		/// (<see cref="Attributes_NoLock"/> &amp; <paramref name="andMask"/>) | <paramref name="orMask"/>.
		/// </summary>
		/// <param name="andMask">Value to <c>AND</c></param>
		/// <param name="orMask">Value to OR</param>
		void ModifyAttributes(TypeAttributes andMask, TypeAttributes orMask) {
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
		void ModifyAttributes(bool set, TypeAttributes flags) {
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
		/// Gets/sets the visibility
		/// </summary>
		public TypeAttributes Visibility {
			get { return Attributes & TypeAttributes.VisibilityMask; }
			set { ModifyAttributes(~TypeAttributes.VisibilityMask, value & TypeAttributes.VisibilityMask); }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.NotPublic"/> is set
		/// </summary>
		public bool IsNotPublic {
			get { return (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.Public"/> is set
		/// </summary>
		public bool IsPublic {
			get { return (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.NestedPublic"/> is set
		/// </summary>
		public bool IsNestedPublic {
			get { return (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPublic; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.NestedPrivate"/> is set
		/// </summary>
		public bool IsNestedPrivate {
			get { return (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPrivate; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.NestedFamily"/> is set
		/// </summary>
		public bool IsNestedFamily {
			get { return (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamily; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.NestedAssembly"/> is set
		/// </summary>
		public bool IsNestedAssembly {
			get { return (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedAssembly; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.NestedFamANDAssem"/> is set
		/// </summary>
		public bool IsNestedFamilyAndAssembly {
			get { return (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamANDAssem; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.NestedFamORAssem"/> is set
		/// </summary>
		public bool IsNestedFamilyOrAssembly {
			get { return (Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamORAssem; }
		}

		/// <summary>
		/// Gets/sets the layout
		/// </summary>
		public TypeAttributes Layout {
			get { return Attributes & TypeAttributes.LayoutMask; }
			set { ModifyAttributes(~TypeAttributes.LayoutMask, value & TypeAttributes.LayoutMask); }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.AutoLayout"/> is set
		/// </summary>
		public bool IsAutoLayout {
			get { return (Attributes & TypeAttributes.LayoutMask) == TypeAttributes.AutoLayout; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.SequentialLayout"/> is set
		/// </summary>
		public bool IsSequentialLayout {
			get { return (Attributes & TypeAttributes.LayoutMask) == TypeAttributes.SequentialLayout; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.ExplicitLayout"/> is set
		/// </summary>
		public bool IsExplicitLayout {
			get { return (Attributes & TypeAttributes.LayoutMask) == TypeAttributes.ExplicitLayout; }
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.Interface"/> bit
		/// </summary>
		public bool IsInterface {
			get { return (Attributes & TypeAttributes.Interface) != 0; }
			set { ModifyAttributes(value, TypeAttributes.Interface); }
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.Class"/> bit
		/// </summary>
		public bool IsClass {
			get { return (Attributes & TypeAttributes.Interface) == 0; }
			set { ModifyAttributes(!value, TypeAttributes.Interface); }
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.Abstract"/> bit
		/// </summary>
		public bool IsAbstract {
			get { return (Attributes & TypeAttributes.Abstract) != 0; }
			set { ModifyAttributes(value, TypeAttributes.Abstract); }
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.Sealed"/> bit
		/// </summary>
		public bool IsSealed {
			get { return (Attributes & TypeAttributes.Sealed) != 0; }
			set { ModifyAttributes(value, TypeAttributes.Sealed); }
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.SpecialName"/> bit
		/// </summary>
		public bool IsSpecialName {
			get { return (Attributes & TypeAttributes.SpecialName) != 0; }
			set { ModifyAttributes(value, TypeAttributes.SpecialName); }
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.Import"/> bit
		/// </summary>
		public bool IsImport {
			get { return (Attributes & TypeAttributes.Import) != 0; }
			set { ModifyAttributes(value, TypeAttributes.Import); }
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.Serializable"/> bit
		/// </summary>
		public bool IsSerializable {
			get { return (Attributes & TypeAttributes.Serializable) != 0; }
			set { ModifyAttributes(value, TypeAttributes.Serializable); }
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.WindowsRuntime"/> bit
		/// </summary>
		public bool IsWindowsRuntime {
			get { return (Attributes & TypeAttributes.WindowsRuntime) != 0; }
			set { ModifyAttributes(value, TypeAttributes.WindowsRuntime); }
		}

		/// <summary>
		/// Gets/sets the string format
		/// </summary>
		public TypeAttributes StringFormat {
			get { return Attributes & TypeAttributes.StringFormatMask; }
			set { ModifyAttributes(~TypeAttributes.StringFormatMask, value & TypeAttributes.StringFormatMask); }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.AnsiClass"/> is set
		/// </summary>
		public bool IsAnsiClass {
			get { return (Attributes & TypeAttributes.StringFormatMask) == TypeAttributes.AnsiClass; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.UnicodeClass"/> is set
		/// </summary>
		public bool IsUnicodeClass {
			get { return (Attributes & TypeAttributes.StringFormatMask) == TypeAttributes.UnicodeClass; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.AutoClass"/> is set
		/// </summary>
		public bool IsAutoClass {
			get { return (Attributes & TypeAttributes.StringFormatMask) == TypeAttributes.AutoClass; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="TypeAttributes.CustomFormatClass"/> is set
		/// </summary>
		public bool IsCustomFormatClass {
			get { return (Attributes & TypeAttributes.StringFormatMask) == TypeAttributes.CustomFormatClass; }
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.BeforeFieldInit"/> bit
		/// </summary>
		public bool IsBeforeFieldInit {
			get { return (Attributes & TypeAttributes.BeforeFieldInit) != 0; }
			set { ModifyAttributes(value, TypeAttributes.BeforeFieldInit); }
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.Forwarder"/> bit
		/// </summary>
		public bool IsForwarder {
			get { return (Attributes & TypeAttributes.Forwarder) != 0; }
			set { ModifyAttributes(value, TypeAttributes.Forwarder); }
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.RTSpecialName"/> bit
		/// </summary>
		public bool IsRuntimeSpecialName {
			get { return (Attributes & TypeAttributes.RTSpecialName) != 0; }
			set { ModifyAttributes(value, TypeAttributes.RTSpecialName); }
		}

		/// <summary>
		/// Gets/sets the <see cref="TypeAttributes.HasSecurity"/> bit
		/// </summary>
		public bool HasSecurity {
			get { return (Attributes & TypeAttributes.HasSecurity) != 0; }
			set { ModifyAttributes(value, TypeAttributes.HasSecurity); }
		}

		/// <summary>
		/// Resolves the type
		/// </summary>
		/// <returns>A <see cref="TypeDef"/> instance or <c>null</c> if it couldn't be resolved</returns>
		public TypeDef Resolve() {
			if (module == null)
				return null;
			var etAsm = module.Context.AssemblyResolver.Resolve(DefinitionAssembly, module);
			if (etAsm == null)
				return null;

			return etAsm.Find(this.FullName, false);
		}

		/// <summary>
		/// Resolves the type
		/// </summary>
		/// <returns>A <see cref="TypeDef"/> instance</returns>
		/// <exception cref="TypeResolveException">If the type couldn't be resolved</exception>
		public TypeDef ResolveThrow() {
			var type = Resolve();
			if (type != null)
				return type;
			throw new TypeResolveException(string.Format("Could not resolve type: {0}", this));
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// An ExportedType row created by the user and not present in the original .NET file
	/// </summary>
	public class ExportedTypeUser : ExportedType {
		readonly CustomAttributeCollection customAttributeCollection = new CustomAttributeCollection();
		TypeAttributes flags;
		uint typeDefId;
		UTF8String typeName;
		UTF8String typeNamespace;
		IImplementation implementation;

		/// <inheritdoc/>
		public override CustomAttributeCollection CustomAttributes {
			get { return customAttributeCollection; }
		}

		/// <inheritdoc/>
		protected override TypeAttributes Attributes_NoLock {
			get { return flags; }
			set { flags = value; }
		}

		/// <inheritdoc/>
		public override uint TypeDefId {
			get { return typeDefId; }
			set { typeDefId = value; }
		}

		/// <inheritdoc/>
		public override UTF8String TypeName {
			get { return typeName; }
			set { typeName = value; }
		}

		/// <inheritdoc/>
		public override UTF8String TypeNamespace {
			get { return typeNamespace; }
			set { typeNamespace = value; }
		}

		/// <inheritdoc/>
		public override IImplementation Implementation {
			get { return implementation; }
			set { implementation = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		public ExportedTypeUser(ModuleDef module) {
			this.module = module;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="typeDefId">TypeDef ID</param>
		/// <param name="typeName">Type name</param>
		/// <param name="typeNamespace">Type namespace</param>
		/// <param name="flags">Flags</param>
		/// <param name="implementation">Implementation</param>
		public ExportedTypeUser(ModuleDef module, uint typeDefId, UTF8String typeNamespace, UTF8String typeName, TypeAttributes flags, IImplementation implementation) {
			this.module = module;
			this.typeDefId = typeDefId;
			this.typeName = typeName;
			this.typeNamespace = typeNamespace;
			this.flags = flags;
			this.implementation = implementation;
		}
	}

	/// <summary>
	/// Created from a row in the ExportedType table
	/// </summary>
	sealed class ExportedTypeMD : ExportedType {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow_NoLock"/> is called</summary>
		RawExportedTypeRow rawRow;

		CustomAttributeCollection customAttributeCollection;
		UserValue<TypeAttributes> flags;
		UserValue<uint> typeDefId;
		UserValue<UTF8String> typeName;
		UserValue<UTF8String> typeNamespace;
		UserValue<IImplementation> implementation;

		/// <inheritdoc/>
		public override CustomAttributeCollection CustomAttributes {
			get {
				if (customAttributeCollection == null) {
					var list = readerModule.MetaData.GetCustomAttributeRidList(Table.ExportedType, rid);
					var tmp = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
					Interlocked.CompareExchange(ref customAttributeCollection, tmp, null);
				}
				return customAttributeCollection;
			}
		}

		/// <inheritdoc/>
		protected override TypeAttributes Attributes_NoLock {
			get { return flags.Value; }
			set { flags.Value = value; }
		}

		/// <inheritdoc/>
		public override uint TypeDefId {
			get { return typeDefId.Value; }
			set { typeDefId.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String TypeName {
			get { return typeName.Value; }
			set { typeName.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String TypeNamespace {
			get { return typeNamespace.Value; }
			set { typeNamespace.Value = value; }
		}

		/// <inheritdoc/>
		public override IImplementation Implementation {
			get { return implementation.Value; }
			set { implementation.Value = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>ExportedType</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public ExportedTypeMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.ExportedTypeTable.IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("ExportedType rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			this.module = readerModule;
			Initialize();
		}

		void Initialize() {
			flags.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				return (TypeAttributes)rawRow.Flags;
			};
			typeDefId.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				return rawRow.TypeDefId;
			};
			typeName.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				return readerModule.StringsStream.ReadNoNull(rawRow.TypeName);
			};
			typeNamespace.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				return readerModule.StringsStream.ReadNoNull(rawRow.TypeNamespace);
			};
			implementation.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				return readerModule.ResolveImplementation(rawRow.Implementation);
			};
#if THREAD_SAFE
			// flags.Lock = theLock;	No lock for this one
			typeDefId.Lock = theLock;
			typeName.Lock = theLock;
			typeNamespace.Lock = theLock;
			implementation.Lock = theLock;
#endif
		}

		void InitializeRawRow_NoLock() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadExportedTypeRow(rid);
		}
	}
}
