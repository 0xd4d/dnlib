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
		/// <summary>
		/// Constructor for a PE image in a Stream
		/// </summary>
		/// <param name="data">The PE file data</param>
		/// <param name="verify">Verify PE file data</param>
		public FilePEImage(Stream data, bool verify)
			: base(data, verify) {
		}

		/// <summary>
		/// Constructor for a PE image in a byte[]
		/// </summary>
		/// <param name="data">The PE file data</param>
		/// <param name="verify">Verify PE file data</param>
		public FilePEImage(byte[] data, bool verify)
			: this(new MemoryStream(data), verify) {
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
