// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Comparing {
	/// <summary>
	/// The same type can be a <see cref="TypeDef"/> (its definition), a <see cref="TypeRef"/> (a reference from
	/// another assembly), an <see cref="ExportedType"/> (a type forwarder), a <see cref="TypeSig"/> (in a
	/// signature) or a <see cref="TypeSpec"/> (a signature in the TypeSpec table). <see cref="SigComparer"/>
	/// has a method for every pair of them: all must agree, in both directions, and equal types must have the
	/// same hash code.
	/// </summary>
	public sealed class TypeFormComparisonTests : IDisposable {
		const string LibrarySource = @"
namespace Ns {
	public class Outer {
		public class Inner { }
	}
	public class Other { }
}";

		// The five forms of one type
		sealed class TypeForms {
			public TypeDef Def = null!;
			public TypeRef Ref = null!;
			public ExportedType Exported = null!;
			public TypeSig Sig = null!;
			public TypeSpec Spec = null!;
		}

		readonly ModuleDefMD library;
		// The module that references the library's types
		readonly ModuleDefUser user;

		public TypeFormComparisonTests() {
			library = ModuleDefMD.Load(CSharpCompiler.Compile(LibrarySource, new CSharpCompilerOptions { AssemblyName = "CompareLibrary" }).Image);
			user = new ModuleDefUser("User.dll");
			new AssemblyDefUser("User").Modules.Add(user);
		}

		public void Dispose() => library.Dispose();

		// Creates the forms of a type of the library. assemblyName is the assembly that the references name.
		TypeForms CreateForms(string @namespace, string name, string? nestedName = null, string assemblyName = "CompareLibrary") {
			var assemblyRef = new AssemblyRefUser(assemblyName, library.Assembly!.Version);
			var forms = new TypeForms();
			forms.Def = library.Find(@namespace + "." + name + (nestedName is null ? "" : "+" + nestedName), isReflectionName: true)!;
			forms.Ref = new TypeRefUser(user, @namespace, name, assemblyRef);
			forms.Exported = new ExportedTypeUser(user, 0, @namespace, name, TypeAttributes.Public, assemblyRef);
			if (nestedName is not null) {
				forms.Ref = new TypeRefUser(user, string.Empty, nestedName, forms.Ref);
				forms.Exported = new ExportedTypeUser(user, 0, string.Empty, nestedName, TypeAttributes.NestedPublic, forms.Exported);
			}
			forms.Sig = new ClassSig(forms.Ref);
			forms.Spec = new TypeSpecUser(new ClassSig(forms.Ref));
			return forms;
		}

		// Calls every Equals() method that takes two of the forms
		static void AssertAllPairs(bool expected, TypeForms a, TypeForms b, SigComparerOptions options = 0) {
			var comparer = new SigComparer(options);
			// The same form
			Assert.Equal(expected, comparer.Equals(a.Def, b.Def));
			Assert.Equal(expected, comparer.Equals(a.Ref, b.Ref));
			Assert.Equal(expected, comparer.Equals(a.Exported, b.Exported));
			Assert.Equal(expected, comparer.Equals(a.Sig, b.Sig));
			Assert.Equal(expected, comparer.Equals(a.Spec, b.Spec));
			// A definition and another form
			Assert.Equal(expected, comparer.Equals(a.Def, b.Ref));
			Assert.Equal(expected, comparer.Equals(a.Ref, b.Def));
			Assert.Equal(expected, comparer.Equals(a.Def, b.Exported));
			Assert.Equal(expected, comparer.Equals(a.Exported, b.Def));
			Assert.Equal(expected, comparer.Equals(a.Def, b.Sig));
			Assert.Equal(expected, comparer.Equals(a.Sig, b.Def));
			Assert.Equal(expected, comparer.Equals(a.Def, b.Spec));
			Assert.Equal(expected, comparer.Equals(a.Spec, b.Def));
			// Two forms that aren't definitions
			Assert.Equal(expected, comparer.Equals(a.Ref, b.Exported));
			Assert.Equal(expected, comparer.Equals(a.Exported, b.Ref));
			Assert.Equal(expected, comparer.Equals(a.Ref, b.Sig));
			Assert.Equal(expected, comparer.Equals(a.Sig, b.Ref));
			Assert.Equal(expected, comparer.Equals(a.Ref, b.Spec));
			Assert.Equal(expected, comparer.Equals(a.Spec, b.Ref));
			Assert.Equal(expected, comparer.Equals(a.Exported, b.Sig));
			Assert.Equal(expected, comparer.Equals(a.Sig, b.Exported));
			Assert.Equal(expected, comparer.Equals(a.Exported, b.Spec));
			Assert.Equal(expected, comparer.Equals(a.Spec, b.Exported));
			Assert.Equal(expected, comparer.Equals(a.Sig, b.Spec));
			Assert.Equal(expected, comparer.Equals(a.Spec, b.Sig));
			// The methods that take any form
			Assert.Equal(expected, comparer.Equals((IType)a.Def, (IType)b.Exported));
			Assert.Equal(expected, comparer.Equals((IType)a.Sig, (IType)b.Ref));
			Assert.Equal(expected, comparer.Equals((ITypeDefOrRef)a.Spec, (ITypeDefOrRef)b.Def));
			Assert.Equal(expected, comparer.Equals((IMemberRef)a.Ref, (IMemberRef)b.Def));
		}

		static void AssertSameHashCodes(TypeForms forms) {
			var comparer = new SigComparer();
			int expected = comparer.GetHashCode(forms.Def);
			Assert.Equal(expected, comparer.GetHashCode(forms.Ref));
			Assert.Equal(expected, comparer.GetHashCode(forms.Exported));
			Assert.Equal(expected, comparer.GetHashCode(forms.Sig));
			Assert.Equal(expected, comparer.GetHashCode(forms.Spec));
			Assert.Equal(expected, comparer.GetHashCode((IType)forms.Ref));
			Assert.Equal(expected, comparer.GetHashCode((ITypeDefOrRef)forms.Spec));
			Assert.Equal(expected, comparer.GetHashCode((IMemberRef)forms.Def));
		}

		[Fact]
		public void AllFormsOfATypeAreEqual() {
			var forms = CreateForms("Ns", "Other");

			AssertAllPairs(true, forms, forms);
			AssertAllPairs(true, forms, CreateForms("Ns", "Other"));
			AssertSameHashCodes(forms);
		}

		[Fact]
		public void AllFormsOfANestedTypeAreEqual() {
			var forms = CreateForms("Ns", "Outer", "Inner");

			AssertAllPairs(true, forms, CreateForms("Ns", "Outer", "Inner"));
			AssertSameHashCodes(forms);
		}

		[Fact]
		public void FormsOfDifferentTypesAreNotEqual() {
			AssertAllPairs(false, CreateForms("Ns", "Other"), CreateForms("Ns", "Outer"));
			// A nested type and the type it's in
			AssertAllPairs(false, CreateForms("Ns", "Outer", "Inner"), CreateForms("Ns", "Outer"));
		}

		// The references name another assembly than the one the definition is in
		[Fact]
		public void TypeOfAnotherAssemblyIsNotEqualUnlessTheScopeIsIgnored() {
			var inLibrary = CreateForms("Ns", "Other");
			var inAnotherAssembly = CreateForms("Ns", "Other", assemblyName: "AnotherLibrary");
			var comparer = new SigComparer();

			Assert.False(comparer.Equals(inLibrary.Ref, inAnotherAssembly.Ref));
			Assert.False(comparer.Equals(inLibrary.Def, inAnotherAssembly.Ref));
			Assert.False(comparer.Equals(inAnotherAssembly.Exported, inLibrary.Def));
			Assert.False(comparer.Equals(inLibrary.Sig, inAnotherAssembly.Spec));
			AssertAllPairs(true, inLibrary, inAnotherAssembly, SigComparerOptions.DontCompareTypeScope);
		}

		[Fact]
		public void NamesAreComparedWithoutCaseIfTheOptionIsSet() {
			var forms = CreateForms("Ns", "Other");
			var upperCase = new TypeRefUser(user, "NS", "OTHER", new AssemblyRefUser("CompareLibrary", library.Assembly!.Version));

			Assert.False(new SigComparer().Equals(forms.Def, upperCase));
			Assert.False(new SigComparer().Equals(upperCase, forms.Ref));
			var ignoreCase = new SigComparer(SigComparerOptions.CaseInsensitiveTypeNamespaces | SigComparerOptions.CaseInsensitiveTypeNames);
			Assert.True(ignoreCase.Equals(forms.Def, upperCase));
			Assert.True(ignoreCase.Equals(upperCase, forms.Ref));
			Assert.Equal(ignoreCase.GetHashCode(forms.Def), ignoreCase.GetHashCode(upperCase));
		}

		[Fact]
		public void NullIsOnlyEqualToNull() {
			var forms = CreateForms("Ns", "Other");
			var comparer = new SigComparer();

			Assert.True(comparer.Equals((TypeDef?)null, (TypeDef?)null));
			Assert.True(comparer.Equals((TypeSig?)null, (TypeSig?)null));
			Assert.False(comparer.Equals(forms.Def, (TypeRef?)null));
			Assert.False(comparer.Equals((TypeRef?)null, forms.Def));
			Assert.False(comparer.Equals(forms.Sig, (TypeSpec?)null));
			Assert.False(comparer.Equals((ExportedType?)null, forms.Sig));
			// A null TypeDef is hashed like the global type (<Module>): in reflection, the declaring type of a
			// global method is null. A null signature has hash code 0.
			Assert.Equal(comparer.GetHashCode(library.GlobalType), comparer.GetHashCode((TypeDef?)null));
			Assert.Equal(0, comparer.GetHashCode((TypeSig?)null));
		}
	}
}
