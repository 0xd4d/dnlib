using System.Collections.Generic;
using System.IO;

namespace dot10.PE {
	/// <summary>
	/// Accesses a PE file
	/// </summary>
	public abstract class PEImageBase : IPEImage {
		readonly BinaryReader reader;

		/// <summary>
		/// Access to the PE headers
		/// </summary>
		protected readonly PEInfo peInfo;

		/// <inheritdoc/>
		public ImageDosHeader ImageDosHeader {
			get { return peInfo.ImageDosHeader; }
		}

		/// <inheritdoc/>
		public ImageNTHeaders ImageNTHeaders {
			get { return peInfo.ImageNTHeaders; }
		}

		/// <inheritdoc/>
		public IList<ImageSectionHeader> ImageSectionHeaders {
			get { return peInfo.ImageSectionHeaders; }
		}

		/// <summary>
		/// Constructor for a PE image in a Stream
		/// </summary>
		/// <param name="data">The PE file data</param>
		/// <param name="verify">Verify PE file data</param>
		protected PEImageBase(Stream data, bool verify) {
			this.reader = new BinaryReader(data);
			this.peInfo = new PEInfo(reader, verify);
		}

		/// <inheritdoc/>
		public abstract RVA ToRVA(FileOffset offset);

		/// <inheritdoc/>
		public abstract FileOffset ToFileOffset(RVA rva);
	}
}
