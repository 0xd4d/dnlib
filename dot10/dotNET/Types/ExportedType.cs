#pragma warning disable 0169	//TODO:

namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the ExportedType table
	/// </summary>
	public abstract class ExportedType : IHasCustomAttribute, IImplementation {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// From column ExportedType.Flags
		/// </summary>
		uint flags;

		/// <summary>
		/// From column ExportedType.TypeDefId
		/// </summary>
		uint typeDefId;

		/// <summary>
		/// From column ExportedType.TypeName
		/// </summary>
		string typeName;

		/// <summary>
		/// From column ExportedType.TypeNamespace
		/// </summary>
		string typeNamespace;

		/// <summary>
		/// From column ExportedType.Implementation
		/// </summary>
		IImplementation implementation;

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
	}

	/// <summary>
	/// A ExportedType row created by the user and not present in the original .NET file
	/// </summary>
	public class ExportedTypeUser : ExportedType {
	}
}
