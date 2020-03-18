// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using dnlib.IO;
using dnlib.PE;
using dnlib.DotNet.MD;
using dnlib.DotNet.Emit;
using System.Diagnostics;
using dnlib.DotNet.Pdb;
using dnlib.DotNet.Pdb.Portable;
using System.Linq;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// <see cref="Metadata"/> flags
	/// </summary>
	[Flags]
	public enum MetadataFlags : uint {
		/// <summary>
		/// Preserves all rids in the <c>TypeRef</c> table
		/// </summary>
		PreserveTypeRefRids = 1,

		/// <summary>
		/// Preserves all rids in the <c>TypeDef</c> table
		/// </summary>
		PreserveTypeDefRids = 2,

		/// <summary>
		/// Preserves all rids in the <c>Field</c> table
		/// </summary>
		PreserveFieldRids = 4,

		/// <summary>
		/// Preserves all rids in the <c>Method</c> table
		/// </summary>
		PreserveMethodRids = 8,

		/// <summary>
		/// Preserves all rids in the <c>Param</c> table
		/// </summary>
		PreserveParamRids = 0x10,

		/// <summary>
		/// Preserves all rids in the <c>MemberRef</c> table
		/// </summary>
		PreserveMemberRefRids = 0x20,

		/// <summary>
		/// Preserves all rids in the <c>StandAloneSig</c> table
		/// </summary>
		PreserveStandAloneSigRids = 0x40,

		/// <summary>
		/// Preserves all rids in the <c>Event</c> table
		/// </summary>
		PreserveEventRids = 0x80,

		/// <summary>
		/// Preserves all rids in the <c>Property</c> table
		/// </summary>
		PreservePropertyRids = 0x100,

		/// <summary>
		/// Preserves all rids in the <c>TypeSpec</c> table
		/// </summary>
		PreserveTypeSpecRids = 0x200,

		/// <summary>
		/// Preserves all rids in the <c>MethodSpec</c> table
		/// </summary>
		PreserveMethodSpecRids = 0x400,

		/// <summary>
		/// Preserves all method rids, i.e., <c>Method</c>, <c>MemberRef</c> and
		/// <c>MethodSpec</c> rids.
		/// </summary>
		PreserveAllMethodRids = PreserveMethodRids | PreserveMemberRefRids | PreserveMethodSpecRids,

		/// <summary>
		/// Preserves all rids in the following tables: <c>TypeRef</c>, <c>TypeDef</c>,
		/// <c>Field</c>, <c>Method</c>, <c>Param</c>, <c>MemberRef</c>, <c>StandAloneSig</c>,
		/// <c>Event</c>, <c>Property</c>, <c>TypeSpec</c>, <c>MethodSpec</c>
		/// </summary>
		PreserveRids =	PreserveTypeRefRids |
						PreserveTypeDefRids |
						PreserveFieldRids |
						PreserveMethodRids |
						PreserveParamRids |
						PreserveMemberRefRids |
						PreserveStandAloneSigRids |
						PreserveEventRids |
						PreservePropertyRids |
						PreserveTypeSpecRids |
						PreserveMethodSpecRids,

		/// <summary>
		/// Preserves all offsets in the #Strings heap (the original #Strings heap will be saved
		/// in the new file). Type names, field names, and other non-user strings are stored
		/// in the #Strings heap.
		/// </summary>
		PreserveStringsOffsets = 0x800,

		/// <summary>
		/// Preserves all offsets in the #US heap (the original #US heap will be saved
		/// in the new file). User strings (referenced by the ldstr instruction) are stored in
		/// the #US heap.
		/// </summary>
		PreserveUSOffsets = 0x1000,

		/// <summary>
		/// Preserves all offsets in the #Blob heap (the original #Blob heap will be saved
		/// in the new file). Custom attributes, signatures and other blobs are stored in the
		/// #Blob heap.
		/// </summary>
		PreserveBlobOffsets = 0x2000,

		/// <summary>
		/// Preserves the extra data that is present after the original signature in the #Blob
		/// heap. This extra data shouldn't be present but might be present if an obfuscator
		/// has added this extra data and is eg. using it to decrypt stuff.
		/// </summary>
		PreserveExtraSignatureData = 0x4000,

		/// <summary>
		/// Preserves as much as possible
		/// </summary>
		PreserveAll = PreserveRids | PreserveStringsOffsets | PreserveUSOffsets |
					PreserveBlobOffsets | PreserveExtraSignatureData,

		/// <summary>
		/// The original method body's max stack field should be used and a new one should not
		/// be calculated.
		/// </summary>
		KeepOldMaxStack = 0x8000,

		/// <summary>
		/// Always create the #GUID heap even if it's empty
		/// </summary>
		AlwaysCreateGuidHeap = 0x10000,

		/// <summary>
		/// Always create the #Strings heap even if it's empty
		/// </summary>
		AlwaysCreateStringsHeap = 0x20000,

		/// <summary>
		/// Always create the #US heap even if it's empty
		/// </summary>
		AlwaysCreateUSHeap = 0x40000,

		/// <summary>
		/// Always create the #Blob heap even if it's empty
		/// </summary>
		AlwaysCreateBlobHeap = 0x80000,

		/// <summary>
		/// Sort the InterfaceImpl table the same way Roslyn sorts it. Roslyn doesn't sort it
		/// according to the ECMA spec, see https://github.com/dotnet/roslyn/issues/3905
		/// </summary>
		RoslynSortInterfaceImpl = 0x100000,

		/// <summary>
		/// Don't write method bodies
		/// </summary>
		NoMethodBodies = 0x200000,

		/// <summary>
		/// Don't write .NET resources
		/// </summary>
		NoDotNetResources = 0x400000,

		/// <summary>
		/// Don't write field data
		/// </summary>
		NoFieldData = 0x800000,

		/// <summary>
		/// Serialized type names stored in custom attributes are optimized if the types
		/// exist in the core library (eg. mscorlib/System.Private.CoreLib).
		/// Instead of storing type-name + assembly-name, only type-name is stored. This results in
		/// slightly smaller assemblies.
		/// <br/>
		/// <br/>
		/// If it's a type in the current module, the type name is optimized and no assembly name is stored in the custom attribute.
		/// <br/>
		/// <br/>
		/// This is disabled by default. It's safe to enable if the reference core assembly
		/// is the same as the runtime core assembly (eg. it's mscorlib.dll and .NET Framework,
		/// but not .NET Core / .NET Standard).
		/// </summary>
		OptimizeCustomAttributeSerializedTypeNames = 0x1000000,
	}

	/// <summary>
	/// Metadata heaps event args
	/// </summary>
	public readonly struct MetadataHeapsAddedEventArgs {
		/// <summary>
		/// Gets the metadata writer
		/// </summary>
		public Metadata Metadata { get; }

		/// <summary>
		/// Gets all heaps
		/// </summary>
		public List<IHeap> Heaps { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="metadata">Metadata writer</param>
		/// <param name="heaps">All heaps</param>
		public MetadataHeapsAddedEventArgs(Metadata metadata, List<IHeap> heaps) {
			Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
			Heaps = heaps ?? throw new ArgumentNullException(nameof(heaps));
		}
	}

	/// <summary>
	/// <see cref="Metadata"/> options
	/// </summary>
	public sealed class MetadataOptions {
		MetadataHeaderOptions metadataHeaderOptions;
		MetadataHeaderOptions debugMetadataHeaderOptions;
		TablesHeapOptions tablesHeapOptions;
		List<IHeap> customHeaps;

		/// <summary>
		/// Gets/sets the <see cref="MetadataHeader"/> options. This is never <c>null</c>.
		/// </summary>
		public MetadataHeaderOptions MetadataHeaderOptions {
			get => metadataHeaderOptions ??= new MetadataHeaderOptions();
			set => metadataHeaderOptions = value;
		}

		/// <summary>
		/// Gets/sets the debug (portable PDB) <see cref="MetadataHeader"/> options. This is never <c>null</c>.
		/// </summary>
		public MetadataHeaderOptions DebugMetadataHeaderOptions {
			get => debugMetadataHeaderOptions ??= MetadataHeaderOptions.CreatePortablePdbV1_0();
			set => debugMetadataHeaderOptions = value;
		}

		/// <summary>
		/// Gets/sets the <see cref="TablesHeap"/> options. This is never <c>null</c>.
		/// </summary>
		public TablesHeapOptions TablesHeapOptions {
			get => tablesHeapOptions ??= new TablesHeapOptions();
			set => tablesHeapOptions = value;
		}

		/// <summary>
		/// Gets/sets the debug (portable PDB) <see cref="TablesHeap"/> options. This is never <c>null</c>.
		/// </summary>
		public TablesHeapOptions DebugTablesHeapOptions {
			get => tablesHeapOptions ??= TablesHeapOptions.CreatePortablePdbV1_0();
			set => tablesHeapOptions = value;
		}

		/// <summary>
		/// Various options
		/// </summary>
		public MetadataFlags Flags;

		/// <summary>
		/// Extra heaps to add to the metadata. Also see <see cref="MetadataHeapsAdded"/> and <see cref="PreserveHeapOrder(ModuleDef, bool)"/>
		/// </summary>
		public List<IHeap> CustomHeaps => customHeaps ??= new List<IHeap>();

		/// <summary>
		/// Raised after all heaps have been added. The caller can sort the list if needed
		/// </summary>
		public event EventHandler2<MetadataHeapsAddedEventArgs> MetadataHeapsAdded;
		internal void RaiseMetadataHeapsAdded(MetadataHeapsAddedEventArgs e) => MetadataHeapsAdded?.Invoke(e.Metadata, e);

		/// <summary>
		/// Preserves the original order of heaps, and optionally adds all custom heaps to <see cref="CustomHeaps"/>.
		/// </summary>
		/// <param name="module">Original module with the heaps</param>
		/// <param name="addCustomHeaps">If true, all custom streams are added to <see cref="CustomHeaps"/></param>
		public void PreserveHeapOrder(ModuleDef module, bool addCustomHeaps) {
			if (module is null)
				throw new ArgumentNullException(nameof(module));
			if (module is ModuleDefMD mod) {
				if (addCustomHeaps) {
					var otherStreams = mod.Metadata.AllStreams.Where(a => a.GetType() == typeof(CustomDotNetStream)).Select(a => new DataReaderHeap(a));
					CustomHeaps.AddRange(otherStreams.OfType<IHeap>());
				}
				var streamToOrder = new Dictionary<DotNetStream, int>(mod.Metadata.AllStreams.Count);
				for (int i = 0, order = 0; i < mod.Metadata.AllStreams.Count; i++) {
					var stream = mod.Metadata.AllStreams[i];
					if (stream.StartOffset == 0)
						continue;
					streamToOrder.Add(stream, order++);
				}
				var nameToOrder = new Dictionary<string, int>(mod.Metadata.AllStreams.Count, StringComparer.Ordinal);
				for (int i = 0, order = 0; i < mod.Metadata.AllStreams.Count; i++) {
					var stream = mod.Metadata.AllStreams[i];
					if (stream.StartOffset == 0)
						continue;
					bool isKnownStream = stream is BlobStream || stream is GuidStream ||
						stream is PdbStream || stream is StringsStream || stream is TablesStream || stream is USStream;
					if (!nameToOrder.ContainsKey(stream.Name) || isKnownStream)
						nameToOrder[stream.Name] = order;
					order++;
				}
				MetadataHeapsAdded += (s, e) => {
					e.Heaps.Sort((a, b) => {
						int oa = GetOrder(streamToOrder, nameToOrder, a);
						int ob = GetOrder(streamToOrder, nameToOrder, b);
						int c = oa - ob;
						if (c != 0)
							return c;
						return StringComparer.Ordinal.Compare(a.Name, b.Name);
					});
				};
			}
		}

		static int GetOrder(Dictionary<DotNetStream, int> streamToOrder, Dictionary<string, int> nameToOrder, IHeap heap) {
			if (heap is DataReaderHeap drHeap && drHeap.OptionalOriginalStream is DotNetStream dnHeap && streamToOrder.TryGetValue(dnHeap, out int order))
				return order;
			if (nameToOrder.TryGetValue(heap.Name, out order))
				return order;

			return int.MaxValue;
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public MetadataOptions() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="flags">Flags</param>
		public MetadataOptions(MetadataFlags flags) => Flags = flags;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="mdhOptions">Meta data header options</param>
		public MetadataOptions(MetadataHeaderOptions mdhOptions) => metadataHeaderOptions = mdhOptions;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="mdhOptions">Meta data header options</param>
		/// <param name="flags">Flags</param>
		public MetadataOptions(MetadataHeaderOptions mdhOptions, MetadataFlags flags) {
			Flags = flags;
			metadataHeaderOptions = mdhOptions;
		}
	}

	sealed class DataWriterContext {
		public readonly MemoryStream OutStream;
		public readonly DataWriter Writer;
		public DataWriterContext() {
			OutStream = new MemoryStream();
			Writer = new DataWriter(OutStream);
		}
	}

	/// <summary>
	/// Portable PDB metadata kind
	/// </summary>
	public enum DebugMetadataKind {
		/// <summary>
		/// No debugging metadata
		/// </summary>
		None,

		/// <summary>
		/// Standalone / embedded portable PDB metadata
		/// </summary>
		Standalone,
	}

	/// <summary>
	/// Metadata writer event args
	/// </summary>
	public readonly struct MetadataWriterEventArgs {
		/// <summary>
		/// Gets the metadata writer
		/// </summary>
		public Metadata Metadata { get; }

		/// <summary>
		/// Gets the event
		/// </summary>
		public MetadataEvent Event { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="metadata">Writer</param>
		/// <param name="event">Event</param>
		public MetadataWriterEventArgs(Metadata metadata, MetadataEvent @event) {
			Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
			Event = @event;
		}
	}

	/// <summary>
	/// Metadata writer progress event args
	/// </summary>
	public readonly struct MetadataProgressEventArgs {
		/// <summary>
		/// Gets the metadata writer
		/// </summary>
		public Metadata Metadata { get; }

		/// <summary>
		/// Gets the progress, 0.0 - 1.0
		/// </summary>
		public double Progress { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="metadata">Writer</param>
		/// <param name="progress">Progress, 0.0 - 1.0</param>
		public MetadataProgressEventArgs(Metadata metadata, double progress) {
			if (progress < 0 || progress > 1)
				throw new ArgumentOutOfRangeException(nameof(progress));
			Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
			Progress = progress;
		}
	}

	/// <summary>
	/// .NET meta data
	/// </summary>
	public abstract class Metadata : IReuseChunk, ISignatureWriterHelper, ITokenProvider, ICustomAttributeWriterHelper, IPortablePdbCustomDebugInfoWriterHelper {
		uint length;
		FileOffset offset;
		RVA rva;
		readonly MetadataOptions options;
		ILogger logger;
		readonly NormalMetadata debugMetadata;
		readonly bool isStandaloneDebugMetadata;
		internal readonly ModuleDef module;
		internal readonly UniqueChunkList<ByteArrayChunk> constants;
		internal readonly MethodBodyChunks methodBodies;
		internal readonly NetResources netResources;
		internal readonly MetadataHeader metadataHeader;
		internal readonly PdbHeap pdbHeap;
		internal readonly TablesHeap tablesHeap;
		internal readonly StringsHeap stringsHeap;
		internal readonly USHeap usHeap;
		internal readonly GuidHeap guidHeap;
		internal readonly BlobHeap blobHeap;
		internal TypeDef[] allTypeDefs;
		internal readonly Rows<ModuleDef> moduleDefInfos = new Rows<ModuleDef>();
		internal readonly SortedRows<InterfaceImpl, RawInterfaceImplRow> interfaceImplInfos = new SortedRows<InterfaceImpl, RawInterfaceImplRow>();
		internal readonly SortedRows<IHasConstant, RawConstantRow> hasConstantInfos = new SortedRows<IHasConstant, RawConstantRow>();
		internal readonly SortedRows<CustomAttribute, RawCustomAttributeRow> customAttributeInfos = new SortedRows<CustomAttribute, RawCustomAttributeRow>();
		internal readonly SortedRows<IHasFieldMarshal, RawFieldMarshalRow> fieldMarshalInfos = new SortedRows<IHasFieldMarshal, RawFieldMarshalRow>();
		internal readonly SortedRows<DeclSecurity, RawDeclSecurityRow> declSecurityInfos = new SortedRows<DeclSecurity, RawDeclSecurityRow>();
		internal readonly SortedRows<TypeDef, RawClassLayoutRow> classLayoutInfos = new SortedRows<TypeDef, RawClassLayoutRow>();
		internal readonly SortedRows<FieldDef, RawFieldLayoutRow> fieldLayoutInfos = new SortedRows<FieldDef, RawFieldLayoutRow>();
		internal readonly Rows<TypeDef> eventMapInfos = new Rows<TypeDef>();
		internal readonly Rows<TypeDef> propertyMapInfos = new Rows<TypeDef>();
		internal readonly SortedRows<MethodDef, RawMethodSemanticsRow> methodSemanticsInfos = new SortedRows<MethodDef, RawMethodSemanticsRow>();
		internal readonly SortedRows<MethodDef, RawMethodImplRow> methodImplInfos = new SortedRows<MethodDef, RawMethodImplRow>();
		internal readonly Rows<ModuleRef> moduleRefInfos = new Rows<ModuleRef>();
		internal readonly SortedRows<IMemberForwarded, RawImplMapRow> implMapInfos = new SortedRows<IMemberForwarded, RawImplMapRow>();
		internal readonly SortedRows<FieldDef, RawFieldRVARow> fieldRVAInfos = new SortedRows<FieldDef, RawFieldRVARow>();
		internal readonly Rows<AssemblyDef> assemblyInfos = new Rows<AssemblyDef>();
		internal readonly Rows<AssemblyRef> assemblyRefInfos = new Rows<AssemblyRef>();
		internal readonly Rows<FileDef> fileDefInfos = new Rows<FileDef>();
		internal readonly Rows<ExportedType> exportedTypeInfos = new Rows<ExportedType>();
		internal readonly Rows<Resource> manifestResourceInfos = new Rows<Resource>();
		internal readonly SortedRows<TypeDef, RawNestedClassRow> nestedClassInfos = new SortedRows<TypeDef, RawNestedClassRow>();
		internal readonly SortedRows<GenericParam, RawGenericParamRow> genericParamInfos = new SortedRows<GenericParam, RawGenericParamRow>();
		internal readonly SortedRows<GenericParamConstraint, RawGenericParamConstraintRow> genericParamConstraintInfos = new SortedRows<GenericParamConstraint, RawGenericParamConstraintRow>();
		internal readonly Dictionary<MethodDef, MethodBody> methodToBody = new Dictionary<MethodDef, MethodBody>();
		internal readonly Dictionary<MethodDef, NativeMethodBody> methodToNativeBody = new Dictionary<MethodDef, NativeMethodBody>();
		internal readonly Dictionary<EmbeddedResource, DataReaderChunk> embeddedResourceToByteArray = new Dictionary<EmbeddedResource, DataReaderChunk>();
		readonly Dictionary<FieldDef, ByteArrayChunk> fieldToInitialValue = new Dictionary<FieldDef, ByteArrayChunk>();
		readonly Rows<PdbDocument> pdbDocumentInfos = new Rows<PdbDocument>();
		bool methodDebugInformationInfosUsed;
		readonly SortedRows<PdbScope, RawLocalScopeRow> localScopeInfos = new SortedRows<PdbScope, RawLocalScopeRow>();
		readonly Rows<PdbLocal> localVariableInfos = new Rows<PdbLocal>();
		readonly Rows<PdbConstant> localConstantInfos = new Rows<PdbConstant>();
		readonly Rows<PdbImportScope> importScopeInfos = new Rows<PdbImportScope>();
		readonly SortedRows<PdbCustomDebugInfo, RawStateMachineMethodRow> stateMachineMethodInfos = new SortedRows<PdbCustomDebugInfo, RawStateMachineMethodRow>();
		readonly SortedRows<PdbCustomDebugInfo, RawCustomDebugInformationRow> customDebugInfos = new SortedRows<PdbCustomDebugInfo, RawCustomDebugInformationRow>();
		readonly List<DataWriterContext> binaryWriterContexts = new List<DataWriterContext>();
		readonly List<SerializerMethodContext> serializerMethodContexts = new List<SerializerMethodContext>();
		readonly List<MethodDef> exportedMethods = new List<MethodDef>();

		/// <summary>
		/// Raised at various times when writing the metadata
		/// </summary>
		public event EventHandler2<MetadataWriterEventArgs> MetadataEvent;

		/// <summary>
		/// Raised when the progress is updated
		/// </summary>
		public event EventHandler2<MetadataProgressEventArgs> ProgressUpdated;

		/// <summary>
		/// Gets/sets the logger
		/// </summary>
		public ILogger Logger {
			get => logger;
			set => logger = value;
		}

		/// <summary>
		/// Gets the module
		/// </summary>
		public ModuleDef Module => module;

		/// <summary>
		/// Gets the constants
		/// </summary>
		public UniqueChunkList<ByteArrayChunk> Constants => constants;

		/// <summary>
		/// Gets the method body chunks
		/// </summary>
		public MethodBodyChunks MethodBodyChunks => methodBodies;

		/// <summary>
		/// Gets the .NET resources
		/// </summary>
		public NetResources NetResources => netResources;

		/// <summary>
		/// Gets the MD header
		/// </summary>
		public MetadataHeader MetadataHeader => metadataHeader;

		/// <summary>
		/// Gets the tables heap. Access to this heap is not recommended, but is useful if you
		/// want to add random table entries.
		/// </summary>
		public TablesHeap TablesHeap => tablesHeap;

		/// <summary>
		/// Gets the #Strings heap. Access to this heap is not recommended, but is useful if you
		/// want to add random strings.
		/// </summary>
		public StringsHeap StringsHeap => stringsHeap;

		/// <summary>
		/// Gets the #US heap. Access to this heap is not recommended, but is useful if
		/// you want to add random user strings.
		/// </summary>
		public USHeap USHeap => usHeap;

		/// <summary>
		/// Gets the #GUID heap. Access to this heap is not recommended, but is useful if you
		/// want to add random GUIDs.
		/// </summary>
		public GuidHeap GuidHeap => guidHeap;

		/// <summary>
		/// Gets the #Blob heap. Access to this heap is not recommended, but is useful if you
		/// want to add random blobs.
		/// </summary>
		public BlobHeap BlobHeap => blobHeap;

		/// <summary>
		/// Gets the #Pdb heap. It's only used if it's portable PDB metadata
		/// </summary>
		public PdbHeap PdbHeap => pdbHeap;

		/// <summary>
		/// Gets all exported methods
		/// </summary>
		public List<MethodDef> ExportedMethods => exportedMethods;

		/// <summary>
		/// The public key that should be used instead of the one in <see cref="AssemblyDef"/>.
		/// </summary>
		internal byte[] AssemblyPublicKey { get; set; }

		internal sealed class SortedRows<T, TRow> where T : class where TRow : struct {
			public List<Info> infos = new List<Info>();
			Dictionary<T, uint> toRid = new Dictionary<T, uint>();
			bool isSorted;

			public struct Info {
				public readonly T data;
				public /*readonly*/ TRow row;
				public Info(T data, ref TRow row) {
					this.data = data;
					this.row = row;
				}
			}

			public void Add(T data, TRow row) {
				if (isSorted)
					throw new ModuleWriterException($"Adding a row after it's been sorted. Table: {row.GetType()}");
				infos.Add(new Info(data, ref row));
				toRid[data] = (uint)toRid.Count + 1;
			}

			public void Sort(Comparison<Info> comparison) {
				infos.Sort(CreateComparison(comparison));
				toRid.Clear();
				for (int i = 0; i < infos.Count; i++)
					toRid[infos[i].data] = (uint)i + 1;
				isSorted = true;
			}

			Comparison<Info> CreateComparison(Comparison<Info> comparison) =>
				(a, b) => {
					int c = comparison(a, b);
					if (c != 0)
						return c;
					// Make sure it's a stable sort
					return toRid[a.data].CompareTo(toRid[b.data]);
				};

			public uint Rid(T data) => toRid[data];

			public bool TryGetRid(T data, out uint rid) {
				if (data is null) {
					rid = 0;
					return false;
				}
				return toRid.TryGetValue(data, out rid);
			}
		}

		internal sealed class Rows<T> where T : class {
			Dictionary<T, uint> dict = new Dictionary<T, uint>();

			public int Count => dict.Count;

			public bool TryGetRid(T value, out uint rid) {
				if (value is null) {
					rid = 0;
					return false;
				}
				return dict.TryGetValue(value, out rid);
			}

			public bool Exists(T value) => dict.ContainsKey(value);
			public void Add(T value, uint rid) => dict.Add(value, rid);
			public uint Rid(T value) => dict[value];
			public void SetRid(T value, uint rid) => dict[value] = rid;
		}

		/// <summary>
		/// Creates a <see cref="Metadata"/> instance
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="constants">Constants list</param>
		/// <param name="methodBodies">Method bodies list</param>
		/// <param name="netResources">.NET resources list</param>
		/// <param name="options">Options</param>
		/// <param name="debugKind">Debug metadata kind</param>
		/// <returns>A new <see cref="Metadata"/> instance</returns>
		public static Metadata Create(ModuleDef module, UniqueChunkList<ByteArrayChunk> constants, MethodBodyChunks methodBodies, NetResources netResources, MetadataOptions options = null, DebugMetadataKind debugKind = DebugMetadataKind.None) {
			if (options is null)
				options = new MetadataOptions();
			if ((options.Flags & MetadataFlags.PreserveRids) != 0 && module is ModuleDefMD)
				return new PreserveTokensMetadata(module, constants, methodBodies, netResources, options, debugKind, false);
			return new NormalMetadata(module, constants, methodBodies, netResources, options, debugKind, false);
		}

		/// <inheritdoc/>
		public FileOffset FileOffset => offset;

		/// <inheritdoc/>
		public RVA RVA => rva;

		/// <summary>
		/// Gets the <see cref="MetadataFlags.PreserveTypeRefRids"/> bit
		/// </summary>
		public bool PreserveTypeRefRids => (options.Flags & MetadataFlags.PreserveTypeRefRids) != 0;

		/// <summary>
		/// Gets the <see cref="MetadataFlags.PreserveTypeDefRids"/> bit
		/// </summary>
		public bool PreserveTypeDefRids => (options.Flags & MetadataFlags.PreserveTypeDefRids) != 0;

		/// <summary>
		/// Gets the <see cref="MetadataFlags.PreserveFieldRids"/> bit
		/// </summary>
		public bool PreserveFieldRids => (options.Flags & MetadataFlags.PreserveFieldRids) != 0;

		/// <summary>
		/// Gets the <see cref="MetadataFlags.PreserveMethodRids"/> bit
		/// </summary>
		public bool PreserveMethodRids => (options.Flags & MetadataFlags.PreserveMethodRids) != 0;

		/// <summary>
		/// Gets the <see cref="MetadataFlags.PreserveParamRids"/> bit
		/// </summary>
		public bool PreserveParamRids => (options.Flags & MetadataFlags.PreserveParamRids) != 0;

		/// <summary>
		/// Gets the <see cref="MetadataFlags.PreserveMemberRefRids"/> bit
		/// </summary>
		public bool PreserveMemberRefRids => (options.Flags & MetadataFlags.PreserveMemberRefRids) != 0;

		/// <summary>
		/// Gets the <see cref="MetadataFlags.PreserveStandAloneSigRids"/> bit
		/// </summary>
		public bool PreserveStandAloneSigRids => (options.Flags & MetadataFlags.PreserveStandAloneSigRids) != 0;

		/// <summary>
		/// Gets the <see cref="MetadataFlags.PreserveEventRids"/> bit
		/// </summary>
		public bool PreserveEventRids => (options.Flags & MetadataFlags.PreserveEventRids) != 0;

		/// <summary>
		/// Gets the <see cref="MetadataFlags.PreservePropertyRids"/> bit
		/// </summary>
		public bool PreservePropertyRids => (options.Flags & MetadataFlags.PreservePropertyRids) != 0;

		/// <summary>
		/// Gets the <see cref="MetadataFlags.PreserveTypeSpecRids"/> bit
		/// </summary>
		public bool PreserveTypeSpecRids => (options.Flags & MetadataFlags.PreserveTypeSpecRids) != 0;

		/// <summary>
		/// Gets the <see cref="MetadataFlags.PreserveMethodSpecRids"/> bit
		/// </summary>
		public bool PreserveMethodSpecRids => (options.Flags & MetadataFlags.PreserveMethodSpecRids) != 0;

		/// <summary>
		/// Gets/sets the <see cref="MetadataFlags.PreserveStringsOffsets"/> bit
		/// </summary>
		public bool PreserveStringsOffsets {
			get => (options.Flags & MetadataFlags.PreserveStringsOffsets) != 0;
			set {
				if (value)
					options.Flags |= MetadataFlags.PreserveStringsOffsets;
				else
					options.Flags &= ~MetadataFlags.PreserveStringsOffsets;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MetadataFlags.PreserveUSOffsets"/> bit
		/// </summary>
		public bool PreserveUSOffsets {
			get => (options.Flags & MetadataFlags.PreserveUSOffsets) != 0;
			set {
				if (value)
					options.Flags |= MetadataFlags.PreserveUSOffsets;
				else
					options.Flags &= ~MetadataFlags.PreserveUSOffsets;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MetadataFlags.PreserveBlobOffsets"/> bit
		/// </summary>
		public bool PreserveBlobOffsets {
			get => (options.Flags & MetadataFlags.PreserveBlobOffsets) != 0;
			set {
				if (value)
					options.Flags |= MetadataFlags.PreserveBlobOffsets;
				else
					options.Flags &= ~MetadataFlags.PreserveBlobOffsets;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MetadataFlags.PreserveExtraSignatureData"/> bit
		/// </summary>
		public bool PreserveExtraSignatureData {
			get => (options.Flags & MetadataFlags.PreserveExtraSignatureData) != 0;
			set {
				if (value)
					options.Flags |= MetadataFlags.PreserveExtraSignatureData;
				else
					options.Flags &= ~MetadataFlags.PreserveExtraSignatureData;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MetadataFlags.KeepOldMaxStack"/> bit
		/// </summary>
		public bool KeepOldMaxStack {
			get => (options.Flags & MetadataFlags.KeepOldMaxStack) != 0;
			set {
				if (value)
					options.Flags |= MetadataFlags.KeepOldMaxStack;
				else
					options.Flags &= ~MetadataFlags.KeepOldMaxStack;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MetadataFlags.AlwaysCreateGuidHeap"/> bit
		/// </summary>
		public bool AlwaysCreateGuidHeap {
			get => (options.Flags & MetadataFlags.AlwaysCreateGuidHeap) != 0;
			set {
				if (value)
					options.Flags |= MetadataFlags.AlwaysCreateGuidHeap;
				else
					options.Flags &= ~MetadataFlags.AlwaysCreateGuidHeap;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MetadataFlags.AlwaysCreateStringsHeap"/> bit
		/// </summary>
		public bool AlwaysCreateStringsHeap {
			get => (options.Flags & MetadataFlags.AlwaysCreateStringsHeap) != 0;
			set {
				if (value)
					options.Flags |= MetadataFlags.AlwaysCreateStringsHeap;
				else
					options.Flags &= ~MetadataFlags.AlwaysCreateStringsHeap;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MetadataFlags.AlwaysCreateUSHeap"/> bit
		/// </summary>
		public bool AlwaysCreateUSHeap {
			get => (options.Flags & MetadataFlags.AlwaysCreateUSHeap) != 0;
			set {
				if (value)
					options.Flags |= MetadataFlags.AlwaysCreateUSHeap;
				else
					options.Flags &= ~MetadataFlags.AlwaysCreateUSHeap;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MetadataFlags.AlwaysCreateBlobHeap"/> bit
		/// </summary>
		public bool AlwaysCreateBlobHeap {
			get => (options.Flags & MetadataFlags.AlwaysCreateBlobHeap) != 0;
			set {
				if (value)
					options.Flags |= MetadataFlags.AlwaysCreateBlobHeap;
				else
					options.Flags &= ~MetadataFlags.AlwaysCreateBlobHeap;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MetadataFlags.RoslynSortInterfaceImpl"/> bit
		/// </summary>
		public bool RoslynSortInterfaceImpl {
			get => (options.Flags & MetadataFlags.RoslynSortInterfaceImpl) != 0;
			set {
				if (value)
					options.Flags |= MetadataFlags.RoslynSortInterfaceImpl;
				else
					options.Flags &= ~MetadataFlags.RoslynSortInterfaceImpl;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MetadataFlags.NoMethodBodies"/> bit
		/// </summary>
		public bool NoMethodBodies {
			get => (options.Flags & MetadataFlags.NoMethodBodies) != 0;
			set {
				if (value)
					options.Flags |= MetadataFlags.NoMethodBodies;
				else
					options.Flags &= ~MetadataFlags.NoMethodBodies;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MetadataFlags.NoDotNetResources"/> bit
		/// </summary>
		public bool NoDotNetResources {
			get => (options.Flags & MetadataFlags.NoDotNetResources) != 0;
			set {
				if (value)
					options.Flags |= MetadataFlags.NoDotNetResources;
				else
					options.Flags &= ~MetadataFlags.NoDotNetResources;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MetadataFlags.NoFieldData"/> bit
		/// </summary>
		public bool NoFieldData {
			get => (options.Flags & MetadataFlags.NoFieldData) != 0;
			set {
				if (value)
					options.Flags |= MetadataFlags.NoFieldData;
				else
					options.Flags &= ~MetadataFlags.NoFieldData;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MetadataFlags.OptimizeCustomAttributeSerializedTypeNames"/> bit
		/// </summary>
		public bool OptimizeCustomAttributeSerializedTypeNames {
			get => (options.Flags & MetadataFlags.OptimizeCustomAttributeSerializedTypeNames) != 0;
			set {
				if (value)
					options.Flags |= MetadataFlags.OptimizeCustomAttributeSerializedTypeNames;
				else
					options.Flags &= ~MetadataFlags.OptimizeCustomAttributeSerializedTypeNames;
			}
		}

		/// <summary>
		/// If <c>true</c>, use the original Field RVAs. If it has no RVA, assume it's a new
		/// field value and create a new Field RVA.
		/// </summary>
		internal bool KeepFieldRVA { get; set; }

		/// <summary>
		/// Gets the number of methods that will be written.
		/// </summary>
		protected abstract int NumberOfMethods { get; }

		internal Metadata(ModuleDef module, UniqueChunkList<ByteArrayChunk> constants, MethodBodyChunks methodBodies, NetResources netResources, MetadataOptions options, DebugMetadataKind debugKind, bool isStandaloneDebugMetadata) {
			this.module = module;
			this.constants = constants;
			this.methodBodies = methodBodies;
			this.netResources = netResources;
			this.options = options ?? new MetadataOptions();
			metadataHeader = new MetadataHeader(isStandaloneDebugMetadata ? this.options.DebugMetadataHeaderOptions : this.options.MetadataHeaderOptions);
			tablesHeap = new TablesHeap(this, isStandaloneDebugMetadata ? this.options.DebugTablesHeapOptions : this.options.TablesHeapOptions);
			stringsHeap = new StringsHeap();
			usHeap = new USHeap();
			guidHeap = new GuidHeap();
			blobHeap = new BlobHeap();
			pdbHeap = new PdbHeap();

			this.isStandaloneDebugMetadata = isStandaloneDebugMetadata;
			switch (debugKind) {
			case DebugMetadataKind.None:
				break;

			case DebugMetadataKind.Standalone:
				Debug.Assert(!isStandaloneDebugMetadata);
				//TODO: Refactor this into a smaller class
				debugMetadata = new NormalMetadata(module, constants, methodBodies, netResources, options, DebugMetadataKind.None, true);
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(debugKind));
			}
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="module">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetRid(ModuleDef module) {
			moduleDefInfos.TryGetRid(module, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="tr">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public abstract uint GetRid(TypeRef tr);

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="td">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public abstract uint GetRid(TypeDef td);

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="fd">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public abstract uint GetRid(FieldDef fd);

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="md">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public abstract uint GetRid(MethodDef md);

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="pd">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public abstract uint GetRid(ParamDef pd);

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="ii">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetRid(InterfaceImpl ii) {
			interfaceImplInfos.TryGetRid(ii, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="mr">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public abstract uint GetRid(MemberRef mr);

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="hc">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetConstantRid(IHasConstant hc) {
			hasConstantInfos.TryGetRid(hc, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="ca">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetCustomAttributeRid(CustomAttribute ca) {
			customAttributeInfos.TryGetRid(ca, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="hfm">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetFieldMarshalRid(IHasFieldMarshal hfm) {
			fieldMarshalInfos.TryGetRid(hfm, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="ds">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetRid(DeclSecurity ds) {
			declSecurityInfos.TryGetRid(ds, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="td">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetClassLayoutRid(TypeDef td) {
			classLayoutInfos.TryGetRid(td, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="fd">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetFieldLayoutRid(FieldDef fd) {
			fieldLayoutInfos.TryGetRid(fd, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="sas">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public abstract uint GetRid(StandAloneSig sas);

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="td">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetEventMapRid(TypeDef td) {
			eventMapInfos.TryGetRid(td, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="ed">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public abstract uint GetRid(EventDef ed);

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="td">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetPropertyMapRid(TypeDef td) {
			propertyMapInfos.TryGetRid(td, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="pd">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public abstract uint GetRid(PropertyDef pd);

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="md">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetMethodSemanticsRid(MethodDef md) {
			methodSemanticsInfos.TryGetRid(md, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="mr">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetRid(ModuleRef mr) {
			moduleRefInfos.TryGetRid(mr, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="ts">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public abstract uint GetRid(TypeSpec ts);

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="mf">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetImplMapRid(IMemberForwarded mf) {
			implMapInfos.TryGetRid(mf, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="fd">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetFieldRVARid(FieldDef fd) {
			fieldRVAInfos.TryGetRid(fd, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="asm">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetRid(AssemblyDef asm) {
			assemblyInfos.TryGetRid(asm, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="asmRef">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetRid(AssemblyRef asmRef) {
			assemblyRefInfos.TryGetRid(asmRef, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="fd">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetRid(FileDef fd) {
			fileDefInfos.TryGetRid(fd, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="et">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetRid(ExportedType et) {
			exportedTypeInfos.TryGetRid(et, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="resource">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetManifestResourceRid(Resource resource) {
			manifestResourceInfos.TryGetRid(resource, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="td">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetNestedClassRid(TypeDef td) {
			nestedClassInfos.TryGetRid(td, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="gp">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetRid(GenericParam gp) {
			genericParamInfos.TryGetRid(gp, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="ms">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public abstract uint GetRid(MethodSpec ms);

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="gpc">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetRid(GenericParamConstraint gpc) {
			genericParamConstraintInfos.TryGetRid(gpc, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="doc">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetRid(PdbDocument doc) {
			if (debugMetadata is null)
				return 0;
			debugMetadata.pdbDocumentInfos.TryGetRid(doc, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="scope">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetRid(PdbScope scope) {
			if (debugMetadata is null)
				return 0;
			debugMetadata.localScopeInfos.TryGetRid(scope, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="local">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetRid(PdbLocal local) {
			if (debugMetadata is null)
				return 0;
			debugMetadata.localVariableInfos.TryGetRid(local, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="constant">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetRid(PdbConstant constant) {
			if (debugMetadata is null)
				return 0;

			debugMetadata.localConstantInfos.TryGetRid(constant, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="importScope">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetRid(PdbImportScope importScope) {
			if (debugMetadata is null)
				return 0;
			debugMetadata.importScopeInfos.TryGetRid(importScope, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="asyncMethod">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetStateMachineMethodRid(PdbAsyncMethodCustomDebugInfo asyncMethod) {
			if (debugMetadata is null)
				return 0;
			debugMetadata.stateMachineMethodInfos.TryGetRid(asyncMethod, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="iteratorMethod">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetStateMachineMethodRid(PdbIteratorMethodCustomDebugInfo iteratorMethod) {
			if (debugMetadata is null)
				return 0;
			debugMetadata.stateMachineMethodInfos.TryGetRid(iteratorMethod, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="cdi">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetCustomDebugInfoRid(PdbCustomDebugInfo cdi) {
			if (debugMetadata is null)
				return 0;
			debugMetadata.customDebugInfos.TryGetRid(cdi, out uint rid);
			return rid;
		}

		/// <summary>
		/// Gets the <see cref="MethodBody"/>
		/// </summary>
		/// <param name="md">Method</param>
		/// <returns>The <see cref="MethodBody"/> or <c>null</c> if <paramref name="md"/> is
		/// <c>null</c> or not a method defined in this module.</returns>
		public MethodBody GetMethodBody(MethodDef md) {
			if (md is null)
				return null;
			methodToBody.TryGetValue(md, out var mb);
			return mb;
		}

		/// <summary>
		/// Gets a method's local variable signature token
		/// </summary>
		/// <param name="md">Method</param>
		/// <returns>Locals sig token or <c>0</c></returns>
		public uint GetLocalVarSigToken(MethodDef md) => GetMethodBody(md)?.LocalVarSigTok ?? 0;

		/// <summary>
		/// Gets the <see cref="DataReaderChunk"/> where the resource data will be stored
		/// </summary>
		/// <param name="er">Embedded resource</param>
		/// <returns>A <see cref="DataReaderChunk"/> instance or <c>null</c> if <paramref name="er"/>
		/// is invalid</returns>
		public DataReaderChunk GetChunk(EmbeddedResource er) {
			if (er is null)
				return null;
			embeddedResourceToByteArray.TryGetValue(er, out var chunk);
			return chunk;
		}

		/// <summary>
		/// Gets the <see cref="ByteArrayChunk"/> where the initial value is stored
		/// </summary>
		/// <param name="fd">Field</param>
		/// <returns>A <see cref="ByteArrayChunk"/> instance or <c>null</c> if <paramref name="fd"/>
		/// is invalid</returns>
		public ByteArrayChunk GetInitialValueChunk(FieldDef fd) {
			if (fd is null)
				return null;
			fieldToInitialValue.TryGetValue(fd, out var chunk);
			return chunk;
		}

		ILogger GetLogger() => logger ?? DummyLogger.ThrowModuleWriterExceptionOnErrorInstance;

		/// <summary>
		/// Called when an error is detected
		/// </summary>
		/// <param name="message">Error message</param>
		/// <param name="args">Optional message arguments</param>
		protected void Error(string message, params object[] args) => GetLogger().Log(this, LoggerEvent.Error, message, args);

		/// <summary>
		/// Called to warn of something
		/// </summary>
		/// <param name="message">Warning message</param>
		/// <param name="args">Optional message arguments</param>
		protected void Warning(string message, params object[] args) => GetLogger().Log(this, LoggerEvent.Warning, message, args);

		/// <summary>
		/// Raises <see cref="MetadataEvent"/>
		/// </summary>
		/// <param name="evt">Event</param>
		protected void OnMetadataEvent(MetadataEvent evt) {
			RaiseProgress(evt, 0);
			MetadataEvent?.Invoke(this, new MetadataWriterEventArgs(this, evt));
		}

		static readonly double[] eventToProgress = new double[(int)Writer.MetadataEvent.EndCreateTables - (int)Writer.MetadataEvent.BeginCreateTables + 1 + 1] {
			0,					// BeginCreateTables
			0.00134240009466231,// AllocateTypeDefRids
			0.00257484711254305,// AllocateMemberDefRids
			0.0762721800615359,	// MemberDefRidsAllocated
			0.196633787905108,	// MemberDefsInitialized
			0.207788892253819,	// BeforeSortTables
			0.270543867900699,	// MostTablesSorted
			0.451478814851716,	// MemberDefCustomAttributesWritten
			0.451478949929206,	// BeginAddResources
			0.454664752528583,	// EndAddResources
			0.454664887606073,	// BeginWriteMethodBodies
			0.992591810143725,	// EndWriteMethodBodies
			0.999984331011171,	// OnAllTablesSorted
			1,					// EndCreateTables
			1,// An extra one so we can get the next base progress without checking the index
		};

		/// <summary>
		/// Raises the progress event
		/// </summary>
		/// <param name="evt">Base event</param>
		/// <param name="subProgress">Sub progress</param>
		protected void RaiseProgress(MetadataEvent evt, double subProgress) {
			subProgress = Math.Min(1, Math.Max(0, subProgress));
			var baseProgress = eventToProgress[(int)evt];
			var nextProgress = eventToProgress[(int)evt + 1];
			var progress = baseProgress + (nextProgress - baseProgress) * subProgress;
			progress = Math.Min(1, Math.Max(0, progress));
			ProgressUpdated?.Invoke(this, new MetadataProgressEventArgs(this, progress));
		}

		/// <summary>
		/// Creates the .NET metadata tables
		/// </summary>
		public void CreateTables() {
			OnMetadataEvent(Writer.MetadataEvent.BeginCreateTables);

			if (module.Types.Count == 0 || module.Types[0] is null)
				throw new ModuleWriterException("Missing global <Module> type");

			if (module is ModuleDefMD moduleDefMD) {
				if (PreserveStringsOffsets)
					stringsHeap.Populate(moduleDefMD.StringsStream);
				if (PreserveUSOffsets)
					usHeap.Populate(moduleDefMD.USStream);
				if (PreserveBlobOffsets)
					blobHeap.Populate(moduleDefMD.BlobStream);
			}

			Create();
		}

		/// <summary>
		/// Updates each <c>Method</c> row's <c>RVA</c> column if it has any code
		/// </summary>
		void UpdateMethodRvas() {
			foreach (var kv in methodToBody) {
				var method = kv.Key;
				var body = kv.Value;
				uint rid = GetRid(method);
				var row = tablesHeap.MethodTable[rid];
				row = new RawMethodRow((uint)body.RVA, row.ImplFlags, row.Flags, row.Name, row.Signature, row.ParamList);
				tablesHeap.MethodTable[rid] = row;
			}
			foreach (var kv in methodToNativeBody) {
				var method = kv.Key;
				var body = kv.Value;
				uint rid = GetRid(method);
				var row = tablesHeap.MethodTable[rid];
				row = new RawMethodRow((uint)body.RVA, row.ImplFlags, row.Flags, row.Name, row.Signature, row.ParamList);
				tablesHeap.MethodTable[rid] = row;
			}
		}

		/// <summary>
		/// Updates the <c>FieldRVA</c> rows
		/// </summary>
		void UpdateFieldRvas() {
			foreach (var kv in fieldToInitialValue) {
				var field = kv.Key;
				var iv = kv.Value;
				uint rid = fieldRVAInfos.Rid(field);
				var row = tablesHeap.FieldRVATable[rid];
				row = new RawFieldRVARow((uint)iv.RVA, row.Field);
				tablesHeap.FieldRVATable[rid] = row;
			}
		}

		void Create() {
			Debug.Assert(!isStandaloneDebugMetadata);
			Initialize();
			allTypeDefs = GetAllTypeDefs();
			OnMetadataEvent(Writer.MetadataEvent.AllocateTypeDefRids);
			AllocateTypeDefRids();
			OnMetadataEvent(Writer.MetadataEvent.AllocateMemberDefRids);
			AllocateMemberDefRids();
			OnMetadataEvent(Writer.MetadataEvent.MemberDefRidsAllocated);

			AddModule(module);
			AddPdbDocuments();
			InitializeMethodDebugInformation();
			InitializeTypeDefsAndMemberDefs();
			OnMetadataEvent(Writer.MetadataEvent.MemberDefsInitialized);

			InitializeVTableFixups();

			AddExportedTypes();
			InitializeEntryPoint();
			if (!(module.Assembly is null))
				AddAssembly(module.Assembly, AssemblyPublicKey);

			OnMetadataEvent(Writer.MetadataEvent.BeforeSortTables);
			SortTables();
			InitializeGenericParamConstraintTable();
			OnMetadataEvent(Writer.MetadataEvent.MostTablesSorted);

			WriteTypeDefAndMemberDefCustomAttributesAndCustomDebugInfos();
			OnMetadataEvent(Writer.MetadataEvent.MemberDefCustomAttributesWritten);

			OnMetadataEvent(Writer.MetadataEvent.BeginAddResources);
			AddResources(module.Resources);
			OnMetadataEvent(Writer.MetadataEvent.EndAddResources);

			OnMetadataEvent(Writer.MetadataEvent.BeginWriteMethodBodies);
			WriteMethodBodies();
			OnMetadataEvent(Writer.MetadataEvent.EndWriteMethodBodies);

			BeforeSortingCustomAttributes();
			InitializeCustomAttributeAndCustomDebugInfoTables();
			OnMetadataEvent(Writer.MetadataEvent.OnAllTablesSorted);

			EverythingInitialized();
			OnMetadataEvent(Writer.MetadataEvent.EndCreateTables);
		}

		/// <summary>
		/// Initializes all <c>TypeDef</c>, <c>Field</c>, <c>Method</c>, <c>Event</c>,
		/// <c>Property</c> and <c>Param</c> rows. Other tables that are related to these six
		/// tables are also updated. No custom attributes are written yet, though. Method bodies
		/// aren't written either.
		/// </summary>
		void InitializeTypeDefsAndMemberDefs() {
			int count;
			int numTypes = allTypeDefs.Length;
			int typeNum = 0;
			int notifyNum = 0;
			const int numNotifyEvents = 5;
			int notifyAfter = numTypes / numNotifyEvents;

			foreach (var type in allTypeDefs) {
				if (typeNum++ == notifyAfter && notifyNum < numNotifyEvents) {
					RaiseProgress(Writer.MetadataEvent.MemberDefRidsAllocated, (double)typeNum / numTypes);
					notifyNum++;
					notifyAfter = (int)((double)numTypes / numNotifyEvents * (notifyNum + 1));
				}

				if (type is null) {
					Error("TypeDef is null");
					continue;
				}
				uint typeRid = GetRid(type);
				var typeRow = tablesHeap.TypeDefTable[typeRid];
				typeRow = new RawTypeDefRow((uint)type.Attributes, stringsHeap.Add(type.Name), stringsHeap.Add(type.Namespace), type.BaseType is null ? 0 : AddTypeDefOrRef(type.BaseType), typeRow.FieldList, typeRow.MethodList);
				tablesHeap.TypeDefTable[typeRid] = typeRow;
				AddGenericParams(new MDToken(Table.TypeDef, typeRid), type.GenericParameters);
				AddDeclSecurities(new MDToken(Table.TypeDef, typeRid), type.DeclSecurities);
				AddInterfaceImpls(typeRid, type.Interfaces);
				AddClassLayout(type);
				AddNestedType(type, type.DeclaringType);

				var fields = type.Fields;
				count = fields.Count;
				for (int i = 0; i < count; i++) {
					var field = fields[i];
					if (field is null) {
						Error("Field is null. TypeDef {0} ({1:X8})", type, type.MDToken.Raw);
						continue;
					}
					uint rid = GetRid(field);
					var row = new RawFieldRow((ushort)field.Attributes, stringsHeap.Add(field.Name), GetSignature(field.Signature));
					tablesHeap.FieldTable[rid] = row;
					AddFieldLayout(field);
					AddFieldMarshal(new MDToken(Table.Field, rid), field);
					AddFieldRVA(field);
					AddImplMap(new MDToken(Table.Field, rid), field);
					AddConstant(new MDToken(Table.Field, rid), field);
				}

				var methods = type.Methods;
				count = methods.Count;
				for (int i = 0; i < count; i++) {
					var method = methods[i];
					if (method is null) {
						Error("Method is null. TypeDef {0} ({1:X8})", type, type.MDToken.Raw);
						continue;
					}
					if (!(method.ExportInfo is null))
						ExportedMethods.Add(method);
					uint rid = GetRid(method);
					var row = tablesHeap.MethodTable[rid];
					row = new RawMethodRow(row.RVA, (ushort)method.ImplAttributes, (ushort)method.Attributes, stringsHeap.Add(method.Name), GetSignature(method.Signature), row.ParamList);
					tablesHeap.MethodTable[rid] = row;
					AddGenericParams(new MDToken(Table.Method, rid), method.GenericParameters);
					AddDeclSecurities(new MDToken(Table.Method, rid), method.DeclSecurities);
					AddImplMap(new MDToken(Table.Method, rid), method);
					AddMethodImpls(method, method.Overrides);
					var paramDefs = method.ParamDefs;
					int count2 = paramDefs.Count;
					for (int j = 0; j < count2; j++) {
						var pd = paramDefs[j];
						if (pd is null) {
							Error("Param is null. Method {0} ({1:X8})", method, method.MDToken.Raw);
							continue;
						}
						uint pdRid = GetRid(pd);
						var pdRow = new RawParamRow((ushort)pd.Attributes, pd.Sequence, stringsHeap.Add(pd.Name));
						tablesHeap.ParamTable[pdRid] = pdRow;
						AddConstant(new MDToken(Table.Param, pdRid), pd);
						AddFieldMarshal(new MDToken(Table.Param, pdRid), pd);
					}
				}

				var events = type.Events;
				count = events.Count;
				for (int i = 0; i < count; i++) {
					var evt = events[i];
					if (evt is null) {
						Error("Event is null. TypeDef {0} ({1:X8})", type, type.MDToken.Raw);
						continue;
					}
					uint rid = GetRid(evt);
					var row = new RawEventRow((ushort)evt.Attributes, stringsHeap.Add(evt.Name), AddTypeDefOrRef(evt.EventType));
					tablesHeap.EventTable[rid] = row;
					AddMethodSemantics(evt);
				}

				var properties = type.Properties;
				count = properties.Count;
				for (int i = 0; i < count; i++) {
					var prop = properties[i];
					if (prop is null) {
						Error("Property is null. TypeDef {0} ({1:X8})", type, type.MDToken.Raw);
						continue;
					}
					uint rid = GetRid(prop);
					var row = new RawPropertyRow((ushort)prop.Attributes, stringsHeap.Add(prop.Name), GetSignature(prop.Type));
					tablesHeap.PropertyTable[rid] = row;
					AddConstant(new MDToken(Table.Property, rid), prop);
					AddMethodSemantics(prop);
				}
			}
		}

		/// <summary>
		/// Writes <c>TypeDef</c>, <c>Field</c>, <c>Method</c>, <c>Event</c>,
		/// <c>Property</c> and <c>Param</c> custom attributes and custom debug infos.
		/// </summary>
		void WriteTypeDefAndMemberDefCustomAttributesAndCustomDebugInfos() {
			int count;
			int numTypes = allTypeDefs.Length;
			int typeNum = 0;
			int notifyNum = 0;
			const int numNotifyEvents = 5;
			int notifyAfter = numTypes / numNotifyEvents;

			uint rid;
			foreach (var type in allTypeDefs) {
				if (typeNum++ == notifyAfter && notifyNum < numNotifyEvents) {
					RaiseProgress(Writer.MetadataEvent.MostTablesSorted, (double)typeNum / numTypes);
					notifyNum++;
					notifyAfter = (int)((double)numTypes / numNotifyEvents * (notifyNum + 1));
				}

				if (type is null)
					continue;
				if (type.HasCustomAttributes || type.HasCustomDebugInfos) {
					rid = GetRid(type);
					AddCustomAttributes(Table.TypeDef, rid, type);
					AddCustomDebugInformationList(Table.TypeDef, rid, type);
				}

				var fields = type.Fields;
				count = fields.Count;
				for (int i = 0; i < count; i++) {
					var field = fields[i];
					if (field is null)
						continue;
					if (field.HasCustomAttributes || field.HasCustomDebugInfos) {
						rid = GetRid(field);
						AddCustomAttributes(Table.Field, rid, field);
						AddCustomDebugInformationList(Table.Field, rid, field);
					}
				}

				var methods = type.Methods;
				count = methods.Count;
				for (int i = 0; i < count; i++) {
					var method = methods[i];
					if (method is null)
						continue;
					if (method.HasCustomAttributes) {
						rid = GetRid(method);
						AddCustomAttributes(Table.Method, rid, method);
						// Method custom debug info is added later when writing method bodies
					}
					var paramDefs = method.ParamDefs;
					int count2 = paramDefs.Count;
					for (int j = 0; j < count2; j++) {
						var pd = paramDefs[j];
						if (pd is null)
							continue;
						if (pd.HasCustomAttributes || pd.HasCustomDebugInfos) {
							rid = GetRid(pd);
							AddCustomAttributes(Table.Param, rid, pd);
							AddCustomDebugInformationList(Table.Param, rid, pd);
						}
					}
				}
				var events = type.Events;
				count = events.Count;
				for (int i = 0; i < count; i++) {
					var evt = events[i];
					if (evt is null)
						continue;
					if (evt.HasCustomAttributes || evt.HasCustomDebugInfos) {
						rid = GetRid(evt);
						AddCustomAttributes(Table.Event, rid, evt);
						AddCustomDebugInformationList(Table.Event, rid, evt);
					}
				}
				var properties = type.Properties;
				count = properties.Count;
				for (int i = 0; i < count; i++) {
					var prop = properties[i];
					if (prop is null)
						continue;
					if (prop.HasCustomAttributes || prop.HasCustomDebugInfos) {
						rid = GetRid(prop);
						AddCustomAttributes(Table.Property, rid, prop);
						AddCustomDebugInformationList(Table.Property, rid, prop);
					}
				}
			}
		}

		/// <summary>
		/// Adds the tokens of all methods in all vtables, if any
		/// </summary>
		void InitializeVTableFixups() {
			var fixups = module.VTableFixups;
			if (fixups is null || fixups.VTables.Count == 0)
				return;

			foreach (var vtable in fixups) {
				if (vtable is null) {
					Error("VTable is null");
					continue;
				}
				foreach (var method in vtable) {
					if (method is null)
						continue;
					AddMDTokenProvider(method);
				}
			}
		}

		void AddExportedTypes() {
			var exportedTypes = module.ExportedTypes;
			int count = exportedTypes.Count;
			for (int i = 0; i < count; i++)
				AddExportedType(exportedTypes[i]);
		}

		/// <summary>
		/// Adds the entry point. It's only needed if it's a <see cref="FileDef"/> since if it's
		/// a <see cref="MethodDef"/>, it will have already been added.
		/// </summary>
		void InitializeEntryPoint() {
			if (module.ManagedEntryPoint is FileDef epFile)
				AddFile(epFile);
		}

		/// <summary>
		/// Sorts all unsorted tables except <c>GenericParamConstraint</c> and <c>CustomAttribute</c>
		/// </summary>
		void SortTables() {
			classLayoutInfos.Sort((a, b)	=> a.row.Parent.CompareTo(b.row.Parent));
			hasConstantInfos.Sort((a, b)	=> a.row.Parent.CompareTo(b.row.Parent));
			declSecurityInfos.Sort((a, b)	=> a.row.Parent.CompareTo(b.row.Parent));
			fieldLayoutInfos.Sort((a, b)	=> a.row.Field.CompareTo(b.row.Field));
			fieldMarshalInfos.Sort((a, b)	=> a.row.Parent.CompareTo(b.row.Parent));
			fieldRVAInfos.Sort((a, b)		=> a.row.Field.CompareTo(b.row.Field));
			implMapInfos.Sort((a, b)		=> a.row.MemberForwarded.CompareTo(b.row.MemberForwarded));
			methodImplInfos.Sort((a, b)		=> a.row.Class.CompareTo(b.row.Class));
			methodSemanticsInfos.Sort((a, b)=> a.row.Association.CompareTo(b.row.Association));
			nestedClassInfos.Sort((a, b)	=> a.row.NestedClass.CompareTo(b.row.NestedClass));
			genericParamInfos.Sort((a, b) => {
				if (a.row.Owner != b.row.Owner)
					return a.row.Owner.CompareTo(b.row.Owner);
				return a.row.Number.CompareTo(b.row.Number);
			});
			if (RoslynSortInterfaceImpl)
				interfaceImplInfos.Sort((a, b) => a.row.Class.CompareTo(b.row.Class));
			else {
				interfaceImplInfos.Sort((a, b) => {
					if (a.row.Class != b.row.Class)
						return a.row.Class.CompareTo(b.row.Class);
					return a.row.Interface.CompareTo(b.row.Interface);
				});
			}

			tablesHeap.ClassLayoutTable.IsSorted = true;
			tablesHeap.ConstantTable.IsSorted = true;
			tablesHeap.DeclSecurityTable.IsSorted = true;
			tablesHeap.FieldLayoutTable.IsSorted = true;
			tablesHeap.FieldMarshalTable.IsSorted = true;
			tablesHeap.FieldRVATable.IsSorted = true;
			tablesHeap.GenericParamTable.IsSorted = true;
			tablesHeap.ImplMapTable.IsSorted = true;
			tablesHeap.InterfaceImplTable.IsSorted = true;
			tablesHeap.MethodImplTable.IsSorted = true;
			tablesHeap.MethodSemanticsTable.IsSorted = true;
			tablesHeap.NestedClassTable.IsSorted = true;

			// These two are also sorted
			tablesHeap.EventMapTable.IsSorted = true;
			tablesHeap.PropertyMapTable.IsSorted = true;

			foreach (var info in classLayoutInfos.infos) tablesHeap.ClassLayoutTable.Create(info.row);
			foreach (var info in hasConstantInfos.infos) tablesHeap.ConstantTable.Create(info.row);
			foreach (var info in declSecurityInfos.infos) tablesHeap.DeclSecurityTable.Create(info.row);
			foreach (var info in fieldLayoutInfos.infos) tablesHeap.FieldLayoutTable.Create(info.row);
			foreach (var info in fieldMarshalInfos.infos) tablesHeap.FieldMarshalTable.Create(info.row);
			foreach (var info in fieldRVAInfos.infos) tablesHeap.FieldRVATable.Create(info.row);
			foreach (var info in genericParamInfos.infos) tablesHeap.GenericParamTable.Create(info.row);
			foreach (var info in implMapInfos.infos) tablesHeap.ImplMapTable.Create(info.row);
			foreach (var info in interfaceImplInfos.infos) tablesHeap.InterfaceImplTable.Create(info.row);
			foreach (var info in methodImplInfos.infos) tablesHeap.MethodImplTable.Create(info.row);
			foreach (var info in methodSemanticsInfos.infos) tablesHeap.MethodSemanticsTable.Create(info.row);
			foreach (var info in nestedClassInfos.infos) tablesHeap.NestedClassTable.Create(info.row);

			foreach (var info in interfaceImplInfos.infos) {
				if (info.data.HasCustomAttributes || info.data.HasCustomDebugInfos) {
					uint rid = interfaceImplInfos.Rid(info.data);
					AddCustomAttributes(Table.InterfaceImpl, rid, info.data);
					AddCustomDebugInformationList(Table.InterfaceImpl, rid, info.data);
				}
			}
			foreach (var info in declSecurityInfos.infos) {
				if (info.data.HasCustomAttributes || info.data.HasCustomDebugInfos) {
					uint rid = declSecurityInfos.Rid(info.data);
					AddCustomAttributes(Table.DeclSecurity, rid, info.data);
					AddCustomDebugInformationList(Table.DeclSecurity, rid, info.data);
				}
			}
			foreach (var info in genericParamInfos.infos) {
				if (info.data.HasCustomAttributes || info.data.HasCustomDebugInfos) {
					uint rid = genericParamInfos.Rid(info.data);
					AddCustomAttributes(Table.GenericParam, rid, info.data);
					AddCustomDebugInformationList(Table.GenericParam, rid, info.data);
				}
			}
		}

		/// <summary>
		/// Initializes the <c>GenericParamConstraint</c> table
		/// </summary>
		void InitializeGenericParamConstraintTable() {
			foreach (var type in allTypeDefs) {
				if (type is null)
					continue;
				AddGenericParamConstraints(type.GenericParameters);
				var methods = type.Methods;
				int count = methods.Count;
				for (int i = 0; i < count; i++) {
					var method = methods[i];
					if (method is null)
						continue;
					AddGenericParamConstraints(method.GenericParameters);
				}
			}
			genericParamConstraintInfos.Sort((a, b) => a.row.Owner.CompareTo(b.row.Owner));
			tablesHeap.GenericParamConstraintTable.IsSorted = true;
			foreach (var info in genericParamConstraintInfos.infos)
				tablesHeap.GenericParamConstraintTable.Create(info.row);
			foreach (var info in genericParamConstraintInfos.infos) {
				if (info.data.HasCustomAttributes || info.data.HasCustomDebugInfos) {
					uint rid = genericParamConstraintInfos.Rid(info.data);
					AddCustomAttributes(Table.GenericParamConstraint, rid, info.data);
					AddCustomDebugInformationList(Table.GenericParamConstraint, rid, info.data);
				}
			}
		}

		/// <summary>
		/// Inserts all custom attribute / custom debug info rows in the tables and sort them
		/// </summary>
		void InitializeCustomAttributeAndCustomDebugInfoTables() {
			customAttributeInfos.Sort((a, b) => a.row.Parent.CompareTo(b.row.Parent));
			tablesHeap.CustomAttributeTable.IsSorted = true;
			foreach (var info in customAttributeInfos.infos)
				tablesHeap.CustomAttributeTable.Create(info.row);

			if (!(debugMetadata is null)) {
				debugMetadata.stateMachineMethodInfos.Sort((a, b) => a.row.MoveNextMethod.CompareTo(b.row.MoveNextMethod));
				debugMetadata.tablesHeap.StateMachineMethodTable.IsSorted = true;
				foreach (var info in debugMetadata.stateMachineMethodInfos.infos)
					debugMetadata.tablesHeap.StateMachineMethodTable.Create(info.row);

				debugMetadata.customDebugInfos.Sort((a, b) => a.row.Parent.CompareTo(b.row.Parent));
				debugMetadata.tablesHeap.CustomDebugInformationTable.IsSorted = true;
				foreach (var info in debugMetadata.customDebugInfos.infos)
					debugMetadata.tablesHeap.CustomDebugInformationTable.Create(info.row);
			}
		}

		struct MethodScopeDebugInfo {
			public uint MethodRid;
			public PdbScope Scope;
			public uint ScopeStart;
			public uint ScopeLength;
		}

		/// <summary>
		/// Writes all method bodies
		/// </summary>
		void WriteMethodBodies() {
			Debug.Assert(!isStandaloneDebugMetadata);
			if (NoMethodBodies)
				return;
			int numMethods = NumberOfMethods;
			int methodNum = 0;
			int notifyNum = 0;
			// Writing method bodies is the most expensive part and takes the longest
			const int numNotifyEvents = 40;
			int notifyAfter = numMethods / numNotifyEvents;

			var debugMetadata = this.debugMetadata;
			var methodBodies = this.methodBodies;
			var methodToBody = this.methodToBody;

			List<MethodScopeDebugInfo> methodScopeDebugInfos;
			List<PdbScope> scopeStack;
			SerializerMethodContext serializerMethodContext;
			if (debugMetadata is null) {
				methodScopeDebugInfos = null;
				scopeStack = null;
				serializerMethodContext = null;
			}
			else {
				methodScopeDebugInfos = new List<MethodScopeDebugInfo>();
				scopeStack = new List<PdbScope>();
				serializerMethodContext = AllocSerializerMethodContext();
			}

			bool keepMaxStack = KeepOldMaxStack;
			var writer = new MethodBodyWriter(this);
			foreach (var type in allTypeDefs) {
				if (type is null)
					continue;

				var methods = type.Methods;
				for (int i = 0; i < methods.Count; i++) {
					var method = methods[i];
					if (method is null)
						continue;

					if (methodNum++ == notifyAfter && notifyNum < numNotifyEvents) {
						RaiseProgress(Writer.MetadataEvent.BeginWriteMethodBodies, (double)methodNum / numMethods);
						notifyNum++;
						notifyAfter = (int)((double)numMethods / numNotifyEvents * (notifyNum + 1));
					}

					uint localVarSigTok = 0;

					var cilBody = method.Body;
					if (!(cilBody is null)) {
						if (!(cilBody.Instructions.Count == 0 && cilBody.Variables.Count == 0)) {
							writer.Reset(cilBody, keepMaxStack || cilBody.KeepOldMaxStack);
							writer.Write();
							var origRva = method.RVA;
							uint origSize = cilBody.MetadataBodySize;
							var mb = methodBodies.Add(new MethodBody(writer.Code, writer.ExtraSections, writer.LocalVarSigTok), origRva, origSize);
							methodToBody[method] = mb;
							localVarSigTok = writer.LocalVarSigTok;
						}
					}
					else {
						var nativeBody = method.NativeBody;
						if (!(nativeBody is null))
							methodToNativeBody[method] = nativeBody;
						else if (!(method.MethodBody is null))
							Error("Unsupported method body");
					}

					if (!(debugMetadata is null)) {
						uint rid = GetRid(method);

						if (!(cilBody is null)) {
							var pdbMethod = cilBody.PdbMethod;
							if (!(pdbMethod is null)) {
								// We don't need to write empty scopes
								if (!IsEmptyRootScope(cilBody, pdbMethod.Scope)) {
									serializerMethodContext.SetBody(method);
									scopeStack.Add(pdbMethod.Scope);
									while (scopeStack.Count > 0) {
										var scope = scopeStack[scopeStack.Count - 1];
										scopeStack.RemoveAt(scopeStack.Count - 1);
										scopeStack.AddRange(scope.Scopes);
										uint scopeStart = serializerMethodContext.GetOffset(scope.Start);
										uint scopeEnd = serializerMethodContext.GetOffset(scope.End);
										methodScopeDebugInfos.Add(new MethodScopeDebugInfo() {
											MethodRid = rid,
											Scope = scope,
											ScopeStart = scopeStart,
											ScopeLength = scopeEnd - scopeStart,
										});
									}
								}
							}
						}

						// Always add CDIs even if it has no managed method body
						AddCustomDebugInformationList(method, rid, localVarSigTok);
					}
				}
			}
			if (!(debugMetadata is null)) {
				methodScopeDebugInfos.Sort((a, b) => {
					int c = a.MethodRid.CompareTo(b.MethodRid);
					if (c != 0)
						return c;
					c = a.ScopeStart.CompareTo(b.ScopeStart);
					if (c != 0)
						return c;
					return b.ScopeLength.CompareTo(a.ScopeLength);
				});
				foreach (var info in methodScopeDebugInfos) {
					uint localScopeRid = (uint)debugMetadata.localScopeInfos.infos.Count + 1;
					var row = new RawLocalScopeRow(info.MethodRid, AddImportScope(info.Scope.ImportScope),
						(uint)debugMetadata.tablesHeap.LocalVariableTable.Rows + 1,
						(uint)debugMetadata.tablesHeap.LocalConstantTable.Rows + 1,
						info.ScopeStart, info.ScopeLength);
					debugMetadata.localScopeInfos.Add(info.Scope, row);
					var variables = info.Scope.Variables;
					int count = variables.Count;
					for (int i = 0; i < count; i++) {
						var local = variables[i];
						AddLocalVariable(local);
					}
					var constants = info.Scope.Constants;
					count = constants.Count;
					for (int i = 0; i < count; i++) {
						var constant = constants[i];
						AddLocalConstant(constant);
					}
					AddCustomDebugInformationList(Table.LocalScope, localScopeRid, info.Scope.CustomDebugInfos);
				}

				debugMetadata.tablesHeap.LocalScopeTable.IsSorted = true;
				foreach (var info in debugMetadata.localScopeInfos.infos)
					debugMetadata.tablesHeap.LocalScopeTable.Create(info.row);
			}
			if (!(serializerMethodContext is null))
				Free(ref serializerMethodContext);
		}

		static bool IsEmptyRootScope(CilBody cilBody, PdbScope scope) {
			if (scope.Variables.Count != 0)
				return false;
			if (scope.Constants.Count != 0)
				return false;
			if (scope.Namespaces.Count != 0)
				return false;
			if (!(scope.ImportScope is null))
				return false;
			if (scope.Scopes.Count != 0)
				return false;
			if (scope.CustomDebugInfos.Count != 0)
				return false;
			if (!(scope.End is null))
				return false;
			if (cilBody.Instructions.Count != 0 && cilBody.Instructions[0] != scope.Start)
				return false;

			return true;
		}

		/// <summary>
		/// Checks whether a list is empty or whether it contains only <c>null</c>s
		/// </summary>
		/// <typeparam name="T">Any type</typeparam>
		/// <param name="list">The list</param>
		/// <returns><c>true</c> if the list is empty or if it contains only <c>null</c>s, <c>false</c> otherwise</returns>
		protected static bool IsEmpty<T>(IList<T> list) where T : class {
			if (list is null)
				return true;
			int count = list.Count;
			for (int i = 0; i < count; i++) {
				if (!(list[i] is null))
					return false;
			}
			return true;
		}

		/// <inheritdoc/>
		public MDToken GetToken(object o) {
			if (o is IMDTokenProvider tp)
				return new MDToken(tp.MDToken.Table, AddMDTokenProvider(tp));

			if (o is string s)
				return new MDToken((Table)0x70, usHeap.Add(s));

			if (o is MethodSig methodSig)
				return new MDToken(Table.StandAloneSig, AddStandAloneSig(methodSig, methodSig.OriginalToken));

			if (o is FieldSig fieldSig)
				return new MDToken(Table.StandAloneSig, AddStandAloneSig(fieldSig, 0));

			if (o is null)
				Error("Instruction operand is null");
			else
				Error("Invalid instruction operand");
			return new MDToken((Table)0xFF, 0x00FFFFFF);
		}

		/// <inheritdoc/>
		public virtual MDToken GetToken(IList<TypeSig> locals, uint origToken) {
			if (locals is null || locals.Count == 0)
				return new MDToken((Table)0, 0);

			var row = new RawStandAloneSigRow(GetSignature(new LocalSig(locals, false)));
			uint rid = tablesHeap.StandAloneSigTable.Add(row);
			//TODO: Add custom attributes
			//TODO: Add custom debug infos
			return new MDToken(Table.StandAloneSig, rid);
		}

		/// <summary>
		/// Adds a <see cref="StandAloneSig"/>
		/// </summary>
		/// <param name="methodSig">Method signature</param>
		/// <param name="origToken">Original <c>StandAloneSig</c> token or 0 if none</param>
		/// <returns>Its new rid</returns>
		protected virtual uint AddStandAloneSig(MethodSig methodSig, uint origToken) {
			if (methodSig is null) {
				Error("StandAloneSig: MethodSig is null");
				return 0;
			}

			var row = new RawStandAloneSigRow(GetSignature(methodSig));
			uint rid = tablesHeap.StandAloneSigTable.Add(row);
			//TODO: Add custom attributes
			//TODO: Add custom debug infos
			return rid;
		}

		/// <summary>
		/// Adds a <see cref="StandAloneSig"/>
		/// </summary>
		/// <param name="fieldSig">FIeld signature</param>
		/// <param name="origToken">Original <c>StandAloneSig</c> token or 0 if none</param>
		/// <returns>Its new rid</returns>
		protected virtual uint AddStandAloneSig(FieldSig fieldSig, uint origToken) {
			if (fieldSig is null) {
				Error("StandAloneSig: FieldSig is null");
				return 0;
			}

			var row = new RawStandAloneSigRow(GetSignature(fieldSig));
			uint rid = tablesHeap.StandAloneSigTable.Add(row);
			//TODO: Add custom attributes
			//TODO: Add custom debug infos
			return rid;
		}

		uint AddMDTokenProvider(IMDTokenProvider tp) {
			if (!(tp is null)) {
				switch (tp.MDToken.Table) {
				case Table.Module:
					return AddModule((ModuleDef)tp);

				case Table.TypeRef:
					return AddTypeRef((TypeRef)tp);

				case Table.TypeDef:
					return GetRid((TypeDef)tp);

				case Table.Field:
					return GetRid((FieldDef)tp);

				case Table.Method:
					return GetRid((MethodDef)tp);

				case Table.Param:
					return GetRid((ParamDef)tp);

				case Table.MemberRef:
					return AddMemberRef((MemberRef)tp);

				case Table.StandAloneSig:
					return AddStandAloneSig((StandAloneSig)tp);

				case Table.Event:
					return GetRid((EventDef)tp);

				case Table.Property:
					return GetRid((PropertyDef)tp);

				case Table.ModuleRef:
					return AddModuleRef((ModuleRef)tp);

				case Table.TypeSpec:
					return AddTypeSpec((TypeSpec)tp);

				case Table.Assembly:
					return AddAssembly((AssemblyDef)tp, null);

				case Table.AssemblyRef:
					return AddAssemblyRef((AssemblyRef)tp);

				case Table.File:
					return AddFile((FileDef)tp);

				case Table.ExportedType:
					return AddExportedType((ExportedType)tp);

				case Table.MethodSpec:
					return AddMethodSpec((MethodSpec)tp);

				case Table.FieldPtr:
				case Table.MethodPtr:
				case Table.ParamPtr:
				case Table.InterfaceImpl:
				case Table.Constant:
				case Table.CustomAttribute:
				case Table.FieldMarshal:
				case Table.DeclSecurity:
				case Table.ClassLayout:
				case Table.FieldLayout:
				case Table.EventMap:
				case Table.EventPtr:
				case Table.PropertyMap:
				case Table.PropertyPtr:
				case Table.MethodSemantics:
				case Table.MethodImpl:
				case Table.ImplMap:
				case Table.FieldRVA:
				case Table.ENCLog:
				case Table.ENCMap:
				case Table.AssemblyProcessor:
				case Table.AssemblyOS:
				case Table.AssemblyRefProcessor:
				case Table.AssemblyRefOS:
				case Table.ManifestResource:
				case Table.NestedClass:
				case Table.GenericParam:
				case Table.GenericParamConstraint:
				case Table.Document:
				case Table.MethodDebugInformation:
				case Table.LocalScope:
				case Table.LocalVariable:
				case Table.LocalConstant:
				case Table.ImportScope:
				case Table.StateMachineMethod:
				case Table.CustomDebugInformation:
				default:
					break;
				}
			}

			if (tp is null)
				Error("IMDTokenProvider is null");
			else
				Error("Invalid IMDTokenProvider");
			return 0;
		}

		/// <summary>
		/// Adds a <see cref="ITypeDefOrRef"/>
		/// </summary>
		/// <param name="tdr">Value</param>
		/// <returns>Its encoded token</returns>
		protected uint AddTypeDefOrRef(ITypeDefOrRef tdr) {
			if (tdr is null) {
				Error("TypeDefOrRef is null");
				return 0;
			}

			var token = new MDToken(tdr.MDToken.Table, AddMDTokenProvider(tdr));
			if (!CodedToken.TypeDefOrRef.Encode(token, out uint encodedToken)) {
				Error("Can't encode TypeDefOrRef token {0:X8}", token.Raw);
				encodedToken = 0;
			}
			return encodedToken;
		}

		/// <summary>
		/// Adds a <see cref="IResolutionScope"/>
		/// </summary>
		/// <param name="rs">Value</param>
		/// <returns>Its encoded token</returns>
		protected uint AddResolutionScope(IResolutionScope rs) {
			if (rs is null) {
				Error("ResolutionScope is null");
				return 0;
			}

			var token = new MDToken(rs.MDToken.Table, AddMDTokenProvider(rs));
			if (!CodedToken.ResolutionScope.Encode(token, out uint encodedToken)) {
				Error("Can't encode ResolutionScope token {0:X8}", token.Raw);
				encodedToken = 0;
			}
			return encodedToken;
		}

		/// <summary>
		/// Adds a <see cref="IMethodDefOrRef"/>
		/// </summary>
		/// <param name="mdr">Value</param>
		/// <returns>Its encoded token</returns>
		protected uint AddMethodDefOrRef(IMethodDefOrRef mdr) {
			if (mdr is null) {
				Error("MethodDefOrRef is null");
				return 0;
			}

			var token = new MDToken(mdr.MDToken.Table, AddMDTokenProvider(mdr));
			if (!CodedToken.MethodDefOrRef.Encode(token, out uint encodedToken)) {
				Error("Can't encode MethodDefOrRef token {0:X8}", token.Raw);
				encodedToken = 0;
			}
			return encodedToken;
		}

		/// <summary>
		/// Adds a <see cref="IMemberRefParent"/>
		/// </summary>
		/// <param name="parent">Value</param>
		/// <returns>Its encoded token</returns>
		protected uint AddMemberRefParent(IMemberRefParent parent) {
			if (parent is null) {
				Error("MemberRefParent is null");
				return 0;
			}

			var token = new MDToken(parent.MDToken.Table, AddMDTokenProvider(parent));
			if (!CodedToken.MemberRefParent.Encode(token, out uint encodedToken)) {
				Error("Can't encode MemberRefParent token {0:X8}", token.Raw);
				encodedToken = 0;
			}
			return encodedToken;
		}

		/// <summary>
		/// Adds a <see cref="IImplementation"/>
		/// </summary>
		/// <param name="impl">Value</param>
		/// <returns>Its encoded token</returns>
		protected uint AddImplementation(IImplementation impl) {
			if (impl is null) {
				Error("Implementation is null");
				return 0;
			}

			var token = new MDToken(impl.MDToken.Table, AddMDTokenProvider(impl));
			if (!CodedToken.Implementation.Encode(token, out uint encodedToken)) {
				Error("Can't encode Implementation token {0:X8}", token.Raw);
				encodedToken = 0;
			}
			return encodedToken;
		}

		/// <summary>
		/// Adds a <see cref="ICustomAttributeType"/>
		/// </summary>
		/// <param name="cat">Value</param>
		/// <returns>Its encoded token</returns>
		protected uint AddCustomAttributeType(ICustomAttributeType cat) {
			if (cat is null) {
				Error("CustomAttributeType is null");
				return 0;
			}

			var token = new MDToken(cat.MDToken.Table, AddMDTokenProvider(cat));
			if (!CodedToken.CustomAttributeType.Encode(token, out uint encodedToken)) {
				Error("Can't encode CustomAttributeType token {0:X8}", token.Raw);
				encodedToken = 0;
			}
			return encodedToken;
		}

		/// <summary>
		/// Adds a <c>NestedType</c> row
		/// </summary>
		/// <param name="nestedType">Nested type</param>
		/// <param name="declaringType">Declaring type</param>
		protected void AddNestedType(TypeDef nestedType, TypeDef declaringType) {
			if (nestedType is null || declaringType is null)
				return;
			uint nestedRid = GetRid(nestedType);
			uint dtRid = GetRid(declaringType);
			if (nestedRid == 0 || dtRid == 0)
				return;
			var row = new RawNestedClassRow(nestedRid, dtRid);
			nestedClassInfos.Add(declaringType, row);
		}

		/// <summary>
		/// Adds a <c>Module</c> row
		/// </summary>
		/// <param name="module">Module</param>
		/// <returns>Its new rid</returns>
		protected uint AddModule(ModuleDef module) {
			if (module is null) {
				Error("Module is null");
				return 0;
			}
			if (this.module != module)
				Error("Module {0} must be referenced with a ModuleRef, not a ModuleDef", module);
			if (moduleDefInfos.TryGetRid(module, out uint rid))
				return rid;
			var row = new RawModuleRow(module.Generation,
								stringsHeap.Add(module.Name),
								guidHeap.Add(module.Mvid),
								guidHeap.Add(module.EncId),
								guidHeap.Add(module.EncBaseId));
			rid = tablesHeap.ModuleTable.Add(row);
			moduleDefInfos.Add(module, rid);
			AddCustomAttributes(Table.Module, rid, module);
			AddCustomDebugInformationList(Table.Module, rid, module);
			return rid;
		}

		/// <summary>
		/// Adds a <c>ModuleRef</c> row
		/// </summary>
		/// <param name="modRef">Module ref</param>
		/// <returns>Its new rid</returns>
		protected uint AddModuleRef(ModuleRef modRef) {
			if (modRef is null) {
				Error("ModuleRef is null");
				return 0;
			}
			if (moduleRefInfos.TryGetRid(modRef, out uint rid))
				return rid;
			var row = new RawModuleRefRow(stringsHeap.Add(modRef.Name));
			rid = tablesHeap.ModuleRefTable.Add(row);
			moduleRefInfos.Add(modRef, rid);
			AddCustomAttributes(Table.ModuleRef, rid, modRef);
			AddCustomDebugInformationList(Table.ModuleRef, rid, modRef);
			return rid;
		}

		/// <summary>
		/// Adds an <c>AssemblyRef</c> row
		/// </summary>
		/// <param name="asmRef">Assembly ref</param>
		/// <returns>Its new rid</returns>
		protected uint AddAssemblyRef(AssemblyRef asmRef) {
			if (asmRef is null) {
				Error("AssemblyRef is null");
				return 0;
			}
			if (assemblyRefInfos.TryGetRid(asmRef, out uint rid))
				return rid;
			var version = Utils.CreateVersionWithNoUndefinedValues(asmRef.Version);
			var row = new RawAssemblyRefRow((ushort)version.Major,
							(ushort)version.Minor,
							(ushort)version.Build,
							(ushort)version.Revision,
							(uint)asmRef.Attributes,
							blobHeap.Add(PublicKeyBase.GetRawData(asmRef.PublicKeyOrToken)),
							stringsHeap.Add(asmRef.Name),
							stringsHeap.Add(asmRef.Culture),
							blobHeap.Add(asmRef.Hash));
			rid = tablesHeap.AssemblyRefTable.Add(row);
			assemblyRefInfos.Add(asmRef, rid);
			AddCustomAttributes(Table.AssemblyRef, rid, asmRef);
			AddCustomDebugInformationList(Table.AssemblyRef, rid, asmRef);
			return rid;
		}

		/// <summary>
		/// Adds an <c>Assembly</c> row
		/// </summary>
		/// <param name="asm">Assembly</param>
		/// <param name="publicKey">The public key that should be used</param>
		/// <returns>Its new rid</returns>
		protected uint AddAssembly(AssemblyDef asm, byte[] publicKey) {
			if (asm is null) {
				Error("Assembly is null");
				return 0;
			}
			if (assemblyInfos.TryGetRid(asm, out uint rid))
				return rid;

			var asmAttrs = asm.Attributes;
			if (!(publicKey is null))
				asmAttrs |= AssemblyAttributes.PublicKey;
			else
				publicKey = PublicKeyBase.GetRawData(asm.PublicKeyOrToken);

			var version = Utils.CreateVersionWithNoUndefinedValues(asm.Version);
			var row = new RawAssemblyRow((uint)asm.HashAlgorithm,
							(ushort)version.Major,
							(ushort)version.Minor,
							(ushort)version.Build,
							(ushort)version.Revision,
							(uint)asmAttrs,
							blobHeap.Add(publicKey),
							stringsHeap.Add(asm.Name),
							stringsHeap.Add(asm.Culture));
			rid = tablesHeap.AssemblyTable.Add(row);
			assemblyInfos.Add(asm, rid);
			AddDeclSecurities(new MDToken(Table.Assembly, rid), asm.DeclSecurities);
			AddCustomAttributes(Table.Assembly, rid, asm);
			AddCustomDebugInformationList(Table.Assembly, rid, asm);
			return rid;
		}

		/// <summary>
		/// Adds generic parameters
		/// </summary>
		/// <param name="token">New token of owner</param>
		/// <param name="gps">All generic params</param>
		protected void AddGenericParams(MDToken token, IList<GenericParam> gps) {
			if (gps is null)
				return;
			int count = gps.Count;
			for (int i = 0; i < count; i++)
				AddGenericParam(token, gps[i]);
		}

		/// <summary>
		/// Adds a generic param
		/// </summary>
		/// <param name="owner">New token of owner</param>
		/// <param name="gp">Generic paramater</param>
		protected void AddGenericParam(MDToken owner, GenericParam gp) {
			if (gp is null) {
				Error("GenericParam is null");
				return;
			}
			if (!CodedToken.TypeOrMethodDef.Encode(owner, out uint encodedOwner)) {
				Error("Can't encode TypeOrMethodDef token {0:X8}", owner.Raw);
				encodedOwner = 0;
			}
			var row = new RawGenericParamRow(gp.Number,
							(ushort)gp.Flags,
							encodedOwner,
							stringsHeap.Add(gp.Name),
							gp.Kind is null ? 0 : AddTypeDefOrRef(gp.Kind));
			genericParamInfos.Add(gp, row);
		}

		void AddGenericParamConstraints(IList<GenericParam> gps) {
			if (gps is null)
				return;
			int count = gps.Count;
			for (int i = 0; i < count; i++) {
				var gp = gps[i];
				if (gp is null)
					continue;
				uint rid = genericParamInfos.Rid(gp);
				AddGenericParamConstraints(rid, gp.GenericParamConstraints);
			}
		}

		/// <summary>
		/// Adds generic parameter constraints
		/// </summary>
		/// <param name="gpRid">New rid of owner generic param</param>
		/// <param name="constraints">Its constraints</param>
		protected void AddGenericParamConstraints(uint gpRid, IList<GenericParamConstraint> constraints) {
			if (constraints is null)
				return;
			int count = constraints.Count;
			for (int i = 0; i < count; i++)
				AddGenericParamConstraint(gpRid, constraints[i]);
		}

		/// <summary>
		/// Adds a generic parameter constraint
		/// </summary>
		/// <param name="gpRid">New rid of owner generic param</param>
		/// <param name="gpc">Generic parameter constraint</param>
		protected void AddGenericParamConstraint(uint gpRid, GenericParamConstraint gpc) {
			if (gpc is null) {
				Error("GenericParamConstraint is null");
				return;
			}
			var row = new RawGenericParamConstraintRow(gpRid, AddTypeDefOrRef(gpc.Constraint));
			genericParamConstraintInfos.Add(gpc, row);
		}

		/// <summary>
		/// Adds a <c>InterfaceImpl</c> row
		/// </summary>
		/// <param name="typeDefRid">New rid of owner</param>
		/// <param name="ifaces">All interfaces</param>
		protected void AddInterfaceImpls(uint typeDefRid, IList<InterfaceImpl> ifaces) {
			int count = ifaces.Count;
			for (int i = 0; i < count; i++) {
				var iface = ifaces[i];
				if (iface is null)
					continue;
				var row = new RawInterfaceImplRow(typeDefRid,
							AddTypeDefOrRef(iface.Interface));
				interfaceImplInfos.Add(iface, row);
			}
		}

		/// <summary>
		/// Adds a <c>FieldLayout</c> row
		/// </summary>
		/// <param name="field">Owner field</param>
		protected void AddFieldLayout(FieldDef field) {
			if (field is null || field.FieldOffset is null)
				return;
			var rid = GetRid(field);
			var row = new RawFieldLayoutRow(field.FieldOffset.Value, rid);
			fieldLayoutInfos.Add(field, row);
		}

		/// <summary>
		/// Adds a <c>FieldMarshal</c> row
		/// </summary>
		/// <param name="parent">New owner token</param>
		/// <param name="hfm">Owner</param>
		protected void AddFieldMarshal(MDToken parent, IHasFieldMarshal hfm) {
			if (hfm is null || hfm.MarshalType is null)
				return;
			var fieldMarshal = hfm.MarshalType;
			if (!CodedToken.HasFieldMarshal.Encode(parent, out uint encodedParent)) {
				Error("Can't encode HasFieldMarshal token {0:X8}", parent.Raw);
				encodedParent = 0;
			}
			var row = new RawFieldMarshalRow(encodedParent,
						blobHeap.Add(MarshalBlobWriter.Write(module, fieldMarshal, this, OptimizeCustomAttributeSerializedTypeNames)));
			fieldMarshalInfos.Add(hfm, row);
		}

		/// <summary>
		/// Adds a <c>FieldRVA</c> row
		/// </summary>
		/// <param name="field">The field</param>
		protected void AddFieldRVA(FieldDef field) {
			Debug.Assert(!isStandaloneDebugMetadata);
			if (NoFieldData)
				return;
			if (field.RVA != 0 && KeepFieldRVA) {
				uint rid = GetRid(field);
				var row = new RawFieldRVARow((uint)field.RVA, rid);
				fieldRVAInfos.Add(field, row);
			}
			else {
				if (field is null || field.InitialValue is null)
					return;
				var ivBytes = field.InitialValue;
				if (!VerifyFieldSize(field, ivBytes.Length))
					Error("Field {0} ({1:X8}) initial value size != size of field type", field, field.MDToken.Raw);
				uint rid = GetRid(field);
				var iv = constants.Add(new ByteArrayChunk(ivBytes), ModuleWriterBase.DEFAULT_CONSTANTS_ALIGNMENT);
				fieldToInitialValue[field] = iv;
				var row = new RawFieldRVARow(0, rid);
				fieldRVAInfos.Add(field, row);
			}
		}

		static bool VerifyFieldSize(FieldDef field, int size) {
			if (field is null)
				return false;
			var sig = field.FieldSig;
			if (sig is null)
				return false;
			return field.GetFieldSize() == size;
		}

		/// <summary>
		/// Adds a <c>ImplMap</c> row
		/// </summary>
		/// <param name="parent">New owner token</param>
		/// <param name="mf">Owner</param>
		protected void AddImplMap(MDToken parent, IMemberForwarded mf) {
			if (mf is null || mf.ImplMap is null)
				return;
			var implMap = mf.ImplMap;
			if (!CodedToken.MemberForwarded.Encode(parent, out uint encodedParent)) {
				Error("Can't encode MemberForwarded token {0:X8}", parent.Raw);
				encodedParent = 0;
			}
			var row = new RawImplMapRow((ushort)implMap.Attributes,
						encodedParent,
						stringsHeap.Add(implMap.Name),
						AddModuleRef(implMap.Module));
			implMapInfos.Add(mf, row);
		}

		/// <summary>
		/// Adds a <c>Constant</c> row
		/// </summary>
		/// <param name="parent">New owner token</param>
		/// <param name="hc">Owner</param>
		protected void AddConstant(MDToken parent, IHasConstant hc) {
			if (hc is null || hc.Constant is null)
				return;
			var constant = hc.Constant;
			if (!CodedToken.HasConstant.Encode(parent, out uint encodedParent)) {
				Error("Can't encode HasConstant token {0:X8}", parent.Raw);
				encodedParent = 0;
			}
			var row = new RawConstantRow((byte)constant.Type, 0,
						encodedParent,
						blobHeap.Add(GetConstantValueAsByteArray(constant.Type, constant.Value)));
			hasConstantInfos.Add(hc, row);
		}

		static readonly byte[] constantClassByteArray = new byte[4];
		static readonly byte[] constantDefaultByteArray = new byte[8];
		byte[] GetConstantValueAsByteArray(ElementType etype, object o) {
			if (o is null) {
				if (etype == ElementType.Class)
					return constantClassByteArray;
				Error("Constant is null");
				return constantDefaultByteArray;
			}

			var typeCode = Type.GetTypeCode(o.GetType());
			switch (typeCode) {
			case TypeCode.Boolean:
				VerifyConstantType(etype, ElementType.Boolean);
				return BitConverter.GetBytes((bool)o);

			case TypeCode.Char:
				VerifyConstantType(etype, ElementType.Char);
				return BitConverter.GetBytes((char)o);

			case TypeCode.SByte:
				VerifyConstantType(etype, ElementType.I1);
				return new byte[1] { (byte)(sbyte)o };

			case TypeCode.Byte:
				VerifyConstantType(etype, ElementType.U1);
				return new byte[1] { (byte)o };

			case TypeCode.Int16:
				VerifyConstantType(etype, ElementType.I2);
				return BitConverter.GetBytes((short)o);

			case TypeCode.UInt16:
				VerifyConstantType(etype, ElementType.U2);
				return BitConverter.GetBytes((ushort)o);

			case TypeCode.Int32:
				VerifyConstantType(etype, ElementType.I4);
				return BitConverter.GetBytes((int)o);

			case TypeCode.UInt32:
				VerifyConstantType(etype, ElementType.U4);
				return BitConverter.GetBytes((uint)o);

			case TypeCode.Int64:
				VerifyConstantType(etype, ElementType.I8);
				return BitConverter.GetBytes((long)o);

			case TypeCode.UInt64:
				VerifyConstantType(etype, ElementType.U8);
				return BitConverter.GetBytes((ulong)o);

			case TypeCode.Single:
				VerifyConstantType(etype, ElementType.R4);
				return BitConverter.GetBytes((float)o);

			case TypeCode.Double:
				VerifyConstantType(etype, ElementType.R8);
				return BitConverter.GetBytes((double)o);

			case TypeCode.String:
				VerifyConstantType(etype, ElementType.String);
				return Encoding.Unicode.GetBytes((string)o);

			default:
				Error("Invalid constant type: {0}", typeCode);
				return constantDefaultByteArray;
			}
		}

		void VerifyConstantType(ElementType realType, ElementType expectedType) {
			if (realType != expectedType)
				Error("Constant value's type is the wrong type: {0} != {1}", realType, expectedType);
		}

		/// <summary>
		/// Adds a <c>DeclSecurity</c> row
		/// </summary>
		/// <param name="parent">New owner token</param>
		/// <param name="declSecurities">All <c>DeclSecurity</c> rows</param>
		protected void AddDeclSecurities(MDToken parent, IList<DeclSecurity> declSecurities) {
			if (declSecurities is null)
				return;
			if (!CodedToken.HasDeclSecurity.Encode(parent, out uint encodedParent)) {
				Error("Can't encode HasDeclSecurity token {0:X8}", parent.Raw);
				encodedParent = 0;
			}
			var bwctx = AllocBinaryWriterContext();
			int count = declSecurities.Count;
			for (int i = 0; i < count; i++) {
				var decl = declSecurities[i];
				if (decl is null)
					continue;
				var row = new RawDeclSecurityRow((short)decl.Action,
							encodedParent,
							blobHeap.Add(DeclSecurityWriter.Write(module, decl.SecurityAttributes, this, OptimizeCustomAttributeSerializedTypeNames, bwctx)));
				declSecurityInfos.Add(decl, row);
			}
			Free(ref bwctx);
		}

		/// <summary>
		/// Adds <c>MethodSemantics</c> rows
		/// </summary>
		/// <param name="evt">Event</param>
		protected void AddMethodSemantics(EventDef evt) {
			if (evt is null) {
				Error("Event is null");
				return;
			}
			uint rid = GetRid(evt);
			if (rid == 0)
				return;
			var token = new MDToken(Table.Event, rid);
			AddMethodSemantics(token, evt.AddMethod, MethodSemanticsAttributes.AddOn);
			AddMethodSemantics(token, evt.RemoveMethod, MethodSemanticsAttributes.RemoveOn);
			AddMethodSemantics(token, evt.InvokeMethod, MethodSemanticsAttributes.Fire);
			AddMethodSemantics(token, evt.OtherMethods, MethodSemanticsAttributes.Other);
		}

		/// <summary>
		/// Adds <c>MethodSemantics</c> rows
		/// </summary>
		/// <param name="prop">Property</param>
		protected void AddMethodSemantics(PropertyDef prop) {
			if (prop is null) {
				Error("Property is null");
				return;
			}
			uint rid = GetRid(prop);
			if (rid == 0)
				return;
			var token = new MDToken(Table.Property, rid);
			AddMethodSemantics(token, prop.GetMethods, MethodSemanticsAttributes.Getter);
			AddMethodSemantics(token, prop.SetMethods, MethodSemanticsAttributes.Setter);
			AddMethodSemantics(token, prop.OtherMethods, MethodSemanticsAttributes.Other);
		}

		void AddMethodSemantics(MDToken owner, IList<MethodDef> methods, MethodSemanticsAttributes attrs) {
			if (methods is null)
				return;
			int count = methods.Count;
			for (int i = 0; i < count; i++)
				AddMethodSemantics(owner, methods[i], attrs);
		}

		void AddMethodSemantics(MDToken owner, MethodDef method, MethodSemanticsAttributes flags) {
			if (method is null)
				return;
			uint methodRid = GetRid(method);
			if (methodRid == 0)
				return;
			if (!CodedToken.HasSemantic.Encode(owner, out uint encodedOwner)) {
				Error("Can't encode HasSemantic token {0:X8}", owner.Raw);
				encodedOwner = 0;
			}
			var row = new RawMethodSemanticsRow((ushort)flags, methodRid, encodedOwner);
			methodSemanticsInfos.Add(method, row);
		}

		void AddMethodImpls(MethodDef method, IList<MethodOverride> overrides) {
			if (overrides is null)
				return;
			if (method.DeclaringType is null) {
				Error("Method declaring type is null. Method {0} ({1:X8})", method, method.MDToken.Raw);
				return;
			}
			if (overrides.Count != 0) {
				uint rid = GetRid(method.DeclaringType);
				int count = overrides.Count;
				for (int i = 0; i < count; i++) {
					var ovr = overrides[i];
					var row = new RawMethodImplRow(rid,
								AddMethodDefOrRef(ovr.MethodBody),
								AddMethodDefOrRef(ovr.MethodDeclaration));
					methodImplInfos.Add(method, row);
				}
			}
		}

		/// <summary>
		/// Adds a <c>ClassLayout</c> row
		/// </summary>
		/// <param name="type">Type</param>
		protected void AddClassLayout(TypeDef type) {
			if (type is null || type.ClassLayout is null)
				return;
			var rid = GetRid(type);
			var classLayout = type.ClassLayout;
			var row = new RawClassLayoutRow(classLayout.PackingSize, classLayout.ClassSize, rid);
			classLayoutInfos.Add(type, row);
		}

		void AddResources(IList<Resource> resources) {
			if (NoDotNetResources)
				return;
			if (resources is null)
				return;
			int count = resources.Count;
			for (int i = 0; i < count; i++)
				AddResource(resources[i]);
		}

		void AddResource(Resource resource) {
			Debug.Assert(!NoDotNetResources);
			if (resource is EmbeddedResource er) {
				AddEmbeddedResource(er);
				return;
			}

			if (resource is AssemblyLinkedResource alr) {
				AddAssemblyLinkedResource(alr);
				return;
			}

			if (resource is LinkedResource lr) {
				AddLinkedResource(lr);
				return;
			}

			if (resource is null)
				Error("Resource is null");
			else
				Error("Invalid resource type: {0}", resource.GetType());
		}

		uint AddEmbeddedResource(EmbeddedResource er) {
			Debug.Assert(!isStandaloneDebugMetadata);
			Debug.Assert(!NoDotNetResources);
			if (er is null) {
				Error("EmbeddedResource is null");
				return 0;
			}
			if (manifestResourceInfos.TryGetRid(er, out uint rid))
				return rid;
			var row = new RawManifestResourceRow(netResources.NextOffset,
						(uint)er.Attributes,
						stringsHeap.Add(er.Name),
						0);
			rid = tablesHeap.ManifestResourceTable.Add(row);
			manifestResourceInfos.Add(er, rid);
			embeddedResourceToByteArray[er] = netResources.Add(er.CreateReader());
			//TODO: Add custom attributes
			//TODO: Add custom debug infos
			return rid;
		}

		uint AddAssemblyLinkedResource(AssemblyLinkedResource alr) {
			Debug.Assert(!NoDotNetResources);
			if (alr is null) {
				Error("AssemblyLinkedResource is null");
				return 0;
			}
			if (manifestResourceInfos.TryGetRid(alr, out uint rid))
				return rid;
			var row = new RawManifestResourceRow(0,
						(uint)alr.Attributes,
						stringsHeap.Add(alr.Name),
						AddImplementation(alr.Assembly));
			rid = tablesHeap.ManifestResourceTable.Add(row);
			manifestResourceInfos.Add(alr, rid);
			//TODO: Add custom attributes
			//TODO: Add custom debug infos
			return rid;
		}

		uint AddLinkedResource(LinkedResource lr) {
			Debug.Assert(!NoDotNetResources);
			if (lr is null) {
				Error("LinkedResource is null");
				return 0;
			}
			if (manifestResourceInfos.TryGetRid(lr, out uint rid))
				return rid;
			var row = new RawManifestResourceRow(0,
						(uint)lr.Attributes,
						stringsHeap.Add(lr.Name),
						AddImplementation(lr.File));
			rid = tablesHeap.ManifestResourceTable.Add(row);
			manifestResourceInfos.Add(lr, rid);
			//TODO: Add custom attributes
			//TODO: Add custom debug infos
			return rid;
		}

		/// <summary>
		/// Adds a <c>File</c> row
		/// </summary>
		/// <param name="file">File</param>
		/// <returns>Its new rid</returns>
		protected uint AddFile(FileDef file) {
			if (file is null) {
				Error("FileDef is null");
				return 0;
			}
			if (fileDefInfos.TryGetRid(file, out uint rid))
				return rid;
			var row = new RawFileRow((uint)file.Flags,
						stringsHeap.Add(file.Name),
						blobHeap.Add(file.HashValue));	//TODO: Re-calculate the hash value if possible
			rid = tablesHeap.FileTable.Add(row);
			fileDefInfos.Add(file, rid);
			AddCustomAttributes(Table.File, rid, file);
			AddCustomDebugInformationList(Table.File, rid, file);
			return rid;
		}

		/// <summary>
		/// Adds a <c>ExportedType</c> row
		/// </summary>
		/// <param name="et">Exported type</param>
		/// <returns>Its new rid</returns>
		protected uint AddExportedType(ExportedType et) {
			if (et is null) {
				Error("ExportedType is null");
				return 0;
			}
			if (exportedTypeInfos.TryGetRid(et, out uint rid))
				return rid;
			exportedTypeInfos.Add(et, 0);	// Prevent inf recursion
			var row = new RawExportedTypeRow((uint)et.Attributes,
						et.TypeDefId,	//TODO: Should be updated with the new rid
						stringsHeap.Add(et.TypeName),
						stringsHeap.Add(et.TypeNamespace),
						AddImplementation(et.Implementation));
			rid = tablesHeap.ExportedTypeTable.Add(row);
			exportedTypeInfos.SetRid(et, rid);
			AddCustomAttributes(Table.ExportedType, rid, et);
			AddCustomDebugInformationList(Table.ExportedType, rid, et);
			return rid;
		}

		/// <summary>
		/// Gets a #Blob offset of a type signature
		/// </summary>
		/// <param name="ts">Type sig</param>
		/// <param name="extraData">Extra data to append the signature if
		/// <see cref="PreserveExtraSignatureData"/> is <c>true</c>.</param>
		/// <returns>#Blob offset</returns>
		protected uint GetSignature(TypeSig ts, byte[] extraData) {
			byte[] blob;
			if (ts is null) {
				Error("TypeSig is null");
				blob = null;
			}
			else {
				var bwctx = AllocBinaryWriterContext();
				blob = SignatureWriter.Write(this, ts, bwctx);
				Free(ref bwctx);
			}
			AppendExtraData(ref blob, extraData);
			return blobHeap.Add(blob);
		}

		/// <summary>
		/// Gets a #Blob offset of a calling convention signature
		/// </summary>
		/// <param name="sig">Signature</param>
		/// <returns>#Blob offset</returns>
		protected uint GetSignature(CallingConventionSig sig) {
			if (sig is null) {
				Error("CallingConventionSig is null");
				return 0;
			}

			var bwctx = AllocBinaryWriterContext();
			var blob = SignatureWriter.Write(this, sig, bwctx);
			Free(ref bwctx);
			AppendExtraData(ref blob, sig.ExtraData);
			return blobHeap.Add(blob);
		}

		void AppendExtraData(ref byte[] blob, byte[] extraData) {
			if (PreserveExtraSignatureData && !(extraData is null) && extraData.Length > 0) {
				int blen = blob is null ? 0 : blob.Length;
				Array.Resize(ref blob, blen + extraData.Length);
				Array.Copy(extraData, 0, blob, blen, extraData.Length);
			}
		}

		/// <summary>
		/// Adds a <c>CustomAttribute</c> row
		/// </summary>
		/// <param name="table">Owner table</param>
		/// <param name="rid">New owner rid</param>
		/// <param name="hca">Onwer</param>
		protected void AddCustomAttributes(Table table, uint rid, IHasCustomAttribute hca) => AddCustomAttributes(table, rid, hca.CustomAttributes);

		void AddCustomAttributes(Table table, uint rid, CustomAttributeCollection caList) {
			var token = new MDToken(table, rid);
			int count = caList.Count;
			for (int i = 0; i < count; i++)
				AddCustomAttribute(token, caList[i]);
		}

		void AddCustomAttribute(MDToken token, CustomAttribute ca) {
			if (ca is null) {
				Error("Custom attribute is null");
				return;
			}
			if (!CodedToken.HasCustomAttribute.Encode(token, out uint encodedToken)) {
				Error("Can't encode HasCustomAttribute token {0:X8}", token.Raw);
				encodedToken = 0;
			}
			var bwctx = AllocBinaryWriterContext();
			var caBlob = CustomAttributeWriter.Write(this, ca, bwctx);
			Free(ref bwctx);
			var row = new RawCustomAttributeRow(encodedToken,
						AddCustomAttributeType(ca.Constructor),
						blobHeap.Add(caBlob));
			customAttributeInfos.Add(ca, row);
		}

		void AddCustomDebugInformationList(MethodDef method, uint rid, uint localVarSigToken) {
			Debug.Assert(!(debugMetadata is null));
			if (debugMetadata is null)
				return;
			var serializerMethodContext = AllocSerializerMethodContext();
			serializerMethodContext.SetBody(method);
			if (method.CustomDebugInfos.Count != 0)
				AddCustomDebugInformationCore(serializerMethodContext, Table.Method, rid, method.CustomDebugInfos);
			AddMethodDebugInformation(method, rid, localVarSigToken);
			Free(ref serializerMethodContext);
		}

		void AddMethodDebugInformation(MethodDef method, uint rid, uint localVarSigToken) {
			Debug.Assert(!(debugMetadata is null));
			var body = method.Body;
			if (body is null)
				return;

			GetSingleDocument(body, out var singleDoc, out var firstDoc, out bool hasNoSeqPoints);
			if (hasNoSeqPoints)
				return;

			var bwctx = AllocBinaryWriterContext();
			var outStream = bwctx.OutStream;
			var writer = bwctx.Writer;
			outStream.SetLength(0);
			outStream.Position = 0;

			writer.WriteCompressedUInt32(localVarSigToken);
			if (singleDoc is null)
				writer.WriteCompressedUInt32(VerifyGetRid(firstDoc));

			var instrs = body.Instructions;
			var currentDoc = firstDoc;
			uint ilOffset = uint.MaxValue;
			int line = -1, column = 0;
			uint instrOffset = 0;
			Instruction instr = null;
			for (int i = 0; i < instrs.Count; i++, instrOffset += (uint)instr.GetSize()) {
				instr = instrs[i];
				var seqPoint = instr.SequencePoint;
				if (seqPoint is null)
					continue;
				if (seqPoint.Document is null) {
					Error("PDB document is null");
					return;
				}
				if (currentDoc != seqPoint.Document) {
					// document-record

					currentDoc = seqPoint.Document;
					writer.WriteCompressedUInt32(0);
					writer.WriteCompressedUInt32(VerifyGetRid(currentDoc));
				}

				// SequencePointRecord

				if (ilOffset == uint.MaxValue)
					writer.WriteCompressedUInt32(instrOffset);
				else
					writer.WriteCompressedUInt32(instrOffset - ilOffset);
				ilOffset = instrOffset;

				if (seqPoint.StartLine == SequencePointConstants.HIDDEN_LINE && seqPoint.EndLine == SequencePointConstants.HIDDEN_LINE) {
					// hidden-sequence-point-record

					writer.WriteCompressedUInt32(0);
					writer.WriteCompressedUInt32(0);
				}
				else {
					// sequence-point-record

					uint dlines = (uint)(seqPoint.EndLine - seqPoint.StartLine);
					int dcolumns = seqPoint.EndColumn - seqPoint.StartColumn;
					writer.WriteCompressedUInt32(dlines);
					if (dlines == 0)
						writer.WriteCompressedUInt32((uint)dcolumns);
					else
						writer.WriteCompressedInt32(dcolumns);

					if (line < 0) {
						writer.WriteCompressedUInt32((uint)seqPoint.StartLine);
						writer.WriteCompressedUInt32((uint)seqPoint.StartColumn);
					}
					else {
						writer.WriteCompressedInt32(seqPoint.StartLine - line);
						writer.WriteCompressedInt32(seqPoint.StartColumn - column);
					}
					line = seqPoint.StartLine;
					column = seqPoint.StartColumn;
				}
			}

			var seqPointsBlob = outStream.ToArray();
			var row = new RawMethodDebugInformationRow(singleDoc is null ? 0 : AddPdbDocument(singleDoc), debugMetadata.blobHeap.Add(seqPointsBlob));
			debugMetadata.tablesHeap.MethodDebugInformationTable[rid] = row;
			debugMetadata.methodDebugInformationInfosUsed = true;
			Free(ref bwctx);
		}

		uint VerifyGetRid(PdbDocument doc) {
			Debug.Assert(!(debugMetadata is null));
			if (!debugMetadata.pdbDocumentInfos.TryGetRid(doc, out uint rid)) {
				Error("PDB document has been removed");
				return 0;
			}
			return rid;
		}

		static void GetSingleDocument(CilBody body, out PdbDocument singleDoc, out PdbDocument firstDoc, out bool hasNoSeqPoints) {
			var instrs = body.Instructions;
			int docCount = 0;
			singleDoc = null;
			firstDoc = null;
			for (int i = 0; i < instrs.Count; i++) {
				var seqPt = instrs[i].SequencePoint;
				if (seqPt is null)
					continue;
				var doc = seqPt.Document;
				if (doc is null)
					continue;
				if (firstDoc is null)
					firstDoc = doc;
				if (singleDoc != doc) {
					singleDoc = doc;
					docCount++;
					if (docCount > 1)
						break;
				}
			}
			hasNoSeqPoints = docCount == 0;
			if (docCount != 1)
				singleDoc = null;
		}

		/// <summary>
		/// Adds a <c>CustomDebugInformation</c> row
		/// </summary>
		/// <param name="table">Owner table</param>
		/// <param name="rid">New owner rid</param>
		/// <param name="hcdi">Onwer</param>
		protected void AddCustomDebugInformationList(Table table, uint rid, IHasCustomDebugInformation hcdi) {
			Debug.Assert(table != Table.Method);
			if (debugMetadata is null)
				return;
			if (hcdi.CustomDebugInfos.Count == 0)
				return;
			var serializerMethodContext = AllocSerializerMethodContext();
			serializerMethodContext.SetBody(null);
			AddCustomDebugInformationCore(serializerMethodContext, table, rid, hcdi.CustomDebugInfos);
			Free(ref serializerMethodContext);
		}

		void AddCustomDebugInformationList(Table table, uint rid, IList<PdbCustomDebugInfo> cdis) {
			Debug.Assert(table != Table.Method);
			if (debugMetadata is null)
				return;
			if (cdis.Count == 0)
				return;
			var serializerMethodContext = AllocSerializerMethodContext();
			serializerMethodContext.SetBody(null);
			AddCustomDebugInformationCore(serializerMethodContext, table, rid, cdis);
			Free(ref serializerMethodContext);
		}

		void AddCustomDebugInformationCore(SerializerMethodContext serializerMethodContext, Table table, uint rid, IList<PdbCustomDebugInfo> cdis) {
			Debug.Assert(!(debugMetadata is null));
			Debug.Assert(cdis.Count != 0);

			var token = new MDToken(table, rid);
			if (!CodedToken.HasCustomDebugInformation.Encode(token, out uint encodedToken)) {
				Error("Couldn't encode HasCustomDebugInformation token {0:X8}", token.Raw);
				return;
			}

			for (int i = 0; i < cdis.Count; i++) {
				var cdi = cdis[i];
				if (cdi is null) {
					Error("Custom debug info is null");
					continue;
				}

				AddCustomDebugInformation(serializerMethodContext, token.Raw, encodedToken, cdi);
			}
		}

		void AddCustomDebugInformation(SerializerMethodContext serializerMethodContext, uint token, uint encodedToken, PdbCustomDebugInfo cdi) {
			Debug.Assert(!(debugMetadata is null));

			switch (cdi.Kind) {
			case PdbCustomDebugInfoKind.UsingGroups:
			case PdbCustomDebugInfoKind.ForwardMethodInfo:
			case PdbCustomDebugInfoKind.ForwardModuleInfo:
			case PdbCustomDebugInfoKind.StateMachineTypeName:
			case PdbCustomDebugInfoKind.DynamicLocals:
			case PdbCustomDebugInfoKind.TupleElementNames:
			case PdbCustomDebugInfoKind.SourceServer:
				// These are Windows PDB CDIs
				Error("Unsupported custom debug info {0}", cdi.Kind);
				break;

			case PdbCustomDebugInfoKind.StateMachineHoistedLocalScopes:
			case PdbCustomDebugInfoKind.EditAndContinueLocalSlotMap:
			case PdbCustomDebugInfoKind.EditAndContinueLambdaMap:
			case PdbCustomDebugInfoKind.Unknown:
			case PdbCustomDebugInfoKind.TupleElementNames_PortablePdb:
			case PdbCustomDebugInfoKind.DefaultNamespace:
			case PdbCustomDebugInfoKind.DynamicLocalVariables:
			case PdbCustomDebugInfoKind.EmbeddedSource:
			case PdbCustomDebugInfoKind.SourceLink:
				AddCustomDebugInformationCore(serializerMethodContext, encodedToken, cdi, cdi.Guid);
				break;

			case PdbCustomDebugInfoKind.AsyncMethod:
				// This is a portable PDB pseudo CDI
				AddCustomDebugInformationCore(serializerMethodContext, encodedToken, cdi, CustomDebugInfoGuids.AsyncMethodSteppingInformationBlob);
				AddStateMachineMethod(cdi, token, ((PdbAsyncMethodCustomDebugInfo)cdi).KickoffMethod);
				break;

			case PdbCustomDebugInfoKind.IteratorMethod:
				// This is a portable PDB pseudo CDI
				AddStateMachineMethod(cdi, token, ((PdbIteratorMethodCustomDebugInfo)cdi).KickoffMethod);
				break;

			default:
				Error("Unknown custom debug info {0}", cdi.Kind.ToString());
				break;
			}
		}

		void AddStateMachineMethod(PdbCustomDebugInfo cdi, uint moveNextMethodToken, MethodDef kickoffMethod) {
			Debug.Assert(new MDToken(moveNextMethodToken).Table == Table.Method);
			Debug.Assert(!(debugMetadata is null));
			if (kickoffMethod is null) {
				Error("KickoffMethod is null");
				return;
			}
			var row = new RawStateMachineMethodRow(new MDToken(moveNextMethodToken).Rid, GetRid(kickoffMethod));
			debugMetadata.stateMachineMethodInfos.Add(cdi, row);
		}

		void AddCustomDebugInformationCore(SerializerMethodContext serializerMethodContext, uint encodedToken, PdbCustomDebugInfo cdi, Guid cdiGuid) {
			Debug.Assert(!(debugMetadata is null));

			var bwctx = AllocBinaryWriterContext();
			var cdiBlob = PortablePdbCustomDebugInfoWriter.Write(this, serializerMethodContext, this, cdi, bwctx);
			Debug.Assert(cdiGuid != Guid.Empty);
			Free(ref bwctx);
			var row = new RawCustomDebugInformationRow(encodedToken,
						debugMetadata.guidHeap.Add(cdiGuid),
						debugMetadata.blobHeap.Add(cdiBlob));
			debugMetadata.customDebugInfos.Add(cdi, row);
		}

		void InitializeMethodDebugInformation() {
			if (debugMetadata is null)
				return;
			int numMethods = NumberOfMethods;
			for (int i = 0; i < numMethods; i++)
				debugMetadata.tablesHeap.MethodDebugInformationTable.Create(new RawMethodDebugInformationRow());
		}

		void AddPdbDocuments() {
			if (debugMetadata is null)
				return;
			foreach (var doc in module.PdbState.Documents)
				AddPdbDocument(doc);
		}

		uint AddPdbDocument(PdbDocument doc) {
			Debug.Assert(!(debugMetadata is null));
			if (doc is null) {
				Error("PdbDocument is null");
				return 0;
			}
			if (debugMetadata.pdbDocumentInfos.TryGetRid(doc, out uint rid))
				return rid;
			var row = new RawDocumentRow(GetDocumentNameBlobOffset(doc.Url),
							debugMetadata.guidHeap.Add(doc.CheckSumAlgorithmId),
							debugMetadata.blobHeap.Add(doc.CheckSum),
							debugMetadata.guidHeap.Add(doc.Language));
			rid = debugMetadata.tablesHeap.DocumentTable.Add(row);
			debugMetadata.pdbDocumentInfos.Add(doc, rid);
			AddCustomDebugInformationList(Table.Document, rid, doc.CustomDebugInfos);
			return rid;
		}

		uint GetDocumentNameBlobOffset(string name) {
			Debug.Assert(!(debugMetadata is null));
			if (name is null) {
				Error("Document name is null");
				name = string.Empty;
			}

			var bwctx = AllocBinaryWriterContext();
			var outStream = bwctx.OutStream;
			var writer = bwctx.Writer;
			outStream.SetLength(0);
			outStream.Position = 0;
			var parts = name.Split(directorySeparatorCharArray);
			if (parts.Length == 1)
				writer.WriteByte(0);
			else
				writer.WriteBytes(directorySeparatorCharUtf8);
			for (int i = 0; i < parts.Length; i++) {
				var part = parts[i];
				uint partOffset = debugMetadata.blobHeap.Add(Encoding.UTF8.GetBytes(part));
				writer.WriteCompressedUInt32(partOffset);
			}

			var res = debugMetadata.blobHeap.Add(outStream.ToArray());
			Free(ref bwctx);
			return res;
		}
		static readonly byte[] directorySeparatorCharUtf8 = Encoding.UTF8.GetBytes(Path.DirectorySeparatorChar.ToString());
		static readonly char[] directorySeparatorCharArray = new char[] { Path.DirectorySeparatorChar };

		uint AddImportScope(PdbImportScope scope) {
			Debug.Assert(!(debugMetadata is null));
			if (scope is null)
				return 0;
			if (debugMetadata.importScopeInfos.TryGetRid(scope, out uint rid)) {
				if (rid == 0)
					Error("PdbImportScope has an infinite Parent loop");
				return rid;
			}
			debugMetadata.importScopeInfos.Add(scope, 0);   // Prevent inf recursion

			var bwctx = AllocBinaryWriterContext();
			var outStream = bwctx.OutStream;
			var writer = bwctx.Writer;
			outStream.SetLength(0);
			outStream.Position = 0;
			ImportScopeBlobWriter.Write(this, this, writer, debugMetadata.blobHeap, scope.Imports);
			var importsData = outStream.ToArray();
			Free(ref bwctx);

			var row = new RawImportScopeRow(AddImportScope(scope.Parent), debugMetadata.blobHeap.Add(importsData));
			rid = debugMetadata.tablesHeap.ImportScopeTable.Add(row);
			debugMetadata.importScopeInfos.SetRid(scope, rid);

			AddCustomDebugInformationList(Table.ImportScope, rid, scope.CustomDebugInfos);
			return rid;
		}

		void AddLocalVariable(PdbLocal local) {
			Debug.Assert(!(debugMetadata is null));
			if (local is null) {
				Error("PDB local is null");
				return;
			}
			var row = new RawLocalVariableRow((ushort)local.Attributes, (ushort)local.Index, debugMetadata.stringsHeap.Add(local.Name));
			uint rid = debugMetadata.tablesHeap.LocalVariableTable.Create(row);
			debugMetadata.localVariableInfos.Add(local, rid);
			AddCustomDebugInformationList(Table.LocalVariable, rid, local.CustomDebugInfos);
		}

		void AddLocalConstant(PdbConstant constant) {
			Debug.Assert(!(debugMetadata is null));
			if (constant is null) {
				Error("PDB constant is null");
				return;
			}

			var bwctx = AllocBinaryWriterContext();
			var outStream = bwctx.OutStream;
			var writer = bwctx.Writer;
			outStream.SetLength(0);
			outStream.Position = 0;
			LocalConstantSigBlobWriter.Write(this, this, writer, constant.Type, constant.Value);
			var signature = outStream.ToArray();
			Free(ref bwctx);

			var row = new RawLocalConstantRow(debugMetadata.stringsHeap.Add(constant.Name), debugMetadata.blobHeap.Add(signature));
			uint rid = debugMetadata.tablesHeap.LocalConstantTable.Create(row);
			debugMetadata.localConstantInfos.Add(constant, rid);
			AddCustomDebugInformationList(Table.LocalConstant, rid, constant.CustomDebugInfos);
		}

		/// <summary>
		/// Writes the portable PDB to <paramref name="output"/>.
		/// </summary>
		/// <param name="output">Output stream</param>
		/// <param name="entryPointToken">Entry point token</param>
		/// <param name="pdbIdOffset">Updated with the offset of the 20-byte PDB ID. The caller is responsible for initializing it with the PDB ID</param>
		internal void WritePortablePdb(Stream output, uint entryPointToken, out long pdbIdOffset) {
			if (debugMetadata is null)
				throw new InvalidOperationException();
			var pdbHeap = debugMetadata.PdbHeap;
			pdbHeap.EntryPoint = entryPointToken;

			tablesHeap.GetSystemTableRows(out ulong systemTablesMask, pdbHeap.TypeSystemTableRows);
			debugMetadata.tablesHeap.SetSystemTableRows(pdbHeap.TypeSystemTableRows);
			if (!debugMetadata.methodDebugInformationInfosUsed)
				debugMetadata.tablesHeap.MethodDebugInformationTable.Reset();
			pdbHeap.ReferencedTypeSystemTables = systemTablesMask;
			var writer = new DataWriter(output);
			debugMetadata.OnBeforeSetOffset();
			debugMetadata.SetOffset(0, 0);
			debugMetadata.GetFileLength();
			debugMetadata.VerifyWriteTo(writer);
			pdbIdOffset = (long)pdbHeap.PdbIdOffset;
		}

		/// <inheritdoc/>
		uint ISignatureWriterHelper.ToEncodedToken(ITypeDefOrRef typeDefOrRef) => AddTypeDefOrRef(typeDefOrRef);

		/// <inheritdoc/>
		void IWriterError.Error(string message) => Error(message);

		/// <inheritdoc/>
		bool IFullNameFactoryHelper.MustUseAssemblyName(IType type) =>
			FullNameFactory.MustUseAssemblyName(module, type, OptimizeCustomAttributeSerializedTypeNames);

		/// <summary>
		/// Called before any other methods
		/// </summary>
		protected virtual void Initialize() {
		}

		/// <summary>
		/// Gets all <see cref="TypeDef"/>s that should be saved in the meta data
		/// </summary>
		protected abstract TypeDef[] GetAllTypeDefs();

		/// <summary>
		/// Initializes <c>TypeDef</c> rids and creates raw rows, but does not initialize
		/// any columns.
		/// </summary>
		protected abstract void AllocateTypeDefRids();

		/// <summary>
		/// Allocates <c>Field</c>, <c>Method</c>, <c>Property</c>, <c>Event</c>, <c>Param</c>:
		/// rid and raw row, but doesn't initialize the raw row.
		/// Initializes <c>TypeDef</c> columns: <c>FieldList</c>, <c>MethodList</c>.
		/// Initializes <c>Method</c> column: <c>ParamList</c>.
		/// Initializes <see cref="Metadata.eventMapInfos"/> and <see cref="Metadata.propertyMapInfos"/>.
		/// </summary>
		protected abstract void AllocateMemberDefRids();

		/// <summary>
		/// Adds a <see cref="TypeRef"/>. Its custom attributes are also added.
		/// </summary>
		/// <param name="tr">Type reference</param>
		/// <returns>Its new rid</returns>
		protected abstract uint AddTypeRef(TypeRef tr);

		/// <summary>
		/// Adds a <see cref="TypeSpec"/>. Its custom attributes are also added.
		/// </summary>
		/// <param name="ts">Type spec</param>
		/// <returns>Its new rid</returns>
		protected abstract uint AddTypeSpec(TypeSpec ts);

		/// <summary>
		/// Adds a <see cref="MemberRef"/>. Its custom attributes are also added.
		/// </summary>
		/// <param name="mr">Member ref</param>
		/// <returns>Its new rid</returns>
		protected abstract uint AddMemberRef(MemberRef mr);

		/// <summary>
		/// Adds a <see cref="StandAloneSig"/>. Its custom attributes are also added.
		/// </summary>
		/// <param name="sas">Stand alone sig</param>
		/// <returns>Its new rid</returns>
		protected abstract uint AddStandAloneSig(StandAloneSig sas);

		/// <summary>
		/// Adds a <see cref="MethodSpec"/>. Its custom attributes are also added.
		/// </summary>
		/// <param name="ms">Method spec</param>
		/// <returns>Its new rid</returns>
		protected abstract uint AddMethodSpec(MethodSpec ms);

		/// <summary>
		/// Called before sorting the <c>CustomAttribute</c> table. This is the last time anything
		/// can be inserted into this table.
		/// </summary>
		protected virtual void BeforeSortingCustomAttributes() {
		}

		/// <summary>
		/// Called after everything has been initialized. The sub class can initialize more
		/// rows if necessary or do nothing. After this method has been called, nothing else
		/// can be added.
		/// </summary>
		protected virtual void EverythingInitialized() {
		}

		const uint HEAP_ALIGNMENT = 4;

		bool IReuseChunk.CanReuse(RVA origRva, uint origSize) {
			// The caller should've called SetOffset() so we know our final size
			Debug.Assert(length != 0);
			if (length == 0)
				throw new InvalidOperationException();
			return length <= origSize;
		}

		/// <summary>
		/// Should be called before all chunks get an RVA
		/// </summary>
		internal void OnBeforeSetOffset() =>
			stringsHeap.AddOptimizedStringsAndSetReadOnly();

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			// This method can be called twice by NativeModuleWriter. It needs to know the size
			// of the final metadata. If it fits in the old location, the new MD will be written
			// there (smaller file size). If the new MD doesn't fit in the old location, this
			// method gets called a second time with the updated offset + rva.
			bool initAll = this.offset == 0;
			this.offset = offset;
			this.rva = rva;

			if (initAll) {
				// #Strings heap is initialized in OnBeforeSetOffset()
				blobHeap.SetReadOnly();
				guidHeap.SetReadOnly();
				tablesHeap.SetReadOnly();
				pdbHeap.SetReadOnly();
				tablesHeap.BigStrings = stringsHeap.IsBig;
				tablesHeap.BigBlob = blobHeap.IsBig;
				tablesHeap.BigGuid = guidHeap.IsBig;
				metadataHeader.Heaps = GetHeaps();
			}

			metadataHeader.SetOffset(offset, rva);
			uint len = metadataHeader.GetFileLength();
			offset += len;
			rva += len;

			foreach (var heap in metadataHeader.Heaps) {
				offset = offset.AlignUp(HEAP_ALIGNMENT);
				rva = rva.AlignUp(HEAP_ALIGNMENT);
				heap.SetOffset(offset, rva);
				len = heap.GetFileLength();
				offset += len;
				rva += len;
			}
			Debug.Assert(initAll || length == rva - this.rva);
			if (!(initAll || length == rva - this.rva))
				throw new InvalidOperationException();
			length = rva - this.rva;

			if (!isStandaloneDebugMetadata && initAll)
				UpdateMethodAndFieldRvas();
		}

		internal void UpdateMethodAndFieldRvas() {
			UpdateMethodRvas();
			UpdateFieldRvas();
		}

		IList<IHeap> GetHeaps() {
			var heaps = new List<IHeap>();

			if (isStandaloneDebugMetadata) {
				heaps.Add(pdbHeap);
				heaps.Add(tablesHeap);
				if (!stringsHeap.IsEmpty)
					heaps.Add(stringsHeap);
				if (!usHeap.IsEmpty)
					heaps.Add(usHeap);
				if (!guidHeap.IsEmpty)
					heaps.Add(guidHeap);
				if (!blobHeap.IsEmpty)
					heaps.Add(blobHeap);
			}
			else {
				heaps.Add(tablesHeap);
				if (!stringsHeap.IsEmpty || AlwaysCreateStringsHeap)
					heaps.Add(stringsHeap);
				if (!usHeap.IsEmpty || AlwaysCreateUSHeap)
					heaps.Add(usHeap);
				if (!guidHeap.IsEmpty || AlwaysCreateGuidHeap)
					heaps.Add(guidHeap);
				if (!blobHeap.IsEmpty || AlwaysCreateBlobHeap)
					heaps.Add(blobHeap);

				heaps.AddRange(options.CustomHeaps);
				options.RaiseMetadataHeapsAdded(new MetadataHeapsAddedEventArgs(this, heaps));
			}

			return heaps;
		}

		/// <inheritdoc/>
		public uint GetFileLength() => length;

		/// <inheritdoc/>
		public uint GetVirtualSize() => GetFileLength();

		/// <inheritdoc/>
		public void WriteTo(DataWriter writer) {
			var rva2 = rva;
			metadataHeader.VerifyWriteTo(writer);
			rva2 += metadataHeader.GetFileLength();

			foreach (var heap in metadataHeader.Heaps) {
				writer.WriteZeroes((int)(rva2.AlignUp(HEAP_ALIGNMENT) - rva2));
				rva2 = rva2.AlignUp(HEAP_ALIGNMENT);
				heap.VerifyWriteTo(writer);
				rva2 += heap.GetFileLength();
			}
		}

		/// <summary>
		/// Sorts the <see cref="ParamDef"/>s
		/// </summary>
		/// <param name="pds">All <see cref="ParamDef"/>s</param>
		/// <returns>A sorted <see cref="ParamDef"/> list</returns>
		protected static List<ParamDef> Sort(IEnumerable<ParamDef> pds) {
			var sorted = new List<ParamDef>(pds);
			sorted.Sort((a, b) => {
				if (a is null)
					return -1;
				if (b is null)
					return 1;
				return a.Sequence.CompareTo(b.Sequence);
			});
			return sorted;
		}

		DataWriterContext AllocBinaryWriterContext() {
			if (binaryWriterContexts.Count == 0)
				return new DataWriterContext();
			var res = binaryWriterContexts[binaryWriterContexts.Count - 1];
			binaryWriterContexts.RemoveAt(binaryWriterContexts.Count - 1);
			return res;
		}

		void Free(ref DataWriterContext ctx) {
			binaryWriterContexts.Add(ctx);
			ctx = null;
		}

		SerializerMethodContext AllocSerializerMethodContext() {
			if (serializerMethodContexts.Count == 0)
				return new SerializerMethodContext(this);
			var res = serializerMethodContexts[serializerMethodContexts.Count - 1];
			serializerMethodContexts.RemoveAt(serializerMethodContexts.Count - 1);
			return res;
		}

		void Free(ref SerializerMethodContext ctx) {
			serializerMethodContexts.Add(ctx);
			ctx = null;
		}
	}
}
