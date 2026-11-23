// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using dnlib.IO;
using dnlib.TestUtils.Dumping;
using Xunit;

namespace dnlib.Tests.InvalidMetadata {
	/// <summary>
	/// dnlib must be able to read files with invalid data (eg. obfuscated assemblies). This class opens a file
	/// that a test made invalid, reads everything dnlib can return and writes the module. This is allowed:
	/// <list type="bullet">
	/// <item><see cref="BadImageFormatException"/> or an <see cref="IOException"/> (eg.
	/// <see cref="DataReaderException"/>) when the file is opened</item>
	/// <item>no exception while the module is read</item>
	/// <item><see cref="ModuleWriterException"/> when it's written (the writer reports what it can't write)</item>
	/// </list>
	/// The invalid data also must not make dnlib use much more memory than it needs for the valid file, eg. by
	/// using a count from the file as the size of an array before checking it (only measured on .NET).
	/// </summary>
	static class InvalidFileChecker {
		/// <summary>
		/// Reads and writes an invalid file. Fails the test if dnlib does something that isn't allowed.
		/// </summary>
		/// <param name="validImage">The valid file that <paramref name="image"/> was made from</param>
		/// <param name="image">The invalid file</param>
		/// <param name="description">What's invalid, for the message if the test fails</param>
		public static void ReadAndWrite(byte[] validImage, byte[] image, string description) {
			long limit = GetAllocationLimit(validImage);
			long allocated = MeasureAllocatedBytes(() => ReadAndWrite(image, description));
			Assert.True(allocated <= limit, $"{allocated:N0} bytes were allocated, the limit is {limit:N0} ({description})");
		}

		// How many bytes may be allocated to read and write an invalid file: several times what the valid file
		// needs (invalid data can make dnlib read other, bigger things) plus a fixed amount, since the valid
		// files are small. An array whose size comes from a 32-bit value in the file is far above this.
		static long GetAllocationLimit(byte[] validImage) {
			lock (allocationLimits) {
				if (!allocationLimits.TryGetValue(validImage, out long limit)) {
					limit = MeasureAllocatedBytes(() => ReadAndWrite(validImage, "the valid file")) * 10 + 32 * 1024 * 1024;
					allocationLimits.Add(validImage, limit);
				}
				return limit;
			}
		}
		static readonly Dictionary<byte[], long> allocationLimits = new Dictionary<byte[], long>();

		static long MeasureAllocatedBytes(Action action) {
#if NET
			long before = GC.GetAllocatedBytesForCurrentThread();
			action();
			return GC.GetAllocatedBytesForCurrentThread() - before;
#else
			action();
			return 0;
#endif
		}

		static void ReadAndWrite(byte[] image, string description) {
			ModuleDefMD module;
			try {
				module = ModuleDefMD.Load(image);
			}
			catch (BadImageFormatException) {
				return;
			}
			catch (IOException) {
				return;
			}
			catch (Exception ex) {
				throw new InvalidOperationException($"Opening the file threw {ex.GetType().Name} ({description})", ex);
			}

			using (module) {
				try {
					ModuleDumper.Dump(module, new ModuleDumpOptions { MaxStack = true });
				}
				catch (Exception ex) {
					throw new InvalidOperationException($"Reading the module threw {ex.GetType().Name} ({description})", ex);
				}

				try {
					module.Write(new MemoryStream(), new ModuleWriterOptions(module));
				}
				catch (ModuleWriterException) {
				}
				catch (Exception ex) {
					throw new InvalidOperationException($"Writing the module threw {ex.GetType().Name} ({description})", ex);
				}
			}
		}
	}
}
