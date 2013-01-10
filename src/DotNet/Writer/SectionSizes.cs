/*
    Copyright (C) 2012-2013 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

﻿using System.Collections.Generic;
using dnlib.Utils;

namespace dnlib.DotNet.Writer {
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
