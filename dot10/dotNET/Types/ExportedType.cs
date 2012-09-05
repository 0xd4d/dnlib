using System;
using System.Diagnostics;

namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the ExportedType table
	/// </summary>
	[DebuggerDisplay("{TypeDefId} {TypeNamespace}.{TypeName} {Implementation} {Flags}")]
	public abstract class ExportedType : IHasCustomAttribute, IImplementation {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.ExportedType, rid); }
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
		/// From column ExportedType.Flags
		/// </summary>
		public abstract TypeAttributes Flags { get; set; }

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
	}

	/// <summary>
	/// An ExportedType row created by the user and not present in the original .NET file
	/// </summary>
	public class ExportedTypeUser : ExportedType {
		TypeAttributes flags;
		uint typeDefId;
		UTF8String typeName;
		UTF8String typeNamespace;
		IImplementation implementation;

		/// <inheritdoc/>
		public override TypeAttributes Flags {
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
		/// Default constructor
		/// </summary>
		public ExportedTypeUser() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="typeDefId">TypeDef ID</param>
		/// <param name="typeName">Type name</param>
		/// <param name="typeNamespace">Type namespace</param>
		/// <param name="flags">Flags</param>
		/// <param name="implementation">Implementation</param>
		public ExportedTypeUser(uint typeDefId, UTF8String typeName, UTF8String typeNamespace, TypeAttributes flags, IImplementation implementation) {
			this.typeDefId = typeDefId;
			this.typeName = typeName;
			this.typeNamespace = typeNamespace;
			this.flags = flags;
			this.implementation = implementation;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="typeDefId">TypeDef ID</param>
		/// <param name="typeName">Type name</param>
		/// <param name="typeNamespace">Type namespace</param>
		/// <param name="flags">Flags</param>
		/// <param name="implementation">Implementation</param>
		public ExportedTypeUser(uint typeDefId, string typeName, string typeNamespace, TypeAttributes flags, IImplementation implementation)
			: this(typeDefId, new UTF8String(typeName), new UTF8String(typeNamespace), flags, implementation) {
		}
	}

	/// <summary>
	/// Created from a row in the ExportedType table
	/// </summary>
	sealed class ExportedTypeMD : ExportedType {
		/// <summary>The module where this instance is located</summary>
		ModuleDefMD readerModule;
		/// <summary>The raw table row. It's null until <see cref="InitializeRawRow"/> is called</summary>
		RawExportedTypeRow rawRow;

		UserValue<TypeAttributes> flags;
		UserValue<uint> typeDefId;
		UserValue<UTF8String> typeName;
		UserValue<UTF8String> typeNamespace;
		UserValue<IImplementation> implementation;

		/// <inheritdoc/>
		public override TypeAttributes Flags {
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
		/// <exception cref="ArgumentException">If <paramref name="rid"/> is <c>0</c> or &gt; <c>0x00FFFFFF</c></exception>
		public ExportedTypeMD(ModuleDefMD readerModule, uint rid) {
#if DEBUG
			if (readerModule == null)
				throw new ArgumentNullException("readerModule");
			if (rid == 0 || rid > 0x00FFFFFF)
				throw new ArgumentException("rid");
			if (readerModule.TablesStream.Get(Table.ExportedType).Rows < rid)
				throw new BadImageFormatException(string.Format("ExportedType rid {0} does not exist", rid));
#endif
			this.rid = rid;
			this.readerModule = readerModule;
			Initialize();
		}

		void Initialize() {
			flags.ReadOriginalValue = () => {
				InitializeRawRow();
				return (TypeAttributes)rawRow.Flags;
			};
			typeDefId.ReadOriginalValue = () => {
				InitializeRawRow();
				return rawRow.TypeDefId;
			};
			typeName.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.TypeName);
			};
			typeNamespace.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.StringsStream.Read(rawRow.TypeNamespace);
			};
			implementation.ReadOriginalValue = () => {
				InitializeRawRow();
				return readerModule.ResolveImplementation(rawRow.Implementation);
			};
		}

		void InitializeRawRow() {
			if (rawRow != null)
				return;
			rawRow = readerModule.TablesStream.ReadExportedTypeRow(rid);
		}
	}
}
