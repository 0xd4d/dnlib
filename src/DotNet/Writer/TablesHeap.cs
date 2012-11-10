using System.Collections.Generic;
using System.IO;
using dot10.IO;
using dot10.PE;
using dot10.DotNet.MD;

namespace dot10.DotNet.Writer {
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
		/// <c>true</c> if the Edit N' Continue stream header should be used (#-) instead of
		/// the normal compressed stream (#~).
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
		TablesHeapOptions options;
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
				if (options.UseENC ?? false)
					return true;
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
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;
		}

		/// <inheritdoc/>
		public uint GetLength() {
			if (length == 0)
				CalculateLength();
			return Utils.AlignUp(length, HeapBase.ALIGNMENT);
		}

		void CalculateLength() {
			foreach (var mdt in Tables)
				mdt.SetReadOnly();

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

			WriteModuleTable(writer);
			WriteTypeRefTable(writer);
			WriteTypeDefTable(writer);
			WriteFieldPtrTable(writer);
			WriteFieldTable(writer);
			WriteMethodPtrTable(writer);
			WriteMethodTable(writer);
			WriteParamPtrTable(writer);
			WriteParamTable(writer);
			WriteInterfaceImplTable(writer);
			WriteMemberRefTable(writer);
			WriteConstantTable(writer);
			WriteCustomAttributeTable(writer);
			WriteFieldMarshalTable(writer);
			WriteDeclSecurityTable(writer);
			WriteClassLayoutTable(writer);
			WriteFieldLayoutTable(writer);
			WriteStandAloneSigTable(writer);
			WriteEventMapTable(writer);
			WriteEventPtrTable(writer);
			WriteEventTable(writer);
			WritePropertyMapTable(writer);
			WritePropertyPtrTable(writer);
			WritePropertyTable(writer);
			WriteMethodSemanticsTable(writer);
			WriteMethodImplTable(writer);
			WriteModuleRefTable(writer);
			WriteTypeSpecTable(writer);
			WriteImplMapTable(writer);
			WriteFieldRVATable(writer);
			WriteENCLogTable(writer);
			WriteENCMapTable(writer);
			WriteAssemblyTable(writer);
			WriteAssemblyProcessorTable(writer);
			WriteAssemblyOSTable(writer);
			WriteAssemblyRefTable(writer);
			WriteAssemblyRefProcessorTable(writer);
			WriteAssemblyRefOSTable(writer);
			WriteFileTable(writer);
			WriteExportedTypeTable(writer);
			WriteManifestResourceTable(writer);
			WriteNestedClassTable(writer);
			WriteGenericParamTable(writer);
			WriteMethodSpecTable(writer);
			WriteGenericParamConstraintTable(writer);
			writer.WriteZeros((int)(Utils.AlignUp(length, HeapBase.ALIGNMENT) - length));
		}

		void WriteModuleTable(BinaryWriter writer) {
			var table = ModuleTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.Generation);
				cols[1].Write(writer, row.Name);
				cols[2].Write(writer, row.Mvid);
				cols[3].Write(writer, row.EncId);
				cols[4].Write(writer, row.EncBaseId);
			}
		}

		void WriteTypeRefTable(BinaryWriter writer) {
			var table = TypeRefTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.ResolutionScope);
				cols[1].Write(writer, row.Name);
				cols[2].Write(writer, row.Namespace);
			}
		}

		void WriteTypeDefTable(BinaryWriter writer) {
			var table = TypeDefTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.Flags);
				cols[1].Write(writer, row.Name);
				cols[2].Write(writer, row.Namespace);
				cols[3].Write(writer, row.Extends);
				cols[4].Write(writer, row.FieldList);
				cols[5].Write(writer, row.MethodList);
			}
		}

		void WriteFieldPtrTable(BinaryWriter writer) {
			var table = FieldPtrTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table)
				cols[0].Write(writer, row.Field);
		}

		void WriteFieldTable(BinaryWriter writer) {
			var table = FieldTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.Flags);
				cols[1].Write(writer, row.Name);
				cols[2].Write(writer, row.Signature);
			}
		}

		void WriteMethodPtrTable(BinaryWriter writer) {
			var table = MethodPtrTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table)
				cols[0].Write(writer, row.Method);
		}

		void WriteMethodTable(BinaryWriter writer) {
			var table = MethodTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.RVA);
				writer.Write(row.ImplFlags);
				writer.Write(row.Flags);
				cols[3].Write(writer, row.Name);
				cols[4].Write(writer, row.Signature);
				cols[5].Write(writer, row.ParamList);
			}
		}

		void WriteParamPtrTable(BinaryWriter writer) {
			var table = ParamPtrTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table)
				cols[0].Write(writer, row.Param);
		}

		void WriteParamTable(BinaryWriter writer) {
			var table = ParamTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.Flags);
				writer.Write(row.Sequence);
				cols[2].Write(writer, row.Name);
			}
		}

		void WriteInterfaceImplTable(BinaryWriter writer) {
			var table = InterfaceImplTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.Class);
				cols[1].Write(writer, row.Interface);
			}
		}

		void WriteMemberRefTable(BinaryWriter writer) {
			var table = MemberRefTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.Class);
				cols[1].Write(writer, row.Name);
				cols[2].Write(writer, row.Signature);
			}
		}

		void WriteConstantTable(BinaryWriter writer) {
			var table = ConstantTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.Type);
				writer.Write(row.Padding);
				cols[1].Write(writer, row.Parent);
				cols[2].Write(writer, row.Value);
			}
		}

		void WriteCustomAttributeTable(BinaryWriter writer) {
			var table = CustomAttributeTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.Parent);
				cols[1].Write(writer, row.Type);
				cols[2].Write(writer, row.Value);
			}
		}

		void WriteFieldMarshalTable(BinaryWriter writer) {
			var table = FieldMarshalTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.Parent);
				cols[1].Write(writer, row.NativeType);
			}
		}

		void WriteDeclSecurityTable(BinaryWriter writer) {
			var table = DeclSecurityTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.Action);
				cols[1].Write(writer, row.Parent);
				cols[2].Write(writer, row.PermissionSet);
			}
		}

		void WriteClassLayoutTable(BinaryWriter writer) {
			var table = ClassLayoutTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.PackingSize);
				writer.Write(row.ClassSize);
				cols[2].Write(writer, row.Parent);
			}
		}

		void WriteFieldLayoutTable(BinaryWriter writer) {
			var table = FieldLayoutTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.OffSet);
				cols[1].Write(writer, row.Field);
			}
		}

		void WriteStandAloneSigTable(BinaryWriter writer) {
			var table = StandAloneSigTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table)
				cols[0].Write(writer, row.Signature);
		}

		void WriteEventMapTable(BinaryWriter writer) {
			var table = EventMapTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.Parent);
				cols[1].Write(writer, row.EventList);
			}
		}

		void WriteEventPtrTable(BinaryWriter writer) {
			var table = EventPtrTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table)
				cols[0].Write(writer, row.Event);
		}

		void WriteEventTable(BinaryWriter writer) {
			var table = EventTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.EventFlags);
				cols[1].Write(writer, row.Name);
				cols[2].Write(writer, row.EventType);
			}
		}

		void WritePropertyMapTable(BinaryWriter writer) {
			var table = PropertyMapTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.Parent);
				cols[1].Write(writer, row.PropertyList);
			}
		}

		void WritePropertyPtrTable(BinaryWriter writer) {
			var table = PropertyPtrTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table)
				cols[0].Write(writer, row.Property);
		}

		void WritePropertyTable(BinaryWriter writer) {
			var table = PropertyTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.PropFlags);
				cols[1].Write(writer, row.Name);
				cols[2].Write(writer, row.Type);
			}
		}

		void WriteMethodSemanticsTable(BinaryWriter writer) {
			var table = MethodSemanticsTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.Semantic);
				cols[1].Write(writer, row.Method);
				cols[2].Write(writer, row.Association);
			}
		}

		void WriteMethodImplTable(BinaryWriter writer) {
			var table = MethodImplTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.Class);
				cols[1].Write(writer, row.MethodBody);
				cols[2].Write(writer, row.MethodDeclaration);
			}
		}

		void WriteModuleRefTable(BinaryWriter writer) {
			var table = ModuleRefTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table)
				cols[0].Write(writer, row.Name);
		}

		void WriteTypeSpecTable(BinaryWriter writer) {
			var table = TypeSpecTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table)
				cols[0].Write(writer, row.Signature);
		}

		void WriteImplMapTable(BinaryWriter writer) {
			var table = ImplMapTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.MappingFlags);
				cols[1].Write(writer, row.MemberForwarded);
				cols[2].Write(writer, row.ImportName);
				cols[3].Write(writer, row.ImportScope);
			}
		}

		void WriteFieldRVATable(BinaryWriter writer) {
			var table = FieldRVATable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.RVA);
				cols[1].Write(writer, row.Field);
			}
		}

		void WriteENCLogTable(BinaryWriter writer) {
			var table = ENCLogTable;
			foreach (var row in table) {
				writer.Write(row.Token);
				writer.Write(row.FuncCode);
			}
		}

		void WriteENCMapTable(BinaryWriter writer) {
			var table = ENCMapTable;
			foreach (var row in table)
				writer.Write(row.Token);
		}

		void WriteAssemblyTable(BinaryWriter writer) {
			var table = AssemblyTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.HashAlgId);
				writer.Write(row.MajorVersion);
				writer.Write(row.MinorVersion);
				writer.Write(row.BuildNumber);
				writer.Write(row.RevisionNumber);
				writer.Write(row.Flags);
				cols[6].Write(writer, row.PublicKey);
				cols[7].Write(writer, row.Name);
				cols[8].Write(writer, row.Locale);
			}
		}

		void WriteAssemblyProcessorTable(BinaryWriter writer) {
			var table = AssemblyProcessorTable;
			foreach (var row in table)
				writer.Write(row.Processor);
		}

		void WriteAssemblyOSTable(BinaryWriter writer) {
			var table = AssemblyOSTable;
			foreach (var row in table) {
				writer.Write(row.OSPlatformId);
				writer.Write(row.OSMajorVersion);
				writer.Write(row.OSMinorVersion);
			}
		}

		void WriteAssemblyRefTable(BinaryWriter writer) {
			var table = AssemblyRefTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.MajorVersion);
				writer.Write(row.MinorVersion);
				writer.Write(row.BuildNumber);
				writer.Write(row.RevisionNumber);
				writer.Write(row.Flags);
				cols[5].Write(writer, row.PublicKeyOrToken);
				cols[6].Write(writer, row.Name);
				cols[7].Write(writer, row.Locale);
				cols[8].Write(writer, row.HashValue);
			}
		}

		void WriteAssemblyRefProcessorTable(BinaryWriter writer) {
			var table = AssemblyRefProcessorTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.Processor);
				cols[1].Write(writer, row.AssemblyRef);
			}
		}

		void WriteAssemblyRefOSTable(BinaryWriter writer) {
			var table = AssemblyRefOSTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.OSPlatformId);
				writer.Write(row.OSMajorVersion);
				writer.Write(row.OSMinorVersion);
				cols[3].Write(writer, row.AssemblyRef);
			}
		}

		void WriteFileTable(BinaryWriter writer) {
			var table = FileTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.Flags);
				cols[1].Write(writer, row.Name);
				cols[2].Write(writer, row.HashValue);
			}
		}

		void WriteExportedTypeTable(BinaryWriter writer) {
			var table = ExportedTypeTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.Flags);
				writer.Write(row.TypeDefId);
				cols[2].Write(writer, row.TypeName);
				cols[3].Write(writer, row.TypeNamespace);
				cols[4].Write(writer, row.Implementation);
			}
		}

		void WriteManifestResourceTable(BinaryWriter writer) {
			var table = ManifestResourceTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				writer.Write(row.Offset);
				writer.Write(row.Flags);
				cols[2].Write(writer, row.Name);
				cols[3].Write(writer, row.Implementation);
			}
		}

		void WriteNestedClassTable(BinaryWriter writer) {
			var table = NestedClassTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.NestedClass);
				cols[1].Write(writer, row.EnclosingClass);
			}
		}

		void WriteGenericParamTable(BinaryWriter writer) {
			var table = GenericParamTable;
			var cols = table.TableInfo.Columns;
			bool useKindColumn = majorVersion == 1 && minorVersion == 1;
			foreach (var row in table) {
				writer.Write(row.Number);
				writer.Write(row.Flags);
				cols[2].Write(writer, row.Owner);
				cols[3].Write(writer, row.Name);
				if (useKindColumn)
					cols[4].Write(writer, row.Kind);
			}
		}

		void WriteMethodSpecTable(BinaryWriter writer) {
			var table = MethodSpecTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.Method);
				cols[1].Write(writer, row.Instantiation);
			}
		}

		void WriteGenericParamConstraintTable(BinaryWriter writer) {
			var table = GenericParamConstraintTable;
			var cols = table.TableInfo.Columns;
			foreach (var row in table) {
				cols[0].Write(writer, row.Owner);
				cols[1].Write(writer, row.Constraint);
			}
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
