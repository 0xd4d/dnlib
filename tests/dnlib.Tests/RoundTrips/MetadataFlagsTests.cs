// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using dnlib.TestUtils.Dumping;
using Xunit;

namespace dnlib.Tests.RoundTrips {
	/// <summary>
	/// The writer options that keep parts of the original metadata (<see cref="MetadataFlags"/>): each flag
	/// alone must keep what it says and still write a working module. The other round trip tests use no flag
	/// or all of them.
	/// </summary>
	public class MetadataFlagsTests {
		static byte[] Image => Fixtures.Fixtures.Features.Image;

		static byte[] Write(ModuleDef module, MetadataFlags flags) {
			var options = new ModuleWriterOptions(module);
			options.MetadataOptions.Flags |= flags;
			var stream = new MemoryStream();
			module.Write(stream, options);
			return stream.ToArray();
		}

		// If the fields, methods or parameters keep their row numbers but the types don't, the rows aren't in
		// the order of their types anymore. The writer then adds a pointer table (FieldPtr, MethodPtr, ParamPtr)
		// and the metadata is "uncompressed" (#-). The CLR loads that, Mono doesn't.
		static void RunUnlessMonoCanNotLoadIt(byte[] written) {
			bool isMono = Type.GetType("Mono.Runtime") is not null;
			using (var module = ModuleDefMD.Load(written)) {
				if (isMono && !module.Metadata.IsCompressed)
					return;
			}
			Assert.Equal(0, AssemblyRunner.RunEntryPoint(written));
		}

		// The tokens of the rows of a table: the objects are found in the same order in both modules
		static List<MDToken> GetTokens(ModuleDefMD module, Table table) {
			IEnumerable<IMDTokenProvider> items;
			var types = module.GetTypes().ToList();
			var bodies = types.SelectMany(a => a.Methods).Where(a => a.Body is not null).Select(a => a.Body!).ToList();
			var operands = bodies.SelectMany(a => a.Instructions).Select(a => a.Operand).ToList();
			switch (table) {
			case Table.TypeDef: items = types; break;
			case Table.Field: items = types.SelectMany(a => a.Fields); break;
			case Table.Method: items = types.SelectMany(a => a.Methods); break;
			case Table.Param: items = types.SelectMany(a => a.Methods).SelectMany(a => a.ParamDefs); break;
			case Table.Event: items = types.SelectMany(a => a.Events); break;
			case Table.Property: items = types.SelectMany(a => a.Properties); break;
			case Table.TypeRef: items = operands.OfType<TypeRef>(); break;
			case Table.MemberRef: items = operands.OfType<MemberRef>(); break;
			case Table.TypeSpec: items = operands.OfType<TypeSpec>(); break;
			case Table.MethodSpec: items = operands.OfType<MethodSpec>(); break;
			default: throw new ArgumentException("Unknown table: " + table);
			}
			return items.Select(a => a.MDToken).ToList();
		}

		[Theory]
		[InlineData(MetadataFlags.PreserveTypeRefRids, Table.TypeRef)]
		[InlineData(MetadataFlags.PreserveTypeDefRids, Table.TypeDef)]
		[InlineData(MetadataFlags.PreserveFieldRids, Table.Field)]
		[InlineData(MetadataFlags.PreserveMethodRids, Table.Method)]
		[InlineData(MetadataFlags.PreserveParamRids, Table.Param)]
		[InlineData(MetadataFlags.PreserveMemberRefRids, Table.MemberRef)]
		[InlineData(MetadataFlags.PreserveEventRids, Table.Event)]
		[InlineData(MetadataFlags.PreservePropertyRids, Table.Property)]
		[InlineData(MetadataFlags.PreserveTypeSpecRids, Table.TypeSpec)]
		[InlineData(MetadataFlags.PreserveMethodSpecRids, Table.MethodSpec)]
		public void FlagKeepsTheTokensOfItsTable(MetadataFlags flag, Table table) {
			using (var original = ModuleDefMD.Load(Image)) {
				var expectedTokens = GetTokens(original, table);
				Assert.NotEmpty(expectedTokens);

				var written = Write(original, flag);

				using (var writtenModule = ModuleDefMD.Load(written)) {
					ModuleComparer.AssertEqual(original, writtenModule);
					Assert.Equal(expectedTokens, GetTokens(writtenModule, table));
				}
				RunUnlessMonoCanNotLoadIt(written);
			}
		}

		// The offset of every string and blob in the heaps
		static List<uint> GetHeapOffsets(ModuleDefMD module, Table table, string column) {
			var mdTable = module.TablesStream.Get(table)!;
			var columnInfo = mdTable.Columns.Single(a => a.Name == column);
			var offsets = new List<uint>();
			for (uint rid = 1; rid <= mdTable.Rows; rid++) {
				Assert.True(module.TablesStream.TryReadColumn(mdTable, rid, columnInfo, out uint offset));
				offsets.Add(offset);
			}
			return offsets;
		}

		static bool StartsWith(byte[] data, byte[] start) => data.Length >= start.Length && data.Take(start.Length).SequenceEqual(start);

		// The original #Strings heap is kept and new strings are added after it. Nothing is added here: every
		// name is in the original heap, also a name that the compiler stored as the end of a longer name
		// (eg. "Value" in "get_Value").
		[Fact]
		public void PreserveStringsOffsetsKeepsTheOriginalHeap() {
			using (var original = ModuleDefMD.Load(Image)) {
				var written = Write(original, MetadataFlags.PreserveStringsOffsets);

				using (var writtenModule = ModuleDefMD.Load(written)) {
					Assert.Equal(original.StringsStream.CreateReader().ToArray(), writtenModule.StringsStream.CreateReader().ToArray());
					ModuleComparer.AssertEqual(original, writtenModule);
				}
				RunUnlessMonoCanNotLoadIt(written);
			}
		}

		// If the rows keep their place too, every row has the offset it had
		[Fact]
		public void PreserveStringsOffsetsKeepsTheOffsetOfEveryName() {
			using (var original = ModuleDefMD.Load(Image)) {
				var written = Write(original, MetadataFlags.PreserveStringsOffsets | MetadataFlags.PreserveRids);

				using (var writtenModule = ModuleDefMD.Load(written)) {
					foreach (var table in new[] { Table.Module, Table.TypeRef, Table.TypeDef, Table.Field, Table.Method, Table.Param, Table.MemberRef, Table.Property })
						Assert.Equal(GetHeapOffsets(original, table, "Name"), GetHeapOffsets(writtenModule, table, "Name"));
					Assert.Equal(GetHeapOffsets(original, Table.TypeDef, "Namespace"), GetHeapOffsets(writtenModule, Table.TypeDef, "Namespace"));
				}
			}
		}

		// A new name that's the end of a name in the original heap uses the bytes of that name, a name that
		// isn't is added after the original heap.
		[Fact]
		public void PreserveStringsOffsetsUsesTheEndOfAnOriginalString() {
			using (var original = ModuleDefMD.Load(Image)) {
				var originalHeap = original.StringsStream.CreateReader().ToArray();
				string longName = original.Types.Select(type => type.Name.String).OrderByDescending(name => name.Length).First();
				string endOfName = longName.Substring(1);
				Assert.DoesNotContain(original.GetTypes(), type => type.Name == endOfName);
				original.Types.Add(new TypeDefUser("", endOfName, original.CorLibTypes.Object.TypeDefOrRef));

				var withEndOfName = Write(original, MetadataFlags.PreserveStringsOffsets);
				using (var writtenModule = ModuleDefMD.Load(withEndOfName)) {
					Assert.Equal(originalHeap, writtenModule.StringsStream.CreateReader().ToArray());
					Assert.NotNull(writtenModule.Find(endOfName, isReflectionName: false));
				}

				original.Types.Add(new TypeDefUser("", "NotInTheOriginalHeap", original.CorLibTypes.Object.TypeDefOrRef));
				var withNewName = Write(original, MetadataFlags.PreserveStringsOffsets);
				using (var writtenModule = ModuleDefMD.Load(withNewName)) {
					var heap = writtenModule.StringsStream.CreateReader().ToArray();
					Assert.True(StartsWith(heap, originalHeap));
					// The new name, its terminating zero and the padding to a multiple of 4 bytes
					var added = heap.Skip(originalHeap.Length).ToArray();
					Assert.Equal("NotInTheOriginalHeap", System.Text.Encoding.UTF8.GetString(added).TrimEnd('\0'));
					Assert.InRange(added.Length, "NotInTheOriginalHeap".Length + 1, "NotInTheOriginalHeap".Length + 4);
					Assert.NotNull(writtenModule.Find("NotInTheOriginalHeap", isReflectionName: false));
				}
			}
		}

		[Fact]
		public void PreserveBlobOffsetsKeepsTheOriginalHeap() {
			using (var original = ModuleDefMD.Load(Image)) {
				var written = Write(original, MetadataFlags.PreserveBlobOffsets);

				using (var writtenModule = ModuleDefMD.Load(written)) {
					Assert.True(StartsWith(writtenModule.BlobStream.CreateReader().ToArray(), original.BlobStream.CreateReader().ToArray()));
					ModuleComparer.AssertEqual(original, writtenModule);
				}
				RunUnlessMonoCanNotLoadIt(written);
			}
		}

		// A signature has the tokens of the types it uses. If all tokens are kept, every signature is found in
		// the original heap and keeps its offset.
		[Fact]
		public void SignaturesKeepTheirOffsetsIfAllTokensAreKept() {
			using (var original = ModuleDefMD.Load(Image)) {
				var written = Write(original, MetadataFlags.PreserveAll);

				using (var writtenModule = ModuleDefMD.Load(written)) {
					Assert.Equal(GetHeapOffsets(original, Table.Method, "Signature"), GetHeapOffsets(writtenModule, Table.Method, "Signature"));
					Assert.Equal(GetHeapOffsets(original, Table.Field, "Signature"), GetHeapOffsets(writtenModule, Table.Field, "Signature"));
					Assert.Equal(GetHeapOffsets(original, Table.MemberRef, "Signature"), GetHeapOffsets(writtenModule, Table.MemberRef, "Signature"));
					Assert.Equal(original.BlobStream.StreamLength, writtenModule.BlobStream.StreamLength);
				}
			}
		}

		// The token of a string in the code is its offset in the #US heap
		[Fact]
		public void PreserveUSOffsetsKeepsTheWholeUserStringsHeap() {
			using (var original = ModuleDefMD.Load(Image)) {
				var written = Write(original, MetadataFlags.PreserveUSOffsets);

				using (var writtenModule = ModuleDefMD.Load(written))
					Assert.Equal(original.USStream.CreateReader().ToArray(), writtenModule.USStream.CreateReader().ToArray());
				RunUnlessMonoCanNotLoadIt(written);
			}
		}

		[Theory]
		[InlineData(MetadataFlags.KeepOldMaxStack)]
		[InlineData(MetadataFlags.PreserveExtraSignatureData)]
		[InlineData(MetadataFlags.AlwaysCreateGuidHeap | MetadataFlags.AlwaysCreateStringsHeap | MetadataFlags.AlwaysCreateUSHeap | MetadataFlags.AlwaysCreateBlobHeap)]
		[InlineData(MetadataFlags.RoslynSortInterfaceImpl)]
		[InlineData(MetadataFlags.NoMethodBodies)]
		[InlineData(MetadataFlags.NoDotNetResources)]
		[InlineData(MetadataFlags.NoFieldData)]
		[InlineData(MetadataFlags.OptimizeCustomAttributeSerializedTypeNames)]
		public void ModuleIsWrittenWithTheFlag(MetadataFlags flag) {
			using (var original = ModuleDefMD.Load(Image)) {
				var written = Write(original, flag);

				using (var writtenModule = ModuleDefMD.Load(written)) {
					Assert.Equal(original.GetTypes().Count(), writtenModule.GetTypes().Count());
					Assert.Equal(original.EntryPoint!.FullName, writtenModule.EntryPoint!.FullName);
				}
			}
		}

		// A removed member leaves a gap: the writer fills it with an unused row so the other rows keep their numbers
		[Fact]
		public void RowsKeepTheirNumbersIfAMemberIsRemoved() {
			// Add a method that nothing uses to the fixture: every method of the fixture is called
			byte[] imageWithUnusedMethod;
			using (var module = ModuleDefMD.Load(Image)) {
				var unused = new MethodDefUser("Unused", MethodSig.CreateStatic(module.CorLibTypes.Void), MethodImplAttributes.IL, MethodAttributes.Public | MethodAttributes.Static);
				unused.Body = new dnlib.DotNet.Emit.CilBody();
				unused.Body.Instructions.Add(dnlib.DotNet.Emit.OpCodes.Ret.ToInstruction());
				// Not the last method of the module: the methods after it would get other numbers
				module.Find("Features.Code", isReflectionName: true)!.Methods.Insert(0, unused);
				imageWithUnusedMethod = Write(module, 0);
			}

			using (var original = ModuleDefMD.Load(imageWithUnusedMethod)) {
				var type = original.Find("Features.Code", isReflectionName: true)!;
				var removedMethod = type.FindMethod("Unused")!;
				var remainingTokens = original.GetTypes().SelectMany(a => a.Methods).Where(a => a != removedMethod).Select(a => (a.FullName, a.MDToken)).ToList();
				Assert.Contains(remainingTokens, a => a.MDToken.Rid > removedMethod.Rid);
				type.Remove(removedMethod);

				var written = Write(original, MetadataFlags.PreserveAll);

				using (var writtenModule = ModuleDefMD.Load(written)) {
					var writtenTokens = writtenModule.GetTypes().SelectMany(a => a.Methods).ToDictionary(a => a.FullName, a => a.MDToken);
					foreach (var (fullName, token) in remainingTokens)
						Assert.Equal(token, writtenTokens[fullName]);
					Assert.Equal(original.TablesStream.MethodTable.Rows, writtenModule.TablesStream.MethodTable.Rows);
				}
				RunUnlessMonoCanNotLoadIt(written);
			}
		}
	}
}
