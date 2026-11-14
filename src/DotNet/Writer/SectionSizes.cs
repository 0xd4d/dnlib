// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;

namespace dnlib.DotNet.Writer {
	readonly struct SectionSizeInfo {
		/// <summary>
		/// Length of section
		/// </summary>
		public readonly uint length;

		/// <summary>
		/// Section characteristics
		/// </summary>
		public readonly uint characteristics;

		/// <summary>
		/// RVA of section if <see cref="hasRva"/> is true, else the section follows the previous section
		/// </summary>
		public readonly uint rva;

		/// <summary>
		/// true if <see cref="rva"/> is valid
		/// </summary>
		public readonly bool hasRva;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="length">Length of section</param>
		/// <param name="characteristics">Section characteristics</param>
		public SectionSizeInfo(uint length, uint characteristics) {
			this.length = length;
			this.characteristics = characteristics;
			rva = 0;
			hasRva = false;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="rva">RVA of section. There can be a gap between this section and the previous section.</param>
		/// <param name="length">Length of section</param>
		/// <param name="characteristics">Section characteristics</param>
		public SectionSizeInfo(uint rva, uint length, uint characteristics) {
			this.length = length;
			this.characteristics = characteristics;
			this.rva = rva;
			hasRva = true;
		}
	}

	/// <summary>
	/// Calculates the optional header section sizes
	/// </summary>
	readonly struct SectionSizes {
		public readonly uint SizeOfHeaders;
		public readonly uint SizeOfImage;
		public readonly uint BaseOfData, BaseOfCode;
		public readonly uint SizeOfCode, SizeOfInitdData, SizeOfUninitdData;

		public static uint GetSizeOfHeaders(uint fileAlignment, uint headerLen) => Utils.AlignUp(headerLen, fileAlignment);

		public SectionSizes(uint fileAlignment, uint sectionAlignment, uint headerLen, Func<IEnumerable<SectionSizeInfo>> getSectionSizeInfos) {
			SizeOfHeaders = GetSizeOfHeaders(fileAlignment, headerLen);
			SizeOfImage = Utils.AlignUp(SizeOfHeaders, sectionAlignment);
			BaseOfData = 0;
			BaseOfCode = 0;
			SizeOfCode = 0;
			SizeOfInitdData = 0;
			SizeOfUninitdData = 0;
			foreach (var section in getSectionSizeInfos()) {
				// Sections are sorted by RVA but there can be gaps between them (eg. crossgen2 images)
				uint sectionRva = section.hasRva ? section.rva : SizeOfImage;
				uint fileAlignedVs = Utils.AlignUp(section.length, fileAlignment);

				bool isCode = (section.characteristics & 0x20) != 0;
				bool isInitdData = (section.characteristics & 0x40) != 0;
				bool isUnInitdData = (section.characteristics & 0x80) != 0;

				if (BaseOfCode == 0 && isCode)
					BaseOfCode = sectionRva;
				if (BaseOfData == 0 && (isInitdData || isUnInitdData))
					BaseOfData = sectionRva;
				if (isCode)
					SizeOfCode += fileAlignedVs;
				if (isInitdData)
					SizeOfInitdData += fileAlignedVs;
				if (isUnInitdData)
					SizeOfUninitdData += fileAlignedVs;

				uint sectionEnd = Utils.AlignUp(sectionRva + section.length, sectionAlignment);
				if (sectionEnd > SizeOfImage)
					SizeOfImage = sectionEnd;
			}
		}
	}
}
