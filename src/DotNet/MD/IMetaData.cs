using System;

namespace dot10.DotNet.MD {
	/// <summary>
	/// Interface to access the .NET metadata
	/// </summary>
	public interface IMetaData : IDisposable {
		/// <summary>
		/// Returns the #Strings stream or a default empty one if it's not present
		/// </summary>
		StringsStream StringsStream { get; }

		/// <summary>
		/// Returns the #US stream or a default empty one if it's not present
		/// </summary>
		USStream USStream { get; }

		/// <summary>
		/// Returns the #Blob stream or a default empty one if it's not present
		/// </summary>
		BlobStream BlobStream { get; }

		/// <summary>
		/// Returns the #GUID stream or a default empty one if it's not present
		/// </summary>
		GuidStream GuidStream { get; }

		/// <summary>
		/// Returns the #~ or #- tables stream
		/// </summary>
		TablesStream TablesStream { get; }

		/// <summary>
		/// Gets the <c>Field</c> rid list
		/// </summary>
		/// <param name="typeDefRid"><c>TypeDef</c> rid</param>
		/// <returns>A new <see cref="RidList"/> instance</returns>
		RidList GetFieldRidList(uint typeDefRid);

		/// <summary>
		/// Gets the <c>Method</c> rid list
		/// </summary>
		/// <param name="typeDefRid"><c>TypeDef</c> rid</param>
		/// <returns>A new <see cref="RidList"/> instance</returns>
		RidList GetMethodRidList(uint typeDefRid);

		/// <summary>
		/// Gets the <c>Param</c> rid list
		/// </summary>
		/// <param name="methodRid"><c>Method</c> rid</param>
		/// <returns>A new <see cref="RidList"/> instance</returns>
		RidList GetParamRidList(uint methodRid);

		/// <summary>
		/// Gets the <c>Event</c> rid list
		/// </summary>
		/// <param name="eventMapRid"><c>EventMap</c> rid</param>
		/// <returns>A new <see cref="RidList"/> instance</returns>
		RidList GetEventRidList(uint eventMapRid);

		/// <summary>
		/// Gets the <c>Property</c> rid list
		/// </summary>
		/// <param name="propertyMapRid"><c>PropertyMap</c> rid</param>
		/// <returns>A new <see cref="RidList"/> instance</returns>
		RidList GetPropertyRidList(uint propertyMapRid);

		/// <summary>
		/// Finds all <c>InterfaceImpl</c> rids owned by <paramref name="typeDefRid"/>
		/// </summary>
		/// <param name="typeDefRid">Owner <c>TypeDef</c> rid</param>
		/// <returns>A <see cref="RidList"/> instance containing the valid <c>InterfaceImpl</c> rids</returns>
		RidList GetInterfaceImplRidList(uint typeDefRid);

		/// <summary>
		/// Finds all <c>GenericParam</c> rids owned by <paramref name="rid"/> in table <paramref name="table"/>
		/// </summary>
		/// <param name="table">A <c>TypeOrMethodDef</c> table</param>
		/// <param name="rid">Owner rid</param>
		/// <returns>A <see cref="RidList"/> instance containing the valid <c>GenericParam</c> rids</returns>
		RidList GetGenericParamRidList(Table table, uint rid);

		/// <summary>
		/// Finds all <c>GenericParamConstraint</c> rids owned by <paramref name="genericParamRid"/>
		/// </summary>
		/// <param name="genericParamRid">Owner <c>GenericParam</c> rid</param>
		/// <returns>A <see cref="RidList"/> instance containing the valid <c>GenericParamConstraint</c> rids</returns>
		RidList GetGenericParamConstraintRidList(uint genericParamRid);

		/// <summary>
		/// Finds all <c>CustomAttribute</c> rids owned by <paramref name="rid"/> in table <paramref name="table"/>
		/// </summary>
		/// <param name="table">A <c>HasCustomAttribute</c> table</param>
		/// <param name="rid">Owner rid</param>
		/// <returns>A <see cref="RidList"/> instance containing the valid <c>CustomAttribute</c> rids</returns>
		RidList GetCustomAttributeRidList(Table table, uint rid);

		/// <summary>
		/// Finds all <c>DeclSecurity</c> rids owned by <paramref name="rid"/> in table <paramref name="table"/>
		/// </summary>
		/// <param name="table">A <c>HasDeclSecurity</c> table</param>
		/// <param name="rid">Owner rid</param>
		/// <returns>A <see cref="RidList"/> instance containing the valid <c>DeclSecurity</c> rids</returns>
		RidList GetDeclSecurityRidList(Table table, uint rid);

		/// <summary>
		/// Finds all <c>MethodSemantics</c> rids owned by <paramref name="rid"/> in table <paramref name="table"/>
		/// </summary>
		/// <param name="table">A <c>HasSemantic</c> table</param>
		/// <param name="rid">Owner rid</param>
		/// <returns>A <see cref="RidList"/> instance containing the valid <c>MethodSemantics</c> rids</returns>
		RidList GetMethodSemanticsRidList(Table table, uint rid);

		/// <summary>
		/// Finds all <c>MethodImpl</c> rids owned by <paramref name="typeDefRid"/>
		/// </summary>
		/// <param name="typeDefRid">Owner <c>TypeDef</c> rid</param>
		/// <returns>A <see cref="RidList"/> instance containing the valid <c>MethodImpl</c> rids</returns>
		RidList GetMethodImplRidList(uint typeDefRid);

		/// <summary>
		/// Finds a <c>ClassLayout</c> rid
		/// </summary>
		/// <param name="typeDefRid"><c>TypeDef</c> rid</param>
		/// <returns>The <c>ClassLayout</c> rid or 0 if <paramref name="typeDefRid"/> is invalid
		/// or if it has no row in the <c>ClassLayout</c> table.</returns>
		uint GetClassLayoutRid(uint typeDefRid);

		/// <summary>
		/// Finds a <c>FieldLayout</c> rid
		/// </summary>
		/// <param name="fieldRid"><c>Field</c> rid</param>
		/// <returns>The <c>FieldLayout</c> rid or 0 if <paramref name="fieldRid"/> is invalid
		/// or if it has no row in the <c>FieldLayout</c> table.</returns>
		uint GetFieldLayoutRid(uint fieldRid);

		/// <summary>
		/// Finds a <c>FieldMarshal</c> rid
		/// </summary>
		/// <param name="table">A <c>HasFieldMarshal</c> table</param>
		/// <param name="rid">Owner rid</param>
		/// <returns>The <c>FieldMarshal</c> rid or 0 if <paramref name="rid"/> is invalid
		/// or if it has no row in the <c>FieldMarshal</c> table.</returns>
		uint GetFieldMarshalRid(Table table, uint rid);

		/// <summary>
		/// Finds a <c>FieldRVA</c> rid
		/// </summary>
		/// <param name="fieldRid"><c>Field</c> rid</param>
		/// <returns>The <c>FieldRVA</c> rid or 0 if <paramref name="fieldRid"/> is invalid
		/// or if it has no row in the <c>FieldRVA</c> table.</returns>
		uint GetFieldRVARid(uint fieldRid);

		/// <summary>
		/// Finds a <c>ImplMap</c> rid
		/// </summary>
		/// <param name="table">A <c>MemberForwarded</c> table</param>
		/// <param name="rid">Owner rid</param>
		/// <returns>The <c>ImplMap</c> rid or 0 if <paramref name="rid"/> is invalid
		/// or if it has no row in the <c>ImplMap</c> table.</returns>
		uint GetImplMapRid(Table table, uint rid);

		/// <summary>
		/// Finds a <c>NestedClass</c> rid
		/// </summary>
		/// <param name="typeDefRid"><c>TypeDef</c> rid</param>
		/// <returns>The <c>NestedClass</c> rid or 0 if <paramref name="typeDefRid"/> is invalid
		/// or if it has no row in the <c>NestedClass</c> table.</returns>
		uint GetNestedClassRid(uint typeDefRid);

		/// <summary>
		/// Finds a <c>EventMap</c> rid
		/// </summary>
		/// <param name="typeDefRid"><c>TypeDef</c> rid</param>
		/// <returns>The <c>EventMap</c> rid or 0 if <paramref name="typeDefRid"/> is invalid
		/// or if it has no row in the <c>EventMap</c> table.</returns>
		uint GetEventMapRid(uint typeDefRid);

		/// <summary>
		/// Finds a <c>PropertyMap</c> rid
		/// </summary>
		/// <param name="typeDefRid"><c>TypeDef</c> rid</param>
		/// <returns>The <c>PropertyMap</c> rid or 0 if <paramref name="typeDefRid"/> is invalid
		/// or if it has no row in the <c>PropertyMap</c> table.</returns>
		uint GetPropertyMapRid(uint typeDefRid);

		/// <summary>
		/// Finds a <c>Constant</c> rid
		/// </summary>
		/// <param name="table">A <c>HasConstant</c> table</param>
		/// <param name="rid">Owner rid</param>
		/// <returns>The <c>Constant</c> rid or 0 if <paramref name="rid"/> is invalid
		/// or if it has no row in the <c>Constant</c> table.</returns>
		uint GetConstantRid(Table table, uint rid);
	}
}
