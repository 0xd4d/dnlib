// dnlib: See LICENSE.txt for more info

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

// Dss = Diagnostics Symbol Store = http://msdn.microsoft.com/en-us/library/ms404519.aspx
namespace dnlib.DotNet.Pdb.Dss {
	[ComVisible(true),
	ComImport,
	Guid("809C652E-7396-11D2-9771-00A0C9B4D50C"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface IMetaDataDispenser {
		void DefineScope([In] ref Guid rclsid, [In] uint dwCreateFlags, [In] ref Guid riid, [Out, MarshalAs(UnmanagedType.IUnknown)] out object ppIUnk);
		void OpenScope([In, MarshalAs(UnmanagedType.LPWStr)] string szScope, [In] uint dwOpenFlags, [In] ref Guid riid, [Out, MarshalAs(UnmanagedType.IUnknown)] out object ppIUnk);
		void OpenScopeOnMemory([In] IntPtr pData, [In] uint cbData, [In] uint dwOpenFlags, [In] ref Guid riid, [Out, MarshalAs(UnmanagedType.IUnknown)] out object ppIUnk);
	}

	[ComVisible(true),
	ComImport,
	Guid("AA544D42-28CB-11D3-BD22-0000F80849BD"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface ISymUnmanagedBinder {
		[PreserveSig]
		int GetReaderForFile([In, MarshalAs(UnmanagedType.IUnknown)] object importer, [In, MarshalAs(UnmanagedType.LPWStr)] string fileName, [In, MarshalAs(UnmanagedType.LPWStr)] string searchPath, [Out] out ISymUnmanagedReader pRetVal);
		[PreserveSig]
		int GetReaderFromStream([In, MarshalAs(UnmanagedType.IUnknown)] object importer, [In] IStream pstream, [Out] out ISymUnmanagedReader pRetVal);
	}

	[ComVisible(true),
	ComImport,
	Guid("B4CE6286-2A6B-3712-A3B7-1EE1DAD467B5"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface ISymUnmanagedReader {
		void GetDocument([In, MarshalAs(UnmanagedType.LPWStr)] string url, [In] Guid language, [In] Guid languageVendor, [In] Guid documentType, [Out] out ISymUnmanagedDocument pRetVal);
		void GetDocuments([In] uint cDocs, [Out] out uint pcDocs, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedDocument[] pDocs);
		[PreserveSig]
		int GetUserEntryPoint([Out] out uint pToken);
		[PreserveSig]
		int GetMethod([In] uint token, [Out] out ISymUnmanagedMethod retVal);
		[PreserveSig]
		int GetMethodByVersion([In] uint token, [In] int version, [Out] out ISymUnmanagedMethod pRetVal);
		void GetVariables([In] uint parent, [In] uint cVars, [Out] out uint pcVars, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ISymUnmanagedVariable[] pVars);
		void GetGlobalVariables([In] uint cVars, [Out] out uint pcVars, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedVariable[] pVars);
		[PreserveSig]
		int GetMethodFromDocumentPosition([In] ISymUnmanagedDocument document, [In] uint line, [In] uint column, [Out] out ISymUnmanagedMethod pRetVal);
		void GetSymAttribute([In] uint parent, [In, MarshalAs(UnmanagedType.LPWStr)] string name, [In] uint cBuffer, [Out] out uint pcBuffer, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] buffer);
		void GetNamespaces([In] uint cNameSpaces, [Out] out uint pcNameSpaces, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedNamespace[] namespaces);
		void Initialize([In, MarshalAs(UnmanagedType.IUnknown)] object importer, [In, MarshalAs(UnmanagedType.LPWStr)] string filename, [In, MarshalAs(UnmanagedType.LPWStr)] string searchPath, [In] IStream pIStream);
		void UpdateSymbolStore([In, MarshalAs(UnmanagedType.LPWStr)] string filename, [In] IStream pIStream);
		void ReplaceSymbolStore([In, MarshalAs(UnmanagedType.LPWStr)] string filename, [In] IStream pIStream);
		void GetSymbolStoreFileName([In] uint cchName, [Out] out uint pcchName, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] char[] szName);
		void GetMethodsFromDocumentPosition([In] ISymUnmanagedDocument document, [In] uint line, [In] uint column, [In] uint cMethod, [Out] out uint pcMethod, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] ISymUnmanagedMethod[] pRetVal);
		void GetDocumentVersion([In] ISymUnmanagedDocument pDoc, [Out] out int version, [Out] out bool pbCurrent);
		void GetMethodVersion([In] ISymUnmanagedMethod pMethod, [Out] out int version);
	}

	[ComVisible(true),
	ComImport,
	Guid("40DE4037-7C81-3E1E-B022-AE1ABFF2CA08"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface ISymUnmanagedDocument {
		void GetURL([In] uint cchUrl, [Out] out uint pcchUrl, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] char[] szUrl);
		void GetDocumentType([Out] out Guid pRetVal);
		void GetLanguage([Out] out Guid pRetVal);
		void GetLanguageVendor([Out] out Guid pRetVal);
		void GetCheckSumAlgorithmId([Out] out Guid pRetVal);
		void GetCheckSum([In] uint cData, [Out] out uint pcData, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] data);
		void FindClosestLine([In] uint line, [Out] out uint pRetVal);
		void HasEmbeddedSource([Out] out bool pRetVal);
		void GetSourceLength([Out] out uint pRetVal);
		void GetSourceRange([In] uint startLine, [In] uint startColumn, [In] uint endLine, [In] uint endColumn, [In] uint cSourceBytes, [Out] out uint pcSourceBytes, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] source);
	}

	[ComVisible(true),
	ComImport,
	Guid("B62B923C-B500-3158-A543-24F307A8B7E1"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface ISymUnmanagedMethod {
		void GetToken([Out] out uint pToken);
		void GetSequencePointCount([Out] out uint pRetVal);
		void GetRootScope([Out] out ISymUnmanagedScope pRetVal);
		void GetScopeFromOffset([In] uint offset, [Out] out ISymUnmanagedScope pRetVal);
		void GetOffset([In] ISymUnmanagedDocument document, [In] uint line, [In] uint column, [Out] out uint pRetVal);
		void GetRanges([In] ISymUnmanagedDocument document, [In] uint line, [In] uint column, [In] uint cRanges, [Out] out uint pcRanges, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] int[] ranges);
		void GetParameters([In] uint cParams, [Out] out uint pcParams, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedVariable[] parameters);
		void GetNamespace([Out] out ISymUnmanagedNamespace pRetVal);
		void GetSourceStartEnd([In] ISymUnmanagedDocument[/*2*/] docs, [In] int[/*2*/] lines, [In] int[/*2*/] columns, [Out] out bool pRetVal);
		void GetSequencePoints([In] uint cPoints, [Out] out uint pcPoints, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] offsets, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedDocument[] documents, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] lines, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] columns, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] endLines, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] endColumns);
	}

	[ComVisible(true),
	ComImport,
	Guid("B20D55B3-532E-4906-87E7-25BD5734ABD2"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface ISymUnmanagedAsyncMethod {
		bool IsAsyncMethod();
		uint GetKickoffMethod();
		bool HasCatchHandlerILOffset();
		uint GetCatchHandlerILOffset();
		uint GetAsyncStepInfoCount();
		void GetAsyncStepInfo([In] uint cStepInfo, [Out] out uint pcStepInfo, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] uint[] yieldOffsets, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] uint[] breakpointOffset, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] uint[] breakpointMethod);
	}

	[ComVisible(true),
	ComImport,
	Guid("9F60EEBE-2D9A-3F7C-BF58-80BC991C60BB"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface ISymUnmanagedVariable {
		void GetName([In] uint cchName, [Out] out uint pcchName, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] char[] szName);
		void GetAttributes([Out] out uint pRetVal);
		void GetSignature([In] uint cSig, [Out] out uint pcSig, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] sig);
		void GetAddressKind([Out] out uint pRetVal);
		void GetAddressField1([Out] out uint pRetVal);
		void GetAddressField2([Out] out uint pRetVal);
		void GetAddressField3([Out] out uint pRetVal);
		void GetStartOffset([Out] out uint pRetVal);
		void GetEndOffset([Out] out uint pRetVal);
	}

	[ComVisible(true),
	ComImport,
	Guid("0DFF7289-54F8-11D3-BD28-0000F80849BD"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface ISymUnmanagedNamespace {
		void GetName([In] uint cchName, [Out] out uint pcchName, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] char[] szName);
		void GetNamespaces([In] uint cNameSpaces, [Out] out uint pcNameSpaces, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedNamespace[] namespaces);
		void GetVariables([In] uint cVars, [Out] out uint pcVars, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedVariable[] pVars);
	}

	[ComVisible(true),
	ComImport,
	Guid("68005D0F-B8E0-3B01-84D5-A11A94154942"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface ISymUnmanagedScope {
		void GetMethod([Out] out ISymUnmanagedMethod pRetVal);
		void GetParent([Out] out ISymUnmanagedScope pRetVal);
		void GetChildren([In] uint cChildren, [Out] out uint pcChildren, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedScope[] children);
		void GetStartOffset([Out] out uint pRetVal);
		void GetEndOffset([Out] out uint pRetVal);
		void GetLocalCount([Out] out uint pRetVal);
		void GetLocals([In] uint cLocals, [Out] out uint pcLocals, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedVariable[] locals);
		void GetNamespaces([In] uint cNameSpaces, [Out] out uint pcNameSpaces, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedNamespace[] namespaces);
	}

	[ComVisible(true),
	ComImport,
	Guid("AE932FBA-3FD8-4dba-8232-30A2309B02DB"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface ISymUnmanagedScope2 : ISymUnmanagedScope {
#pragma warning disable 0108
		void GetMethod([Out] out ISymUnmanagedMethod pRetVal);
		void GetParent([Out] out ISymUnmanagedScope pRetVal);
		void GetChildren([In] uint cChildren, [Out] out uint pcChildren, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedScope[] children);
		void GetStartOffset([Out] out uint pRetVal);
		void GetEndOffset([Out] out uint pRetVal);
		void GetLocalCount([Out] out uint pRetVal);
		void GetLocals([In] uint cLocals, [Out] out uint pcLocals, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedVariable[] locals);
		void GetNamespaces([In] uint cNameSpaces, [Out] out uint pcNameSpaces, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedNamespace[] namespaces);
#pragma warning restore 0108

		uint GetConstantCount();
		void GetConstants([In] uint cConstants, [Out] out uint pcConstants, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedConstant[] constants);
	}

	[ComVisible(true),
	ComImport,
	Guid("48B25ED8-5BAD-41bc-9CEE-CD62FABC74E9"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface ISymUnmanagedConstant {
		void GetName([In] uint cchName, [Out] out uint pcchName, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] char[] szName);
		void GetValue(out object pValue);
		[PreserveSig]
		int GetSignature([In] uint cSig, [Out] out uint pcSig, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] sig);
	}

	[ComVisible(true),
	ComImport,
	Guid("7DAC8207-D3AE-4C75-9B67-92801A497D44"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface IMetaDataImport {
		void CloseEnum(IntPtr hEnum);
		void CountEnum(IntPtr hEnum, ref uint pulCount);
		void ResetEnum(IntPtr hEnum, uint ulPos);
		void EnumTypeDefs(IntPtr phEnum, uint[] rTypeDefs, uint cMax, out uint pcTypeDefs);
		void EnumInterfaceImpls(ref IntPtr phEnum, uint td, uint[] rImpls, uint cMax, ref uint pcImpls);
		void EnumTypeRefs(ref IntPtr phEnum, uint[] rTypeRefs, uint cMax, ref uint pcTypeRefs);
		void FindTypeDefByName([In, MarshalAs(UnmanagedType.LPWStr)] string szTypeDef, [In] uint tkEnclosingClass, [Out] out uint ptd);
		void GetScopeProps([Out] IntPtr szName, [In] uint cchName, [Out] out uint pchName, [Out] out Guid pmvid);
		void GetModuleFromScope([Out] out uint pmd);
		unsafe void GetTypeDefProps([In] uint td, [In] ushort* szTypeDef, [In] uint cchTypeDef, [Out] uint* pchTypeDef, [Out] uint* pdwTypeDefFlags, [Out] uint* ptkExtends);
		void GetInterfaceImplProps([In] uint iiImpl, [Out] out uint pClass, [Out] out uint ptkIface);
		void GetTypeRefProps([In] uint tr, [Out] out uint ptkResolutionScope, [Out] IntPtr szName, [In] uint cchName, [Out] out uint pchName);
		void ResolveTypeRef(uint tr, ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppIScope, out uint ptd);
		void EnumMembers([In, Out] ref IntPtr phEnum, [In] uint cl, [Out] uint[] rMembers, [In] uint cMax, [Out] out uint pcTokens);
		void EnumMembersWithName([In, Out] ref IntPtr phEnum, [In] uint cl, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [Out] uint[] rMembers, [In] uint cMax, [Out] out uint pcTokens);
		void EnumMethods([In, Out] ref IntPtr phEnum, [In] uint cl, [Out] uint[] rMethods, [In] uint cMax, [Out] out uint pcTokens);
		void EnumMethodsWithName([In, Out] ref IntPtr phEnum, [In] uint cl, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, uint[] rMethods, [In] uint cMax, [Out] out uint pcTokens);
		void EnumFields([In, Out] ref IntPtr phEnum, [In] uint cl, [Out] uint[] rFields, [In] uint cMax, [Out] out uint pcTokens);
		void EnumFieldsWithName([In, Out] ref IntPtr phEnum, [In] uint cl, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [Out] uint[] rFields, [In] uint cMax, [Out] out uint pcTokens);
		void EnumParams([In, Out] ref IntPtr phEnum, [In] uint mb, [Out] uint[] rParams, [In] uint cMax, [Out] out uint pcTokens);
		void EnumMemberRefs([In, Out] ref IntPtr phEnum, [In] uint tkParent, [Out] uint[] rMemberRefs, [In] uint cMax, [Out] out uint pcTokens);
		void EnumMethodImpls([In, Out] ref IntPtr phEnum, [In] uint td, [Out] uint[] rMethodBody, [Out] uint[] rMethodDecl, [In] uint cMax, [Out] out uint pcTokens);
		void EnumPermissionSets([In, Out] ref IntPtr phEnum, [In] uint tk, [In] uint dwActions, [Out] uint[] rPermission, [In] uint cMax, [Out] out uint pcTokens);
		void FindMember([In] uint td, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [In] IntPtr pvSigBlob, [In] uint cbSigBlob, [Out] out uint pmb);
		void FindMethod([In] uint td, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [In] IntPtr pvSigBlob, [In] uint cbSigBlob, [Out] out uint pmb);
		void FindField([In] uint td, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [In] IntPtr pvSigBlob, [In] uint cbSigBlob, [Out] out uint pmb);
		void FindMemberRef([In] uint td, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [In] IntPtr pvSigBlob, [In] uint cbSigBlob, [Out] out uint pmr);
		unsafe void GetMethodProps(uint mb, uint* pClass, [In] ushort* szMethod, uint cchMethod, uint* pchMethod, uint* pdwAttr, [Out] IntPtr* ppvSigBlob, [Out] uint* pcbSigBlob, [Out] uint* pulCodeRVA, [Out] uint* pdwImplFlags);
		void GetMemberRefProps([In] uint mr, [Out] out uint ptk, [Out] IntPtr szMember, [In] uint cchMember, [Out] out uint pchMember, [Out] out IntPtr ppvSigBlob, [Out] out uint pbSig);
		void EnumProperties([In, Out] ref IntPtr phEnum, [In] uint td, [Out] uint[] rProperties, [In] uint cMax, [Out] out uint pcProperties);
		void EnumEvents([In, Out] ref IntPtr phEnum, [In] uint td, [Out] uint[] rEvents, [In] uint cMax, [Out] out uint pcEvents);
		void GetEventProps([In] uint ev, [Out] out uint pClass, [Out] [MarshalAs(UnmanagedType.LPWStr)] string szEvent, [In] uint cchEvent, [Out] out uint pchEvent, [Out] out uint pdwEventFlags, [Out] out uint ptkEventType, [Out] out uint pmdAddOn, [Out] out uint pmdRemoveOn, [Out] out uint pmdFire, [In, Out] uint[] rmdOtherMethod, [In] uint cMax, [Out] out uint pcOtherMethod);
		void EnumMethodSemantics([In, Out] ref IntPtr phEnum, [In] uint mb, [In, Out] uint[] rEventProp, [In] uint cMax, [Out] out uint pcEventProp);
		void GetMethodSemantics([In] uint mb, [In] uint tkEventProp, [Out] out uint pdwSemanticsFlags);
		void GetClassLayout([In] uint td, [Out] out uint pdwPackSize, [Out] out IntPtr rFieldOffset, [In] uint cMax, [Out] out uint pcFieldOffset, [Out] out uint pulClassSize);
		void GetFieldMarshal([In] uint tk, [Out] out IntPtr ppvNativeType, [Out] out uint pcbNativeType);
		void GetRVA(uint tk, out uint pulCodeRVA, out uint pdwImplFlags);
		void GetPermissionSetProps([In] uint pm, [Out] out uint pdwAction, [Out] out IntPtr ppvPermission, [Out] out uint pcbPermission);
		void GetSigFromToken([In] uint mdSig, [Out] out IntPtr ppvSig, [Out] out uint pcbSig);
		void GetModuleRefProps([In] uint mur, [Out] IntPtr szName, [In] uint cchName, [Out] out uint pchName);
		void EnumModuleRefs([In, Out] ref IntPtr phEnum, [Out] uint[] rModuleRefs, [In] uint cmax, [Out] out uint pcModuleRefs);
		void GetTypeSpecFromToken([In] uint typespec, [Out] out IntPtr ppvSig, [Out] out uint pcbSig);
		void GetNameFromToken([In] uint tk, [Out] out IntPtr pszUtf8NamePtr);
		void EnumUnresolvedMethods([In, Out] ref IntPtr phEnum, [Out] uint[] rMethods, [In] uint cMax, [Out] out uint pcTokens);
		void GetUserString([In] uint stk, [Out] IntPtr szString, [In] uint cchString, [Out] out uint pchString);
		void GetPinvokeMap([In] uint tk, [Out] out uint pdwMappingFlags, [Out] IntPtr szImportName, [In] uint cchImportName, [Out] out uint pchImportName, [Out] out uint pmrImportDLL);
		void EnumSignatures([In, Out] ref IntPtr phEnum, [Out] uint[] rSignatures, [In] uint cmax, [Out] out uint pcSignatures);
		void EnumTypeSpecs([In, Out] ref IntPtr phEnum, [Out] uint[] rTypeSpecs, [In] uint cmax, [Out] out uint pcTypeSpecs);
		void EnumUserStrings([In, Out] ref IntPtr phEnum, [Out] uint[] rStrings, [In] uint cmax, [Out] out uint pcStrings);
		void GetParamForMethodIndex([In] uint md, [In] uint ulParamSeq, [Out] out uint ppd);
		void EnumCustomAttributes([In, Out] IntPtr phEnum, [In] uint tk, [In] uint tkType, [Out] uint[] rCustomAttributes, [In] uint cMax, [Out] out uint pcCustomAttributes);
		void GetCustomAttributeProps([In] uint cv, [Out] out uint ptkObj, [Out] out uint ptkType, [Out] out IntPtr ppBlob, [Out] out uint pcbSize);
		void FindTypeRef([In] uint tkResolutionScope, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [Out] out uint ptr);
		void GetMemberProps(uint mb, out uint pClass, IntPtr szMember, uint cchMember, out uint pchMember, out uint pdwAttr, [Out] out IntPtr ppvSigBlob, [Out] out uint pcbSigBlob, [Out] out uint pulCodeRVA, [Out] out uint pdwImplFlags, [Out] out uint pdwCPlusTypeFlag, [Out] out IntPtr ppValue, [Out] out uint pcchValue);
		void GetFieldProps(uint mb, out uint pClass, IntPtr szField, uint cchField, out uint pchField, out uint pdwAttr, [Out] out IntPtr ppvSigBlob, [Out] out uint pcbSigBlob, [Out] out uint pdwCPlusTypeFlag, [Out] out IntPtr ppValue, [Out] out uint pcchValue);
		void GetPropertyProps([In] uint prop, [Out] out uint pClass, [Out] IntPtr szProperty, [In] uint cchProperty, [Out] out uint pchProperty, [Out] out uint pdwPropFlags, [Out] out IntPtr ppvSig, [Out] out uint pbSig, [Out] out uint pdwCPlusTypeFlag, [Out] out IntPtr ppDefaultValue, [Out] out uint pcchDefaultValue, [Out] out uint pmdSetter, [Out] out uint pmdGetter, [In, Out] uint[] rmdOtherMethod, [In] uint cMax, [Out] out uint pcOtherMethod);
		void GetParamProps([In] uint tk, [Out] out uint pmd, [Out] out uint pulSequence, [Out] IntPtr szName, [Out] uint cchName, [Out] out uint pchName, [Out] out uint pdwAttr, [Out] out uint pdwCPlusTypeFlag, [Out] out IntPtr ppValue, [Out] out uint pcchValue);
		void GetCustomAttributeByName([In] uint tkObj, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [Out] out IntPtr ppData, [Out] out uint pcbData);
		bool IsValidToken([In] uint tk);
		unsafe void GetNestedClassProps([In] uint tdNestedClass, [Out] uint* ptdEnclosingClass);
		void GetNativeCallConvFromSig([In] IntPtr pvSig, [In] uint cbSig, [Out] out uint pCallConv);
		void IsGlobal([In] uint pd, [Out] out int pbGlobal);
	}

	[ComVisible(true),
	ComImport,
	Guid("BA3FEE4C-ECB9-4E41-83B7-183FA41CD859"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface IMetaDataEmit {
		void SetModuleProps([In] [MarshalAs(UnmanagedType.LPWStr)] string szName);
		void Save([In] [MarshalAs(UnmanagedType.LPWStr)] string szFile, [In] uint dwSaveFlags);
		void SaveToStream([In] IStream pIStream, [In] uint dwSaveFlags);
		void GetSaveSize([In] int fSave, [Out] out uint pdwSaveSize);
		void DefineTypeDef([In] [MarshalAs(UnmanagedType.LPWStr)] string szTypeDef, [In] uint dwTypeDefFlags, [In] uint tkExtends, [In] uint[] rtkImplements, [Out] out uint ptd);
		void DefineNestedType([In] [MarshalAs(UnmanagedType.LPWStr)] string szTypeDef, [In] uint dwTypeDefFlags, [In] uint tkExtends, [In] uint[] rtkImplements, [In] uint tdEncloser, [Out] out uint ptd);
		void SetHandler([In, MarshalAs(UnmanagedType.IUnknown)] object pUnk);
		void DefineMethod(uint td, [MarshalAs(UnmanagedType.LPWStr)] string szName, uint dwMethodFlags, [In] IntPtr pvSigBlob, [In] uint cbSigBlob, uint ulCodeRVA, uint dwImplFlags, out uint pmd);
		void DefineMethodImpl([In] uint td, [In] uint tkBody, [In] uint tkDecl);
		void DefineTypeRefByName([In] uint tkResolutionScope, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [Out] out uint ptr);
		void DefineImportType([In] IntPtr pAssemImport, [In] IntPtr pbHashValue, [In] uint cbHashValue, [In] IMetaDataImport pImport, [In] uint tdImport, [In] IntPtr pAssemEmit, [Out] out uint ptr);
		void DefineMemberRef([In] uint tkImport, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [In] IntPtr pvSigBlob, [In] uint cbSigBlob, [Out] out uint pmr);
		void DefineImportMember([In] IntPtr pAssemImport, [In] IntPtr pbHashValue, [In] uint cbHashValue, [In] IMetaDataImport pImport, [In] uint mbMember, [In] IntPtr pAssemEmit, [In] uint tkParent, [Out] out uint pmr);
		void DefineEvent([In] uint td, [In] [MarshalAs(UnmanagedType.LPWStr)] string szEvent, [In] uint dwEventFlags, [In] uint tkEventType, [In] uint mdAddOn, [In] uint mdRemoveOn, [In] uint mdFire, [In] uint[] rmdOtherMethods, [Out] out uint pmdEvent);
		void SetClassLayout([In] uint td, [In] uint dwPackSize, [In] IntPtr rFieldOffsets, [In] uint ulClassSize);
		void DeleteClassLayout([In] uint td);
		void SetFieldMarshal([In] uint tk, [In] IntPtr pvNativeType, [In] uint cbNativeType);
		void DeleteFieldMarshal([In] uint tk);
		void DefinePermissionSet([In] uint tk, [In] uint dwAction, [In] IntPtr pvPermission, [In] uint cbPermission, [Out] out uint ppm);
		void SetRVA([In] uint md, [In] uint ulRVA);
		void GetTokenFromSig([In] IntPtr pvSig, [In] uint cbSig, [Out] out uint pmsig);
		void DefineModuleRef([In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [Out] out uint pmur);
		void SetParent([In] uint mr, [In] uint tk);
		void GetTokenFromTypeSpec([In] IntPtr pvSig, [In] uint cbSig, [Out] out uint ptypespec);
		void SaveToMemory([Out] out IntPtr pbData, [In] uint cbData);
		void DefineUserString([In] [MarshalAs(UnmanagedType.LPWStr)] string szString, [In] uint cchString, [Out] out uint pstk);
		void DeleteToken([In] uint tkObj);
		void SetMethodProps([In] uint md, [In] uint dwMethodFlags, [In] uint ulCodeRVA, [In] uint dwImplFlags);
		void SetTypeDefProps([In] uint td, [In] uint dwTypeDefFlags, [In] uint tkExtends, [In] uint[] rtkImplements);
		void SetEventProps([In] uint ev, [In] uint dwEventFlags, [In] uint tkEventType, [In] uint mdAddOn, [In] uint mdRemoveOn, [In] uint mdFire, [In] uint[] rmdOtherMethods);
		void SetPermissionSetProps([In] uint tk, [In] uint dwAction, [In] IntPtr pvPermission, [In] uint cbPermission, [Out] out uint ppm);
		void DefinePinvokeMap([In] uint tk, [In] uint dwMappingFlags, [In] [MarshalAs(UnmanagedType.LPWStr)] string szImportName, [In] uint mrImportDLL);
		void SetPinvokeMap([In] uint tk, [In] uint dwMappingFlags, [In] [MarshalAs(UnmanagedType.LPWStr)] string szImportName, [In] uint mrImportDLL);
		void DeletePinvokeMap([In] uint tk);
		void DefineCustomAttribute([In] uint tkOwner, [In] uint tkCtor, [In] IntPtr pCustomAttribute, [In] uint cbCustomAttribute, [Out] out uint pcv);
		void SetCustomAttributeValue([In] uint pcv, [In] IntPtr pCustomAttribute, [In] uint cbCustomAttribute);
		void DefineField(uint td, [MarshalAs(UnmanagedType.LPWStr)] string szName, uint dwFieldFlags, [In] IntPtr pvSigBlob, [In] uint cbSigBlob, [In] uint dwCPlusTypeFlag, [In] IntPtr pValue, [In] uint cchValue, [Out] out uint pmd);
		void DefineProperty([In] uint td, [In] [MarshalAs(UnmanagedType.LPWStr)] string szProperty, [In] uint dwPropFlags, [In] IntPtr pvSig, [In] uint cbSig, [In] uint dwCPlusTypeFlag, [In] IntPtr pValue, [In] uint cchValue, [In] uint mdSetter, [In] uint mdGetter, [In] uint[] rmdOtherMethods, [Out] out uint pmdProp);
		void DefineParam([In] uint md, [In] uint ulParamSeq, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [In] uint dwParamFlags, [In] uint dwCPlusTypeFlag, [In] IntPtr pValue, [In] uint cchValue, [Out] out uint ppd);
		void SetFieldProps([In] uint fd, [In] uint dwFieldFlags, [In] uint dwCPlusTypeFlag, [In] IntPtr pValue, [In] uint cchValue);
		void SetPropertyProps([In] uint pr, [In] uint dwPropFlags, [In] uint dwCPlusTypeFlag, [In] IntPtr pValue, [In] uint cchValue, [In] uint mdSetter, [In] uint mdGetter, [In] uint[] rmdOtherMethods);
		void SetParamProps([In] uint pd, [In] [MarshalAs(UnmanagedType.LPWStr)] string szName, [In] uint dwParamFlags, [In] uint dwCPlusTypeFlag, [Out] IntPtr pValue, [In] uint cchValue);
		void DefineSecurityAttributeSet([In] uint tkObj, [In] IntPtr rSecAttrs, [In] uint cSecAttrs, [Out] out uint pulErrorAttr);
		void ApplyEditAndContinue([In, MarshalAs(UnmanagedType.IUnknown)] object pImport);
		void TranslateSigWithScope([In] IntPtr pAssemImport, [In] IntPtr pbHashValue, [In] uint cbHashValue, [In] IMetaDataImport import, [In] IntPtr pbSigBlob, [In] uint cbSigBlob, [In] IntPtr pAssemEmit, [In] IMetaDataEmit emit, [Out] IntPtr pvTranslatedSig, uint cbTranslatedSigMax, [Out] out uint pcbTranslatedSig);
		void SetMethodImplFlags([In] uint md, uint dwImplFlags);
		void SetFieldRVA([In] uint fd, [In] uint ulRVA);
		void Merge([In] IMetaDataImport pImport, [In] IntPtr pHostMapToken, [In, MarshalAs(UnmanagedType.IUnknown)] object pHandler);
		void MergeEnd();
	}

	[ComVisible(true),
	ComImport,
	Guid("ED14AA72-78E2-4884-84E2-334293AE5214"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface ISymUnmanagedWriter {
		void DefineDocument([In] [MarshalAs(UnmanagedType.LPWStr)] string url, [In] ref Guid language, [In] ref Guid languageVendor, [In] ref Guid documentType, [Out] out ISymUnmanagedDocumentWriter pRetVal);
		void SetUserEntryPoint([In] uint entryMethod);
		void OpenMethod([In] uint method);
		void CloseMethod();
		void OpenScope([In] uint startOffset, [Out] out uint pRetVal);
		void CloseScope([In] uint endOffset);
		void SetScopeRange([In] uint scopeID, [In] uint startOffset, [In] uint endOffset);
		void DefineLocalVariable([In] [MarshalAs(UnmanagedType.LPWStr)] string name, [In] uint attributes, [In] uint cSig, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] signature, [In] uint addrKind, [In] uint addr1, [In] uint addr2, [In] uint addr3, [In] uint startOffset, [In] uint endOffset);
		void DefineParameter([In] [MarshalAs(UnmanagedType.LPWStr)] string name, [In] uint attributes, [In] uint sequence, [In] uint addrKind, [In] uint addr1, [In] uint addr2, [In] uint addr3);
		void DefineField([In] uint parent, [In] [MarshalAs(UnmanagedType.LPWStr)] string name, [In] uint attributes, [In] uint cSig, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] signature, [In] uint addrKind, [In] uint addr1, [In] uint addr2, [In] uint addr3);
		void DefineGlobalVariable([In] [MarshalAs(UnmanagedType.LPWStr)] string name, [In] uint attributes, [In] uint cSig, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] signature, [In] uint addrKind, [In] uint addr1, [In] uint addr2, [In] uint addr3);
		void Close();
		void SetSymAttribute([In] uint parent, [In] [MarshalAs(UnmanagedType.LPWStr)] string name, [In] uint cData, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] data);
		void OpenNamespace([In] [MarshalAs(UnmanagedType.LPWStr)] string name);
		void CloseNamespace();
		void UsingNamespace([In] [MarshalAs(UnmanagedType.LPWStr)] string fullName);
		void SetMethodSourceRange([In] ISymUnmanagedDocumentWriter startDoc, [In] uint startLine, [In] uint startColumn, [In] ISymUnmanagedDocumentWriter endDoc, [In] uint endLine, [In] uint endColumn);
		void Initialize([In] IntPtr emitter, [In] [MarshalAs(UnmanagedType.LPWStr)] string filename, [In] IStream pIStream, [In] bool fFullBuild);
		void GetDebugInfo([Out] out IMAGE_DEBUG_DIRECTORY pIDD, [In] uint cData, [Out] out uint pcData, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] data);
		void DefineSequencePoints([In] ISymUnmanagedDocumentWriter document, [In] uint spCount, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] offsets, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] lines, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] columns, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] endLines, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] endColumns);
		void RemapToken([In] uint oldToken, [In] uint newToken);
		void Initialize2([In, MarshalAs(UnmanagedType.IUnknown)] object emitter, [In] [MarshalAs(UnmanagedType.LPWStr)] string tempfilename, [In] IStream pIStream, [In] bool fFullBuild, [In] [MarshalAs(UnmanagedType.LPWStr)] string finalfilename);
		void DefineConstant([In] [MarshalAs(UnmanagedType.LPWStr)] string name, [In] object value, [In] uint cSig, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] signature);
		void Abort();
	}

#pragma warning disable 1591
	[ComVisible(true),
	ComImport,
	Guid("0B97726E-9E6D-4F05-9A26-424022093CAA"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface ISymUnmanagedWriter2 {
		void DefineDocument([In] [MarshalAs(UnmanagedType.LPWStr)] string url, [In] ref Guid language, [In] ref Guid languageVendor, [In] ref Guid documentType, [Out] out ISymUnmanagedDocumentWriter pRetVal);
		void SetUserEntryPoint([In] uint entryMethod);
		void OpenMethod([In] uint method);
		void CloseMethod();
		void OpenScope([In] uint startOffset, [Out] out uint pRetVal);
		void CloseScope([In] uint endOffset);
		void SetScopeRange([In] uint scopeID, [In] uint startOffset, [In] uint endOffset);
		void DefineLocalVariable([In] [MarshalAs(UnmanagedType.LPWStr)] string name, [In] uint attributes, [In] uint cSig, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] signature, [In] uint addrKind, [In] uint addr1, [In] uint addr2, [In] uint addr3, [In] uint startOffset, [In] uint endOffset);
		void DefineParameter([In] [MarshalAs(UnmanagedType.LPWStr)] string name, [In] uint attributes, [In] uint sequence, [In] uint addrKind, [In] uint addr1, [In] uint addr2, [In] uint addr3);
		void DefineField([In] uint parent, [In] [MarshalAs(UnmanagedType.LPWStr)] string name, [In] uint attributes, [In] uint cSig, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] signature, [In] uint addrKind, [In] uint addr1, [In] uint addr2, [In] uint addr3);
		void DefineGlobalVariable([In] [MarshalAs(UnmanagedType.LPWStr)] string name, [In] uint attributes, [In] uint cSig, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] signature, [In] uint addrKind, [In] uint addr1, [In] uint addr2, [In] uint addr3);
		void Close();
		void SetSymAttribute([In] uint parent, [In] [MarshalAs(UnmanagedType.LPWStr)] string name, [In] uint cData, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] data);
		void OpenNamespace([In] [MarshalAs(UnmanagedType.LPWStr)] string name);
		void CloseNamespace();
		void UsingNamespace([In] [MarshalAs(UnmanagedType.LPWStr)] string fullName);
		void SetMethodSourceRange([In] ISymUnmanagedDocumentWriter startDoc, [In] uint startLine, [In] uint startColumn, [In] ISymUnmanagedDocumentWriter endDoc, [In] uint endLine, [In] uint endColumn);
		void Initialize([In, MarshalAs(UnmanagedType.IUnknown)] object emitter, [In] [MarshalAs(UnmanagedType.LPWStr)] string filename, [In] IStream pIStream, [In] bool fFullBuild);
		void GetDebugInfo([Out] out IMAGE_DEBUG_DIRECTORY pIDD, [In] uint cData, [Out] out uint pcData, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] data);
		void DefineSequencePoints([In] ISymUnmanagedDocumentWriter document, [In] uint spCount, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] offsets, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] lines, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] columns, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] endLines, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] endColumns);
		void RemapToken([In] uint oldToken, [In] uint newToken);
		void Initialize2([In, MarshalAs(UnmanagedType.IUnknown)] object emitter, [In] [MarshalAs(UnmanagedType.LPWStr)] string tempfilename, [In] IStream pIStream, [In] bool fFullBuild, [In] [MarshalAs(UnmanagedType.LPWStr)] string finalfilename);
		void DefineConstant([In] [MarshalAs(UnmanagedType.LPWStr)] string name, [In] object value, [In] uint cSig, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] signature);
		void Abort();
		void DefineLocalVariable2([In, MarshalAs(UnmanagedType.LPWStr)] string name, [In] uint attributes, [In] uint sigToken, [In] uint addrKind, [In] uint addr1, [In] uint addr2, [In] uint addr3, [In] uint startOffset, [In] uint endOffset);
		void DefineGlobalVariable2([In, MarshalAs(UnmanagedType.LPWStr)] string name, [In] uint attributes, [In] uint sigToken, [In] uint addrKind, [In] uint addr1, [In] uint addr2, [In] uint addr3);
		void DefineConstant2([In, MarshalAs(UnmanagedType.LPWStr)] string name, [In] object value, [In] uint sigToken);
	}
#pragma warning restore 1591

#pragma warning disable 1591
	[ComVisible(true),
	ComImport,
	Guid("B01FAFEB-C450-3A4D-BEEC-B4CEEC01E006"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface ISymUnmanagedDocumentWriter {
		void SetSource([In] uint sourceSize, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] source);
		void SetCheckSum([In] Guid algorithmId, [In] uint checkSumSize, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] checkSum);
	}
#pragma warning restore 1591

	[ComVisible(true),
	ComImport,
	Guid("FC073774-1739-4232-BD56-A027294BEC15"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface ISymUnmanagedAsyncMethodPropertiesWriter {
		void DefineKickoffMethod([In] uint kickoffMethod);
		void DefineCatchHandlerILOffset([In] uint catchHandlerOffset);
		void DefineAsyncStepInfo([In] uint count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] uint[] yieldOffsets, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] uint[] breakpointOffset, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] uint[] breakpointMethod);
	}
}
