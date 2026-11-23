// dnlib: See LICENSE.txt for more info

using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using dnlib.TestUtils.Dumping;

namespace dnlib.TestUtils {
	/// <summary>
	/// Reads a module, writes it and reads what was written. The module that was read last must be the same
	/// as the first one (see <see cref="ModuleComparer"/>): nothing that dnlib reads may get lost or changed
	/// when it's written.
	/// </summary>
	/// <remarks>
	/// This can't find a bug that the reader and the writer share, eg. if both think a flag has another value.
	/// </remarks>
	public static class RoundTrip {
		/// <summary>
		/// Writes the module with the default options. The writer creates new metadata tables and heaps, so all
		/// tokens and offsets can change.
		/// </summary>
		/// <param name="image">A PE file with only IL code</param>
		/// <returns>The written file</returns>
		public static byte[] CheckWithNewMetadata(byte[] image) {
			using (var original = ModuleDefMD.Load(image)) {
				var writtenImage = Write(original, new ModuleWriterOptions(original));
				using (var written = ModuleDefMD.Load(writtenImage))
					ModuleComparer.AssertEqual(original, written);
				return writtenImage;
			}
		}

		/// <summary>
		/// Writes the module with <see cref="MetadataFlags.PreserveAll"/> and <see cref="MetadataFlags.KeepOldMaxStack"/>.
		/// Tokens, heap offsets and max stack values must stay the same.
		/// </summary>
		/// <param name="image">A PE file with only IL code</param>
		/// <returns>The written file</returns>
		public static byte[] CheckWithPreservedMetadata(byte[] image) {
			using (var original = ModuleDefMD.Load(image)) {
				var writerOptions = new ModuleWriterOptions(original);
				writerOptions.MetadataOptions.Flags |= MetadataFlags.PreserveAll | MetadataFlags.KeepOldMaxStack;
				var writtenImage = Write(original, writerOptions);
				using (var written = ModuleDefMD.Load(writtenImage)) {
					ModuleComparer.AssertEqual(original, written, new ModuleDumpOptions { MaxStack = true });
					AssertSameTokens(original, written);
				}
				return writtenImage;
			}
		}

		/// <summary>
		/// Reads a module and its portable PDB, writes both and reads them again. The debug info (documents,
		/// sequence points, scopes, locals, constants, imports, custom debug info) must be the same too.
		/// </summary>
		/// <param name="image">A PE file with only IL code</param>
		/// <param name="pdb">Its portable PDB</param>
		/// <param name="preserveMetadata"><c>true</c> to write with <see cref="MetadataFlags.PreserveAll"/> and
		/// <see cref="MetadataFlags.KeepOldMaxStack"/>, <c>false</c> to let the writer create new metadata</param>
		/// <returns>The written files</returns>
		public static CompiledAssembly CheckWithPortablePdb(byte[] image, byte[] pdb, bool preserveMetadata) {
			using (var original = LoadWithPdb(image, pdb)) {
				var writerOptions = new ModuleWriterOptions(original);
				if (preserveMetadata)
					writerOptions.MetadataOptions.Flags |= MetadataFlags.PreserveAll | MetadataFlags.KeepOldMaxStack;
				var pdbStream = new MemoryStream();
				writerOptions.WritePdb = true;
				writerOptions.PdbStream = pdbStream;
				// The name that's stored in the PE file's debug directory. Nothing is written to this file.
				writerOptions.PdbFileName = "written.pdb";

				var writtenImage = Write(original, writerOptions);
				var writtenPdb = pdbStream.ToArray();
				using (var written = LoadWithPdb(writtenImage, writtenPdb))
					ModuleComparer.AssertEqual(original, written, new ModuleDumpOptions { Pdb = true, MaxStack = preserveMetadata });
				return new CompiledAssembly(writtenImage, writtenPdb);
			}
		}

		/// <summary>
		/// Reads a module and its PDB. Throws if dnlib doesn't accept the PDB.
		/// </summary>
		public static ModuleDefMD LoadWithPdb(byte[] image, byte[] pdb) {
			var options = new ModuleCreationOptions {
				PdbFileOrData = pdb,
				// Only use the PDB that was passed in
				TryToLoadPdbFromDisk = false,
			};
			var module = ModuleDefMD.Load(image, options);
			if (module.PdbState is null) {
				module.Dispose();
				throw new ModuleDifferenceException("The PDB wasn't loaded");
			}
			return module;
		}

		static byte[] Write(ModuleDef module, ModuleWriterOptions options) {
			var stream = new MemoryStream();
			module.Write(stream, options);
			return stream.ToArray();
		}

		// ModuleComparer already checked that the two modules have the same types and members in the same order
		static void AssertSameTokens(ModuleDef expected, ModuleDef actual) {
			using (var expectedTypes = expected.GetTypes().GetEnumerator())
			using (var actualTypes = actual.GetTypes().GetEnumerator()) {
				while (expectedTypes.MoveNext() && actualTypes.MoveNext()) {
					var expectedType = expectedTypes.Current;
					var actualType = actualTypes.Current;
					AssertSameToken(expectedType, actualType);
					for (int i = 0; i < expectedType.Fields.Count; i++)
						AssertSameToken(expectedType.Fields[i], actualType.Fields[i]);
					for (int i = 0; i < expectedType.Methods.Count; i++)
						AssertSameToken(expectedType.Methods[i], actualType.Methods[i]);
					for (int i = 0; i < expectedType.Properties.Count; i++)
						AssertSameToken(expectedType.Properties[i], actualType.Properties[i]);
					for (int i = 0; i < expectedType.Events.Count; i++)
						AssertSameToken(expectedType.Events[i], actualType.Events[i]);
				}
			}
		}

		static void AssertSameToken(IMDTokenProvider expected, IMDTokenProvider actual) {
			if (expected.MDToken != actual.MDToken)
				throw new ModuleDifferenceException($"The token of {expected} changed from {expected.MDToken} to {actual.MDToken}");
		}
	}
}
