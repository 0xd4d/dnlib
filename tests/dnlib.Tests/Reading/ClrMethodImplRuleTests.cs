// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// A row of the MethodImpl table says that a method of a class (the body) implements another method (the
	/// declaration), eg. a method of an interface that the class implements explicitly. These tests write
	/// classes, bodies and declarations that a compiler doesn't write and read with dnlib which method
	/// implements which one. The runtime that runs the tests is asked which methods of C implement the methods
	/// of the interface. If it has an answer, dnlib must have the same one.
	/// </summary>
	public class ClrMethodImplRuleTests {
		// The MethodImpl table has two rows: C.I.A implements I.A, and C.I.B implements I.B
		const string Source = @"
public interface I { string A(); string B(); }
public class C : I {
	string I.A() => null;
	string I.B() => null;
	public virtual string X() => null;
}
public class D {
	public virtual string Y() => null;
}";
		static readonly byte[] baseImage = CSharpCompiler.Compile(Source).Image;

		// "Column = value", separated by "; ": a column of the row of C.I.A (Class, MethodBody,
		// MethodDeclaration). A value is a number, the name of a type or "Type::Method".
		static byte[] CreateImage(string changes) {
			var patcher = new MetadataPatcher(baseImage);
			using (var module = ModuleDefMD.Load(baseImage)) {
				// A method is a coded token: the row number and 0 = MethodDef, 1 = MemberRef
				uint GetValue(string value) =>
					char.IsDigit(value[0]) ? uint.Parse(value) :
					value.Contains("::") ? module.Find(value.Split(new[] { "::" }, StringSplitOptions.None)[0], false)!.FindMethod(value.Split(new[] { "::" }, StringSplitOptions.None)[1])!.Rid << 1 :
					module.Find(value, false)!.Rid;
				uint body = GetValue("C::I.A");
				uint row = Enumerable.Range(1, 2).Select(a => (uint)a).Single(a => patcher.GetCell(Table.MethodImpl, a, "MethodBody") == body);
				foreach (var change in changes.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries)) {
					var parts = change.Split(new[] { " = " }, StringSplitOptions.None);
					patcher.SetCell(Table.MethodImpl, row, parts[0], GetValue(parts[1]));
				}
			}
			// The rows of the MethodImpl table are sorted by their class: a reader doesn't find a row if they aren't
			patcher.SortRows(Table.MethodImpl, "Class");
			return patcher.ToArray();
		}

		static string Name(IMethod? method) => method is null ? "null" : method.DeclaringType?.Name + "::" + method.Name;

		// Every method of C and D that implements a method: "body => declaration"
		static string ReadWithDnlib(byte[] image) {
			using (var module = ModuleDefMD.Load(image)) {
				var all = from type in new[] { module.Find("C", false)!, module.Find("D", false)! }
						  from method in type.Methods
						  from methodOverride in method.Overrides
						  select Name(methodOverride.MethodBody) + " => " + Name(methodOverride.MethodDeclaration);
				return string.Join(", ", all.OrderBy(a => a, StringComparer.Ordinal));
			}
		}

		// The methods of C that implement the methods of I
		static string ReadWithTheRuntime(byte[] image) {
			using (var loaded = new LoadedAssembly(image)) {
				var map = loaded.Assembly.GetType("C")!.GetInterfaceMap(loaded.Assembly.GetType("I")!);
				var all = map.InterfaceMethods.Select((a, i) => map.TargetMethods[i].DeclaringType!.Name + "::" + map.TargetMethods[i].Name + " => I::" + a.Name);
				return string.Join(", ", all.OrderBy(a => a, StringComparer.Ordinal));
			}
		}

		[Theory]
		[InlineData("", "C::I.A => I::A, C::I.B => I::B", "C::I.A => I::A, C::I.B => I::B")]
		// The body
		[InlineData("MethodBody = C::I.B", "C::I.B => I::A, C::I.B => I::B", "C::I.B => I::A, C::I.B => I::B")]
		[InlineData("MethodBody = C::X", "C::I.B => I::B, C::X => I::A", "C::I.B => I::B, C::X => I::A")]
		[InlineData("MethodBody = D::Y", "C::I.B => I::B", "throws TypeLoadException")]
		[InlineData("MethodBody = I::A", "C::I.B => I::B", "throws TypeLoadException")]
		[InlineData("MethodBody = 0", "C::I.B => I::B", "throws TypeLoadException")]
		[InlineData("MethodBody = 1", "C::I.B => I::B", "throws TypeLoadException")]
		[InlineData("MethodBody = 2000", "C::I.B => I::B", "throws TypeLoadException")]
		// The declaration
		[InlineData("MethodDeclaration = I::B", "C::I.A => I::B, C::I.B => I::B", "throws TypeLoadException")]
		[InlineData("MethodDeclaration = C::X", "C::I.A => C::X, C::I.B => I::B", "throws TypeLoadException")]
		[InlineData("MethodDeclaration = C::I.A", "C::I.A => C::I.A, C::I.B => I::B", "throws TypeLoadException")]
		[InlineData("MethodDeclaration = D::Y", "C::I.A => D::Y, C::I.B => I::B", "throws TypeLoadException")]
		[InlineData("MethodDeclaration = 0", "C::I.B => I::B", "throws TypeLoadException")]
		[InlineData("MethodDeclaration = 1", "C::I.B => I::B", "throws TypeLoadException")]
		[InlineData("MethodDeclaration = 2000", "C::I.B => I::B", "throws TypeLoadException")]
		// The class of the row
		[InlineData("Class = D", "C::I.B => I::B", "throws TypeLoadException")]
		[InlineData("Class = I", "C::I.B => I::B", "throws TypeLoadException")]
		[InlineData("Class = 0", "C::I.B => I::B", "throws TypeLoadException")]
		[InlineData("Class = 1000", "C::I.B => I::B", "throws TypeLoadException")]
		[InlineData("Class = D; MethodBody = D::Y", "C::I.B => I::B, D::Y => I::A", "throws TypeLoadException")]
		public void MethodThatImplementsAnotherMethod(string changes, string expectedDnlib, string expectedRuntime) {
			var image = CreateImage(changes);

			// Only .NET is asked: the test run on .NET Framework never ended when these tests were added, and which
			// of their cases it doesn't return from isn't known yet
			ClrReaderComparison.AssertReadAskOnlyDotNet(expectedDnlib, ReadWithDnlib(image), expectedRuntime, () => ReadWithTheRuntime(image));
		}
	}
}
