// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using dnlib.DotNet.Writer;

namespace dnlib.DotNet.Pdb.Dss {
	sealed unsafe class MDEmitter : MetaDataImport, IMetaDataEmit {
		readonly Metadata metadata;
		readonly Dictionary<uint, TypeDef> tokenToTypeDef;
		readonly Dictionary<uint, MethodDef> tokenToMethodDef;

		public MDEmitter(Metadata metadata) {
			this.metadata = metadata;

			// We could get these from the metadata tables but it's just easier to get name,
			// declaring type etc using TypeDef and MethodDef.

			tokenToTypeDef = new Dictionary<uint, TypeDef>(metadata.TablesHeap.TypeDefTable.Rows);
			tokenToMethodDef = new Dictionary<uint, MethodDef>(metadata.TablesHeap.MethodTable.Rows);
			foreach (var type in metadata.Module.GetTypes()) {
				if (type is null)
					continue;
				tokenToTypeDef.Add(new MDToken(MD.Table.TypeDef, metadata.GetRid(type)).Raw, type);
				foreach (var method in type.Methods) {
					if (method is null)
						continue;
					tokenToMethodDef.Add(new MDToken(MD.Table.Method, metadata.GetRid(method)).Raw, method);
				}
			}
		}

		public override void GetMethodProps(uint mb, uint* pClass, ushort* szMethod, uint cchMethod, uint* pchMethod, uint* pdwAttr, IntPtr* ppvSigBlob, uint* pcbSigBlob, uint* pulCodeRVA, uint* pdwImplFlags) {
			if ((mb >> 24) != 0x06)
				throw new ArgumentException();
			var method = tokenToMethodDef[mb];
			var row = metadata.TablesHeap.MethodTable[mb & 0x00FFFFFF];

			if (!(pClass is null))
				*pClass = new MDToken(MD.Table.TypeDef, metadata.GetRid(method.DeclaringType)).Raw;
			if (!(pdwAttr is null))
				*pdwAttr = row.Flags;
			if (!(ppvSigBlob is null))
				*ppvSigBlob = IntPtr.Zero;
			if (!(pcbSigBlob is null))
				*pcbSigBlob = 0;
			if (!(pulCodeRVA is null))
				*pulCodeRVA = row.RVA;
			if (!(pdwImplFlags is null))
				*pdwImplFlags = row.ImplFlags;

			string name = method.Name.String ?? string.Empty;
			int len = (int)Math.Min((uint)name.Length + 1, cchMethod);
			if (!(szMethod is null)) {
				for (int i = 0; i < len - 1; i++, szMethod++)
					*szMethod = (ushort)name[i];
				if (len > 0)
					*szMethod = 0;
			}
			if (!(pchMethod is null))
				*pchMethod = (uint)len;
		}

		public override void GetTypeDefProps(uint td, ushort* szTypeDef, uint cchTypeDef, uint* pchTypeDef, uint* pdwTypeDefFlags, uint* ptkExtends) {
			if ((td >> 24) != 0x02)
				throw new ArgumentException();
			var type = tokenToTypeDef[td];
			var row = metadata.TablesHeap.TypeDefTable[td & 0x00FFFFFF];
			if (!(pdwTypeDefFlags is null))
				*pdwTypeDefFlags = row.Flags;
			if (!(ptkExtends is null))
				*ptkExtends = row.Extends;
			CopyTypeName(type.Namespace, type.Name, szTypeDef, cchTypeDef, pchTypeDef);
		}

		public override void GetNestedClassProps(uint tdNestedClass, uint* ptdEnclosingClass) {
			if ((tdNestedClass >> 24) != 0x02)
				throw new ArgumentException();
			var type = tokenToTypeDef[tdNestedClass];
			var declType = type.DeclaringType;
			if (!(ptdEnclosingClass is null)) {
				if (declType is null)
					*ptdEnclosingClass = 0;
				else
					*ptdEnclosingClass = new MDToken(MD.Table.TypeDef, metadata.GetRid(declType)).Raw;
			}
		}

		void IMetaDataEmit.GetTokenFromSig(IntPtr pvSig, uint cbSig, out uint pmsig) => pmsig = 0x11000000;

		// The rest of the methods aren't called

		void IMetaDataEmit.SetModuleProps(string szName) => throw new NotImplementedException();
		void IMetaDataEmit.Save(string szFile, uint dwSaveFlags) => throw new NotImplementedException();
		void IMetaDataEmit.SaveToStream(IStream pIStream, uint dwSaveFlags) => throw new NotImplementedException();
		void IMetaDataEmit.GetSaveSize(int fSave, out uint pdwSaveSize) => throw new NotImplementedException();
		void IMetaDataEmit.DefineTypeDef(string szTypeDef, uint dwTypeDefFlags, uint tkExtends, uint[] rtkImplements, out uint ptd) => throw new NotImplementedException();
		void IMetaDataEmit.DefineNestedType(string szTypeDef, uint dwTypeDefFlags, uint tkExtends, uint[] rtkImplements, uint tdEncloser, out uint ptd) => throw new NotImplementedException();
		void IMetaDataEmit.SetHandler(object pUnk) => throw new NotImplementedException();
		void IMetaDataEmit.DefineMethod(uint td, string szName, uint dwMethodFlags, IntPtr pvSigBlob, uint cbSigBlob, uint ulCodeRVA, uint dwImplFlags, out uint pmd) => throw new NotImplementedException();
		void IMetaDataEmit.DefineMethodImpl(uint td, uint tkBody, uint tkDecl) => throw new NotImplementedException();
		void IMetaDataEmit.DefineTypeRefByName(uint tkResolutionScope, string szName, out uint ptr) => throw new NotImplementedException();
		void IMetaDataEmit.DefineImportType(IntPtr pAssemImport, IntPtr pbHashValue, uint cbHashValue, IMetaDataImport pImport, uint tdImport, IntPtr pAssemEmit, out uint ptr) => throw new NotImplementedException();
		void IMetaDataEmit.DefineMemberRef(uint tkImport, string szName, IntPtr pvSigBlob, uint cbSigBlob, out uint pmr) => throw new NotImplementedException();
		void IMetaDataEmit.DefineImportMember(IntPtr pAssemImport, IntPtr pbHashValue, uint cbHashValue, IMetaDataImport pImport, uint mbMember, IntPtr pAssemEmit, uint tkParent, out uint pmr) => throw new NotImplementedException();
		void IMetaDataEmit.DefineEvent(uint td, string szEvent, uint dwEventFlags, uint tkEventType, uint mdAddOn, uint mdRemoveOn, uint mdFire, uint[] rmdOtherMethods, out uint pmdEvent) => throw new NotImplementedException();
		void IMetaDataEmit.SetClassLayout(uint td, uint dwPackSize, IntPtr rFieldOffsets, uint ulClassSize) => throw new NotImplementedException();
		void IMetaDataEmit.DeleteClassLayout(uint td) => throw new NotImplementedException();
		void IMetaDataEmit.SetFieldMarshal(uint tk, IntPtr pvNativeType, uint cbNativeType) => throw new NotImplementedException();
		void IMetaDataEmit.DeleteFieldMarshal(uint tk) => throw new NotImplementedException();
		void IMetaDataEmit.DefinePermissionSet(uint tk, uint dwAction, IntPtr pvPermission, uint cbPermission, out uint ppm) => throw new NotImplementedException();
		void IMetaDataEmit.SetRVA(uint md, uint ulRVA) => throw new NotImplementedException();
		void IMetaDataEmit.DefineModuleRef(string szName, out uint pmur) => throw new NotImplementedException();
		void IMetaDataEmit.SetParent(uint mr, uint tk) => throw new NotImplementedException();
		void IMetaDataEmit.GetTokenFromTypeSpec(IntPtr pvSig, uint cbSig, out uint ptypespec) => throw new NotImplementedException();
		void IMetaDataEmit.SaveToMemory(out IntPtr pbData, uint cbData) => throw new NotImplementedException();
		void IMetaDataEmit.DefineUserString(string szString, uint cchString, out uint pstk) => throw new NotImplementedException();
		void IMetaDataEmit.DeleteToken(uint tkObj) => throw new NotImplementedException();
		void IMetaDataEmit.SetMethodProps(uint md, uint dwMethodFlags, uint ulCodeRVA, uint dwImplFlags) => throw new NotImplementedException();
		void IMetaDataEmit.SetTypeDefProps(uint td, uint dwTypeDefFlags, uint tkExtends, uint[] rtkImplements) => throw new NotImplementedException();
		void IMetaDataEmit.SetEventProps(uint ev, uint dwEventFlags, uint tkEventType, uint mdAddOn, uint mdRemoveOn, uint mdFire, uint[] rmdOtherMethods) => throw new NotImplementedException();
		void IMetaDataEmit.SetPermissionSetProps(uint tk, uint dwAction, IntPtr pvPermission, uint cbPermission, out uint ppm) => throw new NotImplementedException();
		void IMetaDataEmit.DefinePinvokeMap(uint tk, uint dwMappingFlags, string szImportName, uint mrImportDLL) => throw new NotImplementedException();
		void IMetaDataEmit.SetPinvokeMap(uint tk, uint dwMappingFlags, string szImportName, uint mrImportDLL) => throw new NotImplementedException();
		void IMetaDataEmit.DeletePinvokeMap(uint tk) => throw new NotImplementedException();
		void IMetaDataEmit.DefineCustomAttribute(uint tkOwner, uint tkCtor, IntPtr pCustomAttribute, uint cbCustomAttribute, out uint pcv) => throw new NotImplementedException();
		void IMetaDataEmit.SetCustomAttributeValue(uint pcv, IntPtr pCustomAttribute, uint cbCustomAttribute) => throw new NotImplementedException();
		void IMetaDataEmit.DefineField(uint td, string szName, uint dwFieldFlags, IntPtr pvSigBlob, uint cbSigBlob, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue, out uint pmd) => throw new NotImplementedException();
		void IMetaDataEmit.DefineProperty(uint td, string szProperty, uint dwPropFlags, IntPtr pvSig, uint cbSig, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue, uint mdSetter, uint mdGetter, uint[] rmdOtherMethods, out uint pmdProp) => throw new NotImplementedException();
		void IMetaDataEmit.DefineParam(uint md, uint ulParamSeq, string szName, uint dwParamFlags, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue, out uint ppd) => throw new NotImplementedException();
		void IMetaDataEmit.SetFieldProps(uint fd, uint dwFieldFlags, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue) => throw new NotImplementedException();
		void IMetaDataEmit.SetPropertyProps(uint pr, uint dwPropFlags, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue, uint mdSetter, uint mdGetter, uint[] rmdOtherMethods) => throw new NotImplementedException();
		void IMetaDataEmit.SetParamProps(uint pd, string szName, uint dwParamFlags, uint dwCPlusTypeFlag, IntPtr pValue, uint cchValue) => throw new NotImplementedException();
		void IMetaDataEmit.DefineSecurityAttributeSet(uint tkObj, IntPtr rSecAttrs, uint cSecAttrs, out uint pulErrorAttr) => throw new NotImplementedException();
		void IMetaDataEmit.ApplyEditAndContinue(object pImport) => throw new NotImplementedException();
		void IMetaDataEmit.TranslateSigWithScope(IntPtr pAssemImport, IntPtr pbHashValue, uint cbHashValue, IMetaDataImport import, IntPtr pbSigBlob, uint cbSigBlob, IntPtr pAssemEmit, IMetaDataEmit emit, IntPtr pvTranslatedSig, uint cbTranslatedSigMax, out uint pcbTranslatedSig) => throw new NotImplementedException();
		void IMetaDataEmit.SetMethodImplFlags(uint md, uint dwImplFlags) => throw new NotImplementedException();
		void IMetaDataEmit.SetFieldRVA(uint fd, uint ulRVA) => throw new NotImplementedException();
		void IMetaDataEmit.Merge(IMetaDataImport pImport, IntPtr pHostMapToken, object pHandler) => throw new NotImplementedException();
		void IMetaDataEmit.MergeEnd() => throw new NotImplementedException();
	}
}
