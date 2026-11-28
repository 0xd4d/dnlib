// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Pdb;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using Xunit;
using Metadata = dnlib.DotNet.Writer.Metadata;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// While a module is written, a listener of the writer's events can ask the writer's
	/// <see cref="Metadata"/> object where something ends up in the new file: the row number (rid) of a type,
	/// a member, a PDB document etc, the written body of a method, and the bytes of a resource or of a
	/// field's initial value. Tools that patch the written file need this. The tests ask at the last event
	/// and compare the answers with the file that was written.
	/// </summary>
	public class WriterMetadataLookupTests {
		// Loads the fixture with its PDB, writes it with a portable PDB and calls 'ask' at the writer's last event
		static CompiledAssembly Write(Action<ModuleDefMD, Metadata> ask, bool writePdb = true, Action<ModuleDefMD>? change = null) {
			var fixture = Fixtures.Fixtures.Features;
			using (var module = RoundTrip.LoadWithPdb(fixture.Image, fixture.Pdb!)) {
				change?.Invoke(module);
				var options = new ModuleWriterOptions(module) { WritePdb = writePdb, PdbStream = new MemoryStream(), PdbFileName = "written.pdb" };
				options.WriterEvent += (sender, e) => {
					if (e.Event == ModuleWriterEvent.End)
						ask(module, e.Writer.Metadata);
				};
				var image = new MemoryStream();
				module.Write(image, options);
				return new CompiledAssembly(image.ToArray(), writePdb ? ((MemoryStream)options.PdbStream).ToArray() : null);
			}
		}

		[Fact]
		public void RidsOfTypesAndMembersAreTheirRowsInTheWrittenFile() {
			// "TypeDef 5" -> the full name of the object the writer said has this row
			var expected = new Dictionary<string, string>();
			var written = Write((module, metadata) => {
				void Add(Table table, uint rid, IFullName item) {
					Assert.NotEqual(0U, rid);
					expected.Add($"{table} {rid}", item.FullName);
				}
				foreach (var type in module.GetTypes()) {
					Add(Table.TypeDef, metadata.GetRid(type), type);
					foreach (var field in type.Fields)
						Add(Table.Field, metadata.GetRid(field), field);
					foreach (var method in type.Methods)
						Add(Table.Method, metadata.GetRid(method), method);
					foreach (var property in type.Properties)
						Add(Table.Property, metadata.GetRid(property), property);
					foreach (var eventDef in type.Events)
						Add(Table.Event, metadata.GetRid(eventDef), eventDef);
				}
				Assert.Equal(1U, metadata.GetRid(module));
				Assert.Equal(1U, metadata.GetRid(module.Assembly!));
			});

			Assert.True(expected.Count > 100);
			using (var writtenModule = ModuleDefMD.Load(written.Image)) {
				foreach (var item in expected) {
					var parts = item.Key.Split(' ');
					var table = (Table)Enum.Parse(typeof(Table), parts[0]);
					var resolved = Assert.IsAssignableFrom<IFullName>(writtenModule.ResolveToken(new MDToken(table, uint.Parse(parts[1]))));
					Assert.Equal(item.Value, resolved.FullName);
				}
			}
		}

		[Fact]
		public void RidsOfOtherRows() {
			var parameters = new Dictionary<uint, string>();
			var genericParameters = new Dictionary<uint, string>();
			var assemblyReferences = new Dictionary<uint, string>();
			int interfaces = 0, constraints = 0;
			var written = Write((module, metadata) => {
				foreach (var type in module.GetTypes()) {
					foreach (var interfaceImpl in type.Interfaces) {
						Assert.NotEqual(0U, metadata.GetRid(interfaceImpl));
						interfaces++;
					}
					foreach (var owner in type.Methods.Cast<ITypeOrMethodDef>().Concat(new ITypeOrMethodDef[] { type })) {
						foreach (var genericParameter in owner.GenericParameters) {
							genericParameters.Add(metadata.GetRid(genericParameter), genericParameter.Name);
							foreach (var constraint in genericParameter.GenericParamConstraints) {
								Assert.NotEqual(0U, metadata.GetRid(constraint));
								constraints++;
							}
						}
					}
					foreach (var parameter in type.Methods.SelectMany(a => a.ParamDefs))
						parameters.Add(metadata.GetRid(parameter), parameter.Name);
				}
				foreach (var assemblyRef in module.GetAssemblyRefs())
					assemblyReferences[metadata.GetRid(assemblyRef)] = assemblyRef.Name;
				// Something the module doesn't have: for a type or a member, that's an error
				Assert.Equal(0U, metadata.GetRid(new AssemblyRefUser("NotInTheModule")));
				var exception = Assert.Throws<ModuleWriterException>(() => metadata.GetRid(new TypeDefUser("NotInTheModule")));
				Assert.Contains("is not defined in this module", exception.Message);
			});

			Assert.True(interfaces > 0 && constraints > 0);
			Assert.DoesNotContain(0U, parameters.Keys);
			using (var writtenModule = ModuleDefMD.Load(written.Image)) {
				Assert.All(parameters, a => Assert.Equal(a.Value, writtenModule.ResolveParam(a.Key)!.Name.String));
				Assert.All(genericParameters, a => Assert.Equal(a.Value, writtenModule.ResolveGenericParam(a.Key)!.Name.String));
				Assert.All(assemblyReferences, a => Assert.Equal(a.Value, writtenModule.ResolveAssemblyRef(a.Key)!.Name.String));
				Assert.Equal((int)writtenModule.TablesStream.InterfaceImplTable.Rows, interfaces);
				Assert.Equal((int)writtenModule.TablesStream.GenericParamConstraintTable.Rows, constraints);
			}
		}

		// The rows that belong to a type or a member: constants, custom attributes, marshal info, layouts, the
		// event and property maps, P/Invoke info, initial values, and the rows of the assembly (files, exported
		// types, resources). Each item has its own row and the written row has the same values.
		[Fact]
		public void RidsOfRowsThatBelongToAnotherRow() {
			var rids = new Dictionary<Table, List<uint>>();
			var constants = new Dictionary<uint, object?>();
			var moduleRefs = new Dictionary<uint, string>();
			var exportedTypes = new Dictionary<uint, string>();
			var resources = new Dictionary<uint, string>();
			var implMaps = new Dictionary<uint, string>();
			var classLayouts = new Dictionary<uint, (ushort, uint)>();
			void Add(Table table, uint rid) {
				Assert.NotEqual(0U, rid);
				if (!rids.TryGetValue(table, out var list))
					rids.Add(table, list = new List<uint>());
				list.Add(rid);
			}
			var written = Write((module, metadata) => {
				foreach (var type in module.GetTypes()) {
					foreach (var ca in type.CustomAttributes)
						Add(Table.CustomAttribute, metadata.GetCustomAttributeRid(ca));
					if (type.ClassLayout is not null) {
						uint rid = metadata.GetClassLayoutRid(type);
						Add(Table.ClassLayout, rid);
						classLayouts.Add(rid, (type.ClassLayout.PackingSize, type.ClassLayout.ClassSize));
					}
					else
						Assert.Equal(0U, metadata.GetClassLayoutRid(type));
					if (type.DeclaringType is not null)
						Add(Table.NestedClass, metadata.GetNestedClassRid(type));
					else
						Assert.Equal(0U, metadata.GetNestedClassRid(type));
					if (type.Events.Count > 0)
						Add(Table.EventMap, metadata.GetEventMapRid(type));
					if (type.Properties.Count > 0)
						Add(Table.PropertyMap, metadata.GetPropertyMapRid(type));
					foreach (var ds in type.DeclSecurities)
						Add(Table.DeclSecurity, metadata.GetRid(ds));
					foreach (var field in type.Fields) {
						if (field.Constant is not null) {
							uint rid = metadata.GetConstantRid(field);
							Add(Table.Constant, rid);
							constants.Add(rid, field.Constant.Value);
						}
						if (field.MarshalType is not null)
							Add(Table.FieldMarshal, metadata.GetFieldMarshalRid(field));
						if (field.FieldOffset is not null)
							Add(Table.FieldLayout, metadata.GetFieldLayoutRid(field));
						else
							Assert.Equal(0U, metadata.GetFieldLayoutRid(field));
						if (field.HasFieldRVA)
							Add(Table.FieldRVA, metadata.GetFieldRVARid(field));
					}
					foreach (var method in type.Methods) {
						if (method.SemanticsAttributes != 0)
							Add(Table.MethodSemantics, metadata.GetMethodSemanticsRid(method));
						if (method.ImplMap is not null) {
							uint rid = metadata.GetImplMapRid(method);
							Add(Table.ImplMap, rid);
							implMaps.Add(rid, method.ImplMap.Name);
							uint moduleRefRid = metadata.GetRid(method.ImplMap.Module!);
							Assert.NotEqual(0U, moduleRefRid);
							moduleRefs[moduleRefRid] = method.ImplMap.Module!.Name;
						}
						else
							Assert.Equal(0U, metadata.GetImplMapRid(method));
						foreach (var parameter in method.ParamDefs) {
							if (parameter.Constant is not null)
								Add(Table.Constant, metadata.GetConstantRid(parameter));
							if (parameter.MarshalType is not null)
								Add(Table.FieldMarshal, metadata.GetFieldMarshalRid(parameter));
						}
					}
				}
				foreach (var exportedType in module.ExportedTypes) {
					uint rid = metadata.GetRid(exportedType);
					Add(Table.ExportedType, rid);
					exportedTypes.Add(rid, exportedType.FullName);
				}
				foreach (var resource in module.Resources) {
					uint rid = metadata.GetManifestResourceRid(resource);
					Add(Table.ManifestResource, rid);
					resources.Add(rid, resource.Name);
				}
				// Something the module doesn't have has no row
				Assert.Equal(0U, metadata.GetRid(new ModuleRefUser(module, "NotInTheModule")));
				Assert.Equal(0U, metadata.GetRid(new FileDefUser("NotInTheModule", 0, null)));
				Assert.Equal(0U, metadata.GetRid(new ExportedTypeUser(module, 0, "Ns", "NotInTheModule", 0, null)));
				Assert.Equal(0U, metadata.GetManifestResourceRid(new EmbeddedResource("NotInTheModule", new byte[0])));
				Assert.Equal(0U, metadata.GetCustomAttributeRid(new dnlib.DotNet.CustomAttribute(null)));
				Assert.Equal(0U, metadata.GetRid(new DeclSecurityUser()));
			}, change: module => module.Resources.Add(new EmbeddedResource("Added.bin", new byte[] { 1, 2, 3 })));

			foreach (var table in new[] { Table.CustomAttribute, Table.ClassLayout, Table.NestedClass, Table.EventMap, Table.PropertyMap,
				Table.Constant, Table.FieldMarshal, Table.FieldLayout, Table.FieldRVA, Table.MethodSemantics, Table.ImplMap,
				Table.ExportedType, Table.ManifestResource })
				Assert.True(rids.ContainsKey(table), table.ToString());
			using (var writtenModule = ModuleDefMD.Load(written.Image)) {
				foreach (var item in rids) {
					var rows = writtenModule.TablesStream.Get(item.Key)!.Rows;
					Assert.All(item.Value, rid => Assert.InRange(rid, 1U, rows));
					Assert.Equal(item.Value.Count, item.Value.Distinct().Count());
				}
				Assert.All(constants, a => Assert.Equal(a.Value, writtenModule.ResolveConstant(a.Key)!.Value));
				Assert.All(moduleRefs, a => Assert.Equal(a.Value, writtenModule.ResolveModuleRef(a.Key)!.Name.String));
				Assert.All(exportedTypes, a => Assert.Equal(a.Value, writtenModule.ResolveExportedType(a.Key)!.FullName));
				Assert.All(resources, a => Assert.Equal(a.Value, writtenModule.ResolveManifestResource(a.Key)!.Name.String));
				Assert.All(implMaps, a => Assert.Equal(a.Value, writtenModule.ResolveImplMap(a.Key)!.Name.String));
				Assert.All(classLayouts, a => {
					var row = writtenModule.ResolveClassLayout(a.Key)!;
					Assert.Equal(a.Value, (row.PackingSize, row.ClassSize));
				});
			}
		}

		// The state machine and custom debug info rows of the portable PDB
		[Fact]
		public void RidsOfPdbCustomDebugInfos() {
			int stateMachines = 0, customDebugInfos = 0;
			Write((module, metadata) => {
				foreach (var method in module.GetTypes().SelectMany(a => a.Methods)) {
					foreach (var cdi in method.CustomDebugInfos) {
						if (cdi is PdbAsyncMethodCustomDebugInfo asyncMethod) {
							Assert.NotEqual(0U, metadata.GetStateMachineMethodRid(asyncMethod));
							stateMachines++;
						}
						else if (cdi is PdbIteratorMethodCustomDebugInfo iteratorMethod) {
							Assert.NotEqual(0U, metadata.GetStateMachineMethodRid(iteratorMethod));
							stateMachines++;
						}
						else if (metadata.GetCustomDebugInfoRid(cdi) != 0)
							customDebugInfos++;
					}
				}
			});

			Assert.True(stateMachines > 0);
			Assert.True(customDebugInfos > 0);
		}

		// Without a PDB, there are no PDB rows
		[Fact]
		public void PdbCustomDebugInfosHaveNoRidIfNoPdbIsWritten() {
			Write((module, metadata) => {
				var method = module.GetTypes().SelectMany(a => a.Methods).First(a => a.CustomDebugInfos.OfType<PdbIteratorMethodCustomDebugInfo>().Any());
				var iteratorMethod = method.CustomDebugInfos.OfType<PdbIteratorMethodCustomDebugInfo>().First();

				Assert.Equal(0U, metadata.GetStateMachineMethodRid(iteratorMethod));
				Assert.Equal(0U, metadata.GetStateMachineMethodRid(new PdbAsyncMethodCustomDebugInfo(method)));
				Assert.Equal(0U, metadata.GetCustomDebugInfoRid(new PdbDynamicLocalsCustomDebugInfo()));
			}, writePdb: false);
		}

		// The rows of the portable PDB's tables, checked with System.Reflection.Metadata
		[Fact]
		public void RidsOfPdbRows() {
			var documents = new Dictionary<uint, string>();
			var locals = new Dictionary<uint, string>();
			var constants = new Dictionary<uint, string>();
			var scopeRids = new List<uint>();
			var importScopeRids = new List<uint>();
			var written = Write((module, metadata) => {
				foreach (var document in module.PdbState!.Documents)
					documents.Add(metadata.GetRid(document), document.Url);
				foreach (var method in module.GetTypes().SelectMany(a => a.Methods)) {
					var rootScope = method.Body?.PdbMethod?.Scope;
					if (rootScope is null)
						continue;
					foreach (var scope in Pdb.DebugInfoSource.GetScopes(rootScope)) {
						scopeRids.Add(metadata.GetRid(scope));
						foreach (var local in scope.Variables)
							locals.Add(metadata.GetRid(local), local.Name!);
						foreach (var constant in scope.Constants)
							constants.Add(metadata.GetRid(constant), constant.Name!);
						if (scope.ImportScope is not null)
							importScopeRids.Add(metadata.GetRid(scope.ImportScope));
					}
				}
			});

			Assert.NotEmpty(locals);
			Assert.NotEmpty(constants);
			using (var provider = MetadataReaderProvider.FromPortablePdbImage(ImmutableArray.Create(written.Pdb))) {
				var reader = provider.GetMetadataReader();
				Assert.All(documents, a => Assert.Equal(a.Value, reader.GetString(reader.GetDocument(MetadataTokens.DocumentHandle((int)a.Key)).Name)));
				Assert.All(locals, a => Assert.Equal(a.Value, reader.GetString(reader.GetLocalVariable(MetadataTokens.LocalVariableHandle((int)a.Key)).Name)));
				Assert.All(constants, a => Assert.Equal(a.Value, reader.GetString(reader.GetLocalConstant(MetadataTokens.LocalConstantHandle((int)a.Key)).Name)));
				// A scope without locals, constants and imports has no row (rid 0)
				Assert.All(scopeRids, rid => Assert.InRange(rid, 0U, (uint)reader.LocalScopes.Count));
				Assert.Contains(scopeRids, rid => rid != 0);
				Assert.NotEmpty(importScopeRids);
				Assert.All(importScopeRids, rid => Assert.InRange(rid, 1U, (uint)reader.ImportScopes.Count));
			}
		}

		[Fact]
		public void PdbRowsHaveNoRidIfNoPdbIsWritten() {
			Write((module, metadata) => {
				var method = module.GetTypes().SelectMany(a => a.Methods).First(a => a.Body?.PdbMethod?.Scope is not null);
				var scope = method.Body!.PdbMethod!.Scope!;

				Assert.Equal(0U, metadata.GetRid(scope));
				Assert.Equal(0U, metadata.GetRid(module.PdbState!.Documents.First()));
				Assert.Equal(0U, metadata.GetRid(new PdbLocal(new dnlib.DotNet.Emit.Local(module.CorLibTypes.Int32), "notInTheModule", 0)));
				Assert.Equal(0U, metadata.GetRid(new PdbConstant()));
				Assert.Equal(0U, metadata.GetRid(new PdbImportScope()));
			}, writePdb: false);
		}

		// The written body of a method, the bytes of an embedded resource and of a field's initial value: each
		// has its place in the file (RVA and file offset) when the last event is raised
		[Fact]
		public void BodiesResourcesAndInitialValuesHaveTheirPlaceInTheFile() {
			var bodyOffsets = new Dictionary<string, uint>();
			var initialValues = new Dictionary<string, KeyValuePair<long, byte[]>>();
			var resources = new Dictionary<string, long>();
			var localSignatures = new Dictionary<string, uint>();
			var written = Write((module, metadata) => {
				foreach (var method in module.GetTypes().SelectMany(a => a.Methods).Where(a => a.Body is not null)) {
					var body = metadata.GetMethodBody(method)!;
					bodyOffsets.Add(method.FullName, (uint)body.RVA);
					Assert.Equal(body.LocalVarSigTok, metadata.GetLocalVarSigToken(method));
					localSignatures.Add(method.FullName, body.LocalVarSigTok);
				}
				foreach (var field in module.GetTypes().SelectMany(a => a.Fields).Where(a => a.HasFieldRVA)) {
					var chunk = metadata.GetInitialValueChunk(field)!;
					initialValues.Add(field.FullName, new KeyValuePair<long, byte[]>((long)chunk.FileOffset, field.InitialValue!));
				}
				foreach (var resource in module.Resources.OfType<EmbeddedResource>())
					resources.Add(resource.Name, (long)metadata.GetChunk(resource)!.FileOffset);

				Assert.Null(metadata.GetMethodBody(null));
				Assert.Null(metadata.GetMethodBody(new MethodDefUser("NotInTheModule")));
				Assert.Equal(0U, metadata.GetLocalVarSigToken(null));
				Assert.Null(metadata.GetInitialValueChunk(null));
				Assert.Null(metadata.GetChunk(null));
			}, change: module => module.Resources.Add(new EmbeddedResource("Added.Resource", new byte[] { 1, 2, 3, 4, 5 }, dnlib.DotNet.ManifestResourceAttributes.Public)));

			Assert.NotEmpty(initialValues);
			Assert.NotEmpty(resources);
			using (var writtenModule = ModuleDefMD.Load(written.Image)) {
				var methods = writtenModule.GetTypes().SelectMany(a => a.Methods).ToDictionary(a => a.FullName);
				Assert.All(bodyOffsets, a => Assert.Equal(a.Value, (uint)methods[a.Key].RVA));
				Assert.All(localSignatures, a => Assert.Equal(a.Value, methods[a.Key].Body!.LocalVarSigTok));
				// The bytes at the file offset the writer gave
				Assert.All(initialValues, a => Assert.Equal(a.Value.Value, written.Image.Skip((int)a.Value.Key).Take(a.Value.Value.Length)));
				foreach (var resource in writtenModule.Resources.OfType<EmbeddedResource>()) {
					// A resource is stored as its length (4 bytes) and its bytes. The chunk is the bytes.
					int offset = (int)resources[resource.Name];
					Assert.Equal((int)resource.Length, LittleEndian.ToInt32(written.Image, offset - 4));
					Assert.Equal(resource.CreateReader().ToArray(), written.Image.Skip(offset).Take((int)resource.Length));
				}
			}
		}

		// Metadata has a bool property for most flags of MetadataFlags
		[Fact]
		public void FlagPropertiesChangeTheFlagsOfTheOptions() {
			using (var module = ModuleDefMD.Load(Fixtures.Fixtures.Features.Image)) {
				var options = new ModuleWriterOptions(module);
				int count = 0;
				options.WriterEvent += (sender, e) => {
					if (e.Event != ModuleWriterEvent.End)
						return;
					var metadata = e.Writer.Metadata;
					foreach (var property in typeof(Metadata).GetProperties().Where(a => a.PropertyType == typeof(bool) && a.CanWrite)) {
						// The flag has the name of the property
						var flag = (MetadataFlags)Enum.Parse(typeof(MetadataFlags), property.Name);
						var before = options.MetadataOptions.Flags;

						property.SetValue(metadata, true);
						Assert.True((bool)property.GetValue(metadata)!, property.Name);
						Assert.Equal(before | flag, options.MetadataOptions.Flags);
						property.SetValue(metadata, false);
						Assert.False((bool)property.GetValue(metadata)!, property.Name);
						Assert.Equal(before & ~flag, options.MetadataOptions.Flags);

						options.MetadataOptions.Flags = before;
						count++;
					}
				};
				module.Write(new MemoryStream(), options);
				Assert.True(count >= 14, $"Only {count} properties were checked");
			}
		}
	}
}
