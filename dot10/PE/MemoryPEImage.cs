using System.Collections.Generic;
using System.IO;

namespace dot10.PE {
	/// <summary>
	/// Accesses a PE file that has the same structure as a PE file loaded
	/// into memory by the OS PE file loader.
	/// </summary>
	/// <seealso cref="FilePEImage"/>
	public class MemoryPEImage : PEImageBase {
		/// <summary>
		/// Constructor for a PE image in a Stream
		/// </summary>
		/// <param name="data">The PE file data</param>
		/// <param name="verify">Verify PE file data</param>
		public MemoryPEImage(Stream data, bool verify)
			: base(data, verify) {
		}

		/// <inheritdoc/>
		public override RVA ToRVA(FileOffset offset) {
			return new RVA((uint)offset.Value);
		}

		/// <inheritdoc/>
		public override FileOffset ToFileOffset(RVA rva) {
			return new FileOffset(rva.Value);
		}
	}
}
