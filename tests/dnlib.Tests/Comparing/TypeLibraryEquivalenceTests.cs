// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Comparing {
	/// <summary>
	/// Type equivalence (see <see cref="TypeEquivalenceTests"/>) without a TypeIdentifierAttribute: the types
	/// of an assembly that was imported from a COM type library (ImportedFromTypeLibAttribute or
	/// PrimaryInteropAssemblyAttribute) are equivalent to the types with the same name in another such
	/// assembly if both assemblies have the same GuidAttribute (the ID of the type library). Only public
	/// COM interfaces, structs, enums and delegates that aren't generic can be equivalent. A COM interface
	/// is identified by its own GuidAttribute and not by the assembly's.
	/// </summary>
	public sealed class TypeLibraryEquivalenceTests : IDisposable {
		// {0} is replaced by the assembly's attributes
		const string SourceFormat = @"
using System.Runtime.InteropServices;
{0}

namespace Interop {{
	[ComImport, Guid(""11111111-2222-3333-4444-555555555555""), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IThing {{
		void Do();
	}}

	// Not a COM interface
	public interface IPlain {{
		void Do();
	}}

	public struct Point {{
		public int X;
		public int Y;
	}}

	public enum Color {{ Red, Green }}

	public delegate int Callback(int value);

	public class Class {{
		public int X;
	}}

	public struct Generic<T> {{
		public int X;
	}}

	internal struct Hidden {{
		public int X;
	}}

	public struct Outer {{
		public int X;
		public struct Nested {{
			public int Y;
		}}
	}}

	public class OuterClass {{
		public struct Nested {{
			public int Y;
		}}
	}}
}}";

		const string TypeLibraryAttributes = @"[assembly: ImportedFromTypeLib(""Library"")] [assembly: Guid(""AAAAAAAA-0000-0000-0000-000000000001"")]";
		const string PrimaryInteropAttributes = @"[assembly: PrimaryInteropAssembly(1, 0)] [assembly: Guid(""aaaaaaaa-0000-0000-0000-000000000001"")]";
		const string OtherTypeLibraryAttributes = @"[assembly: ImportedFromTypeLib(""Library"")] [assembly: Guid(""AAAAAAAA-0000-0000-0000-000000000002"")]";
		const string OnlyAGuid = @"[assembly: Guid(""AAAAAAAA-0000-0000-0000-000000000001"")]";

		readonly ModuleDefMD first;
		readonly ModuleDefMD second;
		readonly ModuleDefMD primaryInterop;
		readonly ModuleDefMD otherLibrary;
		readonly ModuleDefMD notALibrary;

		public TypeLibraryEquivalenceTests() {
			first = Compile("First", TypeLibraryAttributes);
			second = Compile("Second", TypeLibraryAttributes);
			primaryInterop = Compile("PrimaryInterop", PrimaryInteropAttributes);
			otherLibrary = Compile("OtherLibrary", OtherTypeLibraryAttributes);
			notALibrary = Compile("NotALibrary", OnlyAGuid);
		}

		static ModuleDefMD Compile(string assemblyName, string assemblyAttributes) =>
			ModuleDefMD.Load(CSharpCompiler.Compile(string.Format(SourceFormat, assemblyAttributes), new CSharpCompilerOptions { AssemblyName = assemblyName }).Image);

		public void Dispose() {
			first.Dispose();
			second.Dispose();
			primaryInterop.Dispose();
			otherLibrary.Dispose();
			notALibrary.Dispose();
		}

		static TypeDef Find(ModuleDef module, string name) => module.Find("Interop." + name, isReflectionName: true)!;

		static void AssertEquivalent(bool expected, TypeDef a, TypeDef b) {
			Assert.Equal(expected, new SigComparer().Equals(a, b));
			Assert.Equal(expected, new SigComparer().Equals(b, a));
			if (expected)
				Assert.Equal(new SigComparer().GetHashCode(a), new SigComparer().GetHashCode(b));
			Assert.False(new SigComparer(SigComparerOptions.DontCheckTypeEquivalence).Equals(a, b));
		}

		[Theory]
		[InlineData("IThing")]
		[InlineData("Point")]
		[InlineData("Color")]
		[InlineData("Callback")]
		[InlineData("Outer")]
		[InlineData("Outer+Nested")]
		public void TypesOfTheSameTypeLibraryAreEquivalent(string typeName) =>
			AssertEquivalent(true, Find(first, typeName), Find(second, typeName));

		// The GUIDs are compared without case
		[Theory]
		[InlineData("IThing")]
		[InlineData("Point")]
		public void PrimaryInteropAssemblyIsATypeLibraryToo(string typeName) =>
			AssertEquivalent(true, Find(first, typeName), Find(primaryInterop, typeName));

		[Theory]
		[InlineData("IPlain")]
		[InlineData("Class")]
		[InlineData("Generic`1")]
		[InlineData("Hidden")]
		[InlineData("OuterClass+Nested")]
		public void OtherKindsOfTypesAreNotEquivalent(string typeName) =>
			AssertEquivalent(false, Find(first, typeName), Find(second, typeName));

		[Theory]
		[InlineData("IThing", true)]
		[InlineData("Point", false)]
		[InlineData("Color", false)]
		[InlineData("Callback", false)]
		public void OnlyComInterfacesAreEquivalentIfTheAssembliesHaveDifferentGuids(string typeName, bool expected) =>
			AssertEquivalent(expected, Find(first, typeName), Find(otherLibrary, typeName));

		[Theory]
		[InlineData("IThing")]
		[InlineData("Point")]
		public void AssemblyThatIsNotATypeLibraryHasNoEquivalentTypes(string typeName) =>
			AssertEquivalent(false, Find(first, typeName), Find(notALibrary, typeName));

		[Fact]
		public void TypesWithOtherNamesAreNotEquivalent() =>
			AssertEquivalent(false, Find(first, "Point"), Find(second, "Outer"));
	}
}
