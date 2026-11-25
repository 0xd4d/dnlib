// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using dnlib.DotNet;
using dnlib.DotNet.Pdb;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Pdb {
	/// <summary>
	/// What dnlib reads from a portable PDB: the special cases that <see cref="PortablePdbRoundTripTests"/>
	/// doesn't check, and PDB files that the C# compiler doesn't create.
	/// </summary>
	public class PortablePdbReaderTests {
		static Dictionary<string, PdbConstant> ReadConstants(ModuleDef module) =>
			DebugInfoSource.GetRootScope(module, "Constants").Constants.ToDictionary(a => a.Name!);

		[Fact]
		public void ConstantsHaveTheirValues() {
			using (var module = DebugInfoSource.Load()) {
				var constants = ReadConstants(module);

				Assert.Null(constants["NoArray"].Value);
				Assert.Null(constants["NoList"].Value);
				Assert.Null(constants["NoString"].Value);
				Assert.Equal((byte)1, constants["NestedInGeneric"].Value);
				Assert.Equal(5, constants["FromAnotherAssembly"].Value);
				Assert.Equal(12.5m, constants["Price"].Value);
				Assert.Equal('x', constants["Letter"].Value);
			}
		}

		[Fact]
		public void ConstantsHaveTheirTypes() {
			using (var module = DebugInfoSource.Load()) {
				var constants = ReadConstants(module);

				// Fixed in e23ef7b1: the compiler stores an array type as a TypeSpec, and the constant's type
				// was a class with that TypeSpec as its "name"
				Assert.IsType<SZArraySig>(constants["NoArray"].Type);
				Assert.Equal("System.Int32[]", constants["NoArray"].Type!.FullName);
				Assert.IsType<GenericInstSig>(constants["NoList"].Type);
				Assert.Equal("System.Collections.Generic.List`1<System.String>", constants["NoList"].Type!.FullName);
				Assert.Equal(ElementType.String, constants["NoString"].Type!.ElementType);
				Assert.Equal("Generic`1/Kind<System.Int32>", constants["NestedInGeneric"].Type!.FullName);
				Assert.Equal("System.Decimal", constants["Price"].Type!.FullName);
				Assert.Equal(ElementType.Char, constants["Letter"].Type!.ElementType);
			}
		}

		// Fixed in 2acb8ac1: the reader had to find the enum's assembly to know that it's an enum. The file only
		// says that the type is System.DayOfWeek and that the value is an Int32.
		[Fact]
		public void EnumConstantIsReadWithoutTheEnumsAssembly() {
			using (var module = DebugInfoSource.Load()) {
				// Nothing can be resolved: there's no assembly resolver
				Assert.Null(module.Context.AssemblyResolver.Resolve(module.CorLibTypes.AssemblyRef, module));

				var constant = ReadConstants(module)["FromAnotherAssembly"];

				var type = Assert.IsType<ValueTypeSig>(constant.Type);
				Assert.Equal("System.DayOfWeek", type.FullName);
				Assert.Equal(5, constant.Value);
			}
		}

		// The indexes of the flags that are true
		static int[] GetDynamicParts(PdbLocal local) {
			var flags = Assert.IsType<PdbDynamicLocalVariablesCustomDebugInfo>(Assert.Single(local.CustomDebugInfos)).Flags;
			return Enumerable.Range(0, flags.Length).Where(a => flags[a]).ToArray();
		}

		[Fact]
		public void DynamicLocalsAndTupleElementNamesAreRead() {
			using (var module = DebugInfoSource.Load()) {
				var anything = DebugInfoSource.GetLocal(module, "SpecialLocals", "anything");
				var map = DebugInfoSource.GetLocal(module, "SpecialLocals", "map");
				var pair = DebugInfoSource.GetLocal(module, "SpecialLocals", "pair");

				// One flag for every part of the type: 'dynamic' is the type itself, or the third part of
				// Dictionary, String, Object. The flags are stored as bits, so there are 8 flags for every byte.
				Assert.Equal(new[] { 0 }, GetDynamicParts(anything));
				Assert.Equal(new[] { 2 }, GetDynamicParts(map));
				Assert.Equal(new[] { "first", "second" }, Assert.IsType<PortablePdbTupleElementNamesCustomDebugInfo>(Assert.Single(pair.CustomDebugInfos)).Names);
			}
		}

		// The kinds of custom debug info of all methods of the module
		internal static HashSet<PdbCustomDebugInfoKind> GetMethodCustomDebugInfoKinds(ModuleDef module) =>
			new HashSet<PdbCustomDebugInfoKind>(module.GetTypes().SelectMany(a => a.Methods).SelectMany(a => a.CustomDebugInfos).Select(a => a.Kind));

		// The compiler writes this for a debug build, so the debugger can change the code of a running program
		[Fact]
		public void EditAndContinueDebugInfoIsRead() {
			using (var module = DebugInfoSource.Load()) {
				var kinds = GetMethodCustomDebugInfoKinds(module);

				Assert.Contains(PdbCustomDebugInfoKind.EditAndContinueLocalSlotMap, kinds);
				Assert.Contains(PdbCustomDebugInfoKind.EditAndContinueLambdaMap, kinds);
				Assert.Contains(PdbCustomDebugInfoKind.EditAndContinueStateMachineStateMap, kinds);
				Assert.Contains(PdbCustomDebugInfoKind.AsyncMethod, kinds);
				Assert.Contains(PdbCustomDebugInfoKind.StateMachineHoistedLocalScopes, kinds);

				var lambda = DebugInfoSource.GetMethod(module, "Lambda");
				Assert.Contains(lambda.CustomDebugInfos, a => a is PdbEditAndContinueLambdaMapCustomDebugInfo info && info.Data.Length != 0);
				Assert.Contains(lambda.CustomDebugInfos, a => a is PdbEditAndContinueLocalSlotMapCustomDebugInfo info && info.Data.Length != 0);
			}
		}

		// The rows of a LocalScope table for the SRM-created PDB files below
		sealed class ScopeRow {
			public string MethodName = string.Empty;
			public int StartOffset;
			public int Length;
			// The name of the scope's only local variable (its index is 0), or null if it has none
			public string? LocalName;
		}

		// Creates a portable PDB for DebugInfoSource's assembly that only has LocalScope and LocalVariable rows.
		// System.Reflection.Metadata writes it, so it can have what the C# compiler and dnlib's writer never write.
		static byte[] CreatePdbWithScopes(params ScopeRow[] scopes) {
			var compiled = DebugInfoSource.Compiled;
			var builder = new MetadataBuilder();
			ImmutableArray<int> moduleRowCounts;
			using (var peReader = new PEReader(ImmutableArray.Create(compiled.Image))) {
				var metadata = peReader.GetMetadataReader();
				moduleRowCounts = Enumerable.Range(0, MetadataTokens.TableCount).Select(a => metadata.GetTableRowCount((TableIndex)a)).ToImmutableArray();

				// The rows must be sorted by method, then by start offset, then by length (longest first)
				int nextLocal = 1;
				foreach (var scope in scopes) {
					var method = metadata.MethodDefinitions.Single(a => metadata.GetString(metadata.GetMethodDefinition(a).Name) == scope.MethodName);
					builder.AddLocalScope(method, default, MetadataTokens.LocalVariableHandle(nextLocal), MetadataTokens.LocalConstantHandle(1), scope.StartOffset, scope.Length);
					if (scope.LocalName is not null) {
						builder.AddLocalVariable(LocalVariableAttributes.None, index: 0, builder.GetOrAddString(scope.LocalName));
						nextLocal++;
					}
				}
			}

			// dnlib only uses a PDB that has the ID that's stored in the PE file
			BlobContentId pdbId;
			using (var originalPdb = MetadataReaderProvider.FromPortablePdbImage(ImmutableArray.Create(compiled.Pdb)))
				pdbId = new BlobContentId(originalPdb.GetMetadataReader().DebugMetadataHeader!.Id);

			var pdbBuilder = new PortablePdbBuilder(builder, moduleRowCounts, entryPoint: default, idProvider: _ => pdbId);
			var pdb = new BlobBuilder();
			pdbBuilder.Serialize(pdb);
			return pdb.ToArray();
		}

		static ModuleDefMD LoadWithPdb(byte[] pdb) => RoundTrip.LoadWithPdb(DebugInfoSource.Compiled.Image, pdb);

		// Fixed in 0e625737: the MethodDebugInformation table is empty if no method has sequence points. The
		// methods' scopes weren't read then.
		[Fact]
		public void ScopesAreReadIfNoMethodHasSequencePoints() {
			var pdb = CreatePdbWithScopes(new ScopeRow { MethodName = "Sum", StartOffset = 0, Length = 10, LocalName = "onlyLocal" });

			using (var module = LoadWithPdb(pdb)) {
				var body = DebugInfoSource.GetMethod(module, "Sum").Body!;
				Assert.DoesNotContain(body.Instructions, a => a.SequencePoint is not null);
				var local = Assert.Single(body.PdbMethod!.Scope!.Variables);
				Assert.Equal("onlyLocal", local.Name);
				Assert.Same(body.Variables[0], local.Local);
			}
		}

		// Fixed in 6cbf6d04: a method's first LocalScope row was its root scope, and the other rows were only
		// read if they were inside it. Now a root scope is added if the first row doesn't contain all others.
		[Fact]
		public void MethodWithSeveralTopLevelScopesGetsARootScope() {
			var pdb = CreatePdbWithScopes(
				new ScopeRow { MethodName = "Sum", StartOffset = 0, Length = 4, LocalName = "first" },
				new ScopeRow { MethodName = "Sum", StartOffset = 4, Length = 6, LocalName = "second" });

			using (var module = LoadWithPdb(pdb)) {
				var body = DebugInfoSource.GetMethod(module, "Sum").Body!;
				var rootScope = body.PdbMethod!.Scope!;

				Assert.Empty(rootScope.Variables);
				Assert.Equal(0U, rootScope.Start!.Offset);
				Assert.Equal(10U, rootScope.End!.Offset);
				Assert.Equal(new[] { "first", "second" }, rootScope.Scopes.Select(a => Assert.Single(a.Variables).Name));
				Assert.Equal(new uint[] { 0, 4 }, rootScope.Scopes.Select(a => a.Start!.Offset));
				Assert.Equal(new uint[] { 4, 10 }, rootScope.Scopes.Select(a => a.End!.Offset));
			}
		}

		[Fact]
		public void MethodWithOneTopLevelScopeGetsNoExtraRootScope() {
			var pdb = CreatePdbWithScopes(
				new ScopeRow { MethodName = "Sum", StartOffset = 0, Length = 10, LocalName = "outer" },
				new ScopeRow { MethodName = "Sum", StartOffset = 4, Length = 6, LocalName = "inner" });

			using (var module = LoadWithPdb(pdb)) {
				var rootScope = DebugInfoSource.GetRootScope(module, "Sum");

				Assert.Equal("outer", Assert.Single(rootScope.Variables).Name);
				Assert.Equal("inner", Assert.Single(Assert.Single(rootScope.Scopes).Variables).Name);
			}
		}
	}
}
