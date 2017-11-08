// dnlib: See LICENSE.txt for more info

using System.Diagnostics;

namespace dnlib.DotNet.Pdb.Portable {
	static class ImportDefinitionKindUtils {
		public const PdbImportDefinitionKind UNKNOWN_IMPORT_KIND = (PdbImportDefinitionKind)(-1);

		public static PdbImportDefinitionKind ToPdbImportDefinitionKind(uint value) {
			// See System.Reflection.Metadata.ImportDefinitionKind
			switch (value) {
			case 1:		return PdbImportDefinitionKind.ImportNamespace;
			case 2:		return PdbImportDefinitionKind.ImportAssemblyNamespace;
			case 3:		return PdbImportDefinitionKind.ImportType;
			case 4:		return PdbImportDefinitionKind.ImportXmlNamespace;
			case 5:		return PdbImportDefinitionKind.ImportAssemblyReferenceAlias;
			case 6:		return PdbImportDefinitionKind.AliasAssemblyReference;
			case 7:		return PdbImportDefinitionKind.AliasNamespace;
			case 8:		return PdbImportDefinitionKind.AliasAssemblyNamespace;
			case 9:		return PdbImportDefinitionKind.AliasType;
			default:
				Debug.Fail("Unknown import definition kind: 0x" + value.ToString("X"));
				return UNKNOWN_IMPORT_KIND;
			}
		}

		public static bool ToImportDefinitionKind(PdbImportDefinitionKind kind, out uint rawKind) {
			switch (kind) {
			case PdbImportDefinitionKind.ImportNamespace:				rawKind = 1; return true;
			case PdbImportDefinitionKind.ImportAssemblyNamespace:		rawKind = 2; return true;
			case PdbImportDefinitionKind.ImportType:					rawKind = 3; return true;
			case PdbImportDefinitionKind.ImportXmlNamespace:			rawKind = 4; return true;
			case PdbImportDefinitionKind.ImportAssemblyReferenceAlias:	rawKind = 5; return true;
			case PdbImportDefinitionKind.AliasAssemblyReference:		rawKind = 6; return true;
			case PdbImportDefinitionKind.AliasNamespace:				rawKind = 7; return true;
			case PdbImportDefinitionKind.AliasAssemblyNamespace:		rawKind = 8; return true;
			case PdbImportDefinitionKind.AliasType:						rawKind = 9; return true;
			default:													rawKind = uint.MaxValue; return false;
			}
		}
	}
}
