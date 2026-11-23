// A test input, not a test: this file is compiled by the tests (see Fixtures.cs), it's not part of dnlib.Tests.
//
// It uses many metadata features that C# can express, so that a test that reads or writes it touches a lot of
// dnlib's code. When a feature turns out to be missing, add it here. It must compile with .NET Framework 4.5's mscorlib/System/System.Core too, so no newer library types
// (tuples, Span<T>, records, default interface methods).
//
// Main() returns 0 if the code still works. Tests that write the assembly with dnlib run it.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

[assembly: Features.Everything("assembly", Number = 1)]
[module: Features.Everything("module")]

namespace Features {
	public enum Color : byte {
		Red = 1,
		Green = 2,
		Blue = 4,
	}

	[Flags]
	public enum LongFlags : long {
		None = 0,
		Low = 1,
		High = long.MinValue,
	}

	// An attribute with every kind of argument a custom attribute can have
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
	public sealed class EverythingAttribute : Attribute {
		public EverythingAttribute(string text) { }
		public EverythingAttribute(bool b, char c, sbyte i1, byte u1, short i2, ushort u2, int i4, uint u4, long i8, ulong u8, float r4, double r8) { }
		public EverythingAttribute(Type type, Color color, object boxed) { }
		public EverythingAttribute(int[] numbers, string[] texts, Type[] types, object[] objects) { }

		public int Number { get; set; }
		public Color ColorField;
		public Type TypeProperty { get; set; }
		public object[] Objects { get; set; }
	}

	// A generic attribute: its constructor is a method of a generic type instance (a MemberRef whose class is a
	// TypeSpec), and the types of its arguments are the type's generic arguments
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
	public sealed class GenericAttribute<T> : Attribute {
		public GenericAttribute(T value) { }
		public GenericAttribute(T[] values, object boxed) { }
		public T Property { get; set; }
		public T[] Field;
	}

	[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
	public sealed class GenericAttribute<TKey, TValue> : Attribute {
		public GenericAttribute(TKey key, TValue value, TValue[] values) { }
	}

	// An enum in a generic type: the enum type itself is generic, eg. GenericOwner<int>.Kind
	public class GenericOwner<T> {
		public enum Kind : short {
			First,
			Second = 5,
		}
	}

	[Generic<int>(5, Property = 6)]
	[Generic<string>(new[] { "a", null }, 1.5, Field = new[] { "b" })]
	[Generic<Type>(typeof(List<>), Property = typeof(Dictionary<int, string[]>))]
	[Generic<object>("boxed", Property = 123)]
	[Generic<Color>(Color.Blue, Field = new[] { Color.Red, Color.Green })]
	[Generic<string, long>("key", long.MinValue, new long[] { 1, 2 })]
	[Everything(typeof(GenericOwner<int>.Kind), Color.Red, GenericOwner<string>.Kind.Second)]
	[Everything(null, null, null, new object[] { GenericOwner<int>.Kind.Second, new[] { GenericOwner<int>.Kind.First } })]
	public static class GenericAttributeUser {
		// Generic attributes and generic enum types need .NET 7 or later
		public static bool IsSupported => Environment.Version.Major >= 7;
		public static int CountAttributes() => typeof(GenericAttributeUser).GetCustomAttributes(false).Length;
	}

	public interface IShape {
		double Area { get; }
		event EventHandler Changed;
		string Describe(string prefix);
	}

	public interface IContainer<out T> : IEnumerable<T> {
		T First { get; }
	}

	[Everything("class", Number = -5, ColorField = Color.Blue, TypeProperty = typeof(List<int>), Objects = new object[] { 1, "two", null, typeof(void), Color.Green })]
	[Everything(true, 'x', -1, 2, -3, 4, -5, 6, -7, 8, 1.5f, -2.5)]
	[Everything(typeof(Dictionary<string, int[]>), Color.Green, 123L)]
	[Everything(new[] { 1, 2, 3 }, new[] { "a", null }, new[] { typeof(int), null, typeof(Outer<>.Inner<>) }, new object[] { new[] { 1 }, 'c' })]
	public abstract class Shape : IShape {
		public const string Kind = "shape";
		public const int MaxSides = int.MaxValue;
		public const double Tolerance = 0.001;
		public const Color DefaultColor = Color.Red;
		public const object Nothing = null;

		public static readonly DateTime Created = new DateTime(2000, 1, 1);
		protected internal int sides;
		volatile int version;

		public abstract double Area { get; }
		public virtual string Name { get; protected set; }
		public int this[int index] {
			get { return index * sides; }
			set { sides = value / (index == 0 ? 1 : index); }
		}

		public event EventHandler Changed;
		event EventHandler IShape.Changed {
			add { Changed += value; }
			remove { Changed -= value; }
		}

		protected Shape(int sides) {
			this.sides = sides;
			Name = GetType().Name;
		}

		static Shape() { }

		public virtual string Describe(string prefix = "A ") => prefix + Name;
		string IShape.Describe(string prefix) => "explicit " + Describe(prefix);

		protected void OnChanged() {
			version++;
			Changed?.Invoke(this, EventArgs.Empty);
		}

		public int Version => version;
	}

	public sealed class Square : Shape {
		readonly double side;
		public Square(double side) : base(4) { this.side = side; }
		public override double Area => side * side;
		public override string Describe(string prefix = "The ") => base.Describe(prefix) + "!";
		public void Touch() => OnChanged();
	}

	public sealed class Key : IComparable<Key> {
		public int Value;
		public int CompareTo(Key other) => Value.CompareTo(other.Value);
	}

	public class Outer<T> where T : class, IComparable<T>, new() {
		public class Inner<U> : IContainer<U> where U : struct {
			readonly U[] items;
			public Inner(params U[] items) { this.items = items; }
			public U First => items[0];
			public IEnumerator<U> GetEnumerator() {
				foreach (var item in items)
					yield return item;
			}
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
			public Dictionary<T, List<U[]>> Map<V>(V value, Func<V, T> convert) where V : IEnumerable<U> =>
				new Dictionary<T, List<U[]>> { { convert(value), new List<U[]> { items } } };
		}

		public T Smallest(T a, T b) => a.CompareTo(b) <= 0 ? a : b;
		public static TResult Create<TResult>() where TResult : T, new() => new TResult();
	}

	[StructLayout(LayoutKind.Explicit, Size = 16, Pack = 2)]
	public struct Union {
		[FieldOffset(0)] public long Whole;
		[FieldOffset(0)] public int Low;
		[FieldOffset(4)] public int High;
		[FieldOffset(8)] public Color Color;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public unsafe struct NativeData {
		public fixed byte Buffer[8];
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)] public string Text;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = UnmanagedType.I2)] public short[] Numbers;
		[MarshalAs(UnmanagedType.Bool)] public bool Flag;
		public IntPtr Pointer;
	}

	public delegate TResult Converter<in TInput, out TResult>(TInput input, ref int counter, out string text);

	public static unsafe class Native {
		[DllImport("kernel32.dll", EntryPoint = "GetTickCount64", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
		public static extern ulong GetTicks();

		[DllImport("user32", CharSet = CharSet.Unicode, BestFitMapping = false, ThrowOnUnmappableChar = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool Show(IntPtr window, [MarshalAs(UnmanagedType.LPWStr)] string text, [In, Out] ref NativeData data,
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] int[] values, int count);

		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		public static int Sum(int* values, int count) {
			int sum = 0;
			for (int* p = values; p < values + count; p++)
				sum += *p;
			return sum;
		}

		public static int CallThroughPointer(delegate*<int, int, int> function, int a, int b) => function(a, b);
		public static int Add(int a, int b) => a + b;
	}

	public static class Code {
		// Compiled to a field with an RVA: the array's bytes are stored in the PE file
		static readonly int[] primes = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37 };
		static readonly byte[] bytes = { 0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x02, 0x03, 0x04, 0x05 };

		public static int Varargs(int first, __arglist) {
			var iterator = new ArgIterator(__arglist);
			int count = first;
			while (iterator.GetRemainingCount() > 0) {
				iterator.GetNextArg();
				count++;
			}
			return count;
		}

		public static string Switch(int value, string text) {
			switch (value) {
			case 0: return "zero";
			case 1: return "one";
			case 2: return "two";
			case 3: return "three";
			case 4: return "four";
			case 100: return "hundred";
			}
			switch (text) {
			case "a": return "A";
			case "b": return "B";
			case "c": return "C";
			case "d": return "D";
			case "e": return "E";
			case "f": return "F";
			case "g": return "G";
			default: return text ?? "null";
			}
		}

		public static int Exceptions(int value) {
			int result = 0;
			try {
				try {
					if (value == 0)
						throw new ArgumentException("zero");
					if (value == 1)
						throw new InvalidOperationException("one");
					result = checked(value * 2);
				}
				catch (ArgumentException) when (value == 0) {
					result = -1;
				}
				catch (InvalidOperationException ex) {
					result = -ex.Message.Length;
				}
				finally {
					result += 1000;
				}
			}
			catch {
				result = int.MinValue;
			}
			return result;
		}

		public static unsafe int Locals(int count) {
			// Constants of a method are only stored in the PDB
			const int Factor = 3;
			const string Label = "label";
			const double Half = 0.5;
			const Color Favorite = Color.Green;
			const object NoObject = null;
			int* buffer = stackalloc int[count];
			for (int i = 0; i < count; i++)
				buffer[i] = primes[i % primes.Length];
			fixed (byte* p = bytes) {
				// Locals in a nested scope
				long total = Native.Sum(buffer, count) * Factor / (Label.Length - 2);
				decimal money = 1.5m;
				double real = 2.5;
				float small = 0.5f;
				object boxed = total;
				string text = boxed.ToString();
				return (int)total + p[0] + (int)money + (int)real + (int)small + text.Length;
			}
		}

		public static IEnumerable<int> Iterator(int count) {
			for (int i = 0; i < count; i++) {
				if (i == 3)
					continue;
				yield return i * i;
			}
		}

		public static async Task<int> Async(int value) {
			await Task.Yield();
			int doubled = await Task.FromResult(value * 2);
			return doubled + 1;
		}

		public static int Lambdas(int value) {
			int captured = value;
			Func<int, int> add = a => a + captured;
			Func<int, Func<int, int>> curry = a => b => a * b + captured;
			captured++;
			return add(1) + curry(2)(3);
		}

		public static ref int RefReturn(int[] array, int index) => ref array[index];

		public static T Generic<T>(T value, ref T other) where T : struct, IComparable<T> {
			if (value.CompareTo(other) > 0)
				other = value;
			return default(T);
		}

		public static string Constants() =>
			"text\0with€odd \uD800 chars" + long.MaxValue + ulong.MaxValue + double.NaN + float.NegativeInfinity + -0.0 + 'x' + int.MinValue;
	}

	public static class Program {
		static int failures;

		static void Check(bool condition, string what) {
			if (!condition) {
				failures++;
				Console.WriteLine("FAILED: " + what);
			}
		}

		// In its own method: it can't be compiled by the JIT compiler where vararg methods aren't supported
		[MethodImpl(MethodImplOptions.NoInlining)]
		static void CheckVarargs() => Check(Code.Varargs(1, __arglist(1, "two", 3.0)) == 4, "Varargs");

		public static int Main() {
			var square = new Square(3);
			int changes = 0;
			square.Changed += (sender, e) => changes++;
			square.Touch();
			Check(square.Area == 9 && changes == 1 && square.Version == 1, "Square");
			Check(square.Describe() == "The Square!", "Describe");
			Check(((IShape)square).Describe("x") == "explicit xSquare!", "explicit interface method");
			square[2] = 10;
			Check(square[3] == 15, "indexer");

			var key = new Key { Value = 1 };
			var inner = new Outer<Key>.Inner<int>(4, 5, 6);
			Check(inner.First == 4, "Inner.First");
			int sum = 0;
			foreach (var item in inner)
				sum += item;
			Check(sum == 15, "Inner enumerator");
			Check(inner.Map(new[] { 1 }, a => key).ContainsKey(key), "Inner.Map");
			Check(new Outer<Key>().Smallest(new Key { Value = 2 }, key) == key, "Outer.Smallest");
			Check(Outer<Key>.Create<Key>().Value == 0, "Outer.Create");

			var union = new Union { Low = 1, High = 2 };
			Check(union.Whole == 0x200000001, "Union");

			// .NET only supports vararg methods on Windows
			if (Environment.OSVersion.Platform == PlatformID.Win32NT || Type.GetType("Mono.Runtime") != null)
				CheckVarargs();
			Check(Code.Switch(100, null) == "hundred" && Code.Switch(-1, "f") == "F" && Code.Switch(-1, null) == "null", "Switch");
			Check(Code.Exceptions(0) == 999 && Code.Exceptions(1) == 997 && Code.Exceptions(5) == 1010 && Code.Exceptions(int.MaxValue) == int.MinValue, "Exceptions");
			Check(Code.Locals(4) == 17 + 0xDE + 1 + 2 + 0 + 2, "Locals");
			int iteratorSum = 0;
			foreach (var value in Code.Iterator(5))
				iteratorSum += value;
			Check(iteratorSum == 0 + 1 + 4 + 16, "Iterator");
			Check(Code.Async(20).Result == 41, "Async");
			Check(Code.Lambdas(10) == 12 + 17, "Lambdas");
			var array = new[] { 1, 2, 3 };
			Code.RefReturn(array, 1) = 20;
			Check(array[1] == 20, "RefReturn");
			int other = 1;
			Code.Generic(5, ref other);
			Check(other == 5, "Generic");
			Check(Code.Constants().Length > 40 && Code.Constants()[4] == '\0', "Constants");
			unsafe {
				Check(Native.CallThroughPointer(&Native.Add, 2, 3) == 5, "function pointer");
			}
			Check(Shape.Kind == "shape" && Shape.Created.Year == 2000, "constants and static fields");
			var attributes = typeof(Shape).GetCustomAttributes(typeof(EverythingAttribute), false);
			Check(attributes.Length == 4, "custom attributes");

			if (GenericAttributeUser.IsSupported)
				Check(GenericAttributeUser.CountAttributes() == 8, "generic custom attributes");

			return failures;
		}
	}
}
