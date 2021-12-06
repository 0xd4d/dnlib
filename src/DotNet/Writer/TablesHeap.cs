// dnlib: See LICENSE.txt for more info

using dnlib.IO;
using dnlib.PE;
using dnlib.DotNet.MD;
using System;
using System.Collections.Generic;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// <see cref="TablesHeap"/> options
	/// </summary>
	public sealed class TablesHeapOptions {
		/// <summary>
		/// Should be 0
		/// </summary>
		public uint? Reserved1;

		/// <summary>
		/// Major version number. Default is 2. Valid versions are v1.0 (no generics),
		/// v1.1 (generics are supported), or v2.0 (recommended).
		/// </summary>
		public byte? MajorVersion;

		/// <summary>
		/// Minor version number. Default is 0.
		/// </summary>
		public byte? MinorVersion;

		/// <summary>
		/// Force #- or #~ stream. Default value is <c>null</c> and recommended because the correct
		/// tables stream will be used. <c>true</c> will force <c>#-</c> (Edit N' Continue)
		/// stream, and <c>false</c> will force <c>#~</c> (normal compressed) stream.
		/// </summary>
		public bool? UseENC;

		/// <summary>
		/// All columns that can be 2 or 4 bytes are forced to be 4 bytes.
		/// Set this to <c>true</c> if you add a <c>#JTD</c> heap and (if CLR) a <c>#-</c> tables heap is used
		/// or (if Mono/Unity) a <c>#~</c> or <c>#-</c> tables heap is used.
		/// dnlib won't try to auto detect this from your added heaps since the CLR/CoreCLR vs Mono/Unity behavior
		/// is a little bit different. You may need to set <see cref="UseENC"/> to <c>true</c> if you target CLR/CoreCLR.
		/// </summary>
		public bool? ForceBigColumns;

		/// <summary>
		/// Extra data to write
		/// </summary>
		public uint? ExtraData;

		/// <summary>
		/// Log2Rid to write
		/// </summary>
		public byte? Log2Rid;

		/// <summary>
		/// <c>true</c> if there are deleted <see cref="TypeDef"/>s, <see cref="ExportedType"/>s,
		/// <see cref="FieldDef"/>s, <see cref="MethodDef"/>s, <see cref="EventDef"/>s and/or
		/// <see cref="PropertyDef"/>s.
		/// </summary>
		public bool? HasDeletedRows;

		/// <summary>
		/// Creates portable PDB v1.0 options
		/// </summary>
		/// <returns></returns>
		public static TablesHeapOptions CreatePortablePdbV1_0() =>
			new TablesHeapOptions {
				Reserved1 = 0,
				MajorVersion = 2,
				MinorVersion = 0,
				UseENC = null,
				ExtraData = null,
				Log2Rid = null,
				HasDeletedRows = null,
			};
	}

	/// <summary>
	/// Contains all .NET tables
	/// </summary>
	public sealed class TablesHeap : IHeap {
		uint length;
		byte majorVersion;
		byte minorVersion;
		bool bigStrings;
		bool bigGuid;
		bool bigBlob;
		bool hasDeletedRows;
		readonly Metadata metadata;
		readonly TablesHeapOptions options;
		FileOffset offset;
		RVA rva;

		/// <inheritdoc/>
		public FileOffset FileOffset => offset;

		/// <inheritdoc/>
		public RVA RVA => rva;

#pragma warning disable 1591	// XML doc comment
		public readonly MDTable<RawModuleRow> ModuleTable = new MDTable<RawModuleRow>(Table.Module, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawTypeRefRow> TypeRefTable = new MDTable<RawTypeRefRow>(Table.TypeRef, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawTypeDefRow> TypeDefTable = new MDTable<RawTypeDefRow>(Table.TypeDef, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawFieldPtrRow> FieldPtrTable = new MDTable<RawFieldPtrRow>(Table.FieldPtr, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawFieldRow> FieldTable = new MDTable<RawFieldRow>(Table.Field, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawMethodPtrRow> MethodPtrTable = new MDTable<RawMethodPtrRow>(Table.MethodPtr, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawMethodRow> MethodTable = new MDTable<RawMethodRow>(Table.Method, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawParamPtrRow> ParamPtrTable = new MDTable<RawParamPtrRow>(Table.ParamPtr, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawParamRow> ParamTable = new MDTable<RawParamRow>(Table.Param, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawInterfaceImplRow> InterfaceImplTable = new MDTable<RawInterfaceImplRow>(Table.InterfaceImpl, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawMemberRefRow> MemberRefTable = new MDTable<RawMemberRefRow>(Table.MemberRef, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawConstantRow> ConstantTable = new MDTable<RawConstantRow>(Table.Constant, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawCustomAttributeRow> CustomAttributeTable = new MDTable<RawCustomAttributeRow>(Table.CustomAttribute, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawFieldMarshalRow> FieldMarshalTable = new MDTable<RawFieldMarshalRow>(Table.FieldMarshal, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawDeclSecurityRow> DeclSecurityTable = new MDTable<RawDeclSecurityRow>(Table.DeclSecurity, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawClassLayoutRow> ClassLayoutTable = new MDTable<RawClassLayoutRow>(Table.ClassLayout, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawFieldLayoutRow> FieldLayoutTable = new MDTable<RawFieldLayoutRow>(Table.FieldLayout, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawStandAloneSigRow> StandAloneSigTable = new MDTable<RawStandAloneSigRow>(Table.StandAloneSig, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawEventMapRow> EventMapTable = new MDTable<RawEventMapRow>(Table.EventMap, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawEventPtrRow> EventPtrTable = new MDTable<RawEventPtrRow>(Table.EventPtr, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawEventRow> EventTable = new MDTable<RawEventRow>(Table.Event, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawPropertyMapRow> PropertyMapTable = new MDTable<RawPropertyMapRow>(Table.PropertyMap, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawPropertyPtrRow> PropertyPtrTable = new MDTable<RawPropertyPtrRow>(Table.PropertyPtr, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawPropertyRow> PropertyTable = new MDTable<RawPropertyRow>(Table.Property, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawMethodSemanticsRow> MethodSemanticsTable = new MDTable<RawMethodSemanticsRow>(Table.MethodSemantics, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawMethodImplRow> MethodImplTable = new MDTable<RawMethodImplRow>(Table.MethodImpl, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawModuleRefRow> ModuleRefTable = new MDTable<RawModuleRefRow>(Table.ModuleRef, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawTypeSpecRow> TypeSpecTable = new MDTable<RawTypeSpecRow>(Table.TypeSpec, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawImplMapRow> ImplMapTable = new MDTable<RawImplMapRow>(Table.ImplMap, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawFieldRVARow> FieldRVATable = new MDTable<RawFieldRVARow>(Table.FieldRVA, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawENCLogRow> ENCLogTable = new MDTable<RawENCLogRow>(Table.ENCLog, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawENCMapRow> ENCMapTable = new MDTable<RawENCMapRow>(Table.ENCMap, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawAssemblyRow> AssemblyTable = new MDTable<RawAssemblyRow>(Table.Assembly, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawAssemblyProcessorRow> AssemblyProcessorTable = new MDTable<RawAssemblyProcessorRow>(Table.AssemblyProcessor, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawAssemblyOSRow> AssemblyOSTable = new MDTable<RawAssemblyOSRow>(Table.AssemblyOS, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawAssemblyRefRow> AssemblyRefTable = new MDTable<RawAssemblyRefRow>(Table.AssemblyRef, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawAssemblyRefProcessorRow> AssemblyRefProcessorTable = new MDTable<RawAssemblyRefProcessorRow>(Table.AssemblyRefProcessor, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawAssemblyRefOSRow> AssemblyRefOSTable = new MDTable<RawAssemblyRefOSRow>(Table.AssemblyRefOS, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawFileRow> FileTable = new MDTable<RawFileRow>(Table.File, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawExportedTypeRow> ExportedTypeTable = new MDTable<RawExportedTypeRow>(Table.ExportedType, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawManifestResourceRow> ManifestResourceTable = new MDTable<RawManifestResourceRow>(Table.ManifestResource, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawNestedClassRow> NestedClassTable = new MDTable<RawNestedClassRow>(Table.NestedClass, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawGenericParamRow> GenericParamTable = new MDTable<RawGenericParamRow>(Table.GenericParam, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawMethodSpecRow> MethodSpecTable = new MDTable<RawMethodSpecRow>(Table.MethodSpec, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawGenericParamConstraintRow> GenericParamConstraintTable = new MDTable<RawGenericParamConstraintRow>(Table.GenericParamConstraint, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawDocumentRow> DocumentTable = new MDTable<RawDocumentRow>(Table.Document, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawMethodDebugInformationRow> MethodDebugInformationTable = new MDTable<RawMethodDebugInformationRow>(Table.MethodDebugInformation, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawLocalScopeRow> LocalScopeTable = new MDTable<RawLocalScopeRow>(Table.LocalScope, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawLocalVariableRow> LocalVariableTable = new MDTable<RawLocalVariableRow>(Table.LocalVariable, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawLocalConstantRow> LocalConstantTable = new MDTable<RawLocalConstantRow>(Table.LocalConstant, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawImportScopeRow> ImportScopeTable = new MDTable<RawImportScopeRow>(Table.ImportScope, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawStateMachineMethodRow> StateMachineMethodTable = new MDTable<RawStateMachineMethodRow>(Table.StateMachineMethod, RawRowEqualityComparer.Instance);
		public readonly MDTable<RawCustomDebugInformationRow> CustomDebugInformationTable = new MDTable<RawCustomDebugInformationRow>(Table.CustomDebugInformation, RawRowEqualityComparer.Instance);
#pragma warning restore

		/// <summary>
		/// All tables
		/// </summary>
		public readonly IMDTable[] Tables;

		/// <inheritdoc/>
		public string Name => IsENC ? "#-" : "#~";

		/// <inheritdoc/>
		public bool IsEmpty => false;

		/// <summary>
		/// <c>true</c> if the Edit 'N Continue name will be used (#-)
		/// </summary>
		public bool IsENC {
			get {
				if (options.UseENC.HasValue)
					return options.UseENC.Value;
				return hasDeletedRows ||
						!FieldPtrTable.IsEmpty ||
						!MethodPtrTable.IsEmpty ||
						!ParamPtrTable.IsEmpty ||
						!EventPtrTable.IsEmpty ||
						!PropertyPtrTable.IsEmpty ||
						!(InterfaceImplTable.IsEmpty || InterfaceImplTable.IsSorted) ||
						!(ConstantTable.IsEmpty || ConstantTable.IsSorted) ||
						!(CustomAttributeTable.IsEmpty || CustomAttributeTable.IsSorted) ||
						!(FieldMarshalTable.IsEmpty || FieldMarshalTable.IsSorted) ||
						!(DeclSecurityTable.IsEmpty || DeclSecurityTable.IsSorted) ||
						!(ClassLayoutTable.IsEmpty || ClassLayoutTable.IsSorted) ||
						!(FieldLayoutTable.IsEmpty || FieldLayoutTable.IsSorted) ||
						!(EventMapTable.IsEmpty || EventMapTable.IsSorted) ||
						!(PropertyMapTable.IsEmpty || PropertyMapTable.IsSorted) ||
						!(MethodSemanticsTable.IsEmpty || MethodSemanticsTable.IsSorted) ||
						!(MethodImplTable.IsEmpty || MethodImplTable.IsSorted) ||
						!(ImplMapTable.IsEmpty || ImplMapTable.IsSorted) ||
						!(FieldRVATable.IsEmpty || FieldRVATable.IsSorted) ||
						!(NestedClassTable.IsEmpty || NestedClassTable.IsSorted) ||
						!(GenericParamTable.IsEmpty || GenericParamTable.IsSorted) ||
						!(GenericParamConstraintTable.IsEmpty || GenericParamConstraintTable.IsSorted);
			}
		}

		/// <summary>
		/// <c>true</c> if any rows have been deleted (eg. a deleted TypeDef, Method, Field, etc.
		/// Its name has been renamed to _Deleted).
		/// </summary>
		public bool HasDeletedRows {
			get => hasDeletedRows;
			set => hasDeletedRows = value;
		}

		/// <summary>
		/// <c>true</c> if #Strings heap size > <c>0xFFFF</c>
		/// </summary>
		public bool BigStrings {
			get => bigStrings;
			set => bigStrings = value;
		}

		/// <summary>
		/// <c>true</c> if #GUID heap size > <c>0xFFFF</c>
		/// </summary>
		public bool BigGuid {
			get => bigGuid;
			set => bigGuid = value;
		}

		/// <summary>
		/// <c>true</c> if #Blob heap size > <c>0xFFFF</c>
		/// </summary>
		public bool BigBlob {
			get => bigBlob;
			set => bigBlob = value;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="metadata">Metadata owner</param>
		/// <param name="options">Options</param>
		public TablesHeap(Metadata metadata, TablesHeapOptions options) {
			this.metadata = metadata;
			this.options = options ?? new TablesHeapOptions();
			hasDeletedRows = this.options.HasDeletedRows ?? false;
			Tables = new IMDTable[] {
				ModuleTable,
				TypeRefTable,
				TypeDefTable,
				FieldPtrTable,
				FieldTable,
				MethodPtrTable,
				MethodTable,
				ParamPtrTable,
				ParamTable,
				InterfaceImplTable,
				MemberRefTable,
				ConstantTable,
				CustomAttributeTable,
				FieldMarshalTable,
				DeclSecurityTable,
				ClassLayoutTable,
				FieldLayoutTable,
				StandAloneSigTable,
				EventMapTable,
				EventPtrTable,
				EventTable,
				PropertyMapTable,
				PropertyPtrTable,
				PropertyTable,
				MethodSemanticsTable,
				MethodImplTable,
				ModuleRefTable,
				TypeSpecTable,
				ImplMapTable,
				FieldRVATable,
				ENCLogTable,
				ENCMapTable,
				AssemblyTable,
				AssemblyProcessorTable,
				AssemblyOSTable,
				AssemblyRefTable,
				AssemblyRefProcessorTable,
				AssemblyRefOSTable,
				FileTable,
				ExportedTypeTable,
				ManifestResourceTable,
				NestedClassTable,
				GenericParamTable,
				MethodSpecTable,
				GenericParamConstraintTable,
				new MDTable<RawDummyRow>((Table)0x2D, RawDummyRow.Comparer),
				new MDTable<RawDummyRow>((Table)0x2E, RawDummyRow.Comparer),
				new MDTable<RawDummyRow>((Table)0x2F, RawDummyRow.Comparer),
				DocumentTable,
				MethodDebugInformationTable,
				LocalScopeTable,
				LocalVariableTable,
				LocalConstantTable,
				ImportScopeTable,
				StateMachineMethodTable,
				CustomDebugInformationTable,
			};
		}

		struct RawDummyRow {
			public static readonly IEqualityComparer<RawDummyRow> Comparer = new RawDummyRowEqualityComparer();
			sealed class RawDummyRowEqualityComparer : IEqualityComparer<RawDummyRow> {
				public bool Equals(RawDummyRow x, RawDummyRow y) => throw new NotSupportedException();
				public int GetHashCode(RawDummyRow obj) => throw new NotSupportedException();
			}
		}

		/// <inheritdoc/>
		public void SetReadOnly() {
			foreach (var mdt in Tables)
				mdt.SetReadOnly();
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;

			// NOTE: This method can be called twice by NativeModuleWriter, see Metadata.SetOffset() for more info
		}

		/// <inheritdoc/>
		public uint GetFileLength() {
			if (length == 0)
				CalculateLength();
			return Utils.AlignUp(length, HeapBase.ALIGNMENT);
		}

		/// <inheritdoc/>
		public uint GetVirtualSize() => GetFileLength();

		/// <summary>
		/// Calculates the length. This will set all MD tables to read-only.
		/// </summary>
		public void CalculateLength() {
			if (length != 0)
				return;
			SetReadOnly();

			majorVersion = options.MajorVersion ?? 2;
			minorVersion = options.MinorVersion ?? 0;

			if (((majorVersion << 8) | minorVersion) <= 0x100) {
				if (!GenericParamTable.IsEmpty || !MethodSpecTable.IsEmpty || !GenericParamConstraintTable.IsEmpty)
					throw new ModuleWriterException("Tables heap version <= v1.0 but generic tables are not empty");
			}

			var dnTableSizes = new DotNetTableSizes();
			var tableInfos = dnTableSizes.CreateTables(majorVersion, minorVersion);
			var rowCounts = GetRowCounts();
			dnTableSizes.InitializeSizes(bigStrings, bigGuid, bigBlob, systemTables ?? rowCounts, rowCounts, options.ForceBigColumns ?? false);
			for (int i = 0; i < Tables.Length; i++)
				Tables[i].TableInfo = tableInfos[i];

			length = 24;
			foreach (var mdt in Tables) {
				if (mdt.IsEmpty)
					continue;
				length += (uint)(4 + mdt.TableInfo.RowSize * mdt.Rows);
			}
			if (options.ExtraData.HasValue)
				length += 4;
		}

		uint[] GetRowCounts() {
			var sizes = new uint[Tables.Length];
			for (int i = 0; i < sizes.Length; i++)
				sizes[i] = (uint)Tables[i].Rows;
			return sizes;
		}

		internal void GetSystemTableRows(out ulong mask, uint[] tables) {
			if (tables.Length != 0x40)
				throw new InvalidOperationException();
			var tablesMask = GetValidMask();
			ulong bit = 1;
			mask = 0;
			for (int i = 0; i < 0x40; i++, bit <<= 1) {
				var table = (Table)i;
				if (DotNetTableSizes.IsSystemTable(table)) {
					if ((tablesMask & bit) != 0) {
						tables[i] = (uint)Tables[i].Rows;
						mask |= bit;
					}
					else
						tables[i] = 0;
				}
				else
					tables[i] = 0;
			}
		}

		internal void SetSystemTableRows(uint[] systemTables) => this.systemTables = (uint[])systemTables.Clone();
		uint[] systemTables;

		/// <inheritdoc/>
		public void WriteTo(DataWriter writer) {
			writer.WriteUInt32(options.Reserved1 ?? 0);
			writer.WriteByte(majorVersion);
			writer.WriteByte(minorVersion);
			writer.WriteByte((byte)GetMDStreamFlags());
			writer.WriteByte(GetLog2Rid());
			writer.WriteUInt64(GetValidMask());
			writer.WriteUInt64(GetSortedMask());
			foreach (var mdt in Tables) {
				if (!mdt.IsEmpty)
					writer.WriteInt32(mdt.Rows);
			}
			if (options.ExtraData.HasValue)
				writer.WriteUInt32(options.ExtraData.Value);

			writer.Write(metadata, ModuleTable);
			writer.Write(metadata, TypeRefTable);
			writer.Write(metadata, TypeDefTable);
			writer.Write(metadata, FieldPtrTable);
			writer.Write(metadata, FieldTable);
			writer.Write(metadata, MethodPtrTable);
			writer.Write(metadata, MethodTable);
			writer.Write(metadata, ParamPtrTable);
			writer.Write(metadata, ParamTable);
			writer.Write(metadata, InterfaceImplTable);
			writer.Write(metadata, MemberRefTable);
			writer.Write(metadata, ConstantTable);
			writer.Write(metadata, CustomAttributeTable);
			writer.Write(metadata, FieldMarshalTable);
			writer.Write(metadata, DeclSecurityTable);
			writer.Write(metadata, ClassLayoutTable);
			writer.Write(metadata, FieldLayoutTable);
			writer.Write(metadata, StandAloneSigTable);
			writer.Write(metadata, EventMapTable);
			writer.Write(metadata, EventPtrTable);
			writer.Write(metadata, EventTable);
			writer.Write(metadata, PropertyMapTable);
			writer.Write(metadata, PropertyPtrTable);
			writer.Write(metadata, PropertyTable);
			writer.Write(metadata, MethodSemanticsTable);
			writer.Write(metadata, MethodImplTable);
			writer.Write(metadata, ModuleRefTable);
			writer.Write(metadata, TypeSpecTable);
			writer.Write(metadata, ImplMapTable);
			writer.Write(metadata, FieldRVATable);
			writer.Write(metadata, ENCLogTable);
			writer.Write(metadata, ENCMapTable);
			writer.Write(metadata, AssemblyTable);
			writer.Write(metadata, AssemblyProcessorTable);
			writer.Write(metadata, AssemblyOSTable);
			writer.Write(metadata, AssemblyRefTable);
			writer.Write(metadata, AssemblyRefProcessorTable);
			writer.Write(metadata, AssemblyRefOSTable);
			writer.Write(metadata, FileTable);
			writer.Write(metadata, ExportedTypeTable);
			writer.Write(metadata, ManifestResourceTable);
			writer.Write(metadata, NestedClassTable);
			writer.Write(metadata, GenericParamTable);
			writer.Write(metadata, MethodSpecTable);
			writer.Write(metadata, GenericParamConstraintTable);
			writer.Write(metadata, DocumentTable);
			writer.Write(metadata, MethodDebugInformationTable);
			writer.Write(metadata, LocalScopeTable);
			writer.Write(metadata, LocalVariableTable);
			writer.Write(metadata, LocalConstantTable);
			writer.Write(metadata, ImportScopeTable);
			writer.Write(metadata, StateMachineMethodTable);
			writer.Write(metadata, CustomDebugInformationTable);
			writer.WriteZeroes((int)(Utils.AlignUp(length, HeapBase.ALIGNMENT) - length));
		}

		MDStreamFlags GetMDStreamFlags() {
			MDStreamFlags flags = 0;
			if (bigStrings)
				flags |= MDStreamFlags.BigStrings;
			if (bigGuid)
				flags |= MDStreamFlags.BigGUID;
			if (bigBlob)
				flags |= MDStreamFlags.BigBlob;
			if (options.ExtraData.HasValue)
				flags |= MDStreamFlags.ExtraData;
			if (hasDeletedRows)
				flags |= MDStreamFlags.HasDelete;
			return flags;
		}

		byte GetLog2Rid() {
			//TODO: Sometimes this is 16. Probably when at least one of the table indexes requires 4 bytes.
			return options.Log2Rid ?? 1;
		}

		ulong GetValidMask() {
			ulong mask = 0;
			foreach (var mdt in Tables) {
				if (!mdt.IsEmpty)
					mask |= 1UL << (int)mdt.Table;
			}
			return mask;
		}

		ulong GetSortedMask() {
			ulong mask = 0;
			foreach (var mdt in Tables) {
				if (mdt.IsSorted)
					mask |= 1UL << (int)mdt.Table;
			}
			return mask;
		}

		/// <inheritdoc/>
		public override string ToString() => Name;
	}
}
