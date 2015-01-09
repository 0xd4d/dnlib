// dnlib: See LICENSE.txt for more info

﻿using System.IO;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Stores the instruction that jumps to _CorExeMain/_CorDllMain
	/// </summary>
	public sealed class StartupStub : IChunk {
		FileOffset offset;
		RVA rva;
		uint length;
		uint padding;

		/// <summary>
		/// Gets/sets the <see cref="ImportDirectory"/>
		/// </summary>
		public ImportDirectory ImportDirectory { get; set; }

		/// <summary>
		/// Gets/sets the <see cref="PEHeaders"/>
		/// </summary>
		public PEHeaders PEHeaders { get; set; }

		/// <inheritdoc/>
		public FileOffset FileOffset {
			get { return offset; }
		}

		/// <inheritdoc/>
		public RVA RVA {
			get { return rva; }
		}

		/// <summary>
		/// Gets the address of the JMP instruction
		/// </summary>
		public RVA EntryPointRVA {
			get { return rva + padding; }
		}

		/// <summary>
		/// Gets the address of the operand of the JMP instruction
		/// </summary>
		public RVA RelocRVA {
			get { return EntryPointRVA + 2; }
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;

			padding = rva.AlignUp(4) - rva + 2;
			length = padding + 6;
		}

		/// <inheritdoc/>
		public uint GetFileLength() {
			return length;
		}

		/// <inheritdoc/>
		public uint GetVirtualSize() {
			return GetFileLength();
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			writer.WriteZeros((int)padding);
			writer.Write((byte)0xFF);
			writer.Write((byte)0x25);
			writer.Write((uint)PEHeaders.ImageBase + (uint)ImportDirectory.IatCorXxxMainRVA);
		}
	}
}
