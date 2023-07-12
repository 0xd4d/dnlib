// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;

namespace dnlib.DotNet.Resources {
	/// <summary>
	/// Resource element set
	/// </summary>
	public sealed class ResourceElementSet {
		internal const string DeserializingResourceReaderTypeNameRegex = @"^System\.Resources\.Extensions\.DeserializingResourceReader,\s*System\.Resources\.Extensions";
		internal const string ResourceReaderTypeNameRegex = @"^System\.Resources\.ResourceReader,\s*mscorlib";

		readonly Dictionary<string, ResourceElement> dict = new Dictionary<string, ResourceElement>(StringComparer.Ordinal);

		/// <summary>
		/// The ResourceReader type name used by this set
		/// </summary>
		public string ResourceReaderTypeName { get; }

		/// <summary>
		/// The ResourceSet type name used by this set
		/// </summary>
		public string ResourceSetTypeName { get; }

		/// <summary>
		/// The type of resource reader used to read this set
		/// </summary>
		public ResourceReaderType ReaderType { get; }

		/// <summary>
		/// Format version of the resource set
		/// </summary>
		public int FormatVersion { get; internal set; }

		/// <summary>
		///	Creates a new <see cref="ResourceElementSet"/> instance
		/// </summary>
		/// <param name="resourceReaderTypeName">The ResourceReader type name to use</param>
		/// <param name="resourceSetTypeName">The ResourceSet type name to use</param>
		/// <param name="readerType"></param>
		internal ResourceElementSet(string resourceReaderTypeName, string resourceSetTypeName, ResourceReaderType readerType) {
			ResourceReaderTypeName = resourceReaderTypeName;
			ResourceSetTypeName = resourceSetTypeName;
			ReaderType = readerType;
			FormatVersion = 2;
		}

		/// <summary>
		/// Gets the number of elements in the set
		/// </summary>
		public int Count => dict.Count;

		/// <summary>
		/// Gets all resource elements
		/// </summary>
		public IEnumerable<ResourceElement> ResourceElements => dict.Values;

		/// <summary>
		/// Adds a new resource to the set, overwriting any existing resource
		/// </summary>
		/// <param name="elem"></param>
		public void Add(ResourceElement elem) => dict[elem.Name] = elem;

		/// <summary>
		/// Creates a new <see cref="ResourceElementSet"/> instance for a DeserializingResourceReader
		/// </summary>
		/// <param name="extensionAssemblyVersion"></param>
		public static ResourceElementSet CreateForDeserializingResourceReader(Version extensionAssemblyVersion) {
			var extensionAssemblyFullName = $"System.Resources.Extensions, Version={extensionAssemblyVersion.ToString(4)}, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51";
			return new ResourceElementSet($"System.Resources.Extensions.DeserializingResourceReader, {extensionAssemblyFullName}", $"System.Resources.Extensions.RuntimeResourceSet, {extensionAssemblyFullName}", ResourceReaderType.DeserializingResourceReader);
		}

		/// <summary>
		/// Creates a new <see cref="ResourceElementSet"/> instance for a ResourceReader
		/// </summary>
		/// <param name="module">Module in which the set will reside</param>
		public static ResourceElementSet CreateForResourceReader(ModuleDef module) {
			string mscorlibFullName;
			if (module.CorLibTypes.AssemblyRef.Name == "mscorlib") {
				// Use mscorlib reference found in module.
				mscorlibFullName = module.CorLibTypes.AssemblyRef.FullName;
			}
			else {
				// Use a reference to 4.0.0.0 mscorlib for compatibility with .NET Core, .NET 5, and later.
				mscorlibFullName = "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
			}
			return new ResourceElementSet($"System.Resources.ResourceReader, {mscorlibFullName}", "System.Resources.RuntimeResourceSet", ResourceReaderType.ResourceReader);
		}

		/// <summary>
		/// Creates a new <see cref="ResourceElementSet"/> instance for a ResourceReader
		/// </summary>
		/// <param name="mscorlibVersion">mscorlib assembly version</param>
		public static ResourceElementSet CreateForResourceReader(Version mscorlibVersion) =>
			new ResourceElementSet($"System.Resources.ResourceReader, mscorlib, Version={mscorlibVersion.ToString(4)}, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Resources.RuntimeResourceSet", ResourceReaderType.ResourceReader);
	}
}
