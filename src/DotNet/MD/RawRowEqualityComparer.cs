// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;

#pragma warning disable 1591   // XML doc comments                       

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

		public bool Equals(RawModuleRow x, RawModuleRow y) =>
			x.Generation == y.Generation &&
			x.Name == y.Name &&
			x.Mvid == y.Mvid &&
			x.EncId == y.EncId &&
			x.EncBaseId == y.EncBaseId;

		public int GetHashCode(RawModuleRow obj) =>
			HashCodeCombiner.Combine(obj.Generation, obj.Name, obj.Mvid, obj.EncId, obj.EncBaseId);

		public bool Equals(RawTypeRefRow x, RawTypeRefRow y) =>
			x.ResolutionScope == y.ResolutionScope &&
			x.Name == y.Name &&
			x.Namespace == y.Namespace;

		public int GetHashCode(RawTypeRefRow obj) =>
			HashCodeCombiner.Combine(obj.ResolutionScope, obj.Name, obj.Namespace);

		public bool Equals(RawTypeDefRow x, RawTypeDefRow y) =>
			x.Flags == y.Flags &&
			x.Name == y.Name &&
			x.Namespace == y.Namespace &&
			x.Extends == y.Extends &&
			x.FieldList == y.FieldList &&
			x.MethodList == y.MethodList;

		public int GetHashCode(RawTypeDefRow obj) =>
			HashCodeCombiner.Combine(obj.Flags, obj.Name, obj.Namespace, obj.Extends, obj.FieldList, obj.MethodList);

		public bool Equals(RawFieldPtrRow x, RawFieldPtrRow y) => x.Field == y.Field;

		public int GetHashCode(RawFieldPtrRow obj) => (int)obj.Field;

		public bool Equals(RawFieldRow x, RawFieldRow y) =>
			x.Flags == y.Flags &&
			x.Name == y.Name &&
			x.Signature == y.Signature;

		public int GetHashCode(RawFieldRow obj) =>
			HashCodeCombiner.Combine(obj.Flags, obj.Name, obj.Signature);

		public bool Equals(RawMethodPtrRow x, RawMethodPtrRow y) => x.Method == y.Method;

		public int GetHashCode(RawMethodPtrRow obj) => (int)obj.Method;

		public bool Equals(RawMethodRow x, RawMethodRow y) =>
			x.RVA == y.RVA &&
			x.ImplFlags == y.ImplFlags &&
			x.Flags == y.Flags &&
			x.Name == y.Name &&
			x.Signature == y.Signature &&
			x.ParamList == y.ParamList;

		public int GetHashCode(RawMethodRow obj) =>
			HashCodeCombiner.Combine(obj.RVA, obj.ImplFlags, obj.Flags, obj.Name, obj.Signature, obj.ParamList);

		public bool Equals(RawParamPtrRow x, RawParamPtrRow y) => x.Param == y.Param;

		public int GetHashCode(RawParamPtrRow obj) => (int)obj.Param;

		public bool Equals(RawParamRow x, RawParamRow y) =>
			x.Flags == y.Flags &&
			x.Sequence == y.Sequence &&
			x.Name == y.Name;

		public int GetHashCode(RawParamRow obj) =>
			HashCodeCombiner.Combine(obj.Flags, obj.Sequence, obj.Name);

		public bool Equals(RawInterfaceImplRow x, RawInterfaceImplRow y) =>
			x.Class == y.Class &&
			x.Interface == y.Interface;

		public int GetHashCode(RawInterfaceImplRow obj) =>
			HashCodeCombiner.Combine(obj.Class, obj.Interface);

		public bool Equals(RawMemberRefRow x, RawMemberRefRow y) =>
			x.Class == y.Class &&
			x.Name == y.Name &&
			x.Signature == y.Signature;

		public int GetHashCode(RawMemberRefRow obj) =>
			HashCodeCombiner.Combine(obj.Class, obj.Name, obj.Signature);

		public bool Equals(RawConstantRow x, RawConstantRow y) =>
			x.Type == y.Type &&
			x.Padding == y.Padding &&
			x.Parent == y.Parent &&
			x.Value == y.Value;

		public int GetHashCode(RawConstantRow obj) =>
			HashCodeCombiner.Combine(obj.Type, obj.Padding, obj.Parent, obj.Value);

		public bool Equals(RawCustomAttributeRow x, RawCustomAttributeRow y) =>
			x.Parent == y.Parent &&
			x.Type == y.Type &&
			x.Value == y.Value;

		public int GetHashCode(RawCustomAttributeRow obj) =>
			HashCodeCombiner.Combine(obj.Parent, obj.Type, obj.Value);

		public bool Equals(RawFieldMarshalRow x, RawFieldMarshalRow y) =>
			x.Parent == y.Parent &&
			x.NativeType == y.NativeType;

		public int GetHashCode(RawFieldMarshalRow obj) =>
			HashCodeCombiner.Combine(obj.Parent, obj.NativeType);

		public bool Equals(RawDeclSecurityRow x, RawDeclSecurityRow y) =>
			x.Action == y.Action &&
			x.Parent == y.Parent &&
			x.PermissionSet == y.PermissionSet;

		public int GetHashCode(RawDeclSecurityRow obj) =>
			HashCodeCombiner.Combine((ushort)obj.Action, obj.Parent, obj.PermissionSet);

		public bool Equals(RawClassLayoutRow x, RawClassLayoutRow y) =>
			x.PackingSize == y.PackingSize &&
			x.ClassSize == y.ClassSize &&
			x.Parent == y.Parent;

		public int GetHashCode(RawClassLayoutRow obj) =>
			HashCodeCombiner.Combine(obj.PackingSize, obj.ClassSize, obj.Parent);

		public bool Equals(RawFieldLayoutRow x, RawFieldLayoutRow y) =>
			x.OffSet == y.OffSet &&
			x.Field == y.Field;

		public int GetHashCode(RawFieldLayoutRow obj) =>
			HashCodeCombiner.Combine(obj.OffSet, obj.Field);

		public bool Equals(RawStandAloneSigRow x, RawStandAloneSigRow y) => x.Signature == y.Signature;

		public int GetHashCode(RawStandAloneSigRow obj) => (int)obj.Signature;

		public bool Equals(RawEventMapRow x, RawEventMapRow y) =>
			x.Parent == y.Parent &&
			x.EventList == y.EventList;

		public int GetHashCode(RawEventMapRow obj) =>
			HashCodeCombiner.Combine(obj.Parent, obj.EventList);

		public bool Equals(RawEventPtrRow x, RawEventPtrRow y) => x.Event == y.Event;

		public int GetHashCode(RawEventPtrRow obj) => (int)obj.Event;

		public bool Equals(RawEventRow x, RawEventRow y) =>
			x.EventFlags == y.EventFlags &&
			x.Name == y.Name &&
			x.EventType == y.EventType;

		public int GetHashCode(RawEventRow obj) =>
			HashCodeCombiner.Combine(obj.EventFlags, obj.Name, obj.EventType);

		public bool Equals(RawPropertyMapRow x, RawPropertyMapRow y) =>
			x.Parent == y.Parent &&
			x.PropertyList == y.PropertyList;

		public int GetHashCode(RawPropertyMapRow obj) =>
			HashCodeCombiner.Combine(obj.Parent, obj.PropertyList);

		public bool Equals(RawPropertyPtrRow x, RawPropertyPtrRow y) => x.Property == y.Property;

		public int GetHashCode(RawPropertyPtrRow obj) => (int)obj.Property;

		public bool Equals(RawPropertyRow x, RawPropertyRow y) =>
			x.PropFlags == y.PropFlags &&
			x.Name == y.Name &&
			x.Type == y.Type;

		public int GetHashCode(RawPropertyRow obj) =>
			HashCodeCombiner.Combine(obj.PropFlags, obj.Name, obj.Type);

		public bool Equals(RawMethodSemanticsRow x, RawMethodSemanticsRow y) =>
			x.Semantic == y.Semantic &&
			x.Method == y.Method &&
			x.Association == y.Association;

		public int GetHashCode(RawMethodSemanticsRow obj) =>
			HashCodeCombiner.Combine(obj.Semantic, obj.Method, obj.Association);

		public bool Equals(RawMethodImplRow x, RawMethodImplRow y) =>
			x.Class == y.Class &&
			x.MethodBody == y.MethodBody &&
			x.MethodDeclaration == y.MethodDeclaration;

		public int GetHashCode(RawMethodImplRow obj) =>
			HashCodeCombiner.Combine(obj.Class, obj.MethodBody, obj.MethodDeclaration);

		public bool Equals(RawModuleRefRow x, RawModuleRefRow y) => x.Name == y.Name;

		public int GetHashCode(RawModuleRefRow obj) => (int)obj.Name;

		public bool Equals(RawTypeSpecRow x, RawTypeSpecRow y) => x.Signature == y.Signature;

		public int GetHashCode(RawTypeSpecRow obj) => (int)obj.Signature;

		public bool Equals(RawImplMapRow x, RawImplMapRow y) =>
			x.MappingFlags == y.MappingFlags &&
			x.MemberForwarded == y.MemberForwarded &&
			x.ImportName == y.ImportName &&
			x.ImportScope == y.ImportScope;

		public int GetHashCode(RawImplMapRow obj) =>
			HashCodeCombiner.Combine(obj.MappingFlags, obj.MemberForwarded, obj.ImportName, obj.ImportScope);

		public bool Equals(RawFieldRVARow x, RawFieldRVARow y) =>
			x.RVA == y.RVA &&
			x.Field == y.Field;

		public int GetHashCode(RawFieldRVARow obj) =>
			HashCodeCombiner.Combine(obj.RVA, obj.Field);

		public bool Equals(RawENCLogRow x, RawENCLogRow y) =>
			x.Token == y.Token &&
			x.FuncCode == y.FuncCode;

		public int GetHashCode(RawENCLogRow obj) =>
			HashCodeCombiner.Combine(obj.Token, obj.FuncCode);

		public bool Equals(RawENCMapRow x, RawENCMapRow y) => x.Token == y.Token;

		public int GetHashCode(RawENCMapRow obj) => (int)obj.Token;

		public bool Equals(RawAssemblyRow x, RawAssemblyRow y) =>
			x.HashAlgId == y.HashAlgId &&
			x.MajorVersion == y.MajorVersion &&
			x.MinorVersion == y.MinorVersion &&
			x.BuildNumber == y.BuildNumber &&
			x.RevisionNumber == y.RevisionNumber &&
			x.Flags == y.Flags &&
			x.PublicKey == y.PublicKey &&
			x.Name == y.Name &&
			x.Locale == y.Locale;

		public int GetHashCode(RawAssemblyRow obj) =>
			HashCodeCombiner.Combine(obj.HashAlgId, obj.MajorVersion, obj.MinorVersion, obj.BuildNumber, obj.RevisionNumber, obj.Flags, obj.PublicKey, obj.Name, obj.Locale);

		public bool Equals(RawAssemblyProcessorRow x, RawAssemblyProcessorRow y) => x.Processor == y.Processor;

		public int GetHashCode(RawAssemblyProcessorRow obj) => (int)obj.Processor;

		public bool Equals(RawAssemblyOSRow x, RawAssemblyOSRow y) =>
			x.OSPlatformId == y.OSPlatformId &&
			x.OSMajorVersion == y.OSMajorVersion &&
			x.OSMinorVersion == y.OSMinorVersion;

		public int GetHashCode(RawAssemblyOSRow obj) =>
			HashCodeCombiner.Combine(obj.OSPlatformId, obj.OSMajorVersion, obj.OSMinorVersion);

		public bool Equals(RawAssemblyRefRow x, RawAssemblyRefRow y) =>
			x.MajorVersion == y.MajorVersion &&
			x.MinorVersion == y.MinorVersion &&
			x.BuildNumber == y.BuildNumber &&
			x.RevisionNumber == y.RevisionNumber &&
			x.Flags == y.Flags &&
			x.PublicKeyOrToken == y.PublicKeyOrToken &&
			x.Name == y.Name &&
			x.Locale == y.Locale &&
			x.HashValue == y.HashValue;

		public int GetHashCode(RawAssemblyRefRow obj) =>
			HashCodeCombiner.Combine(obj.MajorVersion, obj.MinorVersion, obj.BuildNumber, obj.RevisionNumber, obj.Flags, obj.PublicKeyOrToken, obj.Name, obj.Locale, obj.HashValue);

		public bool Equals(RawAssemblyRefProcessorRow x, RawAssemblyRefProcessorRow y) =>
			x.Processor == y.Processor &&
			x.AssemblyRef == y.AssemblyRef;

		public int GetHashCode(RawAssemblyRefProcessorRow obj) =>
			HashCodeCombiner.Combine(obj.Processor, obj.AssemblyRef);

		public bool Equals(RawAssemblyRefOSRow x, RawAssemblyRefOSRow y) =>
			x.OSPlatformId == y.OSPlatformId &&
			x.OSMajorVersion == y.OSMajorVersion &&
			x.OSMinorVersion == y.OSMinorVersion &&
			x.AssemblyRef == y.AssemblyRef;

		public int GetHashCode(RawAssemblyRefOSRow obj) =>
			HashCodeCombiner.Combine(obj.OSPlatformId, obj.OSMajorVersion, obj.OSMinorVersion, obj.AssemblyRef);

		public bool Equals(RawFileRow x, RawFileRow y) =>
			x.Flags == y.Flags &&
			x.Name == y.Name &&
			x.HashValue == y.HashValue;

		public int GetHashCode(RawFileRow obj) =>
			HashCodeCombiner.Combine(obj.Flags, obj.Name, obj.HashValue);

		public bool Equals(RawExportedTypeRow x, RawExportedTypeRow y) =>
			x.Flags == y.Flags &&
			x.TypeDefId == y.TypeDefId &&
			x.TypeName == y.TypeName &&
			x.TypeNamespace == y.TypeNamespace &&
			x.Implementation == y.Implementation;

		public int GetHashCode(RawExportedTypeRow obj) =>
			HashCodeCombiner.Combine(obj.Flags, obj.TypeDefId, obj.TypeName, obj.TypeNamespace, obj.Implementation);

		public bool Equals(RawManifestResourceRow x, RawManifestResourceRow y) =>
			x.Offset == y.Offset &&
			x.Flags == y.Flags &&
			x.Name == y.Name &&
			x.Implementation == y.Implementation;

		public int GetHashCode(RawManifestResourceRow obj) =>
			HashCodeCombiner.Combine(obj.Offset, obj.Flags, obj.Name, obj.Implementation);

		public bool Equals(RawNestedClassRow x, RawNestedClassRow y) =>
			x.NestedClass == y.NestedClass &&
			x.EnclosingClass == y.EnclosingClass;

		public int GetHashCode(RawNestedClassRow obj) =>
			HashCodeCombiner.Combine(obj.NestedClass, obj.EnclosingClass);

		public bool Equals(RawGenericParamRow x, RawGenericParamRow y) =>
			x.Number == y.Number &&
			x.Flags == y.Flags &&
			x.Owner == y.Owner &&
			x.Name == y.Name &&
			x.Kind == y.Kind;

		public int GetHashCode(RawGenericParamRow obj) =>
			HashCodeCombiner.Combine(obj.Number, obj.Flags, obj.Owner, obj.Name, obj.Kind);

		public bool Equals(RawMethodSpecRow x, RawMethodSpecRow y) =>
			x.Method == y.Method &&
			x.Instantiation == y.Instantiation;

		public int GetHashCode(RawMethodSpecRow obj) =>
			HashCodeCombiner.Combine(obj.Method, obj.Instantiation);

		public bool Equals(RawGenericParamConstraintRow x, RawGenericParamConstraintRow y) =>
			x.Owner == y.Owner &&
			x.Constraint == y.Constraint;

		public int GetHashCode(RawGenericParamConstraintRow obj) =>
			HashCodeCombiner.Combine(obj.Owner, obj.Constraint);

		public bool Equals(RawDocumentRow x, RawDocumentRow y) =>
			x.Name == y.Name &&
			x.HashAlgorithm == y.HashAlgorithm &&
			x.Hash == y.Hash &&
			x.Language == y.Language;

		public int GetHashCode(RawDocumentRow obj) =>
			HashCodeCombiner.Combine(obj.Name, obj.HashAlgorithm, obj.Hash, obj.Language);

		public bool Equals(RawMethodDebugInformationRow x, RawMethodDebugInformationRow y) =>
			x.Document == y.Document &&
			x.SequencePoints == y.SequencePoints;

		public int GetHashCode(RawMethodDebugInformationRow obj) =>
			HashCodeCombiner.Combine(obj.Document, obj.SequencePoints);

		public bool Equals(RawLocalScopeRow x, RawLocalScopeRow y) =>
			x.Method == y.Method &&
			x.ImportScope == y.ImportScope &&
			x.VariableList == y.VariableList &&
			x.ConstantList == y.ConstantList &&
			x.StartOffset == y.StartOffset &&
			x.Length == y.Length;

		public int GetHashCode(RawLocalScopeRow obj) =>
			HashCodeCombiner.Combine(obj.Method, obj.ImportScope, obj.VariableList, obj.ConstantList, obj.StartOffset, obj.Length);

		public bool Equals(RawLocalVariableRow x, RawLocalVariableRow y) =>
			x.Attributes == y.Attributes &&
			x.Index == y.Index &&
			x.Name == y.Name;

		public int GetHashCode(RawLocalVariableRow obj) =>
			HashCodeCombiner.Combine(obj.Attributes, obj.Index, obj.Name);

		public bool Equals(RawLocalConstantRow x, RawLocalConstantRow y) =>
			x.Name == y.Name &&
			x.Signature == y.Signature;

		public int GetHashCode(RawLocalConstantRow obj) =>
			HashCodeCombiner.Combine(obj.Name, obj.Signature);

		public bool Equals(RawImportScopeRow x, RawImportScopeRow y) =>
			x.Parent == y.Parent &&
			x.Imports == y.Imports;

		public int GetHashCode(RawImportScopeRow obj) =>
			HashCodeCombiner.Combine(obj.Parent, obj.Imports);

		public bool Equals(RawStateMachineMethodRow x, RawStateMachineMethodRow y) =>
			x.MoveNextMethod == y.MoveNextMethod &&
			x.KickoffMethod == y.KickoffMethod;

		public int GetHashCode(RawStateMachineMethodRow obj) =>
			HashCodeCombiner.Combine(obj.MoveNextMethod, obj.KickoffMethod);

		public bool Equals(RawCustomDebugInformationRow x, RawCustomDebugInformationRow y) =>
			x.Parent == y.Parent &&
			x.Kind == y.Kind &&
			x.Value == y.Value;

		public int GetHashCode(RawCustomDebugInformationRow obj) =>
			HashCodeCombiner.Combine(obj.Parent, obj.Kind, obj.Value);
	}
}
