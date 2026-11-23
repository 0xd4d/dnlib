// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Comparing {
	/// <summary>
	/// Type equivalence ("embedded interop types", ECMA-335 doesn't have it, see the CLR's siginfo.cpp): two
	/// types in different assemblies are the same type for the CLR if both have a TypeIdentifierAttribute (or
	/// are COM interfaces with the same GUID) and the same members. <see cref="SigComparer"/> does the same
	/// unless <see cref="SigComparerOptions.DontCheckTypeEquivalence"/> is set.
	/// </summary>
	public sealed class TypeEquivalenceTests : IDisposable {
		// {0} is replaced by the type of Point's second field and of the delegate's parameter
		const string SourceFormat = @"
using System.Runtime.InteropServices;

namespace Interop {{
	[ComImport, Guid(""11111111-2222-3333-4444-555555555555""), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), TypeIdentifier]
	public interface IThing {{
		void Do();
	}}

	[TypeIdentifier(""scope"", ""Interop.Point"")]
	public struct Point {{
		public int X;
		public {0} Y;
	}}

	[TypeIdentifier(""scope"", ""Interop.Color"")]
	public enum Color {{ Red, Green }}

	[TypeIdentifier(""scope"", ""Interop.Callback"")]
	public delegate int Callback({0} value);

	// No TypeIdentifierAttribute
	public struct Plain {{
		public int X;
	}}
}}";

		readonly ModuleDefMD first;
		readonly ModuleDefMD second;
		readonly ModuleDefMD different;

		public TypeEquivalenceTests() {
			first = Compile("First", "int");
			second = Compile("Second", "int");
			different = Compile("Different", "long");
		}

		static ModuleDefMD Compile(string assemblyName, string memberType) =>
			ModuleDefMD.Load(CSharpCompiler.Compile(string.Format(SourceFormat, memberType), new CSharpCompilerOptions { AssemblyName = assemblyName }).Image);

		public void Dispose() {
			first.Dispose();
			second.Dispose();
			different.Dispose();
		}

		static TypeDef Find(ModuleDef module, string name) => module.Find("Interop." + name, isReflectionName: true)!;

		static bool AreEqual(TypeDef a, TypeDef b, SigComparerOptions options = 0) => new SigComparer(options).Equals(a, b);

		[Theory]
		[InlineData("IThing")]
		[InlineData("Point")]
		[InlineData("Color")]
		[InlineData("Callback")]
		public void TypesWithTheSameIdentityAndMembersAreEqual(string typeName) {
			var a = Find(first, typeName);
			var b = Find(second, typeName);

			Assert.True(AreEqual(a, b));
			Assert.True(AreEqual(b, a));
			Assert.Equal(new SigComparer().GetHashCode(a), new SigComparer().GetHashCode(b));
			Assert.True(new SigComparer().Equals(a.ToTypeSig(), b.ToTypeSig()));
		}

		[Theory]
		[InlineData("IThing")]
		[InlineData("Point")]
		[InlineData("Color")]
		[InlineData("Callback")]
		public void TypesAreNotEqualIfTypeEquivalenceIsNotChecked(string typeName) =>
			Assert.False(AreEqual(Find(first, typeName), Find(second, typeName), SigComparerOptions.DontCheckTypeEquivalence));

		[Fact]
		public void TypesWithoutTheAttributeAreNotEqual() =>
			Assert.False(AreEqual(Find(first, "Plain"), Find(second, "Plain")));

		// Fixed in 5207f81a: the fields of structs and the Invoke methods of delegates weren't compared
		[Theory]
		[InlineData("Point")]
		[InlineData("Callback")]
		public void TypesWithOtherMembersAreNotEqual(string typeName) {
			Assert.False(AreEqual(Find(first, typeName), Find(different, typeName)));
			Assert.False(AreEqual(Find(different, typeName), Find(first, typeName)));
		}

		[Fact]
		public void TypesWithOtherNamesAreNotEqual() =>
			Assert.False(AreEqual(Find(first, "Point"), Find(second, "Color")));

		// A signature that uses the types: 'Point Method(Color)'
		[Fact]
		public void MethodSignaturesWithEquivalentTypesAreEqual() {
			MethodSig CreateSignature(ModuleDef module) =>
				MethodSig.CreateStatic(Find(module, "Point").ToTypeSig(), Find(module, "Color").ToTypeSig());

			Assert.True(new SigComparer().Equals(CreateSignature(first), CreateSignature(second)));
			Assert.False(new SigComparer().Equals(CreateSignature(first), CreateSignature(different)));
			Assert.False(new SigComparer(SigComparerOptions.DontCheckTypeEquivalence).Equals(CreateSignature(first), CreateSignature(second)));
		}
	}
}
