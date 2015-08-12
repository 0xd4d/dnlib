// dnlib: See LICENSE.txt for more info

using System.IO;
using dnlib.IO;
using dnlib.PE;
using dnlib.DotNet.MD;

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
		/// Extra data to write
		/// </summary>
		public uint? ExtraData;

		/// <summary>
		/// <c>true</c> if there are deleted <see cref="TypeDef"/>s, <see cref="ExportedType"/>s,
		/// <see cref="FieldDef"/>s, <see cref="MethodDef"/>s, <see cref="EventDef"/>s and/or
		/// <see cref="PropertyDef"/>s.
		/// </summary>
		public bool? HasDeletedRows;
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
		readonly TablesHeapOptions options;
		FileOffset offset;
		RVA rva;

		/// <inheritdoc/>
		public FileOffset FileOffset {
			get { return offset; }
		}

		/// <inheritdoc/>
		public RVA RVA {
			get { return rva; }
		}

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
#pragma warning restore

		/// <summary>
		/// All tables
		/// </summary>
		public readonly IMDTable[] Tables;

		/// <inheritdoc/>
		public string Name {
			get { return IsENC ? "#-" : "#~"; }
		}

		/// <inheritdoc/>
		public bool IsEmpty {
			get { return false; }
		}

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
			get { return hasDeletedRows; }
			set { hasDeletedRows = value; }
		}

		/// <summary>
		/// <c>true</c> if #Strings heap size > <c>0xFFFF</c>
		/// </summary>
		public bool BigStrings {
			get { return bigStrings; }
			set { bigStrings = value; }
		}

		/// <summary>
		/// <c>true</c> if #GUID heap size > <c>0xFFFF</c>
		/// </summary>
		public bool BigGuid {
			get { return bigGuid; }
			set { bigGuid = value; }
		}

		/// <summary>
		/// <c>true</c> if #Blob heap size > <c>0xFFFF</c>
		/// </summary>
		public bool BigBlob {
			get { return bigBlob; }
			set { bigBlob = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public TablesHeap()
			: this(null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options">Options</param>
		public TablesHeap(TablesHeapOptions options) {
			this.options = options ?? new TablesHeapOptions();
			this.hasDeletedRows = this.options.HasDeletedRows ?? false;
			this.Tables = new IMDTable[] {
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
			};
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
		}

		/// <inheritdoc/>
		public uint GetFileLength() {
			if (length == 0)
				CalculateLength();
			return Utils.AlignUp(length, HeapBase.ALIGNMENT);
		}

		/// <inheritdoc/>
		public uint GetVirtualSize() {
			return GetFileLength();
		}

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
			dnTableSizes.InitializeSizes(bigStrings, bigGuid, bigBlob, GetRowCounts());
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

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			writer.Write(options.Reserved1 ?? 0);
			writer.Write(majorVersion);
			writer.Write(minorVersion);
			writer.Write((byte)GetMDStreamFlags());
			writer.Write(GetLog2Rid());
			writer.Write(GetValidMask());
			writer.Write(GetSortedMask());
			foreach (var mdt in Tables) {
				if (!mdt.IsEmpty)
					writer.Write(mdt.Rows);
			}
			if (options.ExtraData.HasValue)
				writer.Write(options.ExtraData.Value);

			writer.Write(ModuleTable);
			writer.Write(TypeRefTable);
			writer.Write(TypeDefTable);
			writer.Write(FieldPtrTable);
			writer.Write(FieldTable);
			writer.Write(MethodPtrTable);
			writer.Write(MethodTable);
			writer.Write(ParamPtrTable);
			writer.Write(ParamTable);
			writer.Write(InterfaceImplTable);
			writer.Write(MemberRefTable);
			writer.Write(ConstantTable);
			writer.Write(CustomAttributeTable);
			writer.Write(FieldMarshalTable);
			writer.Write(DeclSecurityTable);
			writer.Write(ClassLayoutTable);
			writer.Write(FieldLayoutTable);
			writer.Write(StandAloneSigTable);
			writer.Write(EventMapTable);
			writer.Write(EventPtrTable);
			writer.Write(EventTable);
			writer.Write(PropertyMapTable);
			writer.Write(PropertyPtrTable);
			writer.Write(PropertyTable);
			writer.Write(MethodSemanticsTable);
			writer.Write(MethodImplTable);
			writer.Write(ModuleRefTable);
			writer.Write(TypeSpecTable);
			writer.Write(ImplMapTable);
			writer.Write(FieldRVATable);
			writer.Write(ENCLogTable);
			writer.Write(ENCMapTable);
			writer.Write(AssemblyTable);
			writer.Write(AssemblyProcessorTable);
			writer.Write(AssemblyOSTable);
			writer.Write(AssemblyRefTable);
			writer.Write(AssemblyRefProcessorTable);
			writer.Write(AssemblyRefOSTable);
			writer.Write(FileTable);
			writer.Write(ExportedTypeTable);
			writer.Write(ManifestResourceTable);
			writer.Write(NestedClassTable);
			writer.Write(GenericParamTable);
			writer.Write(MethodSpecTable);
			writer.Write(GenericParamConstraintTable);
			writer.WriteZeros((int)(Utils.AlignUp(length, HeapBase.ALIGNMENT) - length));
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
			return 1;
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
		public override string ToString() {
			return Name;
		}
	}
}
