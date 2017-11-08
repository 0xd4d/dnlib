// dnlib: See LICENSE.txt for more info

﻿using System.Collections.Generic;

#pragma warning disable 1591	// XML doc comments

namespace dnlib.DotNet.MD {
	/// <summary>
	/// Equality comparer for all raw rows
	/// </summary>
	public sealed class RawRowEqualityComparer : IEqualityComparer<RawModuleRow>,
		IEqualityComparer<RawTypeRefRow>, IEqualityComparer<RawTypeDefRow>,
		IEqualityComparer<RawFieldPtrRow>, IEqualityComparer<RawFieldRow>,
		IEqualityComparer<RawMethodPtrRow>, IEqualityComparer<RawMethodRow>,
		IEqualityComparer<RawParamPtrRow>, IEqualityComparer<RawParamRow>,
		IEqualityComparer<RawInterfaceImplRow>, IEqualityComparer<RawMemberRefRow>,
		IEqualityComparer<RawConstantRow>, IEqualityComparer<RawCustomAttributeRow>,
		IEqualityComparer<RawFieldMarshalRow>, IEqualityComparer<RawDeclSecurityRow>,
		IEqualityComparer<RawClassLayoutRow>, IEqualityComparer<RawFieldLayoutRow>,
		IEqualityComparer<RawStandAloneSigRow>, IEqualityComparer<RawEventMapRow>,
		IEqualityComparer<RawEventPtrRow>, IEqualityComparer<RawEventRow>,
		IEqualityComparer<RawPropertyMapRow>, IEqualityComparer<RawPropertyPtrRow>,
		IEqualityComparer<RawPropertyRow>, IEqualityComparer<RawMethodSemanticsRow>,
		IEqualityComparer<RawMethodImplRow>, IEqualityComparer<RawModuleRefRow>,
		IEqualityComparer<RawTypeSpecRow>, IEqualityComparer<RawImplMapRow>,
		IEqualityComparer<RawFieldRVARow>, IEqualityComparer<RawENCLogRow>,
		IEqualityComparer<RawENCMapRow>, IEqualityComparer<RawAssemblyRow>,
		IEqualityComparer<RawAssemblyProcessorRow>, IEqualityComparer<RawAssemblyOSRow>,
		IEqualityComparer<RawAssemblyRefRow>, IEqualityComparer<RawAssemblyRefProcessorRow>,
		IEqualityComparer<RawAssemblyRefOSRow>, IEqualityComparer<RawFileRow>,
		IEqualityComparer<RawExportedTypeRow>, IEqualityComparer<RawManifestResourceRow>,
		IEqualityComparer<RawNestedClassRow>, IEqualityComparer<RawGenericParamRow>,
		IEqualityComparer<RawMethodSpecRow>, IEqualityComparer<RawGenericParamConstraintRow>,
		IEqualityComparer<RawDocumentRow>, IEqualityComparer<RawMethodDebugInformationRow>,
		IEqualityComparer<RawLocalScopeRow>, IEqualityComparer<RawLocalVariableRow>,
		IEqualityComparer<RawLocalConstantRow>, IEqualityComparer<RawImportScopeRow>,
		IEqualityComparer<RawStateMachineMethodRow>, IEqualityComparer<RawCustomDebugInformationRow> {

		/// <summary>
		/// Default instance
		/// </summary>
		public static readonly RawRowEqualityComparer Instance = new RawRowEqualityComparer();

		static int rol(uint val, int shift) {
			return (int)((val << shift) | (val >> (32 - shift)));
		}

		public bool Equals(RawModuleRow x, RawModuleRow y) {
			return x.Generation == y.Generation &&
				x.Name == y.Name &&
				x.Mvid == y.Mvid &&
				x.EncId == y.EncId &&
				x.EncBaseId == y.EncBaseId;
		}

		public int GetHashCode(RawModuleRow obj) {
			return obj.Generation +
				rol(obj.Name, 3) +
				rol(obj.Mvid, 7) +
				rol(obj.EncId, 11) +
				rol(obj.EncBaseId, 15);
		}

		public bool Equals(RawTypeRefRow x, RawTypeRefRow y) {
			return x.ResolutionScope == y.ResolutionScope &&
				x.Name == y.Name &&
				x.Namespace == y.Namespace;
		}

		public int GetHashCode(RawTypeRefRow obj) {
			return (int)obj.ResolutionScope +
				rol(obj.Name, 3) +
				rol(obj.Namespace, 7);
		}

		public bool Equals(RawTypeDefRow x, RawTypeDefRow y) {
			return x.Flags == y.Flags &&
				x.Name == y.Name &&
				x.Namespace == y.Namespace &&
				x.Extends == y.Extends &&
				x.FieldList == y.FieldList &&
				x.MethodList == y.MethodList;
		}

		public int GetHashCode(RawTypeDefRow obj) {
			return (int)obj.Flags +
				rol(obj.Name, 3) +
				rol(obj.Namespace, 7) +
				rol(obj.Extends, 11) +
				rol(obj.FieldList, 15) +
				rol(obj.MethodList, 19);
		}

		public bool Equals(RawFieldPtrRow x, RawFieldPtrRow y) {
			return x.Field == y.Field;
		}

		public int GetHashCode(RawFieldPtrRow obj) {
			return (int)obj.Field;
		}

		public bool Equals(RawFieldRow x, RawFieldRow y) {
			return x.Flags == y.Flags &&
				x.Name == y.Name &&
				x.Signature == y.Signature;
		}

		public int GetHashCode(RawFieldRow obj) {
			return (int)obj.Flags +
				rol(obj.Name, 3) +
				rol(obj.Signature, 7);
		}

		public bool Equals(RawMethodPtrRow x, RawMethodPtrRow y) {
			return x.Method == y.Method;
		}

		public int GetHashCode(RawMethodPtrRow obj) {
			return (int)obj.Method;
		}

		public bool Equals(RawMethodRow x, RawMethodRow y) {
			return x.RVA == y.RVA &&
				x.ImplFlags == y.ImplFlags &&
				x.Flags == y.Flags &&
				x.Name == y.Name &&
				x.Signature == y.Signature &&
				x.ParamList == y.ParamList;
		}

		public int GetHashCode(RawMethodRow obj) {
			return (int)obj.RVA +
				rol(obj.ImplFlags, 3) +
				rol(obj.Flags, 7) +
				rol(obj.Name, 11) +
				rol(obj.Signature, 15) +
				rol(obj.ParamList, 19);
		}

		public bool Equals(RawParamPtrRow x, RawParamPtrRow y) {
			return x.Param == y.Param;
		}

		public int GetHashCode(RawParamPtrRow obj) {
			return (int)obj.Param;
		}

		public bool Equals(RawParamRow x, RawParamRow y) {
			return x.Flags == y.Flags &&
				x.Sequence == y.Sequence &&
				x.Name == y.Name;
		}

		public int GetHashCode(RawParamRow obj) {
			return (int)obj.Flags +
				rol(obj.Sequence, 3) +
				rol(obj.Name, 7);
		}

		public bool Equals(RawInterfaceImplRow x, RawInterfaceImplRow y) {
			return x.Class == y.Class &&
				x.Interface == y.Interface;
		}

		public int GetHashCode(RawInterfaceImplRow obj) {
			return (int)obj.Class +
				rol(obj.Interface, 3);
		}

		public bool Equals(RawMemberRefRow x, RawMemberRefRow y) {
			return x.Class == y.Class &&
				x.Name == y.Name &&
				x.Signature == y.Signature;
		}

		public int GetHashCode(RawMemberRefRow obj) {
			return (int)obj.Class +
				rol(obj.Name, 3) +
				rol(obj.Signature, 7);
		}

		public bool Equals(RawConstantRow x, RawConstantRow y) {
			return x.Type == y.Type &&
				x.Padding == y.Padding &&
				x.Parent == y.Parent &&
				x.Value == y.Value;
		}

		public int GetHashCode(RawConstantRow obj) {
			return (int)obj.Type +
				rol(obj.Padding, 3) +
				rol(obj.Parent, 7) +
				rol(obj.Value, 11);
		}

		public bool Equals(RawCustomAttributeRow x, RawCustomAttributeRow y) {
			return x.Parent == y.Parent &&
				x.Type == y.Type &&
				x.Value == y.Value;
		}

		public int GetHashCode(RawCustomAttributeRow obj) {
			return (int)obj.Parent +
				rol(obj.Type, 3) +
				rol(obj.Value, 7);
		}

		public bool Equals(RawFieldMarshalRow x, RawFieldMarshalRow y) {
			return x.Parent == y.Parent &&
				x.NativeType == y.NativeType;
		}

		public int GetHashCode(RawFieldMarshalRow obj) {
			return (int)obj.Parent +
				rol(obj.NativeType, 3);
		}

		public bool Equals(RawDeclSecurityRow x, RawDeclSecurityRow y) {
			return x.Action == y.Action &&
				x.Parent == y.Parent &&
				x.PermissionSet == y.PermissionSet;
		}

		public int GetHashCode(RawDeclSecurityRow obj) {
			return (int)obj.Action +
				rol(obj.Parent, 3) +
				rol(obj.PermissionSet, 7);
		}

		public bool Equals(RawClassLayoutRow x, RawClassLayoutRow y) {
			return x.PackingSize == y.PackingSize &&
				x.ClassSize == y.ClassSize &&
				x.Parent == y.Parent;
		}

		public int GetHashCode(RawClassLayoutRow obj) {
			return (int)obj.PackingSize +
				rol(obj.ClassSize, 3) +
				rol(obj.Parent, 7);
		}

		public bool Equals(RawFieldLayoutRow x, RawFieldLayoutRow y) {
			return x.OffSet == y.OffSet &&
				x.Field == y.Field;
		}

		public int GetHashCode(RawFieldLayoutRow obj) {
			return (int)obj.OffSet +
				rol(obj.Field, 3);
		}

		public bool Equals(RawStandAloneSigRow x, RawStandAloneSigRow y) {
			return x.Signature == y.Signature;
		}

		public int GetHashCode(RawStandAloneSigRow obj) {
			return (int)obj.Signature;
		}

		public bool Equals(RawEventMapRow x, RawEventMapRow y) {
			return x.Parent == y.Parent &&
				x.EventList == y.EventList;
		}

		public int GetHashCode(RawEventMapRow obj) {
			return (int)obj.Parent +
				rol(obj.EventList, 3);
		}

		public bool Equals(RawEventPtrRow x, RawEventPtrRow y) {
			return x.Event == y.Event;
		}

		public int GetHashCode(RawEventPtrRow obj) {
			return (int)obj.Event;
		}

		public bool Equals(RawEventRow x, RawEventRow y) {
			return x.EventFlags == y.EventFlags &&
				x.Name == y.Name &&
				x.EventType == y.EventType;
		}

		public int GetHashCode(RawEventRow obj) {
			return (int)obj.EventFlags +
				rol(obj.Name, 3) +
				rol(obj.EventType, 7);
		}

		public bool Equals(RawPropertyMapRow x, RawPropertyMapRow y) {
			return x.Parent == y.Parent &&
				x.PropertyList == y.PropertyList;
		}

		public int GetHashCode(RawPropertyMapRow obj) {
			return (int)obj.Parent +
				rol(obj.PropertyList, 3);
		}

		public bool Equals(RawPropertyPtrRow x, RawPropertyPtrRow y) {
			return x.Property == y.Property;
		}

		public int GetHashCode(RawPropertyPtrRow obj) {
			return (int)obj.Property;
		}

		public bool Equals(RawPropertyRow x, RawPropertyRow y) {
			return x.PropFlags == y.PropFlags &&
				x.Name == y.Name &&
				x.Type == y.Type;
		}

		public int GetHashCode(RawPropertyRow obj) {
			return (int)obj.PropFlags +
				rol(obj.Name, 3) +
				rol(obj.Type, 7);
		}

		public bool Equals(RawMethodSemanticsRow x, RawMethodSemanticsRow y) {
			return x.Semantic == y.Semantic &&
				x.Method == y.Method &&
				x.Association == y.Association;
		}

		public int GetHashCode(RawMethodSemanticsRow obj) {
			return (int)obj.Semantic +
				rol(obj.Method, 3) +
				rol(obj.Association, 7);
		}

		public bool Equals(RawMethodImplRow x, RawMethodImplRow y) {
			return x.Class == y.Class &&
				x.MethodBody == y.MethodBody &&
				x.MethodDeclaration == y.MethodDeclaration;
		}

		public int GetHashCode(RawMethodImplRow obj) {
			return (int)obj.Class +
				rol(obj.MethodBody, 3) +
				rol(obj.MethodDeclaration, 7);
		}

		public bool Equals(RawModuleRefRow x, RawModuleRefRow y) {
			return x.Name == y.Name;
		}

		public int GetHashCode(RawModuleRefRow obj) {
			return (int)obj.Name;
		}

		public bool Equals(RawTypeSpecRow x, RawTypeSpecRow y) {
			return x.Signature == y.Signature;
		}

		public int GetHashCode(RawTypeSpecRow obj) {
			return (int)obj.Signature;
		}

		public bool Equals(RawImplMapRow x, RawImplMapRow y) {
			return x.MappingFlags == y.MappingFlags &&
				x.MemberForwarded == y.MemberForwarded &&
				x.ImportName == y.ImportName &&
				x.ImportScope == y.ImportScope;
		}

		public int GetHashCode(RawImplMapRow obj) {
			return (int)obj.MappingFlags +
				rol(obj.MemberForwarded, 3) +
				rol(obj.ImportName, 7) +
				rol(obj.ImportScope, 11);
		}

		public bool Equals(RawFieldRVARow x, RawFieldRVARow y) {
			return x.RVA == y.RVA &&
				x.Field == y.Field;
		}

		public int GetHashCode(RawFieldRVARow obj) {
			return (int)obj.RVA +
				rol(obj.Field, 3);
		}

		public bool Equals(RawENCLogRow x, RawENCLogRow y) {
			return x.Token == y.Token &&
				x.FuncCode == y.FuncCode;
		}

		public int GetHashCode(RawENCLogRow obj) {
			return (int)obj.Token +
				rol(obj.FuncCode, 3);
		}

		public bool Equals(RawENCMapRow x, RawENCMapRow y) {
			return x.Token == y.Token;
		}

		public int GetHashCode(RawENCMapRow obj) {
			return (int)obj.Token;
		}

		public bool Equals(RawAssemblyRow x, RawAssemblyRow y) {
			return x.HashAlgId == y.HashAlgId &&
				x.MajorVersion == y.MajorVersion &&
				x.MinorVersion == y.MinorVersion &&
				x.BuildNumber == y.BuildNumber &&
				x.RevisionNumber == y.RevisionNumber &&
				x.Flags == y.Flags &&
				x.PublicKey == y.PublicKey &&
				x.Name == y.Name &&
				x.Locale == y.Locale;
		}

		public int GetHashCode(RawAssemblyRow obj) {
			return (int)obj.HashAlgId +
				rol(obj.MajorVersion, 3) +
				rol(obj.MinorVersion, 7) +
				rol(obj.BuildNumber, 11) +
				rol(obj.RevisionNumber, 15) +
				rol(obj.Flags, 19) +
				rol(obj.PublicKey, 23) +
				rol(obj.Name, 27) +
				rol(obj.Locale, 31);
		}

		public bool Equals(RawAssemblyProcessorRow x, RawAssemblyProcessorRow y) {
			return x.Processor == y.Processor;
		}

		public int GetHashCode(RawAssemblyProcessorRow obj) {
			return (int)obj.Processor;
		}

		public bool Equals(RawAssemblyOSRow x, RawAssemblyOSRow y) {
			return x.OSPlatformId == y.OSPlatformId &&
				x.OSMajorVersion == y.OSMajorVersion &&
				x.OSMinorVersion == y.OSMinorVersion;
		}

		public int GetHashCode(RawAssemblyOSRow obj) {
			return (int)obj.OSPlatformId +
				rol(obj.OSMajorVersion, 3) +
				rol(obj.OSMinorVersion, 7);
		}

		public bool Equals(RawAssemblyRefRow x, RawAssemblyRefRow y) {
			return x.MajorVersion == y.MajorVersion &&
				x.MinorVersion == y.MinorVersion &&
				x.BuildNumber == y.BuildNumber &&
				x.RevisionNumber == y.RevisionNumber &&
				x.Flags == y.Flags &&
				x.PublicKeyOrToken == y.PublicKeyOrToken &&
				x.Name == y.Name &&
				x.Locale == y.Locale &&
				x.HashValue == y.HashValue;
		}

		public int GetHashCode(RawAssemblyRefRow obj) {
			return (int)obj.MajorVersion +
				rol(obj.MinorVersion, 3) +
				rol(obj.BuildNumber, 7) +
				rol(obj.RevisionNumber, 11) +
				rol(obj.Flags, 15) +
				rol(obj.PublicKeyOrToken, 19) +
				rol(obj.Name, 23) +
				rol(obj.Locale, 27) +
				rol(obj.HashValue, 31);
		}

		public bool Equals(RawAssemblyRefProcessorRow x, RawAssemblyRefProcessorRow y) {
			return x.Processor == y.Processor &&
				x.AssemblyRef == y.AssemblyRef;
		}

		public int GetHashCode(RawAssemblyRefProcessorRow obj) {
			return (int)obj.Processor +
				rol(obj.AssemblyRef, 3);
		}

		public bool Equals(RawAssemblyRefOSRow x, RawAssemblyRefOSRow y) {
			return x.OSPlatformId == y.OSPlatformId &&
				x.OSMajorVersion == y.OSMajorVersion &&
				x.OSMinorVersion == y.OSMinorVersion &&
				x.AssemblyRef == y.AssemblyRef;
		}

		public int GetHashCode(RawAssemblyRefOSRow obj) {
			return (int)obj.OSPlatformId +
				rol(obj.OSMajorVersion, 3) +
				rol(obj.OSMinorVersion, 7) +
				rol(obj.AssemblyRef, 11);
		}

		public bool Equals(RawFileRow x, RawFileRow y) {
			return x.Flags == y.Flags &&
				x.Name == y.Name &&
				x.HashValue == y.HashValue;
		}

		public int GetHashCode(RawFileRow obj) {
			return (int)obj.Flags +
				rol(obj.Name, 3) +
				rol(obj.HashValue, 7);
		}

		public bool Equals(RawExportedTypeRow x, RawExportedTypeRow y) {
			return x.Flags == y.Flags &&
				x.TypeDefId == y.TypeDefId &&
				x.TypeName == y.TypeName &&
				x.TypeNamespace == y.TypeNamespace &&
				x.Implementation == y.Implementation;
		}

		public int GetHashCode(RawExportedTypeRow obj) {
			return (int)obj.Flags +
				rol(obj.TypeDefId, 3) +
				rol(obj.TypeName, 7) +
				rol(obj.TypeNamespace, 11) +
				rol(obj.Implementation, 15);
		}

		public bool Equals(RawManifestResourceRow x, RawManifestResourceRow y) {
			return x.Offset == y.Offset &&
				x.Flags == y.Flags &&
				x.Name == y.Name &&
				x.Implementation == y.Implementation;
		}

		public int GetHashCode(RawManifestResourceRow obj) {
			return (int)obj.Offset +
				rol(obj.Flags, 3) +
				rol(obj.Name, 7) +
				rol(obj.Implementation, 11);
		}

		public bool Equals(RawNestedClassRow x, RawNestedClassRow y) {
			return x.NestedClass == y.NestedClass &&
				x.EnclosingClass == y.EnclosingClass;
		}

		public int GetHashCode(RawNestedClassRow obj) {
			return (int)obj.NestedClass +
				rol(obj.EnclosingClass, 3);
		}

		public bool Equals(RawGenericParamRow x, RawGenericParamRow y) {
			return x.Number == y.Number &&
				x.Flags == y.Flags &&
				x.Owner == y.Owner &&
				x.Name == y.Name &&
				x.Kind == y.Kind;
		}

		public int GetHashCode(RawGenericParamRow obj) {
			return (int)obj.Number +
				rol(obj.Flags, 3) +
				rol(obj.Owner, 7) +
				rol(obj.Name, 11) +
				rol(obj.Kind, 15);
		}

		public bool Equals(RawMethodSpecRow x, RawMethodSpecRow y) {
			return x.Method == y.Method &&
				x.Instantiation == y.Instantiation;
		}

		public int GetHashCode(RawMethodSpecRow obj) {
			return (int)obj.Method +
				rol(obj.Instantiation, 3);
		}

		public bool Equals(RawGenericParamConstraintRow x, RawGenericParamConstraintRow y) {
			return x.Owner == y.Owner &&
				x.Constraint == y.Constraint;
		}

		public int GetHashCode(RawGenericParamConstraintRow obj) {
			return (int)obj.Owner +
				rol(obj.Constraint, 3);
		}

		public bool Equals(RawDocumentRow x, RawDocumentRow y) {
			return x.Name == y.Name &&
				x.HashAlgorithm == y.HashAlgorithm &&
				x.Hash == y.Hash &&
				x.Language == y.Language;
		}

		public int GetHashCode(RawDocumentRow obj) {
			return (int)obj.Name +
				rol(obj.HashAlgorithm, 3) +
				rol(obj.Hash, 7) +
				rol(obj.Language, 11);
		}

		public bool Equals(RawMethodDebugInformationRow x, RawMethodDebugInformationRow y) {
			return x.Document == y.Document &&
				x.SequencePoints == y.SequencePoints;
		}

		public int GetHashCode(RawMethodDebugInformationRow obj) {
			return (int)obj.Document +
				rol(obj.SequencePoints, 3);
		}

		public bool Equals(RawLocalScopeRow x, RawLocalScopeRow y) {
			return x.Method == y.Method &&
				x.ImportScope == y.ImportScope &&
				x.VariableList == y.VariableList &&
				x.ConstantList == y.ConstantList &&
				x.StartOffset == y.StartOffset &&
				x.Length == y.Length;
		}

		public int GetHashCode(RawLocalScopeRow obj) {
			return (int)obj.Method +
				rol(obj.ImportScope, 3) +
				rol(obj.VariableList, 7) +
				rol(obj.ConstantList, 11) +
				rol(obj.StartOffset, 15) +
				rol(obj.Length, 19);
		}

		public bool Equals(RawLocalVariableRow x, RawLocalVariableRow y) {
			return x.Attributes == y.Attributes &&
				x.Index == y.Index &&
				x.Name == y.Name;
		}

		public int GetHashCode(RawLocalVariableRow obj) {
			return obj.Attributes +
				rol(obj.Index, 3) +
				rol(obj.Name, 7);
		}

		public bool Equals(RawLocalConstantRow x, RawLocalConstantRow y) {
			return x.Name == y.Name &&
				x.Signature == y.Signature;
		}

		public int GetHashCode(RawLocalConstantRow obj) {
			return (int)obj.Name +
				rol(obj.Signature, 3);
		}

		public bool Equals(RawImportScopeRow x, RawImportScopeRow y) {
			return x.Parent == y.Parent &&
				x.Imports == y.Imports;
		}

		public int GetHashCode(RawImportScopeRow obj) {
			return (int)obj.Parent +
				rol(obj.Imports, 3);
		}

		public bool Equals(RawStateMachineMethodRow x, RawStateMachineMethodRow y) {
			return x.MoveNextMethod == y.MoveNextMethod &&
				x.KickoffMethod == y.KickoffMethod;
		}

		public int GetHashCode(RawStateMachineMethodRow obj) {
			return (int)obj.MoveNextMethod +
				rol(obj.KickoffMethod, 3);
		}

		public bool Equals(RawCustomDebugInformationRow x, RawCustomDebugInformationRow y) {
			return x.Parent == y.Parent &&
				x.Kind == y.Kind &&
				x.Value == y.Value;
		}

		public int GetHashCode(RawCustomDebugInformationRow obj) {
			return (int)obj.Parent +
				rol(obj.Kind, 3) +
				rol(obj.Value, 7);
		}
	}
}
