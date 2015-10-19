// dnlib: See LICENSE.txt for more info

using dnlib.Utils;
using dnlib.Threading;

namespace dnlib.DotNet {
	/// <summary>
	/// A collection of <see cref="Resource"/>s
	/// </summary>
	public class ResourceCollection : LazyList<Resource> {
		/// <summary>
		/// Default constructor
		/// </summary>
		public ResourceCollection() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="listener">List listener</param>
		public ResourceCollection(IListListener<Resource> listener)
			: base(listener) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="length">Initial length of the list</param>
		/// <param name="context">Context passed to <paramref name="readOriginalValue"/></param>
		/// <param name="readOriginalValue">Delegate instance that returns original values</param>
		public ResourceCollection(int length, object context, MFunc<object, uint, Resource> readOriginalValue)
			: base(length, context, readOriginalValue) {
		}

		/// <summary>
		/// Finds the index of a resource
		/// </summary>
		/// <param name="name">Name of resource</param>
		/// <returns>The index of the <see cref="Resource"/> or <c>-1</c> if none was found</returns>
		public int IndexOf(UTF8String name) {
			int i = -1;
			foreach (var resource in this.GetSafeEnumerable()) {
				i++;
				if (resource != null && resource.Name == name)
					return i;
			}
			return -1;
		}

		/// <summary>
		/// Finds the index of an embedded resource
		/// </summary>
		/// <param name="name">Name of resource</param>
		/// <returns>The index of the <see cref="EmbeddedResource"/> or <c>-1</c> if none was found</returns>
		public int IndexOfEmbeddedResource(UTF8String name) {
			int i = -1;
			foreach (var resource in this.GetSafeEnumerable()) {
				i++;
				if (resource != null &&
					resource.ResourceType == ResourceType.Embedded &&
					resource.Name == name)
					return i;
			}
			return -1;
		}

		/// <summary>
		/// Finds the index of an assembly linked resource
		/// </summary>
		/// <param name="name">Name of resource</param>
		/// <returns>The index of the <see cref="AssemblyLinkedResource"/> or <c>-1</c> if none was found</returns>
		public int IndexOfAssemblyLinkedResource(UTF8String name) {
			int i = -1;
			foreach (var resource in this.GetSafeEnumerable()) {
				i++;
				if (resource != null &&
					resource.ResourceType == ResourceType.AssemblyLinked &&
					resource.Name == name)
					return i;
			}
			return -1;
		}

		/// <summary>
		/// Finds the index of a linked resource
		/// </summary>
		/// <param name="name">Name of resource</param>
		/// <returns>The index of the <see cref="LinkedResource"/> or <c>-1</c> if none was found</returns>
		public int IndexOfLinkedResource(UTF8String name) {
			int i = -1;
			foreach (var resource in this.GetSafeEnumerable()) {
				i++;
				if (resource != null &&
					resource.ResourceType == ResourceType.Linked &&
					resource.Name == name)
					return i;
			}
			return -1;
		}

		/// <summary>
		/// Finds a resource
		/// </summary>
		/// <param name="name">Name of resource</param>
		/// <returns>The <see cref="Resource"/> or <c>null</c> if none was found</returns>
		public Resource Find(UTF8String name) {
			foreach (var resource in this.GetSafeEnumerable()) {
				if (resource != null && resource.Name == name)
					return resource;
			}
			return null;
		}

		/// <summary>
		/// Finds an embedded resource
		/// </summary>
		/// <param name="name">Name of resource</param>
		/// <returns>The <see cref="EmbeddedResource"/> or <c>null</c> if none was found</returns>
		public EmbeddedResource FindEmbeddedResource(UTF8String name) {
			foreach (var resource in this.GetSafeEnumerable()) {
				if (resource != null &&
					resource.ResourceType == ResourceType.Embedded &&
					resource.Name == name)
					return (EmbeddedResource)resource;
			}
			return null;
		}

		/// <summary>
		/// Finds an assembly linked resource
		/// </summary>
		/// <param name="name">Name of resource</param>
		/// <returns>The <see cref="AssemblyLinkedResource"/> or <c>null</c> if none was found</returns>
		public AssemblyLinkedResource FindAssemblyLinkedResource(UTF8String name) {
			foreach (var resource in this.GetSafeEnumerable()) {
				if (resource != null &&
					resource.ResourceType == ResourceType.AssemblyLinked &&
					resource.Name == name)
					return (AssemblyLinkedResource)resource;
			}
			return null;
		}

		/// <summary>
		/// Finds a linked resource
		/// </summary>
		/// <param name="name">Name of resource</param>
		/// <returns>The <see cref="LinkedResource"/> or <c>null</c> if none was found</returns>
		public LinkedResource FindLinkedResource(UTF8String name) {
			foreach (var resource in this.GetSafeEnumerable()) {
				if (resource != null &&
					resource.ResourceType == ResourceType.Linked &&
					resource.Name == name)
					return (LinkedResource)resource;
			}
			return null;
		}
	}
}
