// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using dnlib.IO;
using dnlib.Utils;
using dnlib.W32Resources;
using dnlib.Threading;

namespace dnlib.PE {
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
	public sealed class PEImage : IInternalPEImage {
		// Default to false because an OS loaded PE image may contain memory holes. If there
		// are memory holes, other code (eg. .NET resource creator) must verify that all memory
		// is available, which will be slower.
		const bool USE_MEMORY_LAYOUT_WITH_MAPPED_FILES = false;

		static readonly IPEType MemoryLayout = new MemoryPEType();
		static readonly IPEType FileLayout = new FilePEType();

		DataReaderFactory dataReaderFactory;
		IPEType peType;
		PEInfo peInfo;
		UserValue<Win32Resources> win32Resources;
#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		sealed class FilePEType : IPEType {
			public RVA ToRVA(PEInfo peInfo, FileOffset offset) => peInfo.ToRVA(offset);
			public FileOffset ToFileOffset(PEInfo peInfo, RVA rva) => peInfo.ToFileOffset(rva);
		}

		sealed class MemoryPEType : IPEType {
			public RVA ToRVA(PEInfo peInfo, FileOffset offset) => (RVA)offset;
			public FileOffset ToFileOffset(PEInfo peInfo, RVA rva) => (FileOffset)rva;
		}

		/// <inheritdoc/>
		public bool IsFileImageLayout => peType is FilePEType;

		/// <inheritdoc/>
		public bool MayHaveInvalidAddresses => !IsFileImageLayout;

		/// <inheritdoc/>
		public string Filename => dataReaderFactory.Filename;

		/// <inheritdoc/>
		public ImageDosHeader ImageDosHeader => peInfo.ImageDosHeader;

		/// <inheritdoc/>
		public ImageNTHeaders ImageNTHeaders => peInfo.ImageNTHeaders;

		/// <inheritdoc/>
		public IList<ImageSectionHeader> ImageSectionHeaders => peInfo.ImageSectionHeaders;

		/// <inheritdoc/>
		public IList<ImageDebugDirectory> ImageDebugDirectories {
			get {
				if (imageDebugDirectories is null)
					imageDebugDirectories = ReadImageDebugDirectories();
				return imageDebugDirectories;
			}
		}
		ImageDebugDirectory[] imageDebugDirectories;

		/// <inheritdoc/>
		public DataReaderFactory DataReaderFactory => dataReaderFactory;

		/// <inheritdoc/>
		public Win32Resources Win32Resources {
			get => win32Resources.Value;
			set {
				IDisposable origValue = null;
				if (win32Resources.IsValueInitialized) {
					origValue = win32Resources.Value;
					if (origValue == value)
						return;
				}
				win32Resources.Value = value;

				if (!(origValue is null))
					origValue.Dispose();
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="dataReaderFactory">Data reader factory</param>
		/// <param name="imageLayout">Image layout</param>
		/// <param name="verify">Verify PE file data</param>
		public PEImage(DataReaderFactory dataReaderFactory, ImageLayout imageLayout, bool verify) {
			try {
				this.dataReaderFactory = dataReaderFactory;
				peType = ConvertImageLayout(imageLayout);
				var reader = dataReaderFactory.CreateReader();
				peInfo = new PEInfo(ref reader, verify);
				Initialize();
			}
			catch {
				Dispose();
				throw;
			}
		}

		void Initialize() {
			win32Resources.ReadOriginalValue = () => {
				var dataDir = peInfo.ImageNTHeaders.OptionalHeader.DataDirectories[2];
				if (dataDir.VirtualAddress == 0 || dataDir.Size == 0)
					return null;
				return new Win32ResourcesPE(this);
			};
#if THREAD_SAFE
			win32Resources.Lock = theLock;
#endif
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
		/// <param name="filename">Name of the file</param>
		/// <param name="mapAsImage"><c>true</c> if we should map it as an executable</param>
		/// <param name="verify">Verify PE file data</param>
		internal PEImage(string filename, bool mapAsImage, bool verify)
			: this(DataReaderFactoryFactory.Create(filename, mapAsImage), mapAsImage ? ImageLayout.Memory : ImageLayout.File, verify) {
			try {
				if (mapAsImage && dataReaderFactory is MemoryMappedDataReaderFactory)
					((MemoryMappedDataReaderFactory)dataReaderFactory).SetLength(peInfo.GetImageSize());
			}
			catch {
				Dispose();
				throw;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="filename">Name of the file</param>
		/// <param name="verify">Verify PE file data</param>
		public PEImage(string filename, bool verify)
			: this(filename, USE_MEMORY_LAYOUT_WITH_MAPPED_FILES, verify) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="filename">Name of the file</param>
		public PEImage(string filename)
			: this(filename, true) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">The PE file data</param>
		/// <param name="filename">Filename or null</param>
		/// <param name="imageLayout">Image layout</param>
		/// <param name="verify">Verify PE file data</param>
		public PEImage(byte[] data, string filename, ImageLayout imageLayout, bool verify)
			: this(ByteArrayDataReaderFactory.Create(data, filename), imageLayout, verify) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">The PE file data</param>
		/// <param name="imageLayout">Image layout</param>
		/// <param name="verify">Verify PE file data</param>
		public PEImage(byte[] data, ImageLayout imageLayout, bool verify)
			: this(data, null, imageLayout, verify) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">The PE file data</param>
		/// <param name="verify">Verify PE file data</param>
		public PEImage(byte[] data, bool verify)
			: this(data, null, ImageLayout.File, verify) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">The PE file data</param>
		/// <param name="filename">Filename or null</param>
		/// <param name="verify">Verify PE file data</param>
		public PEImage(byte[] data, string filename, bool verify)
			: this(data, filename, ImageLayout.File, verify) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">The PE file data</param>
		public PEImage(byte[] data)
			: this(data, null, true) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="data">The PE file data</param>
		/// <param name="filename">Filename or null</param>
		public PEImage(byte[] data, string filename)
			: this(data, filename, true) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="baseAddr">Address of PE image</param>
		/// <param name="length">Length of PE image</param>
		/// <param name="imageLayout">Image layout</param>
		/// <param name="verify">Verify PE file data</param>
		public unsafe PEImage(IntPtr baseAddr, uint length, ImageLayout imageLayout, bool verify)
			: this(NativeMemoryDataReaderFactory.Create((byte*)baseAddr, length, filename: null), imageLayout, verify) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="baseAddr">Address of PE image</param>
		/// <param name="length">Length of PE image</param>
		/// <param name="verify">Verify PE file data</param>
		public PEImage(IntPtr baseAddr, uint length, bool verify)
			: this(baseAddr, length, ImageLayout.Memory, verify) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="baseAddr">Address of PE image</param>
		/// <param name="length">Length of PE image</param>
		public PEImage(IntPtr baseAddr, uint length)
			: this(baseAddr, length, true) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="baseAddr">Address of PE image</param>
		/// <param name="imageLayout">Image layout</param>
		/// <param name="verify">Verify PE file data</param>
		public unsafe PEImage(IntPtr baseAddr, ImageLayout imageLayout, bool verify)
			: this(NativeMemoryDataReaderFactory.Create((byte*)baseAddr, 0x10000, filename: null), imageLayout, verify) {
			try {
				((NativeMemoryDataReaderFactory)dataReaderFactory).SetLength(peInfo.GetImageSize());
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

		/// <inheritdoc/>
		public RVA ToRVA(FileOffset offset) => peType.ToRVA(peInfo, offset);

		/// <inheritdoc/>
		public FileOffset ToFileOffset(RVA rva) => peType.ToFileOffset(peInfo, rva);

		/// <inheritdoc/>
		public void Dispose() {
			IDisposable id;
			if (win32Resources.IsValueInitialized && !((id = win32Resources.Value) is null))
				id.Dispose();
			dataReaderFactory?.Dispose();
			win32Resources.Value = null;
			dataReaderFactory = null;
			peType = null;
			peInfo = null;
		}

		/// <inheritdoc/>
		public DataReader CreateReader(FileOffset offset) =>
			DataReaderFactory.CreateReader((uint)offset, DataReaderFactory.Length - (uint)offset);

		/// <inheritdoc/>
		public DataReader CreateReader(FileOffset offset, uint length) =>
			DataReaderFactory.CreateReader((uint)offset, length);

		/// <inheritdoc/>
		public DataReader CreateReader(RVA rva) => CreateReader(ToFileOffset(rva));

		/// <inheritdoc/>
		public DataReader CreateReader(RVA rva, uint length) => CreateReader(ToFileOffset(rva), length);

		/// <inheritdoc/>
		public DataReader CreateReader() => DataReaderFactory.CreateReader();

		void IInternalPEImage.UnsafeDisableMemoryMappedIO() {
			if (dataReaderFactory is MemoryMappedDataReaderFactory creator)
				creator.UnsafeDisableMemoryMappedIO();
		}

		bool IInternalPEImage.IsMemoryMappedIO {
			get {
				var creator = dataReaderFactory as MemoryMappedDataReaderFactory;
				return creator is null ? false : creator.IsMemoryMappedIO;
			}
		}

		ImageDebugDirectory[] ReadImageDebugDirectories() {
			try {
				var dataDir = ImageNTHeaders.OptionalHeader.DataDirectories[6];
				if (dataDir.VirtualAddress == 0)
					return Array2.Empty<ImageDebugDirectory>();
				var reader = DataReaderFactory.CreateReader();
				if (dataDir.Size > reader.Length)
					return Array2.Empty<ImageDebugDirectory>();
				int count = (int)(dataDir.Size / 0x1C);
				if (count == 0)
					return Array2.Empty<ImageDebugDirectory>();
				reader.CurrentOffset = (uint)ToFileOffset(dataDir.VirtualAddress);
				if ((ulong)reader.CurrentOffset + dataDir.Size > reader.Length)
					return Array2.Empty<ImageDebugDirectory>();
				var res = new ImageDebugDirectory[count];
				for (int i = 0; i < res.Length; i++)
					res[i] = new ImageDebugDirectory(ref reader, true);
				return res;
			}
			catch (IOException) {
			}
			return Array2.Empty<ImageDebugDirectory>();
		}
	}
}
