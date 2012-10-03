using System;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// A high-level representation of a row in the ModuleRef table
	/// </summary>
	public abstract class ModuleRef : IHasCustomAttribute, IMemberRefParent, IResolutionScope, IModule {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// The owner module
		/// </summary>
		protected ModuleDef ownerModule;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.ModuleRef, rid); }
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

		/// <summary>
		/// From column ModuleRef.Name
		/// </summary>
		public abstract UTF8String Name { get; set; }

		/// <inheritdoc/>
		public ModuleDef OwnerModule {
			get { return ownerModule; }
		}

		/// <summary>
		/// Gets the definition module, i.e., the module which it references, or <c>null</c>
		/// if the module can't be found.
		/// </summary>
		public ModuleDef DefinitionModule {
			get {
				if (ownerModule == null)
					return null;
				if (UTF8String.CaseInsensitiveEquals(Name, ownerModule.Name))
					return ownerModule;
				var asm = DefinitionAssembly;
				return asm == null ? null : asm.FindModule(Name);
			}
		}

		/// <summary>
		/// Gets the definition assembly, i.e., the assembly of the module it references, or
		/// <c>null</c> if the assembly can't be found.
		/// </summary>
		public AssemblyDef DefinitionAssembly {
			get { return ownerModule == null ? null : ownerModule.Assembly; }
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

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name; }
			set { name = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ownerModule">Owner module</param>
		public ModuleRefUser(ModuleDef ownerModule)
			: this(ownerModule, UTF8String.Empty) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ownerModule">Owner module</param>
		/// <param name="name">Module name</param>
		public ModuleRefUser(ModuleDef ownerModule, string name)
			: this(ownerModule, new UTF8String(name)) {
		}

		/// <summary>
		/// Constructor
		/// <param name="ownerModule">Owner module</param>
		/// </summary>
		/// <param name="name">Module name</param>
		public ModuleRefUser(ModuleDef ownerModule, UTF8String name) {
			this.ownerModule = ownerModule;
			this.name = name;
		}
	}

	/// <summary>
	/// Created from a row in the ModuleRef table
	/// </summary>
	sealed class ModuleRefMD : ModuleRef {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's <c>null</c> until <see cref="InitializeRawRow"/> is called</summary>
		RawModuleRefRow rawRow;

		UserValue<UTF8String> name;

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name.Value; }
			set { name.Value = value; }
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
			if (readerModule.TablesStream.Get(Table.ModuleRef).IsInvalidRID(rid))
				throw new BadImageFormatException(string.Format("ModuleRef rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			this.ownerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			name.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.Name);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadModuleRefRow(rid);
		}
	}
}
