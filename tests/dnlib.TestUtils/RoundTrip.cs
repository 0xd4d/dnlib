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
