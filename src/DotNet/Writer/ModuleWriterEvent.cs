// dnlib: See LICENSE.txt for more info

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// All <see cref="ModuleWriter"/> / <see cref="NativeModuleWriter"/> events
	/// </summary>
	public enum ModuleWriterEvent {
		/// <summary>
		/// Writing has just begun
		/// </summary>
		Begin,

		/// <summary>
		/// All PE sections have been created
		/// </summary>
		PESectionsCreated,

		/// <summary>
		/// All chunks have been created
		/// </summary>
		ChunksCreated,

		/// <summary>
		/// All chunks have been added to their sections
		/// </summary>
		ChunksAddedToSections,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.BeginCreateTables"/>.
		/// Creating the metadata tables has just begun
		/// </summary>
		MDBeginCreateTables,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.AllocateTypeDefRids"/>.
		/// Before allocating all TypeDef RIDs
		/// </summary>
		MDAllocateTypeDefRids,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.AllocateMemberDefRids"/>.
		/// Before allocating all MemberDef RIDs
		/// </summary>
		MDAllocateMemberDefRids,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.AllocateMemberDefRids0"/>.
		/// Sent by the metadata writer so a UI can update its progress bar
		/// </summary>
		MDAllocateMemberDefRids0,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.AllocateMemberDefRids1"/>.
		/// Sent by the metadata writer so a UI can update its progress bar
		/// </summary>
		MDAllocateMemberDefRids1,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.AllocateMemberDefRids2"/>.
		/// Sent by the metadata writer so a UI can update its progress bar
		/// </summary>
		MDAllocateMemberDefRids2,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.AllocateMemberDefRids3"/>.
		/// Sent by the metadata writer so a UI can update its progress bar
		/// </summary>
		MDAllocateMemberDefRids3,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.AllocateMemberDefRids4"/>.
		/// Sent by the metadata writer so a UI can update its progress bar
		/// </summary>
		MDAllocateMemberDefRids4,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.MemberDefRidsAllocated"/>.
		/// The <c>rid</c>s of types, fields, methods, events, properties and parameters are
		/// now known.
		/// </summary>
		MDMemberDefRidsAllocated,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.InitializeTypeDefsAndMemberDefs0"/>.
		/// Sent by the metadata writer so a UI can update its progress bar
		/// </summary>
		MDInitializeTypeDefsAndMemberDefs0,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.InitializeTypeDefsAndMemberDefs1"/>.
		/// Sent by the metadata writer so a UI can update its progress bar
		/// </summary>
		MDInitializeTypeDefsAndMemberDefs1,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.InitializeTypeDefsAndMemberDefs2"/>.
		/// Sent by the metadata writer so a UI can update its progress bar
		/// </summary>
		MDInitializeTypeDefsAndMemberDefs2,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.InitializeTypeDefsAndMemberDefs3"/>.
		/// Sent by the metadata writer so a UI can update its progress bar
		/// </summary>
		MDInitializeTypeDefsAndMemberDefs3,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.InitializeTypeDefsAndMemberDefs4"/>.
		/// Sent by the metadata writer so a UI can update its progress bar
		/// </summary>
		MDInitializeTypeDefsAndMemberDefs4,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.MemberDefsInitialized"/>.
		/// The tables and rows of all types, fields, methods, events, properties and parameters
		/// have been initialized. Method body RVAs are still not known, and no method has been
		/// written yet.
		/// </summary>
		MDMemberDefsInitialized,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.BeforeSortTables"/>.
		/// Before sorting most tables
		/// </summary>
		MDBeforeSortTables,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.MostTablesSorted"/>.
		/// Most of the tables that should be sorted have been sorted. The <c>CustomAttribute</c>
		/// table is still unsorted since it's not been created yet.
		/// </summary>
		MDMostTablesSorted,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.WriteTypeDefAndMemberDefCustomAttributes0"/>.
		/// Sent by the metadata writer so a UI can update its progress bar
		/// </summary>
		MDWriteTypeDefAndMemberDefCustomAttributes0,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.WriteTypeDefAndMemberDefCustomAttributes1"/>.
		/// Sent by the metadata writer so a UI can update its progress bar
		/// </summary>
		MDWriteTypeDefAndMemberDefCustomAttributes1,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.WriteTypeDefAndMemberDefCustomAttributes2"/>.
		/// Sent by the metadata writer so a UI can update its progress bar
		/// </summary>
		MDWriteTypeDefAndMemberDefCustomAttributes2,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.WriteTypeDefAndMemberDefCustomAttributes3"/>.
		/// Sent by the metadata writer so a UI can update its progress bar
		/// </summary>
		MDWriteTypeDefAndMemberDefCustomAttributes3,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.WriteTypeDefAndMemberDefCustomAttributes4"/>.
		/// Sent by the metadata writer so a UI can update its progress bar
		/// </summary>
		MDWriteTypeDefAndMemberDefCustomAttributes4,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.MemberDefCustomAttributesWritten"/>.
		/// Custom attributes of all types, fields, methods, events, properties and parameters
		/// have now been written.
		/// </summary>
		MDMemberDefCustomAttributesWritten,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.BeginAddResources"/>.
		/// All resources are about to be added to the .NET resources table
		/// </summary>
		MDBeginAddResources,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.EndAddResources"/>.
		/// All resources have been added to the .NET resources table
		/// </summary>
		MDEndAddResources,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.BeginWriteMethodBodies"/>.
		/// All method bodies are about to be written
		/// </summary>
		MDBeginWriteMethodBodies,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.WriteMethodBodies0"/>.
		/// Sent by the metadata writer so a UI can update its progress bar
		/// </summary>
		MDWriteMethodBodies0,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.WriteMethodBodies1"/>.
		/// Sent by the metadata writer so a UI can update its progress bar
		/// </summary>
		MDWriteMethodBodies1,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.WriteMethodBodies2"/>.
		/// Sent by the metadata writer so a UI can update its progress bar
		/// </summary>
		MDWriteMethodBodies2,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.WriteMethodBodies3"/>.
		/// Sent by the metadata writer so a UI can update its progress bar
		/// </summary>
		MDWriteMethodBodies3,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.WriteMethodBodies4"/>.
		/// Sent by the metadata writer so a UI can update its progress bar
		/// </summary>
		MDWriteMethodBodies4,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.WriteMethodBodies5"/>.
		/// Sent by the metadata writer so a UI can update its progress bar
		/// </summary>
		MDWriteMethodBodies5,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.WriteMethodBodies6"/>.
		/// Sent by the metadata writer so a UI can update its progress bar
		/// </summary>
		MDWriteMethodBodies6,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.WriteMethodBodies7"/>.
		/// Sent by the metadata writer so a UI can update its progress bar
		/// </summary>
		MDWriteMethodBodies7,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.WriteMethodBodies8"/>.
		/// Sent by the metadata writer so a UI can update its progress bar
		/// </summary>
		MDWriteMethodBodies8,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.WriteMethodBodies9"/>.
		/// Sent by the metadata writer so a UI can update its progress bar
		/// </summary>
		MDWriteMethodBodies9,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.EndWriteMethodBodies"/>.
		/// All method bodies have been written. Their RVAs are still not known.
		/// </summary>
		MDEndWriteMethodBodies,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.OnAllTablesSorted"/>.
		/// All tables are now sorted, including the <c>CustomAttribute</c> table.
		/// </summary>
		MDOnAllTablesSorted,

		/// <summary>
		/// Original event: <see cref="MetadataEvent.EndCreateTables"/>.
		/// All tables have been created and all rows populated. The only columns that haven't
		/// been initialized yet are the ones that are RVAs.
		/// </summary>
		MDEndCreateTables,

		/// <summary>
		/// This event occurs before the PDB file is written. This event occurs even if no PDB file
		/// will be written.
		/// </summary>
		BeginWritePdb,

		/// <summary>
		/// The PDB file has been written. This event occurs even if no PDB file has been written.
		/// </summary>
		EndWritePdb,

		/// <summary>
		/// This event occurs just before all RVAs and file offsets of the chunks are calculated.
		/// </summary>
		BeginCalculateRvasAndFileOffsets,

		/// <summary>
		/// File offsets and RVAs of all chunks are now known. This includes method body and
		/// field RVAs. Nothing has been written to the destination stream yet.
		/// </summary>
		EndCalculateRvasAndFileOffsets,

		/// <summary>
		/// This event occurs before all chunks are written to the destination stream, and after
		/// all RVAs and file offsets are known.
		/// </summary>
		BeginWriteChunks,

		/// <summary>
		/// All chunks have been written to the destination stream.
		/// </summary>
		EndWriteChunks,

		/// <summary>
		/// This event occurs before the strong name signature is calculated. This event
		/// occurs even if the assembly isn't strong name signed.
		/// </summary>
		BeginStrongNameSign,

		/// <summary>
		/// This event occurs after the strong name signature has been calculated. This event
		/// occurs even if the assembly isn't strong name signed.
		/// </summary>
		EndStrongNameSign,

		/// <summary>
		/// This event occurs before the checksum in the PE header is updated. This event
		/// occurs even if the checksum isn't updated.
		/// </summary>
		BeginWritePEChecksum,

		/// <summary>
		/// This event occurs after the checksum in the PE header has been updated. This event
		/// occurs even if the checksum isn't updated.
		/// </summary>
		EndWritePEChecksum,

		/// <summary>
		/// Writing has ended
		/// </summary>
		End,
	}
}
