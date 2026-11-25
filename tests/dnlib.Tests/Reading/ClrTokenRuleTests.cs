// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// The code of a method has tokens: the table and the row of the type, the method or the field that an
	/// instruction uses. These tests resolve every token of the tables with types and members (every row, and
	/// rows that don't exist) with dnlib and with the runtime that runs the tests (<c>Module.ResolveType()</c>
	/// and <c>Module.ResolveMember()</c>): both must find the same type or member, or none.
	/// </summary>
	public class ClrTokenRuleTests {
		// Types and members of many kinds: generic types and methods, arrays with more than one dimension (their
		// methods only exist as MemberRef rows), nested types, a method with a variable number of arguments
		const string Source = @"
using System;
using System.Collections.Generic;
public class Generic<T> {
	public T Field;
	public static int StaticField;
	public T Method<U>(U value) => Field;
	public class Nested {
		public T NestedField;
		public void NestedMethod() { }
	}
}
public struct Value {
	public int X;
}
public interface IThing {
	void Do();
}
public static class Marker {
	public static int Field;
	public static void Vararg(int a, __arglist) { }
	public static U Echo<U>(U value) => value;
	public static void Use() {
		var generic = new Generic<int>();
		generic.Field = 1;
		generic.Method<string>(""a"");
		Generic<long>.StaticField = 2;
		var nested = new Generic<Value>.Nested();
		nested.NestedField = new Value { X = 1 };
		nested.NestedMethod();
		var array = new int[2, 2];
		array[0, 0] = 1;
		Field = array[1, 1] + array.GetLength(0);
		var strings = new List<string[]>();
		strings.Add(new[] { string.Concat(""a"", ""b"") });
		Vararg(1, __arglist(2, ""x""));
		Echo<Value?>(null);
		Echo(new Dictionary<string, List<int>>()).Clear();
		Console.WriteLine(typeof(IThing).Name + typeof(Generic<>.Nested).Name + typeof(int*[]).Name);
	}
}
";
		static readonly byte[] image = CSharpCompiler.Compile(Source).Image;

		// "List`1[[System.Int32, mscorlib, Version=...]]" is "List`1[[System.Int32]]"
		static string WithoutAssemblyNames(string typeName) => Regex.Replace(typeName, @", [\w.]+, Version=[\d.]+, Culture=neutral, PublicKeyToken=\w+", "");

		static string DescribeType(ITypeDefOrRef? type) => type is null ? "no type" : WithoutAssemblyNames(type.ReflectionFullName);

		// The type and the name of a member. A method also has the number of its parameters and of its generic
		// arguments.
		static string ReadWithDnlib(ModuleDefMD module, uint token) {
			var resolved = module.ResolveToken(token);
			switch (resolved) {
			case null:
				return "none";
			case ITypeDefOrRef type:
				return "type " + DescribeType(type);
			case MethodDef method:
				return $"method {DescribeType(method.DeclaringType)}::{method.Name}({method.MethodSig?.Params.Count})";
			case FieldDef field:
				return $"field {DescribeType(field.DeclaringType)}::{field.Name}";
			case MemberRef memberRef when memberRef.IsFieldRef:
				return $"field {DescribeType(memberRef.DeclaringType)}::{memberRef.Name}";
			case MemberRef memberRef:
				return $"method {DescribeType(memberRef.DeclaringType)}::{memberRef.Name}({memberRef.MethodSig?.Params.Count})";
			case MethodSpec methodSpec:
				return $"method {DescribeType(methodSpec.DeclaringType)}::{methodSpec.Name}({methodSpec.Method?.MethodSig?.Params.Count})<{methodSpec.GenericInstMethodSig?.GenericArguments.Count}>";
			default:
				return resolved.GetType().Name;
			}
		}

		static string DescribeType(Type? type) => type is null ? "no type" : WithoutAssemblyNames(type.FullName ?? type.ToString());

		static string ReadWithTheRuntime(System.Reflection.Module module, uint token) {
			try {
				var table = (Table)(token >> 24);
				if (table == Table.TypeDef || table == Table.TypeRef || table == Table.TypeSpec)
					return "type " + DescribeType(module.ResolveType((int)token));
				var member = module.ResolveMember((int)token);
				switch (member) {
				case null:
					return "none";
				case Type type:
					return "type " + DescribeType(type);
				case System.Reflection.MethodBase method:
					string genericArguments = method.IsGenericMethod && !method.IsGenericMethodDefinition ? $"<{method.GetGenericArguments().Length}>" : "";
					return $"method {DescribeType(method.DeclaringType)}::{method.Name}({method.GetParameters().Length}){genericArguments}";
				case System.Reflection.FieldInfo field:
					return $"field {DescribeType(field.DeclaringType)}::{field.Name}";
				default:
					return member.GetType().Name;
				}
			}
			catch (ArgumentException) {
				// The token isn't a token of a type or a member, or the row doesn't exist
				return "none";
			}
		}

		public static TheoryData<string> Tables => new TheoryData<string> { "TypeDef", "TypeRef", "TypeSpec", "Field", "Method", "MemberRef", "MethodSpec" };

		// Every row of the table, and the rows 0, the one after the last one and the biggest row number
		[Theory, MemberData(nameof(Tables))]
		public void EveryTokenOfATableIsTheSameTypeOrMember(string tableName) {
			var table = (Table)Enum.Parse(typeof(Table), tableName);
			var dnlib = new List<string>();
			var tokens = new List<uint>();
			using (var module = ModuleDefMD.Load(image)) {
				uint rows = module.TablesStream.Get(table)!.Rows;
				Assert.True(rows != 0, "The table has no rows");
				for (uint rid = 0; rid <= rows + 1; rid++)
					tokens.Add(new MDToken(table, rid).Raw);
				tokens.Add(new MDToken(table, 0x00FFFFFF).Raw);
				foreach (uint token in tokens)
					dnlib.Add($"{token:X8} {ReadWithDnlib(module, token)}");
				// A row that doesn't exist is nothing
				Assert.EndsWith(" none", dnlib[0]);
				Assert.EndsWith(" none", dnlib[dnlib.Count - 1]);
				Assert.EndsWith(" none", dnlib[dnlib.Count - 2]);
			}

			// The runtime has nothing for some tokens that dnlib resolves (the type <Module>, and a type or a member
			// with generic parameters of the method that uses it: the runtime needs to know that method). Those
			// aren't compared.
			ClrReaderComparison.AssertSameAsTheRuntime(string.Join("\n", dnlib), () => {
				using (var loaded = new LoadedAssembly(image)) {
					return string.Join("\n", tokens.Select((a, i) => {
						string runtime = $"{a:X8} {ReadWithTheRuntime(loaded.Assembly.ManifestModule, a)}";
						return runtime.EndsWith(" none") ? dnlib[i] : runtime;
					}));
				}
			});
		}
	}
}
