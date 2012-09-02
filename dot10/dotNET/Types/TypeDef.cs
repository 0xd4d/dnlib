#pragma warning disable 0169	//TODO:

using System.Collections.Generic;

namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the TypeDef table
	/// </summary>
	public class TypeDef : ITypeDefOrRef, IHasCustomAttribute, IHasDeclSecurity, IMemberRefParent, ITypeOrMethodDef {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// From column TypeDef.Flags
		/// </summary>
		uint flags;

		/// <summary>
		/// From column TypeDef.Name
		/// </summary>
		string name;

		/// <summary>
		/// From column TypeDef.Namespace
		/// </summary>
		string @namespace;

		/// <summary>
		/// From column TypeDef.Extends
		/// </summary>
		ITypeDefOrRef extends;

		/// <summary>
		/// From column TypeDef.FieldList
		/// </summary>
		IList<FieldDef> fieldList;

		/// <summary>
		/// From column TypeDef.MethodList
		/// </summary>
		IList<MethodDef> methodList;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.TypeDef, rid); }
		}

		/// <inheritdoc/>
		public int TypeDefOrRefTag {
			get { return 0; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 3; }
		}

		/// <inheritdoc/>
		public int HasDeclSecurityTag {
			get { return 0; }
		}

		/// <inheritdoc/>
		public int MemberRefParentTag {
			get { return 0; }
		}

		/// <inheritdoc/>
		public int TypeOrMethodDefTag {
			get { return 0; }
		}
	}
}
