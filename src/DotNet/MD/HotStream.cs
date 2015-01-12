// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Threading;
using dnlib.IO;
using dnlib.Threading;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet.MD {
	/// <summary>
	/// Represents the (undocumented) #! stream. The CLR only uses this stream if the
	/// normal compressed tables stream (#~) is used.
	/// </summary>
	abstract class HotStream : DotNetStream {
		protected readonly IImageStream fullStream;
		protected readonly long baseOffset;
		protected readonly long endOffset;
		protected HotTableStream hotTableStream;
		protected ThreadSafe.IList<HotHeapStream> hotHeapStreams;

		/// <summary>
		/// Gets the <see cref="dnlib.DotNet.MD.HotTableStream"/> or <c>null</c> if there's none
		/// </summary>
		public HotTableStream HotTableStream {
			get {
				if (hotTableStream == null) {
					var newHts = CreateHotTableStream();
					if (Interlocked.CompareExchange(ref hotTableStream, newHts, null) != null)
						newHts.Dispose();
				}
				return hotTableStream;
			}
		}

		/// <summary>
		/// Gets all <see cref="HotHeapStream"/>s
		/// </summary>
		public IList<HotHeapStream> HotHeapStreams {
			get {
				if (hotHeapStreams == null) {
					var newHhs = CreateHotHeapStreams();
					if (Interlocked.CompareExchange(ref hotHeapStreams, newHhs, null) != null) {
						foreach (var hhs in newHhs)
							hhs.Dispose();
					}
				}
				return hotHeapStreams;
			}
		}

		/// <summary>
		/// Creates a <see cref="HotStream"/> instance
		/// </summary>
		/// <param name="version">Hot heap version</param>
		/// <param name="imageStream">Heap stream</param>
		/// <param name="streamHeader">Stream header info</param>
		/// <param name="fullStream">Stream for the full PE file</param>
		/// <param name="baseOffset">Offset in <paramref name="fullStream"/> where the data starts</param>
		/// <returns>A <see cref="HotStream"/> instance or <c>null</c> if <paramref name="version"/>
		/// is invalid</returns>
		public static HotStream Create(HotHeapVersion version, IImageStream imageStream, StreamHeader streamHeader, IImageStream fullStream, FileOffset baseOffset) {
			switch (version) {
			case HotHeapVersion.CLR20: return new HotStreamCLR20(imageStream, streamHeader, fullStream, baseOffset);
			case HotHeapVersion.CLR40: return new HotStreamCLR40(imageStream, streamHeader, fullStream, baseOffset);
			default: return null;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="imageStream">Heap stream</param>
		/// <param name="streamHeader">Stream header info</param>
		/// <param name="fullStream">Stream for the full PE file</param>
		/// <param name="baseOffset">Offset in <paramref name="fullStream"/> where the data starts</param>
		protected HotStream(IImageStream imageStream, StreamHeader streamHeader, IImageStream fullStream, FileOffset baseOffset)
			: base(imageStream, streamHeader) {
			this.fullStream = fullStream;
			this.baseOffset = (long)baseOffset;
			this.endOffset = (long)baseOffset + imageStream.Length;
		}

		[HandleProcessCorruptedStateExceptions, SecurityCritical]	// Req'd on .NET 4.0
		HotTableStream CreateHotTableStream() {
			try {
				return CreateHotTableStreamImpl();
			}
			catch (AccessViolationException) {
				return null;
			}
			catch (IOException) {
				return null;
			}
		}

		[HandleProcessCorruptedStateExceptions, SecurityCritical]	// Req'd on .NET 4.0
		ThreadSafe.IList<HotHeapStream> CreateHotHeapStreams() {
			try {
				return CreateHotHeapStreams2();
			}
			catch (AccessViolationException) {
				return null;
			}
			catch (IOException) {
				return null;
			}
		}

		ThreadSafe.IList<HotHeapStream> CreateHotHeapStreams2() {
			var list = ThreadSafeListCreator.Create<HotHeapStream>();
			try {
				long dirBaseOffs = GetHotHeapDirectoryBaseOffset();
				for (long offs = dirBaseOffs; offs + 8 <= endOffset - 8; offs += 8) {
					fullStream.Position = offs;
					HeapType heapType;
					long hotHeapOffset;
					ReadHotHeapDirectory(fullStream, dirBaseOffs, out heapType, out hotHeapOffset);

					IImageStream dataStream = null;
					HotHeapStream hotHeapStream = null;
					try {
						dataStream = fullStream.Clone();
						list.Add(hotHeapStream = CreateHotHeapStream(heapType, dataStream, hotHeapOffset));
						dataStream = null;
						hotHeapStream = null;
					}
					catch {
						if (hotHeapStream != null)
							hotHeapStream.Dispose();
						if (dataStream != null)
							dataStream.Dispose();
						throw;
					}
				}
			}
			catch {
				foreach (var h in list)
					h.Dispose();
				throw;
			}
			return list;
		}

		/// <summary>
		/// Reads a hot heap directory
		/// </summary>
		/// <param name="reader">Reader stream</param>
		/// <param name="dirBaseOffs">Pool directory base offset</param>
		/// <param name="heapType">Updated with heap type</param>
		/// <param name="hotHeapOffs">Updated with heap offset</param>
		protected abstract void ReadHotHeapDirectory(IImageStream reader, long dirBaseOffs, out HeapType heapType, out long hotHeapOffs);

		/// <summary>
		/// Creates a <see cref="HotHeapStream"/>
		/// </summary>
		/// <param name="heapType">Heap type</param>
		/// <param name="stream">Data stream</param>
		/// <param name="baseOffset">Offset in <paramref name="stream"/> of start of data</param>
		/// <returns>A new <see cref="HotHeapStream"/> instance</returns>
		protected abstract HotHeapStream CreateHotHeapStream(HeapType heapType, IImageStream stream, long baseOffset);

		/// <summary>
		/// Creates the <see cref="dnlib.DotNet.MD.HotTableStream"/>
		/// </summary>
		/// <returns>A new instance or <c>null</c> if it doesn't exist</returns>
		protected abstract HotTableStream CreateHotTableStreamImpl();

		/// <summary>
		/// Gets the offset of the hot table directory in <see cref="fullStream"/>
		/// </summary>
		protected long GetHotTableBaseOffset() {
			// All bits in this dword are used
			return endOffset - 8 - HotTableStream.HOT_HEAP_DIR_SIZE - fullStream.ReadUInt32At(endOffset - 8);
		}

		/// <summary>
		/// Gets the offset of the hot pool directory in <see cref="fullStream"/>
		/// </summary>
		protected abstract long GetHotHeapDirectoryBaseOffset();

		/// <inheritdoc/>
		protected override void Dispose(bool disposing) {
			if (disposing) {
				IDisposable id = fullStream;
				if (id != null)
					id.Dispose();
				id = hotTableStream;
				if (id != null)
					id.Dispose();
				var hhs = hotHeapStreams;
				if (hhs != null) {
					foreach (var hs in hhs) {
						if (hs != null)
							hs.Dispose();
					}
				}
			}
			base.Dispose(disposing);
		}
	}

	/// <summary>
	/// Represents the #! stream. Should be used if the module uses CLR 2.0 (.NET 2.0 - 3.5)
	/// </summary>
	sealed class HotStreamCLR20 : HotStream {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="imageStream">Heap stream</param>
		/// <param name="streamHeader">Stream header info</param>
		/// <param name="fullStream">Stream for the full PE file</param>
		/// <param name="baseOffset">Offset in <paramref name="fullStream"/> where the data starts</param>
		public HotStreamCLR20(IImageStream imageStream, StreamHeader streamHeader, IImageStream fullStream, FileOffset baseOffset)
			: base(imageStream, streamHeader, fullStream, baseOffset) {
		}

		/// <inheritdoc/>
		protected override HotTableStream CreateHotTableStreamImpl() {
			IImageStream stream = null;
			try {
				stream = fullStream.Clone();
				return new HotTableStreamCLR20(stream, GetHotTableBaseOffset());
			}
			catch {
				if (stream != null)
					stream.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		protected override long GetHotHeapDirectoryBaseOffset() {
			// Lower 2 bits are ignored
			return endOffset - 8 - (fullStream.ReadUInt32At(endOffset - 8 + 4) & ~3);
		}

		/// <inheritdoc/>
		protected override void ReadHotHeapDirectory(IImageStream reader, long dirBaseOffs, out HeapType heapType, out long hotHeapOffs) {
			heapType = (HeapType)reader.ReadUInt32();
			// Lower 2 bits are ignored
			hotHeapOffs = dirBaseOffs - (reader.ReadUInt32() & ~3);
		}

		/// <inheritdoc/>
		protected override HotHeapStream CreateHotHeapStream(HeapType heapType, IImageStream stream, long baseOffset) {
			return new HotHeapStreamCLR20(heapType, stream, baseOffset);
		}
	}

	/// <summary>
	/// Represents the #! stream. Should be used if the module uses CLR 4.0 (.NET 4.0 - 4.5)
	/// </summary>
	sealed class HotStreamCLR40 : HotStream {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="imageStream">Heap stream</param>
		/// <param name="streamHeader">Stream header info</param>
		/// <param name="fullStream">Stream for the full PE file</param>
		/// <param name="baseOffset">Offset in <paramref name="fullStream"/> where the data starts</param>
		public HotStreamCLR40(IImageStream imageStream, StreamHeader streamHeader, IImageStream fullStream, FileOffset baseOffset)
			: base(imageStream, streamHeader, fullStream, baseOffset) {
		}

		/// <inheritdoc/>
		protected override HotTableStream CreateHotTableStreamImpl() {
			IImageStream stream = null;
			try {
				stream = fullStream.Clone();
				return new HotTableStreamCLR40(stream, GetHotTableBaseOffset());
			}
			catch {
				if (stream != null)
					stream.Dispose();
				throw;
			}
		}

		/// <inheritdoc/>
		protected override long GetHotHeapDirectoryBaseOffset() {
			// All bits are used
			return endOffset - 8 - fullStream.ReadUInt32At(endOffset - 8 + 4);
		}

		/// <inheritdoc/>
		protected override void ReadHotHeapDirectory(IImageStream reader, long dirBaseOffs, out HeapType heapType, out long hotHeapOffs) {
			heapType = (HeapType)reader.ReadUInt32();
			// All bits are used
			hotHeapOffs = dirBaseOffs - reader.ReadUInt32();
		}

		/// <inheritdoc/>
		protected override HotHeapStream CreateHotHeapStream(HeapType heapType, IImageStream stream, long baseOffset) {
			return new HotHeapStreamCLR40(heapType, stream, baseOffset);
		}
	}
}
