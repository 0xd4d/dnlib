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
	/// A high-level representation of a row in the ModuleRef table
	/// </summary>
	public abstract class ModuleRef : IHasCustomAttribute, IMemberRefParent, IResolutionScope, IModule, IOwnerModule {
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
			get { return new MDToken(Table.ModuleRef, rid); }
		}

		/// <inheritdoc/>
		public uint Rid {
			get { return rid; }
			set { rid = value; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 12; }
		}

		/// <inheritdoc/>
		public int MemberRefParentTag {
			get { return 2; }
		}

		/// <inheritdoc/>
		public int ResolutionScopeTag {
			get { return 1; }
		}

		/// <inheritdoc/>
		public ScopeType ScopeType {
			get { return ScopeType.ModuleRef; }
		}

		/// <inheritdoc/>
		public string ScopeName {
			get { return FullName; }
		}

		/// <summary>
		/// From column ModuleRef.Name
		/// </summary>
		public abstract UTF8String Name { get; set; }

		/// <summary>
		/// Gets all custom attributes
		/// </summary>
		public abstract CustomAttributeCollection CustomAttributes { get; }

		/// <inheritdoc/>
		public bool HasCustomAttributes {
			get { return CustomAttributes.Count > 0; }
		}

		/// <inheritdoc/>
		public ModuleDef Module {
			get { return module; }
		}

		/// <summary>
		/// Gets the definition module, i.e., the module which it references, or <c>null</c>
		/// if the module can't be found.
		/// </summary>
		public ModuleDef DefinitionModule {
			get {
				if (module == null)
					return null;
				var name = Name;
				if (UTF8String.CaseInsensitiveEquals(name, module.Name))
					return module;
				var asm = DefinitionAssembly;
				return asm == null ? null : asm.FindModule(name);
			}
		}

		/// <summary>
		/// Gets the definition assembly, i.e., the assembly of the module it references, or
		/// <c>null</c> if the assembly can't be found.
		/// </summary>
		public AssemblyDef DefinitionAssembly {
			get { return module == null ? null : module.Assembly; }
		}

		/// <inheritdoc/>
		public string FullName {
			get { return UTF8String.ToSystemStringOrEmpty(Name); }
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// A ModuleRef row created by the user and not present in the original .NET file
	/// </summary>
	public class ModuleRefUser : ModuleRef {
		UTF8String name;
		readonly CustomAttributeCollection customAttributeCollection = new CustomAttributeCollection();

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name; }
			set { name = value; }
		}

		/// <inheritdoc/>
		public override CustomAttributeCollection CustomAttributes {
			get { return customAttributeCollection; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		public ModuleRefUser(ModuleDef module)
			: this(module, UTF8String.Empty) {
		}

		/// <summary>
		/// Constructor
		/// <param name="module">Owner module</param>
		/// </summary>
		/// <param name="name">Module name</param>
		public ModuleRefUser(ModuleDef module, UTF8String name) {
			this.module = module;
			this.name = name;
		}
	}

	/// <summary>
	/// Created from a row in the ModuleRef table
	/// </summary>
	sealed class ModuleRefMD : ModuleRef {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow_NoLock"/> is called</summary>
		RawModuleRefRow rawRow;

		UserValue<UTF8String> name;
		CustomAttributeCollection customAttributeCollection;
#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

		/// <inheritdoc/>
		public override CustomAttributeCollection CustomAttributes {
			get {
				if (customAttributeCollection == null) {
					var list = readerModule.MetaData.GetCustomAttributeRidList(Table.ModuleRef, rid);
					var tmp = new CustomAttributeCollection((int)list.Length, list, (list2, index) => readerModule.ReadCustomAttribute(((RidList)list2)[index]));
					Interlocked.CompareExchange(ref customAttributeCollection, tmp, null);
				}
				return customAttributeCollection;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="readerModule">The module which contains this <c>ModuleRef</c> row</param>
		/// <param name="rid">Row ID</param>
		/// <exception cref="ArgumentNullException">If <paramref name="readerModule"/> is <c>null</c></exception>
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is invalid</exception>
		public ModuleRefMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (readerModule.TablesStream.ModuleRefTable.IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("ModuleRef rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			this.module = readerModule;
			Initialize();
		}

		void Initialize() {
			name.ReadOriginalValue = () => {
				InitializeRawRow_NoLock();
				return readerModule.StringsStream.ReadNoNull(rawRow.Name);
			};
#if THREAD_SAFE
			name.Lock = theLock;
#endif
		}

		void InitializeRawRow_NoLock() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadModuleRefRow(rid);
		}
	}
}
