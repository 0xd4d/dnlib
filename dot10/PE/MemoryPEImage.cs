using System;
using System.IO;
using dot10.IO;

namespace dot10.PE {
	/// <summary>
	/// Accesses a PE file that has the same structure as a PE file loaded
	/// into memory by the OS PE file loader.
	/// </summary>
	/// <seealso cref="FilePEImage"/>
	public class MemoryPEImage : PEImageBase {
		/// <inheritdoc/>
		public MemoryPEImage(IStreamCreator streamCreator, bool verify)
			: base(streamCreator, verify) {
		}

		/// <inheritdoc/>
		public MemoryPEImage(string filename, bool verify)
			: base(filename, verify) {
		}

		/// <inheritdoc/>
		public MemoryPEImage(byte[] data, bool verify)
			: base(data, verify) {
		}

		/// <inheritdoc/>
		public MemoryPEImage(IntPtr baseAddr, long length, bool verify)
			: base(baseAddr, length, verify) {
		}

		/// <inheritdoc/>
		public MemoryPEImage(IntPtr baseAddr, bool verify)
			: base(baseAddr, verify) {
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
