// dnlib: See LICENSE.txt for more info

using System;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Pdb {
	readonly struct PdbReaderContext {
		readonly IPEImage peImage;
		readonly ImageDebugDirectory codeViewDebugDir;

		public bool HasDebugInfo => !(codeViewDebugDir is null);
		public ImageDebugDirectory CodeViewDebugDirectory => codeViewDebugDir;
		public PdbReaderOptions Options { get; }

		public PdbReaderContext(IPEImage peImage, PdbReaderOptions options) {
			this.peImage = peImage;
			Options = options;
			codeViewDebugDir = TryGetDebugDirectoryEntry(peImage, ImageDebugType.CodeView);
		}

		public ImageDebugDirectory TryGetDebugDirectoryEntry(ImageDebugType imageDebugType) =>
			TryGetDebugDirectoryEntry(peImage, imageDebugType);

		static ImageDebugDirectory TryGetDebugDirectoryEntry(IPEImage peImage, ImageDebugType imageDebugType) {
			var list = peImage.ImageDebugDirectories;
			int count = list.Count;
			for (int i = 0; i < count; i++) {
				var entry = list[i];
				if (entry.Type == imageDebugType)
					return entry;
			}
			return null;
		}

		public bool TryGetCodeViewData(out Guid guid, out uint age) => TryGetCodeViewData(out guid, out age, out _);

		public bool TryGetCodeViewData(out Guid guid, out uint age, out string pdbFilename) {
			guid = Guid.Empty;
			age = 0;
			pdbFilename = null;
			var reader = GetCodeViewDataReader();
			// magic, guid, age, zero-terminated string
			if (reader.Length < 4 + 16 + 4 + 1)
				return false;
			if (reader.ReadUInt32() != 0x53445352)
				return false;
			guid = reader.ReadGuid();
			age = reader.ReadUInt32();
			pdbFilename = reader.TryReadZeroTerminatedUtf8String();
			return !(pdbFilename is null);
		}

		DataReader GetCodeViewDataReader() {
			if (codeViewDebugDir is null)
				return default;
			return CreateReader(codeViewDebugDir.AddressOfRawData, codeViewDebugDir.SizeOfData);
		}

		public DataReader CreateReader(RVA rva, uint size) {
			if (rva == 0 || size == 0)
				return default;
			var reader = peImage.CreateReader(rva, size);
			if (reader.Length != size)
				return default;
			return reader;
		}
	}
}
