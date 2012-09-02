using System;
using System.Collections.Generic;
using dot10.PE;

namespace dot10.dotNET {
	/// <summary>
	/// Common base class for #~ and #- metadata classes
	/// </summary>
	abstract class MetaData : IMetaData {
		/// <summary>
		/// The PE image
		/// </summary>
		protected IPEImage peImage;

		/// <summary>
		/// The .NET header
		/// </summary>
		protected ImageCor20Header cor20Header;

		/// <summary>
		/// The MD header
		/// </summary>
		protected MetaDataHeader mdHeader;

		/// <summary>
		/// The #Strings stream
		/// </summary>
		protected StringsStream stringsStream;

		/// <summary>
		/// The #US stream
		/// </summary>
		protected USStream usStream;

		/// <summary>
		/// The #Blob stream
		/// </summary>
		protected BlobStream blobStream;

		/// <summary>
		/// The #GUID stream
		/// </summary>
		protected GuidStream guidStream;

		/// <summary>
		/// The #~ or #- stream
		/// </summary>
		protected TablesStream tablesStream;

		/// <summary>
		/// All the streams that are present in the PE image
		/// </summary>
		protected List<DotNetStream> allStreams;

		/// <inheritdoc/>
		public StringsStream StringsStream {
			get { return stringsStream; }
		}

		/// <inheritdoc/>
		public USStream USStream {
			get { return usStream; }
		}

		/// <inheritdoc/>
		public BlobStream BlobStream {
			get { return blobStream; }
		}

		/// <inheritdoc/>
		public GuidStream GuidStream {
			get { return guidStream; }
		}

		/// <inheritdoc/>
		public TablesStream TablesStream {
			get { return tablesStream; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="peImage">The PE image</param>
		/// <param name="cor20Header">The .NET header</param>
		/// <param name="mdHeader">The MD header</param>
		protected MetaData(IPEImage peImage, ImageCor20Header cor20Header, MetaDataHeader mdHeader) {
			try {
				this.allStreams = new List<DotNetStream>();
				this.peImage = peImage;
				this.cor20Header = cor20Header;
				this.mdHeader = mdHeader;
			}
			catch {
				if (peImage != null)
					peImage.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Initialize the metadata, tables, streams
		/// </summary>
		public abstract void Initialize();

		/// <inheritdoc/>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose method
		/// </summary>
		/// <param name="disposing">true if called by <see cref="Dispose()"/></param>
		protected virtual void Dispose(bool disposing) {
			if (!disposing)
				return;
			if (peImage != null)
				peImage.Dispose();
			if (stringsStream != null)
				stringsStream.Dispose();
			if (usStream != null)
				usStream.Dispose();
			if (blobStream != null)
				blobStream.Dispose();
			if (guidStream != null)
				guidStream.Dispose();
			if (tablesStream != null)
				tablesStream.Dispose();
			peImage = null;
			cor20Header = null;
			mdHeader = null;
			stringsStream = null;
			usStream = null;
			blobStream = null;
			guidStream = null;
			tablesStream = null;
			allStreams = null;
		}
	}
}
