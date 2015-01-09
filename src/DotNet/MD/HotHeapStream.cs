// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Security;
using dnlib.IO;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// Stores some/all heap data
	/// </summary>
	abstract class HotHeapStream : IDisposable {
		readonly HeapType heapType;
		protected readonly IImageStream reader;
		protected readonly long baseOffset;
		protected bool invalid;
		protected long posData;
		protected long posIndexes;
		protected long posRids;
		protected uint numRids;
		protected long offsetMask;

		/// <summary>
		/// Gets the heap type
		/// </summary>
		public HeapType HeapType {
			get { return heapType; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="heapType">Heap type</param>
		/// <param name="reader">Data stream</param>
		/// <param name="baseOffset">Offset in <paramref name="reader"/> of start of data</param>
		protected HotHeapStream(HeapType heapType, IImageStream reader, long baseOffset) {
			this.heapType = heapType;
			this.reader = reader;
			this.baseOffset = baseOffset;
		}

		/// <summary>
		/// Must be called once after creating it so it can initialize
		/// </summary>
		/// <param name="mask">Offset mask (<c>0xFFFFFFFF</c> or <c>0xFFFFFFFFFFFFFFFF</c>)</param>
		public abstract void Initialize(long mask);

		/// <summary>
		/// Returns a stream that can access a blob
		/// </summary>
		/// <param name="originalHeapOffset">Offset in the original heap. If it's the #GUID heap, it should
		/// be <c>(index - 1) * 16</c></param>
		/// <returns>The reader (owned by us) or <c>null</c> if the data isn't present</returns>
		public IImageStream GetBlobReader(uint originalHeapOffset) {
			long dataOffset;
			if (GetBlobOffset(originalHeapOffset, out dataOffset)) {
				reader.Position = dataOffset;
				return reader;
			}
			return null;
		}

		/// <summary>
		/// Returns the offset in <see cref="reader"/> of some data
		/// </summary>
		/// <param name="originalHeapOffset">Original heap offset</param>
		/// <param name="dataOffset">Updated with offset in <see cref="reader"/> of data</param>
		/// <returns><c>true</c> if data is present, <c>false</c> if data is not present</returns>
		protected abstract bool GetBlobOffset(uint originalHeapOffset, out long dataOffset);

		/// <summary>
		/// Binary searches the rids table for <paramref name="originalHeapOffset"/>
		/// </summary>
		/// <param name="originalHeapOffset">Original heap offset</param>
		/// <returns>The rids table index or <see cref="uint.MaxValue"/> if not found</returns>
		protected uint BinarySearch(uint originalHeapOffset) {
			uint lo = 0, hi = numRids - 1;
			while (lo <= hi && hi != uint.MaxValue) {
				uint index = (lo + hi) / 2;
				uint val = reader.ReadUInt32At(posRids + index * 4);
				if (originalHeapOffset == val)
					return index;
				if (originalHeapOffset > val)
					lo = index + 1;
				else
					hi = index - 1;
			}
			return uint.MaxValue;
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
				if (reader != null)
					reader.Dispose();
			}
		}
	}

	/// <summary>
	/// Hot heap stream (CLR 2.0)
	/// </summary>
	sealed class HotHeapStreamCLR20 : HotHeapStream {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="heapType">Heap type</param>
		/// <param name="reader">Data stream</param>
		/// <param name="baseOffset">Offset in <paramref name="reader"/> of start of data</param>
		public HotHeapStreamCLR20(HeapType heapType, IImageStream reader, long baseOffset)
			: base(heapType, reader, baseOffset) {
		}

		/// <inheritdoc/>
		[HandleProcessCorruptedStateExceptions, SecurityCritical]	// Req'd on .NET 4.0
		public override void Initialize(long mask) {
			try {
				offsetMask = mask;
				reader.Position = baseOffset;
				posData = (baseOffset - reader.ReadInt32()) & mask;
				posIndexes = (baseOffset - (reader.ReadInt32() & ~3)) & mask;
				uint ridsOffset = reader.ReadUInt32();
				numRids = ridsOffset / 4;
				posRids = (baseOffset - numRids * 4) & mask;
			}
			// Ignore exceptions. The CLR only reads these values when needed. Assume
			// that this was invalid data that the CLR will never read anyway.
			catch (AccessViolationException) {
				invalid = true;
			}
			catch (IOException) {
				invalid = true;
			}
		}

		/// <inheritdoc/>
		protected override bool GetBlobOffset(uint originalHeapOffset, out long dataOffset) {
			if (invalid) {
				dataOffset = 0;
				return false;
			}
			uint index = BinarySearch(originalHeapOffset);
			if (index == uint.MaxValue) {
				dataOffset = 0;
				return false;
			}

			if (index == 0)
				dataOffset = posData;
			else
				dataOffset = posData + reader.ReadUInt32At((posIndexes + (index - 1) * 4) & offsetMask);
			dataOffset &= offsetMask;
			return true;
		}
	}

	/// <summary>
	/// Hot heap stream (CLR 4.0)
	/// </summary>
	sealed class HotHeapStreamCLR40 : HotHeapStream {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="heapType">Heap type</param>
		/// <param name="reader">Data stream</param>
		/// <param name="baseOffset">Offset in <paramref name="reader"/> of start of data</param>
		public HotHeapStreamCLR40(HeapType heapType, IImageStream reader, long baseOffset)
			: base(heapType, reader, baseOffset) {
		}

		/// <inheritdoc/>
		[HandleProcessCorruptedStateExceptions, SecurityCritical]	// Req'd on .NET 4.0
		public override void Initialize(long mask) {
			try {
				offsetMask = mask;
				reader.Position = baseOffset;
				uint ridsOffset = reader.ReadUInt32();
				numRids = ridsOffset / 4;
				posRids = (baseOffset - ridsOffset) & mask;
				posIndexes = (baseOffset - reader.ReadInt32()) & mask;
				posData = (baseOffset - reader.ReadInt32()) & mask;
			}
			// Ignore exceptions. The CLR only reads these values when needed. Assume
			// that this was invalid data that the CLR will never read anyway.
			catch (AccessViolationException) {
				invalid = true;
			}
			catch (IOException) {
				invalid = true;
			}
		}

		/// <inheritdoc/>
		protected override bool GetBlobOffset(uint originalHeapOffset, out long dataOffset) {
			if (invalid) {
				dataOffset = 0;
				return false;
			}
			uint index = BinarySearch(originalHeapOffset);
			if (index == uint.MaxValue) {
				dataOffset = 0;
				return false;
			}

			dataOffset = posData + reader.ReadUInt32At((posIndexes + index * 4) & offsetMask);
			dataOffset &= offsetMask;
			return true;
		}
	}
}
