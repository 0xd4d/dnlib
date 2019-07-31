// dnlib: See LICENSE.txt for more info

using System;
using dnlib.IO;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// .NET metadata tables stream
	/// </summary>
	public sealed partial class TablesStream : DotNetStream {
		bool initialized;
		uint reserved1;
		byte majorVersion;
		byte minorVersion;
		MDStreamFlags flags;
		byte log2Rid;
		ulong validMask;
		ulong sortedMask;
		uint extraData;
		MDTable[] mdTables;
		uint mdTablesPos;

		IColumnReader columnReader;
		IRowReader<RawMethodRow> methodRowReader;
		readonly CLRRuntimeReaderKind runtime;

#pragma warning disable 1591	// XML doc comment
		public MDTable ModuleTable { get; private set; }
		public MDTable TypeRefTable { get; private set; }
		public MDTable TypeDefTable { get; private set; }
		public MDTable FieldPtrTable { get; private set; }
		public MDTable FieldTable { get; private set; }
		public MDTable MethodPtrTable { get; private set; }
		public MDTable MethodTable { get; private set; }
		public MDTable ParamPtrTable { get; private set; }
		public MDTable ParamTable { get; private set; }
		public MDTable InterfaceImplTable { get; private set; }
		public MDTable MemberRefTable { get; private set; }
		public MDTable ConstantTable { get; private set; }
		public MDTable CustomAttributeTable { get; private set; }
		public MDTable FieldMarshalTable { get; private set; }
		public MDTable DeclSecurityTable { get; private set; }
		public MDTable ClassLayoutTable { get; private set; }
		public MDTable FieldLayoutTable { get; private set; }
		public MDTable StandAloneSigTable { get; private set; }
		public MDTable EventMapTable { get; private set; }
		public MDTable EventPtrTable { get; private set; }
		public MDTable EventTable { get; private set; }
		public MDTable PropertyMapTable { get; private set; }
		public MDTable PropertyPtrTable { get; private set; }
		public MDTable PropertyTable { get; private set; }
		public MDTable MethodSemanticsTable { get; private set; }
		public MDTable MethodImplTable { get; private set; }
		public MDTable ModuleRefTable { get; private set; }
		public MDTable TypeSpecTable { get; private set; }
		public MDTable ImplMapTable { get; private set; }
		public MDTable FieldRVATable { get; private set; }
		public MDTable ENCLogTable { get; private set; }
		public MDTable ENCMapTable { get; private set; }
		public MDTable AssemblyTable { get; private set; }
		public MDTable AssemblyProcessorTable { get; private set; }
		public MDTable AssemblyOSTable { get; private set; }
		public MDTable AssemblyRefTable { get; private set; }
		public MDTable AssemblyRefProcessorTable { get; private set; }
		public MDTable AssemblyRefOSTable { get; private set; }
		public MDTable FileTable { get; private set; }
		public MDTable ExportedTypeTable { get; private set; }
		public MDTable ManifestResourceTable { get; private set; }
		public MDTable NestedClassTable { get; private set; }
		public MDTable GenericParamTable { get; private set; }
		public MDTable MethodSpecTable { get; private set; }
		public MDTable GenericParamConstraintTable { get; private set; }
		public MDTable DocumentTable { get; private set; }
		public MDTable MethodDebugInformationTable { get; private set; }
		public MDTable LocalScopeTable { get; private set; }
		public MDTable LocalVariableTable { get; private set; }
		public MDTable LocalConstantTable { get; private set; }
		public MDTable ImportScopeTable { get; private set; }
		public MDTable StateMachineMethodTable { get; private set; }
		public MDTable CustomDebugInformationTable { get; private set; }
#pragma warning restore

		/// <summary>
		/// Gets/sets the column reader
		/// </summary>
		public IColumnReader ColumnReader {
			get => columnReader;
			set => columnReader = value;
		}

		/// <summary>
		/// Gets/sets the <c>Method</c> table reader
		/// </summary>
		public IRowReader<RawMethodRow> MethodRowReader {
			get => methodRowReader;
			set => methodRowReader = value;
		}

		/// <summary>
		/// Gets the reserved field
		/// </summary>
		public uint Reserved1 => reserved1;

		/// <summary>
		/// Gets the version. The major version is in the upper 8 bits, and the minor version
		/// is in the lower 8 bits.
		/// </summary>
		public ushort Version => (ushort)((majorVersion << 8) | minorVersion);

		/// <summary>
		/// Gets <see cref="MDStreamFlags"/>
		/// </summary>
		public MDStreamFlags Flags => flags;

		/// <summary>
		/// Gets the reserved log2 rid field
		/// </summary>
		public byte Log2Rid => log2Rid;

		/// <summary>
		/// Gets the valid mask
		/// </summary>
		public ulong ValidMask => validMask;

		/// <summary>
		/// Gets the sorted mask
		/// </summary>
		public ulong SortedMask => sortedMask;

		/// <summary>
		/// Gets the extra data
		/// </summary>
		public uint ExtraData => extraData;

		/// <summary>
		/// Gets the MD tables
		/// </summary>
		public MDTable[] MDTables => mdTables;

		/// <summary>
		/// Gets the <see cref="MDStreamFlags.BigStrings"/> bit
		/// </summary>
		public bool HasBigStrings => (flags & MDStreamFlags.BigStrings) != 0;

		/// <summary>
		/// Gets the <see cref="MDStreamFlags.BigGUID"/> bit
		/// </summary>
		public bool HasBigGUID => (flags & MDStreamFlags.BigGUID) != 0;

		/// <summary>
		/// Gets the <see cref="MDStreamFlags.BigBlob"/> bit
		/// </summary>
		public bool HasBigBlob => (flags & MDStreamFlags.BigBlob) != 0;

		/// <summary>
		/// Gets the <see cref="MDStreamFlags.Padding"/> bit
		/// </summary>
		public bool HasPadding => runtime == CLRRuntimeReaderKind.CLR && (flags & MDStreamFlags.Padding) != 0;

		/// <summary>
		/// Gets the <see cref="MDStreamFlags.DeltaOnly"/> bit
		/// </summary>
		public bool HasDeltaOnly => runtime == CLRRuntimeReaderKind.CLR && (flags & MDStreamFlags.DeltaOnly) != 0;

		/// <summary>
		/// Gets the <see cref="MDStreamFlags.ExtraData"/> bit
		/// </summary>
		public bool HasExtraData => runtime == CLRRuntimeReaderKind.CLR && (flags & MDStreamFlags.ExtraData) != 0;

		/// <summary>
		/// Gets the <see cref="MDStreamFlags.HasDelete"/> bit
		/// </summary>
		public bool HasDelete => runtime == CLRRuntimeReaderKind.CLR && (flags & MDStreamFlags.HasDelete) != 0;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="mdReaderFactory"><see cref="DataReader"/> factory</param>
		/// <param name="metadataBaseOffset">Offset of metadata</param>
		/// <param name="streamHeader">Stream header</param>
		public TablesStream(DataReaderFactory mdReaderFactory, uint metadataBaseOffset, StreamHeader streamHeader)
			: this(mdReaderFactory, metadataBaseOffset, streamHeader, CLRRuntimeReaderKind.CLR) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="mdReaderFactory"><see cref="DataReader"/> factory</param>
		/// <param name="metadataBaseOffset">Offset of metadata</param>
		/// <param name="streamHeader">Stream header</param>
		/// <param name="runtime">Runtime kind</param>
		public TablesStream(DataReaderFactory mdReaderFactory, uint metadataBaseOffset, StreamHeader streamHeader, CLRRuntimeReaderKind runtime)
			: base(mdReaderFactory, metadataBaseOffset, streamHeader) {
			this.runtime = runtime;
		}

		/// <summary>
		/// Initializes MD tables
		/// </summary>
		/// <param name="typeSystemTableRows">Type system table rows (from #Pdb stream)</param>
		public void Initialize(uint[] typeSystemTableRows) {
			if (initialized)
				throw new Exception("Initialize() has already been called");
			initialized = true;

			var reader = dataReader;
			reserved1 = reader.ReadUInt32();
			majorVersion = reader.ReadByte();
			minorVersion = reader.ReadByte();
			flags = (MDStreamFlags)reader.ReadByte();
			log2Rid = reader.ReadByte();
			validMask = reader.ReadUInt64();
			sortedMask = reader.ReadUInt64();

			var dnTableSizes = new DotNetTableSizes();
			var tableInfos = dnTableSizes.CreateTables(majorVersion, minorVersion, out int maxPresentTables);
			if (!(typeSystemTableRows is null))
				maxPresentTables = DotNetTableSizes.normalMaxTables;
			mdTables = new MDTable[tableInfos.Length];

			ulong valid = validMask;
			var sizes = new uint[64];
			for (int i = 0; i < 64; valid >>= 1, i++) {
				uint rows = (valid & 1) == 0 ? 0 : reader.ReadUInt32();
				// Mono ignores the high byte
				rows &= 0x00FFFFFF;
				if (i >= maxPresentTables)
					rows = 0;
				sizes[i] = rows;
				if (i < mdTables.Length)
					mdTables[i] = new MDTable((Table)i, rows, tableInfos[i]);
			}

			if (HasExtraData)
				extraData = reader.ReadUInt32();

			var debugSizes = sizes;
			if (!(typeSystemTableRows is null)) {
				debugSizes = new uint[sizes.Length];
				for (int i = 0; i < 64; i++) {
					if (DotNetTableSizes.IsSystemTable((Table)i))
						debugSizes[i] = typeSystemTableRows[i];
					else
						debugSizes[i] = sizes[i];
				}
			}

			dnTableSizes.InitializeSizes(HasBigStrings, HasBigGUID, HasBigBlob, sizes, debugSizes);

			mdTablesPos = reader.Position;
			InitializeMdTableReaders();
			InitializeTables();
		}

		/// <inheritdoc/>
		protected override void OnReaderRecreated() => InitializeMdTableReaders();

		void InitializeMdTableReaders() {
			var reader = dataReader;
			reader.Position = mdTablesPos;
			var currentPos = reader.Position;
			foreach (var mdTable in mdTables) {
				var dataLen = (uint)mdTable.TableInfo.RowSize * mdTable.Rows;
				if (currentPos > reader.Length)
					currentPos = reader.Length;
				if ((ulong)currentPos + dataLen > reader.Length)
					dataLen = reader.Length - currentPos;
				mdTable.DataReader = reader.Slice(currentPos, dataLen);
				var newPos = currentPos + dataLen;
				if (newPos < currentPos)
					throw new BadImageFormatException("Too big MD table");
				currentPos = newPos;
			}
		}

		void InitializeTables() {
			ModuleTable = mdTables[(int)Table.Module];
			TypeRefTable = mdTables[(int)Table.TypeRef];
			TypeDefTable = mdTables[(int)Table.TypeDef];
			FieldPtrTable = mdTables[(int)Table.FieldPtr];
			FieldTable = mdTables[(int)Table.Field];
			MethodPtrTable = mdTables[(int)Table.MethodPtr];
			MethodTable = mdTables[(int)Table.Method];
			ParamPtrTable = mdTables[(int)Table.ParamPtr];
			ParamTable = mdTables[(int)Table.Param];
			InterfaceImplTable = mdTables[(int)Table.InterfaceImpl];
			MemberRefTable = mdTables[(int)Table.MemberRef];
			ConstantTable = mdTables[(int)Table.Constant];
			CustomAttributeTable = mdTables[(int)Table.CustomAttribute];
			FieldMarshalTable = mdTables[(int)Table.FieldMarshal];
			DeclSecurityTable = mdTables[(int)Table.DeclSecurity];
			ClassLayoutTable = mdTables[(int)Table.ClassLayout];
			FieldLayoutTable = mdTables[(int)Table.FieldLayout];
			StandAloneSigTable = mdTables[(int)Table.StandAloneSig];
			EventMapTable = mdTables[(int)Table.EventMap];
			EventPtrTable = mdTables[(int)Table.EventPtr];
			EventTable = mdTables[(int)Table.Event];
			PropertyMapTable = mdTables[(int)Table.PropertyMap];
			PropertyPtrTable = mdTables[(int)Table.PropertyPtr];
			PropertyTable = mdTables[(int)Table.Property];
			MethodSemanticsTable = mdTables[(int)Table.MethodSemantics];
			MethodImplTable = mdTables[(int)Table.MethodImpl];
			ModuleRefTable = mdTables[(int)Table.ModuleRef];
			TypeSpecTable = mdTables[(int)Table.TypeSpec];
			ImplMapTable = mdTables[(int)Table.ImplMap];
			FieldRVATable = mdTables[(int)Table.FieldRVA];
			ENCLogTable = mdTables[(int)Table.ENCLog];
			ENCMapTable = mdTables[(int)Table.ENCMap];
			AssemblyTable = mdTables[(int)Table.Assembly];
			AssemblyProcessorTable = mdTables[(int)Table.AssemblyProcessor];
			AssemblyOSTable = mdTables[(int)Table.AssemblyOS];
			AssemblyRefTable = mdTables[(int)Table.AssemblyRef];
			AssemblyRefProcessorTable = mdTables[(int)Table.AssemblyRefProcessor];
			AssemblyRefOSTable = mdTables[(int)Table.AssemblyRefOS];
			FileTable = mdTables[(int)Table.File];
			ExportedTypeTable = mdTables[(int)Table.ExportedType];
			ManifestResourceTable = mdTables[(int)Table.ManifestResource];
			NestedClassTable = mdTables[(int)Table.NestedClass];
			GenericParamTable = mdTables[(int)Table.GenericParam];
			MethodSpecTable = mdTables[(int)Table.MethodSpec];
			GenericParamConstraintTable = mdTables[(int)Table.GenericParamConstraint];
			DocumentTable = mdTables[(int)Table.Document];
			MethodDebugInformationTable = mdTables[(int)Table.MethodDebugInformation];
			LocalScopeTable = mdTables[(int)Table.LocalScope];
			LocalVariableTable = mdTables[(int)Table.LocalVariable];
			LocalConstantTable = mdTables[(int)Table.LocalConstant];
			ImportScopeTable = mdTables[(int)Table.ImportScope];
			StateMachineMethodTable = mdTables[(int)Table.StateMachineMethod];
			CustomDebugInformationTable = mdTables[(int)Table.CustomDebugInformation];
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing) {
			if (disposing) {
				var mt = mdTables;
				if (!(mt is null)) {
					foreach (var mdTable in mt) {
						if (!(mdTable is null))
							mdTable.Dispose();
					}
					mdTables = null;
				}
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Returns a MD table
		/// </summary>
		/// <param name="table">The table type</param>
		/// <returns>A <see cref="MDTable"/> or <c>null</c> if table doesn't exist</returns>
		public MDTable Get(Table table) {
			int index = (int)table;
			if ((uint)index >= (uint)mdTables.Length)
				return null;
			return mdTables[index];
		}

		/// <summary>
		/// Checks whether a table exists
		/// </summary>
		/// <param name="table">The table type</param>
		/// <returns><c>true</c> if the table exists</returns>
		public bool HasTable(Table table) => (uint)table < (uint)mdTables.Length;

		/// <summary>
		/// Checks whether table <paramref name="table"/> is sorted
		/// </summary>
		/// <param name="table">The table</param>
		public bool IsSorted(MDTable table) {
			int index = (int)table.Table;
			if ((uint)index >= 64)
				return false;
			return (sortedMask & (1UL << index)) != 0;
		}
	}
}
