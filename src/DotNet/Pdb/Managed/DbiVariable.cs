// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet.Pdb.Symbols;
using dnlib.IO;

namespace dnlib.DotNet.Pdb.Managed {
	sealed class DbiVariable : SymbolVariable {
		public override string Name => name;
		string name;

		public override PdbLocalAttributes Attributes => attributes;
		PdbLocalAttributes attributes;

		public override int Index => index;
		int index;

		public override PdbCustomDebugInfo[] CustomDebugInfos => Array2.Empty<PdbCustomDebugInfo>();

		public bool Read(ref DataReader reader) {
			index = reader.ReadInt32();
			reader.Position += 10;
			ushort flags = reader.ReadUInt16();
			attributes = GetAttributes(flags);
			name = PdbReader.ReadCString(ref reader);

			const int fIsParam = 1;
			return (flags & fIsParam) == 0;
		}

		static PdbLocalAttributes GetAttributes(uint flags) {
			PdbLocalAttributes res = 0;
			const int fCompGenx = 4;
			if ((flags & fCompGenx) != 0)
				res |= PdbLocalAttributes.DebuggerHidden;
			return res;
		}
	}
}
