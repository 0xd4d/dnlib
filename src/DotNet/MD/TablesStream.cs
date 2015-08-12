// dnlib: See LICENSE.txt for more info

using System;
using dnlib.IO;
using dnlib.PE;
using dnlib.Threading;

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

		HotTableStream hotTableStream;
		IColumnReader columnReader;
		IRowReader<RawMethodRow> methodRowReader;

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
#pragma warning restore

#if THREAD_SAFE
		internal readonly Lock theLock = Lock.Create();
#endif

		internal HotTableStream HotTableStream {
			set { hotTableStream = value; }
		}

		/// <summary>
		/// Gets/sets the column reader
		/// </summary>
		public IColumnReader ColumnReader {
			get { return columnReader; }
			set { columnReader = value; }
		}

		/// <summary>
		/// Gets/sets the <c>Method</c> table reader
		/// </summary>
		public IRowReader<RawMethodRow> MethodRowReader {
			get { return methodRowReader; }
			set { methodRowReader = value; }
		}

		/// <summary>
		/// Gets the reserved field
		/// </summary>
		public uint Reserved1 {
			get { return reserved1; }
		}

		/// <summary>
		/// Gets the version. The major version is in the upper 8 bits, and the minor version
		/// is in the lower 8 bits.
		/// </summary>
		public ushort Version {
			get { return (ushort)((majorVersion << 8) | minorVersion); }
		}

		/// <summary>
		/// Gets <see cref="MDStreamFlags"/>
		/// </summary>
		public MDStreamFlags Flags {
			get { return flags; }
		}

		/// <summary>
		/// Gets the reserved log2 rid field
		/// </summary>
		public byte Log2Rid {
			get { return log2Rid; }
		}

		/// <summary>
		/// Gets the valid mask
		/// </summary>
		public ulong ValidMask {
			get { return validMask; }
		}

		/// <summary>
		/// Gets the sorted mask
		/// </summary>
		public ulong SortedMask {
			get { return sortedMask; }
		}

		/// <summary>
		/// Gets the extra data
		/// </summary>
		public uint ExtraData {
			get { return extraData; }
		}

		/// <summary>
		/// Gets the MD tables
		/// </summary>
		public MDTable[] MDTables {
			get { return mdTables; }
		}

		/// <summary>
		/// Gets the <see cref="MDStreamFlags.BigStrings"/> bit
		/// </summary>
		public bool HasBigStrings {
			get { return (flags & MDStreamFlags.BigStrings) != 0; }
		}

		/// <summary>
		/// Gets the <see cref="MDStreamFlags.BigGUID"/> bit
		/// </summary>
		public bool HasBigGUID {
			get { return (flags & MDStreamFlags.BigGUID) != 0; }
		}

		/// <summary>
		/// Gets the <see cref="MDStreamFlags.BigBlob"/> bit
		/// </summary>
		public bool HasBigBlob {
			get { return (flags & MDStreamFlags.BigBlob) != 0; }
		}

		/// <summary>
		/// Gets the <see cref="MDStreamFlags.Padding"/> bit
		/// </summary>
		public bool HasPadding {
			get { return (flags & MDStreamFlags.Padding) != 0; }
		}

		/// <summary>
		/// Gets the <see cref="MDStreamFlags.DeltaOnly"/> bit
		/// </summary>
		public bool HasDeltaOnly {
			get { return (flags & MDStreamFlags.DeltaOnly) != 0; }
		}

		/// <summary>
		/// Gets the <see cref="MDStreamFlags.ExtraData"/> bit
		/// </summary>
		public bool HasExtraData {
			get { return (flags & MDStreamFlags.ExtraData) != 0; }
		}

		/// <summary>
		/// Gets the <see cref="MDStreamFlags.HasDelete"/> bit
		/// </summary>
		public bool HasDelete {
			get { return (flags & MDStreamFlags.HasDelete) != 0; }
		}

		/// <inheritdoc/>
		public TablesStream(IImageStream imageStream, StreamHeader streamHeader)
			: base(imageStream, streamHeader) {
		}

		/// <summary>
		/// Initializes MD tables
		/// </summary>
		/// <param name="peImage">The PEImage</param>
		public void Initialize(IPEImage peImage) {
			if (initialized)
				throw new Exception("Initialize() has already been called");
			initialized = true;

			reserved1 = imageStream.ReadUInt32();
			majorVersion = imageStream.ReadByte();
			minorVersion = imageStream.ReadByte();
			flags = (MDStreamFlags)imageStream.ReadByte();
			log2Rid = imageStream.ReadByte();
			validMask = imageStream.ReadUInt64();
			sortedMask = imageStream.ReadUInt64();

			int maxPresentTables;
			var dnTableSizes = new DotNetTableSizes();
			var tableInfos = dnTableSizes.CreateTables(majorVersion, minorVersion, out maxPresentTables);
			mdTables = new MDTable[tableInfos.Length];

			ulong valid = validMask;
			var sizes = new uint[64];
			for (int i = 0; i < 64; valid >>= 1, i++) {
				uint rows = (valid & 1) == 0 ? 0 : imageStream.ReadUInt32();
				if (i >= maxPresentTables)
					rows = 0;
				sizes[i] = rows;
				if (i < mdTables.Length)
					mdTables[i] = new MDTable((Table)i, rows, tableInfos[i]);
			}

			if (HasExtraData)
				extraData = imageStream.ReadUInt32();

			dnTableSizes.InitializeSizes(HasBigStrings, HasBigGUID, HasBigBlob, sizes);

			var currentRva = peImage.ToRVA(imageStream.FileOffset) + (uint)imageStream.Position;
			foreach (var mdTable in mdTables) {
				var dataLen = (long)mdTable.TableInfo.RowSize * (long)mdTable.Rows;
				mdTable.ImageStream = peImage.CreateStream(currentRva, dataLen);
				var newRva = currentRva + (uint)dataLen;
				if (newRva < currentRva)
					throw new BadImageFormatException("Too big MD table");
				currentRva = newRva;
			}

			InitializeTables();
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
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing) {
			if (disposing) {
				var mt = mdTables;
				if (mt != null) {
					foreach (var mdTable in mt) {
						if (mdTable != null)
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
		public bool HasTable(Table table) {
			return (uint)table < (uint)mdTables.Length;
		}

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
