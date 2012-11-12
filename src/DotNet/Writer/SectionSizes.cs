using System.Collections.Generic;
using dot10.Utils;

namespace dot10.DotNet.Writer {
	struct SectionSizeInfo {
		/// <summary>
		/// Length of section
		/// </summary>
		public uint length;

		/// <summary>
		/// Section characteristics
		/// </summary>
		public uint characteristics;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="length">Length of section</param>
		/// <param name="characteristics">Section characteristics</param>
		public SectionSizeInfo(uint length, uint characteristics) {
			this.length = length;
			this.characteristics = characteristics;
		}
	}

	/// <summary>
	/// Calculates the optional header section sizes
	/// </summary>
	struct SectionSizes {
		public uint sizeOfHeaders;
		public uint sizeOfImage;
		public uint baseOfData, baseOfCode;
		public uint sizeOfCode, sizeOfInitdData, sizeOfUninitdData;

		public SectionSizes(uint fileAlignment, uint sectionAlignment, uint headerLen, MFunc<IEnumerable<SectionSizeInfo>> getSectionSizeInfos) {
			sizeOfHeaders = Utils.AlignUp(headerLen, fileAlignment);
			sizeOfImage = Utils.AlignUp(sizeOfHeaders, sectionAlignment);
			baseOfData = 0;
			baseOfCode = 0;
			sizeOfCode = 0;
			sizeOfInitdData = 0;
			sizeOfUninitdData = 0;
			foreach (var section in getSectionSizeInfos()) {
				uint sectAlignedVs = Utils.AlignUp(section.length, sectionAlignment);
				uint fileAlignedVs = Utils.AlignUp(section.length, fileAlignment);

				bool isCode = (section.characteristics & 0x20) != 0;
				bool isInitdData = (section.characteristics & 0x40) != 0;
				bool isUnInitdData = (section.characteristics & 0x80) != 0;

				if (baseOfCode == 0 && isCode)
					baseOfCode = sizeOfImage;
				if (baseOfData == 0 && (isInitdData || isUnInitdData))
					baseOfData = sizeOfImage;
				if (isCode)
					sizeOfCode += fileAlignedVs;
				if (isInitdData)
					sizeOfInitdData += fileAlignedVs;
				if (isUnInitdData)
					sizeOfUninitdData += fileAlignedVs;

				sizeOfImage += sectAlignedVs;
			}
		}
	}
}
