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

﻿using System;
using dnlib.Utils;
using dnlib.DotNet.MD;

namespace dnlib.DotNet {
	/// <summary>
	/// A high-level representation of a row in the TypeRef table
	/// </summary>
	public abstract class TypeRef : ITypeDefOrRef, IHasCustomAttribute, IMemberRefParent, IResolutionScope {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// The owner module
		/// </summary>
		protected ModuleDef module;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.TypeRef, rid); }
		}

		/// <inheritdoc/>
		public uint Rid {
			get { return rid; }
			set { rid = value; }
		}

		/// <inheritdoc/>
		public int TypeDefOrRefTag {
			get { return 1; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 2; }
		}

		/// <inheritdoc/>
		public int MemberRefParentTag {
			get { return 1; }
		}

		/// <inheritdoc/>
		public int ResolutionScopeTag {
			get { return 3; }
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

		/// <inheritdoc/>
		string IType.TypeName {
			get { return FullNameCreator.Name(this, false); }
		}

		/// <inheritdoc/>
		public string ReflectionName {
			get { return FullNameCreator.Name(this, true); }
		}

		/// <inheritdoc/>
		string IType.Namespace {
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
			get { return this; }
		}

		/// <inheritdoc/>
		public ModuleDef Module {
			get { return module; }
		}

		/// <summary>
		/// From column TypeRef.ResolutionScope
		/// </summary>
		public abstract IResolutionScope ResolutionScope { get; set; }

		/// <summary>
		/// From column TypeRef.Name
		/// </summary>
		public abstract UTF8String Name { get; set; }

		/// <summary>
		/// From column TypeRef.Namespace
		/// </summary>
		public abstract UTF8String Namespace { get; set; }

		/// <summary>
		/// Gets all custom attributes
		/// </summary>
		public abstract CustomAttributeCollection CustomAttributes { get; }

		/// <inheritdoc/>
		public bool HasCustomAttributes {
			get { return CustomAttributes.Count > 0; }
		}

		/// <summary>
		/// <c>true</c> if it's nested within another <see cref="TypeRef"/>
		/// </summary>
		public bool IsNested {
			get { return DeclaringType != null; }
		}

		/// <inheritdoc/>
		public bool IsValueType {
			get {
				var td = Resolve();
				return td != null && td.IsValueType;
			}
		}

		/// <summary>
		/// Gets the declaring type, if any
		/// </summary>
		public TypeRef DeclaringType {
			get { return ResolutionScope as TypeRef; }
		}

		/// <summary>
		/// Resolves the type
		/// </summary>
		/// <returns>A <see cref="TypeDef"/> instance or <c>null</c> if it couldn't be resolved</returns>
		public TypeDef Resolve() {
			if (module == null)
				return null;
			return module.Context.Resolver.Resolve(this);
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

		/// <summary>
		/// Gets the top-most (non-nested) <see cref="TypeRef"/>
		/// </summary>
		/// <param name="typeRef">Input</param>
		/// <returns>The non-nested <see cref="TypeRef"/> or <c>null</c></returns>
		internal static TypeRef GetNonNestedTypeRef(TypeRef typeRef) {
			if (typeRef == null)
				return null;
			for (int i = 0; i < 1000; i++) {
				var next = typeRef.ResolutionScope as TypeRef;
				if (next == null)
					return typeRef;
				typeRef = next;
			}
			return null;	// Should never happen
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// A TypeRef row created by the user and not present in the original .NET file
	/// </summary>
	public class TypeRefUser : TypeRef {
		IResolutionScope resolutionScope;
		UTF8String name;
		UTF8String @namespace;
		CustomAttributeCollection customAttributeCollection = new CustomAttributeCollection();

		/// <inheritdoc/>
		public override IResolutionScope ResolutionScope {
			get { return resolutionScope; }
			set { resolutionScope = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name; }
			set { name = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Namespace {
			get { return @namespace; }
			set { @namespace = value; }
		}

		/// <inheritdoc/>
		public override CustomAttributeCollection CustomAttributes {
			get { return customAttributeCollection; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="name">Type name</param>
		public TypeRefUser(ModuleDef module, UTF8String name)
			: this(module, UTF8String.Empty, name) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="namespace">Type namespace</param>
		/// <param name="name">Type name</param>
		public TypeRefUser(ModuleDef module, UTF8String @namespace, UTF8String name)
			: this(module, @namespace, name, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="namespace">Type namespace</param>
		/// <param name="name">Type name</param>
		/// <param name="resolutionScope">Resolution scope (a <see cref="ModuleDef"/>,
		/// <see cref="ModuleRef"/>, <see cref="AssemblyRef"/> or <see cref="TypeRef"/>)</param>
		public TypeRefUser(ModuleDef module, UTF8String @namespace, UTF8String name, IResolutionScope resolutionScope) {
			this.module = module;
			this.resolutionScope = resolutionScope;
			this.name = name;
			this.@namespace = @namespace;
		}
	}

	/// <summary>
	/// Created from a row in the TypeRef table
	/// </summary>
	sealed class TypeRefMD : TypeRef {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow"/> is called</summary>
		RawTypeRefRow rawRow;

		UserValue<IResolutionScope> resolutionScope;
		UserValue<UTF8String> name;
		UserValue<UTF8String> @namespace;
		CustomAttributeCollection customAttributeCollection;

		/// <inheritdoc/>
		public override IResolutionScope ResolutionScope {
			get { return resolutionScope.Value; }
			set { resolutionScope.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Namespace {
			get { return @namespace.Value; }
			set { @namespace.Value = value; }
		}

		/// <inheritdoc/>
		public override CustomAttributeCollection CustomAttributes {
			get {
				if (customAttributeCollection == null) {
					var list = readerModule.MetaData.GetCustomAttributeRidList(Table.TypeRef, rid);
					customAttributeCollection = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
				}
				return customAttributeCollection;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>TypeRef</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public TypeRefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.TypeRefTable.IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("TypeRef rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			this.module = readerModule;
			Initialize();
		}

		void Initialize() {
			resolutionScope.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveResolutionScope(rawRow.ResolutionScope);
			};
			name.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.ReadNoNull(rawRow.Name);
			};
			@namespace.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.ReadNoNull(rawRow.Namespace);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadTypeRefRow(rid);
		}
	}
}
