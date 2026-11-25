// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// The base class, the interfaces and the enclosing class of a type are tokens in the TypeDef, the
	/// InterfaceImpl and the NestedClass table. These tests write tokens that a compiler doesn't write (a
	/// table that isn't a type table, a row that doesn't exist, the type itself, the same row twice) and
	/// read the type with dnlib and with the runtime that runs the tests. If the runtime loads the type,
	/// dnlib must read the same base class, interfaces and enclosing class.
	/// </summary>
	public class ClrTypeRuleTests {
		// The TypeDef table: <Module> is row 1, I1 is 2, I2 is 3, Base is 4, Marker is 5, Nested is 6.
		// The InterfaceImpl table: row 1 is I1 of Marker, row 2 is I2 of Marker.
		// The NestedClass table: row 1 is Nested in Marker.
		const string Source = @"
public interface I1 { }
public interface I2 { }
public class Base { }
public class Marker : Base, I1, I2 {
	public class Nested { }
}
";
		static readonly byte[] baseImage = Compile();
		static readonly uint objectRid;

		// The tests use row numbers, so the file must only have the types of the source code: the compiler
		// can add other types (attributes that the target framework doesn't have)
		static byte[] Compile() {
			using (var module = ModuleDefMD.Load(CSharpCompiler.Compile(Source).Image)) {
				var names = new[] { "I1", "I2", "Base", "Marker" };
				foreach (var type in module.Types.Where(a => !a.IsGlobalModuleType && !names.Contains(a.Name.String)).ToArray())
					module.Types.Remove(type);
				module.CustomAttributes.Clear();
				var stream = new MemoryStream();
				module.Write(stream);
				return stream.ToArray();
			}
		}

		static ClrTypeRuleTests() {
			using (var module = ModuleDefMD.Load(baseImage)) {
				Assert.Equal(new[] { "<Module>", "I1", "I2", "Base", "Marker", "Nested" }, module.GetTypes().OrderBy(a => a.Rid).Select(a => a.Name.String).ToArray());
				objectRid = module.GetTypeRefs().Single(a => a.FullName == "System.Object").Rid;
			}
		}

		// "Table.Column of N = value": writes the value to that column of row N. A token of a type is the
		// row * 4 + the table (0: TypeDef, 1: TypeRef, 2: TypeSpec). "Object" is the token of System.Object.
		static byte[] CreateImage(string changes) {
			var patcher = new MetadataPatcher(baseImage);
			if (changes == "nothing")
				return patcher.ToArray();
			foreach (var change in changes.Split(',')) {
				var parts = change.Split(new[] { ' ', '.', '=' }, StringSplitOptions.RemoveEmptyEntries);
				Assert.Equal(5, parts.Length);
				var table = (Table)Enum.Parse(typeof(Table), parts[0]);
				uint value = parts[4] == "Object" ? objectRid * 4 + 1 : uint.Parse(parts[4]);
				patcher.SetCell(table, uint.Parse(parts[3]), parts[1], value);
			}
			return patcher.ToArray();
		}

		const uint MarkerRid = 5, NestedRid = 6;

		// The base class, the interfaces and the enclosing class of Marker and of Nested
		static string Describe(string? markerBase, string[] markerInterfaces, string? markerEnclosing, string? nestedEnclosing) =>
			$"Marker : {markerBase ?? "null"} [{string.Join(" ", markerInterfaces)}] in {markerEnclosing ?? "null"}, Nested in {nestedEnclosing ?? "null"}";

		static string ReadWithDnlib(byte[] image) {
			using (var module = ModuleDefMD.Load(image)) {
				var marker = module.ResolveTypeDef(MarkerRid)!;
				var nested = module.ResolveTypeDef(NestedRid)!;
				return Describe(marker.BaseType?.ReflectionFullName, marker.Interfaces.Select(a => a.Interface?.ReflectionFullName ?? "null").ToArray(),
					marker.DeclaringType?.Name, nested.DeclaringType?.Name);
			}
		}

		static string ReadWithTheRuntime(byte[] image) {
			try {
				using (var loaded = new LoadedAssembly(image)) {
					var module = loaded.Assembly.ManifestModule;
					var marker = module.ResolveType(new MDToken(Table.TypeDef, MarkerRid).ToInt32());
					var nested = module.ResolveType(new MDToken(Table.TypeDef, NestedRid).ToInt32());
					return Describe(marker.BaseType?.FullName, marker.GetInterfaces().Select(a => a.FullName!).OrderBy(a => a, StringComparer.Ordinal).ToArray(),
						marker.DeclaringType?.Name, nested.DeclaringType?.Name);
				}
			}
			catch (Exception ex) {
				return "throws " + ex.GetType().Name;
			}
		}

		// dnlib has an interface as often as the type has rows for it, System.Reflection returns it once
		[Theory]
		[InlineData("nothing", "Marker : Base [I1 I2] in null, Nested in Marker", "Marker : Base [I1 I2] in null, Nested in Marker")]
		// The base class
		[InlineData("TypeDef.Extends of 5 = Object", "Marker : System.Object [I1 I2] in null, Nested in Marker", "Marker : System.Object [I1 I2] in null, Nested in Marker")]
		[InlineData("TypeDef.Extends of 5 = 0", "Marker : null [I1 I2] in null, Nested in Marker", "throws TypeLoadException")]
		[InlineData("TypeDef.Extends of 5 = 1", "Marker : null [I1 I2] in null, Nested in Marker", "throws TypeLoadException")]
		[InlineData("TypeDef.Extends of 5 = 2", "Marker : null [I1 I2] in null, Nested in Marker", "throws TypeLoadException")]
		[InlineData("TypeDef.Extends of 5 = 3", "Marker : null [I1 I2] in null, Nested in Marker", "throws TypeLoadException")]
		[InlineData("TypeDef.Extends of 5 = 19", "Marker : null [I1 I2] in null, Nested in Marker", "throws TypeLoadException")]
		[InlineData("TypeDef.Extends of 5 = 20", "Marker : Marker [I1 I2] in null, Nested in Marker", "throws TypeLoadException")]
		[InlineData("TypeDef.Extends of 5 = 24", "Marker : Marker+Nested [I1 I2] in null, Nested in Marker", "Marker : Marker+Nested [I1 I2] in null, Nested in Marker")]
		[InlineData("TypeDef.Extends of 5 = 8", "Marker : I1 [I1 I2] in null, Nested in Marker", "throws TypeLoadException")]
		[InlineData("TypeDef.Extends of 5 = 400", "Marker : null [I1 I2] in null, Nested in Marker", "throws TypeLoadException")]
		[InlineData("TypeDef.Extends of 5 = 401", "Marker : null [I1 I2] in null, Nested in Marker", "throws TypeLoadException")]
		[InlineData("TypeDef.Extends of 5 = 6", "Marker : null [I1 I2] in null, Nested in Marker", "throws ArgumentException")]
		[InlineData("TypeDef.Extends of 4 = 20", "Marker : Base [I1 I2] in null, Nested in Marker", "throws TypeLoadException")]
		// The interfaces: the Class column is the row of the type, the Interface column is a token
		[InlineData("InterfaceImpl.Interface of 2 = 8", "Marker : Base [I1 I1] in null, Nested in Marker", "Marker : Base [I1] in null, Nested in Marker")]
		[InlineData("InterfaceImpl.Interface of 2 = 16", "Marker : Base [I1 Base] in null, Nested in Marker", "throws TypeLoadException")]
		[InlineData("InterfaceImpl.Interface of 2 = Object", "Marker : Base [I1 System.Object] in null, Nested in Marker", "throws TypeLoadException")]
		[InlineData("InterfaceImpl.Interface of 2 = 0", "Marker : Base [I1 null] in null, Nested in Marker", "throws TypeLoadException")]
		[InlineData("InterfaceImpl.Interface of 2 = 3", "Marker : Base [I1 null] in null, Nested in Marker", "throws TypeLoadException")]
		[InlineData("InterfaceImpl.Interface of 2 = 400", "Marker : Base [I1 null] in null, Nested in Marker", "throws TypeLoadException")]
		[InlineData("InterfaceImpl.Interface of 2 = 20", "Marker : Base [I1 Marker] in null, Nested in Marker", "throws TypeLoadException")]
		[InlineData("InterfaceImpl.Class of 2 = 6", "Marker : Base [I1] in null, Nested in Marker", "Marker : Base [I1] in null, Nested in Marker")]
		[InlineData("InterfaceImpl.Class of 2 = 0", "Marker : Base [I1] in null, Nested in Marker", "Marker : Base [I1] in null, Nested in Marker")]
		[InlineData("InterfaceImpl.Class of 2 = 100", "Marker : Base [I1] in null, Nested in Marker", "Marker : Base [I1] in null, Nested in Marker")]
		[InlineData("InterfaceImpl.Class of 1 = 6", "Marker : Base [] in null, Nested in Marker", "Marker : Base [] in null, Nested in Marker")]
		[InlineData("InterfaceImpl.Class of 1 = 1", "Marker : Base [I2] in null, Nested in Marker", "Marker : Base [I2] in null, Nested in Marker")]
		// The enclosing class: the NestedClass column is the row of the nested type, the EnclosingClass
		// column the row of the type that it is in
		[InlineData("NestedClass.EnclosingClass of 1 = 4", "Marker : Base [I1 I2] in null, Nested in Base", "Marker : Base [I1 I2] in null, Nested in Base")]
		[InlineData("NestedClass.EnclosingClass of 1 = 6", "Marker : Base [I1 I2] in null, Nested in Nested", "throws BadImageFormatException")]
		[InlineData("NestedClass.EnclosingClass of 1 = 0", "Marker : Base [I1 I2] in null, Nested in null", "throws BadImageFormatException")]
		[InlineData("NestedClass.EnclosingClass of 1 = 100", "Marker : Base [I1 I2] in null, Nested in null", "throws BadImageFormatException")]
		[InlineData("NestedClass.NestedClass of 1 = 5", "Marker : Base [I1 I2] in Marker, Nested in null", "throws BadImageFormatException")]
		[InlineData("NestedClass.NestedClass of 1 = 0", "Marker : Base [I1 I2] in null, Nested in null", "throws TypeLoadException")]
		[InlineData("NestedClass.NestedClass of 1 = 100", "Marker : Base [I1 I2] in null, Nested in null", "throws TypeLoadException")]
		[InlineData("NestedClass.NestedClass of 1 = 5,NestedClass.EnclosingClass of 1 = 6", "Marker : Base [I1 I2] in Nested, Nested in null", "throws BadImageFormatException")]
		public void BaseClassInterfacesAndEnclosingClass(string changes, string expectedDnlib, string expectedRuntime) {
			var image = CreateImage(changes);

			ClrReaderComparison.AssertRead(expectedDnlib, ReadWithDnlib(image), expectedRuntime, () => ReadWithTheRuntime(image));
		}
	}
}
