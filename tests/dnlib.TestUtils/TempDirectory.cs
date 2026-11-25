// dnlib: See LICENSE.txt for more info

using System;
using System.IO;

namespace dnlib.TestUtils {
	/// <summary>
	/// A new empty directory that's deleted when this instance is disposed.
	/// It's created next to the test assembly and not in the system's temp directory, which can be a RAM disk.
	/// </summary>
	public sealed class TempDirectory : IDisposable {
		/// <summary>
		/// Full path of the directory
		/// </summary>
		public string Path { get; }

		public TempDirectory() {
			var root = System.IO.Path.Combine(AppContext.BaseDirectory, "tmp");
			Path = System.IO.Path.Combine(root, Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(Path);
		}

		/// <summary>
		/// Returns the full path of a file in this directory
		/// </summary>
		public string GetPath(string fileName) => System.IO.Path.Combine(Path, fileName);

		public void Dispose() {
			try {
				Directory.Delete(Path, recursive: true);
			}
			catch (IOException) {
				// eg. a file is still memory mapped on Windows. Leaving it behind doesn't fail a test.
			}
			catch (UnauthorizedAccessException) {
			}
		}
	}
}
