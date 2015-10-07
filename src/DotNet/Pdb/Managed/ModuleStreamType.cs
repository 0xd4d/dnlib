// dnlib: See LICENSE.txt for more info

namespace dnlib.DotNet.Pdb.Managed {
	enum ModuleStreamType : uint {
		Symbols = 0xF1,
		Lines = 0xF2,
		StringTable = 0xF3,
		FileInfo = 0xF4,
		FrameData = 0xF5,
		InlineeLines = 0xF6,
		CrossScopeImports = 0xF7,
		CrossScopeExports = 0xF8,
		ILLines = 0xF9,
		FuncMDTokenMap = 0xFA,
		TypeMDTokenMap = 0xFB,
		MergedAssemblyInput = 0xFC,
	}
}