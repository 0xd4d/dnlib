// dnlib: See LICENSE.txt for more info

namespace dnlib.DotNet.Resources {
	/// <summary>
	/// Resource element
	/// </summary>
	public sealed class ResourceElement {
		IResourceData? resourceData;

		/// <summary>
		/// Name of resource. It's <c>""</c> until it's set.
		/// </summary>
		public string Name { get; set; } = "";

		/// <summary>
		/// Data of resource. If it hasn't been set, getting it returns (and stores) a new
		/// <see cref="BuiltInResourceData"/> with <see cref="ResourceTypeCode.Null"/>.
		/// </summary>
		public IResourceData ResourceData {
			get => resourceData ??= new BuiltInResourceData(ResourceTypeCode.Null, null);
			set => resourceData = value;
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public ResourceElement() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name of resource</param>
		/// <param name="resourceData">Data of resource</param>
		public ResourceElement(string name, IResourceData resourceData) {
			Name = name;
			this.resourceData = resourceData;
		}

		/// <inheritdoc/>
		public override string ToString() => $"N: {Name}, V: {ResourceData}";
	}
}
