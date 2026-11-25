// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// The files of <see cref="ClrMemberListRuleTests"/> (types and methods whose lists of fields, methods,
	/// parameters, properties and events have values that a compiler doesn't write) read like Mono reads them
	/// (<see cref="CLRRuntimeReaderKind.Mono"/>). A small program that the <c>mono</c> command runs reads the
	/// same members: if Mono reads them, dnlib must read the same ones.
	/// </summary>
	/// <remarks>
	/// Mono finds the row of a type in the PropertyMap and the EventMap table with a binary search (the CLR
	/// uses the first row of the type), so it doesn't find a row if the table isn't sorted. Fixed in this
	/// commit: dnlib used the first row like the CLR does.
	///
	/// If a method has two Param rows with the same number, Mono uses the name of the last row and dnlib the
	/// first row (like the CLR): the cases with "Method.ParamList" where both read parameters have other names.
	///
	/// Mono is only asked by the tests for .NET Framework (the files then reference mscorlib) and only if the
	/// <c>mono</c> command exists. Every file needs a new process: Mono stops the process if it doesn't like
	/// the metadata.
	/// </remarks>
	public sealed class MonoMemberListRuleTests : IClassFixture<MonoMemberListRuleTests.Fixture> {
		// The second argument says what is read. The result has a line for every type or method, like the
		// methods of ClrMemberListRuleTests that read with the runtime.
		const string ProgramSource = @"
using System;
using System.Linq;
using System.Reflection;
static class Program {
	const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

	static string Names(Func<MemberInfo[]> getMembers) {
		try {
			return string.Join("" "", getMembers().OrderBy(a => a.MetadataToken).Select(a => a.Name).ToArray());
		}
		catch (Exception ex) {
			return ""throws "" + ex.GetType().Name;
		}
	}

	static string[] Read(string file, string what) {
		var assembly = Assembly.LoadFile(file);
		var module = assembly.ManifestModule;
		switch (what) {
		case ""members"":
			return new[] { ""<Module>"", ""A"", ""B"", ""C"" }.Select(name => {
				if (name == ""<Module>"")
					return name + ""["" + Names(() => module.GetFields(Declared)) + ""; "" + Names(() => module.GetMethods(Declared)) + ""]"";
				try {
					var type = assembly.GetType(name, true);
					return name + ""["" + Names(() => type.GetFields(Declared)) + ""; "" + Names(() => type.GetMethods(Declared)) + ""]"";
				}
				catch (Exception ex) {
					return name + ""[throws "" + ex.GetType().Name + ""]"";
				}
			}).ToArray();
		case ""parameters"":
			var methods = assembly.GetTypes().SelectMany(a => a.GetMethods(Declared)).ToArray();
			return new[] { ""MA1"", ""MA2"", ""MB1"", ""MB2"", ""MC1"", ""MC2"" }.Select(name => {
				try {
					return name + ""("" + string.Join("","", methods.Single(a => a.Name == name).GetParameters().Select(a => a.Name).ToArray()) + "")"";
				}
				catch (Exception) {
					return name + ""(throws)"";
				}
			}).ToArray();
		default:
			return new[] { ""A"", ""B"", ""C"" }.Select(name => {
				var type = assembly.GetType(name, true);
				return name + ""["" + Names(() => type.GetProperties(Declared)) + ""; "" + Names(() => type.GetEvents(Declared)) + ""]"";
			}).ToArray();
		}
	}

	static int Main(string[] args) {
		string result;
		try {
			result = string.Join(""\n"", Read(args[0], args[1]));
		}
		catch (Exception ex) {
			result = ""throws "" + ex.GetType().Name;
		}
		Console.Write(""<result>"" + result);
		return 0;
	}
}
";

		public sealed class Fixture : IDisposable {
			public MonoProgram Program { get; } = new MonoProgram(ProgramSource);
			public void Dispose() => Program.Dispose();
		}

		readonly Fixture fixture;

		public MonoMemberListRuleTests(Fixture fixture) => this.fixture = fixture;

		// true if the tests can ask Mono
		static bool IsMonoAsked {
			get {
#if NETFRAMEWORK
				return MonoRuntime.IsAvailable;
#else
				return false;
#endif
			}
		}

		// What was read, without the types or methods that have what the compiler gave them ("normal" if all of
		// them have that). The kinds are the test methods of ClrMemberListRuleTests.
		static string ReadWithDnlib(string kind, byte[] image) {
			switch (kind) {
			case "members":
				return ClrMemberListRuleTests.OnlyChanges(ClrMemberListRuleTests.ReadMembersWithDnlib(image, CLRRuntimeReaderKind.Mono), ClrMemberListRuleTests.normalMembers);
			case "parameters":
				return ClrMemberListRuleTests.OnlyChanges(ClrMemberListRuleTests.ReadParametersWithDnlib(image, CLRRuntimeReaderKind.Mono), ClrMemberListRuleTests.normalParameters);
			default:
				return ClrMemberListRuleTests.OnlyChanges(ClrMemberListRuleTests.ReadPropertiesWithDnlib(image, CLRRuntimeReaderKind.Mono), ClrMemberListRuleTests.normalProperties);
			}
		}

		string ReadWithMono(string kind, byte[] image) {
			var output = fixture.Program.Run(image, kind);
			if (output is null)
				return "stops the process";
			var normal = kind == "members" ? ClrMemberListRuleTests.normalMembers : kind == "parameters" ? ClrMemberListRuleTests.normalParameters : ClrMemberListRuleTests.normalProperties;
			return ClrMemberListRuleTests.OnlyChanges(output.Split('\n'), normal);
		}

		// "Table.Column of X = value": writes the value to that column of the row of the type or method X (or
		// of row X if it's a number)
		[Theory]
		[InlineData("members", "nothing", "normal", "normal")]
		[InlineData("members", "TypeDef.MethodList of B = 1", "A[a1 a2; ] B[b1 b2; MA1 MA2 MB1 MB2]", "A[a1 a2; ] B[b1 b2; MA1 MA2 MB1 MB2]")]
		[InlineData("members", "TypeDef.MethodList of B = 5", "A[a1 a2; MA1 MA2 MB1 MB2] B[b1 b2; ]", "A[a1 a2; MA1 MA2 MB1 MB2] B[b1 b2; ]")]
		[InlineData("members", "TypeDef.MethodList of A = 3", "<Module>[; MA1 MA2] A[a1 a2; ]", "<Module>[; MA1 MA2] A[a1 a2; ]")]
		[InlineData("members", "TypeDef.FieldList of B = 1", "A[; MA1 MA2] B[a1 a2 b1 b2; MB1 MB2]", "A[; MA1 MA2] B[a1 a2 b1 b2; MB1 MB2]")]
		[InlineData("members", "TypeDef.FieldList of A = 3", "<Module>[a1 a2; ] A[; MA1 MA2]", "<Module>[a1 a2; ] A[; MA1 MA2]")]
		[InlineData("members", "TypeDef.MethodList of B = 6", "A[a1 a2; MA1 MA2 MB1 MB2 MC1] B[b1 b2; ]", "stops the process")]
		[InlineData("members", "TypeDef.MethodList of A = 4", "<Module>[; MA1 MA2 MB1] A[a1 a2; ]", "stops the process")]
		[InlineData("members", "TypeDef.FieldList of B = 6", "A[a1 a2 b1 b2 c1; MA1 MA2] B[; MB1 MB2]", "stops the process")]
		[InlineData("members", "TypeDef.MethodList of A = 3,TypeDef.MethodList of B = 1", "<Module>[; MA1 MA2] A[a1 a2; ] B[b1 b2; MA1 MA2 MB1 MB2]", "stops the process")]
		[InlineData("members", "TypeDef.MethodList of B = 0", "A[a1 a2; ] B[b1 b2; ]", "stops the process")]
		[InlineData("members", "TypeDef.MethodList of C = 0", "B[b1 b2; ] C[c1 c2; ]", "stops the process")]
		[InlineData("members", "TypeDef.MethodList of C = 7", "B[b1 b2; MB1 MB2 MC1 MC2] C[c1 c2; ]", "B[b1 b2; MB1 MB2 MC1 MC2] C[c1 c2; ]")]
		[InlineData("members", "TypeDef.MethodList of C = 100", "B[b1 b2; MB1 MB2 MC1 MC2] C[c1 c2; ]", "B[b1 b2; throws TypeLoadException] C[c1 c2; ]")]
		[InlineData("members", "TypeDef.MethodList of B = 7", "A[a1 a2; MA1 MA2 MB1 MB2 MC1 MC2] B[b1 b2; ]", "A[a1 a2; MA1 MA2 MB1 MB2 MC1 MC2] B[b1 b2; ]")]
		[InlineData("members", "TypeDef.MethodList of B = 100", "A[a1 a2; MA1 MA2 MB1 MB2 MC1 MC2] B[b1 b2; ]", "A[a1 a2; throws TypeLoadException] B[b1 b2; ]")]
		[InlineData("members", "TypeDef.FieldList of B = 0", "A[; MA1 MA2] B[; MB1 MB2]", "stops the process")]
		[InlineData("members", "TypeDef.FieldList of C = 0", "B[; MB1 MB2] C[; MC1 MC2]", "stops the process")]
		[InlineData("members", "TypeDef.FieldList of C = 7", "B[b1 b2 c1 c2; MB1 MB2] C[; MC1 MC2]", "B[b1 b2 c1 c2; MB1 MB2] C[; MC1 MC2]")]
		[InlineData("members", "TypeDef.FieldList of C = 100", "B[b1 b2 c1 c2; MB1 MB2] C[; MC1 MC2]", "stops the process")]
		[InlineData("members", "TypeDef.FieldList of B = 100", "A[a1 a2 b1 b2 c1 c2; MA1 MA2] B[; MB1 MB2]", "stops the process")]
		[InlineData("parameters", "nothing", "normal", "normal")]
		[InlineData("parameters", "Method.ParamList of MA2 = 1", "MA1(,) MA2(a1x,a1y)", "MA1(,)")]
		[InlineData("parameters", "Method.ParamList of MA2 = 5", "MA2(,)", "MA1(a2x,a2y) MA2(,)")]
		[InlineData("parameters", "Method.ParamList of MB1 = 1", "MA2(,) MB1(a1x,a1y)", "MA2(,)")]
		[InlineData("parameters", "Method.ParamList of MA2 = 2", "MA1(a1x,) MA2(a2x,a1y)", "MA1(a1x,)")]
		[InlineData("parameters", "Method.ParamList of MA2 = 4", "MA2(,a2y)", "MA1(a2x,a1y) MA2(,a2y)")]
		[InlineData("parameters", "Method.ParamList of MA2 = 7", "MA2(,)", "MA1(b1x,b1y) MA2(,)")]
		[InlineData("parameters", "Method.ParamList of MA2 = 6", "MA2(,)", "MA1(b1x,a2y) MA2(,)")]
		[InlineData("parameters", "Method.ParamList of MA1 = 0", "MA1(,)", "stops the process")]
		[InlineData("parameters", "Method.ParamList of MA2 = 0", "MA1(,) MA2(,)", "stops the process")]
		[InlineData("parameters", "Method.ParamList of MC2 = 0", "MC1(,) MC2(,)", "stops the process")]
		[InlineData("parameters", "Method.ParamList of MC2 = 13", "MC2(,)", "MC1(c2x,c2y) MC2(,)")]
		[InlineData("parameters", "Method.ParamList of MC2 = 100", "MC2(,)", "stops the process")]
		[InlineData("parameters", "Method.ParamList of MB2 = 13", "MB2(,)", "MB1(c2x,c2y) MB2(,)")]
		[InlineData("parameters", "Method.ParamList of MB2 = 100", "MB2(,)", "stops the process")]
		[InlineData("properties", "nothing", "normal", "normal")]
		[InlineData("properties", "PropertyMap.PropertyList of 2 = 1", "A[; EA1 EA2] B[PA1 PA2 PB1 PB2; EB1 EB2]", "stops the process")]
		[InlineData("properties", "PropertyMap.PropertyList of 2 = 5", "A[PA1 PA2 PB1 PB2; EA1 EA2] B[; EB1 EB2]", "stops the process")]
		[InlineData("properties", "EventMap.EventList of 2 = 1", "A[PA1 PA2; ] B[PB1 PB2; EA1 EA2 EB1 EB2]", "stops the process")]
		[InlineData("properties", "EventMap.EventList of 2 = 5", "A[PA1 PA2; EA1 EA2 EB1 EB2] B[PB1 PB2; ]", "stops the process")]
		[InlineData("properties", "PropertyMap.PropertyList of 2 = 6", "A[PA1 PA2 PB1 PB2 PC1; EA1 EA2] B[; EB1 EB2]", "stops the process")]
		[InlineData("properties", "EventMap.EventList of 2 = 6", "A[PA1 PA2; EA1 EA2 EB1 EB2 EC1] B[PB1 PB2; ]", "stops the process")]
		[InlineData("properties", "PropertyMap.PropertyList of 1 = 0", "A[; EA1 EA2]", "A[; EA1 EA2]")]
		[InlineData("properties", "PropertyMap.PropertyList of 2 = 0", "A[; EA1 EA2] B[; EB1 EB2]", "stops the process")]
		[InlineData("properties", "PropertyMap.PropertyList of 3 = 0", "B[; EB1 EB2] C[; EC1 EC2]", "stops the process")]
		[InlineData("properties", "PropertyMap.PropertyList of 3 = 7", "B[PB1 PB2 PC1 PC2; EB1 EB2] C[; EC1 EC2]", "B[PB1 PB2 PC1 PC2; EB1 EB2] C[; EC1 EC2]")]
		[InlineData("properties", "PropertyMap.PropertyList of 3 = 100", "B[PB1 PB2 PC1 PC2; EB1 EB2] C[; EC1 EC2]", "stops the process")]
		[InlineData("properties", "PropertyMap.PropertyList of 2 = 100", "A[PA1 PA2 PB1 PB2 PC1 PC2; EA1 EA2] B[; EB1 EB2]", "stops the process")]
		[InlineData("properties", "EventMap.EventList of 1 = 0", "A[PA1 PA2; ]", "stops the process")]
		[InlineData("properties", "EventMap.EventList of 2 = 0", "A[PA1 PA2; ] B[PB1 PB2; ]", "stops the process")]
		[InlineData("properties", "EventMap.EventList of 3 = 0", "B[PB1 PB2; ] C[PC1 PC2; ]", "stops the process")]
		[InlineData("properties", "EventMap.EventList of 3 = 7", "B[PB1 PB2; EB1 EB2 EC1 EC2] C[PC1 PC2; ]", "stops the process")]
		[InlineData("properties", "EventMap.EventList of 3 = 100", "B[PB1 PB2; EB1 EB2 EC1 EC2] C[PC1 PC2; ]", "stops the process")]
		[InlineData("properties", "EventMap.EventList of 2 = 100", "A[PA1 PA2; EA1 EA2 EB1 EB2 EC1 EC2] B[PB1 PB2; ]", "stops the process")]
		[InlineData("properties", "PropertyMap.Parent of 2 = 2", "A[PB1 PB2; EA1 EA2] B[; EB1 EB2]", "stops the process")]
		[InlineData("properties", "PropertyMap.Parent of 1 = 3", "A[; EA1 EA2]", "A[; EA1 EA2]")]
		[InlineData("properties", "EventMap.Parent of 2 = 2", "A[PA1 PA2; EB1 EB2] B[PB1 PB2; ]", "stops the process")]
		[InlineData("properties", "EventMap.Parent of 1 = 3", "A[PA1 PA2; ]", "A[PA1 PA2; ]")]
		[InlineData("properties", "PropertyMap.Parent of 2 = 0", "A[; EA1 EA2] B[; EB1 EB2]", "A[; EA1 EA2] B[; EB1 EB2]")]
		[InlineData("properties", "PropertyMap.Parent of 2 = 100", "B[; EB1 EB2] C[; EC1 EC2]", "B[; EB1 EB2] C[; EC1 EC2]")]
		[InlineData("properties", "EventMap.Parent of 2 = 0", "A[PA1 PA2; ] B[PB1 PB2; ]", "A[PA1 PA2; ] B[PB1 PB2; ]")]
		[InlineData("properties", "EventMap.Parent of 2 = 100", "B[PB1 PB2; ] C[PC1 PC2; ]", "B[PB1 PB2; ] C[PC1 PC2; ]")]
		public void MembersAreTheOnesThatMonoReads(string kind, string changes, string expectedDnlib, string expectedMono) {
			var image = kind == "properties" ? ClrMemberListRuleTests.CreateImage(ClrMemberListRuleTests.propertyImage, changes) : ClrMemberListRuleTests.CreateImage(changes);
			string dnlib = ReadWithDnlib(kind, image);

			Assert.Equal(expectedDnlib, dnlib);
			if (IsMonoAsked)
				Assert.Equal(expectedMono, ReadWithMono(kind, image));
		}
	}
}
