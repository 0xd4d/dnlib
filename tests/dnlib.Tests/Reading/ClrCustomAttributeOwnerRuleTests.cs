// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// The owner of a custom attribute is the Parent column of its row: a row number and the kind of the
	/// owner (0 = method, 1 = field, 2 = TypeRef, 3 = type, 4 = parameter, ... 21 = MethodSpec). These tests
	/// give an attribute every kind of owner and read with dnlib who has the attribute. The runtime that runs
	/// the tests is asked the same for the owners that System.Reflection has. If it finds the attribute, dnlib
	/// must find it at the same owner.
	/// </summary>
	public class ClrCustomAttributeOwnerRuleTests {
		const string Source = @"
using System;
public class Mark : Attribute { }
public interface I { }
[Mark]
public class Marker<T> : I where T : IDisposable {
	public int F;
	public int P { get; set; }
	public event EventHandler E;
	public void M(int parameter) { E?.Invoke(null, null); Console.WriteLine(); }
}";
		static readonly byte[] baseImage = CSharpCompiler.Compile(Source).Image;

		// "kind row": the kind of the owner (its number in the Parent column) and its row number. The row is a
		// number or a name: the row of that member of Marker.
		static byte[] CreateImage(uint kind, string row) {
			var patcher = new MetadataPatcher(baseImage);
			using (var module = ModuleDefMD.Load(baseImage)) {
				var type = module.Find("Marker`1", false)!;
				uint attributeRow = Enumerable.Range(1, (int)patcher.GetRowCount(Table.CustomAttribute)).Select(a => (uint)a)
					.Single(a => patcher.GetCell(Table.CustomAttribute, a, "Parent") == (type.Rid << 5 | 3));
				uint rid = row switch {
					"Marker" => type.Rid,
					"F" => type.FindField("F")!.Rid,
					"M" => type.FindMethod("M")!.Rid,
					"parameter" => type.FindMethod("M")!.ParamDefs.Single().Rid,
					"P" => type.FindProperty("P")!.Rid,
					"E" => type.FindEvent("E")!.Rid,
					"T" => type.GenericParameters.Single().Rid,
					_ => uint.Parse(row),
				};
				patcher.SetCell(Table.CustomAttribute, attributeRow, "Parent", (rid << 5) | kind);
			}
			// The rows are sorted by their parent: a reader doesn't find a row if they aren't
			patcher.SortRows(Table.CustomAttribute, "Parent");
			return patcher.ToArray();
		}

		static bool HasMark(IHasCustomAttribute? owner) => owner is not null && owner.CustomAttributes.Any(a => a.AttributeType?.Name == "Mark");

		// Everything in the module that has the attribute
		static string ReadWithDnlib(byte[] image) {
			using (var module = ModuleDefMD.Load(image)) {
				var type = module.Find("Marker`1", false)!;
				var method = type.FindMethod("M")!;
				var owners = new List<KeyValuePair<string, IHasCustomAttribute?>> {
					new KeyValuePair<string, IHasCustomAttribute?>("type", type),
					new KeyValuePair<string, IHasCustomAttribute?>("field", type.FindField("F")),
					new KeyValuePair<string, IHasCustomAttribute?>("method", method),
					new KeyValuePair<string, IHasCustomAttribute?>("parameter", method.ParamDefs.Single()),
					new KeyValuePair<string, IHasCustomAttribute?>("property", type.FindProperty("P")),
					new KeyValuePair<string, IHasCustomAttribute?>("event", type.FindEvent("E")),
					new KeyValuePair<string, IHasCustomAttribute?>("generic parameter", type.GenericParameters.Single()),
					new KeyValuePair<string, IHasCustomAttribute?>("module", module),
					new KeyValuePair<string, IHasCustomAttribute?>("assembly", module.Assembly),
					new KeyValuePair<string, IHasCustomAttribute?>("constraint", type.GenericParameters.Single().GenericParamConstraints.Single()),
					new KeyValuePair<string, IHasCustomAttribute?>("interface", type.Interfaces.Single()),
				};
				foreach (var typeRef in module.GetTypeRefs())
					owners.Add(new KeyValuePair<string, IHasCustomAttribute?>("a TypeRef", typeRef));
				foreach (var memberRef in module.GetMemberRefs())
					owners.Add(new KeyValuePair<string, IHasCustomAttribute?>("a MemberRef", memberRef));
				foreach (var assemblyRef in module.GetAssemblyRefs())
					owners.Add(new KeyValuePair<string, IHasCustomAttribute?>("an AssemblyRef", assemblyRef));
				var found = owners.Where(a => HasMark(a.Value)).Select(a => a.Key).Distinct().ToArray();
				return found.Length == 0 ? "nothing" : string.Join(", ", found);
			}
		}

		static bool HasMark(IList<CustomAttributeData> attributes) => attributes.Any(a => a.Constructor.DeclaringType!.Name == "Mark");

		static string ReadWithTheRuntime(byte[] image) {
			using (var loaded = new LoadedAssembly(image)) {
				var type = loaded.Assembly.GetType("Marker`1")!;
				var method = type.GetMethod("M")!;
				var owners = new[] {
					new KeyValuePair<string, Func<IList<CustomAttributeData>>>("type", () => type.GetCustomAttributesData()),
					new KeyValuePair<string, Func<IList<CustomAttributeData>>>("field", () => type.GetField("F")!.GetCustomAttributesData()),
					new KeyValuePair<string, Func<IList<CustomAttributeData>>>("method", () => method.GetCustomAttributesData()),
					new KeyValuePair<string, Func<IList<CustomAttributeData>>>("parameter", () => method.GetParameters()[0].GetCustomAttributesData()),
					new KeyValuePair<string, Func<IList<CustomAttributeData>>>("property", () => type.GetProperty("P")!.GetCustomAttributesData()),
					new KeyValuePair<string, Func<IList<CustomAttributeData>>>("event", () => type.GetEvent("E")!.GetCustomAttributesData()),
					new KeyValuePair<string, Func<IList<CustomAttributeData>>>("generic parameter", () => type.GetGenericArguments()[0].GetCustomAttributesData()),
					new KeyValuePair<string, Func<IList<CustomAttributeData>>>("module", () => loaded.Assembly.ManifestModule.GetCustomAttributesData()),
					new KeyValuePair<string, Func<IList<CustomAttributeData>>>("assembly", () => loaded.Assembly.GetCustomAttributesData()),
				};
				var found = owners.Where(a => HasMark(a.Value())).Select(a => a.Key).ToArray();
				return found.Length == 0 ? "nothing" : string.Join(", ", found);
			}
		}

		[Theory]
		[InlineData(3, "Marker", "type", "type")]
		[InlineData(0, "M", "method", "method")]
		[InlineData(1, "F", "field", "field")]
		[InlineData(4, "parameter", "parameter", "parameter")]
		[InlineData(9, "P", "property", "property")]
		[InlineData(10, "E", "event", "event")]
		[InlineData(19, "T", "generic parameter", "generic parameter")]
		[InlineData(7, "1", "module", "module")]
		[InlineData(14, "1", "assembly", "assembly")]
		// Owners that System.Reflection doesn't have
		[InlineData(2, "1", "a TypeRef", "nothing")]
		[InlineData(5, "1", "interface", "nothing")]
		[InlineData(6, "1", "a MemberRef", "nothing")]
		[InlineData(15, "1", "an AssemblyRef", "nothing")]
		[InlineData(20, "1", "constraint", "nothing")]
		// Owners that the file doesn't have: a permission set, a signature, a ModuleRef, a TypeSpec, a file,
		// an exported type, a resource, a MethodSpec
		[InlineData(8, "1", "nothing", "nothing")]
		[InlineData(11, "1", "nothing", "nothing")]
		[InlineData(12, "1", "nothing", "nothing")]
		[InlineData(13, "1", "nothing", "nothing")]
		[InlineData(16, "1", "nothing", "nothing")]
		[InlineData(17, "1", "nothing", "nothing")]
		[InlineData(18, "1", "nothing", "nothing")]
		[InlineData(21, "1", "nothing", "nothing")]
		// Kinds that aren't used, and rows that don't exist
		[InlineData(22, "1", "nothing", "nothing")]
		[InlineData(31, "1", "nothing", "nothing")]
		[InlineData(3, "0", "nothing", "nothing")]
		[InlineData(3, "1000", "nothing", "nothing")]
		[InlineData(7, "0", "nothing", "nothing")]
		[InlineData(7, "2", "nothing", "nothing")]
		[InlineData(14, "0", "nothing", "nothing")]
		[InlineData(14, "2", "nothing", "nothing")]
		public void OwnerOfAnAttribute(uint kind, string row, string expectedDnlib, string expectedRuntime) {
			var image = CreateImage(kind, row);

			// Only .NET is asked: the test run on .NET Framework never ended when these tests were added, and which
			// of their cases it doesn't return from isn't known yet
			ClrReaderComparison.AssertReadAskOnlyDotNet(expectedDnlib, ReadWithDnlib(image), expectedRuntime, () => ReadWithTheRuntime(image));
		}
	}
}
