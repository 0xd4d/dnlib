// dnlib: See LICENSE.txt for more info

﻿using System;

namespace dnlib.DotNet.Pdb.Managed {
	struct DbiSourceLine {
		public DbiDocument Document;
		public uint Offset;
		public uint LineBegin;
		public uint LineEnd;
		public uint ColumnBegin;
		public uint ColumnEnd;
	}
}