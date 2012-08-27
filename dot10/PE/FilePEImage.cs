using System;
using System.IO;
using dot10.IO;

namespace dot10.PE {
	/// <summary>
	/// Accesses a PE file that has the same structure as a PE file on disk.
	/// </summary>
	/// <remarks>You can't use it to access a PE file that has been loaded into
	/// memory by eg. the OS PE loader. Use <see cref="MemoryPEImage"/> instead.
	/// </remarks>
	/// <seealso cref="MemoryPEImage"/>
	public class FilePEImage : PEImageBase {
		/// <inheritdoc/>
		public FilePEImage(IStreamCreator streamCreator, bool verify)
			: base(streamCreator, verify) {
		}

		/// <inheritdoc/>
		public FilePEImage(string filename, bool verify)
			: base(filename, verify) {
		}

		/// <inheritdoc/>
		public FilePEImage(byte[] data, bool verify)
			: base(data, verify) {
		}

		/// <inheritdoc/>
		public FilePEImage(IntPtr baseAddr, long length, bool verify)
			: base(baseAddr, length, verify) {
		}

		/// <inheritdoc/>
		public FilePEImage(IntPtr baseAddr, bool verify)
			: base(baseAddr, verify) {
		}

		/// <inheritdoc/>
		public override RVA ToRVA(FileOffset offset) {
			return peInfo.ToRVA(offset);
		}

		/// <inheritdoc/>
		public override FileOffset ToFileOffset(RVA rva) {
			return peInfo.ToFileOffset(rva);
		}
	}
}
