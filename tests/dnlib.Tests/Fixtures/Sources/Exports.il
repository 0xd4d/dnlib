// A test input, see Fixtures.cs. It's assembled by the tests with ilasm.
//
// A DLL that isn't "IL only": it has a method that's exported like a function of a native DLL. Native code calls
// such a method through a slot in a vtable, which the runtime fills in when the DLL is loaded (a "vtable fixup").
// C# can't create this. dnlib needs NativeModuleWriter to write such a file without losing the native parts.

.assembly extern mscorlib { }
.assembly Exports { }
.module Exports.dll
// 32BITREQUIRED, and not ILONLY
.corflags 0x00000002

// One vtable with one 32-bit slot, stored in the data below
.vtfixup [1] int32 fromunmanaged at VTable
.data VTable = int32(0)
.data Bytes = bytearray (DE AD BE EF 01 02 03 04)

.class public abstract sealed auto ansi Exports.Functions extends [mscorlib]System.Object {
	// A field whose value is stored in the PE file
	.field public static int64 Data at Bytes

	.method public static int32 Add(int32 a, int32 b) cil managed {
		.vtentry 1 : 1
		.export [1] as Add
		ldarg.0
		ldarg.1
		add
		ret
	}

	.method public static int32 NotExported(int32 a) cil managed {
		ldarg.0
		ret
	}
}
