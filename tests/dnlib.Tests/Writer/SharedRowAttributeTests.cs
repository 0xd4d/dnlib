// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// A TypeSpec, MemberRef, MethodSpec or StandAloneSig row that uses a generic parameter (<c>!0</c>,
	/// <c>!!0</c>) can be used by methods of several generic types. dnlib creates an object for the row for each
	/// of them, since the generic parameter is another one every time. The row's custom attributes belong to the
	/// row: all of those objects must share them, and they must be written once.
	/// </summary>
	public class SharedRowAttributeTests {
		// The C# compiler creates one row for each of 'Generic<!0>', 'Generic<!0>::Method()' and
		// 'Helper::Create<!0>()', and the methods of First<T> and Second<U> use the same rows
		const string Source = @"
public class Generic<T> {
	public static void Method() { }
}

public static class Helper {
	public static void Create<T>() { }
}

public class First<T> {
	public static object Use() {
		Generic<T>.Method();
		Helper.Create<T>();
		T local = default(T);
		return local;
	}
}

public class Second<U> {
	public static object Use() {
		Generic<U>.Method();
		Helper.Create<U>();
		U local = default(U);
		return local;
	}
}
";

		static MethodDef GetUseMethod(ModuleDef module, string typeName) => module.Find(typeName, isReflectionName: true)!.FindMethod("Use")!;

		// The objects that dnlib created for the 4 rows when it read a 'Use' method
		sealed class RowObjects {
			public readonly MemberRef MemberRef;
			public readonly TypeSpec TypeSpec;
			public readonly MethodSpec MethodSpec;
			public readonly StandAloneSig StandAloneSig;

			public RowObjects(ModuleDefMD module, string typeName) {
				var method = GetUseMethod(module, typeName);
				var operands = method.Body!.Instructions.Where(a => a.OpCode == OpCodes.Call).Select(a => a.Operand).ToList();
				MemberRef = operands.OfType<MemberRef>().Single();
				TypeSpec = (TypeSpec)MemberRef.Class!;
				MethodSpec = operands.OfType<MethodSpec>().Single();
				// The signature of the method's locals
				StandAloneSig = module.ResolveStandAloneSig(MDToken.ToRID(method.Body.LocalVarSigTok), new GenericParamContext(method.DeclaringType))!;
			}

			public IHasCustomAttribute[] All => new IHasCustomAttribute[] { MemberRef, TypeSpec, MethodSpec, StandAloneSig };
		}

		static CustomAttribute CreateAttribute(ModuleDef module) {
			var attributeType = module.CorLibTypes.GetTypeRef("System", "ObsoleteAttribute");
			var constructor = new MemberRefUser(module, ".ctor", MethodSig.CreateInstance(module.CorLibTypes.Void), attributeType);
			return new CustomAttribute(constructor);
		}

		static byte[] Write(ModuleDefMD module, bool preserveMetadata) {
			var options = new ModuleWriterOptions(module);
			if (preserveMetadata)
				options.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
			var stream = new MemoryStream();
			module.Write(stream, options);
			return stream.ToArray();
		}

		// Compiles the code and adds a custom attribute to each of the 4 rows
		static readonly Lazy<byte[]> imageWithAttributes = new Lazy<byte[]>(() => {
			// Not optimized: the compiler keeps the locals
			var image = CSharpCompiler.Compile(Source, new CSharpCompilerOptions { Optimize = false }).Image;
			using (var module = ModuleDefMD.Load(image)) {
				foreach (var row in new RowObjects(module, "First`1").All)
					row.CustomAttributes.Add(CreateAttribute(module));
				return Write(module, preserveMetadata: true);
			}
		});

		static int CountCustomAttributeRows(byte[] image) {
			using (var module = ModuleDefMD.Load(image))
				return (int)module.TablesStream.CustomAttributeTable.Rows;
		}

		[Fact]
		public void BothMethodsUseTheSameRows() {
			using (var module = ModuleDefMD.Load(imageWithAttributes.Value)) {
				var first = new RowObjects(module, "First`1");
				var second = new RowObjects(module, "Second`1");

				for (int i = 0; i < first.All.Length; i++) {
					Assert.Equal(first.All[i].MDToken, second.All[i].MDToken);
					// Another object: its generic parameter is the one of the other type
					Assert.NotSame(first.All[i], second.All[i]);
				}
			}
		}

		// Fixed in 75af5295 (TypeSpec: c079c951)
		[Fact]
		public void ObjectsOfTheSameRowShareTheCustomAttributes() {
			using (var module = ModuleDefMD.Load(imageWithAttributes.Value)) {
				var first = new RowObjects(module, "First`1");
				var second = new RowObjects(module, "Second`1");

				for (int i = 0; i < first.All.Length; i++) {
					Assert.Single(first.All[i].CustomAttributes);
					Assert.Same(first.All[i].CustomAttributes, second.All[i].CustomAttributes);
				}
			}
		}

		// Reads both methods, so both objects of every row exist, and writes the module
		static byte[] ReadBothMethodsAndWrite(bool preserveMetadata) {
			using (var module = ModuleDefMD.Load(imageWithAttributes.Value)) {
				_ = new RowObjects(module, "First`1");
				_ = new RowObjects(module, "Second`1");
				return Write(module, preserveMetadata);
			}
		}

		static void AssertSameRowCount(Func<TablesStream, MDTable> getTable, byte[] expectedImage, byte[] actualImage) {
			using (var expected = ModuleDefMD.Load(expectedImage))
			using (var actual = ModuleDefMD.Load(actualImage))
				Assert.Equal(getTable(expected.TablesStream).Rows, getTable(actual.TablesStream).Rows);
		}

		// Fixed in 75af5295, c079c951 and ef9b164d: every object's attributes were written, so the number of
		// CustomAttribute rows grew with each write, and the StandAloneSig row was written twice
		[Fact]
		public void CustomAttributesAreWrittenOnceIfTheMetadataIsPreserved() {
			var written = ReadBothMethodsAndWrite(preserveMetadata: true);

			AssertSameRowCount(a => a.CustomAttributeTable, imageWithAttributes.Value, written);
			AssertSameRowCount(a => a.StandAloneSigTable, imageWithAttributes.Value, written);
			AssertSameRowCount(a => a.MemberRefTable, imageWithAttributes.Value, written);
			AssertSameRowCount(a => a.MethodSpecTable, imageWithAttributes.Value, written);
			AssertSameRowCount(a => a.TypeSpecTable, imageWithAttributes.Value, written);
			using (var module = ModuleDefMD.Load(written)) {
				foreach (var row in new RowObjects(module, "Second`1").All)
					Assert.Single(row.CustomAttributes);
			}
		}

		// If the tokens aren't preserved, the writer creates the signature of a method's locals from the locals
		// and doesn't use the original StandAloneSig row: the attribute of that row isn't written
		[Fact]
		public void CustomAttributesAreWrittenOnceIfTheMetadataIsNotPreserved() {
			var written = ReadBothMethodsAndWrite(preserveMetadata: false);

			Assert.Equal(CountCustomAttributeRows(imageWithAttributes.Value) - 1, CountCustomAttributeRows(written));
			using (var module = ModuleDefMD.Load(written)) {
				var rows = new RowObjects(module, "Second`1");
				Assert.Single(rows.MemberRef.CustomAttributes);
				Assert.Single(rows.TypeSpec.CustomAttributes);
				Assert.Single(rows.MethodSpec.CustomAttributes);
				Assert.Empty(rows.StandAloneSig.CustomAttributes);
			}
		}

		[Fact]
		public void AttributeAddedThroughOneObjectIsWrittenOnce() {
			int originalCount = CountCustomAttributeRows(imageWithAttributes.Value);
			byte[] written;
			using (var module = ModuleDefMD.Load(imageWithAttributes.Value)) {
				var first = new RowObjects(module, "First`1");
				_ = new RowObjects(module, "Second`1");
				first.MemberRef.CustomAttributes.Add(CreateAttribute(module));
				written = Write(module, preserveMetadata: true);
			}

			Assert.Equal(originalCount + 1, CountCustomAttributeRows(written));
			using (var module = ModuleDefMD.Load(written))
				Assert.Equal(2, new RowObjects(module, "Second`1").MemberRef.CustomAttributes.Count);
		}
	}
}
