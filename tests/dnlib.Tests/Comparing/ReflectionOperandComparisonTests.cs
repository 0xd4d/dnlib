// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Comparing {
	/// <summary>
	/// <see cref="SigComparer"/> compares dnlib's types and members with the objects of System.Reflection. These
	/// tests compile code that uses types, methods and fields in many forms (generic types and methods, arrays,
	/// nested types, members of generic instances), and compare every type, method and field that an
	/// instruction uses with the reflection object that the runtime finds for the same token: they must be equal
	/// and have the same hash code.
	///
	/// Fixed in this commit: if the declaring types are compared, a member of a generic type that the type's own
	/// code uses (a reference to <c>Generic&lt;T&gt;::Field</c> in a method of <c>Generic&lt;T&gt;</c>) wasn't equal
	/// to its reflection member, whose declaring type is the generic type definition, and had another hash code.
	/// </summary>
	public class ReflectionOperandComparisonTests {
		const string Source = @"
using System;
using System.Collections.Generic;
public class Generic<T> {
	public T Field;
	public static int StaticField;
	public Generic() { }
	public Generic(T value) { Field = value; }
	public T Method<U>(U value, T other) => Field;
	public static void Static(T[] values, List<T> list) { }
	public class Nested<V> {
		public T First;
		public V Second;
		public KeyValuePair<T, V> Both() => new KeyValuePair<T, V>(First, Second);
	}
	// Uses its own members: the tokens have the generic parameters of the type
	public T UsesItself<W>(W value) {
		StaticField++;
		Static(new[] { Field }, new List<T>());
		var nested = new Nested<W> { First = Field, Second = value };
		nested.Both();
		return Method(value, Field);
	}
}
public struct Value : IComparable<Value> {
	public int X;
	public int CompareTo(Value other) => X.CompareTo(other.X);
}
public interface IThing<in T> {
	void Do(T value);
	int Count { get; }
}
public abstract class Base {
	public abstract int Number(ref int a, out string b, params object[] rest);
	public virtual event EventHandler Changed;
	protected void Raise() => Changed?.Invoke(this, EventArgs.Empty);
}
public class Derived : Base, IThing<string> {
	public override int Number(ref int a, out string b, params object[] rest) { b = null; return a + rest.Length; }
	public void Do(string value) { }
	public int Count => 0;
	public int this[int index, string name] => index;
}
public static class Marker {
	public static int Field;
	public static readonly int[,] Matrix = new int[2, 3];
	public static U Echo<U>(U value) => value;
	public static T[] Repeat<T>(T value, int count) where T : struct {
		var result = new T[count];
		for (int i = 0; i < count; i++)
			result[i] = value;
		return result;
	}
	public static unsafe int Unsafe(int* pointer, void** other) => *pointer + sizeof(Value);
	public static void Use() {
		var generic = new Generic<int>(5);
		generic.Field = 1;
		generic.Method<string>(""a"", 2);
		generic.UsesItself(1.5);
		Generic<long>.StaticField = 2;
		Generic<string>.Static(new string[1], new List<string>());
		var nested = new Generic<Value>.Nested<Value?>();
		nested.First = new Value { X = 1 };
		nested.Both();
		Matrix[0, 0] = 1;
		Field = Matrix[1, 1] + Matrix.GetLength(0);
		var jagged = new string[2][];
		jagged[0] = new[] { string.Concat(""a"", ""b"") };
		var list = new List<string[]>();
		list.Add(jagged[0]);
		Echo<Value?>(null);
		Echo(new Dictionary<string, List<int>>()).Clear();
		Repeat(new Value(), 2)[0].CompareTo(default(Value));
		Base derived = new Derived();
		int number = 1;
		string text;
		derived.Number(ref number, out text, 1, ""x"");
		derived.Changed += (sender, e) => Field++;
		IThing<string> thing = (Derived)derived;
		thing.Do(thing.Count.ToString());
		Field += ((Derived)derived)[1, ""a""];
		Func<int, string> function = a => a.ToString();
		Console.WriteLine(function(3) + typeof(IThing<>).Name + typeof(Generic<>.Nested<>).Name + typeof(int*[]).Name + typeof(Dictionary<,>).Name);
		object boxed = 5;
		Field += (int)boxed + (boxed is IComparable<int> ? 1 : 0);
		var tuple = Tuple.Create(1, ""a"", 2.0);
		Field += tuple.Item1 + tuple.Item2.Length;
		try { throw new InvalidOperationException(); } catch (InvalidOperationException) { }
	}
}
";
		static readonly byte[] image = CSharpCompiler.Compile(Source).Image;

		const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

		// Every type, method and field that the code uses: the dnlib object and the reflection object of the same token
		static List<(string where, object dnlib, object reflection)> GetOperands(ModuleDefMD module, Module reflectionModule) {
			var operands = new List<(string, object, object)>();
			foreach (var method in module.GetTypes().SelectMany(a => a.Methods).Where(a => a.HasBody)) {
				var reflectionMethod = reflectionModule.ResolveMethod(method.MDToken.ToInt32())!;
				// The tokens of generic code have the generic parameters of the type and the method
				var typeArguments = reflectionMethod.DeclaringType is Type declaringType && declaringType.IsGenericType ? declaringType.GetGenericArguments() : null;
				var methodArguments = reflectionMethod.IsGenericMethod ? reflectionMethod.GetGenericArguments() : null;
				foreach (var instruction in method.Body!.Instructions) {
					if (instruction.Operand is not IMDTokenProvider operand || instruction.Operand is not (ITypeDefOrRef or IMethod or IField))
						continue;
					int token = operand.MDToken.ToInt32();
					object reflection;
					if (instruction.Operand is ITypeDefOrRef)
						reflection = reflectionModule.ResolveType(token, typeArguments, methodArguments);
					else
						reflection = reflectionModule.ResolveMember(token, typeArguments, methodArguments)!;
					operands.Add(($"{method.FullName}: {instruction}", instruction.Operand, reflection));
				}
			}
			return operands;
		}

		static bool AreEqual(SigComparer comparer, object dnlib, object reflection) => (dnlib, reflection) switch {
			(ITypeDefOrRef a, Type b) => comparer.Equals(a, b) && comparer.Equals(b, a),
			(IMethod a, MethodBase b) => comparer.Equals(a, b) && comparer.Equals(b, a),
			(IField a, FieldInfo b) => comparer.Equals(a, b) && comparer.Equals(b, a),
			_ => false,
		};

		static int GetHashCode(SigComparer comparer, object value) => value switch {
			ITypeDefOrRef a => comparer.GetHashCode(a),
			IMethod a => comparer.GetHashCode(a),
			IField a => comparer.GetHashCode(a),
			Type a => comparer.GetHashCode(a),
			MethodBase a => comparer.GetHashCode(a),
			FieldInfo a => comparer.GetHashCode(a),
			_ => throw new ArgumentException(),
		};

		[Theory]
		[InlineData((SigComparerOptions)0)]
		[InlineData(SigComparerOptions.CompareMethodFieldDeclaringType)]
		[InlineData(SigComparerOptions.CompareMethodFieldDeclaringType | SigComparerOptions.PrivateScopeIsComparable)]
		public void EveryOperandEqualsItsReflectionObject(SigComparerOptions options) {
			using (var module = ModuleDefMD.Load(image))
			using (var loaded = new LoadedAssembly(image)) {
				var operands = GetOperands(module, loaded.Assembly.ManifestModule);
				Assert.True(operands.Count > 100, $"Only {operands.Count} operands");
				Assert.Contains(operands, a => a.dnlib is MethodSpec);
				Assert.Contains(operands, a => a.dnlib is MemberRef memberRef && memberRef.IsFieldRef);
				Assert.Contains(operands, a => a.dnlib is TypeSpec);

				var problems = new List<string>();
				foreach (var (where, dnlib, reflection) in operands) {
					var comparer = new SigComparer(options);
					if (!AreEqual(comparer, dnlib, reflection))
						problems.Add($"not equal: {where} / {reflection}");
					else if (GetHashCode(comparer, dnlib) != GetHashCode(comparer, reflection))
						problems.Add($"other hash code: {where} / {reflection}");
				}

				Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems.Distinct().Take(30)));
			}
		}

		// An operand doesn't equal the reflection object of an operand with another name
		[Fact]
		public void OperandDoesNotEqualTheReflectionObjectOfAnotherOperand() {
			using (var module = ModuleDefMD.Load(image))
			using (var loaded = new LoadedAssembly(image)) {
				var operands = GetOperands(module, loaded.Assembly.ManifestModule);
				var comparer = new SigComparer(SigComparerOptions.CompareMethodFieldDeclaringType);
				var problems = new List<string>();
				int compared = 0;
				foreach (var a in operands) {
					foreach (var b in operands) {
						string nameA = a.dnlib is ITypeDefOrRef typeA ? typeA.ReflectionFullName : ((IMemberRef)a.dnlib).Name.String;
						string nameB = b.reflection is Type typeB ? typeB.FullName ?? typeB.ToString() : ((MemberInfo)b.reflection).Name;
						if (nameA == nameB || a.dnlib.GetType() != b.dnlib.GetType() && !(a.dnlib is ITypeDefOrRef && b.dnlib is ITypeDefOrRef))
							continue;
						if (a.dnlib is ITypeDefOrRef != b.reflection is Type)
							continue;
						// A generic parameter has no full name
						if (b.reflection is Type { IsGenericParameter: true } || a.dnlib is TypeSpec { TypeSig: GenericSig })
							continue;
						compared++;
						if (AreEqual(comparer, a.dnlib, b.reflection))
							problems.Add($"equal: {a.dnlib} / {b.reflection}");
					}
				}
				Assert.True(compared > 1000, $"Only {compared} pairs");
				Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems.Distinct().Take(30)));
			}
		}
	}
}
