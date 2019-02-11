// dnlib: See LICENSE.txt for more info

namespace dnlib.W32Resources {
	/// <summary>
	/// Base class of <see cref="ResourceDirectory"/> and <see cref="ResourceData"/>
	/// </summary>
	public abstract class ResourceDirectoryEntry {
		ResourceName name;

		/// <summary>
		/// Gets/sets the name
		/// </summary>
		public ResourceName Name {
			get => name;
			set => name = value;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		protected ResourceDirectoryEntry(ResourceName name) => this.name = name;

		/// <inheritdoc/>
		public override string ToString() => name.ToString();
	}
}
