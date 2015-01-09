// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using dnlib.DotNet.MD;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// The hot metadata heap: <c>#!</c>. MS' CLR 2.0+ can read this heap. It's used by native
	/// images (NGEN'd) only, but if you patch the CLR, you can use it with normal non-NGEN'd
	/// images as well.
	/// It's only used by the CLR when the compressed heap (<c>#~</c>) is present, not when
	/// the ENC heap (<c>#-</c>) is present.
	/// </summary>
	public abstract class HotHeap : HeapBase {
		const uint HOT_HEAP_MAGIC = 0x484F4E44;	// "HOND"
		const int MAX_TABLES = (int)Table.GenericParamConstraint + 1;
		const uint HOT_HEAP_DIR_SIZE = 4 + MAX_TABLES * 4;
		const uint HH_ALIGNMENT = 4;

		uint totalLength;
		readonly HotTable[] headers = new HotTable[MAX_TABLES];
		readonly List<HotPool> hotPools = new List<HotPool>();

		/// <summary>
		/// Gets the hot heap version
		/// </summary>
		public abstract HotHeapVersion HotHeapVersion { get; }

		/// <inheritdoc/>
		public override string Name {
			get { return "#!"; }
		}

		/// <summary>
		/// Creates a <see cref="HotTable"/> instance
		/// </summary>
		/// <param name="mdTable">The MD table</param>
		public abstract HotTable CreateHotTable(IMDTable mdTable);

		/// <summary>
		/// Creates a <see cref="HotPool"/> instance
		/// </summary>
		/// <param name="heapType">Pool type</param>
		public abstract HotPool CreateHotPool(HeapType heapType);

		/// <summary>
		/// Creates a hot heap instance
		/// </summary>
		/// <param name="module">Target module</param>
		public static HotHeap Create(ModuleDef module) {
			return Create(GetHotHeapVersion(module));
		}

		/// <summary>
		/// Creates a hot heap instance
		/// </summary>
		/// <param name="version">Hot heap version</param>
		public static HotHeap Create(HotHeapVersion version) {
			switch (version) {
			case HotHeapVersion.CLR20: return new HotHeap20();
			case HotHeapVersion.CLR40: return new HotHeap40();
			default: throw new ArgumentException("Invalid version");
			}
		}

		/// <summary>
		/// Returns the correct hot heap version that should be used
		/// </summary>
		/// <param name="module">Target module</param>
		public static HotHeapVersion GetHotHeapVersion(ModuleDef module) {
			if (module.IsClr20)
				return HotHeapVersion.CLR20;
			return HotHeapVersion.CLR40;
		}

		/// <summary>
		/// Adds a hot table
		/// </summary>
		/// <param name="hotTable">The hot table</param>
		public void Add(HotTable hotTable) {
			var table = hotTable.Table;
			if ((uint)table >= (uint)headers.Length)
				throw new ArgumentException("Invalid table type");
			headers[(int)table] = hotTable;
		}

		/// <summary>
		/// Adds a hot pool
		/// </summary>
		/// <param name="hotPool">The hot pool</param>
		public void Add(HotPool hotPool) {
			hotPools.Add(hotPool);
		}

		/// <inheritdoc/>
		public override void SetOffset(FileOffset offset, RVA rva) {
			base.SetOffset(offset, rva);

			uint startOffs = (uint)offset;

			offset += HOT_HEAP_DIR_SIZE;
			rva += HOT_HEAP_DIR_SIZE;
			foreach (var header in headers) {
				if (header == null)
					continue;
				header.SetOffset(offset, rva);
				uint len = header.GetFileLength();
				offset += len;
				rva += len;
				Align(ref offset, ref rva);
			}

			foreach (var hotPool in hotPools) {
				Align(ref offset, ref rva);
				hotPool.SetOffset(offset, rva);
				uint len = hotPool.GetFileLength();
				offset += len;
				rva += len;
			}

			Align(ref offset, ref rva);
			offset += hotPools.Count * 8;
			rva += (uint)hotPools.Count * 8;

			offset += 8;
			rva += 8;

			totalLength = (uint)offset - startOffs;
		}

		static void Align(ref FileOffset offset, ref RVA rva) {
			offset = offset.AlignUp(HH_ALIGNMENT);
			rva = rva.AlignUp(HH_ALIGNMENT);
		}

		/// <inheritdoc/>
		public override uint GetRawLength() {
			return totalLength;
		}

		/// <inheritdoc/>
		protected override void WriteToImpl(BinaryWriter writer) {
			// The CLR doesn't verify this magic value
			writer.Write(HOT_HEAP_MAGIC);
			uint offs = HOT_HEAP_DIR_SIZE;
			foreach (var header in headers) {
				if (header == null)
					writer.Write(0);
				else {
					writer.Write(offs);	// any alignment, all bits are used
					offs += header.GetFileLength();
					offs = Utils.AlignUp(offs, HH_ALIGNMENT);
				}
			}

			offs = HOT_HEAP_DIR_SIZE;

			foreach (var header in headers) {
				if (header == null)
					continue;
				header.VerifyWriteTo(writer);
				offs += header.GetFileLength();
				WriteAlign(writer, ref offs);
			}

			var hotPoolOffset = new List<long>();
			foreach (var hotPool in hotPools) {
				WriteAlign(writer, ref offs);
				hotPoolOffset.Add(writer.BaseStream.Position);
				hotPool.VerifyWriteTo(writer);
				offs += hotPool.GetFileLength();
			}

			WriteAlign(writer, ref offs);
			long poolDBOffs = writer.BaseStream.Position;
			for (int i = 0; i < hotPools.Count; i++) {
				var hotPool = hotPools[i];
				writer.Write((uint)hotPool.HeapType);
				// CLR 2.0: low 2 bits are ignored
				// CLR 4.0: any alignment, all bits are used
				writer.Write((uint)(poolDBOffs - hotPoolOffset[i] - hotPool.HeaderOffset));
			}
			offs += (uint)hotPools.Count * 8;

			long hotMDDirOffs = writer.BaseStream.Position;
			// any alignment
			writer.Write((uint)(offs - HOT_HEAP_DIR_SIZE));
			// CLR 2.0: low 2 bits are ignored
			// CLR 4.0: any alignment, all bits are used
			writer.Write((uint)(hotMDDirOffs - poolDBOffs));
		}

		static void WriteAlign(BinaryWriter writer, ref uint offs) {
			uint align = Utils.AlignUp(offs, HH_ALIGNMENT) - offs;
			offs += align;
			writer.WriteZeros((int)align);
		}
	}

	/// <summary>
	/// CLR 2.0 (.NET 2.0 - 3.5) hot heap (#!)
	/// </summary>
	public sealed class HotHeap20 : HotHeap {
		/// <inheritdoc/>
		public override HotHeapVersion HotHeapVersion {
			get { return HotHeapVersion.CLR20; }
		}

		/// <inheritdoc/>
		public override HotTable CreateHotTable(IMDTable mdTable) {
			return new HotTable20(mdTable);
		}

		/// <inheritdoc/>
		public override HotPool CreateHotPool(HeapType heapType) {
			return new HotPool20(heapType);
		}
	}

	/// <summary>
	/// CLR 4.0 (.NET 4.0 - 4.5) hot heap (#!)
	/// </summary>
	public sealed class HotHeap40 : HotHeap {
		/// <inheritdoc/>
		public override HotHeapVersion HotHeapVersion {
			get { return HotHeapVersion.CLR40; }
		}

		/// <inheritdoc/>
		public override HotTable CreateHotTable(IMDTable mdTable) {
			return new HotTable40(mdTable);
		}

		/// <inheritdoc/>
		public override HotPool CreateHotPool(HeapType heapType) {
			return new HotPool40(heapType);
		}
	}
}
