// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using dnlib.IO;
using dnlib.Threading;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// .NET metadata stream
	/// </summary>
	[DebuggerDisplay("{imageStream.Length} {streamHeader.Name}")]
	public class DotNetStream : IFileSection, IDisposable {
		/// <summary>
		/// Reader that can access the whole stream
		/// </summary>
		protected IImageStream imageStream;

		/// <summary>
		/// <c>null</c> if it wasn't present in the file
		/// </summary>
		StreamHeader streamHeader;

		/// <inheritdoc/>
		public FileOffset StartOffset {
			get { return imageStream.FileOffset; }
		}

		/// <inheritdoc/>
		public FileOffset EndOffset {
			get { return imageStream.FileOffset + imageStream.Length; }
		}

		/// <summary>
		/// Gets the length of the internal .NET blob stream
		/// </summary>
		public long ImageStreamLength {
			get { return imageStream.Length; }
		}

		/// <summary>
		/// Gets the stream header
		/// </summary>
		public StreamHeader StreamHeader {
			get { return streamHeader; }
		}

		/// <summary>
		/// Gets the name of the stream
		/// </summary>
		public string Name {
			get { return streamHeader == null ? string.Empty : streamHeader.Name; }
		}

		/// <summary>
		/// Returns a cloned <see cref="IImageStream"/> of the internal .NET blob stream.
		/// </summary>
		/// <returns>A new <see cref="IImageStream"/> instance</returns>
		public IImageStream GetClonedImageStream() {
			return imageStream.Clone();
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public DotNetStream() {
			this.imageStream = MemoryImageStream.CreateEmpty();
			this.streamHeader = null;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="imageStream">Stream data</param>
		/// <param name="streamHeader">The stream header</param>
		public DotNetStream(IImageStream imageStream, StreamHeader streamHeader) {
			this.imageStream = imageStream;
			this.streamHeader = streamHeader;
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
			if (disposing) {
				var ims = imageStream;
				if (ims != null)
					ims.Dispose();
				imageStream = null;
				streamHeader = null;
			}
		}

		/// <summary>
		/// Checks whether an index is valid
		/// </summary>
		/// <param name="index">The index</param>
		/// <returns><c>true</c> if the index is valid</returns>
		public virtual bool IsValidIndex(uint index) {
			return IsValidOffset(index);
		}

		/// <summary>
		/// Check whether an offset is within the stream
		/// </summary>
		/// <param name="offset">Stream offset</param>
		/// <returns><c>true</c> if the offset is valid</returns>
		public bool IsValidOffset(uint offset) {
			return offset == 0 || offset < imageStream.Length;
		}

		/// <summary>
		/// Check whether an offset is within the stream
		/// </summary>
		/// <param name="offset">Stream offset</param>
		/// <param name="size">Size of data</param>
		/// <returns><c>true</c> if the offset is valid</returns>
		public bool IsValidOffset(uint offset, int size) {
			if (size == 0)
				return IsValidOffset(offset);
			return size > 0 && (long)offset + (uint)size <= imageStream.Length;
		}
	}

	/// <summary>
	/// Base class of #US, #Strings, #Blob, and #GUID classes
	/// </summary>
	public abstract class HeapStream : DotNetStream {
		HotHeapStream hotHeapStream;
#if THREAD_SAFE
		internal readonly Lock theLock = Lock.Create();
#endif

		/// <summary>
		/// Gets/sets the <see cref="HotHeapStream"/> instance
		/// </summary>
		internal HotHeapStream HotHeapStream {
			set { hotHeapStream = value; }
		}

		/// <inheritdoc/>
		protected HeapStream() {
		}

		/// <inheritdoc/>
		protected HeapStream(IImageStream imageStream, StreamHeader streamHeader)
			: base(imageStream, streamHeader) {
		}

		/// <summary>
		/// Gets the heap reader and initializes its position
		/// </summary>
		/// <param name="offset">Offset in the heap. If it's the #GUID heap, this should
		/// be the offset of the GUID, not its index</param>
		/// <returns>The heap reader</returns>
		protected IImageStream GetReader_NoLock(uint offset) {
			var stream = hotHeapStream == null ? null : hotHeapStream.GetBlobReader(offset);
			if (stream == null) {
				stream = imageStream;
				stream.Position = offset;
			}
			return stream;
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing) {
			if (disposing) {
				var hhs = hotHeapStream;
				if (hhs != null)
					hhs.Dispose();
				hotHeapStream = null;
			}
			base.Dispose(disposing);
		}
	}
}
