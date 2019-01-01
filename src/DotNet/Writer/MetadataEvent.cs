// dnlib: See LICENSE.txt for more info

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// All <see cref="Metadata"/> events
	/// </summary>
	public enum MetadataEvent {
		/// <summary>
		/// Creating the tables has just begun
		/// </summary>
		BeginCreateTables,

		/// <summary>
		/// Before allocating all TypeDef RIDs
		/// </summary>
		AllocateTypeDefRids,

		/// <summary>
		/// Before allocating all MemberDef RIDs
		/// </summary>
		AllocateMemberDefRids,

		/// <summary>
		/// The <c>rid</c>s of types, fields, methods, events, properties and parameters are
		/// now known.
		/// </summary>
		MemberDefRidsAllocated,

		/// <summary>
		/// The tables and rows of all types, fields, methods, events, properties and parameters
		/// have been initialized. Method body RVAs are still not known, and no method has been
		/// written yet.
		/// </summary>
		MemberDefsInitialized,

		/// <summary>
		/// Before sorting most tables
		/// </summary>
		BeforeSortTables,

		/// <summary>
		/// Most of the tables that should be sorted have been sorted. The <c>CustomAttribute</c>
		/// table is still unsorted since it's not been created yet.
		/// </summary>
		MostTablesSorted,

		/// <summary>
		/// Custom attributes of all types, fields, methods, events, properties and parameters
		/// have now been written.
		/// </summary>
		MemberDefCustomAttributesWritten,

		/// <summary>
		/// All resources are about to be added to the .NET resources table
		/// </summary>
		BeginAddResources,

		/// <summary>
		/// All resources have been added to the .NET resources table
		/// </summary>
		EndAddResources,

		/// <summary>
		/// All method bodies are about to be written
		/// </summary>
		BeginWriteMethodBodies,

		/// <summary>
		/// All method bodies have been written. Their RVAs are still not known.
		/// </summary>
		EndWriteMethodBodies,

		/// <summary>
		/// All tables are now sorted, including the <c>CustomAttribute</c> table.
		/// </summary>
		OnAllTablesSorted,

		/// <summary>
		/// All tables have been created and all rows populated. The only columns that haven't
		/// been initialized yet are the ones that are RVAs.
		/// </summary>
		EndCreateTables,
	}
}
