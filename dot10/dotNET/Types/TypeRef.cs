#pragma warning disable 0169	//TODO:

namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the TypeRef table
	/// </summary>
	public abstract class TypeRef : ITypeDefOrRef, IHasCustomAttribute, IMemberRefParent, IResolutionScope {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// From column TypeRef.ResolutionScope
		/// </summary>
		IResolutionScope resolutionScope;

		/// <summary>
		/// From column TypeRef.Name
		/// </summary>
		string name;

		/// <summary>
		/// From column TypeRef.Namespace
		/// </summary>
		string @namespace;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.TypeRef, rid); }
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
	}

	/// <summary>
	/// A TypeRef row created by the user and not present in the original .NET file
	/// </summary>
	public class TypeRefUser : TypeRef {
	}
}
