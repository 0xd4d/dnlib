#pragma warning disable 0169	//TODO:

using System.Collections.Generic;
using dot10.PE;

namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the Method table
	/// </summary>
	public class MethodDef : IHasCustomAttribute, IHasDeclSecurity, IMemberRefParent, IMethodDefOrRef, IMemberForwarded, ICustomAttributeType, ITypeOrMethodDef {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// From column Method.RVA
		/// </summary>
		RVA rva;

		/// <summary>
		/// From column Method.ImplFlags
		/// </summary>
		ushort implFlags;

		/// <summary>
		/// From column Method.Flags
		/// </summary>
		ushort flags;

		/// <summary>
		/// From column Method.Name
		/// </summary>
		string name;

		/// <summary>
		/// From column Method.Signature
		/// </summary>
		ISignature signature;

		/// <summary>
		/// From column Method.ParamList
		/// </summary>
		IList<ParamDef> paramList;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.Method, rid); }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 0; }
		}

		/// <inheritdoc/>
		public int HasDeclSecurityTag {
			get { return 1; }
		}

		/// <inheritdoc/>
		public int MemberRefParentTag {
			get { return 3; }
		}

		/// <inheritdoc/>
		public int MethodDefOrRefTag {
			get { return 0; }
		}

		/// <inheritdoc/>
		public int MemberForwardedTag {
			get { return 1; }
		}

		/// <inheritdoc/>
		public int CustomAttributeTypeTag {
			get { return 2; }
		}

		/// <inheritdoc/>
		public int TypeOrMethodDefTag {
			get { return 1; }
		}
	}
}
