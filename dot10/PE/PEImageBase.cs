using System;
using System.Collections.Generic;
using System.IO;
using dot10.IO;

namespace dot10.PE {
	/// <summary>
	/// Accesses a PE file
	/// </summary>
	public abstract class PEImageBase : IPEImage {
		BinaryReader reader;

		/// <summary>
		/// The stream creator
		/// </summary>
		protected IStreamCreator streamCreator;

		/// <summary>
		/// Access to the PE headers
		/// </summary>
		protected PEInfo peInfo;

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
		/// Constructor
		/// </summary>
		/// <param name="streamCreator">The PE stream creator</param>
		/// <param name="verify">Verify PE file data</param>
		protected PEImageBase(IStreamCreator streamCreator, bool verify) {
			this.streamCreator = streamCreator;
			ResetReader();
			this.peInfo = new PEInfo(reader, verify);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="filename">Name of the file</param>
		/// <param name="verify">Verify PE file data</param>
		public PEImageBase(string filename, bool verify)
			: this(new FileStreamCreator(filename), verify) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">The PE file data</param>
		/// <param name="verify">Verify PE file data</param>
		public PEImageBase(byte[] data, bool verify)
			: this(new MemoryStreamCreator(data), verify) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="baseAddr">Address of PE image</param>
		/// <param name="length">Length of PE image</param>
		/// <param name="verify">Verify PE file data</param>
		public PEImageBase(IntPtr baseAddr, long length, bool verify)
			: this(new UnmanagedMemoryStreamCreator(baseAddr, length), verify) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="baseAddr">Address of PE image</param>
		/// <param name="verify">Verify PE file data</param>
		public PEImageBase(IntPtr baseAddr, bool verify)
			: this(new UnmanagedMemoryStreamCreator(baseAddr, 0x10000), verify) {
			((UnmanagedMemoryStreamCreator)streamCreator).Length = getTotalMemorySize();
			ResetReader();
		}

		void ResetReader() {
			this.reader = new BinaryReader(streamCreator.CreateFull());
		}

		static ulong alignUp(ulong val, uint alignment) {
			return (val + alignment - 1) & ~(ulong)(alignment - 1);
		}

		long getTotalMemorySize() {
			var optHdr = ImageNTHeaders.OptionalHeader;
			uint alignment = optHdr.SectionAlignment;
			ulong len = alignUp(optHdr.SizeOfHeaders, alignment);
			foreach (var section in ImageSectionHeaders) {
				ulong len2 = alignUp((ulong)section.VirtualAddress.Value + Math.Max(section.VirtualSize, section.SizeOfRawData), alignment);
				if (len2 > len)
					len = len2;
			}
			return (long)len;
		}

		/// <inheritdoc/>
		public abstract RVA ToRVA(FileOffset offset);

		/// <inheritdoc/>
		public abstract FileOffset ToFileOffset(RVA rva);
	}
}
