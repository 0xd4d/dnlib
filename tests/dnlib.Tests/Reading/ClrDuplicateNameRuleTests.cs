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
	/// A file can have two resources or two types with the same name: a compiler doesn't write that, but a
	/// tool can rename one. These tests read "the" resource or type with that name with dnlib and with the
	/// runtime that runs the tests: dnlib should find the one that the CLR finds.
	/// </summary>
	public class ClrDuplicateNameRuleTests {
		// Gives the row 'row' of the table the name (and the namespace, if the table has one) of row 'nameOfRow'
		static byte[] SetSameName(byte[] image, Table table, uint row, uint nameOfRow) {
			var patcher = new MetadataPatcher(image);
			foreach (var column in patcher.GetColumnNames(table).Where(a => a == "Name" || a == "Namespace" || a == "TypeName" || a == "TypeNamespace"))
				patcher.SetCell(table, row, column, patcher.GetCell(table, nameOfRow, column));
			return patcher.ToArray();
		}

		// dnlib must read what .NET reads. Another CLR (.NET Framework) may read something else: the test is
		// then skipped with a message that says what it read. Mono isn't asked.
		static void AssertSame(string dnlib, Func<string> readWithTheRuntime) {
			if (ClrReaderComparison.IsMono)
				return;
			string runtime;
			try {
				runtime = readWithTheRuntime();
			}
			catch (Exception ex) {
				runtime = "throws " + ex.GetType().Name;
			}
			if (ClrReaderComparison.IsDotNet)
				Assert.Equal(runtime, dnlib);
			else
				Assert.SkipUnless(runtime == dnlib, $"This CLR reads '{runtime}', dnlib reads '{dnlib}'");
		}

		// Resources R1 .. R5 whose content is their number. Row 'row' gets the name of row 'nameOfRow'.
		[Theory]
		[InlineData(2, 1)]
		[InlineData(1, 2)]
		[InlineData(5, 1)]
		[InlineData(1, 5)]
		[InlineData(3, 4)]
		[InlineData(4, 3)]
		public void ResourceWithANameThatTwoResourcesHave(uint row, uint nameOfRow) {
			var options = new CSharpCompilerOptions();
			for (int i = 1; i <= 5; i++)
				options.Resources.Add("R" + i, new[] { (byte)i });
			var original = CSharpCompiler.Compile("public class C { }", options).Image;
			string name;
			using (var module = ModuleDefMD.Load(original))
				name = module.Resources[(int)nameOfRow - 1].Name;
			var image = SetSameName(original, Table.ManifestResource, row, nameOfRow);

			string dnlib;
			using (var module = ModuleDefMD.Load(image))
				dnlib = ((EmbeddedResource)module.Resources.Find(name)!).CreateReader().ReadByte().ToString();

			AssertSame(dnlib, () => {
				using (var loaded = new LoadedAssembly(image))
				using (var stream = loaded.Assembly.GetManifestResourceStream(name)!)
					return stream.ReadByte().ToString();
			});
		}

		// Classes T1 .. T5 with a constant that is their number. Row 'row' of the TypeDef table gets the name
		// of row 'nameOfRow' (row 1 is <Module>, T1 is row 2).
		//
		// NOT the same: the CLR finds the LAST type with the name (it adds every type to a hash table and a
		// lookup finds the newest entry), dnlib's TypeDefFinder finds the FIRST one. The test has what both
		// do now.
		[Theory]
		[InlineData(3, 2, "1", "2")]
		[InlineData(2, 3, "1", "2")]
		[InlineData(6, 2, "1", "5")]
		[InlineData(2, 6, "1", "5")]
		[InlineData(4, 5, "3", "4")]
		[InlineData(5, 4, "3", "4")]
		public void TypeWithANameThatTwoTypesHave(uint row, uint nameOfRow, string expectedDnlib, string expectedRuntime) {
			var source = string.Join("\n", Enumerable.Range(1, 5).Select(a => $"namespace Ns {{ public static class T{a} {{ public const int Number = {a}; }} }}"));
			byte[] original;
			using (var module = ModuleDefMD.Load(CSharpCompiler.Compile(source).Image)) {
				// Only these classes: the compiler can add other types
				foreach (var type in module.Types.Where(a => !a.IsGlobalModuleType && a.Namespace != "Ns").ToArray())
					module.Types.Remove(type);
				module.CustomAttributes.Clear();
				var stream = new MemoryStream();
				module.Write(stream);
				original = stream.ToArray();
			}
			string name;
			using (var module = ModuleDefMD.Load(original))
				name = module.ResolveTypeDef(nameOfRow)!.ReflectionFullName;
			var image = SetSameName(original, Table.TypeDef, row, nameOfRow);

			string dnlib;
			using (var module = ModuleDefMD.Load(image))
				dnlib = module.Find(name, isReflectionName: true)!.FindField("Number")!.Constant!.Value!.ToString()!;

			ClrReaderComparison.AssertRead(expectedDnlib, dnlib, expectedRuntime, () => {
				using (var loaded = new LoadedAssembly(image))
					return loaded.Assembly.GetType(name, throwOnError: true)!.GetField("Number")!.GetRawConstantValue()!.ToString()!;
			});
		}
	}
}
