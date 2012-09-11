using System;
using System.Collections.Generic;
using System.IO;
using dot10.IO;

namespace dot10.PE {
	/// <summary>
	/// Image layout
	/// </summary>
	public enum ImageLayout {
		/// <summary>
		/// Use this if the PE file has a normal structure (eg. it's been read from a file on disk)
		/// </summary>
		File,

		/// <summary>
		/// Use this if the PE file has been loaded into memory by the OS PE file loader
		/// </summary>
		Memory,
	}

	/// <summary>
	/// Accesses a PE file
	/// </summary>
	public sealed class PEImage : IPEImage {
		static readonly IPEType MemoryLayout = new MemoryPEType();
		static readonly IPEType FileLayout = new FilePEType();

		IImageStream imageStream;
		IImageStreamCreator imageStreamCreator;
		IPEType peType;
		PEInfo peInfo;

		class FilePEType : IPEType {
			/// <inheritdoc/>
			public RVA ToRVA(PEInfo peInfo, FileOffset offset) {
				return peInfo.ToRVA(offset);
			}

			/// <inheritdoc/>
			public FileOffset ToFileOffset(PEInfo peInfo, RVA rva) {
				return peInfo.ToFileOffset(rva);
			}
		}

		class MemoryPEType : IPEType {
			/// <inheritdoc/>
			public RVA ToRVA(PEInfo peInfo, FileOffset offset) {
				return new RVA((uint)offset.Value);
			}

			/// <inheritdoc/>
			public FileOffset ToFileOffset(PEInfo peInfo, RVA rva) {
				return new FileOffset(rva.Value);
			}
		}

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
		/// <param name="imageStreamCreator">The PE stream creator</param>
		/// <param name="imageLayout">Image layout</param>
		/// <param name="verify">Verify PE file data</param>
		public PEImage(IImageStreamCreator imageStreamCreator, ImageLayout imageLayout, bool verify) {
			try {
				this.imageStreamCreator = imageStreamCreator;
				this.peType = ConvertImageLayout(imageLayout);
				ResetReader();
				this.peInfo = new PEInfo(imageStream, verify);
			}
			catch {
				Dispose();
				throw;
			}
		}

		static IPEType ConvertImageLayout(ImageLayout imageLayout) {
			switch (imageLayout) {
			case ImageLayout.File: return FileLayout;
			case ImageLayout.Memory: return MemoryLayout;
			default: throw new ArgumentException("imageLayout");
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="fileName">Name of the file</param>
		/// <param name="mapAsImage">true if we should map it as an executable</param>
		/// <param name="verify">Verify PE file data</param>
		public PEImage(string fileName, bool mapAsImage, bool verify)
			: this(new MemoryMappedFileStreamCreator(fileName, mapAsImage), mapAsImage ? ImageLayout.Memory : ImageLayout.File, verify) {
			try {
				if (mapAsImage) {
					((MemoryMappedFileStreamCreator)imageStreamCreator).Length = peInfo.GetImageSize();
					ResetReader();
				}
			}
			catch {
				Dispose();
				throw;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="fileName">Name of the file</param>
		/// <param name="verify">Verify PE file data</param>
		public PEImage(string fileName, bool verify)
			: this(fileName, true, verify) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="fileName">Name of the file</param>
		public PEImage(string fileName)
			: this(fileName, true) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">The PE file data</param>
		/// <param name="imageLayout">Image layout</param>
		/// <param name="verify">Verify PE file data</param>
		public PEImage(byte[] data, ImageLayout imageLayout, bool verify)
			: this(new MemoryStreamCreator(data), imageLayout, verify) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">The PE file data</param>
		/// <param name="verify">Verify PE file data</param>
		public PEImage(byte[] data, bool verify)
			: this(data, ImageLayout.File, verify) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">The PE file data</param>
		public PEImage(byte[] data)
			: this(data, true) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="baseAddr">Address of PE image</param>
		/// <param name="length">Length of PE image</param>
		/// <param name="imageLayout">Image layout</param>
		/// <param name="verify">Verify PE file data</param>
		public PEImage(IntPtr baseAddr, long length, ImageLayout imageLayout, bool verify)
			: this(new UnmanagedMemoryStreamCreator(baseAddr, length), imageLayout, verify) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="baseAddr">Address of PE image</param>
		/// <param name="length">Length of PE image</param>
		/// <param name="verify">Verify PE file data</param>
		public PEImage(IntPtr baseAddr, long length, bool verify)
			: this(baseAddr, length, ImageLayout.Memory, verify) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="baseAddr">Address of PE image</param>
		/// <param name="length">Length of PE image</param>
		public PEImage(IntPtr baseAddr, long length)
			: this(baseAddr, length, true) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="baseAddr">Address of PE image</param>
		/// <param name="imageLayout">Image layout</param>
		/// <param name="verify">Verify PE file data</param>
		public PEImage(IntPtr baseAddr, ImageLayout imageLayout, bool verify)
			: this(new UnmanagedMemoryStreamCreator(baseAddr, 0x10000), imageLayout, verify) {
			try {
				((UnmanagedMemoryStreamCreator)imageStreamCreator).Length = peInfo.GetImageSize();
				ResetReader();
			}
			catch {
				Dispose();
				throw;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="baseAddr">Address of PE image</param>
		/// <param name="verify">Verify PE file data</param>
		public PEImage(IntPtr baseAddr, bool verify)
			: this(baseAddr, ImageLayout.Memory, verify) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="baseAddr">Address of PE image</param>
		public PEImage(IntPtr baseAddr)
			: this(baseAddr, true) {
		}

		void ResetReader() {
			if (imageStream != null) {
				imageStream.Dispose();
				imageStream = null;
			}
			imageStream = imageStreamCreator.CreateFull();
		}

		/// <inheritdoc/>
		public RVA ToRVA(FileOffset offset) {
			return peType.ToRVA(peInfo, offset);
		}

		/// <inheritdoc/>
		public FileOffset ToFileOffset(RVA rva) {
			return peType.ToFileOffset(peInfo, rva);
		}

		/// <inheritdoc/>
		public void Dispose() {
			if (imageStream != null)
				imageStream.Dispose();
			if (imageStreamCreator != null)
				imageStreamCreator.Dispose();
			imageStream = null;
			imageStreamCreator = null;
			peType = null;
			peInfo = null;
		}

		/// <inheritdoc/>
		public IImageStream CreateStream(FileOffset offset) {
			if (offset.Value > imageStreamCreator.Length)
				throw new ArgumentOutOfRangeException("offset");
			long length = imageStreamCreator.Length - offset.Value;
			return CreateStream(offset, length);
		}

		/// <inheritdoc/>
		public IImageStream CreateStream(FileOffset offset, long length) {
			return imageStreamCreator.Create(offset, length);
		}

		/// <inheritdoc/>
		public IImageStream CreateFullStream() {
			return imageStreamCreator.CreateFull();
		}
	}
}
