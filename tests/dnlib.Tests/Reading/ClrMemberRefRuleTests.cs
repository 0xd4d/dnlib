// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// Code calls a method of another assembly with a row of the MemberRef table (the class, the name and the
	/// signature of the method), and a generic method with a row of the MethodSpec table (the method and its
	/// type arguments). These tests write values that a compiler doesn't write and read the rows with dnlib.
	/// The runtime that runs the tests is asked for the method of the token. If it finds a method, dnlib must
	/// have that class, name and type arguments.
	/// </summary>
	public class ClrMemberRefRuleTests {
		const string Source = @"
public class C {
	public static string Concat(string a, string b) => null;
	public static string Other(string a, string b) => null;
	public static T G<T>(T a) => a;
	public static T H<T>(T a) => a;
	public static U TwoArguments<T, U>(T a) => default(U);
	public static string CallsConcat(string a) => string.Concat(a, a);
	public static int CallsG() => G<int>(1);
	public static long CallsAbs() => System.Math.Abs(1L);
}";
		static readonly byte[] baseImage = CSharpCompiler.Compile(Source).Image;

		// The token of the method that a method of C calls
		static MDToken GetCalledToken(ModuleDefMD module, string caller) =>
			((IMDTokenProvider)module.Find("C", false)!.FindMethod(caller)!.Body!.Instructions.Single(a => a.OpCode == OpCodes.Call).Operand!).MDToken;

		// "Column = value", separated by "; ": a column of the MemberRef row of String.Concat (Class, Name,
		// Signature) or of the MethodSpec row of G<int> (Method, Instantiation). A value is a number or:
		// "TypeDef C" / "MethodDef C::Concat" / "TypeRef of Abs" (for Class), "name of X" / "signature of X"
		// (of the method X of C), "MethodDef C::X" / "MemberRef of Concat" (for Method).
		static byte[] CreateImage(string changes) {
			var patcher = new MetadataPatcher(baseImage);
			using (var module = ModuleDefMD.Load(baseImage)) {
				var type = module.Find("C", false)!;
				uint memberRefRow = GetCalledToken(module, "CallsConcat").Rid;
				uint methodSpecRow = GetCalledToken(module, "CallsG").Rid;
				uint MethodRid(string value) => type.FindMethod(value.Substring(value.LastIndexOf(' ') + 1).Replace("C::", ""))!.Rid;
				foreach (var change in changes.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries)) {
					var parts = change.Split(new[] { " = " }, StringSplitOptions.None);
					string column = parts[0], value = parts[1];
					bool isMethodSpec = column == "Method" || column == "Instantiation";
					uint cell;
					if (char.IsDigit(value[0]))
						cell = uint.Parse(value);
					// The class of a MemberRef is a coded token: the row number and 0 = TypeDef, 1 = TypeRef,
					// 2 = ModuleRef, 3 = MethodDef, 4 = TypeSpec
					else if (value == "TypeDef C")
						cell = type.Rid << 3;
					else if (value == "TypeRef of Abs")
						cell = patcher.GetCell(Table.MemberRef, GetCalledToken(module, "CallsAbs").Rid, "Class");
					else if (column == "Class")
						cell = (MethodRid(value) << 3) | 3;
					else if (value.StartsWith("name of "))
						cell = patcher.GetCell(Table.Method, MethodRid(value), "Name");
					else if (value.StartsWith("signature of "))
						cell = patcher.GetCell(Table.Method, MethodRid(value), "Signature");
					// The method of a MethodSpec is a coded token: the row number and 0 = MethodDef, 1 = MemberRef
					else if (value == "MemberRef of Concat")
						cell = (memberRefRow << 1) | 1;
					else
						cell = MethodRid(value) << 1;
					patcher.SetCell(isMethodSpec ? Table.MethodSpec : Table.MemberRef, isMethodSpec ? methodSpecRow : memberRefRow, column, cell);
				}
			}
			return patcher.ToArray();
		}

		static string ReadWithDnlib(byte[] image) {
			using (var original = ModuleDefMD.Load(baseImage))
			using (var module = ModuleDefMD.Load(image)) {
				var memberRef = (MemberRef?)module.ResolveToken(GetCalledToken(original, "CallsConcat"));
				var methodSpec = (MethodSpec?)module.ResolveToken(GetCalledToken(original, "CallsG"));
				return Describe(memberRef) + ", " + Describe(methodSpec);
			}
		}
		static string Describe(MemberRef? memberRef) {
			if (memberRef is null)
				return "null";
			string owner = memberRef.Class is null ? "null" : memberRef.Class is MethodDef method ? "method " + method.Name : memberRef.Class.GetType().Name.Replace("MD", "") + " " + memberRef.Class.Name;
			return $"{owner}::{memberRef.Name}{(memberRef.Signature is null ? " no signature" : memberRef.IsFieldRef ? " field" : "(" + memberRef.MethodSig!.Params.Count + ")")}";
		}
		static string Describe(MethodSpec? methodSpec) {
			if (methodSpec is null)
				return "null";
			string method = methodSpec.Method is null ? "null" : methodSpec.Method is MemberRef memberRef ? Describe(memberRef) : methodSpec.Method.DeclaringType?.Name + "::" + methodSpec.Method.Name;
			string arguments = methodSpec.Instantiation is GenericInstMethodSig sig ? string.Join(",", sig.GenericArguments.Select(a => a?.FullName ?? "null")) : "no instantiation";
			return $"{method}<{arguments}>";
		}

		static string ReadWithTheRuntime(byte[] image) {
			int memberRefToken, methodSpecToken;
			using (var original = ModuleDefMD.Load(baseImage)) {
				memberRefToken = GetCalledToken(original, "CallsConcat").ToInt32();
				methodSpecToken = GetCalledToken(original, "CallsG").ToInt32();
			}
			using (var loaded = new LoadedAssembly(image))
				return Resolve(loaded.Assembly.ManifestModule, memberRefToken) + ", " + Resolve(loaded.Assembly.ManifestModule, methodSpecToken);
		}
		static string Resolve(System.Reflection.Module module, int token) {
			try {
				var method = module.ResolveMethod(token)!;
				string arguments = method.IsGenericMethod ? "<" + string.Join(",", method.GetGenericArguments().Select(a => a.FullName ?? a.Name)) + ">" : "";
				return $"{method.DeclaringType!.Name}::{method.Name}({method.GetParameters().Length}){arguments}";
			}
			catch (Exception ex) {
				return "throws " + ex.GetType().Name;
			}
		}

		[Theory]
		[InlineData("", "TypeRef String::Concat(2), C::G<System.Int32>", "String::Concat(2), C::G(1)<System.Int32>")]
		// The class of the MemberRef
		[InlineData("Class = TypeDef C", "TypeDef C::Concat(2), C::G<System.Int32>", "C::Concat(2), C::G(1)<System.Int32>")]
		[InlineData("Class = TypeRef of Abs", "TypeRef Math::Concat(2), C::G<System.Int32>", "throws MissingMethodException, C::G(1)<System.Int32>")]
		[InlineData("Class = MethodDef C::Concat", "method Concat::Concat(2), C::G<System.Int32>", "C::Concat(2), C::G(1)<System.Int32>")]
		[InlineData("Class = MethodDef C::Other", "method Other::Concat(2), C::G<System.Int32>", "C::Other(2), C::G(1)<System.Int32>")]
		[InlineData("Class = 0", "null::Concat(2), C::G<System.Int32>", "throws TypeLoadException, C::G(1)<System.Int32>")]
		[InlineData("Class = 1", "null::Concat(2), C::G<System.Int32>", "throws TypeLoadException, C::G(1)<System.Int32>")]
		[InlineData("Class = 2", "null::Concat(2), C::G<System.Int32>", "throws ArgumentException, C::G(1)<System.Int32>")]
		[InlineData("Class = 10", "null::Concat(2), C::G<System.Int32>", "throws ArgumentException, C::G(1)<System.Int32>")]
		[InlineData("Class = 4", "null::Concat(2), C::G<System.Int32>", "throws ArgumentException, C::G(1)<System.Int32>")]
		[InlineData("Class = 12", "TypeSpec ::Concat(2), C::G<System.Int32>", "throws ArgumentException, C::G(1)<System.Int32>")]
		[InlineData("Class = 5", "null::Concat(2), C::G<System.Int32>", "throws TypeLoadException, C::G(1)<System.Int32>")]
		[InlineData("Class = 7", "null::Concat(2), C::G<System.Int32>", "throws TypeLoadException, C::G(1)<System.Int32>")]
		[InlineData("Class = 8001", "null::Concat(2), C::G<System.Int32>", "throws TypeLoadException, C::G(1)<System.Int32>")]
		// Its name and its signature
		[InlineData("Name = name of Other", "TypeRef String::Other(2), C::G<System.Int32>", "throws MissingMethodException, C::G(1)<System.Int32>")]
		[InlineData("Name = 0", "TypeRef String::(2), C::G<System.Int32>", "throws MissingMethodException, C::G(1)<System.Int32>")]
		[InlineData("Class = TypeDef C; Name = name of Other", "TypeDef C::Other(2), C::G<System.Int32>", "C::Other(2), C::G(1)<System.Int32>")]
		[InlineData("Signature = signature of CallsConcat", "TypeRef String::Concat(1), C::G<System.Int32>", "throws MissingMethodException, C::G(1)<System.Int32>")]
		[InlineData("Signature = 0", "TypeRef String::Concat no signature, C::G<System.Int32>", "throws ArgumentException, C::G(1)<System.Int32>")]
		[InlineData("Class = TypeDef C; Signature = signature of CallsConcat", "TypeDef C::Concat(1), C::G<System.Int32>", "throws MissingMethodException, C::G(1)<System.Int32>")]
		// The method of the MethodSpec
		[InlineData("Method = MethodDef C::H", "TypeRef String::Concat(2), C::H<System.Int32>", "String::Concat(2), C::H(1)<System.Int32>")]
		[InlineData("Method = MethodDef C::Concat", "TypeRef String::Concat(2), C::Concat<System.Int32>", "String::Concat(2), throws ArgumentException")]
		[InlineData("Method = MethodDef C::TwoArguments", "TypeRef String::Concat(2), C::TwoArguments<System.Int32>", "String::Concat(2), throws ArgumentException")]
		[InlineData("Method = MemberRef of Concat", "TypeRef String::Concat(2), TypeRef String::Concat(2)<System.Int32>", "String::Concat(2), throws ArgumentException")]
		[InlineData("Method = 0", "TypeRef String::Concat(2), null<System.Int32>", "String::Concat(2), throws TypeLoadException")]
		[InlineData("Method = 1", "TypeRef String::Concat(2), null<System.Int32>", "String::Concat(2), throws ArgumentException")]
		[InlineData("Method = 4000", "TypeRef String::Concat(2), null<System.Int32>", "String::Concat(2), throws ArgumentException")]
		// Its type arguments
		[InlineData("Instantiation = 0", "TypeRef String::Concat(2), C::G<no instantiation>", "String::Concat(2), throws ArgumentException")]
		[InlineData("Instantiation = signature of G", "TypeRef String::Concat(2), C::G<no instantiation>", "String::Concat(2), throws ArgumentException")]
		[InlineData("Instantiation = signature of Concat", "TypeRef String::Concat(2), C::G<no instantiation>", "String::Concat(2), throws ArgumentException")]
		public void CalledMethod(string changes, string expectedDnlib, string expectedRuntime) {
			var image = CreateImage(changes);

			ClrReaderComparison.AssertRead(expectedDnlib, ReadWithDnlib(image), expectedRuntime, () => ReadWithTheRuntime(image));
		}
	}
}
