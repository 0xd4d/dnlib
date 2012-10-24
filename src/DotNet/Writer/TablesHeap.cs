using System.IO;
using dot10.IO;
using dot10.PE;
using dot10.DotNet.MD;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// <see cref="TablesHeap"/> options
	/// </summary>
	public class TablesHeapOptions {
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
	}

	/// <summary>
	/// Contains all .NET tables
	/// </summary>
	public sealed class TablesHeap : IHeap {
		uint length;
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
				return HasDeletedRows ||
						!FieldPtrTable.IsEmpty ||
						!MethodPtrTable.IsEmpty ||
						!ParamPtrTable.IsEmpty ||
						!EventPtrTable.IsEmpty ||
						!PropertyPtrTable.IsEmpty;
			}
		}

		/// <summary>
		/// <c>true</c> if any rows have been deleted (eg. a deleted TypeDef, Method, Field, etc.
		/// Its name has been renamed to _Deleted).
		/// </summary>
		public bool HasDeletedRows {
			get { return false; }
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
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;
			//TODO:
		}

		/// <inheritdoc/>
		public uint GetLength() {
			if (length == 0)
				CalculateLength();
			return length;
		}

		void CalculateLength() {
			//TODO:
			length = 24;
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			//TODO:

			//TODO: Throw if <= v1.0 and generic tables are not empty
			//TODO: Use HasDeletedRows to set the Deleted flag
		}

		/// <inheritdoc/>
		public override string ToString() {
			return Name;
		}
	}
}
