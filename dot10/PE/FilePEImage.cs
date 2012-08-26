using System.IO;

namespace dot10.PE {
	/// <summary>
	/// Accesses a PE file that has the same structure as a PE file on disk.
	/// </summary>
	/// <remarks>You can't use it to access a PE file that has been loaded into
	/// memory by eg. the OS PE loader. Use <see cref="MemoryPEImage"/> instead.
	/// </remarks>
	/// <seealso cref="MemoryPEImage"/>
	class FilePEImage : IPEImage, IPEInfoSeeker {
		readonly BinaryReader reader;
		readonly PEInfo peInfo;

		/// <summary>
		/// Constructor for a PE image in a Stream
		/// </summary>
		/// <param name="data">The PE file data</param>
		/// <param name="verify">Verify PE file data</param>
		public FilePEImage(Stream data, bool verify) {
			this.reader = new BinaryReader(data);
			this.peInfo = new PEInfo(this, reader, verify);
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
		public RVA ToRVA(FileOffset offset) {
			return peInfo.ToRVA(offset);
		}

		/// <inheritdoc/>
		public FileOffset ToFileOffset(RVA rva) {
			return peInfo.ToFileOffset(rva);
		}

		void IPEInfoSeeker.seek(BinaryReader reader, FileOffset offset) {
			reader.BaseStream.Position = offset.Value;
		}
	}
}
