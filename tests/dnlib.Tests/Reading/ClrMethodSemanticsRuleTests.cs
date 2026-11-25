// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// The MethodSemantics table says which methods are the accessors of a property or an event. These
	/// tests write rows that a compiler doesn't write (two getters, no kind, a method of another type, a
	/// property that doesn't exist) and read the accessors with dnlib and with the runtime that runs the
	/// tests. If the runtime returns the accessors, dnlib must have the same ones.
	/// </summary>
	public class ClrMethodSemanticsRuleTests {
		// The Method table: get_P1 is row 1, set_P1 is 2, get_P2 is 3, set_P2 is 4, add_E is 5, remove_E is 6,
		// Other is 7, and Method of the class Second is 8.
		// The MethodSemantics table (the rows are sorted by the Association column): rows 1-2 are add_E and
		// remove_E, rows 3-4 are get_P1 and set_P1, rows 5-6 are get_P2 and set_P2.
		const string Source = @"
public abstract class Marker {
	public static int P1 { get => 1; set { } }
	public static int P2 { get => 2; set { } }
	public static event System.Action E { add { } remove { } }
	public static void Other() { }
}
public static class Second {
	public static int Method() => 0;
}
";
		static readonly byte[] baseImage = Compile();

		// The tests use row numbers, so the file must only have the types of the source code: the compiler
		// can add other types (attributes that the target framework doesn't have)
		static byte[] Compile() {
			using (var module = ModuleDefMD.Load(CSharpCompiler.Compile(Source).Image)) {
				var names = new[] { "Marker", "Second" };
				foreach (var type in module.Types.Where(a => !a.IsGlobalModuleType && !names.Contains(a.Name.String)).ToArray())
					module.Types.Remove(type);
				module.CustomAttributes.Clear();
				// No constructor: the methods then have the rows of the comment above
				var marker = module.Find("Marker", isReflectionName: true)!;
				foreach (var constructor in marker.Methods.Where(a => a.IsConstructor).ToArray())
					marker.Methods.Remove(constructor);
				var stream = new MemoryStream();
				module.Write(stream);
				return stream.ToArray();
			}
		}

		[Fact]
		public void TheFileHasTheRowsThatTheTestsExpect() {
			var patcher = new MetadataPatcher(baseImage);
			using (var module = ModuleDefMD.Load(baseImage)) {
				var methods = Enumerable.Range(1, 8).Select(a => module.ResolveMethod((uint)a)!.Name.String).ToArray();
				Assert.Equal(new[] { "get_P1", "set_P1", "get_P2", "set_P2", "add_E", "remove_E", "Other", "Method" }, methods);
				var rows = Enumerable.Range(1, 6).Select(a => $"{patcher.GetCell(Table.MethodSemantics, (uint)a, "Semantic")} {patcher.GetCell(Table.MethodSemantics, (uint)a, "Method")} {patcher.GetCell(Table.MethodSemantics, (uint)a, "Association")}").ToArray();
				Assert.Equal(new[] { "8 5 2", "16 6 2", "2 1 3", "1 2 3", "2 3 5", "1 4 5" }, rows);
			}
		}

		// "Table.Column of N = value": writes the value to that column of row N
		static byte[] CreateImage(string change) {
			var patcher = new MetadataPatcher(baseImage);
			if (change == "nothing")
				return patcher.ToArray();
			var parts = change.Split(new[] { ' ', '.', '=' }, StringSplitOptions.RemoveEmptyEntries);
			Assert.Equal(5, parts.Length);
			patcher.SetCell((Table)Enum.Parse(typeof(Table), parts[0]), uint.Parse(parts[3]), parts[1], uint.Parse(parts[4]));
			return patcher.ToArray();
		}

		// "P1[get_P1,set_P1]": the getter and the setter of a property, or the add and the remove method of
		// an event ("-" if it has none), and then its other methods
		static string Describe(string name, string? first, string? second, System.Collections.Generic.IEnumerable<string> others) =>
			$"{name}[{string.Join(",", new[] { first ?? "-", second ?? "-" }.Concat(others))}]";

		static string[] ReadWithDnlib(byte[] image) {
			using (var module = ModuleDefMD.Load(image)) {
				var type = module.Find("Marker", isReflectionName: true)!;
				var properties = type.Properties.Select(a => Describe(a.Name, a.GetMethod?.Name, a.SetMethod?.Name, a.OtherMethods.Select(b => b.Name.String)));
				var events = type.Events.Select(a => Describe(a.Name, a.AddMethod?.Name, a.RemoveMethod?.Name,
					(a.InvokeMethod is null ? new string[0] : new[] { "raise:" + a.InvokeMethod.Name }).Concat(a.OtherMethods.Select(b => b.Name.String))));
				return properties.Concat(events).ToArray();
			}
		}

		const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

		static string[] ReadWithTheRuntime(byte[] image) {
			try {
				using (var loaded = new LoadedAssembly(image)) {
					var type = loaded.Assembly.GetType("Marker", throwOnError: true)!;
					var properties = type.GetProperties(Declared).OrderBy(a => a.MetadataToken).Select(a => {
						var getter = a.GetGetMethod(true);
						var setter = a.GetSetMethod(true);
						return Describe(a.Name, getter?.Name, setter?.Name, a.GetAccessors(true).Where(b => b != getter && b != setter).Select(b => b.Name));
					});
					var events = type.GetEvents(Declared).OrderBy(a => a.MetadataToken).Select(a => {
						var raise = a.GetRaiseMethod(true);
						return Describe(a.Name, a.GetAddMethod(true)?.Name, a.GetRemoveMethod(true)?.Name,
							(raise is null ? new string[0] : new[] { "raise:" + raise.Name }).Concat(a.GetOtherMethods(true).Select(b => b.Name)));
					});
					return properties.Concat(events).ToArray();
				}
			}
			catch (Exception ex) {
				return new[] { "throws " + ex.GetType().Name };
			}
		}

		static readonly string[] normal = { "P1[get_P1,set_P1]", "P2[get_P2,set_P2]", "E[add_E,remove_E]" };

		// What was read, without the properties and events that have what the compiler gave them
		static string OnlyChanges(string[] read) {
			if (read.Length != normal.Length)
				return string.Join(" ", read);
			var changes = read.Where((a, i) => a != normal[i]).ToArray();
			return changes.Length == 0 ? "normal" : string.Join(" ", changes);
		}

		// Fixed in this commit: if a property or an event has two methods of a kind, System.Reflection uses
		// the last row and dnlib used the first one. And a row whose Semantic value isn't one of the six
		// kinds (no bit, more than one bit, an unknown bit) is an other method, dnlib ignored the row.
		//
		// The Semantic column: 1 = setter, 2 = getter, 4 = other, 8 = add, 16 = remove, 32 = raise.
		// The Association column: the row of the event * 2, or the row of the property * 2 + 1.
		[Theory]
		[InlineData("nothing", "normal", "normal")]
		[InlineData("MethodSemantics.Semantic of 3 = 1", "P1[-,set_P1]", "P1[-,set_P1]")]
		[InlineData("MethodSemantics.Semantic of 3 = 3", "P1[-,set_P1,get_P1]", "P1[-,set_P1,get_P1]")]
		[InlineData("MethodSemantics.Semantic of 3 = 0", "P1[-,set_P1,get_P1]", "P1[-,set_P1,get_P1]")]
		[InlineData("MethodSemantics.Semantic of 3 = 4", "P1[-,set_P1,get_P1]", "P1[-,set_P1,get_P1]")]
		[InlineData("MethodSemantics.Semantic of 3 = 8", "P1[-,set_P1]", "P1[-,set_P1]")]
		[InlineData("MethodSemantics.Semantic of 3 = 64", "P1[-,set_P1,get_P1]", "P1[-,set_P1,get_P1]")]
		[InlineData("MethodSemantics.Semantic of 3 = 34", "P1[-,set_P1,get_P1]", "P1[-,set_P1,get_P1]")]
		[InlineData("MethodSemantics.Semantic of 4 = 2", "P1[set_P1,-]", "P1[set_P1,-]")]
		[InlineData("MethodSemantics.Semantic of 4 = 0", "P1[get_P1,-,set_P1]", "P1[get_P1,-,set_P1]")]
		[InlineData("MethodSemantics.Semantic of 1 = 16", "E[-,remove_E]", "E[-,remove_E]")]
		[InlineData("MethodSemantics.Semantic of 1 = 32", "E[-,remove_E,raise:add_E]", "E[-,remove_E,raise:add_E]")]
		[InlineData("MethodSemantics.Semantic of 1 = 4", "E[-,remove_E,add_E]", "E[-,remove_E,add_E]")]
		[InlineData("MethodSemantics.Semantic of 1 = 2", "E[-,remove_E]", "E[-,remove_E]")]
		[InlineData("MethodSemantics.Semantic of 1 = 24", "E[-,remove_E,add_E]", "E[-,remove_E,add_E]")]
		[InlineData("MethodSemantics.Semantic of 1 = 0", "E[-,remove_E,add_E]", "E[-,remove_E,add_E]")]
		[InlineData("MethodSemantics.Semantic of 2 = 8", "E[remove_E,-]", "E[remove_E,-]")]
		[InlineData("MethodSemantics.Method of 3 = 7", "P1[Other,set_P1]", "P1[Other,set_P1]")]
		[InlineData("MethodSemantics.Method of 3 = 8", "P1[-,set_P1]", "throws ArgumentException")]
		[InlineData("MethodSemantics.Method of 3 = 0", "P1[-,set_P1]", "P1[-,set_P1]")]
		[InlineData("MethodSemantics.Method of 3 = 100", "P1[-,set_P1]", "throws ArgumentOutOfRangeException")]
		[InlineData("MethodSemantics.Method of 3 = 3", "P1[get_P2,set_P1]", "P1[get_P2,set_P1]")]
		[InlineData("MethodSemantics.Method of 3 = 2", "P1[set_P1,set_P1]", "P1[set_P1,set_P1]")]
		[InlineData("MethodSemantics.Association of 3 = 5", "P1[-,-] P2[get_P1,-]", "P1[-,-] P2[get_P1,-]")]
		[InlineData("MethodSemantics.Association of 3 = 2", "P1[-,set_P1]", "P1[-,set_P1]")]
		[InlineData("MethodSemantics.Association of 3 = 100", "P1[-,-] P2[-,-]", "P1[-,-] P2[-,-]")]
		[InlineData("MethodSemantics.Association of 3 = 0", "P1[-,set_P1] E[-,-]", "P1[-,set_P1] E[-,-]")]
		[InlineData("MethodSemantics.Association of 4 = 5", "P1[get_P1,-]", "P1[get_P1,-]")]
		[InlineData("MethodSemantics.Association of 2 = 3", "E[add_E,-]", "E[add_E,-]")]
		[InlineData("MethodSemantics.Association of 6 = 100", "P2[get_P2,-]", "P2[get_P2,-]")]
		public void Accessors(string change, string expectedDnlib, string expectedRuntime) {
			var image = CreateImage(change);

			ClrReaderComparison.AssertRead(expectedDnlib, OnlyChanges(ReadWithDnlib(image)), expectedRuntime, () => OnlyChanges(ReadWithTheRuntime(image)));
		}

		// The getter of a property with two getters is the same one after the file is written
		[Fact]
		public void LastAccessorOfAKindIsStillTheAccessorAfterAWrite() {
			using (var module = ModuleDefMD.Load(CreateImage("MethodSemantics.Semantic of 4 = 2"))) {
				var property = module.Find("Marker", isReflectionName: true)!.FindProperty("P1")!;
				Assert.Equal(new[] { "get_P1", "set_P1" }, property.GetMethods.Select(a => a.Name.String).ToArray());
				Assert.Equal("set_P1", property.GetMethod!.Name);

				var stream = new MemoryStream();
				module.Write(stream);
				var image = stream.ToArray();
				Assert.Equal("P1[set_P1,-]", OnlyChanges(ReadWithDnlib(image)));
				if (ClrReaderComparison.IsDotNet)
					Assert.Equal("P1[set_P1,-]", OnlyChanges(ReadWithTheRuntime(image)));
			}
		}

		// GetMethod and SetMethod are the last method of GetMethods and SetMethods, also when they're set
		[Fact]
		public void GetMethodAndSetMethodAreTheLastMethodOfTheirList() {
			var module = new ModuleDefUser("Test.dll");
			MethodDef Create(string name) => new MethodDefUser(name, MethodSig.CreateStatic(module.CorLibTypes.Void));
			var property = new PropertyDefUser("P", PropertySig.CreateStatic(module.CorLibTypes.Int32));
			Assert.Null(property.GetMethod);
			Assert.Null(property.SetMethod);

			property.GetMethods.Add(Create("get1"));
			property.GetMethods.Add(Create("get2"));
			property.SetMethods.Add(Create("set1"));
			property.SetMethods.Add(Create("set2"));
			Assert.Equal("get2", property.GetMethod!.Name);
			Assert.Equal("set2", property.SetMethod!.Name);

			property.GetMethod = Create("get3");
			property.SetMethod = Create("set3");
			Assert.Equal(new[] { "get1", "get3" }, property.GetMethods.Select(a => a.Name.String).ToArray());
			Assert.Equal(new[] { "set1", "set3" }, property.SetMethods.Select(a => a.Name.String).ToArray());

			property.GetMethod = null;
			property.SetMethod = null;
			Assert.Empty(property.GetMethods);
			Assert.Empty(property.SetMethods);
		}
	}
}
