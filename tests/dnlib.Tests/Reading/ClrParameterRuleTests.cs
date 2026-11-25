// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// A parameter has flags (in, out, optional, has a default value) in its row of the Param table, and its
	/// default value is a row of the Constant table whose parent is the parameter. A field and a property can
	/// have a constant too. These tests write flags and parents that a compiler doesn't write and read the
	/// parameters and the constants with dnlib and with the runtime that runs the tests. If the runtime reads a
	/// value, dnlib must read the same value.
	/// </summary>
	public class ClrParameterRuleTests {
		// The Constant table has three rows: the ones of F, b and c
		const string Source = @"
public class C {
	public const int F = 0x11;
	public static int G;
	public int P { get; set; }
	public static void M(int a, int b = 0x22, int c = 0) { }
}";
		static readonly byte[] baseImage = CSharpCompiler.Compile(Source).Image;

		// The parent of a constant is a coded token: the row number and 0 = Field, 1 = Param, 2 = Property,
		// 3 = not used
		static uint GetConstantParent(ModuleDefMD module, string name) {
			var type = module.Find("C", false)!;
			if (name == "nothing")
				return 0;
			if (name.StartsWith("tag 3 "))
				return (uint.Parse(name.Substring(6)) << 2) | 3;
			if (name.Length == 1 && char.IsLower(name[0]))
				return (type.FindMethod("M")!.ParamDefs.Single(a => a.Name == name).Rid << 2) | 1;
			if (name == "P")
				return (type.FindProperty("P")!.Rid << 2) | 2;
			return type.FindField(name)!.Rid << 2;
		}

		// "Flags of b = number" or "Constant of b = new parent", separated by "; "
		static byte[] CreateImage(string changes) {
			var patcher = new MetadataPatcher(baseImage);
			using (var module = ModuleDefMD.Load(baseImage)) {
				uint parentB = GetConstantParent(module, "b");
				uint constantRow = Enumerable.Range(1, (int)patcher.GetRowCount(Table.Constant)).Select(a => (uint)a).Single(a => patcher.GetCell(Table.Constant, a, "Parent") == parentB);
				foreach (var change in changes.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries)) {
					var parts = change.Split(new[] { " = " }, StringSplitOptions.None);
					if (parts[0] == "Flags of b")
						patcher.SetCell(Table.Param, parentB >> 2, "Flags", uint.Parse(parts[1]));
					else
						patcher.SetCell(Table.Constant, constantRow, "Parent", GetConstantParent(module, parts[1]));
				}
			}
			// The rows of the Constant table are sorted by their parent: a reader doesn't find a row if they aren't
			patcher.SortRows(Table.Constant, "Parent");
			return patcher.ToArray();
		}

		static string Describe(string name, bool isIn, bool isOut, bool isOptional, bool hasDefault, object? value) =>
			name + (isIn ? " in" : "") + (isOut ? " out" : "") + (isOptional ? " optional" : "") + (hasDefault ? " default" : "") + (value is null ? "" : " = " + ((int)value).ToString("X"));

		// The flags of the parameters and the constants of the parameters, of the fields and of the property
		static string ReadWithDnlib(byte[] image) {
			using (var module = ModuleDefMD.Load(image)) {
				var type = module.Find("C", false)!;
				var parameters = type.FindMethod("M")!.ParamDefs.Select(a => Describe(a.Name, a.IsIn, a.IsOut, a.IsOptional, a.HasDefault, a.Constant?.Value));
				var others = new[] { "F" + ValueText(type.FindField("F")!.Constant), "G" + ValueText(type.FindField("G")!.Constant), "P" + ValueText(type.FindProperty("P")!.Constant) };
				return string.Join(", ", parameters.Concat(others));
			}
		}
		static string ValueText(Constant? constant) => constant?.Value is null ? "" : " = " + ((int)constant.Value).ToString("X");

		static string ReadWithTheRuntime(byte[] image) {
			using (var loaded = new LoadedAssembly(image)) {
				var type = loaded.Assembly.GetType("C")!;
				var parameters = type.GetMethod("M")!.GetParameters().Select(a =>
					Describe(a.Name!, a.IsIn, a.IsOut, a.IsOptional, (a.Attributes & ParameterAttributes.HasDefault) != 0, a.RawDefaultValue is int ? a.RawDefaultValue : null));
				var others = new[] {
					"F" + ValueText(() => type.GetField("F")!.GetRawConstantValue()),
					"G" + ValueText(() => type.GetField("G")!.GetRawConstantValue()),
					"P" + ValueText(() => type.GetProperty("P")!.GetRawConstantValue()),
				};
				return string.Join(", ", parameters.Concat(others));
			}
		}
		// A member without a constant throws InvalidOperationException
		static string ValueText(Func<object?> getValue) {
			try {
				return getValue() is int value ? " = " + value.ToString("X") : "";
			}
			catch (InvalidOperationException) {
				return "";
			}
		}

		[Theory]
		[InlineData("", "a, b optional default = 22, c optional default = 0, F = 11, G, P", "a, b optional default = 22, c optional default = 0, F = 11, G, P")]
		// The flags of b: 1 = in, 2 = out, 0x10 = optional, 0x1000 = has a default value, 0x2000 = has a marshal
		// type
		[InlineData("Flags of b = 0", "a, b = 22, c optional default = 0, F = 11, G, P", "a, b = 22, c optional default = 0, F = 11, G, P")]
		[InlineData("Flags of b = 16", "a, b optional = 22, c optional default = 0, F = 11, G, P", "a, b optional = 22, c optional default = 0, F = 11, G, P")]
		[InlineData("Flags of b = 4096", "a, b default = 22, c optional default = 0, F = 11, G, P", "a, b default = 22, c optional default = 0, F = 11, G, P")]
		[InlineData("Flags of b = 1", "a, b in = 22, c optional default = 0, F = 11, G, P", "a, b in = 22, c optional default = 0, F = 11, G, P")]
		[InlineData("Flags of b = 2", "a, b out = 22, c optional default = 0, F = 11, G, P", "a, b out = 22, c optional default = 0, F = 11, G, P")]
		[InlineData("Flags of b = 3", "a, b in out = 22, c optional default = 0, F = 11, G, P", "a, b in out = 22, c optional default = 0, F = 11, G, P")]
		[InlineData("Flags of b = 8192", "a, b = 22, c optional default = 0, F = 11, G, P", "a, b = 22, c optional default = 0, F = 11, G, P")]
		[InlineData("Flags of b = 65535", "a, b in out optional default = 22, c optional default = 0, F = 11, G, P", "a, b in out optional default = 22, c optional default = 0, F = 11, G, P")]
		// The parent of the constant of b
		[InlineData("Constant of b = a", "a = 22, b optional default, c optional default = 0, F = 11, G, P", "a = 22, b optional default, c optional default = 0, F = 11, G, P")]
		[InlineData("Constant of b = c", "a, b optional default, c optional default = 22, F = 11, G, P", "a, b optional default, c optional default = 22, F = 11, G, P")]
		// G isn't a constant field: System.Reflection only has the constant of a field that is one
		[InlineData("Constant of b = G", "a, b optional default, c optional default = 0, F = 11, G = 22, P", "a, b optional default, c optional default = 0, F = 11, G, P")]
		[InlineData("Constant of b = P", "a, b optional default, c optional default = 0, F = 11, G, P = 22", "a, b optional default, c optional default = 0, F = 11, G, P = 22")]
		[InlineData("Constant of b = F", "a, b optional default, c optional default = 0, F = 22, G, P", "a, b optional default, c optional default = 0, F = 22, G, P")]
		[InlineData("Constant of b = nothing", "a, b optional default, c optional default = 0, F = 11, G, P", "a, b optional default, c optional default = 0, F = 11, G, P")]
		[InlineData("Constant of b = tag 3 1", "a, b optional default, c optional default = 0, F = 11, G, P", "a, b optional default, c optional default = 0, F = 11, G, P")]
		[InlineData("Constant of b = tag 3 2", "a, b optional default, c optional default = 0, F = 11, G, P", "a, b optional default, c optional default = 0, F = 11, G, P")]
		// A constant but not the flag, and the flag but no constant
		[InlineData("Constant of b = a; Flags of b = 4112", "a = 22, b optional default, c optional default = 0, F = 11, G, P", "a = 22, b optional default, c optional default = 0, F = 11, G, P")]
		[InlineData("Constant of b = nothing; Flags of b = 4096", "a, b default, c optional default = 0, F = 11, G, P", "a, b default, c optional default = 0, F = 11, G, P")]
		public void ParametersAndConstants(string changes, string expectedDnlib, string expectedRuntime) {
			var image = CreateImage(changes);

			ClrReaderComparison.AssertRead(expectedDnlib, ReadWithDnlib(image), expectedRuntime, () => ReadWithTheRuntime(image));
		}
	}
}
