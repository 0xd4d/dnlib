// dnlib: See LICENSE.txt for more info

using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Import address table chunk
	/// </summary>
	public sealed class ImportAddressTable : IChunk {
		readonly bool is64bit;
		FileOffset offset;
		RVA rva;

		/// <summary>
		/// Gets/sets the <see cref="ImportDirectory"/>
		/// </summary>
		public ImportDirectory ImportDirectory { get; set; }

		/// <inheritdoc/>
		public FileOffset FileOffset => offset;

		/// <inheritdoc/>
		public RVA RVA => rva;

		internal bool Enable { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="is64bit">true if it's a 64-bit PE file, false if it's a 32-bit PE file</param>
		public ImportAddressTable(bool is64bit) => this.is64bit = is64bit;

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;
		}

		/// <inheritdoc/>
		public uint GetFileLength() {
			if (!Enable)
				return 0;
			return is64bit ? 16U : 8;
		}

		/// <inheritdoc/>
		public uint GetVirtualSize() => GetFileLength();

		/// <inheritdoc/>
		public void WriteTo(DataWriter writer) {
			if (!Enable)
				return;
			if (is64bit) {
				writer.WriteUInt64((ulong)(uint)ImportDirectory.CorXxxMainRVA);
				writer.WriteUInt64(0);
			}
			else {
				writer.WriteUInt32((uint)ImportDirectory.CorXxxMainRVA);
				writer.WriteInt32(0);
			}
		}
	}
}
