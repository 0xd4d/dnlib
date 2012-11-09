using System;
using System.IO;
using dot10.IO;

namespace dot10.W32Resources {
	/// <summary>
	/// A resource blob
	/// </summary>
	public sealed class ResourceData : ResourceDirectoryEntry, IDisposable {
		IBinaryReader reader;
		uint codePage;
		uint reserved;

		/// <summary>
		/// Gets/sets the data reader. This instance owns the reader.
		/// </summary>
		public IBinaryReader Data {
			get { return reader; }
			set {
				if (reader != null)
					reader.Dispose();
				reader = value;
			}
		}

		/// <summary>
		/// Gets/sets the code page
		/// </summary>
		public uint CodePage {
			get { return codePage; }
			set { codePage = value; }
		}

		/// <summary>
		/// Gets/sets the reserved field
		/// </summary>
		public uint Reserved {
			get { return reserved; }
			set { reserved = value; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		public ResourceData(ResourceName name)
			: base(name) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">Raw data. This instance owns this reader.</param>
		/// <param name="name">Name</param>
		public ResourceData(ResourceName name, IBinaryReader reader)
			: base(name) {
			this.reader = reader;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">Raw data. This instance owns this reader.</param>
		/// <param name="name">Name</param>
		/// <param name="codePage">Code page</param>
		/// <param name="reserved">Reserved value</param>
		public ResourceData(ResourceName name, IBinaryReader reader, uint codePage, uint reserved)
			: base(name) {
			this.reader = reader;
			this.codePage = codePage;
			this.reserved = reserved;
		}

		/// <summary>
		/// Gets the data as a <see cref="Stream"/>. It shares the file offsets with <see cref="Data"/>
		/// </summary>
		public Stream ToDataStream() {
			return Data.CreateStream();
		}

		/// <inheritdoc/>
		public void Dispose() {
			if (reader != null)
				reader.Dispose();
			reader = null;
		}
	}
}
