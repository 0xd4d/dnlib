// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using dnlib.DotNet.Writer;

namespace dnlib.DotNet.Pdb.Dss {
	/// <summary>
	/// Pass this instance to <see cref="ISymUnmanagedWriter.Initialize"/> when writing the PDB file
	/// </summary>
	sealed class MDEmitter : IMetaDataImport, IMetaDataEmit {
		readonly MetaData metaData;
		readonly Dictionary<uint, TypeDef> tokenToTypeDef;
		readonly Dictionary<uint, MethodDef> tokenToMethodDef;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="metaData">Metadata</param>
		public MDEmitter(MetaData metaData) {
			this.metaData = metaData;

			// We could get these from the metadata tables but it's just easier to get name,
			// declaring type etc using TypeDef and MethodDef.

			tokenToTypeDef = new Dictionary<uint, TypeDef>(metaData.TablesHeap.TypeDefTable.Rows);
			tokenToMethodDef = new Dictionary<uint, MethodDef>(metaData.TablesHeap.MethodTable.Rows);
			foreach (var type in metaData.Module.GetTypes()) {
				if (type == null)
					continue;
				tokenToTypeDef.Add(new MDToken(MD.Table.TypeDef, metaData.GetRid(type)).Raw, type);
				foreach (var method in type.Methods) {
					if (method == null)
						continue;
					tokenToMethodDef.Add(new MDToken(MD.Table.Method, metaData.GetRid(method)).Raw, method);
				}
			}
		}

		unsafe void IMetaDataImport.GetMethodProps(uint mb, uint* pClass, ushort* szMethod, uint cchMethod, uint* pchMethod, uint* pdwAttr, IntPtr* ppvSigBlob, uint* pcbSigBlob, uint* pulCodeRVA, uint* pdwImplFlags) {
			if ((mb >> 24) != 0x06)
				throw new ArgumentException();
			var method = tokenToMethodDef[mb];
			var row = metaData.TablesHeap.MethodTable[mb & 0x00FFFFFF];

			if (pClass != null)
				*pClass = new MDToken(MD.Table.TypeDef, metaData.GetRid(method.DeclaringType)).Raw;
			if (pdwAttr != null)
				*pdwAttr = row.Flags;
			if (ppvSigBlob != null)
				*ppvSigBlob = IntPtr.Zero;
			if (pcbSigBlob != null)
				*pcbSigBlob = 0;
			if (pulCodeRVA != null)
				*pulCodeRVA = row.RVA;
			if (pdwImplFlags != null)
				*pdwImplFlags = row.ImplFlags;

			string name = method.Name.String ?? string.Empty;
			int len = (int)Math.Min((uint)name.Length + 1, cchMethod);
			if (szMethod != null) {
				for (int i = 0; i < len - 1; i++, szMethod++)
					*szMethod = (ushort)name[i];
				if (len > 0)
					*szMethod = 0;
			}
			if (pchMethod != null)
				*pchMethod = (uint)len;
		}

		unsafe void IMetaDataImport.GetTypeDefProps(uint td, ushort* szTypeDef, uint cchTypeDef, uint* pchTypeDef, uint* pdwTypeDefFlags, uint* ptkExtends) {
			if ((td >> 24) != 0x02)
				throw new ArgumentException();
			var type = tokenToTypeDef[td];
			var row = metaData.TablesHeap.TypeDefTable[td & 0x00FFFFFF];
			if (pdwTypeDefFlags != null)
				*pdwTypeDefFlags = row.Flags;
			if (ptkExtends != null)
				*ptkExtends = row.Extends;

			string name = type.Name.String ?? string.Empty;
			int len = (int)Math.Min((uint)name.Length + 1, cchTypeDef);
			if (szTypeDef != null) {
				for (int i = 0; i < len - 1; i++, szTypeDef++)
					*szTypeDef = (ushort)name[i];
				if (len > 0)
					*szTypeDef = 0;
			}
			if (pchTypeDef != null)
				*pchTypeDef = (uint)len;
		}

		unsafe void IMetaDataImport.GetNestedClassProps(uint tdNestedClass, uint* ptdEnclosingClass) {
			if ((tdNestedClass >> 24) != 0x02)
				throw new ArgumentException();
			var type = tokenToTypeDef[tdNestedClass];
			var declType = type.DeclaringType;
			if (ptdEnclosingClass != null) {
				if (declType == null)
					*ptdEnclosingClass = 0;
				else
					*ptdEnclosingClass = new MDToken(MD.Table.TypeDef, metaData.GetRid(declType)).Raw;
			}
		}

		// The rest of the methods aren't called

		void IMetaDataImport.CloseEnum(IntPtr hEnum) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.CountEnum(IntPtr hEnum, ref uint pulCount) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.ResetEnum(IntPtr hEnum, uint ulPos) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.EnumTypeDefs(IntPtr phEnum, uint[] rTypeDefs, uint cMax, out uint pcTypeDefs) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.EnumInterfaceImpls(ref IntPtr phEnum, uint td, uint[] rImpls, uint cMax, ref uint pcImpls) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.EnumTypeRefs(ref IntPtr phEnum, uint[] rTypeRefs, uint cMax, ref uint pcTypeRefs) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.FindTypeDefByName(string szTypeDef, uint tkEnclosingClass, out uint ptd) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.GetScopeProps(IntPtr szName, uint cchName, out uint pchName, out Guid pmvid) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.GetModuleFromScope(out uint pmd) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.GetInterfaceImplProps(uint iiImpl, out uint pClass, out uint ptkIface) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.GetTypeRefProps(uint tr, out uint ptkResolutionScope, IntPtr szName, uint cchName, out uint pchName) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.ResolveTypeRef(uint tr, ref Guid riid, out object ppIScope, out uint ptd) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.EnumMembers(ref IntPtr phEnum, uint cl, uint[] rMembers, uint cMax, out uint pcTokens) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.EnumMembersWithName(ref IntPtr phEnum, uint cl, string szName, uint[] rMembers, uint cMax, out uint pcTokens) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.EnumMethods(ref IntPtr phEnum, uint cl, uint[] rMethods, uint cMax, out uint pcTokens) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.EnumMethodsWithName(ref IntPtr phEnum, uint cl, string szName, uint[] rMethods, uint cMax, out uint pcTokens) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.EnumFields(ref IntPtr phEnum, uint cl, uint[] rFields, uint cMax, out uint pcTokens) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.EnumFieldsWithName(ref IntPtr phEnum, uint cl, string szName, uint[] rFields, uint cMax, out uint pcTokens) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.EnumParams(ref IntPtr phEnum, uint mb, uint[] rParams, uint cMax, out uint pcTokens) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.EnumMemberRefs(ref IntPtr phEnum, uint tkParent, uint[] rMemberRefs, uint cMax, out uint pcTokens) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.EnumMethodImpls(ref IntPtr phEnum, uint td, uint[] rMethodBody, uint[] rMethodDecl, uint cMax, out uint pcTokens) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.EnumPermissionSets(ref IntPtr phEnum, uint tk, uint dwActions, uint[] rPermission, uint cMax, out uint pcTokens) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.FindMember(uint td, string szName, IntPtr pvSigBlob, uint cbSigBlob, out uint pmb) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.FindMethod(uint td, string szName, IntPtr pvSigBlob, uint cbSigBlob, out uint pmb) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.FindField(uint td, string szName, IntPtr pvSigBlob, uint cbSigBlob, out uint pmb) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.FindMemberRef(uint td, string szName, IntPtr pvSigBlob, uint cbSigBlob, out uint pmr) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.GetMemberRefProps(uint mr, out uint ptk, IntPtr szMember, uint cchMember, out uint pchMember, out IntPtr ppvSigBlob, out uint pbSig) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.EnumProperties(ref IntPtr phEnum, uint td, uint[] rProperties, uint cMax, out uint pcProperties) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.EnumEvents(ref IntPtr phEnum, uint td, uint[] rEvents, uint cMax, out uint pcEvents) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.GetEventProps(uint ev, out uint pClass, string szEvent, uint cchEvent, out uint pchEvent, out uint pdwEventFlags, out uint ptkEventType, out uint pmdAddOn, out uint pmdRemoveOn, out uint pmdFire, uint[] rmdOtherMethod, uint cMax, out uint pcOtherMethod) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.EnumMethodSemantics(ref IntPtr phEnum, uint mb, uint[] rEventProp, uint cMax, out uint pcEventProp) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.GetMethodSemantics(uint mb, uint tkEventProp, out uint pdwSemanticsFlags) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.GetClassLayout(uint td, out uint pdwPackSize, out IntPtr rFieldOffset, uint cMax, out uint pcFieldOffset, out uint pulClassSize) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.GetFieldMarshal(uint tk, out IntPtr ppvNativeType, out uint pcbNativeType) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.GetRVA(uint tk, out uint pulCodeRVA, out uint pdwImplFlags) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.GetPermissionSetProps(uint pm, out uint pdwAction, out IntPtr ppvPermission, out uint pcbPermission) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.GetSigFromToken(uint mdSig, out IntPtr ppvSig, out uint pcbSig) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.GetModuleRefProps(uint mur, IntPtr szName, uint cchName, out uint pchName) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.EnumModuleRefs(ref IntPtr phEnum, uint[] rModuleRefs, uint cmax, out uint pcModuleRefs) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.GetTypeSpecFromToken(uint typespec, out IntPtr ppvSig, out uint pcbSig) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.GetNameFromToken(uint tk, out IntPtr pszUtf8NamePtr) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.EnumUnresolvedMethods(ref IntPtr phEnum, uint[] rMethods, uint cMax, out uint pcTokens) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.GetUserString(uint stk, IntPtr szString, uint cchString, out uint pchString) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.GetPinvokeMap(uint tk, out uint pdwMappingFlags, IntPtr szImportName, uint cchImportName, out uint pchImportName, out uint pmrImportDLL) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.EnumSignatures(ref IntPtr phEnum, uint[] rSignatures, uint cmax, out uint pcSignatures) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.EnumTypeSpecs(ref IntPtr phEnum, uint[] rTypeSpecs, uint cmax, out uint pcTypeSpecs) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.EnumUserStrings(ref IntPtr phEnum, uint[] rStrings, uint cmax, out uint pcStrings) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.GetParamForMethodIndex(uint md, uint ulParamSeq, out uint ppd) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.EnumCustomAttributes(IntPtr phEnum, uint tk, uint tkType, uint[] rCustomAttributes, uint cMax, out uint pcCustomAttributes) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.GetCustomAttributeProps(uint cv, out uint ptkObj, out uint ptkType, out IntPtr ppBlob, out uint pcbSize) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.FindTypeRef(uint tkResolutionScope, string szName, out uint ptr) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.GetMemberProps(uint mb, out uint pClass, IntPtr szMember, uint cchMember, out uint pchMember, out uint pdwAttr, out IntPtr ppvSigBlob, out uint pcbSigBlob, out uint pulCodeRVA, out uint pdwImplFlags, out uint pdwCPlusTypeFlag, out IntPtr ppValue, out uint pcchValue) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.GetFieldProps(uint mb, out uint pClass, IntPtr szField, uint cchField, out uint pchField, out uint pdwAttr, out IntPtr ppvSigBlob, out uint pcbSigBlob, out uint pdwCPlusTypeFlag, out IntPtr ppValue, out uint pcchValue) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.GetPropertyProps(uint prop, out uint pClass, IntPtr szProperty, uint cchProperty, out uint pchProperty, out uint pdwPropFlags, out IntPtr ppvSig, out uint pbSig, out uint pdwCPlusTypeFlag, out IntPtr ppDefaultValue, out uint pcchDefaultValue, out uint pmdSetter, out uint pmdGetter, uint[] rmdOtherMethod, uint cMax, out uint pcOtherMethod) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.GetParamProps(uint tk, out uint pmd, out uint pulSequence, IntPtr szName, uint cchName, out uint pchName, out uint pdwAttr, out uint pdwCPlusTypeFlag, out IntPtr ppValue, out uint pcchValue) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.GetCustomAttributeByName(uint tkObj, string szName, out IntPtr ppData, out uint pcbData) {
			throw new NotImplementedException();
		}

		bool IMetaDataImport.IsValidToken(uint tk) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.GetNativeCallConvFromSig(IntPtr pvSig, uint cbSig, out uint pCallConv) {
			throw new NotImplementedException();
		}

		void IMetaDataImport.IsGlobal(uint pd, out int pbGlobal) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.SetModuleProps(string szName) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.Save(string szFile, uint dwSaveFlags) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.SaveToStream(IStream pIStream, uint dwSaveFlags) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.GetSaveSize(int fSave, out uint pdwSaveSize) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.DefineTypeDef(string szTypeDef, uint dwTypeDefFlags, uint tkExtends, uint[] rtkImplements, out uint ptd) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.DefineNestedType(string szTypeDef, uint dwTypeDefFlags, uint tkExtends, uint[] rtkImplements, uint tdEncloser, out uint ptd) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.SetHandler(object pUnk) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.DefineMethod(uint td, string szName, uint dwMethodFlags, IntPtr pvSigBlob, uint cbSigBlob, uint ulCodeRVA, uint dwImplFlags, out uint pmd) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.DefineMethodImpl(uint td, uint tkBody, uint tkDecl) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.DefineTypeRefByName(uint tkResolutionScope, string szName, out uint ptr) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.DefineImportType(IntPtr pAssemImport, IntPtr pbHashValue, uint cbHashValue, IMetaDataImport pImport, uint tdImport, IntPtr pAssemEmit, out uint ptr) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.DefineMemberRef(uint tkImport, string szName, IntPtr pvSigBlob, uint cbSigBlob, out uint pmr) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.DefineImportMember(IntPtr pAssemImport, IntPtr pbHashValue, uint cbHashValue, IMetaDataImport pImport, uint mbMember, IntPtr pAssemEmit, uint tkParent, out uint pmr) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.DefineEvent(uint td, string szEvent, uint dwEventFlags, uint tkEventType, uint mdAddOn, uint mdRemoveOn, uint mdFire, uint[] rmdOtherMethods, out uint pmdEvent) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.SetClassLayout(uint td, uint dwPackSize, IntPtr rFieldOffsets, uint ulClassSize) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.DeleteClassLayout(uint td) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.SetFieldMarshal(uint tk, IntPtr pvNativeType, uint cbNativeType) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.DeleteFieldMarshal(uint tk) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.DefinePermissionSet(uint tk, uint dwAction, IntPtr pvPermission, uint cbPermission, out uint ppm) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.SetRVA(uint md, uint ulRVA) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.GetTokenFromSig(IntPtr pvSig, uint cbSig, out uint pmsig) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.DefineModuleRef(string szName, out uint pmur) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.SetParent(uint mr, uint tk) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.GetTokenFromTypeSpec(IntPtr pvSig, uint cbSig, out uint ptypespec) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.SaveToMemory(out IntPtr pbData, uint cbData) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.DefineUserString(string szString, uint cchString, out uint pstk) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.DeleteToken(uint tkObj) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.SetMethodProps(uint md, uint dwMethodFlags, uint ulCodeRVA, uint dwImplFlags) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.SetTypeDefProps(uint td, uint dwTypeDefFlags, uint tkExtends, uint[] rtkImplements) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.SetEventProps(uint ev, uint dwEventFlags, uint tkEventType, uint mdAddOn, uint mdRemoveOn, uint mdFire, uint[] rmdOtherMethods) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.SetPermissionSetProps(uint tk, uint dwAction, IntPtr pvPermission, uint cbPermission, out uint ppm) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.DefinePinvokeMap(uint tk, uint dwMappingFlags, string szImportName, uint mrImportDLL) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.SetPinvokeMap(uint tk, uint dwMappingFlags, string szImportName, uint mrImportDLL) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.DeletePinvokeMap(uint tk) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.DefineCustomAttribute(uint tkOwner, uint tkCtor, IntPtr pCustomAttribute, uint cbCustomAttribute, out uint pcv) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.SetCustomAttributeValue(uint pcv, IntPtr pCustomAttribute, uint cbCustomAttribute) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.DefineField(uint td, string szName, uint dwFieldFlags, IntPtr pvSigBlob, uint cbSigBlob, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue, out uint pmd) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.DefineProperty(uint td, string szProperty, uint dwPropFlags, IntPtr pvSig, uint cbSig, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue, uint mdSetter, uint mdGetter, uint[] rmdOtherMethods, out uint pmdProp) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.DefineParam(uint md, uint ulParamSeq, string szName, uint dwParamFlags, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue, out uint ppd) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.SetFieldProps(uint fd, uint dwFieldFlags, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.SetPropertyProps(uint pr, uint dwPropFlags, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue, uint mdSetter, uint mdGetter, uint[] rmdOtherMethods) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.SetParamProps(uint pd, string szName, uint dwParamFlags, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.DefineSecurityAttributeSet(uint tkObj, IntPtr rSecAttrs, uint cSecAttrs, out uint pulErrorAttr) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.ApplyEditAndContinue(object pImport) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.TranslateSigWithScope(IntPtr pAssemImport, IntPtr pbHashValue, uint cbHashValue, IMetaDataImport import, IntPtr pbSigBlob, uint cbSigBlob, IntPtr pAssemEmit, IMetaDataEmit emit, IntPtr pvTranslatedSig, uint cbTranslatedSigMax, out uint pcbTranslatedSig) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.SetMethodImplFlags(uint md, uint dwImplFlags) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.SetFieldRVA(uint fd, uint ulRVA) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.Merge(IMetaDataImport pImport, IntPtr pHostMapToken, object pHandler) {
			throw new NotImplementedException();
		}

		void IMetaDataEmit.MergeEnd() {
			throw new NotImplementedException();
		}
	}
}
