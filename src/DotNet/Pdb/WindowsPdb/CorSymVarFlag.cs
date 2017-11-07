// dnlib: See LICENSE.txt for more info

using System;

namespace dnlib.DotNet.Pdb.WindowsPdb {
	[Flags]
	enum CorSymVarFlag : uint {
		VAR_IS_COMP_GEN			= 0x00000001,
	}
}
