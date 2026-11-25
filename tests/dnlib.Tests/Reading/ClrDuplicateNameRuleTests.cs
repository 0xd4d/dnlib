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

		// Methods Get1 .. Get5 that return their number, and fields F1 .. F5 with their number. CallMethod()
		// and ReadField() use Get1 and F1 with a reference that has the name and the signature of the member
		// (a MemberRef row), not with the row of the member like a compiler does: the CLR then has to find
		// the member with that name.
		const string MemberSource = @"
public static class Marker {
	public static int Get1() => 1;
	public static int Get2() => 2;
	public static int Get3() => 3;
	public static int Get4() => 4;
	public static int Get5() => 5;
	public static int F1 = 1, F2 = 2, F3 = 3, F4 = 4, F5 = 5;
	public static int CallMethod() => Get1();
	public static int ReadField() => F1;
}
";
		static readonly byte[] memberImage = CreateMemberImage();

		static byte[] CreateMemberImage() {
			using (var module = ModuleDefMD.Load(CSharpCompiler.Compile(MemberSource).Image)) {
				var type = module.Find("Marker", isReflectionName: true)!;
				foreach (var method in new[] { type.FindMethod("CallMethod")!, type.FindMethod("ReadField")! }) {
					foreach (var instruction in method.Body!.Instructions) {
						if (instruction.Operand is MethodDef target)
							instruction.Operand = new MemberRefUser(module, target.Name, target.MethodSig, type);
						else if (instruction.Operand is FieldDef field)
							instruction.Operand = new MemberRefUser(module, field.Name, field.FieldSig, type);
					}
				}
				var stream = new MemoryStream();
				module.Write(stream);
				return stream.ToArray();
			}
		}

		// The member 'renamed' (2: Get2 or F2) gets the name of the member 'name', and the reference of
		// CallMethod() or ReadField() gets that name too. What is read is the number of the member that the
		// reference is.
		[Theory]
		[InlineData(Table.Method, 2, 1)]
		[InlineData(Table.Method, 1, 2)]
		[InlineData(Table.Method, 5, 1)]
		[InlineData(Table.Method, 1, 5)]
		[InlineData(Table.Method, 3, 4)]
		[InlineData(Table.Method, 4, 3)]
		[InlineData(Table.Field, 2, 1)]
		[InlineData(Table.Field, 1, 2)]
		[InlineData(Table.Field, 5, 1)]
		[InlineData(Table.Field, 1, 5)]
		[InlineData(Table.Field, 3, 4)]
		[InlineData(Table.Field, 4, 3)]
		public void MemberWithANameThatTwoMembersHave(Table table, int renamed, int name) {
			string prefix = table == Table.Method ? "Get" : "F";
			string caller = table == Table.Method ? "CallMethod" : "ReadField";
			var patcher = new MetadataPatcher(memberImage);
			uint nameOffset = patcher.GetCell(table, patcher.FindRow(table, "Name", prefix + name), "Name");
			// The numbers of the members by their rows, before one is renamed
			var numbers = Enumerable.Range(1, 5).ToDictionary(a => patcher.FindRow(table, "Name", prefix + a), a => a);
			patcher.SetCell(table, patcher.FindRow(table, "Name", prefix + renamed), "Name", nameOffset);
			patcher.SetCell(Table.MemberRef, patcher.FindRow(Table.MemberRef, "Name", prefix + 1), "Name", nameOffset);
			var image = patcher.ToArray();

			string dnlib;
			using (var module = ModuleDefMD.Load(image, ModuleDef.CreateModuleContext())) {
				var method = module.Find("Marker", isReflectionName: true)!.FindMethod(caller)!;
				var reference = (MemberRef)method.Body!.Instructions[0].Operand!;
				Assert.Equal(prefix + name, reference.Name);
				var resolved = table == Table.Method ? (IMemberDef?)reference.ResolveMethod() : reference.ResolveField();
				dnlib = resolved is null ? "not found" : numbers[resolved.Rid].ToString();
			}

			AssertSame(dnlib, () => {
				using (var loaded = new LoadedAssembly(image))
					return loaded.Assembly.GetType("Marker", throwOnError: true)!.GetMethod(caller)!.Invoke(null, null)!.ToString()!;
			});
		}
	}
}
