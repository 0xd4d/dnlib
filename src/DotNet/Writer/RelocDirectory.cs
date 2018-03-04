// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Relocations directory
	/// </summary>
	public sealed class RelocDirectory : IChunk {
		readonly Machine machine;
		readonly List<RelocInfo> allRelocRvas = new List<RelocInfo>();
		readonly List<List<uint>> relocSections = new List<List<uint>>();
		bool isReadOnly;
		FileOffset offset;
		RVA rva;
		uint totalSize;

		struct RelocInfo {
			public readonly IChunk Chunk;
			public readonly uint OffsetOrRva;
			public RelocInfo(IChunk chunk, uint offset) {
				Chunk = chunk;
				OffsetOrRva = offset;
			}
		}

		/// <inheritdoc/>
		public FileOffset FileOffset {
			get { return offset; }
		}

		/// <inheritdoc/>
		public RVA RVA {
			get { return rva; }
		}

		internal bool NeedsRelocSection {
			get { return allRelocRvas.Count != 0; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="machine">Machine</param>
		public RelocDirectory(Machine machine) {
			this.machine = machine;
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			isReadOnly = true;
			this.offset = offset;
			this.rva = rva;

			var allRvas = new List<uint>(allRelocRvas.Count);
			foreach (var info in allRelocRvas) {
				uint relocRva;
				if (info.Chunk != null)
					relocRva = (uint)info.Chunk.RVA + info.OffsetOrRva;
				else
					relocRva = info.OffsetOrRva;
				allRvas.Add(relocRva);
			}
			allRvas.Sort();

			uint prevPage = uint.MaxValue;
			List<uint> pageList = null;
			foreach (var relocRva in allRvas) {
				uint page = relocRva & ~0xFFFU;
				if (page != prevPage) {
					prevPage = page;
					if (pageList != null)
						totalSize += (uint)(8 + ((pageList.Count + 1) & ~1) * 2);
					pageList = new List<uint>();
					relocSections.Add(pageList);
				}
				pageList.Add(relocRva);
			}
			if (pageList != null)
				totalSize += (uint)(8 + ((pageList.Count + 1) & ~1) * 2);
		}

		/// <inheritdoc/>
		public uint GetFileLength() {
			return totalSize;
		}

		/// <inheritdoc/>
		public uint GetVirtualSize() {
			return GetFileLength();
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			bool is64bit = machine == Machine.AMD64 || machine == Machine.IA64 || machine == Machine.ARM64;
			// 3 = IMAGE_REL_BASED_HIGHLOW, A = IMAGE_REL_BASED_DIR64
			uint relocType = is64bit ? 0xA000U : 0x3000;
			foreach (var pageList in relocSections) {
				writer.Write(pageList[0] & ~0xFFFU);
				writer.Write((uint)(8 + ((pageList.Count + 1) & ~1) * 2));
				foreach (var rva in pageList)
					writer.Write((ushort)(relocType | (rva & 0xFFF)));
				if ((pageList.Count & 1) != 0)
					writer.Write((ushort)0);
			}
		}

		/// <summary>
		/// Adds a relocation
		/// </summary>
		/// <param name="rva">RVA of location</param>
		public void Add(RVA rva) {
			if (isReadOnly)
				throw new InvalidOperationException("Can't add a relocation when the relocs section is read-only");
			allRelocRvas.Add(new RelocInfo(null, (uint)rva));
		}

		/// <summary>
		/// Adds a relocation
		/// </summary>
		/// <param name="chunk">Chunk or null. If it's null, <paramref name="offset"/> is the RVA</param>
		/// <param name="offset">Offset relative to the start of <paramref name="chunk"/>, or if <paramref name="chunk"/> is null, this is the RVA</param>
		public void Add(IChunk chunk, uint offset) {
			if (isReadOnly)
				throw new InvalidOperationException("Can't add a relocation when the relocs section is read-only");
			allRelocRvas.Add(new RelocInfo(chunk, offset));
		}
	}
}
