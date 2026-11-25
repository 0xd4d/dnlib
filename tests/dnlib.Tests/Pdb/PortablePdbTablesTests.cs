// dnlib: See LICENSE.txt for more info

using dnlib.TestUtils;
using dnlib.TestUtils.Dumping;
using Xunit;

namespace dnlib.Tests.Pdb {
	/// <summary>
	/// Checks the tables of the portable PDB that dnlib writes. They're read with System.Reflection.Metadata,
	/// see <see cref="PortablePdbTableDumper"/>.
	/// </summary>
	public class PortablePdbTablesTests {
		static CompiledAssembly Features => Fixtures.Fixtures.Features;

		static void AssertSameRowsAfterAWrite(CompiledAssembly compiled) {
			CompiledAssembly written;
			using (var module = RoundTrip.LoadWithPdb(compiled.Image, compiled.Pdb!))
				written = RoundTrip.WriteWithPortablePdb(module, preserveMetadata: true);

			Assert.Equal(
				PortablePdbTableDumper.Dump(compiled.Pdb!, skipEmptyLocalScopes: true),
				PortablePdbTableDumper.Dump(written.Pdb!, skipEmptyLocalScopes: true));
		}

		// If the module's tokens stay the same, dnlib must write the same rows, strings and blobs as the C#
		// compiler did: nothing in the PDB gets lost or is stored in another way. This also checks many fixes
		// where dnlib's writer didn't write what the compiler writes (eg. 83210ba9 local signature, 39f46bbb
		// and 0f11b68b constants, bb93b4b6 dynamic flags).
		[Fact]
		public void FixturesPdbHasTheSameRowsAfterAWrite() => AssertSameRowsAfterAWrite(Features);

		[Fact]
		public void SpecialLocalsAndConstantsHaveTheSameRowsAfterAWrite() => AssertSameRowsAfterAWrite(DebugInfoSource.Compiled);
	}
}
