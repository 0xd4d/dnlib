// dnlib: See LICENSE.txt for more info

﻿using System.Collections.Generic;
using dnlib.Utils;

namespace dnlib.DotNet.Writer {
	struct SectionSizeInfo {
		/// <summary>
		/// Length of section
		/// </summary>
		public readonly uint length;

		/// <summary>
		/// Section characteristics
		/// </summary>
		public readonly uint characteristics;

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
		public readonly uint SizeOfHeaders;
		public readonly uint SizeOfImage;
		public readonly uint BaseOfData, BaseOfCode;
		public readonly uint SizeOfCode, SizeOfInitdData, SizeOfUninitdData;

		public SectionSizes(uint fileAlignment, uint sectionAlignment, uint headerLen, MFunc<IEnumerable<SectionSizeInfo>> getSectionSizeInfos) {
			SizeOfHeaders = Utils.AlignUp(headerLen, fileAlignment);
			SizeOfImage = Utils.AlignUp(SizeOfHeaders, sectionAlignment);
			BaseOfData = 0;
			BaseOfCode = 0;
			SizeOfCode = 0;
			SizeOfInitdData = 0;
			SizeOfUninitdData = 0;
			foreach (var section in getSectionSizeInfos()) {
				uint sectAlignedVs = Utils.AlignUp(section.length, sectionAlignment);
				uint fileAlignedVs = Utils.AlignUp(section.length, fileAlignment);

				bool isCode = (section.characteristics & 0x20) != 0;
				bool isInitdData = (section.characteristics & 0x40) != 0;
				bool isUnInitdData = (section.characteristics & 0x80) != 0;

				if (BaseOfCode == 0 && isCode)
					BaseOfCode = SizeOfImage;
				if (BaseOfData == 0 && (isInitdData || isUnInitdData))
					BaseOfData = SizeOfImage;
				if (isCode)
					SizeOfCode += fileAlignedVs;
				if (isInitdData)
					SizeOfInitdData += fileAlignedVs;
				if (isUnInitdData)
					SizeOfUninitdData += fileAlignedVs;

				SizeOfImage += sectAlignedVs;
			}
		}
	}
}
