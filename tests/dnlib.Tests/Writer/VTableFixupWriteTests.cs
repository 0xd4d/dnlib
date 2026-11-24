// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using dnlib.PE;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// A file that isn't "IL only" can have vtables: arrays of method tokens that the runtime replaces with
	/// addresses of native code when it loads the file (see <see cref="NativeModuleWriterTests"/>). Native code in
	/// the file uses the addresses of the vtables, so <see cref="NativeModuleWriter"/> can't move them: it writes
	/// <see cref="ModuleDef.VTableFixups"/> over the original vtables, and it's an error if they need more space.
	///
	/// The test input has three vtables that follow each other in the file, with 2, 1 and 2 methods.
	/// </summary>
	public class VTableFixupWriteTests {
		const string Source = @"
.assembly extern mscorlib { }
.assembly VTables { }
.module VTables.dll
// 32BITREQUIRED, and not ILONLY
.corflags 0x00000002

.vtfixup [2] int32 fromunmanaged at VTable1
.vtfixup [1] int32 fromunmanaged at VTable2
.vtfixup [2] int32 fromunmanaged at VTable3
.data VTable1 = int32(0)[2]
.data VTable2 = int32(0)
.data VTable3 = int32(0)[2]
.data DataAfterTheVTables = bytearray (DE AD BE EF 01 02 03 04)

.class public abstract sealed auto ansi Functions extends [mscorlib]System.Object {
	.field public static int64 Data at DataAfterTheVTables

	// .vtentry: the number of the vtable and of the slot in it
	.method public static int32 Method1() cil managed {
		.vtentry 1 : 1
		ldc.i4.1
		ret
	}
	.method public static int32 Method2() cil managed {
		.vtentry 1 : 2
		ldc.i4.2
		ret
	}
	.method public static int32 Method3() cil managed {
		.vtentry 2 : 1
		ldc.i4.3
		ret
	}
	.method public static int32 Method4() cil managed {
		.vtentry 3 : 1
		ldc.i4.4
		ret
	}
	.method public static int32 Method5() cil managed {
		.vtentry 3 : 2
		ldc.i4.5
		ret
	}
	.method public static int32 NotInAVTable() cil managed {
		ldc.i4.6
		ret
	}
}";
		static readonly Lazy<byte[]> image = new Lazy<byte[]>(() => IlAssembler.Assemble(Source).Image);

		const string OriginalVTables = "Method1,Method2 | Method3 | Method4,Method5";

		// The names of the methods of every vtable, eg. "Method1,Method2 | Method3". An empty slot is "null", and
		// "(64-bit)" follows a vtable with 64-bit slots.
		static string GetVTables(ModuleDefMD module) {
			if (module.VTableFixups is null)
				return "no vtables";
			return string.Join(" | ", module.VTableFixups.VTables.Select(vtable =>
				string.Join(",", vtable.Methods.Select(a => a is null ? "null" : a.Name.String)) + (vtable.Is64Bit ? " (64-bit)" : "")));
		}

		static MethodDef GetMethod(ModuleDefMD module, string name) =>
			module.Find("Functions", isReflectionName: true)!.FindMethod(name)!;

		// Changes the vtables, writes the file and returns the vtables of the written file.
		// logger: gets the writer's errors, the writer throws if it's null
		static string ChangeAndWrite(Action<ModuleDefMD> change, ILogger? logger = null) =>
			ChangeAndWrite(change, logger, out _);

		static string ChangeAndWrite(Action<ModuleDefMD> change, ILogger? logger, out ImageDataDirectory vtableFixupsDirectory) {
			var stream = new MemoryStream();
			using (var module = ModuleDefMD.Load(image.Value)) {
				change(module);
				var options = new NativeModuleWriterOptions(module, optimizeImageSize: false);
				if (logger is not null)
					options.Logger = logger;
				module.NativeWrite(stream, options);
			}
			using (var written = ModuleDefMD.Load(stream.ToArray())) {
				vtableFixupsDirectory = written.Metadata.ImageCor20Header.VTableFixups;
				// The data after the vtables wasn't overwritten
				Assert.Equal(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 1, 2, 3, 4 }, written.Find("Functions", isReflectionName: true)!.FindField("Data")!.InitialValue);
				return GetVTables(written);
			}
		}

		[Fact]
		public void InputHasThreeVTablesThatFollowEachOther() {
			using (var module = ModuleDefMD.Load(image.Value)) {
				Assert.Equal(OriginalVTables, GetVTables(module));
				var vtables = module.VTableFixups!.VTables;
				Assert.All(vtables, a => Assert.True(a.Is32Bit));
				Assert.Equal((uint)vtables[0].RVA + 8, (uint)vtables[1].RVA);
				Assert.Equal((uint)vtables[1].RVA + 4, (uint)vtables[2].RVA);
				// Each vtable has an 8 byte header in the vtable fixups directory
				Assert.Equal(3 * 8U, module.Metadata.ImageCor20Header.VTableFixups.Size);
			}
		}

		[Fact]
		public void UnchangedVTablesAreWritten() =>
			Assert.Equal(OriginalVTables, ChangeAndWrite(module => { }));

		[Fact]
		public void MethodInAVTableCanBeReplaced() {
			var vtables = ChangeAndWrite(module => module.VTableFixups!.VTables[1].Methods[0] = GetMethod(module, "NotInAVTable"));

			Assert.Equal("Method1,Method2 | NotInAVTable | Method4,Method5", vtables);
		}

		[Fact]
		public void MethodsInAVTableCanBeRemoved() {
			var vtables = ChangeAndWrite(module => {
				module.VTableFixups!.VTables[0].Methods.RemoveAt(0);
				module.VTableFixups.VTables[2].Methods[1] = null;
			});

			Assert.Equal("Method2 | Method3 | Method4,null", vtables);
		}

		[Fact]
		public void VTableCanBeRemoved() {
			var vtables = ChangeAndWrite(module => module.VTableFixups!.VTables.RemoveAt(1), null, out var directory);

			Assert.Equal("Method1,Method2 | Method4,Method5", vtables);
			Assert.Equal(2 * 8U, directory.Size);
		}

		[Theory, InlineData(false), InlineData(true)]
		public void AllVTablesCanBeRemoved(bool removeTheVTableFixups) {
			var vtables = ChangeAndWrite(module => {
				if (removeTheVTableFixups)
					module.VTableFixups = null;
				else
					module.VTableFixups!.VTables.Clear();
			}, null, out var directory);

			Assert.Equal("no vtables", vtables);
			Assert.Equal(0U, (uint)directory.VirtualAddress);
			Assert.Equal(0U, directory.Size);
		}

		// The 2nd vtable is removed and the 1st one gets its method: it now ends where the 2nd one ended
		// (f6674fb6 made the checks faster, and allowed this)
		[Fact]
		public void VTableCanUseTheSpaceOfTheNextOriginalVTable() {
			var vtables = ChangeAndWrite(module => {
				var all = module.VTableFixups!.VTables;
				all[0].Methods.Add(all[1].Methods[0]);
				all.RemoveAt(1);
			});

			Assert.Equal("Method1,Method2,Method3 | Method4,Method5", vtables);
		}

		// A 64-bit slot is 8 bytes: the token and 4 zero bytes. One of them fits in the 1st vtable.
		[Fact]
		public void VTableCanGetSlotsOfAnotherSize() {
			var vtables = ChangeAndWrite(module => {
				var vtable = module.VTableFixups!.VTables[0];
				vtable.Flags = (vtable.Flags & ~VTableFlags.Bit32) | VTableFlags.Bit64;
				vtable.Methods.RemoveAt(1);
			});

			Assert.Equal("Method1 (64-bit) | Method3 | Method4,Method5", vtables);
		}

		// Changes that need space that the original vtables don't have
		static void MakeChangeThatDoesNotFit(ModuleDefMD module, string change) {
			var vtables = module.VTableFixups!.VTables;
			switch (change) {
			case "a vtable gets another method":
				vtables[2].Methods.Add(GetMethod(module, "NotInAVTable"));
				break;
			case "a vtable is moved":
				vtables[0].RVA += 0x1000;
				break;
			case "a vtable is added":
				vtables.Add(new VTable(vtables[0].RVA, vtables[0].Flags, new IMethod?[] { GetMethod(module, "NotInAVTable") }));
				break;
			case "the vtable fixups are moved":
				module.VTableFixups.RVA += 0x1000;
				break;
			default:
				throw new ArgumentException(change);
			}
		}

		public static TheoryData<string, string> ChangesThatDoNotFit => new TheoryData<string, string> {
			{ "a vtable gets another method", "doesn't fit in an original vtable location" },
			{ "a vtable is moved", "doesn't fit in an original vtable location" },
			{ "a vtable is added", "VTable fixups don't fit in the original vtable fixups location" },
			{ "the vtable fixups are moved", "VTable fixups don't fit in the original vtable fixups location" },
		};

		// The vtables can't be moved and there's other data after them
		[Theory, MemberData(nameof(ChangesThatDoNotFit))]
		public void VTablesThatNeedMoreSpaceAreAnError(string change, string expectedError) {
			var exception = Assert.Throws<ModuleWriterException>(() => ChangeAndWrite(module => MakeChangeThatDoesNotFit(module, change)));

			Assert.Contains(expectedError, exception.Message);
		}

		// If the errors are ignored, the file keeps all its original vtables: half of the new ones would be worse
		[Theory, MemberData(nameof(ChangesThatDoNotFit))]
		public void NoVTableIsWrittenIfOneDoesNotFit(string change, string expectedError) {
			Assert.NotEmpty(expectedError);
			var vtables = ChangeAndWrite(module => {
				MakeChangeThatDoesNotFit(module, change);
				// A change that fits
				module.VTableFixups!.VTables[1].Methods[0] = null;
			}, DummyLogger.NoThrowInstance);

			Assert.Equal(OriginalVTables, vtables);
		}
	}
}
