#pragma warning disable 0169	//TODO:
using dot10.dotNET.MD;

namespace dot10.dotNET {
	/// <summary>
	/// A high-level representation of a row in the TypeSpec table
	/// </summary>
	public abstract class TypeSpec : ITypeDefOrRef, IHasCustomAttribute, IMemberRefParent {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// From column TypeSpec.Signature
		/// </summary>
		ISignature signature;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.TypeSpec, rid); }
		}

		/// <inheritdoc/>
		public int TypeDefOrRefTag {
			get { return 2; }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 13; }
		}

		/// <inheritdoc/>
		public int MemberRefParentTag {
			get { return 4; }
		}

		/// <inheritdoc/>
		public string Name {
			get { return "TODO:"; }
		}

		/// <inheritdoc/>
		public string ReflectionName {
			get { return "TODO:"; }
		}

		/// <inheritdoc/>
		string IFullName.Namespace {
			get { return "TODO:"; }
		}

		/// <inheritdoc/>
		public string ReflectionNamespace {
			get { return "TODO:"; }
		}

		/// <inheritdoc/>
		public string FullName {
			get { return "TODO:"; }
		}

		/// <inheritdoc/>
		public string ReflectionFullName {
			get { return "TODO:"; }
		}

		/// <inheritdoc/>
		public override string ToString() {
			return FullName;
		}
	}

	/// <summary>
	/// A TypeSpec row created by the user and not present in the original .NET file
	/// </summary>
	public class TypeSpecUser : TypeSpec {
	}

	/// <summary>
	/// Created from a row in the TypeSpec table
	/// </summary>
	sealed class TypeSpecMD : TypeSpec {
	}
}
