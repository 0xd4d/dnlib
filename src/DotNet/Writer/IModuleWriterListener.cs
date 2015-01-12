// dnlib: See LICENSE.txt for more info

﻿namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Gets notified of various events when writing a module
	/// </summary>
	public interface IModuleWriterListener {
		/// <summary>
		/// Called by <see cref="ModuleWriterBase"/> and its sub classes.
		/// </summary>
		/// <param name="writer">The module writer</param>
		/// <param name="evt">Type of writer event</param>
		void OnWriterEvent(ModuleWriterBase writer, ModuleWriterEvent evt);
	}

	/// <summary>
	/// A <see cref="IModuleWriterListener"/> which does nothing
	/// </summary>
	public sealed class DummyModuleWriterListener : IModuleWriterListener {
		/// <summary>
		/// An instance of this dummy listener
		/// </summary>
		public static readonly DummyModuleWriterListener Instance = new DummyModuleWriterListener();

		/// <inheritdoc/>
		public void OnWriterEvent(ModuleWriterBase writer, ModuleWriterEvent evt) {
		}
	}

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
		/// Original event: <see cref="MetaDataEvent.BeginCreateTables"/>.
		/// Creating the metadata tables has just begun
		/// </summary>
		MDBeginCreateTables,

		/// <summary>
		/// Original event: <see cref="MetaDataEvent.MemberDefRidsAllocated"/>.
		/// The <c>rid</c>s of types, fields, methods, events, properties and parameters are
		/// now known.
		/// </summary>
		MDMemberDefRidsAllocated,

		/// <summary>
		/// Original event: <see cref="MetaDataEvent.MemberDefsInitialized"/>.
		/// The tables and rows of all types, fields, methods, events, properties and parameters
		/// have been initialized. Method body RVAs are still not known, and no method has been
		/// written yet.
		/// </summary>
		MDMemberDefsInitialized,

		/// <summary>
		/// Original event: <see cref="MetaDataEvent.MostTablesSorted"/>.
		/// Most of the tables that should be sorted have been sorted. The <c>CustomAttribute</c>
		/// table is still unsorted since it's not been created yet.
		/// </summary>
		MDMostTablesSorted,

		/// <summary>
		/// Original event: <see cref="MetaDataEvent.MemberDefCustomAttributesWritten"/>.
		/// Custom attributes of all types, fields, methods, events, properties and parameters
		/// have now been written.
		/// </summary>
		MDMemberDefCustomAttributesWritten,

		/// <summary>
		/// Original event: <see cref="MetaDataEvent.BeginAddResources"/>.
		/// All resources are about to be added to the .NET resources table
		/// </summary>
		MDBeginAddResources,

		/// <summary>
		/// Original event: <see cref="MetaDataEvent.EndAddResources"/>.
		/// All resources have been added to the .NET resources table
		/// </summary>
		MDEndAddResources,

		/// <summary>
		/// Original event: <see cref="MetaDataEvent.BeginWriteMethodBodies"/>.
		/// All method bodies are about to be written
		/// </summary>
		MDBeginWriteMethodBodies,

		/// <summary>
		/// Original event: <see cref="MetaDataEvent.EndWriteMethodBodies"/>.
		/// All method bodies have been written. Their RVAs are still not known.
		/// </summary>
		MDEndWriteMethodBodies,

		/// <summary>
		/// Original event: <see cref="MetaDataEvent.OnAllTablesSorted"/>.
		/// All tables are now sorted, including the <c>CustomAttribute</c> table.
		/// </summary>
		MDOnAllTablesSorted,

		/// <summary>
		/// Original event: <see cref="MetaDataEvent.EndCreateTables"/>.
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
