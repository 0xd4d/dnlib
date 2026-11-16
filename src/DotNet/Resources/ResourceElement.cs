// dnlib: See LICENSE.txt for more info

#nullable enable

namespace dnlib.DotNet.Resources {
	/// <summary>
	/// Resource element
	/// </summary>
	public sealed class ResourceElement {
		/// <summary>
		/// Name of resource
		/// </summary>
		public string Name { get; set; } = null!;

		/// <summary>
		/// Data of resource
		/// </summary>
		public IResourceData ResourceData { get; set; } = null!;

		/// <inheritdoc/>
		public override string ToString() => $"N: {Name}, V: {ResourceData}";
	}
}
