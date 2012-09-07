using System;

namespace dot10.dotNET.MD {
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
	}
}
