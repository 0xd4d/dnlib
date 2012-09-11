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
	}
}
