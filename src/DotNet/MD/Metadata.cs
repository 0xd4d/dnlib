// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using dnlib.PE;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// Reads .NET metadata
	/// </summary>
	public abstract class Metadata : IDisposable {
		/// <summary>
		/// <c>true</c> if the compressed (normal) metadata is used, <c>false</c> if the non-compressed
		/// (Edit N' Continue) metadata is used. This can be <c>false</c> even if the table stream
		/// is <c>#~</c> but that's very uncommon.
		/// </summary>
		public abstract bool IsCompressed { get; }

		/// <summary>
		/// <c>true</c> if this is standalone Portable PDB metadata
		/// </summary>
		public abstract bool IsStandalonePortablePdb { get; }

		/// <summary>
		/// Gets the .NET header
		/// </summary>
		public abstract ImageCor20Header ImageCor20Header { get; }

		/// <summary>
		/// Gets the version found in the metadata header. The major version number is in the high 16 bits
		/// and the lower version number is in the low 16 bits.
		/// </summary>
		public abstract uint Version { get; }

		/// <summary>
		/// Gets the version string found in the metadata header
		/// </summary>
		public abstract string VersionString { get; }

		/// <summary>
		/// Gets the <see cref="IPEImage"/>
		/// </summary>
		public abstract IPEImage PEImage { get; }

		/// <summary>
		/// Gets the metadata header
		/// </summary>
		public abstract MetadataHeader MetadataHeader { get; }

		/// <summary>
		/// Returns the #Strings stream or a default empty one if it's not present
		/// </summary>
		public abstract StringsStream StringsStream { get; }

		/// <summary>
		/// Returns the #US stream or a default empty one if it's not present
		/// </summary>
		public abstract USStream USStream { get; }

		/// <summary>
		/// Returns the #Blob stream or a default empty one if it's not present
		/// </summary>
		public abstract BlobStream BlobStream { get; }

		/// <summary>
		/// Returns the #GUID stream or a default empty one if it's not present
		/// </summary>
		public abstract GuidStream GuidStream { get; }

		/// <summary>
		/// Returns the #~ or #- tables stream
		/// </summary>
		public abstract TablesStream TablesStream { get; }

		/// <summary>
		/// Returns the #Pdb stream or null if it's not a standalone portable PDB file
		/// </summary>
		public abstract PdbStream PdbStream { get; }

		/// <summary>
		/// Gets all streams
		/// </summary>
		public abstract IList<DotNetStream> AllStreams { get; }

		/// <summary>
		/// Gets a list of all the valid <c>TypeDef</c> rids. It's usually every rid in the
		/// <c>TypeDef</c> table, but could be less if a type has been deleted.
		/// </summary>
		public abstract RidList GetTypeDefRidList();

		/// <summary>
		/// Gets a list of all the valid <c>ExportedType</c> rids. It's usually every rid in the
		/// <c>ExportedType</c> table, but could be less if a type has been deleted.
		/// </summary>
		public abstract RidList GetExportedTypeRidList();

		/// <summary>
		/// Gets the <c>Field</c> rid list
		/// </summary>
		/// <param name="typeDefRid"><c>TypeDef</c> rid</param>
		/// <returns>A new <see cref="RidList"/> instance</returns>
		public abstract RidList GetFieldRidList(uint typeDefRid);

		/// <summary>
		/// Gets the <c>Method</c> rid list
		/// </summary>
		/// <param name="typeDefRid"><c>TypeDef</c> rid</param>
		/// <returns>A new <see cref="RidList"/> instance</returns>
		public abstract RidList GetMethodRidList(uint typeDefRid);

		/// <summary>
		/// Gets the <c>Param</c> rid list
		/// </summary>
		/// <param name="methodRid"><c>Method</c> rid</param>
		/// <returns>A new <see cref="RidList"/> instance</returns>
		public abstract RidList GetParamRidList(uint methodRid);

		/// <summary>
		/// Gets the <c>Event</c> rid list
		/// </summary>
		/// <param name="eventMapRid"><c>EventMap</c> rid</param>
		/// <returns>A new <see cref="RidList"/> instance</returns>
		public abstract RidList GetEventRidList(uint eventMapRid);

		/// <summary>
		/// Gets the <c>Property</c> rid list
		/// </summary>
		/// <param name="propertyMapRid"><c>PropertyMap</c> rid</param>
		/// <returns>A new <see cref="RidList"/> instance</returns>
		public abstract RidList GetPropertyRidList(uint propertyMapRid);

		/// <summary>
		/// Finds all <c>InterfaceImpl</c> rids owned by <paramref name="typeDefRid"/>
		/// </summary>
		/// <param name="typeDefRid">Owner <c>TypeDef</c> rid</param>
		/// <returns>A <see cref="RidList"/> instance containing the valid <c>InterfaceImpl</c> rids</returns>
		public abstract RidList GetInterfaceImplRidList(uint typeDefRid);

		/// <summary>
		/// Finds all <c>GenericParam</c> rids owned by <paramref name="rid"/> in table <paramref name="table"/>
		/// </summary>
		/// <param name="table">A <c>TypeOrMethodDef</c> table</param>
		/// <param name="rid">Owner rid</param>
		/// <returns>A <see cref="RidList"/> instance containing the valid <c>GenericParam</c> rids</returns>
		public abstract RidList GetGenericParamRidList(Table table, uint rid);

		/// <summary>
		/// Finds all <c>GenericParamConstraint</c> rids owned by <paramref name="genericParamRid"/>
		/// </summary>
		/// <param name="genericParamRid">Owner <c>GenericParam</c> rid</param>
		/// <returns>A <see cref="RidList"/> instance containing the valid <c>GenericParamConstraint</c> rids</returns>
		public abstract RidList GetGenericParamConstraintRidList(uint genericParamRid);

		/// <summary>
		/// Finds all <c>CustomAttribute</c> rids owned by <paramref name="rid"/> in table <paramref name="table"/>
		/// </summary>
		/// <param name="table">A <c>HasCustomAttribute</c> table</param>
		/// <param name="rid">Owner rid</param>
		/// <returns>A <see cref="RidList"/> instance containing the valid <c>CustomAttribute</c> rids</returns>
		public abstract RidList GetCustomAttributeRidList(Table table, uint rid);

		/// <summary>
		/// Finds all <c>DeclSecurity</c> rids owned by <paramref name="rid"/> in table <paramref name="table"/>
		/// </summary>
		/// <param name="table">A <c>HasDeclSecurity</c> table</param>
		/// <param name="rid">Owner rid</param>
		/// <returns>A <see cref="RidList"/> instance containing the valid <c>DeclSecurity</c> rids</returns>
		public abstract RidList GetDeclSecurityRidList(Table table, uint rid);

		/// <summary>
		/// Finds all <c>MethodSemantics</c> rids owned by <paramref name="rid"/> in table <paramref name="table"/>
		/// </summary>
		/// <param name="table">A <c>HasSemantic</c> table</param>
		/// <param name="rid">Owner rid</param>
		/// <returns>A <see cref="RidList"/> instance containing the valid <c>MethodSemantics</c> rids</returns>
		public abstract RidList GetMethodSemanticsRidList(Table table, uint rid);

		/// <summary>
		/// Finds all <c>MethodImpl</c> rids owned by <paramref name="typeDefRid"/>
		/// </summary>
		/// <param name="typeDefRid">Owner <c>TypeDef</c> rid</param>
		/// <returns>A <see cref="RidList"/> instance containing the valid <c>MethodImpl</c> rids</returns>
		public abstract RidList GetMethodImplRidList(uint typeDefRid);

		/// <summary>
		/// Finds a <c>ClassLayout</c> rid
		/// </summary>
		/// <param name="typeDefRid">Owner <c>TypeDef</c> rid</param>
		/// <returns>The <c>ClassLayout</c> rid or 0 if <paramref name="typeDefRid"/> is invalid
		/// or if it has no row in the <c>ClassLayout</c> table.</returns>
		public abstract uint GetClassLayoutRid(uint typeDefRid);

		/// <summary>
		/// Finds a <c>FieldLayout</c> rid
		/// </summary>
		/// <param name="fieldRid">Owner <c>Field</c> rid</param>
		/// <returns>The <c>FieldLayout</c> rid or 0 if <paramref name="fieldRid"/> is invalid
		/// or if it has no row in the <c>FieldLayout</c> table.</returns>
		public abstract uint GetFieldLayoutRid(uint fieldRid);

		/// <summary>
		/// Finds a <c>FieldMarshal</c> rid
		/// </summary>
		/// <param name="table">A <c>HasFieldMarshal</c> table</param>
		/// <param name="rid">Owner rid</param>
		/// <returns>The <c>FieldMarshal</c> rid or 0 if <paramref name="rid"/> is invalid
		/// or if it has no row in the <c>FieldMarshal</c> table.</returns>
		public abstract uint GetFieldMarshalRid(Table table, uint rid);

		/// <summary>
		/// Finds a <c>FieldRVA</c> rid
		/// </summary>
		/// <param name="fieldRid">Owner <c>Field</c> rid</param>
		/// <returns>The <c>FieldRVA</c> rid or 0 if <paramref name="fieldRid"/> is invalid
		/// or if it has no row in the <c>FieldRVA</c> table.</returns>
		public abstract uint GetFieldRVARid(uint fieldRid);

		/// <summary>
		/// Finds an <c>ImplMap</c> rid
		/// </summary>
		/// <param name="table">A <c>MemberForwarded</c> table</param>
		/// <param name="rid">Owner rid</param>
		/// <returns>The <c>ImplMap</c> rid or 0 if <paramref name="rid"/> is invalid
		/// or if it has no row in the <c>ImplMap</c> table.</returns>
		public abstract uint GetImplMapRid(Table table, uint rid);

		/// <summary>
		/// Finds a <c>NestedClass</c> rid
		/// </summary>
		/// <param name="typeDefRid">Owner <c>TypeDef</c> rid</param>
		/// <returns>The <c>NestedClass</c> rid or 0 if <paramref name="typeDefRid"/> is invalid
		/// or if it has no row in the <c>NestedClass</c> table.</returns>
		public abstract uint GetNestedClassRid(uint typeDefRid);

		/// <summary>
		/// Finds an <c>EventMap</c> rid
		/// </summary>
		/// <param name="typeDefRid">Owner <c>TypeDef</c> rid</param>
		/// <returns>The <c>EventMap</c> rid or 0 if <paramref name="typeDefRid"/> is invalid
		/// or if it has no row in the <c>EventMap</c> table.</returns>
		public abstract uint GetEventMapRid(uint typeDefRid);

		/// <summary>
		/// Finds a <c>PropertyMap</c> rid
		/// </summary>
		/// <param name="typeDefRid">Owner <c>TypeDef</c> rid</param>
		/// <returns>The <c>PropertyMap</c> rid or 0 if <paramref name="typeDefRid"/> is invalid
		/// or if it has no row in the <c>PropertyMap</c> table.</returns>
		public abstract uint GetPropertyMapRid(uint typeDefRid);

		/// <summary>
		/// Finds a <c>Constant</c> rid
		/// </summary>
		/// <param name="table">A <c>HasConstant</c> table</param>
		/// <param name="rid">Owner rid</param>
		/// <returns>The <c>Constant</c> rid or 0 if <paramref name="rid"/> is invalid
		/// or if it has no row in the <c>Constant</c> table.</returns>
		public abstract uint GetConstantRid(Table table, uint rid);

		/// <summary>
		/// Returns the owner <c>TypeDef</c> rid
		/// </summary>
		/// <param name="fieldRid">A <c>Field</c> rid</param>
		/// <returns>The owner <c>TypeDef</c> rid or 0 if <paramref name="fieldRid"/> is invalid
		/// or if it has no owner.</returns>
		public abstract uint GetOwnerTypeOfField(uint fieldRid);

		/// <summary>
		/// Returns the owner <c>TypeDef</c> rid
		/// </summary>
		/// <param name="methodRid">A <c>Method</c> rid</param>
		/// <returns>The owner <c>TypeDef</c> rid or 0 if <paramref name="methodRid"/> is invalid
		/// or if it has no owner.</returns>
		public abstract uint GetOwnerTypeOfMethod(uint methodRid);

		/// <summary>
		/// Returns the owner <c>TypeDef</c> rid
		/// </summary>
		/// <param name="eventRid">A <c>Event</c> rid</param>
		/// <returns>The owner <c>TypeDef</c> rid or 0 if <paramref name="eventRid"/> is invalid
		/// or if it has no owner.</returns>
		public abstract uint GetOwnerTypeOfEvent(uint eventRid);

		/// <summary>
		/// Returns the owner <c>TypeDef</c> rid
		/// </summary>
		/// <param name="propertyRid">A <c>Property</c> rid</param>
		/// <returns>The owner <c>TypeDef</c> rid or 0 if <paramref name="propertyRid"/> is invalid
		/// or if it has no owner.</returns>
		public abstract uint GetOwnerTypeOfProperty(uint propertyRid);

		/// <summary>
		/// Returns the owner <c>TypeOrMethodDef</c> rid
		/// </summary>
		/// <param name="gpRid">A <c>GenericParam</c> rid</param>
		/// <returns>The owner <c>TypeOrMethodDef</c> rid or 0 if <paramref name="gpRid"/> is
		/// invalid or if it has no owner.</returns>
		public abstract uint GetOwnerOfGenericParam(uint gpRid);

		/// <summary>
		/// Returns the owner <c>GenericParam</c> rid
		/// </summary>
		/// <param name="gpcRid">A <c>GenericParamConstraint</c> rid</param>
		/// <returns>The owner <c>GenericParam</c> rid or 0 if <paramref name="gpcRid"/> is
		/// invalid or if it has no owner.</returns>
		public abstract uint GetOwnerOfGenericParamConstraint(uint gpcRid);

		/// <summary>
		/// Returns the owner <c>Method</c> rid
		/// </summary>
		/// <param name="paramRid">A <c>Param</c> rid</param>
		/// <returns>The owner <c>Method</c> rid or 0 if <paramref name="paramRid"/> is invalid
		/// or if it has no owner.</returns>
		public abstract uint GetOwnerOfParam(uint paramRid);

		/// <summary>
		/// Gets a list of all nested classes owned by <paramref name="typeDefRid"/>
		/// </summary>
		/// <param name="typeDefRid">A <c>TypeDef</c> rid</param>
		/// <returns>A new <see cref="RidList"/> instance</returns>
		public abstract RidList GetNestedClassRidList(uint typeDefRid);

		/// <summary>
		/// Gets a list of all non-nested classes. A type is a non-nested type if
		/// <see cref="GetNestedClassRidList(uint)"/> returns an empty list.
		/// </summary>
		/// <returns>A new <see cref="RidList"/> instance</returns>
		public abstract RidList GetNonNestedClassRidList();

		/// <summary>
		/// Finds all <c>LocalScope</c> rids owned by <paramref name="methodRid"/>
		/// </summary>
		/// <param name="methodRid">Owner <c>Method</c> rid</param>
		/// <returns>A <see cref="RidList"/> instance containing the valid <c>LocalScope</c> rids</returns>
		public abstract RidList GetLocalScopeRidList(uint methodRid);

		/// <summary>
		/// Gets the <c>StateMachineMethod</c> rid or 0 if it's not a state machine method
		/// </summary>
		/// <param name="methodRid">Owner <c>Method</c> rid</param>
		/// <returns></returns>
		public abstract uint GetStateMachineMethodRid(uint methodRid);

		/// <summary>
		/// Finds all <c>CustomDebugInformation</c> rids owned by <paramref name="rid"/> in table <paramref name="table"/>
		/// </summary>
		/// <param name="table">A <c>HasCustomDebugInformation</c> table</param>
		/// <param name="rid">Owner rid</param>
		/// <returns>A <see cref="RidList"/> instance containing the valid <c>CustomDebugInformation</c> rids</returns>
		public abstract RidList GetCustomDebugInformationRidList(Table table, uint rid);

		/// <summary>
		/// Disposes of this instance
		/// </summary>
		public abstract void Dispose();
	}
}
