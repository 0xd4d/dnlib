// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using System.Runtime.InteropServices;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;
using MethodAttributes = dnlib.DotNet.MethodAttributes;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// A method of a native library has the PinvokeImpl flag and a row in the ImplMap table: flags (character
	/// set, calling convention, ...), the name of the function and the library (a row of the ModuleRef table).
	/// These tests write values that a compiler doesn't write and read them with dnlib and with the runtime
	/// that runs the tests (System.Reflection shows them as a DllImportAttribute). If the runtime reads a value,
	/// dnlib must read the same value. dnlib has the flags of the file; the DllImportAttribute has "Cdecl" for
	/// a calling convention that isn't one, and false for "best fit" and "throw" if the flags don't say on.
	/// </summary>
	public class ClrPInvokeRuleTests {
		const string Source = @"
using System.Runtime.InteropServices;
public class C {
	[DllImport(""lib"", EntryPoint = ""Entry"")] public static extern void M();
	[DllImport(""other"")] public static extern void Other();
	public static void Normal() { }
}";
		static readonly byte[] baseImage = CSharpCompiler.Compile(Source).Image;

		// "What = value", separated by "; ". 'What' is a column of the ImplMap row of M (MappingFlags,
		// MemberForwarded, ImportName, ImportScope) or "Flags" (of the method M). A value is a number, or for
		// ImportName "name of M" / "name of Other" (the names of the methods), for ImportScope "other" (the
		// library of Other), for MemberForwarded "Normal" (that method), for Flags "-pinvoke".
		static byte[] CreateImage(string changes) {
			uint ridM, ridOther, ridNormal;
			using (var module = ModuleDefMD.Load(baseImage)) {
				var type = module.Find("C", false)!;
				ridM = type.FindMethod("M")!.Rid;
				ridOther = type.FindMethod("Other")!.Rid;
				ridNormal = type.FindMethod("Normal")!.Rid;
			}
			var patcher = new MetadataPatcher(baseImage);
			// MemberForwarded is a coded token: the row number and 0 = Field, 1 = Method
			uint FindRow(uint method) => Enumerable.Range(1, 2).Select(a => (uint)a).Single(a => patcher.GetCell(Table.ImplMap, a, "MemberForwarded") == ((method << 1) | 1));
			uint row = FindRow(ridM), rowOfOther = FindRow(ridOther);
			foreach (var change in changes.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries)) {
				var parts = change.Split(new[] { " = " }, StringSplitOptions.None);
				string what = parts[0], value = parts[1];
				if (what == "Flags") {
					uint flags = patcher.GetCell(Table.Method, ridM, "Flags");
					patcher.SetCell(Table.Method, ridM, "Flags", value == "-pinvoke" ? flags & ~(uint)MethodAttributes.PinvokeImpl : uint.Parse(value));
					continue;
				}
				uint newValue = value == "name of M" ? patcher.GetCell(Table.Method, ridM, "Name") :
					value == "name of Other" ? patcher.GetCell(Table.Method, ridOther, "Name") :
					value == "other" ? patcher.GetCell(Table.ImplMap, rowOfOther, "ImportScope") :
					value == "Normal" ? (ridNormal << 1) | 1 :
					uint.Parse(value);
				patcher.SetCell(Table.ImplMap, row, what, newValue);
			}
			return patcher.ToArray();
		}

		static string Describe(string? library, string? function, string charSet, string callingConvention, bool exactSpelling, bool setLastError, string bestFit, string throwOnUnmappable) =>
			$"{library ?? "null"}!{function ?? "null"} {charSet} {callingConvention}" + (exactSpelling ? " exact" : "") + (setLastError ? " lasterror" : "") + $" bestfit={bestFit} throw={throwOnUnmappable}";

		static string ReadWithDnlib(MethodDef method) {
			var map = method.ImplMap;
			if (map is null)
				return (method.IsPinvokeImpl ? "pinvoke " : "") + "no ImplMap";
			return (method.IsPinvokeImpl ? "" : "not pinvoke ") + Describe(map.Module?.Name, map.Name,
				map.IsCharSetAnsi ? "Ansi" : map.IsCharSetUnicode ? "Unicode" : map.IsCharSetAuto ? "Auto" : "None",
				map.IsCallConvWinapi ? "Winapi" : map.IsCallConvCdecl ? "Cdecl" : map.IsCallConvStdcall ? "StdCall" : map.IsCallConvThiscall ? "ThisCall" : map.IsCallConvFastcall ? "FastCall" : "?",
				map.IsNoMangle, map.SupportsLastError,
				map.IsBestFitEnabled ? "True" : map.IsBestFitDisabled ? "False" : map.IsBestFitUseAssem ? "default" : "?",
				map.IsThrowOnUnmappableCharEnabled ? "True" : map.IsThrowOnUnmappableCharDisabled ? "False" : map.IsThrowOnUnmappableCharUseAssem ? "default" : "?");
		}

		static string ReadWithTheRuntime(System.Reflection.MethodInfo method) {
			var attribute = (DllImportAttribute?)method.GetCustomAttributes(typeof(DllImportAttribute), false).FirstOrDefault();
			if (attribute is null)
				return "no DllImportAttribute";
			return Describe(attribute.Value, attribute.EntryPoint, attribute.CharSet.ToString(), attribute.CallingConvention.ToString(),
				attribute.ExactSpelling, attribute.SetLastError, attribute.BestFitMapping.ToString(), attribute.ThrowOnUnmappableChar.ToString());
		}

		[Theory]
		[InlineData("", "M: lib!Entry None Winapi bestfit=default throw=default, Normal: no ImplMap", "M: lib!Entry None Winapi bestfit=False throw=False, Normal: no DllImportAttribute")]
		// The flags: 1 = exact spelling, 2/4/6 = character set (ANSI/Unicode/auto), 0x10/0x20 = best fit on/off,
		// 0x40 = sets the last error, 0x100-0x500 = calling convention, 0x1000/0x2000 = throw on a character
		// that can't be converted on/off
		[InlineData("MappingFlags = 0", "M: lib!Entry None ? bestfit=default throw=default, Normal: no ImplMap", "M: lib!Entry None Cdecl bestfit=False throw=False, Normal: no DllImportAttribute")]
		[InlineData("MappingFlags = 1", "M: lib!Entry None ? exact bestfit=default throw=default, Normal: no ImplMap", "M: lib!Entry None Cdecl exact bestfit=False throw=False, Normal: no DllImportAttribute")]
		[InlineData("MappingFlags = 2", "M: lib!Entry Ansi ? bestfit=default throw=default, Normal: no ImplMap", "M: lib!Entry Ansi Cdecl bestfit=False throw=False, Normal: no DllImportAttribute")]
		[InlineData("MappingFlags = 4", "M: lib!Entry Unicode ? bestfit=default throw=default, Normal: no ImplMap", "M: lib!Entry Unicode Cdecl bestfit=False throw=False, Normal: no DllImportAttribute")]
		[InlineData("MappingFlags = 6", "M: lib!Entry Auto ? bestfit=default throw=default, Normal: no ImplMap", "M: lib!Entry Auto Cdecl bestfit=False throw=False, Normal: no DllImportAttribute")]
		[InlineData("MappingFlags = 8", "M: lib!Entry None ? bestfit=default throw=default, Normal: no ImplMap", "M: lib!Entry None Cdecl bestfit=False throw=False, Normal: no DllImportAttribute")]
		[InlineData("MappingFlags = 16", "M: lib!Entry None ? bestfit=True throw=default, Normal: no ImplMap", "M: lib!Entry None Cdecl bestfit=True throw=False, Normal: no DllImportAttribute")]
		[InlineData("MappingFlags = 32", "M: lib!Entry None ? bestfit=False throw=default, Normal: no ImplMap", "M: lib!Entry None Cdecl bestfit=False throw=False, Normal: no DllImportAttribute")]
		[InlineData("MappingFlags = 48", "M: lib!Entry None ? bestfit=? throw=default, Normal: no ImplMap", "M: lib!Entry None Cdecl bestfit=False throw=False, Normal: no DllImportAttribute")]
		[InlineData("MappingFlags = 64", "M: lib!Entry None ? lasterror bestfit=default throw=default, Normal: no ImplMap", "M: lib!Entry None Cdecl lasterror bestfit=False throw=False, Normal: no DllImportAttribute")]
		[InlineData("MappingFlags = 128", "M: lib!Entry None ? bestfit=default throw=default, Normal: no ImplMap", "M: lib!Entry None Cdecl bestfit=False throw=False, Normal: no DllImportAttribute")]
		[InlineData("MappingFlags = 256", "M: lib!Entry None Winapi bestfit=default throw=default, Normal: no ImplMap", "M: lib!Entry None Winapi bestfit=False throw=False, Normal: no DllImportAttribute")]
		[InlineData("MappingFlags = 512", "M: lib!Entry None Cdecl bestfit=default throw=default, Normal: no ImplMap", "M: lib!Entry None Cdecl bestfit=False throw=False, Normal: no DllImportAttribute")]
		[InlineData("MappingFlags = 768", "M: lib!Entry None StdCall bestfit=default throw=default, Normal: no ImplMap", "M: lib!Entry None StdCall bestfit=False throw=False, Normal: no DllImportAttribute")]
		[InlineData("MappingFlags = 1024", "M: lib!Entry None ThisCall bestfit=default throw=default, Normal: no ImplMap", "M: lib!Entry None ThisCall bestfit=False throw=False, Normal: no DllImportAttribute")]
		[InlineData("MappingFlags = 1280", "M: lib!Entry None FastCall bestfit=default throw=default, Normal: no ImplMap", "M: lib!Entry None FastCall bestfit=False throw=False, Normal: no DllImportAttribute")]
		[InlineData("MappingFlags = 1536", "M: lib!Entry None ? bestfit=default throw=default, Normal: no ImplMap", "M: lib!Entry None Cdecl bestfit=False throw=False, Normal: no DllImportAttribute")]
		[InlineData("MappingFlags = 1792", "M: lib!Entry None ? bestfit=default throw=default, Normal: no ImplMap", "M: lib!Entry None Cdecl bestfit=False throw=False, Normal: no DllImportAttribute")]
		[InlineData("MappingFlags = 4096", "M: lib!Entry None ? bestfit=default throw=True, Normal: no ImplMap", "M: lib!Entry None Cdecl bestfit=False throw=True, Normal: no DllImportAttribute")]
		[InlineData("MappingFlags = 8192", "M: lib!Entry None ? bestfit=default throw=False, Normal: no ImplMap", "M: lib!Entry None Cdecl bestfit=False throw=False, Normal: no DllImportAttribute")]
		[InlineData("MappingFlags = 12288", "M: lib!Entry None ? bestfit=default throw=?, Normal: no ImplMap", "M: lib!Entry None Cdecl bestfit=False throw=False, Normal: no DllImportAttribute")]
		[InlineData("MappingFlags = 32768", "M: lib!Entry None ? bestfit=default throw=default, Normal: no ImplMap", "M: lib!Entry None Cdecl bestfit=False throw=False, Normal: no DllImportAttribute")]
		[InlineData("MappingFlags = 65535", "M: lib!Entry Auto ? exact lasterror bestfit=? throw=?, Normal: no ImplMap", "M: lib!Entry Auto Cdecl exact lasterror bestfit=False throw=False, Normal: no DllImportAttribute")]
		// The name of the function
		[InlineData("ImportName = 0", "M: lib! None Winapi bestfit=default throw=default, Normal: no ImplMap", "M: lib! None Winapi bestfit=False throw=False, Normal: no DllImportAttribute")]
		[InlineData("ImportName = name of M", "M: lib!M None Winapi bestfit=default throw=default, Normal: no ImplMap", "M: lib!M None Winapi bestfit=False throw=False, Normal: no DllImportAttribute")]
		[InlineData("ImportName = name of Other", "M: lib!Other None Winapi bestfit=default throw=default, Normal: no ImplMap", "M: lib!Other None Winapi bestfit=False throw=False, Normal: no DllImportAttribute")]
		// The library
		[InlineData("ImportScope = other", "M: other!Entry None Winapi bestfit=default throw=default, Normal: no ImplMap", "M: other!Entry None Winapi bestfit=False throw=False, Normal: no DllImportAttribute")]
		[InlineData("ImportScope = 0", "M: null!Entry None Winapi bestfit=default throw=default, Normal: no ImplMap", "throws BadImageFormatException")]
		[InlineData("ImportScope = 1000", "M: null!Entry None Winapi bestfit=default throw=default, Normal: no ImplMap", "throws BadImageFormatException")]
		// The method of the row, and the flag of the method
		[InlineData("MemberForwarded = Normal", "M: pinvoke no ImplMap, Normal: not pinvoke lib!Entry None Winapi bestfit=default throw=default", "M: ! None Cdecl bestfit=False throw=False, Normal: no DllImportAttribute")]
		[InlineData("MemberForwarded = 0", "M: pinvoke no ImplMap, Normal: no ImplMap", "M: ! None Cdecl bestfit=False throw=False, Normal: no DllImportAttribute")]
		[InlineData("MemberForwarded = 1", "M: pinvoke no ImplMap, Normal: no ImplMap", "M: ! None Cdecl bestfit=False throw=False, Normal: no DllImportAttribute")]
		[InlineData("MemberForwarded = 2001", "M: pinvoke no ImplMap, Normal: no ImplMap", "M: ! None Cdecl bestfit=False throw=False, Normal: no DllImportAttribute")]
		[InlineData("Flags = -pinvoke", "M: not pinvoke lib!Entry None Winapi bestfit=default throw=default, Normal: no ImplMap", "M: no DllImportAttribute, Normal: no DllImportAttribute")]
		public void NativeMethod(string changes, string expectedDnlib, string expectedRuntime) {
			var image = CreateImage(changes);

			string dnlib;
			using (var module = ModuleDefMD.Load(image)) {
				var type = module.Find("C", false)!;
				dnlib = "M: " + ReadWithDnlib(type.FindMethod("M")!) + ", Normal: " + ReadWithDnlib(type.FindMethod("Normal")!);
			}

			ClrReaderComparison.AssertRead(expectedDnlib, dnlib, expectedRuntime, () => {
				using (var loaded = new LoadedAssembly(image)) {
					var type = loaded.Assembly.GetType("C")!;
					return "M: " + ReadWithTheRuntime(type.GetMethod("M")!) + ", Normal: " + ReadWithTheRuntime(type.GetMethod("Normal")!);
				}
			});
		}
	}
}
