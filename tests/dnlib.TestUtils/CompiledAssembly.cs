// dnlib: See LICENSE.txt for more info

namespace dnlib.TestUtils {
	/// <summary>
	/// An assembly that was created for a test
	/// </summary>
	public sealed class CompiledAssembly {
		/// <summary>
		/// The PE file
		/// </summary>
		public byte[] Image { get; }

		/// <summary>
		/// The PDB file or <c>null</c> if none was created or if it's embedded in <see cref="Image"/>
		/// </summary>
		public byte[]? Pdb { get; }

		public CompiledAssembly(byte[] image, byte[]? pdb) {
			Image = image;
			Pdb = pdb;
		}
	}
}
