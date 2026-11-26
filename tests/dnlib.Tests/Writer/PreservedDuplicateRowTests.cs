// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// Some compilers (eg. Mono's C# compiler) write the same TypeSpec, MemberRef or MethodSpec row more than once.
	/// If the tokens are preserved, every reference must keep the row it had. A row that uses a generic parameter
	/// of its user (eg. <c>I&lt;T&gt;</c> in <c>class C&lt;T&gt; : I&lt;T&gt;</c>) is another object for every user.
	/// Fixed in this commit: the writer didn't know that such an object was an original row and gave it the first
	/// row with the same columns. Found with Mono 6.8's System.Core.dll and System.dll.
	/// </summary>
	public class PreservedDuplicateRowTests {
		const string Source = @"
public interface I<T> { }
public interface J<T> { }
public class C<T> : I<T>, J<T> { }
public class D<T> : J<T> { }
public static class A<T> {
	public static void X() { }
	public static void Y() { }
}
public static class H {
	public static void First<T>() { }
	public static void Second<T>() { }
}
public class G<T> {
	public static void CallsMembers() { A<T>.X(); A<T>.Y(); A<T>.Y(); }
	public static void CallsGenericMethods() { H.First<T>(); H.Second<T>(); H.Second<T>(); }
}";

		// The last row of the table gets the columns of the row before it
		static byte[] MakeLastRowADuplicate(byte[] image, Table table, params string[] columns) {
			var patcher = new MetadataPatcher(image);
			uint last = patcher.GetRowCount(table);
			Assert.True(last >= 2);
			foreach (var column in columns)
				patcher.SetCell(table, last, column, patcher.GetCell(table, last - 1, column));
			return patcher.ToArray();
		}

		static byte[] WriteWithPreservedTokens(byte[] image) {
			using (var module = ModuleDefMD.Load(image)) {
				var options = new ModuleWriterOptions(module);
				options.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
				var stream = new MemoryStream();
				module.Write(stream, options);
				return stream.ToArray();
			}
		}

		// The tokens of the calls in a method of G<T>
		static List<uint> GetCallTokens(byte[] image, string method) {
			using (var module = ModuleDefMD.Load(image)) {
				var body = module.Find("G`1", isReflectionName: true)!.FindMethod(method)!.Body!;
				return body.Instructions.Where(a => a.Operand is IMDTokenProvider).Select(a => ((IMDTokenProvider)a.Operand!).MDToken.Raw).ToList();
			}
		}

		static List<uint> GetColumn(byte[] image, Table table, string column) {
			var patcher = new MetadataPatcher(image);
			return Enumerable.Range(1, (int)patcher.GetRowCount(table)).Select(a => patcher.GetCell(table, (uint)a, column)).ToList();
		}

		[Fact]
		public void InterfacesKeepTheirDuplicateTypeSpecRows() {
			var image = CSharpCompiler.Compile(Source).Image;
			// The interfaces of C<T> are the first rows: I<T> and J<T>, two TypeSpec rows (the low 2 bits say which
			// table it is, 2 is TypeSpec). The row of J<T> gets the signature of I<T>.
			var interfaces = GetColumn(image, Table.InterfaceImpl, "Interface");
			Assert.Equal(3, interfaces.Count);
			Assert.All(interfaces, a => Assert.Equal(2U, a & 3));
			Assert.NotEqual(interfaces[0], interfaces[1]);
			var patcher = new MetadataPatcher(image);
			patcher.SetCell(Table.TypeSpec, interfaces[1] >> 2, "Signature", patcher.GetCell(Table.TypeSpec, interfaces[0] >> 2, "Signature"));
			image = patcher.ToArray();

			var written = WriteWithPreservedTokens(image);

			Assert.Equal(interfaces, GetColumn(written, Table.InterfaceImpl, "Interface"));
			Assert.Equal(GetColumn(image, Table.TypeSpec, "Signature").Count, GetColumn(written, Table.TypeSpec, "Signature").Count);
		}

		[Fact]
		public void CallsKeepTheirDuplicateMemberRefRows() {
			var image = MakeLastRowADuplicate(CSharpCompiler.Compile(Source).Image, Table.MemberRef, "Class", "Name", "Signature");
			var tokens = GetCallTokens(image, "CallsMembers");
			Assert.Equal(3, tokens.Count);
			Assert.Equal(2, tokens.Distinct().Count());

			var written = WriteWithPreservedTokens(image);

			Assert.Equal(tokens, GetCallTokens(written, "CallsMembers"));
			Assert.Equal(GetColumn(image, Table.MemberRef, "Name").Count, GetColumn(written, Table.MemberRef, "Name").Count);
		}

		[Fact]
		public void CallsKeepTheirDuplicateMethodSpecRows() {
			var image = MakeLastRowADuplicate(CSharpCompiler.Compile(Source).Image, Table.MethodSpec, "Method", "Instantiation");
			var tokens = GetCallTokens(image, "CallsGenericMethods");
			Assert.Equal(3, tokens.Count);
			Assert.Equal(2, tokens.Distinct().Count());

			var written = WriteWithPreservedTokens(image);

			Assert.Equal(tokens, GetCallTokens(written, "CallsGenericMethods"));
			Assert.Equal(GetColumn(image, Table.MethodSpec, "Method").Count, GetColumn(written, Table.MethodSpec, "Method").Count);
		}

		// Without duplicate rows, the rows of a written file are the rows of the compiler
		[Fact]
		public void RowsAreTheSameWithoutDuplicates() {
			var image = CSharpCompiler.Compile(Source).Image;

			var written = WriteWithPreservedTokens(image);

			Assert.Equal(GetColumn(image, Table.InterfaceImpl, "Interface"), GetColumn(written, Table.InterfaceImpl, "Interface"));
			Assert.Equal(GetCallTokens(image, "CallsMembers"), GetCallTokens(written, "CallsMembers"));
			Assert.Equal(GetCallTokens(image, "CallsGenericMethods"), GetCallTokens(written, "CallsGenericMethods"));
		}
	}
}
