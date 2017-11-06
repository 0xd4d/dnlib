// dnlib: See LICENSE.txt for more info

using System;
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
	/// A high-level representation of a row in the ModuleRef table
	/// </summary>
	public abstract class ModuleRef : IHasCustomAttribute, IMemberRefParent, IHasCustomDebugInformation, IResolutionScope, IModule, IOwnerModule {
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
		public UTF8String Name {
			get { return name; }
			set { name = value; }
		}
		/// <summary>Name</summary>
		protected UTF8String name;

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
			get { return 12; }
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
				var n = name;
				if (UTF8String.CaseInsensitiveEquals(n, module.Name))
					return module;
				var asm = DefinitionAssembly;
				return asm == null ? null : asm.FindModule(n);
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
			get { return UTF8String.ToSystemStringOrEmpty(name); }
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
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		public ModuleRefUser(ModuleDef module)
			: this(module, UTF8String.Empty) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="name">Module name</param>
		public ModuleRefUser(ModuleDef module, UTF8String name) {
			this.module = module;
			this.name = name;
		}
	}

	/// <summary>
	/// Created from a row in the ModuleRef table
	/// </summary>
	sealed class ModuleRefMD : ModuleRef, IMDTokenProviderMD {
		/// <summary>The module where this instance is located</summary>
		readonly ModuleDefMD readerModule;

		readonly uint origRid;

		/// <inheritdoc/>
		public uint OrigRid {
			get { return origRid; }
		}

		/// <inheritdoc/>
		protected override void InitializeCustomAttributes() {
			var list = readerModule.MetaData.GetCustomAttributeRidList(Table.ModuleRef, origRid);
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
			this.origRid = rid;
			this.rid = rid;
			this.readerModule = readerModule;
			this.module = readerModule;
			uint name = readerModule.TablesStream.ReadModuleRefRow2(origRid);
			this.name = readerModule.StringsStream.ReadNoNull(name);
		}
	}
}
