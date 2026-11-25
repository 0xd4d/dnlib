// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// <see cref="VTableFixupWriteTests"/> with many vtables: <see cref="NativeModuleWriter"/> writes the
	/// vtables over the original ones and checks for every vtable that it's in space that an original vtable
	/// had. The input has 300 vtables with one method each that follow each other in the file.
	/// </summary>
	public class VTableFixupManyVTablesTests {
		const int Count = 300;

		static string CreateSource() {
			var source = new StringBuilder();
			source.AppendLine(".assembly extern mscorlib { }");
			source.AppendLine(".assembly VTables { }");
			source.AppendLine(".module VTables.dll");
			// 32BITREQUIRED, and not ILONLY
			source.AppendLine(".corflags 0x00000002");
			for (int i = 1; i <= Count; i++)
				source.AppendLine($".vtfixup [1] int32 fromunmanaged at VTable{i}");
			for (int i = 1; i <= Count; i++)
				source.AppendLine($".data VTable{i} = int32(0)");
			source.AppendLine(".data DataAfterTheVTables = bytearray (DE AD BE EF 01 02 03 04)");
			source.AppendLine(".class public abstract sealed auto ansi Functions extends [mscorlib]System.Object {");
			source.AppendLine("	.field public static int64 Data at DataAfterTheVTables");
			// .vtentry: the number of the vtable and of the slot in it
			for (int i = 1; i <= Count; i++)
				source.AppendLine($"	.method public static int32 Method{i}() cil managed {{ .vtentry {i} : 1 ldc.i4.1 ret }}");
			source.AppendLine("	.method public static int32 NotInAVTable() cil managed { ldc.i4.0 ret }");
			source.AppendLine("}");
			return source.ToString();
		}
		static readonly Lazy<byte[]> image = new Lazy<byte[]>(() => IlAssembler.Assemble(CreateSource()).Image);

		// The numbers of the methods of every vtable, eg. "1,2 | 3" for Method1 and Method2 in the first vtable
		// and Method3 in the second one
		static string GetVTables(ModuleDefMD module) =>
			string.Join(" | ", module.VTableFixups!.VTables.Select(vtable => string.Join(",", vtable.Methods.Select(a => a!.Name.String.Replace("Method", "")))));

		// Changes the vtables, writes the file and returns the vtables of the written file
		static string ChangeAndWrite(Action<ModuleDefMD> change) {
			var stream = new MemoryStream();
			using (var module = ModuleDefMD.Load(image.Value)) {
				change(module);
				module.NativeWrite(stream, new NativeModuleWriterOptions(module, optimizeImageSize: false));
			}
			using (var written = ModuleDefMD.Load(stream.ToArray())) {
				// The data after the vtables wasn't overwritten
				Assert.Equal(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 1, 2, 3, 4 }, written.Find("Functions", isReflectionName: true)!.FindField("Data")!.InitialValue);
				return GetVTables(written);
			}
		}

		static string Numbers(int first, int count) => string.Join(",", Enumerable.Range(first, count));

		[Fact]
		public void InputHasVTablesThatFollowEachOther() {
			using (var module = ModuleDefMD.Load(image.Value)) {
				var vtables = module.VTableFixups!.VTables;
				Assert.Equal(Count, vtables.Count);
				Assert.Equal(string.Join(" | ", Enumerable.Range(1, Count)), GetVTables(module));
				for (int i = 1; i < Count; i++)
					Assert.Equal((uint)vtables[i - 1].RVA + 4, (uint)vtables[i].RVA);
			}
		}

		[Fact]
		public void UnchangedVTablesAreWritten() =>
			Assert.Equal(string.Join(" | ", Enumerable.Range(1, Count)), ChangeAndWrite(module => { }));

		// Vtable 1 gets the method of vtable 2, vtable 3 the one of vtable 4, ...: every vtable that is left ends
		// where the vtable after it ended
		[Fact]
		public void EverySecondVTableCanBeMergedIntoTheOneBeforeIt() {
			var vtables = ChangeAndWrite(module => {
				var all = module.VTableFixups!.VTables;
				for (int i = Count - 2; i >= 0; i -= 2) {
					all[i].Methods.Add(all[i + 1].Methods[0]);
					all.RemoveAt(i + 1);
				}
			});

			Assert.Equal(string.Join(" | ", Enumerable.Range(0, Count / 2).Select(a => Numbers(a * 2 + 1, 2))), vtables);
		}

		// One vtable that has the space of all original ones
		[Fact]
		public void AllVTablesCanBeMergedIntoTheFirstOne() {
			var vtables = ChangeAndWrite(module => {
				var all = module.VTableFixups!.VTables;
				while (all.Count > 1) {
					all[0].Methods.Add(all[1].Methods[0]);
					all.RemoveAt(1);
				}
			});

			Assert.Equal(Numbers(1, Count), vtables);
		}

		// The last 100 vtables become one vtable, the others stay as they are
		[Fact]
		public void TheLastVTablesCanBeMerged() {
			var vtables = ChangeAndWrite(module => {
				var all = module.VTableFixups!.VTables;
				while (all.Count > Count - 99) {
					all[Count - 100].Methods.Add(all[Count - 99].Methods[0]);
					all.RemoveAt(Count - 99);
				}
			});

			Assert.Equal(string.Join(" | ", Enumerable.Range(1, Count - 100)) + " | " + Numbers(Count - 99, 100), vtables);
		}

		// The vtables are written in the order of the list: here the last original one is first
		[Fact]
		public void VTablesCanBeInAnotherOrder() {
			var vtables = ChangeAndWrite(module => {
				var all = module.VTableFixups!.VTables;
				var reversed = all.Reverse().ToArray();
				all.Clear();
				foreach (var vtable in reversed)
					all.Add(vtable);
			});

			Assert.Equal(string.Join(" | ", Enumerable.Range(1, Count).Reverse()), vtables);
		}

		// There's other data after the last vtable
		[Theory]
		[InlineData(0)]
		[InlineData(150)]
		[InlineData(299)]
		public void OneMethodTooManyIsAnError(int vtableIndex) {
			var exception = Assert.Throws<ModuleWriterException>(() => ChangeAndWrite(module => {
				var all = module.VTableFixups!.VTables;
				// All vtables from this one on are one vtable, and it gets one more method
				while (all.Count > vtableIndex + 1) {
					all[vtableIndex].Methods.Add(all[vtableIndex + 1].Methods[0]);
					all.RemoveAt(vtableIndex + 1);
				}
				all[vtableIndex].Methods.Add(module.Find("Functions", isReflectionName: true)!.FindMethod("NotInAVTable"));
			}));

			Assert.Contains("doesn't fit in an original vtable location", exception.Message);
		}
	}
}
