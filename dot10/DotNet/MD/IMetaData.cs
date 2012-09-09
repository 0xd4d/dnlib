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
		/// Gets the start rid and length of the field list
		/// </summary>
		/// <remarks>You must call <see cref="ToFieldRid"/> to convert every value in the
		/// returned range to the correct rid in the <c>Field</c> table. The rids in the
		/// <c>Field</c> table aren't always contiguous, eg. when the <c>#-</c> table is used and
		/// the <c>FieldPtr</c> table is present.</remarks>
		/// <param name="typeDefRid"><c>TypeDef</c> rid</param>
		/// <param name="startRid">Updated with start rid</param>
		/// <returns>Number of fields owned by the type</returns>
		uint GetFieldRange(uint typeDefRid, out uint startRid);

		/// <summary>
		/// Converts a rid returned by <see cref="GetFieldRange"/> to a rid into the <c>Field</c>
		/// table.
		/// </summary>
		/// <param name="listRid">A valid rid</param>
		/// <returns>Converted rid or any invalid rid value if <paramref name="listRid"/> is invalid</returns>
		uint ToFieldRid(uint listRid);

		/// <summary>
		/// Gets the start rid and length of the method list
		/// </summary>
		/// <remarks>You must call <see cref="ToMethodRid"/> to convert every value in the
		/// returned range to the correct rid in the <c>Method</c> table. The rids in the
		/// <c>Method</c> table aren't always contiguous, eg. when the <c>#-</c> table is used and
		/// the <c>MethodPtr</c> table is present.</remarks>
		/// <param name="typeDefRid"><c>TypeDef</c> rid</param>
		/// <param name="startRid">Updated with start rid</param>
		/// <returns>Number of methods owned by the type</returns>
		uint GetMethodRange(uint typeDefRid, out uint startRid);

		/// <summary>
		/// Converts a rid returned by <see cref="GetMethodRange"/> to a rid into the <c>Method</c>
		/// table.
		/// </summary>
		/// <param name="listRid">A valid rid</param>
		/// <returns>Converted rid or any invalid rid value if <paramref name="listRid"/> is invalid</returns>
		uint ToMethodRid(uint listRid);

		/// <summary>
		/// Gets the start rid and length of the param list
		/// </summary>
		/// <remarks>You must call <see cref="ToParamRid"/> to convert every value in the
		/// returned range to the correct rid in the <c>Param</c> table. The rids in the
		/// <c>Param</c> table aren't always contiguous, eg. when the <c>#-</c> table is used and
		/// the <c>ParamPtr</c> table is present.</remarks>
		/// <param name="methodRid"><c>Method</c> rid</param>
		/// <param name="startRid">Updated with start rid</param>
		/// <returns>Number of params owned by the method</returns>
		uint GetParamRange(uint methodRid, out uint startRid);

		/// <summary>
		/// Converts a rid returned by <see cref="GetParamRange"/> to a rid into the <c>Param</c>
		/// table.
		/// </summary>
		/// <param name="listRid">A valid rid</param>
		/// <returns>Converted rid or any invalid rid value if <paramref name="listRid"/> is invalid</returns>
		uint ToParamRid(uint listRid);

		/// <summary>
		/// Gets the start rid and length of the event list
		/// </summary>
		/// <remarks>You must call <see cref="ToEventRid"/> to convert every value in the
		/// returned range to the correct rid in the <c>Event</c> table. The rids in the
		/// <c>Event</c> table aren't always contiguous, eg. when the <c>#-</c> table is used and
		/// the <c>EventPtr</c> table is present.</remarks>
		/// <param name="eventMapRid"><c>EventMap</c> rid</param>
		/// <param name="startRid">Updated with start rid</param>
		/// <returns>Number of events owned by the event map</returns>
		uint GetEventMapRange(uint eventMapRid, out uint startRid);

		/// <summary>
		/// Converts a rid returned by <see cref="GetEventMapRange"/> to a rid into the <c>Event</c>
		/// table.
		/// </summary>
		/// <param name="listRid">A valid rid</param>
		/// <returns>Converted rid or any invalid rid value if <paramref name="listRid"/> is invalid</returns>
		uint ToEventRid(uint listRid);

		/// <summary>
		/// Gets the start rid and length of the property list
		/// </summary>
		/// <remarks>You must call <see cref="ToPropertyRid"/> to convert every value in the
		/// returned range to the correct rid in the <c>Property</c> table. The rids in the
		/// <c>Property</c> table aren't always contiguous, eg. when the <c>#-</c> table is used and
		/// the <c>PropertyPtr</c> table is present.</remarks>
		/// <param name="propertyMapRid"><c>PropertyMap</c> rid</param>
		/// <param name="startRid">Updated with start rid</param>
		/// <returns>Number of propertys owned by the property map</returns>
		uint GetPropertyMapRange(uint propertyMapRid, out uint startRid);

		/// <summary>
		/// Converts a rid returned by <see cref="GetPropertyMapRange"/> to a rid into the <c>Property</c>
		/// table.
		/// </summary>
		/// <param name="listRid">A valid rid</param>
		/// <returns>Converted rid or any invalid rid value if <paramref name="listRid"/> is invalid</returns>
		uint ToPropertyRid(uint listRid);
	}
}
