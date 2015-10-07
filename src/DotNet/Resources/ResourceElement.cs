// dnlib: See LICENSE.txt for more info

namespace dnlib.DotNet.Resources {
	/// <summary>
	/// Resource element
	/// </summary>
	public sealed class ResourceElement {
		/// <summary>
		/// Name of resource
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Data of resource
		/// </summary>
		public IResourceData ResourceData { get; set; }

		/// <inheritdoc/>
		public override string ToString() {
			return string.Format("N: {0}, V: {1}", Name, ResourceData);
		}
	}
}
