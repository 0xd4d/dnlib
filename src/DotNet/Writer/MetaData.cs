using System;
using System.Collections.Generic;
using System.IO;
using dot10.IO;
using dot10.PE;
using dot10.DotNet.MD;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// <see cref="MetaData"/> options
	/// </summary>
	[Flags]
	enum MetaDataOptions {
		/// <summary>
		/// Preserves all rids in the following tables: <c>TypeRef</c>, <c>TypeDef</c>,
		/// <c>Field</c>, <c>Method</c>, <c>Param</c>, <c>MemberRef</c>, <c>StandAloneSig</c>,
		/// <c>Event</c>, <c>Property</c>, <c>TypeSpec</c>, <c>MethodSpec</c>
		/// </summary>
		PreserveTokens = 1,

		/// <summary>
		/// Preserves all offsets in the #Strings heap (the original #Strings heap will be saved
		/// in the new file). Type names, field names, and other non-user strings are stored
		/// in the #Strings heap.
		/// </summary>
		PreserveStringsOffsets = 2,

		/// <summary>
		/// Preserves all offsets in the #US heap (the original #US heap will be saved
		/// in the new file). User strings (referenced by the ldstr instruction) are stored in
		/// the #US heap.
		/// </summary>
		PreserveUSOffsets = 4,

		/// <summary>
		/// Preserves all offsets in the #Blob heap (the original #Blob heap will be saved
		/// in the new file). Custom attributes, signatures and other blobs are stored in the
		/// #Blob heap.
		/// </summary>
		PreserveBlobOffsets = 8,
	}

	/// <summary>
	/// .NET meta data
	/// </summary>
	class MetaData : IChunk {
		ModuleDef module;
		UniqueChunkList<ByteArrayChunk> constants;
		MethodBodyChunks methodBodies;
		ChunkList<IChunk> netResources;
		TablesHeap tablesHeap;
		StringsHeap stringsHeap;
		USHeap usHeap;
		GuidHeap guidHeap;
		BlobHeap blobHeap;
		FileOffset offset;
		RVA rva;
		MetaDataOptions options;
		ITablesCreator tablesCreator;

		interface ITablesCreator {
			void Create();
		}

		class NormalTablesCreator : ITablesCreator {
			MetaData metaData;
			List<TypeDef> sortedTypes;
			Rows<ModuleDef> moduleDefInfos = new Rows<ModuleDef>();
			Rows<TypeRef> typeRefInfos = new Rows<TypeRef>();
			Rows<TypeDef> typeDefInfos = new Rows<TypeDef>();
			Rows<TypeSpec> typeSpecInfos = new Rows<TypeSpec>();
			Rows<ModuleRef> moduleRefInfos = new Rows<ModuleRef>();
			Rows<AssemblyRef> assemblyRefInfos = new Rows<AssemblyRef>();
			Rows<FieldDef> fieldDefInfos = new Rows<FieldDef>();
			Rows<MethodDef> methodDefInfos = new Rows<MethodDef>();
			Rows<EventDef> eventDefInfos = new Rows<EventDef>();
			Rows<PropertyDef> propertyDefInfos = new Rows<PropertyDef>();
			List<RawGenericParamRow> genericParams = new List<RawGenericParamRow>();
			List<RawEventMapRow> eventMaps = new List<RawEventMapRow>();
			List<RawPropertyMapRow> propertyMaps = new List<RawPropertyMapRow>();

			class Rows<T> where T : IMDTokenProvider {
				Dictionary<T, uint> dict = new Dictionary<T, uint>();

				public bool TryGetRid(T value, out uint rid) {
					if (value == null) {
						rid = 0;
						return false;
					}
					return dict.TryGetValue(value, out rid);
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

			public NormalTablesCreator(MetaData metaData) {
				this.metaData = metaData;
			}

			public void Create() {
				sortedTypes = GetSortedTypes();

				// Do what the VC# compiler does. It first creates the <Module> type
				// followed by the module row.
				if (sortedTypes.Count > 0)
					AddTypeDef(sortedTypes[0]);
				AddModule(metaData.module);
				for (int i = 1; i < sortedTypes.Count; i++)
					AddTypeDef(sortedTypes[i]);

				uint fieldListRid = 1, methodListRid = 1;
				uint eventListRid = 1, propertyListRid = 1;
				foreach (var type in sortedTypes) {
					if (type == null)
						continue;
					uint typeRid = typeDefInfos.Rid(type);
					var typeRow = metaData.tablesHeap.TypeDefTable[typeRid];
					foreach (var gp in type.GenericParams)
						AddGenericParam(new MDToken(Table.TypeDef, typeRid), gp);
					typeRow.Extends = AddTypeDefOrRef(type.BaseType);	//TODO: null is allowed here if <Module> or iface so don't warn user
					typeRow.FieldList = fieldListRid;
					typeRow.MethodList = methodListRid;

					foreach (var field in type.Fields) {
						if (field == null)
							continue;
						uint rid = fieldListRid++;
						var row = new RawFieldRow((ushort)field.Flags,
									metaData.stringsHeap.Add(field.Name),
									GetSignature(field.Signature));
						fieldDefInfos.Add(field, rid);
						//TODO:
					}

					foreach (var method in type.Methods) {
						if (method == null)
							continue;
						uint rid = methodListRid++;
						var row = new RawMethodRow(0,
									(ushort)method.ImplFlags,
									(ushort)method.Flags,
									metaData.stringsHeap.Add(method.Name),
									GetSignature(method.Signature),
									0);
						methodDefInfos.Add(method, rid);
						//TODO:
					}

					if (!IsEmpty(type.Events)) {
						eventMaps.Add(new RawEventMapRow(typeRid, eventListRid));
						foreach (var evt in type.Events) {
							if (evt == null)
								continue;
							uint rid = eventListRid++;
							var row = new RawEventRow((ushort)evt.Flags,
										metaData.stringsHeap.Add(evt.Name),
										AddTypeDefOrRef(evt.Type));
							eventDefInfos.Add(evt, rid);
							//TODO: MethodSemantics
						}
					}

					if (!IsEmpty(type.Properties)) {
						propertyMaps.Add(new RawPropertyMapRow(typeRid, propertyListRid));
						foreach (var prop in type.Properties) {
							if (prop == null)
								continue;
							uint rid = propertyListRid++;
							var row = new RawPropertyRow((ushort)prop.Flags,
										metaData.stringsHeap.Add(prop.Name),
										GetSignature(prop.Type));
							propertyDefInfos.Add(prop, rid);
							//TODO: MethodSemantics
						}
					}
				}

				//TODO: Write all params

				var asm = metaData.module.Assembly;
				if (asm != null) {
					//TODO: Write assembly
				}

				foreach (var type in sortedTypes) {
					foreach (var method in type.Methods) {
						//TODO:
					}
				}

				//TODO: Write module cust attr
				//TODO: Write assembly cust attr
				//TODO: Write assembly decl security
			}

			static bool IsEmpty<T>(IList<T> list) where T : class {
				if (list == null)
					return true;
				foreach (var e in list) {
					if (e != null)
						return false;
				}
				return true;
			}

			uint AddModule(ModuleDef module) {
				if (module == null)
					return 0;	//TODO: Warn user
				var row = new RawModuleRow(module.Generation,
									metaData.stringsHeap.Add(module.Name),
									metaData.guidHeap.Add(module.Mvid),
									metaData.guidHeap.Add(module.EncId),
									metaData.guidHeap.Add(module.EncBaseId));
				uint rid = metaData.tablesHeap.ModuleTable.Create(row);
				moduleDefInfos.Add(module, rid);
				return rid;
			}

			/// <summary>
			/// Creates a <c>TypeDef</c> row and initializes its Flags, Name, and Namespace columns.
			/// </summary>
			/// <param name="td">The type</param>
			/// <returns>The new <c>rid</c></returns>
			uint AddTypeDef(TypeDef td) {
				if (td == null)
					return 0;	//TODO: Warn user
				var row = new RawTypeDefRow((uint)td.Flags,
							metaData.stringsHeap.Add(td.Name),
							metaData.stringsHeap.Add(td.Namespace),
							0, 0, 0);
				uint rid = metaData.tablesHeap.TypeDefTable.Create(row);
				typeDefInfos.Add(td, rid);
				return rid;
			}

			void AddGenericParam(MDToken owner, GenericParam gp) {
				if (gp == null)
					return;	//TODO: Warn user
				uint encodedOwner;
				if (!CodedToken.TypeOrMethodDef.Encode(owner, out encodedOwner))
					throw new ModuleWriterException("Can't encode GenericParam owner token");
				var row = new RawGenericParamRow(gp.Number,
								(ushort)gp.Flags,
								encodedOwner,
								metaData.stringsHeap.Add(gp.Name),
								gp.Kind == null ? 0 : AddTypeDefOrRef(gp.Kind));
				genericParams.Add(row);
			}

			uint GetTypeDefRid(TypeDef td) {
				uint rid;
				if (typeDefInfos.TryGetRid(td, out rid))
					return rid;
				return 0;	//TODO: Warn user
			}

			uint AddTypeDefOrRef(ITypeDefOrRef tdr) {
				uint encodedToken;

				var td = tdr as TypeDef;
				if (td != null) {
					var token = new MDToken(Table.TypeDef, GetTypeDefRid(td));
					if (!CodedToken.TypeDefOrRef.Encode(token, out encodedToken))
						throw new ModuleWriterException("Can't encode a TypeDefOrRef token");
					return encodedToken;
				}

				var tr = tdr as TypeRef;
				if (tr != null) {
					var token = new MDToken(Table.TypeRef, AddTypeRef(tr));
					if (!CodedToken.TypeDefOrRef.Encode(token, out encodedToken))
						throw new ModuleWriterException("Can't encode a TypeDefOrRef token");
					return encodedToken;
				}

				var ts = tdr as TypeSpec;
				if (ts != null) {
					var token = new MDToken(Table.TypeSpec, AddTypeSpec(ts));
					if (!CodedToken.TypeDefOrRef.Encode(token, out encodedToken))
						throw new ModuleWriterException("Can't encode a TypeDefOrRef token");
					return encodedToken;
				}

				return 0;	//TODO: Warn user
			}

			uint AddTypeRef(TypeRef tr) {
				if (tr == null)
					return 0;	//TODO: Warn user
				uint rid;
				if (typeRefInfos.TryGetRid(tr, out rid))
					return rid;	//TODO: If rid == 0, warn user
				typeRefInfos.Add(tr, 0);	// Prevent inf recursion
				var row = new RawTypeRefRow(AddResolutionScope(tr.ResolutionScope),
							metaData.stringsHeap.Add(tr.Name),
							metaData.stringsHeap.Add(tr.Namespace));
				rid = metaData.tablesHeap.TypeRefTable.Add(row);
				typeRefInfos.SetRid(tr, rid);
				return rid;
			}

			uint AddResolutionScope(IResolutionScope rs) {
				uint encodedToken;

				var asmRef = rs as AssemblyRef;
				if (asmRef != null) {
					var token = new MDToken(Table.AssemblyRef, AddAssemblyRef(asmRef));
					if (!CodedToken.ResolutionScope.Encode(token, out encodedToken))
						throw new ModuleWriterException("Can't encode a ResolutionScope token");
					return encodedToken;
				}

				var tr = rs as TypeRef;
				if (tr != null) {
					var token = new MDToken(Table.TypeRef, AddTypeRef(tr));
					if (!CodedToken.ResolutionScope.Encode(token, out encodedToken))
						throw new ModuleWriterException("Can't encode a ResolutionScope token");
					return encodedToken;
				}

				var modRef = rs as ModuleRef;
				if (modRef != null) {
					var token = new MDToken(Table.ModuleRef, AddModuleRef(modRef));
					if (!CodedToken.ResolutionScope.Encode(token, out encodedToken))
						throw new ModuleWriterException("Can't encode a ResolutionScope token");
					return encodedToken;
				}

				//TODO: Error if mod is not metaData.module
				var mod = rs as ModuleDef;
				if (mod != null) {
					var token = new MDToken(Table.Module, AddModule(mod));
					if (!CodedToken.ResolutionScope.Encode(token, out encodedToken))
						throw new ModuleWriterException("Can't encode a ResolutionScope token");
					return encodedToken;
				}

				return 0;	//TODO: Warn user
			}

			uint AddTypeSpec(TypeSpec ts) {
				if (ts == null)
					return 0;	//TODO: Warn user
				uint rid;
				if (typeSpecInfos.TryGetRid(ts, out rid))
					return rid;	//TODO: If rid == 0, warn user
				typeSpecInfos.Add(ts, 0);	// Prevent inf recursion
				var row = new RawTypeSpecRow(GetSignature(ts.TypeSig));
				rid = metaData.tablesHeap.TypeSpecTable.Add(row);
				typeSpecInfos.SetRid(ts, rid);
				return rid;
			}

			uint AddModuleRef(ModuleRef modRef) {
				if (modRef == null)
					return 0;	//TODO: Warn user
				uint rid;
				if (moduleRefInfos.TryGetRid(modRef, out rid))
					return rid;
				var row = new RawModuleRefRow(metaData.stringsHeap.Add(modRef.Name));
				rid = metaData.tablesHeap.ModuleRefTable.Add(row);
				moduleRefInfos.Add(modRef, rid);
				return rid;
			}

			uint AddAssemblyRef(AssemblyRef asmRef) {
				if (asmRef == null)
					return 0;	//TODO: Warn user
				uint rid;
				if (assemblyRefInfos.TryGetRid(asmRef, out rid))
					return rid;
				var version = Utils.CreateVersionWithNoUndefinedValues(asmRef.Version);
				var row = new RawAssemblyRefRow((ushort)version.Major,
								(ushort)version.Minor,
								(ushort)version.Build,
								(ushort)version.Revision,
								(uint)asmRef.Flags,
								metaData.blobHeap.Add(GetPublicKeyOrTokenData(asmRef.PublicKeyOrToken)),
								metaData.stringsHeap.Add(asmRef.Name),
								metaData.stringsHeap.Add(asmRef.Locale),
								metaData.blobHeap.Add(asmRef.HashValue));
				rid = metaData.tablesHeap.AssemblyRefTable.Add(row);
				assemblyRefInfos.Add(asmRef, rid);
				return rid;
			}

			static byte[] GetPublicKeyOrTokenData(PublicKeyBase pkb) {
				if (pkb == null)
					return null;
				return pkb.Data;
			}

			uint GetSignature(TypeSig ts) {
				if (ts == null)
					return 0;	//TODO: Warn user

				return 0;	//TODO:
			}

			uint GetSignature(CallingConventionSig sig) {
				if (sig == null)
					return 0;	//TODO: Warn user

				return 0;	//TODO:
			}

			/// <summary>
			/// Gets all <see cref="TypeDef"/>s in the order that they'll be saved in the
			/// <c>TypeDef</c> table.
			/// </summary>
			List<TypeDef> GetSortedTypes() {
				// All nested types must be after their enclosing type. This is exactly
				// what module.GetTypes() does.
				return new List<TypeDef>(metaData.module.GetTypes());
			}
		}

		class PreserveTokensTablesWriter : ITablesCreator {
			MetaData metaData;

			public PreserveTokensTablesWriter(MetaData metaData) {
				this.metaData = metaData;
			}

			public void Create() {
				throw new NotImplementedException();	//TODO:
			}
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
		/// Gets/sets the options
		/// </summary>
		public MetaDataOptions Options {
			get { return options; }
			set { options = value; }
		}

		/// <summary>
		/// Gets/sets the <see cref="MetaDataOptions.PreserveTokens"/> bit
		/// </summary>
		public bool PreserveTokens {
			get { return (options & MetaDataOptions.PreserveTokens) != 0; }
			set {
				if (value)
					options |= MetaDataOptions.PreserveTokens;
				else
					options &= ~MetaDataOptions.PreserveTokens;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MetaDataOptions.PreserveStringsOffsets"/> bit
		/// </summary>
		public bool PreserveStringsOffsets {
			get { return (options & MetaDataOptions.PreserveStringsOffsets) != 0; }
			set {
				if (value)
					options |= MetaDataOptions.PreserveStringsOffsets;
				else
					options &= ~MetaDataOptions.PreserveStringsOffsets;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MetaDataOptions.PreserveUSOffsets"/> bit
		/// </summary>
		public bool PreserveUSOffsets {
			get { return (options & MetaDataOptions.PreserveUSOffsets) != 0; }
			set {
				if (value)
					options |= MetaDataOptions.PreserveUSOffsets;
				else
					options &= ~MetaDataOptions.PreserveUSOffsets;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="MetaDataOptions.PreserveBlobOffsets"/> bit
		/// </summary>
		public bool PreserveBlobOffsets {
			get { return (options & MetaDataOptions.PreserveBlobOffsets) != 0; }
			set {
				if (value)
					options |= MetaDataOptions.PreserveBlobOffsets;
				else
					options &= ~MetaDataOptions.PreserveBlobOffsets;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="constants">Constants list</param>
		/// <param name="methodBodies">Method bodies list</param>
		/// <param name="netResources">.NET resources list</param>
		public MetaData(ModuleDef module, UniqueChunkList<ByteArrayChunk> constants, MethodBodyChunks methodBodies, ChunkList<IChunk> netResources) {
			this.module = module;
			this.constants = constants;
			this.methodBodies = methodBodies;
			this.netResources = netResources;
			this.tablesHeap = new TablesHeap();
			this.stringsHeap = new StringsHeap();
			this.usHeap = new USHeap();
			this.guidHeap = new GuidHeap();
			this.blobHeap = new BlobHeap();
		}

		/// <summary>
		/// Creates the .NET metadata tables
		/// </summary>
		public void CreateTables() {
			if (module.Types.Count == 0 || module.Types[0] == null)
				throw new ModuleWriterException("Missing <Module> type");

			var moduleDefMD = module as ModuleDefMD;
			if (moduleDefMD != null) {
				if (PreserveStringsOffsets)
					stringsHeap.Populate(moduleDefMD.StringsStream);
				if (PreserveUSOffsets)
					usHeap.Populate(moduleDefMD.USStream);
				if (PreserveBlobOffsets)
					blobHeap.Populate(moduleDefMD.BlobStream);
			}

			if (PreserveTokens)
				tablesCreator = new PreserveTokensTablesWriter(this);
			else
				tablesCreator = new NormalTablesCreator(this);

			tablesCreator.Create();
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;
			throw new System.NotImplementedException();
		}

		/// <inheritdoc/>
		public uint GetLength() {
			throw new System.NotImplementedException();
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			throw new System.NotImplementedException();
		}
	}
}
