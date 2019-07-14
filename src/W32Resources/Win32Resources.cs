// dnlib: See LICENSE.txt for more info

using System;
using System.Threading;
using dnlib.Utils;
using dnlib.IO;
using dnlib.PE;
using dnlib.Threading;

namespace dnlib.W32Resources {
	/// <summary>
	/// Win32 resources base class
	/// </summary>
	public abstract class Win32Resources : IDisposable {
		/// <summary>
		/// Gets/sets the root directory
		/// </summary>
		public abstract ResourceDirectory Root { get; set; }

		/// <summary>
		/// Finds a <see cref="ResourceDirectory"/>
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns>The <see cref="ResourceDirectory"/> or <c>null</c> if none found</returns>
		public ResourceDirectory Find(ResourceName type) {
			var dir = Root;
			if (dir is null)
				return null;
			return dir.FindDirectory(type);
		}

		/// <summary>
		/// Finds a <see cref="ResourceDirectory"/>
		/// </summary>
		/// <param name="type">Type</param>
		/// <param name="name">Name</param>
		/// <returns>The <see cref="ResourceDirectory"/> or <c>null</c> if none found</returns>
		public ResourceDirectory Find(ResourceName type, ResourceName name) {
			var dir = Find(type);
			if (dir is null)
				return null;
			return dir.FindDirectory(name);
		}

		/// <summary>
		/// Finds a <see cref="ResourceData"/>
		/// </summary>
		/// <param name="type">Type</param>
		/// <param name="name">Name</param>
		/// <param name="langId">Language ID</param>
		/// <returns>The <see cref="ResourceData"/> or <c>null</c> if none found</returns>
		public ResourceData Find(ResourceName type, ResourceName name, ResourceName langId) {
			var dir = Find(type, name);
			if (dir is null)
				return null;
			return dir.FindData(langId);
		}

		/// <inheritdoc/>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose method
		/// </summary>
		/// <param name="disposing"><c>true</c> if called by <see cref="Dispose()"/></param>
		protected virtual void Dispose(bool disposing) {
			if (!disposing)
				return;
			Root = null;	// Property handler will call Dispose()
		}
	}

	/// <summary>
	/// Win32 resources class created by the user
	/// </summary>
	public class Win32ResourcesUser : Win32Resources {
		ResourceDirectory root = new ResourceDirectoryUser(new ResourceName("root"));

		/// <inheritdoc/>
		public override ResourceDirectory Root {
			get => root;
			set => Interlocked.Exchange(ref root, value);
		}
	}

	/// <summary>
	/// Win32 resources class created from a PE file
	/// </summary>
	public sealed class Win32ResourcesPE : Win32Resources {
		/// <summary>
		/// Converts data RVAs to file offsets in <see cref="dataReader_factory"/>
		/// </summary>
		readonly IRvaFileOffsetConverter rvaConverter;

		/// <summary>
		/// This reader only reads the raw data. The data RVA is found in the data header and
		/// it's first converted to a file offset using <see cref="rvaConverter"/>. This file
		/// offset is where we'll read from using this reader.
		/// </summary>
		DataReaderFactory dataReader_factory;
		uint dataReader_offset;
		uint dataReader_length;
		bool owns_dataReader_factory;

		/// <summary>
		/// This reader only reads the directory entries and data headers. The data is read
		/// by <see cref="dataReader_factory"/>
		/// </summary>
		DataReaderFactory rsrcReader_factory;
		uint rsrcReader_offset;
		uint rsrcReader_length;
		bool owns_rsrcReader_factory;

		UserValue<ResourceDirectory> root;

#if THREAD_SAFE
		readonly Lock theLock = Lock.Create();
#endif

		/// <inheritdoc/>
		public override ResourceDirectory Root {
			get => root.Value;
			set {
				if (root.IsValueInitialized) {
					var origValue = root.Value;
					if (origValue == value)
						return;
				}
				root.Value = value;
			}
		}

		/// <summary>
		/// Gets the resource reader
		/// </summary>
		internal DataReader GetResourceReader() => rsrcReader_factory.CreateReader(rsrcReader_offset, rsrcReader_length);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="rvaConverter"><see cref="RVA"/>/<see cref="FileOffset"/> converter</param>
		/// <param name="rsrcReader_factory">Reader for the whole Win32 resources section (usually
		/// the .rsrc section). It's used to read <see cref="ResourceDirectory"/>'s and
		/// <see cref="ResourceData"/>'s but not the actual data blob.</param>
		/// <param name="rsrcReader_offset">Offset of resource section</param>
		/// <param name="rsrcReader_length">Length of resource section</param>
		/// <param name="owns_rsrcReader_factory">true if this instance can dispose of <paramref name="rsrcReader_factory"/></param>
		/// <param name="dataReader_factory">Data reader (it's used after converting an <see cref="RVA"/>
		/// to a <see cref="FileOffset"/>)</param>
		/// <param name="dataReader_offset">Offset of resource section</param>
		/// <param name="dataReader_length">Length of resource section</param>
		/// <param name="owns_dataReader_factory">true if this instance can dispose of <paramref name="dataReader_factory"/></param>
		public Win32ResourcesPE(IRvaFileOffsetConverter rvaConverter, DataReaderFactory rsrcReader_factory, uint rsrcReader_offset, uint rsrcReader_length, bool owns_rsrcReader_factory, DataReaderFactory dataReader_factory, uint dataReader_offset, uint dataReader_length, bool owns_dataReader_factory) {
			this.rvaConverter = rvaConverter ?? throw new ArgumentNullException(nameof(rvaConverter));
			this.rsrcReader_factory = rsrcReader_factory ?? throw new ArgumentNullException(nameof(rsrcReader_factory));
			this.rsrcReader_offset = rsrcReader_offset;
			this.rsrcReader_length = rsrcReader_length;
			this.owns_rsrcReader_factory = owns_rsrcReader_factory;
			this.dataReader_factory = dataReader_factory ?? throw new ArgumentNullException(nameof(dataReader_factory));
			this.dataReader_offset = dataReader_offset;
			this.dataReader_length = dataReader_length;
			this.owns_dataReader_factory = owns_dataReader_factory;
			Initialize();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="peImage">The PE image</param>
		public Win32ResourcesPE(IPEImage peImage)
			: this(peImage, null, 0, 0, false) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="peImage">The PE image</param>
		/// <param name="rsrcReader_factory">Reader for the whole Win32 resources section (usually
		/// the .rsrc section) or <c>null</c> if we should create one from the resource data
		/// directory in the optional header</param>
		/// <param name="rsrcReader_offset">Offset of resource section</param>
		/// <param name="rsrcReader_length">Length of resource section</param>
		/// <param name="owns_rsrcReader_factory">true if this instance can dispose of <paramref name="rsrcReader_factory"/></param>
		public Win32ResourcesPE(IPEImage peImage, DataReaderFactory rsrcReader_factory, uint rsrcReader_offset, uint rsrcReader_length, bool owns_rsrcReader_factory) {
			rvaConverter = peImage ?? throw new ArgumentNullException(nameof(peImage));
			dataReader_factory = peImage.DataReaderFactory;
			dataReader_offset = 0;
			dataReader_length = dataReader_factory.Length;
			if (!(rsrcReader_factory is null)) {
				this.rsrcReader_factory = rsrcReader_factory;
				this.rsrcReader_offset = rsrcReader_offset;
				this.rsrcReader_length = rsrcReader_length;
				this.owns_rsrcReader_factory = owns_rsrcReader_factory;
			}
			else {
				var dataDir = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[2];
				if (dataDir.VirtualAddress != 0 && dataDir.Size != 0) {
					var reader = peImage.CreateReader(dataDir.VirtualAddress, dataDir.Size);
					this.rsrcReader_factory = peImage.DataReaderFactory;
					this.rsrcReader_offset = reader.StartOffset;
					this.rsrcReader_length = reader.Length;
				}
				else {
					this.rsrcReader_factory = ByteArrayDataReaderFactory.Create(Array2.Empty<byte>(), filename: null);
					this.rsrcReader_offset = 0;
					this.rsrcReader_length = 0;
				}
			}
			Initialize();
		}

		void Initialize() {
			root.ReadOriginalValue = () => {
				var rsrcReader_factory = this.rsrcReader_factory;
				if (rsrcReader_factory is null)
					return null;    // It's disposed
				var reader = rsrcReader_factory.CreateReader(rsrcReader_offset, rsrcReader_length);
				return new ResourceDirectoryPE(0, new ResourceName("root"), this, ref reader);
			};
#if THREAD_SAFE
			root.Lock = theLock;
#endif
		}

		/// <summary>
		/// Creates a new data reader
		/// </summary>
		/// <param name="rva">RVA of data</param>
		/// <param name="size">Size of data</param>
		/// <returns></returns>
		public DataReader CreateReader(RVA rva, uint size) {
			GetDataReaderInfo(rva, size, out var dataReaderFactory, out uint dataOffset, out uint dataLength);
			return dataReaderFactory.CreateReader(dataOffset, dataLength);
		}

		internal void GetDataReaderInfo(RVA rva, uint size, out DataReaderFactory dataReaderFactory, out uint dataOffset, out uint dataLength) {
			dataOffset = (uint)rvaConverter.ToFileOffset(rva);
			if ((ulong)dataOffset + size <= dataReader_factory.Length) {
				dataReaderFactory = dataReader_factory;
				dataLength = size;
				return;
			}
			else {
				dataReaderFactory = ByteArrayDataReaderFactory.Create(Array2.Empty<byte>(), filename: null);
				dataOffset = 0;
				dataLength = 0;
			}
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing) {
			if (!disposing)
				return;
			if (owns_dataReader_factory)
				dataReader_factory?.Dispose();
			if (owns_rsrcReader_factory)
				rsrcReader_factory?.Dispose();
			dataReader_factory = null;
			rsrcReader_factory = null;
			base.Dispose(disposing);
		}
	}
}
