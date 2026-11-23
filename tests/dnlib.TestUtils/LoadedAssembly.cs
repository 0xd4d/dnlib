// dnlib: See LICENSE.txt for more info

using System;
using System.Reflection;
#if NET
using System.IO;
using System.Runtime.Loader;
#endif

namespace dnlib.TestUtils {
	/// <summary>
	/// An assembly that was loaded from a byte array into the runtime that runs the tests, so a test can use
	/// reflection on it or execute its code
	/// </summary>
	public sealed class LoadedAssembly : IDisposable {
		public Assembly Assembly { get; }

#if NET
		readonly AssemblyLoadContext context;
#endif

		/// <summary>
		/// Loads an assembly
		/// </summary>
		/// <param name="image">An assembly that only references the runtime's assemblies</param>
		public LoadedAssembly(byte[] image) {
#if NET
			// Every assembly gets its own context: the runtime would otherwise return the assembly that's
			// already loaded when it's asked to load another one with the same name (eg. the same test assembly
			// before and after dnlib wrote it).
			context = new AssemblyLoadContext(name: null, isCollectible: true);
			Assembly = context.LoadFromStream(new MemoryStream(image));
#else
			// Assemblies loaded from a byte array are never shared, and .NET Framework can't unload them
			Assembly = Assembly.Load(image);
#endif
		}

		public void Dispose() {
#if NET
			context.Unload();
#endif
		}
	}
}
