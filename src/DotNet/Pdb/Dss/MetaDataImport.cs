// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace dnlib.DotNet.Pdb.Dss {
	unsafe abstract class MetaDataImport : IMetaDataImport {
		public virtual void GetTypeDefProps([In] uint td, [In] ushort* szTypeDef, [In] uint cchTypeDef, [Out] uint* pchTypeDef, [Out] uint* pdwTypeDefFlags, [Out] uint* ptkExtends) => throw new NotImplementedException();
		public virtual void GetMethodProps(uint mb, uint* pClass, [In] ushort* szMethod, uint cchMethod, uint* pchMethod, uint* pdwAttr, [Out] IntPtr* ppvSigBlob, [Out] uint* pcbSigBlob, [Out] uint* pulCodeRVA, [Out] uint* pdwImplFlags) => throw new NotImplementedException();
		public virtual void GetNestedClassProps([In] uint tdNestedClass, [Out] uint* ptdEnclosingClass) => throw new NotImplementedException();
		public virtual void GetSigFromToken(uint mdSig, byte** ppvSig, uint* pcbSig) => throw new NotImplementedException();
		public virtual void GetTypeRefProps(uint tr, uint* ptkResolutionScope, ushort* szName, uint cchName, uint* pchName) => throw new NotImplementedException();

		protected void CopyTypeName(string typeNamespace, string typeName, ushort* destBuffer, uint destBufferLen, uint* requiredLength) {
			if (typeName is null)
				typeName = string.Empty;
			if (typeNamespace is null)
				typeNamespace = string.Empty;

			if (!(destBuffer is null) && destBufferLen > 0) {
				uint maxChars = destBufferLen - 1;
				uint w = 0;
				if (typeNamespace.Length > 0) {
					for (int i = 0; i < typeNamespace.Length && w < maxChars; i++, w++)
						*destBuffer++ = typeNamespace[i];
					if (w < maxChars) {
						*destBuffer++ = '.';
						w++;
					}
				}
				for (int i = 0; i < typeName.Length && w < maxChars; i++, w++)
					*destBuffer++ = typeName[i];
				Debug.Assert(w < destBufferLen);
				*destBuffer = 0;
			}

			if (!(requiredLength is null)) {
				int totalLen = typeNamespace.Length == 0 ? typeName.Length : typeNamespace.Length + 1 + typeName.Length;
				int copyLen = Math.Min(totalLen, (int)Math.Min(int.MaxValue, destBufferLen == 0 ? 0 : destBufferLen - 1));
				if (!(destBuffer is null))
					*requiredLength = (uint)copyLen;
				else
					*requiredLength = (uint)totalLen;
			}
		}

		void IMetaDataImport.CloseEnum(IntPtr hEnum) => throw new NotImplementedException();
		void IMetaDataImport.CountEnum(IntPtr hEnum, ref uint pulCount) => throw new NotImplementedException();
		void IMetaDataImport.ResetEnum(IntPtr hEnum, uint ulPos) => throw new NotImplementedException();
		void IMetaDataImport.EnumTypeDefs(IntPtr phEnum, uint[] rTypeDefs, uint cMax, out uint pcTypeDefs) => throw new NotImplementedException();
		void IMetaDataImport.EnumInterfaceImpls(ref IntPtr phEnum, uint td, uint[] rImpls, uint cMax, ref uint pcImpls) => throw new NotImplementedException();
		void IMetaDataImport.EnumTypeRefs(ref IntPtr phEnum, uint[] rTypeRefs, uint cMax, ref uint pcTypeRefs) => throw new NotImplementedException();
		void IMetaDataImport.FindTypeDefByName(string szTypeDef, uint tkEnclosingClass, out uint ptd) => throw new NotImplementedException();
		void IMetaDataImport.GetScopeProps(IntPtr szName, uint cchName, out uint pchName, out Guid pmvid) => throw new NotImplementedException();
		void IMetaDataImport.GetModuleFromScope(out uint pmd) => throw new NotImplementedException();
		void IMetaDataImport.GetInterfaceImplProps(uint iiImpl, out uint pClass, out uint ptkIface) => throw new NotImplementedException();
		void IMetaDataImport.ResolveTypeRef(uint tr, ref Guid riid, out object ppIScope, out uint ptd) => throw new NotImplementedException();
		void IMetaDataImport.EnumMembers(ref IntPtr phEnum, uint cl, uint[] rMembers, uint cMax, out uint pcTokens) => throw new NotImplementedException();
		void IMetaDataImport.EnumMembersWithName(ref IntPtr phEnum, uint cl, string szName, uint[] rMembers, uint cMax, out uint pcTokens) => throw new NotImplementedException();
		void IMetaDataImport.EnumMethods(ref IntPtr phEnum, uint cl, uint[] rMethods, uint cMax, out uint pcTokens) => throw new NotImplementedException();
		void IMetaDataImport.EnumMethodsWithName(ref IntPtr phEnum, uint cl, string szName, uint[] rMethods, uint cMax, out uint pcTokens) => throw new NotImplementedException();
		void IMetaDataImport.EnumFields(ref IntPtr phEnum, uint cl, uint[] rFields, uint cMax, out uint pcTokens) => throw new NotImplementedException();
		void IMetaDataImport.EnumFieldsWithName(ref IntPtr phEnum, uint cl, string szName, uint[] rFields, uint cMax, out uint pcTokens) => throw new NotImplementedException();
		void IMetaDataImport.EnumParams(ref IntPtr phEnum, uint mb, uint[] rParams, uint cMax, out uint pcTokens) => throw new NotImplementedException();
		void IMetaDataImport.EnumMemberRefs(ref IntPtr phEnum, uint tkParent, uint[] rMemberRefs, uint cMax, out uint pcTokens) => throw new NotImplementedException();
		void IMetaDataImport.EnumMethodImpls(ref IntPtr phEnum, uint td, uint[] rMethodBody, uint[] rMethodDecl, uint cMax, out uint pcTokens) => throw new NotImplementedException();
		void IMetaDataImport.EnumPermissionSets(ref IntPtr phEnum, uint tk, uint dwActions, uint[] rPermission, uint cMax, out uint pcTokens) => throw new NotImplementedException();
		void IMetaDataImport.FindMember(uint td, string szName, IntPtr pvSigBlob, uint cbSigBlob, out uint pmb) => throw new NotImplementedException();
		void IMetaDataImport.FindMethod(uint td, string szName, IntPtr pvSigBlob, uint cbSigBlob, out uint pmb) => throw new NotImplementedException();
		void IMetaDataImport.FindField(uint td, string szName, IntPtr pvSigBlob, uint cbSigBlob, out uint pmb) => throw new NotImplementedException();
		void IMetaDataImport.FindMemberRef(uint td, string szName, IntPtr pvSigBlob, uint cbSigBlob, out uint pmr) => throw new NotImplementedException();
		void IMetaDataImport.GetMemberRefProps(uint mr, out uint ptk, IntPtr szMember, uint cchMember, out uint pchMember, out IntPtr ppvSigBlob, out uint pbSig) => throw new NotImplementedException();
		void IMetaDataImport.EnumProperties(ref IntPtr phEnum, uint td, uint[] rProperties, uint cMax, out uint pcProperties) => throw new NotImplementedException();
		void IMetaDataImport.EnumEvents(ref IntPtr phEnum, uint td, uint[] rEvents, uint cMax, out uint pcEvents) => throw new NotImplementedException();
		void IMetaDataImport.GetEventProps(uint ev, out uint pClass, string szEvent, uint cchEvent, out uint pchEvent, out uint pdwEventFlags, out uint ptkEventType, out uint pmdAddOn, out uint pmdRemoveOn, out uint pmdFire, uint[] rmdOtherMethod, uint cMax, out uint pcOtherMethod) => throw new NotImplementedException();
		void IMetaDataImport.EnumMethodSemantics(ref IntPtr phEnum, uint mb, uint[] rEventProp, uint cMax, out uint pcEventProp) => throw new NotImplementedException();
		void IMetaDataImport.GetMethodSemantics(uint mb, uint tkEventProp, out uint pdwSemanticsFlags) => throw new NotImplementedException();
		void IMetaDataImport.GetClassLayout(uint td, out uint pdwPackSize, out IntPtr rFieldOffset, uint cMax, out uint pcFieldOffset, out uint pulClassSize) => throw new NotImplementedException();
		void IMetaDataImport.GetFieldMarshal(uint tk, out IntPtr ppvNativeType, out uint pcbNativeType) => throw new NotImplementedException();
		void IMetaDataImport.GetRVA(uint tk, out uint pulCodeRVA, out uint pdwImplFlags) => throw new NotImplementedException();
		void IMetaDataImport.GetPermissionSetProps(uint pm, out uint pdwAction, out IntPtr ppvPermission, out uint pcbPermission) => throw new NotImplementedException();
		void IMetaDataImport.GetModuleRefProps(uint mur, IntPtr szName, uint cchName, out uint pchName) => throw new NotImplementedException();
		void IMetaDataImport.EnumModuleRefs(ref IntPtr phEnum, uint[] rModuleRefs, uint cmax, out uint pcModuleRefs) => throw new NotImplementedException();
		void IMetaDataImport.GetTypeSpecFromToken(uint typespec, out IntPtr ppvSig, out uint pcbSig) => throw new NotImplementedException();
		void IMetaDataImport.GetNameFromToken(uint tk, out IntPtr pszUtf8NamePtr) => throw new NotImplementedException();
		void IMetaDataImport.EnumUnresolvedMethods(ref IntPtr phEnum, uint[] rMethods, uint cMax, out uint pcTokens) => throw new NotImplementedException();
		void IMetaDataImport.GetUserString(uint stk, IntPtr szString, uint cchString, out uint pchString) => throw new NotImplementedException();
		void IMetaDataImport.GetPinvokeMap(uint tk, out uint pdwMappingFlags, IntPtr szImportName, uint cchImportName, out uint pchImportName, out uint pmrImportDLL) => throw new NotImplementedException();
		void IMetaDataImport.EnumSignatures(ref IntPtr phEnum, uint[] rSignatures, uint cmax, out uint pcSignatures) => throw new NotImplementedException();
		void IMetaDataImport.EnumTypeSpecs(ref IntPtr phEnum, uint[] rTypeSpecs, uint cmax, out uint pcTypeSpecs) => throw new NotImplementedException();
		void IMetaDataImport.EnumUserStrings(ref IntPtr phEnum, uint[] rStrings, uint cmax, out uint pcStrings) => throw new NotImplementedException();
		void IMetaDataImport.GetParamForMethodIndex(uint md, uint ulParamSeq, out uint ppd) => throw new NotImplementedException();
		void IMetaDataImport.EnumCustomAttributes(IntPtr phEnum, uint tk, uint tkType, uint[] rCustomAttributes, uint cMax, out uint pcCustomAttributes) => throw new NotImplementedException();
		void IMetaDataImport.GetCustomAttributeProps(uint cv, out uint ptkObj, out uint ptkType, out IntPtr ppBlob, out uint pcbSize) => throw new NotImplementedException();
		void IMetaDataImport.FindTypeRef(uint tkResolutionScope, string szName, out uint ptr) => throw new NotImplementedException();
		void IMetaDataImport.GetMemberProps(uint mb, out uint pClass, IntPtr szMember, uint cchMember, out uint pchMember, out uint pdwAttr, out IntPtr ppvSigBlob, out uint pcbSigBlob, out uint pulCodeRVA, out uint pdwImplFlags, out uint pdwCPlusTypeFlag, out IntPtr ppValue, out uint pcchValue) => throw new NotImplementedException();
		void IMetaDataImport.GetFieldProps(uint mb, out uint pClass, IntPtr szField, uint cchField, out uint pchField, out uint pdwAttr, out IntPtr ppvSigBlob, out uint pcbSigBlob, out uint pdwCPlusTypeFlag, out IntPtr ppValue, out uint pcchValue) => throw new NotImplementedException();
		void IMetaDataImport.GetPropertyProps(uint prop, out uint pClass, IntPtr szProperty, uint cchProperty, out uint pchProperty, out uint pdwPropFlags, out IntPtr ppvSig, out uint pbSig, out uint pdwCPlusTypeFlag, out IntPtr ppDefaultValue, out uint pcchDefaultValue, out uint pmdSetter, out uint pmdGetter, uint[] rmdOtherMethod, uint cMax, out uint pcOtherMethod) => throw new NotImplementedException();
		void IMetaDataImport.GetParamProps(uint tk, out uint pmd, out uint pulSequence, IntPtr szName, uint cchName, out uint pchName, out uint pdwAttr, out uint pdwCPlusTypeFlag, out IntPtr ppValue, out uint pcchValue) => throw new NotImplementedException();
		void IMetaDataImport.GetCustomAttributeByName(uint tkObj, string szName, out IntPtr ppData, out uint pcbData) => throw new NotImplementedException();
		bool IMetaDataImport.IsValidToken(uint tk) => throw new NotImplementedException();
		void IMetaDataImport.GetNativeCallConvFromSig(IntPtr pvSig, uint cbSig, out uint pCallConv) => throw new NotImplementedException();
		void IMetaDataImport.IsGlobal(uint pd, out int pbGlobal) => throw new NotImplementedException();
	}
}
