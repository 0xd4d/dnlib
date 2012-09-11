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
		/// Gets the <c>Field</c> rid range
		/// </summary>
		/// <param name="typeDefRid"><c>TypeDef</c> rid</param>
		/// <returns>A new <see cref="RidRange"/> instance</returns>
		RidRange GetFieldRange(uint typeDefRid);

		/// <summary>
		/// Gets the <c>Method</c> rid range
		/// </summary>
		/// <param name="typeDefRid"><c>TypeDef</c> rid</param>
		/// <returns>A new <see cref="RidRange"/> instance</returns>
		RidRange GetMethodRange(uint typeDefRid);

		/// <summary>
		/// Gets the <c>Param</c> rid range
		/// </summary>
		/// <param name="methodRid"><c>Method</c> rid</param>
		/// <returns>A new <see cref="RidRange"/> instance</returns>
		RidRange GetParamRange(uint methodRid);

		/// <summary>
		/// Gets the <c>Event</c> rid range
		/// </summary>
		/// <param name="eventMapRid"><c>EventMap</c> rid</param>
		/// <returns>A new <see cref="RidRange"/> instance</returns>
		RidRange GetEventRange(uint eventMapRid);

		/// <summary>
		/// Gets the <c>Property</c> rid range
		/// </summary>
		/// <param name="propertyMapRid"><c>PropertyMap</c> rid</param>
		/// <returns>A new <see cref="RidRange"/> instance</returns>
		RidRange GetPropertyRange(uint propertyMapRid);
	}
}
