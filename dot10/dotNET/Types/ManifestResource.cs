#pragma warning disable 0169	//TODO:

namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the ManifestResource table
	/// </summary>
	public abstract class ManifestResource : IHasCustomAttribute {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// From column ManifestResource.Offset
		/// </summary>
		uint offset;

		/// <summary>
		/// From column ManifestResource.Flags
		/// </summary>
		uint flags;

		/// <summary>
		/// From column ManifestResource.Name
		/// </summary>
		string name;

		/// <summary>
		/// From column ManifestResource.Implementation
		/// </summary>
		IImplementation implementation;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.ManifestResource, rid); }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 18; }
		}
	}

	/// <summary>
	/// A ManifestResource row created by the user and not present in the original .NET file
	/// </summary>
	public class ManifestResourceUser : ManifestResource {
	}
}
