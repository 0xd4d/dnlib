using System;
using System.Text;
using dot10.Utils;
using dot10.IO;
using dot10.PE;

namespace dot10.W32Resources {
	/// <summary>
	/// Win32 resources base class
	/// </summary>
	public abstract class Win32Resources : IDisposable {
		/// <summary>
		/// Gets/sets the root directory
		/// </summary>
		public abstract ResourceDirectory Root { get; set; }

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
			var root = Root;
			if (root != null)
				root.Dispose();
			Root = null;
		}
	}

	/// <summary>
	/// Win32 resources class created by the user
	/// </summary>
	public class Win32ResourcesUser : Win32Resources {
		ResourceDirectory root = new ResourceDirectoryUser(new ResourceName("root"));

		/// <inheritdoc/>
		public override ResourceDirectory Root {
			get { return root; }
			set { root = value; }
		}
	}

	/// <summary>
	/// Win32 resources class created from a PE file
	/// </summary>
	public sealed class Win32ResourcesPE : Win32Resources {
		/// <summary>
		/// Converts data RVAs to file offsets in <see cref="dataReader"/>
		/// </summary>
		IRvaFileOffsetConverter rvaConverter;

		/// <summary>
		/// This reader only reads the raw data. The data RVA is found in the data header and
		/// it's first converted to a file offset using <see cref="rvaConverter"/>. This file
		/// offset is where we'll read from using this reader.
		/// </summary>
		IImageStream dataReader;

		/// <summary>
		/// This reader only reads the directory entries and data headers. The data is read
		/// by <see cref="dataReader"/>
		/// </summary>
		IBinaryReader rsrcReader;

		UserValue<ResourceDirectory> root;

		/// <inheritdoc/>
		public override ResourceDirectory Root {
			get { return root.Value; }
			set { root.Value = value; }
		}

		/// <summary>
		/// Gets the resource reader
		/// </summary>
		public IBinaryReader ResourceReader {
			get { return rsrcReader; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="rvaConverter"><see cref="RVA"/>/<see cref="FileOffset"/> converter</param>
		/// <param name="dataReader">Data reader (it's used after converting an <see cref="RVA"/>
		/// to a <see cref="FileOffset"/>). This instance owns the reader.</param>
		/// <param name="rsrcReader">Reader for the whole Win32 resources section (usually
		/// the .rsrc section). It's used to read <see cref="ResourceDirectory"/>'s and
		/// <see cref="ResourceData"/>'s but not the actual data blob. This instance owns the
		/// reader.</param>
		public Win32ResourcesPE(IRvaFileOffsetConverter rvaConverter, IImageStream dataReader, IBinaryReader rsrcReader) {
			if (dataReader == rsrcReader)
				rsrcReader = dataReader.Create(0);	// Must not be the same readers
			this.rvaConverter = rvaConverter;
			this.dataReader = dataReader;
			this.rsrcReader = rsrcReader;
			Initialize();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="peImage">The PE image</param>
		public Win32ResourcesPE(IPEImage peImage)
			: this(peImage, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="peImage">The PE image</param>
		/// <param name="rsrcReader">Reader for the whole Win32 resources section (usually
		/// the .rsrc section) or <c>null</c> if we should create one from the resource data
		/// directory in the optional header. This instance owns the reader.</param>
		public Win32ResourcesPE(IPEImage peImage, IBinaryReader rsrcReader) {
			this.rvaConverter = peImage;
			this.dataReader = peImage.CreateFullStream();
			if (rsrcReader != null)
				this.rsrcReader = rsrcReader;
			else {
				var dataDir = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[2];
				if (dataDir.VirtualAddress != 0 && dataDir.Size != 0)
					this.rsrcReader = peImage.CreateStream(dataDir.VirtualAddress, dataDir.Size);
				else
					this.rsrcReader = MemoryImageStream.CreateEmpty();
			}
			Initialize();
		}

		void Initialize() {
			root.ReadOriginalValue = () => {
				long oldPos = rsrcReader.Position;
				rsrcReader.Position = 0;
				var dir = new ResourceDirectoryPE(0, new ResourceName("root"), this, rsrcReader);
				rsrcReader.Position = oldPos;
				return dir;
			};
		}

		/// <summary>
		/// Checks whether we can read <paramref name="size"/> bytes
		/// </summary>
		/// <param name="reader">Reader</param>
		/// <param name="size">Size in bytes</param>
		public static bool CanRead(IBinaryReader reader, int size) {
			return size >= 0 && CanRead(reader, (uint)size);
		}

		/// <summary>
		/// Checks whether we can read <paramref name="size"/> bytes
		/// </summary>
		/// <param name="reader">Reader</param>
		/// <param name="size">Size in bytes</param>
		public static bool CanRead(IBinaryReader reader, uint size) {
			return reader.Position + size <= reader.Length;
		}

		/// <summary>
		/// Reads a string
		/// </summary>
		/// <param name="reader">Reader</param>
		/// <param name="offset">Offset of string</param>
		/// <returns>The string or <c>null</c> if we could not read it</returns>
		public static string ReadString(IBinaryReader reader, uint offset) {
			reader.Position = offset;
			if (!CanRead(reader, 2))
				return null;
			int size = reader.ReadUInt16();
			int sizeInBytes = size * 2;
			if (!CanRead(reader, sizeInBytes))
				return null;
			var stringData = reader.ReadBytes(sizeInBytes);
			try {
				return Encoding.Unicode.GetString(stringData);
			}
			catch {
				return null;
			}
		}

		/// <summary>
		/// Creates a new data reader
		/// </summary>
		/// <param name="rva">RVA of data</param>
		/// <param name="size">Size of data</param>
		/// <returns>A new <see cref="IBinaryReader"/> for this data</returns>
		public IBinaryReader CreateDataReader(RVA rva, uint size) {
			var reader = dataReader.Create(rvaConverter.ToFileOffset(rva), size);
			if (reader.Length == size)
				return reader;
			reader.Dispose();
			return MemoryImageStream.CreateEmpty();
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing) {
			if (!disposing)
				return;
			if (dataReader != null)
				dataReader.Dispose();
			if (rsrcReader != null)
				rsrcReader.Dispose();
			dataReader = null;
			rsrcReader = null;
			base.Dispose(disposing);
		}
	}
}
