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
	/// The GenericParam table has the generic parameters of types and methods: the number of the parameter,
	/// its owner, its flags and its name. These tests write values that a compiler doesn't write (a number
	/// twice, a number that's too high, an owner that doesn't exist) and read the parameters with dnlib and
	/// with the runtime that runs the tests. If the runtime reads the parameters, dnlib must read the same.
	/// </summary>
	public class ClrGenericParamRuleTests {
		// The TypeDef table: Marker is row 2, Other is row 3. M is row 1 of the Method table.
		// The GenericParam table (sorted by owner and number): rows 1-2 are MA and MB of the method M,
		// rows 3-4 are TA and TB of Marker, row 5 is TC of Other.
		// The GenericParamConstraint table: row 1 is IDisposable of TA.
		const string Source = @"
public static class Marker<TA, TB> where TA : System.IDisposable {
	public static void M<MA, MB>() { }
}
public static class Other<TC> { }
";
		static readonly byte[] baseImage = Compile();

		// The tests use row numbers, so the file must only have the types of the source code: the compiler
		// can add other types (attributes that the target framework doesn't have)
		static byte[] Compile() {
			using (var module = ModuleDefMD.Load(CSharpCompiler.Compile(Source).Image)) {
				foreach (var type in module.Types.Where(a => !a.IsGlobalModuleType && !a.Name.StartsWith("Marker") && !a.Name.StartsWith("Other")).ToArray())
					module.Types.Remove(type);
				module.CustomAttributes.Clear();
				var stream = new MemoryStream();
				module.Write(stream);
				return stream.ToArray();
			}
		}

		[Fact]
		public void TheFileHasTheRowsThatTheTestsExpect() {
			var patcher = new MetadataPatcher(baseImage);
			using (var module = ModuleDefMD.Load(baseImage)) {
				Assert.Equal(new[] { "<Module>", "Marker`2", "Other`1" }, module.GetTypes().OrderBy(a => a.Rid).Select(a => a.Name.String).ToArray());
				// The number, the owner and the name
				var parameters = Enumerable.Range(1, 5).Select(a => $"{patcher.GetCell(Table.GenericParam, (uint)a, "Number")} {patcher.GetCell(Table.GenericParam, (uint)a, "Owner")} {module.StringsStream.ReadNoNull(patcher.GetCell(Table.GenericParam, (uint)a, "Name"))}").ToArray();
				Assert.Equal(new[] { "0 3 MA", "1 3 MB", "0 4 TA", "1 4 TB", "0 6 TC" }, parameters);
				Assert.Equal(3U, patcher.GetCell(Table.GenericParamConstraint, 1, "Owner"));
			}
		}

		// "Table.Column of N = value": writes the value to that column of row N. More than one change is
		// separated by a comma.
		static byte[] CreateImage(string changes) {
			var patcher = new MetadataPatcher(baseImage);
			if (changes == "nothing")
				return patcher.ToArray();
			foreach (var change in changes.Split(',')) {
				var parts = change.Split(new[] { ' ', '.', '=' }, StringSplitOptions.RemoveEmptyEntries);
				Assert.Equal(5, parts.Length);
				patcher.SetCell((Table)Enum.Parse(typeof(Table), parts[0]), uint.Parse(parts[3]), parts[1], uint.Parse(parts[4]));
			}
			return patcher.ToArray();
		}

		// "0:TA(IDisposable)": the number and the name of a parameter, and its constraints
		static string Describe(int number, string name, string[] constraints) =>
			$"{number}:{name}{(constraints.Length == 0 ? "" : "(" + string.Join(",", constraints) + ")")}";

		const uint MarkerRid = 2, OtherRid = 3, MethodRid = 1;

		static string[] ReadWithDnlib(byte[] image) {
			using (var module = ModuleDefMD.Load(image)) {
				string Read(string name, ITypeOrMethodDef owner) =>
					$"{name}[{string.Join(" ", owner.GenericParameters.Select(a => Describe(a.Number, a.Name, a.GenericParamConstraints.Select(b => b.Constraint?.Name.String ?? "null").ToArray())))}]";
				return new[] {
					Read("Marker", module.ResolveTypeDef(MarkerRid)!),
					Read("M", module.ResolveMethod(MethodRid)!),
					Read("Other", module.ResolveTypeDef(OtherRid)!),
				};
			}
		}

		// Every owner is read on its own: "throws" and the exception if the CLR doesn't read it
		static string[] ReadWithTheRuntime(byte[] image) {
			try {
				using (var loaded = new LoadedAssembly(image)) {
					var module = loaded.Assembly.ManifestModule;
					string Read(string name, Func<Type[]> getParameters) {
						try {
							return $"{name}[{string.Join(" ", getParameters().Select(a => Describe(a.GenericParameterPosition, a.Name, a.GetGenericParameterConstraints().Select(b => b.Name).ToArray())))}]";
						}
						catch (Exception ex) {
							return $"{name}[throws {ex.GetType().Name}]";
						}
					}
					return new[] {
						Read("Marker", () => module.ResolveType(new MDToken(Table.TypeDef, MarkerRid).ToInt32()).GetGenericArguments()),
						Read("M", () => module.ResolveMethod(new MDToken(Table.Method, MethodRid).ToInt32())!.GetGenericArguments()),
						Read("Other", () => module.ResolveType(new MDToken(Table.TypeDef, OtherRid).ToInt32()).GetGenericArguments()),
					};
				}
			}
			catch (Exception ex) {
				return new[] { "throws " + ex.GetType().Name };
			}
		}

		static readonly string[] normal = { "Marker[0:TA(IDisposable) 1:TB]", "M[0:MA 1:MB]", "Other[0:TC]" };

		// What was read, without the owners that have what the compiler gave them
		static string OnlyChanges(string[] read) {
			if (read.Length != normal.Length)
				return string.Join(" ", read);
			var changes = read.Where((a, i) => a != normal[i]).ToArray();
			return changes.Length == 0 ? "normal" : string.Join(" ", changes);
		}

		[Theory]
		[InlineData("nothing", "normal", "normal")]
		// The number of a parameter. Fixed in this commit: the CLR doesn't use the Number column, the number
		// of a parameter is its index in the rows of its owner. dnlib used the column, so !0 in a
		// signature could be another parameter than in the CLR.
		[InlineData("GenericParam.Number of 4 = 0", "normal", "normal")]
		[InlineData("GenericParam.Number of 4 = 5", "normal", "normal")]
		[InlineData("GenericParam.Number of 4 = 65535", "normal", "normal")]
		[InlineData("GenericParam.Number of 3 = 1", "normal", "normal")]
		[InlineData("GenericParam.Number of 3 = 1,GenericParam.Number of 4 = 0", "normal", "normal")]
		[InlineData("GenericParam.Number of 2 = 0", "normal", "normal")]
		[InlineData("GenericParam.Number of 2 = 5", "normal", "normal")]
		[InlineData("GenericParam.Number of 5 = 1", "normal", "normal")]
		// The owner: the row of the type * 2, or the row of the method * 2 + 1
		[InlineData("GenericParam.Owner of 4 = 6", "Marker[0:TA(IDisposable)] Other[0:TB 1:TC]", "Marker[0:TA(IDisposable)] Other[0:TB 1:TC]")]
		[InlineData("GenericParam.Owner of 5 = 4", "Marker[0:TA(IDisposable) 1:TB 2:TC] Other[]", "Marker[0:TA(IDisposable) 1:TB 2:TC] Other[]")]
		[InlineData("GenericParam.Owner of 2 = 4", "Marker[0:MB 1:TA(IDisposable) 2:TB] M[0:MA]", "Marker[0:MB 1:TA(IDisposable) 2:TB] M[0:MA]")]
		[InlineData("GenericParam.Owner of 4 = 3", "Marker[0:TA(IDisposable)]", "Marker[0:TA(IDisposable)]")]
		[InlineData("GenericParam.Owner of 3 = 0", "Marker[0:TB] M[]", "Marker[0:TB] M[]")]
		[InlineData("GenericParam.Owner of 5 = 200", "Other[]", "Other[]")]
		[InlineData("GenericParam.Owner of 5 = 7", "Other[]", "Other[]")]
		[InlineData("GenericParam.Owner of 5 = 2", "Other[]", "Other[]")]
		// The flags (1 and 2: variance, 4 to 16: kinds of constraints) and the name
		[InlineData("GenericParam.Flags of 3 = 1", "normal", "Marker[throws TypeLoadException] M[throws TypeLoadException]")]
		[InlineData("GenericParam.Flags of 3 = 3", "normal", "Marker[throws TypeLoadException] M[throws TypeLoadException]")]
		[InlineData("GenericParam.Flags of 3 = 4", "normal", "normal")]
		[InlineData("GenericParam.Flags of 3 = 12", "normal", "normal")]
		[InlineData("GenericParam.Flags of 3 = 65280", "normal", "normal")]
		[InlineData("GenericParam.Name of 3 = 0", "Marker[0:(IDisposable) 1:TB]", "Marker[0:(IDisposable) 1:TB]")]
		// The constraint: the row of the parameter, and the token of the type (row * 4 + table)
		[InlineData("GenericParamConstraint.Owner of 1 = 4", "Marker[0:TA 1:TB(IDisposable)]", "Marker[0:TA 1:TB(IDisposable)]")]
		[InlineData("GenericParamConstraint.Owner of 1 = 1", "Marker[0:TA 1:TB] M[0:MA(IDisposable) 1:MB]", "Marker[0:TA 1:TB] M[0:MA(IDisposable) 1:MB]")]
		[InlineData("GenericParamConstraint.Owner of 1 = 0", "Marker[0:TA 1:TB]", "Marker[0:TA 1:TB]")]
		[InlineData("GenericParamConstraint.Owner of 1 = 100", "Marker[0:TA 1:TB]", "Marker[0:TA 1:TB]")]
		[InlineData("GenericParamConstraint.Constraint of 1 = 0", "Marker[0:TA(null) 1:TB]", "Marker[throws TypeLoadException] M[throws TypeLoadException]")]
		[InlineData("GenericParamConstraint.Constraint of 1 = 3", "Marker[0:TA(null) 1:TB]", "Marker[throws TypeLoadException] M[throws TypeLoadException]")]
		[InlineData("GenericParamConstraint.Constraint of 1 = 8", "Marker[0:TA(Marker`2) 1:TB]", "Marker[throws TypeLoadException] M[throws TypeLoadException]")]
		[InlineData("GenericParamConstraint.Constraint of 1 = 400", "Marker[0:TA(null) 1:TB]", "Marker[throws TypeLoadException] M[throws TypeLoadException]")]
		public void GenericParameters(string changes, string expectedDnlib, string expectedRuntime) {
			var image = CreateImage(changes);

			ClrReaderComparison.AssertRead(expectedDnlib, OnlyChanges(ReadWithDnlib(image)), expectedRuntime, () => OnlyChanges(ReadWithTheRuntime(image)));
		}

		// The writer sorts the rows by owner and number: with the numbers of the columns, the two parameters
		// would change places and !0 would be another parameter in the written file
		[Fact]
		public void ParametersKeepTheirOrderWhenTheFileIsWritten() {
			using (var module = ModuleDefMD.Load(CreateImage("GenericParam.Number of 3 = 1,GenericParam.Number of 4 = 0"))) {
				var type = module.ResolveTypeDef(MarkerRid)!;
				Assert.Equal("TA", new GenericVar(0, type).GenericParam!.Name);
				Assert.Equal("TB", new GenericVar(1, type).GenericParam!.Name);

				var stream = new MemoryStream();
				module.Write(stream);
				var image = stream.ToArray();
				Assert.Equal("normal", OnlyChanges(ReadWithDnlib(image)));
				if (ClrReaderComparison.IsDotNet)
					Assert.Equal("normal", OnlyChanges(ReadWithTheRuntime(image)));
			}
		}
	}
}
