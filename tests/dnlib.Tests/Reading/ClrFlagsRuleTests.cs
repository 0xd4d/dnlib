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
	/// A type, a field, a method, a property and an event have flags in their row. These tests write every
	/// single flag (also the ones that aren't used), no flag and all flags, and read them with dnlib: it has
	/// the flags of the file. The runtime that runs the tests is asked for the Attributes of the member: it
	/// doesn't load a type with flags that it doesn't allow, and has the flags of the file too if it loads it.
	/// </summary>
	public class ClrFlagsRuleTests {
		// Every kind of member has its own class: the runtime may not load a class with a changed member
		const string Source = @"
public class T { }
public class F { public int X; }
public class M { public void X() { } }
public class P { public int X { get; set; } }
public class E { public event System.EventHandler X; }";
		static readonly byte[] baseImage = CSharpCompiler.Compile(Source).Image;

		static string ReadWithDnlib(byte[] image, string column) {
			using (var module = ModuleDefMD.Load(image)) {
				uint flags = column switch {
					"TypeDef.Flags" => (uint)module.Find("T", false)!.Attributes,
					"Field.Flags" => (uint)module.Find("F", false)!.FindField("X")!.Attributes,
					"Method.Flags" => (uint)module.Find("M", false)!.FindMethod("X")!.Attributes,
					"Method.ImplFlags" => (uint)module.Find("M", false)!.FindMethod("X")!.ImplAttributes,
					"Property.PropFlags" => (uint)module.Find("P", false)!.FindProperty("X")!.Attributes,
					"Event.EventFlags" => (uint)module.Find("E", false)!.FindEvent("X")!.Attributes,
					_ => throw new ArgumentException(column),
				};
				return flags.ToString("X");
			}
		}

		static string ReadWithTheRuntime(byte[] image, string column) {
			const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
			using (var loaded = new LoadedAssembly(image)) {
				var assembly = loaded.Assembly;
				uint flags = column switch {
					"TypeDef.Flags" => (uint)assembly.GetType("T", throwOnError: true)!.Attributes,
					"Field.Flags" => (uint)assembly.GetType("F", throwOnError: true)!.GetFields(All).Single().Attributes,
					"Method.Flags" => (uint)assembly.GetType("M", throwOnError: true)!.GetMethods(All).Single(a => a.Name == "X").Attributes,
					"Method.ImplFlags" => (uint)assembly.GetType("M", throwOnError: true)!.GetMethods(All).Single(a => a.Name == "X").GetMethodImplementationFlags(),
					"Property.PropFlags" => (uint)assembly.GetType("P", throwOnError: true)!.GetProperties(All).Single().Attributes,
					"Event.EventFlags" => (uint)assembly.GetType("E", throwOnError: true)!.GetEvents(All).Single().Attributes,
					_ => throw new ArgumentException(column),
				};
				return flags.ToString("X");
			}
		}

		[Theory]
		// TypeDef.Flags
		[InlineData("TypeDef.Flags", 0x0, "0")]
		[InlineData("TypeDef.Flags", 0x1, "1")]
		[InlineData("TypeDef.Flags", 0x2, "throws TypeLoadException")]
		[InlineData("TypeDef.Flags", 0x4, "throws TypeLoadException")]
		[InlineData("TypeDef.Flags", 0x8, "8")]
		[InlineData("TypeDef.Flags", 0x10, "10")]
		[InlineData("TypeDef.Flags", 0x20, "throws TypeLoadException")]
		[InlineData("TypeDef.Flags", 0x40, "40")]
		[InlineData("TypeDef.Flags", 0x80, "80")]
		[InlineData("TypeDef.Flags", 0x100, "100")]
		[InlineData("TypeDef.Flags", 0x200, "200")]
		[InlineData("TypeDef.Flags", 0x400, "400")]
		[InlineData("TypeDef.Flags", 0x800, "800")]
		[InlineData("TypeDef.Flags", 0x1000, "throws TypeLoadException")]
		[InlineData("TypeDef.Flags", 0x2000, "2000")]
		[InlineData("TypeDef.Flags", 0x4000, "throws TypeLoadException")]
		[InlineData("TypeDef.Flags", 0x8000, "8000")]
		[InlineData("TypeDef.Flags", 0x10000, "10000")]
		[InlineData("TypeDef.Flags", 0x20000, "20000")]
		[InlineData("TypeDef.Flags", 0x40000, "40000")]
		[InlineData("TypeDef.Flags", 0x80000, "80000")]
		[InlineData("TypeDef.Flags", 0x100000, "100000")]
		[InlineData("TypeDef.Flags", 0x200000, "200000")]
		[InlineData("TypeDef.Flags", 0x400000, "400000")]
		[InlineData("TypeDef.Flags", 0x800000, "800000")]
		[InlineData("TypeDef.Flags", 0x1000000, "1000000")]
		[InlineData("TypeDef.Flags", 0x2000000, "2000000")]
		[InlineData("TypeDef.Flags", 0x4000000, "4000000")]
		[InlineData("TypeDef.Flags", 0x8000000, "8000000")]
		[InlineData("TypeDef.Flags", 0x10000000, "10000000")]
		[InlineData("TypeDef.Flags", 0x20000000, "20000000")]
		[InlineData("TypeDef.Flags", 0x40000000, "40000000")]
		[InlineData("TypeDef.Flags", 0x80000000, "80000000")]
		[InlineData("TypeDef.Flags", 0xFFFFFFFF, "throws TypeLoadException")]
		// Field.Flags
		[InlineData("Field.Flags", 0x0, "0")]
		[InlineData("Field.Flags", 0x1, "1")]
		[InlineData("Field.Flags", 0x2, "2")]
		[InlineData("Field.Flags", 0x4, "4")]
		[InlineData("Field.Flags", 0x8, "8")]
		[InlineData("Field.Flags", 0x10, "10")]
		[InlineData("Field.Flags", 0x20, "20")]
		[InlineData("Field.Flags", 0x40, "40")]
		[InlineData("Field.Flags", 0x80, "80")]
		[InlineData("Field.Flags", 0x100, "100")]
		[InlineData("Field.Flags", 0x200, "200")]
		[InlineData("Field.Flags", 0x400, "400")]
		[InlineData("Field.Flags", 0x800, "800")]
		[InlineData("Field.Flags", 0x1000, "1000")]
		[InlineData("Field.Flags", 0x2000, "2000")]
		[InlineData("Field.Flags", 0x4000, "4000")]
		[InlineData("Field.Flags", 0x8000, "8000")]
		[InlineData("Field.Flags", 0xFFFF, "throws TypeLoadException")]
		// Method.Flags
		[InlineData("Method.Flags", 0x0, "0")]
		[InlineData("Method.Flags", 0x1, "1")]
		[InlineData("Method.Flags", 0x2, "2")]
		[InlineData("Method.Flags", 0x4, "4")]
		[InlineData("Method.Flags", 0x8, "8")]
		[InlineData("Method.Flags", 0x10, "throws TypeLoadException")]
		[InlineData("Method.Flags", 0x20, "20")]
		[InlineData("Method.Flags", 0x40, "40")]
		[InlineData("Method.Flags", 0x80, "80")]
		[InlineData("Method.Flags", 0x100, "100")]
		[InlineData("Method.Flags", 0x200, "200")]
		[InlineData("Method.Flags", 0x400, "throws TypeLoadException")]
		[InlineData("Method.Flags", 0x800, "800")]
		[InlineData("Method.Flags", 0x1000, "throws TypeLoadException")]
		[InlineData("Method.Flags", 0x2000, "2000")]
		[InlineData("Method.Flags", 0x4000, "4000")]
		[InlineData("Method.Flags", 0x8000, "8000")]
		[InlineData("Method.Flags", 0xFFFF, "throws TypeLoadException")]
		// Method.ImplFlags
		[InlineData("Method.ImplFlags", 0x0, "0")]
		[InlineData("Method.ImplFlags", 0x1, "throws TypeLoadException")]
		[InlineData("Method.ImplFlags", 0x2, "throws TypeLoadException")]
		[InlineData("Method.ImplFlags", 0x4, "throws TypeLoadException")]
		[InlineData("Method.ImplFlags", 0x8, "8")]
		[InlineData("Method.ImplFlags", 0x10, "10")]
		[InlineData("Method.ImplFlags", 0x20, "20")]
		[InlineData("Method.ImplFlags", 0x40, "40")]
		[InlineData("Method.ImplFlags", 0x80, "80")]
		[InlineData("Method.ImplFlags", 0x100, "100")]
		[InlineData("Method.ImplFlags", 0x200, "200")]
		[InlineData("Method.ImplFlags", 0x400, "400")]
		[InlineData("Method.ImplFlags", 0x800, "800")]
		[InlineData("Method.ImplFlags", 0x1000, "throws TypeLoadException")]
		[InlineData("Method.ImplFlags", 0x2000, "throws TypeLoadException")]
		[InlineData("Method.ImplFlags", 0x4000, "4000")]
		[InlineData("Method.ImplFlags", 0x8000, "8000")]
		[InlineData("Method.ImplFlags", 0xFFFF, "throws TypeLoadException")]
		// Property.PropFlags
		[InlineData("Property.PropFlags", 0x0, "0")]
		[InlineData("Property.PropFlags", 0x1, "1")]
		[InlineData("Property.PropFlags", 0x2, "2")]
		[InlineData("Property.PropFlags", 0x4, "4")]
		[InlineData("Property.PropFlags", 0x8, "8")]
		[InlineData("Property.PropFlags", 0x10, "10")]
		[InlineData("Property.PropFlags", 0x20, "20")]
		[InlineData("Property.PropFlags", 0x40, "40")]
		[InlineData("Property.PropFlags", 0x80, "80")]
		[InlineData("Property.PropFlags", 0x100, "100")]
		[InlineData("Property.PropFlags", 0x200, "200")]
		[InlineData("Property.PropFlags", 0x400, "400")]
		[InlineData("Property.PropFlags", 0x800, "800")]
		[InlineData("Property.PropFlags", 0x1000, "1000")]
		[InlineData("Property.PropFlags", 0x2000, "2000")]
		[InlineData("Property.PropFlags", 0x4000, "4000")]
		[InlineData("Property.PropFlags", 0x8000, "8000")]
		[InlineData("Property.PropFlags", 0xFFFF, "FFFF")]
		// Event.EventFlags
		[InlineData("Event.EventFlags", 0x0, "0")]
		[InlineData("Event.EventFlags", 0x1, "1")]
		[InlineData("Event.EventFlags", 0x2, "2")]
		[InlineData("Event.EventFlags", 0x4, "4")]
		[InlineData("Event.EventFlags", 0x8, "8")]
		[InlineData("Event.EventFlags", 0x10, "10")]
		[InlineData("Event.EventFlags", 0x20, "20")]
		[InlineData("Event.EventFlags", 0x40, "40")]
		[InlineData("Event.EventFlags", 0x80, "80")]
		[InlineData("Event.EventFlags", 0x100, "100")]
		[InlineData("Event.EventFlags", 0x200, "200")]
		[InlineData("Event.EventFlags", 0x400, "400")]
		[InlineData("Event.EventFlags", 0x800, "800")]
		[InlineData("Event.EventFlags", 0x1000, "1000")]
		[InlineData("Event.EventFlags", 0x2000, "2000")]
		[InlineData("Event.EventFlags", 0x4000, "4000")]
		[InlineData("Event.EventFlags", 0x8000, "8000")]
		[InlineData("Event.EventFlags", 0xFFFF, "FFFF")]
		public void FlagsOfAMember(string column, uint flags, string expectedRuntime) {
			var patcher = new MetadataPatcher(baseImage);
			var table = (Table)Enum.Parse(typeof(Table), column.Split('.')[0]);
			string className = column == "TypeDef.Flags" ? "T" : column.Substring(0, 1);
			uint row;
			using (var module = ModuleDefMD.Load(baseImage)) {
				var type = module.Find(className, false)!;
				row = table switch {
					Table.TypeDef => type.Rid,
					Table.Field => type.FindField("X")!.Rid,
					Table.Method => type.FindMethod("X")!.Rid,
					Table.Property => type.FindProperty("X")!.Rid,
					_ => type.FindEvent("X")!.Rid,
				};
			}
			var image = patcher.SetCell(table, row, column.Split('.')[1], flags).ToArray();

			// dnlib has the flags of the file
			ClrReaderComparison.AssertRead(flags.ToString("X"), ReadWithDnlib(image, column), expectedRuntime, () => ReadWithTheRuntime(image, column));
		}
	}
}
