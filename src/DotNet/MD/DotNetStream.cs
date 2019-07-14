// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using dnlib.IO;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// .NET metadata stream
	/// </summary>
	[DebuggerDisplay("{dataReader.Length} {streamHeader.Name}")]
	public abstract class DotNetStream : IFileSection, IDisposable {
		/// <summary>
		/// Reader that can access the whole stream.
		/// 
		/// NOTE: Always copy this field to a local variable before using it since it must be thread safe.
		/// </summary>
		protected DataReader dataReader;

		/// <summary>
		/// <c>null</c> if it wasn't present in the file
		/// </summary>
		StreamHeader streamHeader;

		DataReaderFactory mdReaderFactory;
		uint metadataBaseOffset;

		/// <inheritdoc/>
		public FileOffset StartOffset => (FileOffset)dataReader.StartOffset;

		/// <inheritdoc/>
		public FileOffset EndOffset => (FileOffset)dataReader.EndOffset;

		/// <summary>
		/// Gets the length of this stream in the metadata
		/// </summary>
		public uint StreamLength => dataReader.Length;

		/// <summary>
		/// Gets the stream header
		/// </summary>
		public StreamHeader StreamHeader => streamHeader;

		/// <summary>
		/// Gets the name of the stream
		/// </summary>
		public string Name => streamHeader is null ? string.Empty : streamHeader.Name;

		/// <summary>
		/// Gets a data reader that can read the full stream
		/// </summary>
		/// <returns></returns>
		public DataReader CreateReader() => dataReader;

		/// <summary>
		/// Default constructor
		/// </summary>
		protected DotNetStream() {
			streamHeader = null;
			dataReader = default;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="mdReaderFactory">Data reader factory</param>
		/// <param name="metadataBaseOffset">Offset of metadata</param>
		/// <param name="streamHeader">The stream header</param>
		protected DotNetStream(DataReaderFactory mdReaderFactory, uint metadataBaseOffset, StreamHeader streamHeader) {
			this.mdReaderFactory = mdReaderFactory;
			mdReaderFactory.DataReaderInvalidated += DataReaderFactory_DataReaderInvalidated;
			this.mdReaderFactory = mdReaderFactory;
			this.metadataBaseOffset = metadataBaseOffset;
			this.streamHeader = streamHeader;
			RecreateReader(mdReaderFactory, metadataBaseOffset, streamHeader, notifyThisClass: false);
		}

		void DataReaderFactory_DataReaderInvalidated(object sender, EventArgs e) => RecreateReader(mdReaderFactory, metadataBaseOffset, streamHeader, notifyThisClass: true);

		void RecreateReader(DataReaderFactory mdReaderFactory, uint metadataBaseOffset, StreamHeader streamHeader, bool notifyThisClass) {
			if (mdReaderFactory is null || streamHeader is null)
				dataReader = default;
			else
				dataReader = mdReaderFactory.CreateReader(metadataBaseOffset + streamHeader.Offset, streamHeader.StreamSize);
			if (notifyThisClass)
				OnReaderRecreated();
		}

		/// <summary>
		/// Called after <see cref="dataReader"/> gets recreated
		/// </summary>
		protected virtual void OnReaderRecreated() { }

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
			if (disposing) {
				var mdReaderFactory = this.mdReaderFactory;
				if (!(mdReaderFactory is null))
					mdReaderFactory.DataReaderInvalidated -= DataReaderFactory_DataReaderInvalidated;
				streamHeader = null;
				this.mdReaderFactory = null;
			}
		}

		/// <summary>
		/// Checks whether an index is valid
		/// </summary>
		/// <param name="index">The index</param>
		/// <returns><c>true</c> if the index is valid</returns>
		public virtual bool IsValidIndex(uint index) => IsValidOffset(index);

		/// <summary>
		/// Check whether an offset is within the stream
		/// </summary>
		/// <param name="offset">Stream offset</param>
		/// <returns><c>true</c> if the offset is valid</returns>
		public bool IsValidOffset(uint offset) => offset == 0 || offset < dataReader.Length;

		/// <summary>
		/// Check whether an offset is within the stream
		/// </summary>
		/// <param name="offset">Stream offset</param>
		/// <param name="size">Size of data</param>
		/// <returns><c>true</c> if the offset is valid</returns>
		public bool IsValidOffset(uint offset, int size) {
			if (size == 0)
				return IsValidOffset(offset);
			return size > 0 && (ulong)offset + (uint)size <= dataReader.Length;
		}
	}

	/// <summary>
	/// Base class of #US, #Strings, #Blob, and #GUID classes
	/// </summary>
	public abstract class HeapStream : DotNetStream {
		/// <inheritdoc/>
		protected HeapStream() {
		}

		/// <inheritdoc/>
		protected HeapStream(DataReaderFactory mdReaderFactory, uint metadataBaseOffset, StreamHeader streamHeader)
			: base(mdReaderFactory, metadataBaseOffset, streamHeader) {
		}
	}
}
