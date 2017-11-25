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

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// <see cref="MetaData"/> flags
	/// </summary>
	[Flags]
	public enum MetaDataFlags : uint {
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
	}

	/// <summary>
	/// <see cref="MetaData"/> options
	/// </summary>
	public sealed class MetaDataOptions {
		MetaDataHeaderOptions metaDataHeaderOptions;
		MetaDataHeaderOptions debugMetaDataHeaderOptions;
		TablesHeapOptions tablesHeapOptions;
		List<IHeap> otherHeaps;
		List<IHeap> otherHeapsEnd;

		/// <summary>
		/// Gets/sets the <see cref="MetaDataHeader"/> options. This is never <c>null</c>.
		/// </summary>
		public MetaDataHeaderOptions MetaDataHeaderOptions {
			get { return metaDataHeaderOptions ?? (metaDataHeaderOptions = new MetaDataHeaderOptions()); }
			set { metaDataHeaderOptions = value; }
		}

		/// <summary>
		/// Gets/sets the debug (portable PDB) <see cref="MetaDataHeader"/> options. This is never <c>null</c>.
		/// </summary>
		public MetaDataHeaderOptions DebugMetaDataHeaderOptions {
			get { return debugMetaDataHeaderOptions ?? (debugMetaDataHeaderOptions = MetaDataHeaderOptions.CreatePortablePdbV1_0()); }
			set { debugMetaDataHeaderOptions = value; }
		}

		/// <summary>
		/// Gets/sets the <see cref="TablesHeap"/> options. This is never <c>null</c>.
		/// </summary>
		public TablesHeapOptions TablesHeapOptions {
			get { return tablesHeapOptions ?? (tablesHeapOptions = new TablesHeapOptions()); }
			set { tablesHeapOptions = value; }
		}

		/// <summary>
		/// Gets/sets the debug (portable PDB) <see cref="TablesHeap"/> options. This is never <c>null</c>.
		/// </summary>
		public TablesHeapOptions DebugTablesHeapOptions {
			get { return tablesHeapOptions ?? (tablesHeapOptions = TablesHeapOptions.CreatePortablePdbV1_0()); }
			set { tablesHeapOptions = value; }
		}

		/// <summary>
		/// Various options
		/// </summary>
		public MetaDataFlags Flags;

		/// <summary>
		/// Any additional heaps that should be added to the beginning of the heaps list
		/// </summary>
		public List<IHeap> OtherHeaps {
			get { return otherHeaps ?? (otherHeaps = new List<IHeap>()); }
		}

		/// <summary>
		/// Any additional heaps that should be added to end of the heaps list
		/// </summary>
		public List<IHeap> OtherHeapsEnd {
			get { return otherHeapsEnd ?? (otherHeapsEnd = new List<IHeap>()); }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public MetaDataOptions() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="flags">Flags</param>
		public MetaDataOptions(MetaDataFlags flags) {
			this.Flags = flags;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="mdhOptions">Meta data header options</param>
		public MetaDataOptions(MetaDataHeaderOptions mdhOptions) {
			this.metaDataHeaderOptions = mdhOptions;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="mdhOptions">Meta data header options</param>
		/// <param name="flags">Flags</param>
		public MetaDataOptions(MetaDataHeaderOptions mdhOptions, MetaDataFlags flags) {
			this.Flags = flags;
			this.metaDataHeaderOptions = mdhOptions;
		}
	}

	sealed class BinaryWriterContext {
		public readonly MemoryStream OutStream;
		public readonly BinaryWriter Writer;
		public BinaryWriterContext() {
			OutStream = new MemoryStream();
			Writer = new BinaryWriter(OutStream);
		}
	}

	/// <summary>
	/// Portable PDB metadata kind
	/// </summary>
	public enum DebugMetaDataKind {
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
	/// .NET meta data
	/// </summary>
	public abstract class MetaData : IChunk, ISignatureWriterHelper, ITokenCreator, ICustomAttributeWriterHelper, IPortablePdbCustomDebugInfoWriterHelper {
		uint length;
		FileOffset offset;
		RVA rva;
		readonly MetaDataOptions options;
		IMetaDataListener listener;
		ILogger logger;
		readonly NormalMetaData debugMetaData;
		readonly bool isStandaloneDebugMetadata;
		internal readonly ModuleDef module;
		internal readonly UniqueChunkList<ByteArrayChunk> constants;
		internal readonly MethodBodyChunks methodBodies;
		internal readonly NetResources netResources;
		internal readonly MetaDataHeader metaDataHeader;
		internal HotHeap hotHeap;
		internal readonly PdbHeap pdbHeap;
		internal readonly TablesHeap tablesHeap;
		internal readonly StringsHeap stringsHeap;
		internal readonly USHeap usHeap;
		internal readonly GuidHeap guidHeap;
		internal readonly BlobHeap blobHeap;
		internal List<TypeDef> allTypeDefs;
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
		internal readonly Dictionary<EmbeddedResource, ByteArrayChunk> embeddedResourceToByteArray = new Dictionary<EmbeddedResource, ByteArrayChunk>();
		readonly Dictionary<FieldDef, ByteArrayChunk> fieldToInitialValue = new Dictionary<FieldDef, ByteArrayChunk>();
		readonly Rows<PdbDocument> pdbDocumentInfos = new Rows<PdbDocument>();
		bool methodDebugInformationInfosUsed;
		readonly SortedRows<PdbScope, RawLocalScopeRow> localScopeInfos = new SortedRows<PdbScope, RawLocalScopeRow>();
		readonly Rows<PdbLocal> localVariableInfos = new Rows<PdbLocal>();
		readonly Rows<PdbConstant> localConstantInfos = new Rows<PdbConstant>();
		readonly Rows<PdbImportScope> importScopeInfos = new Rows<PdbImportScope>();
		readonly SortedRows<PdbCustomDebugInfo, RawStateMachineMethodRow> stateMachineMethodInfos = new SortedRows<PdbCustomDebugInfo, RawStateMachineMethodRow>();
		readonly SortedRows<PdbCustomDebugInfo, RawCustomDebugInformationRow> customDebugInfos = new SortedRows<PdbCustomDebugInfo, RawCustomDebugInformationRow>();
		readonly List<BinaryWriterContext> binaryWriterContexts = new List<BinaryWriterContext>();
		readonly List<SerializerMethodContext> serializerMethodContexts = new List<SerializerMethodContext>();

		/// <summary>
		/// Gets/sets the listener
		/// </summary>
		public IMetaDataListener Listener {
			get { return listener ?? (listener = DummyMetaDataListener.Instance); }
			set { listener = value; }
		}

		/// <summary>
		/// Gets/sets the logger
		/// </summary>
		public ILogger Logger {
			get { return logger; }
			set { logger = value; }
		}

		/// <summary>
		/// Gets the module
		/// </summary>
		public ModuleDef Module {
			get { return module; }
		}

		/// <summary>
		/// Gets the constants
		/// </summary>
		public UniqueChunkList<ByteArrayChunk> Constants {
			get { return constants; }
		}

		/// <summary>
		/// Gets the method body chunks
		/// </summary>
		public MethodBodyChunks MethodBodyChunks {
			get { return methodBodies; }
		}

		/// <summary>
		/// Gets the .NET resources
		/// </summary>
		public NetResources NetResources {
			get { return netResources; }
		}

		/// <summary>
		/// Gets the MD header
		/// </summary>
		public MetaDataHeader MetaDataHeader {
			get { return metaDataHeader; }
		}

		/// <summary>
		/// Gets/sets the hot heap (<c>#!</c>)
		/// </summary>
		public HotHeap HotHeap {
			get { return hotHeap; }
			set { hotHeap = value; }
		}

		/// <summary>
		/// Gets the tables heap. Access to this heap is not recommended, but is useful if you
		/// want to add random table entries.
		/// </summary>
		public TablesHeap TablesHeap {
			get { return tablesHeap; }
		}

		/// <summary>
		/// Gets the #Strings heap. Access to this heap is not recommended, but is useful if you
		/// want to add random strings.
		/// </summary>
		public StringsHeap StringsHeap {
			get { return stringsHeap; }
		}

		/// <summary>
		/// Gets the #US heap. Access to this heap is not recommended, but is useful if
		/// you want to add random user strings.
		/// </summary>
		public USHeap USHeap {
			get { return usHeap; }
		}

		/// <summary>
		/// Gets the #GUID heap. Access to this heap is not recommended, but is useful if you
		/// want to add random GUIDs.
		/// </summary>
		public GuidHeap GuidHeap {
			get { return guidHeap; }
		}

		/// <summary>
		/// Gets the #Blob heap. Access to this heap is not recommended, but is useful if you
		/// want to add random blobs.
		/// </summary>
		public BlobHeap BlobHeap {
			get { return blobHeap; }
		}

		/// <summary>
		/// Gets the #Pdb heap. It's only used if it's portable PDB metadata
		/// </summary>
		public PdbHeap PdbHeap {
			get { return pdbHeap; }
		}

		/// <summary>
		/// The public key that should be used instead of the one in <see cref="AssemblyDef"/>.
		/// </summary>
		internal byte[] AssemblyPublicKey { get; set; }

		internal sealed class SortedRows<T, TRow> where T : class where TRow : class {
			public List<Info> infos = new List<Info>();
			Dictionary<T, uint> toRid = new Dictionary<T, uint>();
			bool isSorted;

			public struct Info {
				public T data;
				public TRow row;
				public Info(T data, TRow row) {
					this.data = data;
					this.row = row;
				}
			}

			public void Add(T data, TRow row) {
				if (isSorted)
					throw new ModuleWriterException(string.Format("Adding a row after it's been sorted. Table: {0}", row.GetType()));
				infos.Add(new Info(data, row));
				toRid[data] = (uint)toRid.Count + 1;
			}

			public void Sort(Comparison<SortedRows<T, TRow>.Info> comparison) {
				infos.Sort(comparison);
				toRid.Clear();
				for (int i = 0; i < infos.Count; i++)
					toRid[infos[i].data] = (uint)i + 1;
				isSorted = true;
			}

			public uint Rid(T data) {
				return toRid[data];
			}

			public bool TryGetRid(T data, out uint rid) {
				if (data == null) {
					rid = 0;
					return false;
				}
				return toRid.TryGetValue(data, out rid);
			}
		}

		internal sealed class Rows<T> where T : class {
			Dictionary<T, uint> dict = new Dictionary<T, uint>();

			public int Count {
				get { return dict.Count; }
			}

			public bool TryGetRid(T value, out uint rid) {
				if (value == null) {
					rid = 0;
					return false;
				}
				return dict.TryGetValue(value, out rid);
			}

			public bool Exists(T value) {
				return dict.ContainsKey(value);
			}

			public void Add(T value, uint rid) {
				dict.Add(value, rid);
			}

			public uint Rid(T value) {
				return dict[value];
			}

			public void SetRid(T value, uint rid) {
				dict[value] = rid;
			}
		}

		/// <summary>
		/// Creates a <see cref="MetaData"/> instance
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="constants">Constants list</param>
		/// <param name="methodBodies">Method bodies list</param>
		/// <param name="netResources">.NET resources list</param>
		/// <param name="options">Options</param>
		/// <param name="debugKind">Debug metadata kind</param>
		/// <returns>A new <see cref="MetaData"/> instance</returns>
		public static MetaData Create(ModuleDef module, UniqueChunkList<ByteArrayChunk> constants, MethodBodyChunks methodBodies, NetResources netResources, MetaDataOptions options = null, DebugMetaDataKind debugKind = DebugMetaDataKind.None) {
			if (options == null)
				options = new MetaDataOptions();
			if ((options.Flags & MetaDataFlags.PreserveRids) != 0 && module is ModuleDefMD)
				return new PreserveTokensMetaData(module, constants, methodBodies, netResources, options, debugKind, false);
			return new NormalMetaData(module, constants, methodBodies, netResources, options, debugKind, false);
		}

		/// <inheritdoc/>
		public FileOffset FileOffset {
			get { return offset; }
		}

		/// <inheritdoc/>
		public RVA RVA {
			get { return rva; }
		}

		/// <summary>
		/// Gets the <see cref="MetaDataFlags.PreserveTypeRefRids"/> bit
		/// </summary>
		public bool PreserveTypeRefRids {
			get { return (options.Flags & MetaDataFlags.PreserveTypeRefRids) != 0; }
		}

		/// <summary>
		/// Gets the <see cref="MetaDataFlags.PreserveTypeDefRids"/> bit
		/// </summary>
		public bool PreserveTypeDefRids {
			get { return (options.Flags & MetaDataFlags.PreserveTypeDefRids) != 0; }
		}

		/// <summary>
		/// Gets the <see cref="MetaDataFlags.PreserveFieldRids"/> bit
		/// </summary>
		public bool PreserveFieldRids {
			get { return (options.Flags & MetaDataFlags.PreserveFieldRids) != 0; }
		}

		/// <summary>
		/// Gets the <see cref="MetaDataFlags.PreserveMethodRids"/> bit
		/// </summary>
		public bool PreserveMethodRids {
			get { return (options.Flags & MetaDataFlags.PreserveMethodRids) != 0; }
		}

		/// <summary>
		/// Gets the <see cref="MetaDataFlags.PreserveParamRids"/> bit
		/// </summary>
		public bool PreserveParamRids {
			get { return (options.Flags & MetaDataFlags.PreserveParamRids) != 0; }
		}

		/// <summary>
		/// Gets the <see cref="MetaDataFlags.PreserveMemberRefRids"/> bit
		/// </summary>
		public bool PreserveMemberRefRids {
			get { return (options.Flags & MetaDataFlags.PreserveMemberRefRids) != 0; }
		}

		/// <summary>
		/// Gets the <see cref="MetaDataFlags.PreserveStandAloneSigRids"/> bit
		/// </summary>
		public bool PreserveStandAloneSigRids {
			get { return (options.Flags & MetaDataFlags.PreserveStandAloneSigRids) != 0; }
		}

		/// <summary>
		/// Gets the <see cref="MetaDataFlags.PreserveEventRids"/> bit
		/// </summary>
		public bool PreserveEventRids {
			get { return (options.Flags & MetaDataFlags.PreserveEventRids) != 0; }
		}

		/// <summary>
		/// Gets the <see cref="MetaDataFlags.PreservePropertyRids"/> bit
		/// </summary>
		public bool PreservePropertyRids {
			get { return (options.Flags & MetaDataFlags.PreservePropertyRids) != 0; }
		}

		/// <summary>
		/// Gets the <see cref="MetaDataFlags.PreserveTypeSpecRids"/> bit
		/// </summary>
		public bool PreserveTypeSpecRids {
			get { return (options.Flags & MetaDataFlags.PreserveTypeSpecRids) != 0; }
		}

		/// <summary>
		/// Gets the <see cref="MetaDataFlags.PreserveMethodSpecRids"/> bit
		/// </summary>
		public bool PreserveMethodSpecRids {
			get { return (options.Flags & MetaDataFlags.PreserveMethodSpecRids) != 0; }
		}

		/// <summary>
		/// Gets/sets the <see cref="MetaDataFlags.PreserveStringsOffsets"/> bit
		/// </summary>
		public bool PreserveStringsOffsets {
			get { return (options.Flags & MetaDataFlags.PreserveStringsOffsets) != 0; }
			set {
				if (value)
					options.Flags |= MetaDataFlags.PreserveStringsOffsets;
				else
					options.Flags &= ~MetaDataFlags.PreserveStringsOffsets;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MetaDataFlags.PreserveUSOffsets"/> bit
		/// </summary>
		public bool PreserveUSOffsets {
			get { return (options.Flags & MetaDataFlags.PreserveUSOffsets) != 0; }
			set {
				if (value)
					options.Flags |= MetaDataFlags.PreserveUSOffsets;
				else
					options.Flags &= ~MetaDataFlags.PreserveUSOffsets;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MetaDataFlags.PreserveBlobOffsets"/> bit
		/// </summary>
		public bool PreserveBlobOffsets {
			get { return (options.Flags & MetaDataFlags.PreserveBlobOffsets) != 0; }
			set {
				if (value)
					options.Flags |= MetaDataFlags.PreserveBlobOffsets;
				else
					options.Flags &= ~MetaDataFlags.PreserveBlobOffsets;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MetaDataFlags.PreserveExtraSignatureData"/> bit
		/// </summary>
		public bool PreserveExtraSignatureData {
			get { return (options.Flags & MetaDataFlags.PreserveExtraSignatureData) != 0; }
			set {
				if (value)
					options.Flags |= MetaDataFlags.PreserveExtraSignatureData;
				else
					options.Flags &= ~MetaDataFlags.PreserveExtraSignatureData;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MetaDataFlags.KeepOldMaxStack"/> bit
		/// </summary>
		public bool KeepOldMaxStack {
			get { return (options.Flags & MetaDataFlags.KeepOldMaxStack) != 0; }
			set {
				if (value)
					options.Flags |= MetaDataFlags.KeepOldMaxStack;
				else
					options.Flags &= ~MetaDataFlags.KeepOldMaxStack;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MetaDataFlags.AlwaysCreateGuidHeap"/> bit
		/// </summary>
		public bool AlwaysCreateGuidHeap {
			get { return (options.Flags & MetaDataFlags.AlwaysCreateGuidHeap) != 0; }
			set {
				if (value)
					options.Flags |= MetaDataFlags.AlwaysCreateGuidHeap;
				else
					options.Flags &= ~MetaDataFlags.AlwaysCreateGuidHeap;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MetaDataFlags.AlwaysCreateStringsHeap"/> bit
		/// </summary>
		public bool AlwaysCreateStringsHeap {
			get { return (options.Flags & MetaDataFlags.AlwaysCreateStringsHeap) != 0; }
			set {
				if (value)
					options.Flags |= MetaDataFlags.AlwaysCreateStringsHeap;
				else
					options.Flags &= ~MetaDataFlags.AlwaysCreateStringsHeap;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MetaDataFlags.AlwaysCreateUSHeap"/> bit
		/// </summary>
		public bool AlwaysCreateUSHeap {
			get { return (options.Flags & MetaDataFlags.AlwaysCreateUSHeap) != 0; }
			set {
				if (value)
					options.Flags |= MetaDataFlags.AlwaysCreateUSHeap;
				else
					options.Flags &= ~MetaDataFlags.AlwaysCreateUSHeap;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MetaDataFlags.AlwaysCreateBlobHeap"/> bit
		/// </summary>
		public bool AlwaysCreateBlobHeap {
			get { return (options.Flags & MetaDataFlags.AlwaysCreateBlobHeap) != 0; }
			set {
				if (value)
					options.Flags |= MetaDataFlags.AlwaysCreateBlobHeap;
				else
					options.Flags &= ~MetaDataFlags.AlwaysCreateBlobHeap;
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

		internal MetaData(ModuleDef module, UniqueChunkList<ByteArrayChunk> constants, MethodBodyChunks methodBodies, NetResources netResources, MetaDataOptions options, DebugMetaDataKind debugKind, bool isStandaloneDebugMetadata) {
			this.module = module;
			this.constants = constants;
			this.methodBodies = methodBodies;
			this.netResources = netResources;
			this.options = options ?? new MetaDataOptions();
			this.metaDataHeader = new MetaDataHeader(isStandaloneDebugMetadata ? this.options.DebugMetaDataHeaderOptions : this.options.MetaDataHeaderOptions);
			this.tablesHeap = new TablesHeap(isStandaloneDebugMetadata ? this.options.DebugTablesHeapOptions : this.options.TablesHeapOptions);
			this.stringsHeap = new StringsHeap();
			this.usHeap = new USHeap();
			this.guidHeap = new GuidHeap();
			this.blobHeap = new BlobHeap();
			this.pdbHeap = new PdbHeap();

			this.isStandaloneDebugMetadata = isStandaloneDebugMetadata;
			switch (debugKind) {
			case DebugMetaDataKind.None:
				break;

			case DebugMetaDataKind.Standalone:
				Debug.Assert(!isStandaloneDebugMetadata);
				//TODO: Refactor this into a smaller class
				debugMetaData = new NormalMetaData(module, constants, methodBodies, netResources, options, DebugMetaDataKind.None, true);
				break;

			default:
				throw new ArgumentOutOfRangeException("debugKind");
			}
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="module">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetRid(ModuleDef module) {
			uint rid;
			moduleDefInfos.TryGetRid(module, out rid);
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
			uint rid;
			interfaceImplInfos.TryGetRid(ii, out rid);
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
			uint rid;
			hasConstantInfos.TryGetRid(hc, out rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="ca">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetCustomAttributeRid(CustomAttribute ca) {
			uint rid;
			customAttributeInfos.TryGetRid(ca, out rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="hfm">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetFieldMarshalRid(IHasFieldMarshal hfm) {
			uint rid;
			fieldMarshalInfos.TryGetRid(hfm, out rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="ds">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetRid(DeclSecurity ds) {
			uint rid;
			declSecurityInfos.TryGetRid(ds, out rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="td">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetClassLayoutRid(TypeDef td) {
			uint rid;
			classLayoutInfos.TryGetRid(td, out rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="fd">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetFieldLayoutRid(FieldDef fd) {
			uint rid;
			fieldLayoutInfos.TryGetRid(fd, out rid);
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
			uint rid;
			eventMapInfos.TryGetRid(td, out rid);
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
			uint rid;
			propertyMapInfos.TryGetRid(td, out rid);
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
			uint rid;
			methodSemanticsInfos.TryGetRid(md, out rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="mr">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetRid(ModuleRef mr) {
			uint rid;
			moduleRefInfos.TryGetRid(mr, out rid);
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
			uint rid;
			implMapInfos.TryGetRid(mf, out rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="fd">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetFieldRVARid(FieldDef fd) {
			uint rid;
			fieldRVAInfos.TryGetRid(fd, out rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="asm">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetRid(AssemblyDef asm) {
			uint rid;
			assemblyInfos.TryGetRid(asm, out rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="asmRef">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetRid(AssemblyRef asmRef) {
			uint rid;
			assemblyRefInfos.TryGetRid(asmRef, out rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="fd">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetRid(FileDef fd) {
			uint rid;
			fileDefInfos.TryGetRid(fd, out rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="et">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetRid(ExportedType et) {
			uint rid;
			exportedTypeInfos.TryGetRid(et, out rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="resource">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetManifestResourceRid(Resource resource) {
			uint rid;
			manifestResourceInfos.TryGetRid(resource, out rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="td">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetNestedClassRid(TypeDef td) {
			uint rid;
			nestedClassInfos.TryGetRid(td, out rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="gp">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetRid(GenericParam gp) {
			uint rid;
			genericParamInfos.TryGetRid(gp, out rid);
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
			uint rid;
			genericParamConstraintInfos.TryGetRid(gpc, out rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="doc">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetRid(PdbDocument doc) {
			if (debugMetaData == null)
				return 0;
			uint rid;
			debugMetaData.pdbDocumentInfos.TryGetRid(doc, out rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="scope">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetRid(PdbScope scope) {
			if (debugMetaData == null)
				return 0;
			uint rid;
			debugMetaData.localScopeInfos.TryGetRid(scope, out rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="local">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetRid(PdbLocal local) {
			if (debugMetaData == null)
				return 0;
			uint rid;
			debugMetaData.localVariableInfos.TryGetRid(local, out rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="constant">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetRid(PdbConstant constant) {
			if (debugMetaData == null)
				return 0;
			uint rid;
			debugMetaData.localConstantInfos.TryGetRid(constant, out rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="importScope">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetRid(PdbImportScope importScope) {
			if (debugMetaData == null)
				return 0;
			uint rid;
			debugMetaData.importScopeInfos.TryGetRid(importScope, out rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="asyncMethod">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetStateMachineMethodRid(PdbAsyncMethodCustomDebugInfo asyncMethod) {
			if (debugMetaData == null)
				return 0;
			uint rid;
			debugMetaData.stateMachineMethodInfos.TryGetRid(asyncMethod, out rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="iteratorMethod">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetStateMachineMethodRid(PdbIteratorMethodCustomDebugInfo iteratorMethod) {
			if (debugMetaData == null)
				return 0;
			uint rid;
			debugMetaData.stateMachineMethodInfos.TryGetRid(iteratorMethod, out rid);
			return rid;
		}

		/// <summary>
		/// Gets the new rid
		/// </summary>
		/// <param name="cdi">Value</param>
		/// <returns>Its new rid or <c>0</c></returns>
		public uint GetCustomDebugInfoRid(PdbCustomDebugInfo cdi) {
			if (debugMetaData == null)
				return 0;
			uint rid;
			debugMetaData.customDebugInfos.TryGetRid(cdi, out rid);
			return rid;
		}

		/// <summary>
		/// Gets the <see cref="MethodBody"/>
		/// </summary>
		/// <param name="md">Method</param>
		/// <returns>The <see cref="MethodBody"/> or <c>null</c> if <paramref name="md"/> is
		/// <c>null</c> or not a method defined in this module.</returns>
		public MethodBody GetMethodBody(MethodDef md) {
			if (md == null)
				return null;
			MethodBody mb;
			methodToBody.TryGetValue(md, out mb);
			return mb;
		}

		/// <summary>
		/// Gets a method's local variable signature token
		/// </summary>
		/// <param name="md">Method</param>
		/// <returns>Locals sig token or <c>0</c></returns>
		public uint GetLocalVarSigToken(MethodDef md) {
			var mb = GetMethodBody(md);
			return mb == null ? 0 : mb.LocalVarSigTok;
		}

		/// <summary>
		/// Gets the <see cref="ByteArrayChunk"/> where the resource data will be stored
		/// </summary>
		/// <param name="er">Embedded resource</param>
		/// <returns>A <see cref="ByteArrayChunk"/> instance or <c>null</c> if <paramref name="er"/>
		/// is invalid</returns>
		public ByteArrayChunk GetChunk(EmbeddedResource er) {
			if (er == null)
				return null;
			ByteArrayChunk chunk;
			embeddedResourceToByteArray.TryGetValue(er, out chunk);
			return chunk;
		}

		/// <summary>
		/// Gets the <see cref="ByteArrayChunk"/> where the initial value is stored
		/// </summary>
		/// <param name="fd">Field</param>
		/// <returns>A <see cref="ByteArrayChunk"/> instance or <c>null</c> if <paramref name="fd"/>
		/// is invalid</returns>
		public ByteArrayChunk GetInitialValueChunk(FieldDef fd) {
			if (fd == null)
				return null;
			ByteArrayChunk chunk;
			fieldToInitialValue.TryGetValue(fd, out chunk);
			return chunk;
		}

		ILogger GetLogger() {
			return logger ?? DummyLogger.ThrowModuleWriterExceptionOnErrorInstance;
		}

		/// <summary>
		/// Called when an error is detected
		/// </summary>
		/// <param name="message">Error message</param>
		/// <param name="args">Optional message arguments</param>
		protected void Error(string message, params object[] args) {
			GetLogger().Log(this, LoggerEvent.Error, message, args);
		}

		/// <summary>
		/// Called to warn of something
		/// </summary>
		/// <param name="message">Warning message</param>
		/// <param name="args">Optional message arguments</param>
		protected void Warning(string message, params object[] args) {
			GetLogger().Log(this, LoggerEvent.Warning, message, args);
		}

		/// <summary>
		/// Creates the .NET metadata tables
		/// </summary>
		public void CreateTables() {
			Listener.OnMetaDataEvent(this, MetaDataEvent.BeginCreateTables);

			if (module.Types.Count == 0 || module.Types[0] == null)
				throw new ModuleWriterException("Missing global <Module> type");

			var moduleDefMD = module as ModuleDefMD;
			if (moduleDefMD != null) {
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
				var row = tablesHeap.MethodTable[GetRid(method)];
				row.RVA = (uint)body.RVA;
			}
			foreach (var kv in methodToNativeBody) {
				var method = kv.Key;
				var body = kv.Value;
				var row = tablesHeap.MethodTable[GetRid(method)];
				row.RVA = (uint)body.RVA;
			}
		}

		/// <summary>
		/// Updates the <c>FieldRVA</c> rows
		/// </summary>
		void UpdateFieldRvas() {
			foreach (var kv in fieldToInitialValue) {
				var field = kv.Key;
				var iv = kv.Value;
				var row = tablesHeap.FieldRVATable[fieldRVAInfos.Rid(field)];
				row.RVA = (uint)iv.RVA;
			}
		}

		void Create() {
			Debug.Assert(!isStandaloneDebugMetadata);
			Initialize();
			allTypeDefs = GetAllTypeDefs();
			Listener.OnMetaDataEvent(this, MetaDataEvent.AllocateTypeDefRids);
			AllocateTypeDefRids();
			Listener.OnMetaDataEvent(this, MetaDataEvent.AllocateMemberDefRids);
			AllocateMemberDefRids();
			Listener.OnMetaDataEvent(this, MetaDataEvent.MemberDefRidsAllocated);

			AddModule(module);
			AddPdbDocuments();
			InitializeMethodDebugInformation();
			InitializeTypeDefsAndMemberDefs();
			Listener.OnMetaDataEvent(this, MetaDataEvent.MemberDefsInitialized);

			InitializeVTableFixups();

			AddExportedTypes();
			InitializeEntryPoint();
			if (module.Assembly != null)
				AddAssembly(module.Assembly, AssemblyPublicKey);

			Listener.OnMetaDataEvent(this, MetaDataEvent.BeforeSortTables);
			SortTables();
			InitializeGenericParamConstraintTable();
			Listener.OnMetaDataEvent(this, MetaDataEvent.MostTablesSorted);

			WriteTypeDefAndMemberDefCustomAttributesAndCustomDebugInfos();
			Listener.OnMetaDataEvent(this, MetaDataEvent.MemberDefCustomAttributesWritten);

			Listener.OnMetaDataEvent(this, MetaDataEvent.BeginAddResources);
			AddResources(module.Resources);
			Listener.OnMetaDataEvent(this, MetaDataEvent.EndAddResources);

			Listener.OnMetaDataEvent(this, MetaDataEvent.BeginWriteMethodBodies);
			WriteMethodBodies();
			Listener.OnMetaDataEvent(this, MetaDataEvent.EndWriteMethodBodies);

			BeforeSortingCustomAttributes();
			InitializeCustomAttributeAndCustomDebugInfoTables();
			Listener.OnMetaDataEvent(this, MetaDataEvent.OnAllTablesSorted);

			EverythingInitialized();
			Listener.OnMetaDataEvent(this, MetaDataEvent.EndCreateTables);
		}

		/// <summary>
		/// Initializes all <c>TypeDef</c>, <c>Field</c>, <c>Method</c>, <c>Event</c>,
		/// <c>Property</c> and <c>Param</c> rows. Other tables that are related to these six
		/// tables are also updated. No custom attributes are written yet, though. Method bodies
		/// aren't written either.
		/// </summary>
		void InitializeTypeDefsAndMemberDefs() {
			int numTypes = allTypeDefs.Count;
			int typeNum = 0;
			int notifyNum = 0;
			const int numNotifyEvents = 5; // InitializeTypeDefsAndMemberDefs0 - InitializeTypeDefsAndMemberDefs4
			int notifyAfter = numTypes / numNotifyEvents;

			foreach (var type in allTypeDefs) {
				if (typeNum++ == notifyAfter && notifyNum < numNotifyEvents) {
					Listener.OnMetaDataEvent(this, MetaDataEvent.InitializeTypeDefsAndMemberDefs0 + notifyNum++);
					notifyAfter += numTypes / numNotifyEvents;
				}

				if (type == null) {
					Error("TypeDef is null");
					continue;
				}
				uint typeRid = GetRid(type);
				var typeRow = tablesHeap.TypeDefTable[typeRid];
				typeRow.Flags = (uint)type.Attributes;
				typeRow.Name = stringsHeap.Add(type.Name);
				typeRow.Namespace = stringsHeap.Add(type.Namespace);
				typeRow.Extends = type.BaseType == null ? 0 : AddTypeDefOrRef(type.BaseType);
				AddGenericParams(new MDToken(Table.TypeDef, typeRid), type.GenericParameters);
				AddDeclSecurities(new MDToken(Table.TypeDef, typeRid), type.DeclSecurities);
				AddInterfaceImpls(typeRid, type.Interfaces);
				AddClassLayout(type);
				AddNestedType(type, type.DeclaringType);

				foreach (var field in type.Fields) {
					if (field == null) {
						Error("Field is null. TypeDef {0} ({1:X8})", type, type.MDToken.Raw);
						continue;
					}
					uint rid = GetRid(field);
					var row = tablesHeap.FieldTable[rid];
					row.Flags = (ushort)field.Attributes;
					row.Name = stringsHeap.Add(field.Name);
					row.Signature = GetSignature(field.Signature);
					AddFieldLayout(field);
					AddFieldMarshal(new MDToken(Table.Field, rid), field);
					AddFieldRVA(field);
					AddImplMap(new MDToken(Table.Field, rid), field);
					AddConstant(new MDToken(Table.Field, rid), field);
				}

				foreach (var method in type.Methods) {
					if (method == null) {
						Error("Method is null. TypeDef {0} ({1:X8})", type, type.MDToken.Raw);
						continue;
					}
					uint rid = GetRid(method);
					var row = tablesHeap.MethodTable[rid];
					row.ImplFlags = (ushort)method.ImplAttributes;
					row.Flags = (ushort)method.Attributes;
					row.Name = stringsHeap.Add(method.Name);
					row.Signature = GetSignature(method.Signature);
					AddGenericParams(new MDToken(Table.Method, rid), method.GenericParameters);
					AddDeclSecurities(new MDToken(Table.Method, rid), method.DeclSecurities);
					AddImplMap(new MDToken(Table.Method, rid), method);
					AddMethodImpls(method, method.Overrides);
					foreach (var pd in method.ParamDefs) {
						if (pd == null) {
							Error("Param is null. Method {0} ({1:X8})", method, method.MDToken.Raw);
							continue;
						}
						uint pdRid = GetRid(pd);
						var pdRow = tablesHeap.ParamTable[pdRid];
						pdRow.Flags = (ushort)pd.Attributes;
						pdRow.Sequence = pd.Sequence;
						pdRow.Name = stringsHeap.Add(pd.Name);
						AddConstant(new MDToken(Table.Param, pdRid), pd);
						AddFieldMarshal(new MDToken(Table.Param, pdRid), pd);
					}
				}

				if (!IsEmpty(type.Events)) {
					foreach (var evt in type.Events) {
						if (evt == null) {
							Error("Event is null. TypeDef {0} ({1:X8})", type, type.MDToken.Raw);
							continue;
						}
						uint rid = GetRid(evt);
						var row = tablesHeap.EventTable[rid];
						row.EventFlags = (ushort)evt.Attributes;
						row.Name = stringsHeap.Add(evt.Name);
						row.EventType = AddTypeDefOrRef(evt.EventType);
						AddMethodSemantics(evt);
					}
				}

				if (!IsEmpty(type.Properties)) {
					foreach (var prop in type.Properties) {
						if (prop == null) {
							Error("Property is null. TypeDef {0} ({1:X8})", type, type.MDToken.Raw);
							continue;
						}
						uint rid = GetRid(prop);
						var row = tablesHeap.PropertyTable[rid];
						row.PropFlags = (ushort)prop.Attributes;
						row.Name = stringsHeap.Add(prop.Name);
						row.Type = GetSignature(prop.Type);
						AddConstant(new MDToken(Table.Property, rid), prop);
						AddMethodSemantics(prop);
					}
				}
			}
			while (notifyNum < numNotifyEvents)
				Listener.OnMetaDataEvent(this, MetaDataEvent.InitializeTypeDefsAndMemberDefs0 + notifyNum++);
		}

		/// <summary>
		/// Writes <c>TypeDef</c>, <c>Field</c>, <c>Method</c>, <c>Event</c>,
		/// <c>Property</c> and <c>Param</c> custom attributes and custom debug infos.
		/// </summary>
		void WriteTypeDefAndMemberDefCustomAttributesAndCustomDebugInfos() {
			int numTypes = allTypeDefs.Count;
			int typeNum = 0;
			int notifyNum = 0;
			const int numNotifyEvents = 5; // WriteTypeDefAndMemberDefCustomAttributes0 - WriteTypeDefAndMemberDefCustomAttributes4
			int notifyAfter = numTypes / numNotifyEvents;

			uint rid;
			foreach (var type in allTypeDefs) {
				if (typeNum++ == notifyAfter && notifyNum < numNotifyEvents) {
					Listener.OnMetaDataEvent(this, MetaDataEvent.WriteTypeDefAndMemberDefCustomAttributes0 + notifyNum++);
					notifyAfter += numTypes / numNotifyEvents;
				}

				if (type == null)
					continue;
				rid = GetRid(type);
				AddCustomAttributes(Table.TypeDef, rid, type);
				AddCustomDebugInformationList(Table.TypeDef, rid, type);

				foreach (var field in type.Fields) {
					if (field == null)
						continue;
					rid = GetRid(field);
					AddCustomAttributes(Table.Field, rid, field);
					AddCustomDebugInformationList(Table.Field, rid, field);
				}

				foreach (var method in type.Methods) {
					if (method == null)
						continue;
					AddCustomAttributes(Table.Method, GetRid(method), method);
					// Method custom debug info is added later when writing method bodies
					foreach (var pd in method.ParamDefs) {
						if (pd == null)
							continue;
						rid = GetRid(pd);
						AddCustomAttributes(Table.Param, rid, pd);
						AddCustomDebugInformationList(Table.Param, rid, pd);
					}
				}
				foreach (var evt in type.Events) {
					if (evt == null)
						continue;
					rid = GetRid(evt);
					AddCustomAttributes(Table.Event, rid, evt);
					AddCustomDebugInformationList(Table.Event, rid, evt);
				}
				foreach (var prop in type.Properties) {
					if (prop == null)
						continue;
					rid = GetRid(prop);
					AddCustomAttributes(Table.Property, rid, prop);
					AddCustomDebugInformationList(Table.Property, rid, prop);
				}
			}
			while (notifyNum < numNotifyEvents)
				Listener.OnMetaDataEvent(this, MetaDataEvent.WriteTypeDefAndMemberDefCustomAttributes0 + notifyNum++);
		}

		/// <summary>
		/// Adds the tokens of all methods in all vtables, if any
		/// </summary>
		void InitializeVTableFixups() {
			var fixups = module.VTableFixups;
			if (fixups == null || fixups.VTables.Count == 0)
				return;

			foreach (var vtable in fixups) {
				if (vtable == null) {
					Error("VTable is null");
					continue;
				}
				foreach (var method in vtable) {
					if (method == null) {
						Error("VTable method is null");
						continue;
					}
					AddMDTokenProvider(method);
				}
			}
		}

		void AddExportedTypes() {
			foreach (var et in module.ExportedTypes)
				AddExportedType(et);
		}

		/// <summary>
		/// Adds the entry point. It's only needed if it's a <see cref="FileDef"/> since if it's
		/// a <see cref="MethodDef"/>, it will have already been added.
		/// </summary>
		void InitializeEntryPoint() {
			var epFile = module.ManagedEntryPoint as FileDef;
			if (epFile != null)
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
			interfaceImplInfos.Sort((a, b) => {
				if (a.row.Class != b.row.Class)
					return a.row.Class.CompareTo(b.row.Class);
				return a.row.Interface.CompareTo(b.row.Interface);
			});

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
				uint rid = interfaceImplInfos.Rid(info.data);
				AddCustomAttributes(Table.InterfaceImpl, rid, info.data);
				AddCustomDebugInformationList(Table.InterfaceImpl, rid, info.data);
			}
			foreach (var info in declSecurityInfos.infos) {
				uint rid = declSecurityInfos.Rid(info.data);
				AddCustomAttributes(Table.DeclSecurity, rid, info.data);
				AddCustomDebugInformationList(Table.DeclSecurity, rid, info.data);
			}
			foreach (var info in genericParamInfos.infos) {
				uint rid = genericParamInfos.Rid(info.data);
				AddCustomAttributes(Table.GenericParam, rid, info.data);
				AddCustomDebugInformationList(Table.GenericParam, rid, info.data);
			}
		}

		/// <summary>
		/// Initializes the <c>GenericParamConstraint</c> table
		/// </summary>
		void InitializeGenericParamConstraintTable() {
			foreach (var type in allTypeDefs) {
				if (type == null)
					continue;
				AddGenericParamConstraints(type.GenericParameters);
				foreach (var method in type.Methods) {
					if (method == null)
						continue;
					AddGenericParamConstraints(method.GenericParameters);
				}
			}
			genericParamConstraintInfos.Sort((a, b) => a.row.Owner.CompareTo(b.row.Owner));
			tablesHeap.GenericParamConstraintTable.IsSorted = true;
			foreach (var info in genericParamConstraintInfos.infos)
				tablesHeap.GenericParamConstraintTable.Create(info.row);
			foreach (var info in genericParamConstraintInfos.infos) {
				uint rid = genericParamConstraintInfos.Rid(info.data);
				AddCustomAttributes(Table.GenericParamConstraint, rid, info.data);
				AddCustomDebugInformationList(Table.GenericParamConstraint, rid, info.data);
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

			if (debugMetaData != null) {
				debugMetaData.stateMachineMethodInfos.Sort((a, b) => a.row.MoveNextMethod.CompareTo(b.row.MoveNextMethod));
				debugMetaData.tablesHeap.StateMachineMethodTable.IsSorted = true;
				foreach (var info in debugMetaData.stateMachineMethodInfos.infos)
					debugMetaData.tablesHeap.StateMachineMethodTable.Create(info.row);

				debugMetaData.customDebugInfos.Sort((a, b) => a.row.Parent.CompareTo(b.row.Parent));
				debugMetaData.tablesHeap.CustomDebugInformationTable.IsSorted = true;
				foreach (var info in debugMetaData.customDebugInfos.infos)
					debugMetaData.tablesHeap.CustomDebugInformationTable.Create(info.row);
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
			int numMethods = NumberOfMethods;
			int methodNum = 0;
			int notifyNum = 0;
			const int numNotifyEvents = 10; // WriteMethodBodies0 - WriteMethodBodies9
			int notifyAfter = numMethods / numNotifyEvents;

			List<MethodScopeDebugInfo> methodScopeDebugInfos;
			List<PdbScope> scopeStack;
			SerializerMethodContext serializerMethodContext;
			if (debugMetaData == null) {
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
				if (type == null)
					continue;

				foreach (var method in type.Methods) {
					if (method == null)
						continue;

					if (methodNum++ == notifyAfter && notifyNum < numNotifyEvents) {
						Listener.OnMetaDataEvent(this, MetaDataEvent.WriteMethodBodies0 + notifyNum++);
						notifyAfter += numMethods / numNotifyEvents;
					}

					uint localVarSigTok = 0;
					uint rid = GetRid(method);

					var cilBody = method.Body;
					if (cilBody != null) {
						if (!(cilBody.Instructions.Count == 0 && cilBody.Variables.Count == 0)) {
							writer.Reset(cilBody, keepMaxStack || cilBody.KeepOldMaxStack);
							writer.Write();
							var mb = methodBodies.Add(new MethodBody(writer.Code, writer.ExtraSections, writer.LocalVarSigTok));
							methodToBody[method] = mb;
							localVarSigTok = writer.LocalVarSigTok;
						}
					}
					else {
						var nativeBody = method.NativeBody;
						if (nativeBody != null)
							methodToNativeBody[method] = nativeBody;
						else if (method.MethodBody != null)
							Error("Unsupported method body");
					}

					if (debugMetaData != null) {
						if (cilBody != null) {
							var pdbMethod = cilBody.PdbMethod;
							if (pdbMethod != null) {
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
			if (debugMetaData != null) {
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
					var row = new RawLocalScopeRow();
					debugMetaData.localScopeInfos.Add(info.Scope, row);
					uint localScopeRid = (uint)debugMetaData.localScopeInfos.infos.Count;
					row.Method = info.MethodRid;
					row.ImportScope = AddImportScope(info.Scope.ImportScope);
					row.VariableList = (uint)debugMetaData.tablesHeap.LocalVariableTable.Rows + 1;
					row.ConstantList = (uint)debugMetaData.tablesHeap.LocalConstantTable.Rows + 1;
					row.StartOffset = info.ScopeStart;
					row.Length = info.ScopeLength;
					foreach (var local in info.Scope.Variables)
						AddLocalVariable(local);
					foreach (var constant in info.Scope.Constants)
						AddLocalConstant(constant);
					AddCustomDebugInformationList(Table.LocalScope, localScopeRid, info.Scope.CustomDebugInfos);
				}

				debugMetaData.tablesHeap.LocalScopeTable.IsSorted = true;
				foreach (var info in debugMetaData.localScopeInfos.infos)
					debugMetaData.tablesHeap.LocalScopeTable.Create(info.row);
			}
			if (serializerMethodContext != null)
				Free(ref serializerMethodContext);
			while (notifyNum < numNotifyEvents)
				Listener.OnMetaDataEvent(this, MetaDataEvent.WriteMethodBodies0 + notifyNum++);
		}

		/// <summary>
		/// Checks whether a list is empty or whether it contains only <c>null</c>s
		/// </summary>
		/// <typeparam name="T">Any type</typeparam>
		/// <param name="list">The list</param>
		/// <returns><c>true</c> if the list is empty or if it contains only <c>null</c>s, <c>false</c> otherwise</returns>
		protected static bool IsEmpty<T>(IList<T> list) where T : class {
			if (list == null)
				return true;
			foreach (var e in list) {
				if (e != null)
					return false;
			}
			return true;
		}

		/// <inheritdoc/>
		public MDToken GetToken(object o) {
			var tp = o as IMDTokenProvider;
			if (tp != null)
				return new MDToken(tp.MDToken.Table, AddMDTokenProvider(tp));

			var s = o as string;
			if (s != null)
				return new MDToken((Table)0x70, usHeap.Add(s));

			var methodSig = o as MethodSig;
			if (methodSig != null)
				return new MDToken(Table.StandAloneSig, AddStandAloneSig(methodSig, methodSig.OriginalToken));

			var fieldSig = o as FieldSig;
			if (fieldSig != null)
				return new MDToken(Table.StandAloneSig, AddStandAloneSig(fieldSig, 0));

			if (o == null)
				Error("Instruction operand is null");
			else
				Error("Invalid instruction operand");
			return new MDToken((Table)0xFF, 0x00FFFFFF);
		}

		/// <inheritdoc/>
		public virtual MDToken GetToken(IList<TypeSig> locals, uint origToken) {
			if (locals == null || locals.Count == 0)
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
			if (methodSig == null) {
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
			if (fieldSig == null) {
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
			if (tp != null) {
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

			if (tp == null)
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
			if (tdr == null) {
				Error("TypeDefOrRef is null");
				return 0;
			}

			var token = new MDToken(tdr.MDToken.Table, AddMDTokenProvider(tdr));
			uint encodedToken;
			if (!CodedToken.TypeDefOrRef.Encode(token, out encodedToken)) {
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
			if (rs == null) {
				Error("ResolutionScope is null");
				return 0;
			}

			var token = new MDToken(rs.MDToken.Table, AddMDTokenProvider(rs));
			uint encodedToken;
			if (!CodedToken.ResolutionScope.Encode(token, out encodedToken)) {
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
			if (mdr == null) {
				Error("MethodDefOrRef is null");
				return 0;
			}

			var token = new MDToken(mdr.MDToken.Table, AddMDTokenProvider(mdr));
			uint encodedToken;
			if (!CodedToken.MethodDefOrRef.Encode(token, out encodedToken)) {
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
			if (parent == null) {
				Error("MemberRefParent is null");
				return 0;
			}

			var token = new MDToken(parent.MDToken.Table, AddMDTokenProvider(parent));
			uint encodedToken;
			if (!CodedToken.MemberRefParent.Encode(token, out encodedToken)) {
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
			if (impl == null) {
				Error("Implementation is null");
				return 0;
			}

			var token = new MDToken(impl.MDToken.Table, AddMDTokenProvider(impl));
			uint encodedToken;
			if (!CodedToken.Implementation.Encode(token, out encodedToken)) {
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
			if (cat == null) {
				Error("CustomAttributeType is null");
				return 0;
			}

			var token = new MDToken(cat.MDToken.Table, AddMDTokenProvider(cat));
			uint encodedToken;
			if (!CodedToken.CustomAttributeType.Encode(token, out encodedToken)) {
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
			if (nestedType == null || declaringType == null)
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
			if (module == null) {
				Error("Module is null");
				return 0;
			}
			if (this.module != module)
				Error("Module {0} must be referenced with a ModuleRef, not a ModuleDef", module);
			uint rid;
			if (moduleDefInfos.TryGetRid(module, out rid))
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
			if (modRef == null) {
				Error("ModuleRef is null");
				return 0;
			}
			uint rid;
			if (moduleRefInfos.TryGetRid(modRef, out rid))
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
			if (asmRef == null) {
				Error("AssemblyRef is null");
				return 0;
			}
			uint rid;
			if (assemblyRefInfos.TryGetRid(asmRef, out rid))
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
			if (asm == null) {
				Error("Assembly is null");
				return 0;
			}
			uint rid;
			if (assemblyInfos.TryGetRid(asm, out rid))
				return rid;

			var asmAttrs = asm.Attributes;
			if (publicKey != null)
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
			if (gps == null)
				return;
			foreach (var gp in gps)
				AddGenericParam(token, gp);
		}

		/// <summary>
		/// Adds a generic param
		/// </summary>
		/// <param name="owner">New token of owner</param>
		/// <param name="gp">Generic paramater</param>
		protected void AddGenericParam(MDToken owner, GenericParam gp) {
			if (gp == null) {
				Error("GenericParam is null");
				return;
			}
			uint encodedOwner;
			if (!CodedToken.TypeOrMethodDef.Encode(owner, out encodedOwner)) {
				Error("Can't encode TypeOrMethodDef token {0:X8}", owner.Raw);
				encodedOwner = 0;
			}
			var row = new RawGenericParamRow(gp.Number,
							(ushort)gp.Flags,
							encodedOwner,
							stringsHeap.Add(gp.Name),
							gp.Kind == null ? 0 : AddTypeDefOrRef(gp.Kind));
			genericParamInfos.Add(gp, row);
		}

		void AddGenericParamConstraints(IList<GenericParam> gps) {
			if (gps == null)
				return;
			foreach (var gp in gps) {
				if (gp == null)
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
			if (constraints == null)
				return;
			foreach (var gpc in constraints)
				AddGenericParamConstraint(gpRid, gpc);
		}

		/// <summary>
		/// Adds a generic parameter constraint
		/// </summary>
		/// <param name="gpRid">New rid of owner generic param</param>
		/// <param name="gpc">Generic parameter constraint</param>
		protected void AddGenericParamConstraint(uint gpRid, GenericParamConstraint gpc) {
			if (gpc == null) {
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
			foreach (var iface in ifaces) {
				if (iface == null)
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
			if (field == null || field.FieldOffset == null)
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
			if (hfm == null || hfm.MarshalType == null)
				return;
			var fieldMarshal = hfm.MarshalType;
			uint encodedParent;
			if (!CodedToken.HasFieldMarshal.Encode(parent, out encodedParent)) {
				Error("Can't encode HasFieldMarshal token {0:X8}", parent.Raw);
				encodedParent = 0;
			}
			var row = new RawFieldMarshalRow(encodedParent,
						blobHeap.Add(MarshalBlobWriter.Write(module, fieldMarshal, this)));
			fieldMarshalInfos.Add(hfm, row);
		}

		/// <summary>
		/// Adds a <c>FieldRVA</c> row
		/// </summary>
		/// <param name="field">The field</param>
		protected void AddFieldRVA(FieldDef field) {
			Debug.Assert(!isStandaloneDebugMetadata);
			if (field.RVA != 0 && KeepFieldRVA) {
				uint rid = GetRid(field);
				var row = new RawFieldRVARow((uint)field.RVA, rid);
				fieldRVAInfos.Add(field, row);
			}
			else {
				if (field == null || field.InitialValue == null)
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
			if (field == null)
				return false;
			var sig = field.FieldSig;
			if (sig == null)
				return false;
			return field.GetFieldSize() == size;
		}

		/// <summary>
		/// Adds a <c>ImplMap</c> row
		/// </summary>
		/// <param name="parent">New owner token</param>
		/// <param name="mf">Owner</param>
		protected void AddImplMap(MDToken parent, IMemberForwarded mf) {
			if (mf == null || mf.ImplMap == null)
				return;
			var implMap = mf.ImplMap;
			uint encodedParent;
			if (!CodedToken.MemberForwarded.Encode(parent, out encodedParent)) {
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
			if (hc == null || hc.Constant == null)
				return;
			var constant = hc.Constant;
			uint encodedParent;
			if (!CodedToken.HasConstant.Encode(parent, out encodedParent)) {
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
			if (o == null) {
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
			if (declSecurities == null)
				return;
			uint encodedParent;
			if (!CodedToken.HasDeclSecurity.Encode(parent, out encodedParent)) {
				Error("Can't encode HasDeclSecurity token {0:X8}", parent.Raw);
				encodedParent = 0;
			}
			var bwctx = AllocBinaryWriterContext();
			foreach (var decl in declSecurities) {
				if (decl == null)
					continue;
				var row = new RawDeclSecurityRow((short)decl.Action,
							encodedParent,
							blobHeap.Add(DeclSecurityWriter.Write(module, decl.SecurityAttributes, this, bwctx)));
				declSecurityInfos.Add(decl, row);
			}
			Free(ref bwctx);
		}

		/// <summary>
		/// Adds <c>MethodSemantics</c> rows
		/// </summary>
		/// <param name="evt">Event</param>
		protected void AddMethodSemantics(EventDef evt) {
			if (evt == null) {
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
			if (prop == null) {
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
			if (methods == null)
				return;
			foreach (var method in methods)
				AddMethodSemantics(owner, method, attrs);
		}

		void AddMethodSemantics(MDToken owner, MethodDef method, MethodSemanticsAttributes flags) {
			if (method == null)
				return;
			uint methodRid = GetRid(method);
			if (methodRid == 0)
				return;
			uint encodedOwner;
			if (!CodedToken.HasSemantic.Encode(owner, out encodedOwner)) {
				Error("Can't encode HasSemantic token {0:X8}", owner.Raw);
				encodedOwner = 0;
			}
			var row = new RawMethodSemanticsRow((ushort)flags, methodRid, encodedOwner);
			methodSemanticsInfos.Add(method, row);
		}

		void AddMethodImpls(MethodDef method, IList<MethodOverride> overrides) {
			if (overrides == null)
				return;
			if (method.DeclaringType == null) {
				Error("Method declaring type == null. Method {0} ({1:X8})", method, method.MDToken.Raw);
				return;
			}
			uint rid = GetRid(method.DeclaringType);
			foreach (var ovr in overrides) {
				var row = new RawMethodImplRow(rid,
							AddMethodDefOrRef(ovr.MethodBody),
							AddMethodDefOrRef(ovr.MethodDeclaration));
				methodImplInfos.Add(method, row);
			}
		}

		/// <summary>
		/// Adds a <c>ClassLayout</c> row
		/// </summary>
		/// <param name="type">Type</param>
		protected void AddClassLayout(TypeDef type) {
			if (type == null || type.ClassLayout == null)
				return;
			var rid = GetRid(type);
			var classLayout = type.ClassLayout;
			var row = new RawClassLayoutRow(classLayout.PackingSize, classLayout.ClassSize, rid);
			classLayoutInfos.Add(type, row);
		}

		void AddResources(IList<Resource> resources) {
			if (resources == null)
				return;
			foreach (var resource in resources)
				AddResource(resource);
		}

		void AddResource(Resource resource) {
			var er = resource as EmbeddedResource;
			if (er != null) {
				AddEmbeddedResource(er);
				return;
			}

			var alr = resource as AssemblyLinkedResource;
			if (alr != null) {
				AddAssemblyLinkedResource(alr);
				return;
			}

			var lr = resource as LinkedResource;
			if (lr != null) {
				AddLinkedResource(lr);
				return;
			}

			if (resource == null)
				Error("Resource is null");
			else
				Error("Invalid resource type: {0}", resource.GetType());
		}

		uint AddEmbeddedResource(EmbeddedResource er) {
			Debug.Assert(!isStandaloneDebugMetadata);
			if (er == null) {
				Error("EmbeddedResource is null");
				return 0;
			}
			uint rid;
			if (manifestResourceInfos.TryGetRid(er, out rid))
				return rid;
			var row = new RawManifestResourceRow(netResources.NextOffset,
						(uint)er.Attributes,
						stringsHeap.Add(er.Name),
						0);
			rid = tablesHeap.ManifestResourceTable.Add(row);
			manifestResourceInfos.Add(er, rid);
			embeddedResourceToByteArray[er] = netResources.Add(er.Data);
			//TODO: Add custom attributes
			//TODO: Add custom debug infos
			return rid;
		}

		uint AddAssemblyLinkedResource(AssemblyLinkedResource alr) {
			if (alr == null) {
				Error("AssemblyLinkedResource is null");
				return 0;
			}
			uint rid;
			if (manifestResourceInfos.TryGetRid(alr, out rid))
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
			if (lr == null) {
				Error("LinkedResource is null");
				return 0;
			}
			uint rid;
			if (manifestResourceInfos.TryGetRid(lr, out rid))
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
			if (file == null) {
				Error("FileDef is null");
				return 0;
			}
			uint rid;
			if (fileDefInfos.TryGetRid(file, out rid))
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
			if (et == null) {
				Error("ExportedType is null");
				return 0;
			}
			uint rid;
			if (exportedTypeInfos.TryGetRid(et, out rid))
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
			if (ts == null) {
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
			if (sig == null) {
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
			if (PreserveExtraSignatureData && extraData != null && extraData.Length > 0) {
				int blen = blob == null ? 0 : blob.Length;
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
		protected void AddCustomAttributes(Table table, uint rid, IHasCustomAttribute hca) {
			AddCustomAttributes(table, rid, hca.CustomAttributes);
		}

		void AddCustomAttributes(Table table, uint rid, CustomAttributeCollection caList) {
			var token = new MDToken(table, rid);
			foreach (var ca in caList)
				AddCustomAttribute(token, ca);
		}

		void AddCustomAttribute(MDToken token, CustomAttribute ca) {
			if (ca == null) {
				Error("Custom attribute is null");
				return;
			}
			uint encodedToken;
			if (!CodedToken.HasCustomAttribute.Encode(token, out encodedToken)) {
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
			if (debugMetaData == null)
				return;
			var serializerMethodContext = AllocSerializerMethodContext();
			serializerMethodContext.SetBody(method);
			if (method.CustomDebugInfos.Count != 0)
				AddCustomDebugInformationCore(serializerMethodContext, Table.Method, rid, method.CustomDebugInfos);
			AddMethodDebugInformation(method, rid, localVarSigToken);
			Free(ref serializerMethodContext);
		}

		void AddMethodDebugInformation(MethodDef method, uint rid, uint localVarSigToken) {
			Debug.Assert(debugMetaData != null);
			var body = method.Body;
			if (body == null)
				return;

			bool hasNoSeqPoints;
			PdbDocument singleDoc, firstDoc;
			GetSingleDocument(body, out singleDoc, out firstDoc, out hasNoSeqPoints);
			if (hasNoSeqPoints)
				return;

			var bwctx = AllocBinaryWriterContext();
			var outStream = bwctx.OutStream;
			var writer = bwctx.Writer;
			outStream.SetLength(0);
			outStream.Position = 0;

			writer.WriteCompressedUInt32(localVarSigToken);
			if (singleDoc == null)
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
				if (seqPoint == null)
					continue;
				if (seqPoint.Document == null) {
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
			var row = debugMetaData.tablesHeap.MethodDebugInformationTable[rid];
			row.Document = singleDoc == null ? 0 : AddPdbDocument(singleDoc);
			row.SequencePoints = debugMetaData.blobHeap.Add(seqPointsBlob);
			debugMetaData.methodDebugInformationInfosUsed = true;
			Free(ref bwctx);
		}

		uint VerifyGetRid(PdbDocument doc) {
			Debug.Assert(debugMetaData != null);
			uint rid;
			if (!debugMetaData.pdbDocumentInfos.TryGetRid(doc, out rid)) {
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
				if (seqPt == null)
					continue;
				var doc = seqPt.Document;
				if (doc == null)
					continue;
				if (firstDoc == null)
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
			if (debugMetaData == null)
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
			if (debugMetaData == null)
				return;
			if (cdis.Count == 0)
				return;
			var serializerMethodContext = AllocSerializerMethodContext();
			serializerMethodContext.SetBody(null);
			AddCustomDebugInformationCore(serializerMethodContext, table, rid, cdis);
			Free(ref serializerMethodContext);
		}

		void AddCustomDebugInformationCore(SerializerMethodContext serializerMethodContext, Table table, uint rid, IList<PdbCustomDebugInfo> cdis) {
			Debug.Assert(debugMetaData != null);
			Debug.Assert(cdis.Count != 0);

			var token = new MDToken(table, rid);
			uint encodedToken;
			if (!CodedToken.HasCustomDebugInformation.Encode(token, out encodedToken)) {
				Error("Couldn't encode HasCustomDebugInformation token {0:X8}", token.Raw);
				return;
			}

			for (int i = 0; i < cdis.Count; i++) {
				var cdi = cdis[i];
				if (cdi == null) {
					Error("Custom debug info is null");
					continue;
				}

				AddCustomDebugInformation(serializerMethodContext, token.Raw, encodedToken, cdi);
			}
		}

		void AddCustomDebugInformation(SerializerMethodContext serializerMethodContext, uint token, uint encodedToken, PdbCustomDebugInfo cdi) {
			Debug.Assert(debugMetaData != null);

			switch (cdi.Kind) {
			case PdbCustomDebugInfoKind.UsingGroups:
			case PdbCustomDebugInfoKind.ForwardMethodInfo:
			case PdbCustomDebugInfoKind.ForwardModuleInfo:
			case PdbCustomDebugInfoKind.StateMachineTypeName:
			case PdbCustomDebugInfoKind.DynamicLocals:
			case PdbCustomDebugInfoKind.TupleElementNames:
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
			Debug.Assert(debugMetaData != null);
			if (kickoffMethod == null) {
				Error("KickoffMethod is null");
				return;
			}
			var row = new RawStateMachineMethodRow(new MDToken(moveNextMethodToken).Rid, GetRid(kickoffMethod));
			debugMetaData.stateMachineMethodInfos.Add(cdi, row);
		}

		void AddCustomDebugInformationCore(SerializerMethodContext serializerMethodContext, uint encodedToken, PdbCustomDebugInfo cdi, Guid cdiGuid) {
			Debug.Assert(debugMetaData != null);

			var bwctx = AllocBinaryWriterContext();
			var cdiBlob = PortablePdbCustomDebugInfoWriter.Write(this, serializerMethodContext, this, cdi, bwctx);
			Debug.Assert(cdiGuid != Guid.Empty);
			Free(ref bwctx);
			var row = new RawCustomDebugInformationRow(encodedToken,
						debugMetaData.guidHeap.Add(cdiGuid),
						debugMetaData.blobHeap.Add(cdiBlob));
			debugMetaData.customDebugInfos.Add(cdi, row);
		}

		void InitializeMethodDebugInformation() {
			if (debugMetaData == null)
				return;
			int numMethods = NumberOfMethods;
			for (int i = 0; i < numMethods; i++)
				debugMetaData.tablesHeap.MethodDebugInformationTable.Create(new RawMethodDebugInformationRow());
		}

		void AddPdbDocuments() {
			if (debugMetaData == null)
				return;
			foreach (var doc in module.PdbState.Documents)
				AddPdbDocument(doc);
		}

		uint AddPdbDocument(PdbDocument doc) {
			Debug.Assert(debugMetaData != null);
			if (doc == null) {
				Error("PdbDocument is null");
				return 0;
			}
			uint rid;
			if (debugMetaData.pdbDocumentInfos.TryGetRid(doc, out rid))
				return rid;
			var row = new RawDocumentRow(GetDocumentNameBlobOffset(doc.Url),
							debugMetaData.guidHeap.Add(doc.CheckSumAlgorithmId),
							debugMetaData.blobHeap.Add(doc.CheckSum),
							debugMetaData.guidHeap.Add(doc.Language));
			rid = debugMetaData.tablesHeap.DocumentTable.Add(row);
			debugMetaData.pdbDocumentInfos.Add(doc, rid);
			AddCustomDebugInformationList(Table.Document, rid, doc.CustomDebugInfos);
			return rid;
		}

		uint GetDocumentNameBlobOffset(string name) {
			Debug.Assert(debugMetaData != null);
			if (name == null) {
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
				writer.Write((byte)0);
			else
				writer.Write(directorySeparatorCharUtf8);
			for (int i = 0; i < parts.Length; i++) {
				var part = parts[i];
				uint partOffset = debugMetaData.blobHeap.Add(Encoding.UTF8.GetBytes(part));
				writer.WriteCompressedUInt32(partOffset);
			}

			var res = debugMetaData.blobHeap.Add(outStream.ToArray());
			Free(ref bwctx);
			return res;
		}
		static readonly byte[] directorySeparatorCharUtf8 = Encoding.UTF8.GetBytes(Path.DirectorySeparatorChar.ToString());
		static readonly char[] directorySeparatorCharArray = new char[] { Path.DirectorySeparatorChar };

		uint AddImportScope(PdbImportScope scope) {
			Debug.Assert(debugMetaData != null);
			if (scope == null)
				return 0;
			uint rid;
			if (debugMetaData.importScopeInfos.TryGetRid(scope, out rid)) {
				if (rid == 0)
					Error("PdbImportScope has an infinite Parent loop");
				return rid;
			}
			debugMetaData.importScopeInfos.Add(scope, 0);   // Prevent inf recursion

			var bwctx = AllocBinaryWriterContext();
			var outStream = bwctx.OutStream;
			var writer = bwctx.Writer;
			outStream.SetLength(0);
			outStream.Position = 0;
			ImportScopeBlobWriter.Write(this, this, writer, debugMetaData.blobHeap, scope.Imports);
			var importsData = outStream.ToArray();
			Free(ref bwctx);

			var row = new RawImportScopeRow(AddImportScope(scope.Parent), debugMetaData.blobHeap.Add(importsData));
			rid = debugMetaData.tablesHeap.ImportScopeTable.Add(row);
			debugMetaData.importScopeInfos.SetRid(scope, rid);

			AddCustomDebugInformationList(Table.ImportScope, rid, scope.CustomDebugInfos);
			return rid;
		}

		void AddLocalVariable(PdbLocal local) {
			Debug.Assert(debugMetaData != null);
			if (local == null) {
				Error("PDB local is null");
				return;
			}
			var row = new RawLocalVariableRow((ushort)local.Attributes, (ushort)local.Index, debugMetaData.stringsHeap.Add(local.Name));
			uint rid = debugMetaData.tablesHeap.LocalVariableTable.Create(row);
			debugMetaData.localVariableInfos.Add(local, rid);
			AddCustomDebugInformationList(Table.LocalVariable, rid, local.CustomDebugInfos);
		}

		void AddLocalConstant(PdbConstant constant) {
			Debug.Assert(debugMetaData != null);
			if (constant == null) {
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

			var row = new RawLocalConstantRow(debugMetaData.stringsHeap.Add(constant.Name), debugMetaData.blobHeap.Add(signature));
			uint rid = debugMetaData.tablesHeap.LocalConstantTable.Create(row);
			debugMetaData.localConstantInfos.Add(constant, rid);
			AddCustomDebugInformationList(Table.LocalConstant, rid, constant.CustomDebugInfos);
		}

		/// <summary>
		/// Writes the portable PDB to <paramref name="output"/>.
		/// </summary>
		/// <param name="output">Output stream</param>
		/// <param name="entryPointToken">Entry point token</param>
		/// <param name="pdbId">PDB ID, exactly 20 bytes</param>
		internal void WritePortablePdb(Stream output, uint entryPointToken, byte[] pdbId) {
			if (debugMetaData == null)
				throw new InvalidOperationException();
			if (pdbId.Length != 20)
				throw new InvalidOperationException();
			var pdbHeap = debugMetaData.PdbHeap;
			pdbHeap.EntryPoint = entryPointToken;
			for (int i = 0; i < pdbId.Length; i++)
				pdbHeap.PdbId[i] = pdbId[i];

			ulong systemTablesMask;
			tablesHeap.GetSystemTableRows(out systemTablesMask, pdbHeap.TypeSystemTableRows);
			debugMetaData.tablesHeap.SetSystemTableRows(pdbHeap.TypeSystemTableRows);
			if (!debugMetaData.methodDebugInformationInfosUsed)
				debugMetaData.tablesHeap.MethodDebugInformationTable.Reset();
			pdbHeap.ReferencedTypeSystemTables = systemTablesMask;
			var writer = new BinaryWriter(output);
			debugMetaData.SetOffset(0, 0);
			debugMetaData.GetFileLength();
			debugMetaData.VerifyWriteTo(writer);
		}

		/// <inheritdoc/>
		uint ISignatureWriterHelper.ToEncodedToken(ITypeDefOrRef typeDefOrRef) {
			return AddTypeDefOrRef(typeDefOrRef);
		}

		/// <inheritdoc/>
		void IWriterError.Error(string message) {
			Error(message);
		}

		/// <inheritdoc/>
		bool IFullNameCreatorHelper.MustUseAssemblyName(IType type) {
			return FullNameCreator.MustUseAssemblyName(module, type);
		}

		/// <summary>
		/// Called before any other methods
		/// </summary>
		protected virtual void Initialize() {
		}

		/// <summary>
		/// Gets all <see cref="TypeDef"/>s that should be saved in the meta data
		/// </summary>
		protected abstract List<TypeDef> GetAllTypeDefs();

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
		/// Initializes <see cref="MetaData.eventMapInfos"/> and <see cref="MetaData.propertyMapInfos"/>.
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

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;

			stringsHeap.SetReadOnly();
			blobHeap.SetReadOnly();
			guidHeap.SetReadOnly();
			tablesHeap.SetReadOnly();
			pdbHeap.SetReadOnly();
			tablesHeap.BigStrings = stringsHeap.IsBig;
			tablesHeap.BigBlob = blobHeap.IsBig;
			tablesHeap.BigGuid = guidHeap.IsBig;

			metaDataHeader.Heaps = GetHeaps();

			metaDataHeader.SetOffset(offset, rva);
			uint len = metaDataHeader.GetFileLength();
			offset += len;
			rva += len;

			foreach (var heap in metaDataHeader.Heaps) {
				offset = offset.AlignUp(HEAP_ALIGNMENT);
				rva = rva.AlignUp(HEAP_ALIGNMENT);
				heap.SetOffset(offset, rva);
				len = heap.GetFileLength();
				offset += len;
				rva += len;
			}
			length = rva - this.rva;

			if (!isStandaloneDebugMetadata) {
				UpdateMethodRvas();
				UpdateFieldRvas();
			}
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
				if (options.OtherHeaps != null)
					heaps.AddRange(options.OtherHeaps);

				// The #! heap must be added before the other heaps or the CLR can
				// sometimes flag an error. Eg., it can check whether a pointer is valid.
				// It does this by comparing the pointer to the last valid address for
				// the particular heap. If this pointer really is in the #! heap and the
				// #! heap is at an address > than the other heap, then the CLR will think
				// it's an invalid pointer.
				if (hotHeap != null)  // Don't check whether it's empty
					heaps.Add(hotHeap);

				heaps.Add(tablesHeap);
				if (!stringsHeap.IsEmpty || AlwaysCreateStringsHeap)
					heaps.Add(stringsHeap);
				if (!usHeap.IsEmpty || AlwaysCreateUSHeap)
					heaps.Add(usHeap);
				if (!guidHeap.IsEmpty || AlwaysCreateGuidHeap)
					heaps.Add(guidHeap);
				if (!blobHeap.IsEmpty || AlwaysCreateBlobHeap)
					heaps.Add(blobHeap);

				if (options.OtherHeapsEnd != null)
					heaps.AddRange(options.OtherHeapsEnd);
			}

			return heaps;
		}

		/// <inheritdoc/>
		public uint GetFileLength() {
			return length;
		}

		/// <inheritdoc/>
		public uint GetVirtualSize() {
			return GetFileLength();
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			var rva2 = rva;
			metaDataHeader.VerifyWriteTo(writer);
			rva2 += metaDataHeader.GetFileLength();

			foreach (var heap in metaDataHeader.Heaps) {
				writer.WriteZeros((int)(rva2.AlignUp(HEAP_ALIGNMENT) - rva2));
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
				if (a == null)
					return -1;
				if (b == null)
					return 1;
				return a.Sequence.CompareTo(b.Sequence);
			});
			return sorted;
		}

		BinaryWriterContext AllocBinaryWriterContext() {
			if (binaryWriterContexts.Count == 0)
				return new BinaryWriterContext();
			var res = binaryWriterContexts[binaryWriterContexts.Count - 1];
			binaryWriterContexts.RemoveAt(binaryWriterContexts.Count - 1);
			return res;
		}

		void Free(ref BinaryWriterContext ctx) {
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
