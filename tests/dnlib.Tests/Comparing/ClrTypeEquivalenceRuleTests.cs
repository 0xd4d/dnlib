// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using dnlib.DotNet;
using dnlib.Tests.Reading;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Comparing {
	/// <summary>
	/// Two types of different assemblies are the same type for the CLR if they are "equivalent": both have a
	/// <c>TypeIdentifierAttribute</c> with the same values (or are COM interfaces with the same GUID) and the
	/// same members. <see cref="SigComparer"/> has the same rules. Every test compiles a type in two
	/// assemblies, mostly with a difference, and compares the two types with dnlib and with the runtime that
	/// runs the tests (<c>Type.IsEquivalentTo()</c>): dnlib must say the same.
	/// </summary>
	/// <remarks>
	/// Only the CLR on Windows has type equivalence, so the runtime is only asked there.
	/// </remarks>
	public class ClrTypeEquivalenceRuleTests {
		// The code of a test is in the namespace N and defines a type T. "TI" is a TypeIdentifierAttribute with a
		// scope and an identifier, "OTHER" is a type that the code can use: an equivalent struct that isn't the
		// same in both assemblies.
		static byte[] Compile(string code, string assemblyName, string assemblyAttributes) {
			string source = @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
" + assemblyAttributes + @"
namespace N {
	[TypeIdentifier(""scope"", ""N.Same"")] public struct Same { public int Value; }
	[TypeIdentifier(""scope"", ""N.Other"")] public struct Other { public " + (assemblyName == "A" ? "int" : "long") + @" Value; }
	public struct NotEquivalent { public int Value; }
" + code.Replace("TI", "[TypeIdentifier(\"scope\", \"N.T\")]") + @"
}";
			return CSharpCompiler.Compile(source, new CSharpCompilerOptions { AssemblyName = assemblyName }).Image;
		}

		static string ToText(Func<bool> compare) {
			try {
				return compare() ? "yes" : "no";
			}
			catch (Exception ex) {
				return "throws " + ex.GetType().Name;
			}
		}

		// "a equals b, b equals a"
		static string ReadWithDnlib(byte[] imageA, byte[] imageB, string typeName) {
			using (var moduleA = ModuleDefMD.Load(imageA))
			using (var moduleB = ModuleDefMD.Load(imageB)) {
				var a = moduleA.Find(typeName, isReflectionName: true);
				var b = moduleB.Find(typeName, isReflectionName: true);
				return ToText(() => new SigComparer().Equals(a, b)) + ", " + ToText(() => new SigComparer().Equals(b, a));
			}
		}

		static string ReadWithTheRuntime(byte[] imageA, byte[] imageB, string typeName) {
			using (var loadedA = new LoadedAssembly(imageA))
			using (var loadedB = new LoadedAssembly(imageB)) {
				var a = loadedA.Assembly.GetType(typeName, throwOnError: true)!;
				var b = loadedB.Assembly.GetType(typeName, throwOnError: true)!;
				return ToText(() => a.IsEquivalentTo(b)) + ", " + ToText(() => b.IsEquivalentTo(a));
			}
		}

		static void Check(string codeA, string codeB, string typeName, string assemblyAttributesA, string assemblyAttributesB, string expectedDnlib, string expectedRuntime) {
			var imageA = Compile(codeA, "A", assemblyAttributesA);
			var imageB = Compile(codeB == "=" ? codeA : codeB, "B", assemblyAttributesB);
			string dnlib = ReadWithDnlib(imageA, imageB, typeName);

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				ClrReaderComparison.AssertRead(expectedDnlib, dnlib, expectedRuntime, () => ReadWithTheRuntime(imageA, imageB, typeName));
			else
				Assert.Equal(expectedDnlib, dnlib);
		}

		// The code of the type in the first assembly, and in the second one ("=" if it's the same)
		[Theory]
		[InlineData("TI public struct T { public int X; public long Y; }", "=", "yes, yes", "yes, yes")]
		[InlineData("public struct T { public int X; }", "=", "no, no", "no, no")]
		[InlineData("TI public struct T { public int X; }", "public struct T { public int X; }", "no, no", "no, no")]
		// The values of the attribute
		[InlineData("[TypeIdentifier(\"scope\", \"N.T\")] public struct T { public int X; }", "[TypeIdentifier(\"scope\", \"N.t\")] public struct T { public int X; }", "no, no", "no, no")]
		[InlineData("[TypeIdentifier(\"scope\", \"N.T\")] public struct T { public int X; }", "[TypeIdentifier(\"SCOPE\", \"N.T\")] public struct T { public int X; }", "yes, yes", "yes, yes")]
		[InlineData("[TypeIdentifier(\"scope\", \"Anything\")] public struct T { public int X; }", "=", "yes, yes", "yes, yes")]
		[InlineData("[TypeIdentifier(\"\", \"\")] public struct T { public int X; }", "=", "yes, yes", "yes, yes")]
		[InlineData("[TypeIdentifier(null, null)] public struct T { public int X; }", "=", "no, no", "no, no")]
		[InlineData("[TypeIdentifier(null, null)] public struct T { public int X; }", "[TypeIdentifier(\"\", \"\")] public struct T { public int X; }", "no, no", "no, no")]
		[InlineData("[TypeIdentifier] public struct T { public int X; }", "=", "no, no", "no, no")]
		[InlineData("[TypeIdentifier, Guid(\"11111111-2222-3333-4444-555555555555\")] public struct T { public int X; }", "=", "no, no", "no, no")]
		[InlineData("[TypeIdentifier, Guid(\"11111111-2222-3333-4444-555555555555\")] public struct T { public int X; }", "[TypeIdentifier, Guid(\"11111111-2222-3333-4444-555555555556\")] public struct T { public int X; }", "no, no", "no, no")]
		[InlineData("[TypeIdentifier] public struct T { public int X; }", "TI public struct T { public int X; }", "no, no", "no, no")]
		// The fields of a struct
		[InlineData("TI public struct T { public int X; public long Y; }", "TI public struct T { public long Y; public int X; }", "no, no", "no, no")]
		[InlineData("TI public struct T { public int X; }", "TI public struct T { public int Y; }", "yes, yes", "yes, yes")]
		[InlineData("TI public struct T { public int X; }", "TI public struct T { public uint X; }", "no, no", "no, no")]
		[InlineData("TI public struct T { public int X; }", "TI public struct T { public int X; public int Y; }", "no, no", "no, no")]
		[InlineData("TI public struct T { public int X; }", "TI public struct T { public int X; public static int Y; }", "no, no", "throws TypeLoadException")]
		[InlineData("TI public struct T { public int X; }", "TI public struct T { public int X; public const int Y = 1; }", "no, no", "no, no")]
		[InlineData("TI public struct T { public int X; public static int Y; }", "=", "no, no", "throws TypeLoadException")]
		[InlineData("TI public struct T { public int X; public const int Y = 1; }", "=", "no, no", "no, no")]
		[InlineData("TI public struct T { public int X; int y; }", "=", "no, no", "throws TypeLoadException")]
		[InlineData("TI public struct T { public int X; internal int Y; }", "=", "no, no", "throws TypeLoadException")]
		[InlineData("TI public struct T { int x; }", "=", "no, no", "throws TypeLoadException")]
		[InlineData("TI public struct T { }", "=", "yes, yes", "yes, yes")]
		[InlineData("TI public struct T { public int X; public readonly int Y; }", "TI public struct T { public int X; public int Y; }", "yes, yes", "yes, yes")]
		[InlineData("TI public struct T { public int X; [NonSerialized] public int Y; }", "TI public struct T { public int X; public int Y; }", "yes, yes", "yes, yes")]
		[InlineData("TI public struct T { public int X; [Obsolete] public int Y; }", "TI public struct T { public int X; public int Y; }", "yes, yes", "yes, yes")]
		// Other members of a struct
		[InlineData("TI public struct T { public int X; public void M() { } }", "=", "no, no", "throws TypeLoadException")]
		[InlineData("TI public struct T { public int X; public static void M() { } }", "=", "no, no", "no, no")]
		[InlineData("TI public struct T { public int X; public T(int x) { X = x; } }", "=", "no, no", "throws TypeLoadException")]
		[InlineData("TI public struct T { public int X; public int P { get { return X; } } }", "=", "no, no", "throws TypeLoadException")]
		[InlineData("TI public struct T { public int X; public struct Nested { } }", "=", "yes, yes", "yes, yes")]
		[InlineData("TI public struct T : IDisposable { public int X; void IDisposable.Dispose() { } }", "=", "no, no", "throws TypeLoadException")]
		// The types of the fields
		[InlineData("TI public struct T { public Same X; }", "=", "yes, yes", "yes, yes")]
		[InlineData("TI public struct T { public Other X; }", "=", "no, no", "no, no")]
		[InlineData("TI public struct T { public NotEquivalent X; }", "=", "no, no", "no, no")]
		[InlineData("TI public struct T { public Same[] X; }", "=", "yes, yes", "yes, yes")]
		[InlineData("TI public unsafe struct T { public Same* X; }", "=", "yes, yes", "yes, yes")]
		[InlineData("TI public struct T { public List<Same> X; }", "=", "no, no", "no, no")]
		[InlineData("TI public struct T { public List<int> X; }", "=", "yes, yes", "yes, yes")]
		[InlineData("TI public struct T { public Same? X; }", "=", "no, no", "no, no")]
		[InlineData("TI public struct T { public string X; public object Y; public IntPtr Z; public int[] A; }", "=", "yes, yes", "yes, yes")]
		[InlineData("TI public unsafe struct T { public delegate*<Same, void> X; }", "=", "no, no", "no, no")]
		[InlineData("TI public unsafe struct T { public delegate*<int, void> X; }", "=", "yes, yes", "yes, yes")]
		[InlineData("TI public unsafe struct T { public fixed int X[4]; }", "=", "no, no", "no, no")]
		// The layout of a struct
		[InlineData("TI [StructLayout(LayoutKind.Sequential)] public struct T { public int X; }", "TI [StructLayout(LayoutKind.Auto)] public struct T { public int X; }", "no, no", "no, no")]
		[InlineData("TI [StructLayout(LayoutKind.Auto)] public struct T { public int X; }", "=", "no, no", "no, no")]
		[InlineData("TI [StructLayout(LayoutKind.Sequential)] public struct T { public int X; }", "TI [StructLayout(LayoutKind.Explicit)] public struct T { [FieldOffset(0)] public int X; }", "no, no", "no, no")]
		[InlineData("TI [StructLayout(LayoutKind.Explicit)] public struct T { [FieldOffset(0)] public int X; [FieldOffset(4)] public int Y; }", "=", "yes, yes", "yes, yes")]
		[InlineData("TI [StructLayout(LayoutKind.Explicit)] public struct T { [FieldOffset(0)] public int X; [FieldOffset(4)] public int Y; }", "TI [StructLayout(LayoutKind.Explicit)] public struct T { [FieldOffset(0)] public int X; [FieldOffset(8)] public int Y; }", "no, no", "no, no")]
		[InlineData("TI [StructLayout(LayoutKind.Sequential, Pack = 1)] public struct T { public int X; }", "TI [StructLayout(LayoutKind.Sequential, Pack = 2)] public struct T { public int X; }", "no, no", "no, no")]
		[InlineData("TI [StructLayout(LayoutKind.Sequential, Pack = 1)] public struct T { public int X; }", "=", "yes, yes", "yes, yes")]
		[InlineData("TI [StructLayout(LayoutKind.Sequential)] public struct T { public int X; }", "TI [StructLayout(LayoutKind.Sequential, Pack = 8)] public struct T { public int X; }", "no, no", "no, no")]
		[InlineData("TI [StructLayout(LayoutKind.Sequential, Size = 16)] public struct T { public int X; }", "TI [StructLayout(LayoutKind.Sequential, Size = 32)] public struct T { public int X; }", "no, no", "no, no")]
		[InlineData("TI [StructLayout(LayoutKind.Sequential, Size = 16)] public struct T { public int X; }", "TI [StructLayout(LayoutKind.Sequential)] public struct T { public int X; }", "no, no", "no, no")]
		[InlineData("TI [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)] public struct T { public int X; }", "TI [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] public struct T { public int X; }", "no, no", "no, no")]
		[InlineData("TI [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)] public struct T { public int X; }", "=", "yes, yes", "yes, yes")]
		// How a field is marshalled
		[InlineData("TI public struct T { [MarshalAs(UnmanagedType.LPWStr)] public string X; }", "=", "yes, yes", "yes, yes")]
		[InlineData("TI public struct T { [MarshalAs(UnmanagedType.LPWStr)] public string X; }", "TI public struct T { [MarshalAs(UnmanagedType.LPStr)] public string X; }", "no, no", "no, no")]
		[InlineData("TI public struct T { [MarshalAs(UnmanagedType.LPWStr)] public string X; }", "TI public struct T { public string X; }", "no, no", "no, no")]
		[InlineData("TI public struct T { [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public int[] X; }", "TI public struct T { [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)] public int[] X; }", "no, no", "no, no")]
		// Enums: the fields with the values aren't compared
		[InlineData("TI public enum T { A, B }", "=", "yes, yes", "yes, yes")]
		[InlineData("TI public enum T { A, B }", "TI public enum T { A, B, C }", "yes, yes", "yes, yes")]
		[InlineData("TI public enum T { A = 1 }", "TI public enum T { A = 2 }", "yes, yes", "yes, yes")]
		[InlineData("TI public enum T { A }", "TI public enum T { B }", "yes, yes", "yes, yes")]
		[InlineData("TI public enum T { A }", "TI public enum T : long { A }", "no, no", "no, no")]
		[InlineData("TI public enum T : uint { A }", "TI public enum T : int { A }", "no, no", "no, no")]
		[InlineData("TI public enum T { }", "=", "yes, yes", "yes, yes")]
		[InlineData("TI [Flags] public enum T { A }", "TI public enum T { A }", "yes, yes", "yes, yes")]
		[InlineData("TI public enum T { A }", "TI public struct T { public int value__; }", "no, no", "no, no")]
		// Delegates: the signature of the Invoke method
		[InlineData("TI public delegate int T(int a);", "=", "yes, yes", "yes, yes")]
		[InlineData("TI public delegate int T(int a);", "TI public delegate int T(int b);", "yes, yes", "yes, yes")]
		[InlineData("TI public delegate int T(int a);", "TI public delegate long T(int a);", "no, no", "no, no")]
		[InlineData("TI public delegate int T(int a);", "TI public delegate int T(long a);", "no, no", "no, no")]
		[InlineData("TI public delegate int T(int a);", "TI public delegate int T(int a, int b);", "no, no", "no, no")]
		[InlineData("TI public delegate int T(int a);", "TI public delegate int T(ref int a);", "no, no", "no, no")]
		[InlineData("TI public delegate int T(ref int a);", "TI public delegate int T(out int a);", "yes, yes", "yes, yes")]
		[InlineData("TI public delegate int T(ref int a);", "TI public delegate int T(in int a);", "no, no", "no, no")]
		[InlineData("TI public delegate int T(int a);", "TI public delegate int T([MarshalAs(UnmanagedType.U4)] int a);", "yes, yes", "yes, yes")]
		[InlineData("TI public delegate void T(Same a);", "=", "yes, yes", "yes, yes")]
		[InlineData("TI public delegate void T(Other a);", "=", "no, no", "no, no")]
		[InlineData("TI public delegate void T(NotEquivalent a);", "=", "no, no", "no, no")]
		[InlineData("TI public delegate void T(List<Same> a);", "=", "no, no", "no, no")]
		[InlineData("TI public delegate void T(Same[] a);", "=", "yes, yes", "yes, yes")]
		[InlineData("TI public delegate T T();", "=", "yes, yes", "yes, yes")]
		[InlineData("TI [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void T();", "TI public delegate void T();", "yes, yes", "yes, yes")]
		// Interfaces: the members aren't compared
		[InlineData("[ComImport, Guid(\"11111111-2222-3333-4444-555555555555\"), TypeIdentifier] public interface T { void M(); }", "=", "yes, yes", "yes, yes")]
		[InlineData("[ComImport, Guid(\"11111111-2222-3333-4444-555555555555\"), TypeIdentifier] public interface T { void M(); }", "[ComImport, Guid(\"11111111-2222-3333-4444-555555555555\"), TypeIdentifier] public interface T { int Other(int a); void M(); }", "yes, yes", "yes, yes")]
		[InlineData("[ComImport, Guid(\"11111111-2222-3333-4444-555555555555\"), TypeIdentifier] public interface T { }", "[ComImport, Guid(\"11111111-2222-3333-4444-555555555556\"), TypeIdentifier] public interface T { }", "no, no", "no, no")]
		[InlineData("[ComImport, Guid(\"11111111-2222-3333-4444-555555555555\")] public interface T { }", "=", "no, no", "no, no")]
		[InlineData("[ComImport, Guid(\"11111111-2222-3333-4444-555555555555\"), TypeIdentifier] public interface T { }", "[ComImport, Guid(\"11111111-2222-3333-4444-555555555555\")] public interface T { }", "no, no", "no, no")]
		[InlineData("[Guid(\"11111111-2222-3333-4444-555555555555\"), TypeIdentifier] public interface T { }", "=", "no, no", "throws TypeLoadException")]
		[InlineData("[TypeIdentifier] public interface T { }", "=", "no, no", "throws TypeLoadException")]
		[InlineData("TI public interface T { }", "=", "yes, yes", "throws TypeLoadException")]
		[InlineData("[ComImport, Guid(\"11111111-2222-3333-4444-555555555555\"), TypeIdentifier(\"scope\", \"id\")] public interface T { }", "[ComImport, Guid(\"11111111-2222-3333-4444-555555555556\"), TypeIdentifier(\"scope\", \"id\")] public interface T { }", "yes, yes", "yes, yes")]
		[InlineData("[ComImport, Guid(\"11111111-2222-3333-4444-555555555555\"), TypeIdentifier] public interface T { }", "TI public struct T { }", "no, no", "no, no")]
		// Visibility
		[InlineData("TI struct T { public int X; }", "=", "yes, yes", "yes, yes")]
		[InlineData("TI public struct T { public int X; }", "TI struct T { public int X; }", "yes, yes", "yes, yes")]
		public void TypeInTwoAssemblies(string codeA, string codeB, string expectedDnlib, string expectedRuntime) =>
			Check(codeA, codeB, "N.T", "", "", expectedDnlib, expectedRuntime);

		// The type is nested, or it's in a namespace with another name
		[Theory]
		[InlineData("TI public struct Outer { public int X; [TypeIdentifier(\"scope\", \"N.Outer+T\")] public struct T { public int X; } }", "=", "N.Outer+T", "yes, yes", "yes, yes")]
		[InlineData("public struct Outer { public int X; [TypeIdentifier(\"scope\", \"N.Outer+T\")] public struct T { public int X; } }", "=", "N.Outer+T", "no, no", "no, no")]
		[InlineData("TI public struct Outer { public int X; [TypeIdentifier(\"scope\", \"N.Outer+T\")] public struct T { public int X; } }", "TI public struct Outer { public long X; [TypeIdentifier(\"scope\", \"N.Outer+T\")] public struct T { public int X; } }", "N.Outer+T", "no, no", "no, no")]
		[InlineData("TI public struct Outer { [TypeIdentifier(\"scope\", \"N.Outer+T\")] struct T { public int X; } }", "=", "N.Outer+T", "yes, yes", "yes, yes")]
		[InlineData("public class Outer { [TypeIdentifier(\"scope\", \"N.Outer+T\")] public struct T { public int X; } }", "=", "N.Outer+T", "no, no", "no, no")]
		[InlineData("public class Outer<G> { [TypeIdentifier(\"scope\", \"x\")] public struct T { public int X; } }", "=", "N.Outer`1+T", "no, no", "throws TypeLoadException")]
		[InlineData("} namespace N.M { TI public struct T { public int X; }", "} namespace N.m { TI public struct T { public int X; }", "N.M.T", "no, no", "throws TypeLoadException")]
		public void NestedTypeInTwoAssemblies(string codeA, string codeB, string typeName, string expectedDnlib, string expectedRuntime) {
			// The second namespace of the last case has another name
			if (codeB.Contains("namespace N.m"))
				Check(codeA, codeB.Replace("N.m", "N.M2"), typeName, "", "", expectedDnlib, expectedRuntime);
			else
				Check(codeA, codeB, typeName, "", "", expectedDnlib, expectedRuntime);
		}

		// The types of an assembly that was made from a type library are equivalent without the attribute
		const string TypeLibrary = "[assembly: ImportedFromTypeLib(\"Library\")] [assembly: Guid(\"99999999-2222-3333-4444-555555555555\")]";
		const string OtherTypeLibrary = "[assembly: ImportedFromTypeLib(\"Library\")] [assembly: Guid(\"99999999-2222-3333-4444-555555555556\")]";
		const string TypeLibraryWithoutGuid = "[assembly: ImportedFromTypeLib(\"Library\")]";
		const string PrimaryInteropAssembly = "[assembly: PrimaryInteropAssembly(1, 0)] [assembly: Guid(\"99999999-2222-3333-4444-555555555555\")]";

		[Theory]
		[InlineData("public struct T { public int X; }", TypeLibrary, TypeLibrary, "yes, yes", "yes, yes")]
		[InlineData("public struct T { public int X; }", TypeLibrary, OtherTypeLibrary, "no, no", "no, no")]
		[InlineData("public struct T { public int X; }", TypeLibrary, "", "no, no", "no, no")]
		[InlineData("public struct T { public int X; }", TypeLibraryWithoutGuid, TypeLibraryWithoutGuid, "no, no", "no, no")]
		[InlineData("public struct T { public int X; }", PrimaryInteropAssembly, TypeLibrary, "yes, yes", "yes, yes")]
		[InlineData("public enum T { A }", TypeLibrary, TypeLibrary, "yes, yes", "yes, yes")]
		[InlineData("public delegate void T();", TypeLibrary, TypeLibrary, "yes, yes", "yes, yes")]
		[InlineData("public class T { }", TypeLibrary, TypeLibrary, "no, no", "no, no")]
		[InlineData("public interface T { }", TypeLibrary, TypeLibrary, "no, no", "no, no")]
		[InlineData("[ComImport, Guid(\"11111111-2222-3333-4444-555555555555\")] public interface T { }", TypeLibrary, TypeLibrary, "yes, yes", "yes, yes")]
		[InlineData("[ComImport, Guid(\"11111111-2222-3333-4444-555555555555\")] public interface T { }", TypeLibrary, OtherTypeLibrary, "yes, yes", "yes, yes")]
		[InlineData("[Guid(\"11111111-2222-3333-4444-555555555555\")] public struct T { public int X; }", TypeLibrary, OtherTypeLibrary, "no, no", "no, no")]
		[InlineData("struct T { public int X; }", TypeLibrary, TypeLibrary, "no, no", "no, no")]
		[InlineData("public struct T<G> { public int X; }", TypeLibrary, TypeLibrary, "no, no", "no, no")]
		[InlineData("TI public struct T { public int X; }", TypeLibrary, "", "yes, yes", "yes, yes")]
		public void TypeInTwoTypeLibraries(string code, string assemblyAttributesA, string assemblyAttributesB, string expectedDnlib, string expectedRuntime) =>
			Check(code, "=", code.Contains("T<G>") ? "N.T`1" : "N.T", assemblyAttributesA, assemblyAttributesB, expectedDnlib, expectedRuntime);
	}
}
