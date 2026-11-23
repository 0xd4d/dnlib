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
			CheckAllocatedBytes(GetAllocationLimit(validImage, null), image, null, description);
		}

		/// <summary>
		/// Reads a valid module with an invalid PDB file, and writes both. An invalid PDB must not stop the
		/// module from loading: no exception is allowed when the files are opened.
		/// </summary>
		/// <param name="image">The valid module</param>
		/// <param name="validPdb">The valid PDB file that <paramref name="pdb"/> was made from</param>
		/// <param name="pdb">The invalid PDB file</param>
		/// <param name="description">What's invalid, for the message if the test fails</param>
		public static void ReadAndWriteWithPdb(byte[] image, byte[] validPdb, byte[] pdb, string description) {
			CheckAllocatedBytes(GetAllocationLimit(image, validPdb), image, pdb, description);
		}

		static void CheckAllocatedBytes(long limit, byte[] image, byte[]? pdb, string description) {
			long dumpLength = 0;
			long allocated = MeasureAllocatedBytes(() => dumpLength = ReadAndWriteCore(image, pdb, description));
			// The dump is created by the test, not by dnlib, and invalid data can make it much longer (eg. a
			// changed offset that makes dnlib read thousands of small items where there was one). Creating
			// the text allocates about 25 times its length.
			limit += dumpLength * 40;
			Assert.True(allocated <= limit, $"{allocated:N0} bytes were allocated, the limit is {limit:N0} ({description})");
		}

		// How many bytes may be allocated to read and write an invalid file: several times what the valid file
		// needs (invalid data can make dnlib read other, bigger things) plus a fixed amount, since the valid
		// files are small. An array whose size comes from a 32-bit value in the file is far above this.
		static long GetAllocationLimit(byte[] validImage, byte[]? validPdb) {
			var key = validPdb ?? validImage;
			lock (allocationLimits) {
				if (!allocationLimits.TryGetValue(key, out long limit)) {
					limit = MeasureAllocatedBytes(() => ReadAndWriteCore(validImage, validPdb, "the valid file")) * 10 + 32 * 1024 * 1024;
					allocationLimits.Add(key, limit);
				}
				return limit;
			}
		}
		// The key is the valid file that the invalid files are made from
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

		// Returns the length of the module's dump
		static long ReadAndWriteCore(byte[] image, byte[]? pdb, string description) {
			ModuleDefMD module;
			try {
				if (pdb is null)
					module = ModuleDefMD.Load(image);
				else
					module = ModuleDefMD.Load(image, new ModuleCreationOptions { PdbFileOrData = pdb, TryToLoadPdbFromDisk = false });
			}
			// Only the PE file may be rejected, not the PDB
			catch (BadImageFormatException) when (pdb is null) {
				return 0;
			}
			catch (IOException) when (pdb is null) {
				return 0;
			}
			catch (Exception ex) {
				throw new InvalidOperationException($"Opening the file threw {ex.GetType().Name} ({description})", ex);
			}

			using (module) {
				long dumpLength;
				try {
					dumpLength = ModuleDumper.Dump(module, new ModuleDumpOptions { MaxStack = true, Pdb = true }).Length;
				}
				catch (Exception ex) {
					throw new InvalidOperationException($"Reading the module threw {ex.GetType().Name} ({description})", ex);
				}

				try {
					var options = new ModuleWriterOptions(module);
					if (module.PdbState is not null) {
						options.WritePdb = true;
						options.PdbStream = new MemoryStream();
						options.PdbFileName = "written.pdb";
					}
					module.Write(new MemoryStream(), options);
				}
				catch (ModuleWriterException) {
				}
				catch (Exception ex) {
					throw new InvalidOperationException($"Writing the module threw {ex.GetType().Name} ({description})", ex);
				}
				return dumpLength;
			}
		}
	}
}
