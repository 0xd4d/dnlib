/*
    Copyright (C) 2012-2014 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System.Collections.Generic;
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
		internal ResourceCollection() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="listener">List listener</param>
		internal ResourceCollection(IListListener<Resource> listener)
			: base(listener) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="length">Initial length of the list</param>
		/// <param name="context">Context passed to <paramref name="readOriginalValue"/></param>
		/// <param name="readOriginalValue">Delegate instance that returns original values</param>
		internal ResourceCollection(int length, object context, MFunc<object, uint, Resource> readOriginalValue)
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
