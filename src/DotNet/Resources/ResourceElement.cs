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

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name of resource</param>
		/// <param name="resourceData">Data of resource</param>
		public ResourceElement(string name, IResourceData resourceData) {
			Name = name;
			ResourceData = resourceData;
		}

		/// <inheritdoc/>
		public override string ToString() => $"N: {Name}, V: {ResourceData}";
	}
}
