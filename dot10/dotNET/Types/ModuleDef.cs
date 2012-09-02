using System;

namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the Module table
	/// </summary>
	public class ModuleDef : IHasCustomAttribute, IResolutionScope {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// From column Module.Generation
		/// </summary>
		protected ushort generation;

		/// <summary>
		/// From column Module.Name
		/// </summary>
		protected string name;

		/// <summary>
		/// From column Module.Mvid
		/// </summary>
		protected Guid mvid;

		/// <summary>
		/// From column Module.EncId
		/// </summary>
		protected Guid encId;

		/// <summary>
		/// From column Module.EncBaseId
		/// </summary>
		protected Guid encBaseId;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.Module, rid); }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 7; }
		}

		/// <inheritdoc/>
		public int ResolutionScopeTag {
			get { return 0; }
		}
	}
}
