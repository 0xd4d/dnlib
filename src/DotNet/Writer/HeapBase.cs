// dnlib: See LICENSE.txt for more info

﻿using System.IO;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Base class of most heaps
	/// </summary>
	public abstract class HeapBase : IHeap {
		internal const uint ALIGNMENT = 4;
		FileOffset offset;
		RVA rva;

		/// <summary>
		/// <c>true</c> if <see cref="SetReadOnly"/> has been called
		/// </summary>
		protected bool isReadOnly;

		/// <inheritdoc/>
		public FileOffset FileOffset {
			get { return offset; }
		}

		/// <inheritdoc/>
		public RVA RVA {
			get { return rva; }
		}

		/// <inheritdoc/>
		public abstract string Name { get; }

		/// <inheritdoc/>
		public bool IsEmpty {
			get { return GetRawLength() <= 1; }
		}

		/// <summary>
		/// <c>true</c> if offsets require 4 bytes instead of 2 bytes.
		/// </summary>
		public bool IsBig {
			get { return GetFileLength() > 0xFFFF; }
		}

		/// <inheritdoc/>
		public void SetReadOnly() {
			isReadOnly = true;
		}

		/// <inheritdoc/>
		public virtual void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;
		}

		/// <inheritdoc/>
		public uint GetFileLength() {
			return Utils.AlignUp(GetRawLength(), ALIGNMENT);
		}

		/// <inheritdoc/>
		public uint GetVirtualSize() {
			return GetFileLength();
		}

		/// <summary>
		/// Gets the raw length of the heap
		/// </summary>
		/// <returns>Raw length of the heap</returns>
		public abstract uint GetRawLength();

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			WriteToImpl(writer);
			writer.WriteZeros((int)(Utils.AlignUp(GetRawLength(), ALIGNMENT) - GetRawLength()));
		}

		/// <summary>
		/// Writes all data to <paramref name="writer"/> at its current location.
		/// </summary>
		/// <param name="writer">Destination</param>
		protected abstract void WriteToImpl(BinaryWriter writer);

		/// <inheritdoc/>
		public override string ToString() {
			return Name;
		}
	}
}
