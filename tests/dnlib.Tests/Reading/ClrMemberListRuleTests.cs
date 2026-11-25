// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// A type has the fields and methods from the row that its <c>FieldList</c> / <c>MethodList</c> column
	/// says to the row before the list of the next type, and a method has its parameters in the same way.
	/// A compiler writes lists that follow each other. These tests write other values (a list that starts
	/// after the next one, at row 0, after the last row) and read the members with dnlib and with the runtime
	/// that runs the tests: dnlib must read the members that the CLR reads.
	/// </summary>
	public class ClrMemberListRuleTests {
		// Static classes: they have no constructor. The Field and the Method table have 6 rows (2 of every
		// class), the Param table has 12 rows (2 of every method).
		const string Source = @"
public static class A {
	public static int a1, a2;
	public static void MA1(int a1x, int a1y) { }
	public static void MA2(int a2x, int a2y) { }
}
public static class B {
	public static int b1, b2;
	public static void MB1(int b1x, int b1y) { }
	public static void MB2(int b2x, int b2y) { }
}
public static class C {
	public static int c1, c2;
	public static void MC1(int c1x, int c1y) { }
	public static void MC2(int c2x, int c2y) { }
}
";
		static readonly byte[] baseImage = Compile(Source);

		// The tests use row numbers, so the file must only have the classes A, B and C: the compiler can
		// add other types (attributes that the target framework doesn't have)
		static byte[] Compile(string source) {
			using (var module = ModuleDefMD.Load(CSharpCompiler.Compile(source).Image)) {
				foreach (var type in module.Types.Where(a => !a.IsGlobalModuleType && a.Name.Length != 1).ToArray())
					module.Types.Remove(type);
				module.CustomAttributes.Clear();
				var stream = new MemoryStream();
				module.Write(stream);
				return stream.ToArray();
			}
		}

		// The Property and the Event table have 6 rows (2 of every class), and every class has a row in the
		// PropertyMap and in the EventMap table: A has row 1, B has row 2, C has row 3
		const string PropertySource = @"
using System;
public static class A {
	public static int PA1 => 0;
	public static int PA2 => 0;
	public static event Action EA1 { add { } remove { } }
	public static event Action EA2 { add { } remove { } }
}
public static class B {
	public static int PB1 => 0;
	public static int PB2 => 0;
	public static event Action EB1 { add { } remove { } }
	public static event Action EB2 { add { } remove { } }
}
public static class C {
	public static int PC1 => 0;
	public static int PC2 => 0;
	public static event Action EC1 { add { } remove { } }
	public static event Action EC2 { add { } remove { } }
}
";
		static readonly byte[] propertyImage = Compile(PropertySource);

		const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

		// "Table.Column of X = value": writes the value to that column of the row of the type or method X
		// (or of row X if it's a number). More than one change is separated by a comma.
		static byte[] CreateImage(string changes) => CreateImage(baseImage, changes);

		static byte[] CreateImage(byte[] image, string changes) {
			var patcher = new MetadataPatcher(image);
			if (changes == "nothing")
				return patcher.ToArray();
			foreach (var change in changes.Split(',')) {
				var parts = change.Split(new[] { ' ', '.', '=' }, StringSplitOptions.RemoveEmptyEntries);
				Assert.Equal(5, parts.Length);
				Assert.Equal("of", parts[2]);
				var table = (Table)Enum.Parse(typeof(Table), parts[0]);
				if (!uint.TryParse(parts[3], out uint rid))
					rid = patcher.FindRow(table, "Name", parts[3]);
				patcher.SetCell(table, rid, parts[1], uint.Parse(parts[4]));
			}
			return patcher.ToArray();
		}

		static string Describe(string typeName, string[] fields, string[] methods) =>
			$"{typeName}[{string.Join(" ", fields)}; {string.Join(" ", methods)}]";

		static readonly string[] typeNames = { "<Module>", "A", "B", "C" };

		static string[] ReadMembersWithDnlib(byte[] image) {
			using (var module = ModuleDefMD.Load(image)) {
				return typeNames.Select(name => {
					var type = module.Types.Single(a => a.Name == name);
					return Describe(name, type.Fields.Select(a => a.Name.String).ToArray(), type.Methods.Select(a => a.Name.String).ToArray());
				}).ToArray();
			}
		}

		// Every type is read on its own: a type that the CLR doesn't load is "throws" and the exception
		static string[] ReadMembersWithTheRuntime(byte[] image) {
			try {
				using (var loaded = new LoadedAssembly(image)) {
					var module = loaded.Assembly.ManifestModule;
					return typeNames.Select(name => {
						if (name == "<Module>")
							return $"{name}[{Names(() => module.GetFields(Declared))}; {Names(() => module.GetMethods(Declared))}]";
						try {
							var type = loaded.Assembly.GetType(name, throwOnError: true)!;
							return $"{name}[{Names(() => type.GetFields(Declared))}; {Names(() => type.GetMethods(Declared))}]";
						}
						catch (Exception ex) {
							return $"{name}[throws {ex.GetType().Name}]";
						}
					}).ToArray();
				}
			}
			catch (Exception ex) {
				return new[] { "throws " + ex.GetType().Name };
			}
		}

		// The names of the members in the order of their rows
		static string[] InOrder(MemberInfo[] members) => members.OrderBy(a => a.MetadataToken).Select(a => a.Name).ToArray();

		// What was read, without the types or methods that have what the compiler gave them ("normal" if all
		// of them have that)
		static string OnlyChanges(string[] read, string[] normal) {
			if (read.Length != normal.Length)
				return string.Join(" ", read);
			var changes = read.Where((a, i) => a != normal[i]).ToArray();
			return changes.Length == 0 ? "normal" : string.Join(" ", changes);
		}

		static readonly string[] normalMembers = { "<Module>[; ]", "A[a1 a2; MA1 MA2]", "B[b1 b2; MB1 MB2]", "C[c1 c2; MC1 MC2]" };

		[Theory]
		[InlineData("nothing", "normal", "normal")]
		// Lists that start where another list starts
		[InlineData("TypeDef.MethodList of B = 1", "A[a1 a2; ] B[b1 b2; MA1 MA2 MB1 MB2]", "A[a1 a2; ] B[b1 b2; MA1 MA2 MB1 MB2]")]
		[InlineData("TypeDef.MethodList of B = 5", "A[a1 a2; MA1 MA2 MB1 MB2] B[b1 b2; ]", "A[a1 a2; MA1 MA2 MB1 MB2] B[b1 b2; ]")]
		[InlineData("TypeDef.MethodList of A = 3", "<Module>[; MA1 MA2] A[a1 a2; ]", "<Module>[; MA1 MA2] A[a1 a2; ]")]
		[InlineData("TypeDef.FieldList of B = 1", "A[; MA1 MA2] B[a1 a2 b1 b2; MB1 MB2]", "A[; MA1 MA2] B[a1 a2 b1 b2; MB1 MB2]")]
		[InlineData("TypeDef.FieldList of A = 3", "<Module>[a1 a2; ] A[; MA1 MA2]", "<Module>[a1 a2; ] A[; MA1 MA2]")]
		// A list that starts after the next list
		[InlineData("TypeDef.MethodList of B = 6", "A[a1 a2; MA1 MA2 MB1 MB2 MC1] B[b1 b2; ]", "A[a1 a2; MA1 MA2 MB1 MB2 MC1] B[throws TypeLoadException]")]
		[InlineData("TypeDef.MethodList of A = 4", "<Module>[; MA1 MA2 MB1] A[a1 a2; ]", "<Module>[; MA1 MA2 MB1] A[throws TypeLoadException]")]
		[InlineData("TypeDef.FieldList of B = 6", "A[a1 a2 b1 b2 c1; MA1 MA2] B[; MB1 MB2]", "A[a1 a2 b1 b2 c1; MA1 MA2] B[throws TypeLoadException]")]
		[InlineData("TypeDef.MethodList of A = 3,TypeDef.MethodList of B = 1", "<Module>[; MA1 MA2] A[a1 a2; ] B[b1 b2; MA1 MA2 MB1 MB2]", "<Module>[; MA1 MA2] A[throws TypeLoadException] B[b1 b2; MA1 MA2 MB1 MB2]")]
		// Row 0, the row after the last row, and a row that doesn't exist
		[InlineData("TypeDef.MethodList of B = 0", "A[a1 a2; ] B[b1 b2; ]", "A[throws TypeLoadException] B[throws TypeLoadException]")]
		[InlineData("TypeDef.MethodList of C = 0", "B[b1 b2; ] C[c1 c2; ]", "B[throws TypeLoadException] C[throws TypeLoadException]")]
		[InlineData("TypeDef.MethodList of C = 7", "B[b1 b2; MB1 MB2 MC1 MC2] C[c1 c2; ]", "B[b1 b2; MB1 MB2 MC1 MC2] C[c1 c2; ]")]
		[InlineData("TypeDef.MethodList of C = 100", "B[b1 b2; MB1 MB2 MC1 MC2] C[c1 c2; ]", "B[throws TypeLoadException] C[throws TypeLoadException]")]
		[InlineData("TypeDef.MethodList of B = 7", "A[a1 a2; MA1 MA2 MB1 MB2 MC1 MC2] B[b1 b2; ]", "A[a1 a2; MA1 MA2 MB1 MB2 MC1 MC2] B[throws TypeLoadException]")]
		[InlineData("TypeDef.MethodList of B = 100", "A[a1 a2; MA1 MA2 MB1 MB2 MC1 MC2] B[b1 b2; ]", "A[throws TypeLoadException] B[throws TypeLoadException]")]
		[InlineData("TypeDef.FieldList of B = 0", "A[; MA1 MA2] B[; MB1 MB2]", "A[throws TypeLoadException] B[throws TypeLoadException]")]
		[InlineData("TypeDef.FieldList of C = 0", "B[; MB1 MB2] C[; MC1 MC2]", "B[throws TypeLoadException] C[throws TypeLoadException]")]
		[InlineData("TypeDef.FieldList of C = 7", "B[b1 b2 c1 c2; MB1 MB2] C[; MC1 MC2]", "B[b1 b2 c1 c2; MB1 MB2] C[; MC1 MC2]")]
		[InlineData("TypeDef.FieldList of C = 100", "B[b1 b2 c1 c2; MB1 MB2] C[; MC1 MC2]", "B[throws TypeLoadException] C[throws TypeLoadException]")]
		[InlineData("TypeDef.FieldList of B = 100", "A[a1 a2 b1 b2 c1 c2; MA1 MA2] B[; MB1 MB2]", "A[throws TypeLoadException] B[throws TypeLoadException]")]
		public void FieldsAndMethodsOfATypeAreTheOnesThatTheClrReads(string changes, string expectedDnlib, string expectedRuntime) {
			var image = CreateImage(changes);

			ClrReaderComparison.AssertRead(expectedDnlib, OnlyChanges(ReadMembersWithDnlib(image), normalMembers), expectedRuntime, () => OnlyChanges(ReadMembersWithTheRuntime(image), normalMembers));
		}

		static readonly string[] methodNames = { "MA1", "MA2", "MB1", "MB2", "MC1", "MC2" };

		// A parameter has the name of the row with its number (the Sequence column). Every method has two
		// parameters, so that the rows of another method also have the numbers of parameters that exist.
		static string[] ReadParametersWithDnlib(byte[] image) {
			using (var module = ModuleDefMD.Load(image)) {
				var methods = module.Types.SelectMany(a => a.Methods).ToArray();
				return methodNames.Select(name => {
					var method = methods.First(a => a.Name == name);
					return $"{name}({string.Join(",", method.Parameters.Select(a => a.Name))})";
				}).ToArray();
			}
		}

		static string[] ReadParametersWithTheRuntime(byte[] image) {
			try {
				using (var loaded = new LoadedAssembly(image)) {
					var methods = loaded.Assembly.GetTypes().SelectMany(a => a.GetMethods(Declared)).ToArray();
					return methodNames.Select(name => {
						var method = methods.Single(a => a.Name == name);
						try {
							return $"{name}({string.Join(",", method.GetParameters().Select(a => a.Name))})";
						}
						catch (BadImageFormatException) {
							return $"{name}(throws)";
						}
					}).ToArray();
				}
			}
			catch (Exception ex) {
				return new[] { "throws " + ex.GetType().Name };
			}
		}

		static readonly string[] normalParameters = { "MA1(a1x,a1y)", "MA2(a2x,a2y)", "MB1(b1x,b1y)", "MB2(b2x,b2y)", "MC1(c1x,c1y)", "MC2(c2x,c2y)" };

		// The Param table: rows 1-2 are the parameters of MA1, 3-4 of MA2, 5-6 of MB1, 7-8 of MB2, 9-10 of
		// MC1 and 11-12 of MC2.
		//
		// System.Reflection doesn't return the parameters ("throws") if the method has more than one row
		// more than it has parameters. dnlib uses the first row of a parameter.
		[Theory]
		[InlineData("nothing", "normal", "normal")]
		// Lists that start where another list starts
		[InlineData("Method.ParamList of MA2 = 1", "MA1(,) MA2(a1x,a1y)", "MA1(,) MA2(throws)")]
		[InlineData("Method.ParamList of MA2 = 5", "MA2(,)", "MA1(throws) MA2(,)")]
		[InlineData("Method.ParamList of MB1 = 1", "MA2(,) MB1(a1x,a1y)", "MA2(throws) MB1(throws)")]
		// A list that starts in another list. The method then has two rows for a parameter: dnlib uses the
		// first one, like the CLR does when it needs the row of a parameter (FindParamOfMethod() in
		// md/runtime/mdinternalro.cpp). System.Reflection shows the last one.
		[InlineData("Method.ParamList of MA2 = 2", "MA1(a1x,) MA2(a2x,a1y)", "MA1(a1x,)")]
		[InlineData("Method.ParamList of MA2 = 4", "MA2(,a2y)", "MA1(a2x,a1y) MA2(,a2y)")]
		// A list that starts after the next list
		[InlineData("Method.ParamList of MA2 = 7", "MA2(,)", "MA1(throws) MA2(throws)")]
		[InlineData("Method.ParamList of MA2 = 6", "MA2(,)", "MA1(throws) MA2(throws)")]
		// Row 0, the row after the last row, and a row that doesn't exist
		[InlineData("Method.ParamList of MA1 = 0", "MA1(,)", "MA1(throws)")]
		[InlineData("Method.ParamList of MA2 = 0", "MA1(,) MA2(,)", "MA1(throws) MA2(throws)")]
		[InlineData("Method.ParamList of MC2 = 0", "MC1(,) MC2(,)", "MC1(throws) MC2(throws)")]
		[InlineData("Method.ParamList of MC2 = 13", "MC2(,)", "MC1(throws) MC2(,)")]
		[InlineData("Method.ParamList of MC2 = 100", "MC2(,)", "MC1(throws) MC2(throws)")]
		[InlineData("Method.ParamList of MB2 = 13", "MB2(,)", "MB1(throws) MB2(throws)")]
		[InlineData("Method.ParamList of MB2 = 100", "MB2(,)", "MB1(throws) MB2(throws)")]
		public void ParametersOfAMethodAreTheOnesThatTheClrReads(string changes, string expectedDnlib, string expectedRuntime) {
			var image = CreateImage(changes);

			ClrReaderComparison.AssertRead(expectedDnlib, OnlyChanges(ReadParametersWithDnlib(image), normalParameters), expectedRuntime, () => OnlyChanges(ReadParametersWithTheRuntime(image), normalParameters));
		}

		static readonly string[] classNames = { "A", "B", "C" };

		static string[] ReadPropertiesWithDnlib(byte[] image) {
			using (var module = ModuleDefMD.Load(image)) {
				return classNames.Select(name => {
					var type = module.Types.Single(a => a.Name == name);
					return Describe(name, type.Properties.Select(a => a.Name.String).ToArray(), type.Events.Select(a => a.Name.String).ToArray());
				}).ToArray();
			}
		}

		static string[] ReadPropertiesWithTheRuntime(byte[] image) {
			try {
				using (var loaded = new LoadedAssembly(image)) {
					return classNames.Select(name => {
						var type = loaded.Assembly.GetType(name, throwOnError: true)!;
						return $"{name}[{Names(() => type.GetProperties(Declared))}; {Names(() => type.GetEvents(Declared))}]";
					}).ToArray();
				}
			}
			catch (Exception ex) {
				return new[] { "throws " + ex.GetType().Name };
			}
		}

		static string Names(Func<MemberInfo[]> getMembers) {
			try {
				return string.Join(" ", InOrder(getMembers()));
			}
			catch (Exception ex) {
				return "throws " + ex.GetType().Name;
			}
		}

		static readonly string[] normalProperties = { "A[PA1 PA2; EA1 EA2]", "B[PB1 PB2; EB1 EB2]", "C[PC1 PC2; EC1 EC2]" };

		// System.Reflection doesn't return the properties or events of a type ("throws") if one of them has
		// a method of another type: where dnlib reads more than the type's own members, it throws
		[Theory]
		[InlineData("nothing", "normal", "normal")]
		// Lists that start where another list starts
		[InlineData("PropertyMap.PropertyList of 2 = 1", "A[; EA1 EA2] B[PA1 PA2 PB1 PB2; EB1 EB2]", "A[; EA1 EA2] B[throws ArgumentException; EB1 EB2]")]
		[InlineData("PropertyMap.PropertyList of 2 = 5", "A[PA1 PA2 PB1 PB2; EA1 EA2] B[; EB1 EB2]", "A[throws ArgumentException; EA1 EA2] B[; EB1 EB2]")]
		[InlineData("EventMap.EventList of 2 = 1", "A[PA1 PA2; ] B[PB1 PB2; EA1 EA2 EB1 EB2]", "A[PA1 PA2; ] B[PB1 PB2; throws ArgumentException]")]
		[InlineData("EventMap.EventList of 2 = 5", "A[PA1 PA2; EA1 EA2 EB1 EB2] B[PB1 PB2; ]", "A[PA1 PA2; throws ArgumentException] B[PB1 PB2; ]")]
		// A list that starts after the next list
		[InlineData("PropertyMap.PropertyList of 2 = 6", "A[PA1 PA2 PB1 PB2 PC1; EA1 EA2] B[; EB1 EB2]", "A[throws ArgumentException; EA1 EA2] B[; EB1 EB2]")]
		[InlineData("EventMap.EventList of 2 = 6", "A[PA1 PA2; EA1 EA2 EB1 EB2 EC1] B[PB1 PB2; ]", "A[PA1 PA2; throws ArgumentException] B[PB1 PB2; ]")]
		// Row 0, the row after the last row, and a row that doesn't exist
		[InlineData("PropertyMap.PropertyList of 1 = 0", "normal", "normal")]
		[InlineData("PropertyMap.PropertyList of 2 = 0", "A[; EA1 EA2] B[PA1 PA2 PB1 PB2; EB1 EB2]", "A[; EA1 EA2] B[throws ArgumentException; EB1 EB2]")]
		[InlineData("PropertyMap.PropertyList of 3 = 0", "B[; EB1 EB2] C[PA1 PA2 PB1 PB2 PC1 PC2; EC1 EC2]", "B[; EB1 EB2] C[throws ArgumentException; EC1 EC2]")]
		[InlineData("PropertyMap.PropertyList of 3 = 7", "B[PB1 PB2 PC1 PC2; EB1 EB2] C[; EC1 EC2]", "B[throws ArgumentException; EB1 EB2] C[; EC1 EC2]")]
		[InlineData("PropertyMap.PropertyList of 3 = 100", "B[PB1 PB2 PC1 PC2; EB1 EB2] C[; EC1 EC2]", "B[throws ArgumentException; EB1 EB2] C[; EC1 EC2]")]
		[InlineData("PropertyMap.PropertyList of 2 = 100", "A[PA1 PA2 PB1 PB2 PC1 PC2; EA1 EA2] B[; EB1 EB2]", "A[throws ArgumentException; EA1 EA2] B[; EB1 EB2]")]
		[InlineData("EventMap.EventList of 1 = 0", "normal", "normal")]
		[InlineData("EventMap.EventList of 2 = 0", "A[PA1 PA2; ] B[PB1 PB2; EA1 EA2 EB1 EB2]", "A[PA1 PA2; ] B[PB1 PB2; throws ArgumentException]")]
		[InlineData("EventMap.EventList of 3 = 0", "B[PB1 PB2; ] C[PC1 PC2; EA1 EA2 EB1 EB2 EC1 EC2]", "B[PB1 PB2; ] C[PC1 PC2; throws ArgumentException]")]
		[InlineData("EventMap.EventList of 3 = 7", "B[PB1 PB2; EB1 EB2 EC1 EC2] C[PC1 PC2; ]", "B[PB1 PB2; throws ArgumentException] C[PC1 PC2; ]")]
		[InlineData("EventMap.EventList of 3 = 100", "B[PB1 PB2; EB1 EB2 EC1 EC2] C[PC1 PC2; ]", "B[PB1 PB2; throws ArgumentException] C[PC1 PC2; ]")]
		[InlineData("EventMap.EventList of 2 = 100", "A[PA1 PA2; EA1 EA2 EB1 EB2 EC1 EC2] B[PB1 PB2; ]", "A[PA1 PA2; throws ArgumentException] B[PB1 PB2; ]")]
		// Two rows for the same class (the Parent column is the row of the class: <Module> is 1, A is 2)
		[InlineData("PropertyMap.Parent of 2 = 2", "B[; EB1 EB2]", "B[; EB1 EB2]")]
		[InlineData("PropertyMap.Parent of 1 = 3", "A[; EA1 EA2] B[PA1 PA2; EB1 EB2]", "A[; EA1 EA2] B[throws ArgumentException; EB1 EB2]")]
		[InlineData("EventMap.Parent of 2 = 2", "B[PB1 PB2; ]", "B[PB1 PB2; ]")]
		[InlineData("EventMap.Parent of 1 = 3", "A[PA1 PA2; ] B[PB1 PB2; EA1 EA2]", "A[PA1 PA2; ] B[PB1 PB2; throws ArgumentException]")]
		// A row of a class that doesn't exist
		[InlineData("PropertyMap.Parent of 2 = 0", "B[; EB1 EB2]", "B[; EB1 EB2]")]
		[InlineData("PropertyMap.Parent of 2 = 100", "B[; EB1 EB2]", "B[; EB1 EB2]")]
		[InlineData("EventMap.Parent of 2 = 0", "B[PB1 PB2; ]", "B[PB1 PB2; ]")]
		[InlineData("EventMap.Parent of 2 = 100", "B[PB1 PB2; ]", "B[PB1 PB2; ]")]
		public void PropertiesAndEventsOfATypeAreTheOnesThatTheClrReads(string changes, string expectedDnlib, string expectedRuntime) {
			var image = CreateImage(propertyImage, changes);

			ClrReaderComparison.AssertRead(expectedDnlib, OnlyChanges(ReadPropertiesWithDnlib(image), normalProperties), expectedRuntime, () => OnlyChanges(ReadPropertiesWithTheRuntime(image), normalProperties));
		}
	}
}
