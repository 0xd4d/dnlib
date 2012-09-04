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
}
