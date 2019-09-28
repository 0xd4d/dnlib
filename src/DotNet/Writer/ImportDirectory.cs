// dnlib: See LICENSE.txt for more info

using System.Text;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Import directory chunk
	/// </summary>
	public sealed class ImportDirectory : IChunk {
		readonly bool is64bit;
		FileOffset offset;
		RVA rva;
		bool isExeFile;
		uint length;
		RVA importLookupTableRVA;
		RVA corXxxMainRVA;
		RVA dllToImportRVA;
		int stringsPadding;
		string dllToImport;
		string entryPointName;

		/// <summary>
		/// Gets/sets the <see cref="ImportAddressTable"/>
		/// </summary>
		public ImportAddressTable ImportAddressTable { get; set; }

		/// <summary>
		/// Gets the RVA of _CorDllMain/_CorExeMain in the import lookup table
		/// </summary>
		public RVA CorXxxMainRVA => corXxxMainRVA;

		/// <summary>
		/// Gets RVA of _CorExeMain/_CorDllMain in the IAT
		/// </summary>
		public RVA IatCorXxxMainRVA => ImportAddressTable.RVA;

		/// <summary>
		/// Gets/sets a value indicating whether this is a EXE or a DLL file
		/// </summary>
		public bool IsExeFile {
			get => isExeFile;
			set => isExeFile = value;
		}

		/// <inheritdoc/>
		public FileOffset FileOffset => offset;

		/// <inheritdoc/>
		public RVA RVA => rva;

		internal bool Enable { get; set; }

		/// <summary>
		/// Gets/sets the name of the dll which should be imported.
		/// </summary>
		public string DllToImport {
			get => dllToImport ?? "mscoree.dll";
			set => dllToImport = value;
		}

		/// <summary>
		/// Gets/sets the name of the entry point of the imported dll.
		/// </summary>
		public string EntryPointName {
			get => entryPointName ?? (IsExeFile ? "_CorExeMain" : "_CorDllMain");
			set => entryPointName = value;
		}

		const uint STRINGS_ALIGNMENT = 16;
		const uint ZERO_STRING_TERMINATION = 1;
		const uint TWO_BYTE_PADDING = 2;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="is64bit">true if it's a 64-bit PE file, false if it's a 32-bit PE file</param>
		public ImportDirectory(bool is64bit) => this.is64bit = is64bit;

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;

			length = 0x28;
			importLookupTableRVA = rva + length;
			length += is64bit ? 16U : 8;

			stringsPadding = (int)(rva.AlignUp(STRINGS_ALIGNMENT) - rva);
			length += (uint)stringsPadding;
			corXxxMainRVA = rva + length;
			length += (uint)EntryPointName.Length + ZERO_STRING_TERMINATION + TWO_BYTE_PADDING;
			dllToImportRVA = rva + length;
			length += (uint)DllToImport.Length + ZERO_STRING_TERMINATION;
			length++;
		}

		/// <inheritdoc/>
		public uint GetFileLength() {
			if (!Enable)
				return 0;
			return length;
		}

		/// <inheritdoc/>
		public uint GetVirtualSize() => GetFileLength();

		/// <inheritdoc/>
		public void WriteTo(DataWriter writer) {
			if (!Enable)
				return;
			writer.WriteUInt32((uint)importLookupTableRVA);
			writer.WriteInt32(0);	// DateTimeStamp
			writer.WriteInt32(0);	// ForwarderChain
			writer.WriteUInt32((uint)dllToImportRVA);	// Name
			writer.WriteUInt32((uint)ImportAddressTable.RVA);
			writer.WriteUInt64(0);
			writer.WriteUInt64(0);
			writer.WriteInt32(0);

			// ImportLookupTable
			if (is64bit) {
				writer.WriteUInt64((ulong)(uint)corXxxMainRVA);
				writer.WriteUInt64(0);
			}
			else {
				writer.WriteUInt32((uint)corXxxMainRVA);
				writer.WriteInt32(0);
			}

			writer.WriteZeroes(stringsPadding);
			writer.WriteUInt16(0);
			writer.WriteBytes(Encoding.UTF8.GetBytes($"{EntryPointName}\0"));
			writer.WriteBytes(Encoding.UTF8.GetBytes($"{DllToImport}\0"));

			writer.WriteByte(0);
		}
	}
}
