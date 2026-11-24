// dnlib: See LICENSE.txt for more info

using System;
using System.Text;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Names {
	/// <summary>
	/// <see cref="FullNameFactory"/> creates the names that <c>FullName</c>, <c>ReflectionFullName</c>,
	/// <c>AssemblyQualifiedName</c> etc return. It's public, so its methods are also called directly here:
	/// with every kind of type reference, with generic arguments to put in a member's signature, and with a
	/// <see cref="StringBuilder"/> to write the name to.
	/// </summary>
	public class FullNameFactoryTests {
		readonly ModuleDefUser module = new ModuleDefUser("Test.dll");
		readonly ModuleDefUser library = new ModuleDefUser("Library.dll");
		readonly TypeDefUser nested;
		readonly TypeRefUser nestedRef;

		ICorLibTypes CorLib => module.CorLibTypes;

		public FullNameFactoryTests() {
			new AssemblyDefUser("Test", new Version(1, 2, 3, 4)).Modules.Add(module);
			new AssemblyDefUser("Library", new Version(5, 6, 7, 8)).Modules.Add(library);

			// class Ns.Outer`1 { class Nested { } } in Library.dll, and a reference to it in Test.dll
			var outer = new TypeDefUser("Ns", "Outer`1", library.CorLibTypes.Object.TypeDefOrRef);
			outer.GenericParameters.Add(new GenericParamUser(0, GenericParamAttributes.NonVariant, "T"));
			library.Types.Add(outer);
			nested = new TypeDefUser("", "Nested", library.CorLibTypes.Object.TypeDefOrRef) { Attributes = TypeAttributes.NestedPublic };
			outer.NestedTypes.Add(nested);
			var outerRef = new TypeRefUser(module, "Ns", "Outer`1", new AssemblyRefUser(library.Assembly!));
			nestedRef = new TypeRefUser(module, "", "Nested", outerRef);
		}

		const string LibraryName = "Library, Version=5.6.7.8, Culture=neutral, PublicKeyToken=null";

		[Fact]
		public void NamesOfATypeDefAndATypeRefAreTheSame() {
			foreach (var type in new IType[] { nested, nestedRef }) {
				Assert.Equal("Ns.Outer`1/Nested", FullNameFactory.FullName(type, isReflection: false, helper: null, sb: null));
				Assert.Equal("Ns.Outer`1+Nested", FullNameFactory.FullName(type, isReflection: true, helper: null, sb: null));
				Assert.Equal("Nested", FullNameFactory.Name(type, isReflection: false, sb: null));
				Assert.Equal("Nested", FullNameFactory.Name(type, isReflection: true, sb: null));
				Assert.Equal("", FullNameFactory.Namespace(type, isReflection: false, sb: null));
				Assert.Equal("Ns", FullNameFactory.Namespace(type, isReflection: true, sb: null));
				Assert.Equal("Ns.Outer`1+Nested, " + LibraryName, FullNameFactory.AssemblyQualifiedName(type));
			}
		}

		[Fact]
		public void NamesOfAnExportedType() {
			var outerExported = new ExportedTypeUser(module, 0, "Ns", "Outer`1", TypeAttributes.Public, new AssemblyRefUser(library.Assembly!));
			var nestedExported = new ExportedTypeUser(module, 0, "", "Nested", TypeAttributes.NestedPublic, outerExported);

			Assert.Equal("Ns.Outer`1/Nested", FullNameFactory.FullName(nestedExported, isReflection: false, helper: null, sb: null));
			Assert.Equal("Ns.Outer`1+Nested", FullNameFactory.FullName(nestedExported, isReflection: true, helper: null, sb: null));
			Assert.Equal("Nested", FullNameFactory.Name(nestedExported, isReflection: true, sb: null));
			Assert.Equal("Ns", FullNameFactory.Namespace(nestedExported, isReflection: true, sb: null));
			Assert.Equal("Ns.Outer`1+Nested, " + LibraryName, FullNameFactory.AssemblyQualifiedName(nestedExported));
		}

		// Ns.Outer<int>.Nested[] as a signature and as a TypeSpec
		[Fact]
		public void NamesOfASignatureAndATypeSpec() {
			var instance = new GenericInstSig(new ClassSig(nestedRef), CorLib.Int32);
			var array = new SZArraySig(instance);

			foreach (var type in new IType[] { array, new TypeSpecUser(array) }) {
				Assert.Equal("Ns.Outer`1/Nested<System.Int32>[]", FullNameFactory.FullName(type, isReflection: false, helper: null, sb: null));
				Assert.StartsWith("Ns.Outer`1+Nested[[System.Int32, ", FullNameFactory.FullName(type, isReflection: true, helper: null, sb: null));
				Assert.Equal("Nested[]", FullNameFactory.Name(type, isReflection: true, sb: null));
				Assert.Equal("Ns", FullNameFactory.Namespace(type, isReflection: true, sb: null));
				Assert.EndsWith("]][], " + LibraryName, FullNameFactory.AssemblyQualifiedName(type));
			}
		}

		[Fact]
		public void NameOfNullIsEmpty() {
			Assert.Equal("", FullNameFactory.FullName((IType?)null, isReflection: false, helper: null, sb: null));
			Assert.Equal("", FullNameFactory.Name((IType?)null, isReflection: false, sb: null));
			Assert.Equal("", FullNameFactory.Namespace((IType?)null, isReflection: false, sb: null));
			Assert.Equal("", FullNameFactory.AssemblyQualifiedName((IType?)null));
		}

		// The name is added to the StringBuilder that's passed in, which is also returned
		[Fact]
		public void StringBuilderIsUsedForTheName() {
			var builder = new StringBuilder("old text ");

			Assert.Same(builder, FullNameFactory.FullNameSB(nestedRef, isReflection: false, helper: null, sb: builder));
			Assert.Same(builder, FullNameFactory.NameSB(nestedRef, isReflection: false, sb: builder.Append(' ')));
			Assert.Same(builder, FullNameFactory.NamespaceSB(nestedRef, isReflection: true, sb: builder.Append(' ')));
			Assert.Same(builder, FullNameFactory.AssemblyQualifiedNameSB(nestedRef, helper: null, sb: builder.Append(' ')));

			Assert.Equal("old text Ns.Outer`1/Nested Nested Ns Ns.Outer`1+Nested, " + LibraryName, builder.ToString());
		}

		// In a type name inside a custom attribute, the assembly name can be left out if the type is in the
		// module itself or in the core library
		[Fact]
		public void AssemblyNameIsNotNeededForTypesOfTheModuleAndOfTheCoreLibrary() {
			var ownType = new TypeDefUser("Ns", "Own", CorLib.Object.TypeDefOrRef);
			module.Types.Add(ownType);
			var coreLibraryType = CorLib.GetTypeRef("System", "Type");

			Assert.False(FullNameFactory.MustUseAssemblyName(module, ownType));
			Assert.True(FullNameFactory.MustUseAssemblyName(module, nested));
			Assert.True(FullNameFactory.MustUseAssemblyName(module, nestedRef));
			Assert.False(FullNameFactory.MustUseAssemblyName(module, coreLibraryType));
			Assert.True(FullNameFactory.MustUseAssemblyName(module, coreLibraryType, allowCorlib: false));
			Assert.False(FullNameFactory.MustUseAssemblyName(module, new TypeRefUser(module, "Ns", "Own", AssemblyRef.CurrentAssembly), allowCorlib: false));
			// Not a TypeDef or a TypeRef
			Assert.True(FullNameFactory.MustUseAssemblyName(module, CorLib.Int32));
		}

		// A type of the module with the name of a core library type hides it, so the assembly name is needed
		[Fact]
		public void AssemblyNameIsNeededForACoreLibraryTypeIfTheModuleHasATypeWithItsName() {
			module.Types.Add(new TypeDefUser("System", "Type", CorLib.Object.TypeDefOrRef));

			Assert.True(FullNameFactory.MustUseAssemblyName(module, CorLib.GetTypeRef("System", "Type")));
		}

		// !0 and !!0 in the signature are replaced by the generic arguments
		[Fact]
		public void MemberNamesWithGenericArguments() {
			var typeArguments = new TypeSig?[] { CorLib.Int32 };
			var methodArguments = new TypeSig?[] { CorLib.String };
			var methodSig = MethodSig.CreateStaticGeneric(1, new GenericVar(0), new GenericMVar(0), new SZArraySig(new GenericVar(0)));

			Assert.Equal("System.Int32 Owner::Method<System.String>(System.String,System.Int32[])",
				FullNameFactory.MethodFullName("Owner", "Method", methodSig, typeArguments, methodArguments));
			Assert.Equal("!0 Owner::Method<!!0>(!!0,!0[])", FullNameFactory.MethodFullName("Owner", "Method", methodSig));
			Assert.Equal("System.Int32 Owner::Field", FullNameFactory.FieldFullName("Owner", "Field", new FieldSig(new GenericVar(0)), typeArguments));
			Assert.Equal("!0 Owner::Field", FullNameFactory.FieldFullName("Owner", "Field", new FieldSig(new GenericVar(0))));
			Assert.Equal("System.Int32 Owner::Property()", FullNameFactory.PropertyFullName("Owner", "Property", PropertySig.CreateStatic(new GenericVar(0)), typeArguments));
			Assert.Equal("System.Int32 Owner::Item(System.String)", FullNameFactory.PropertyFullName("Owner", "Item", PropertySig.CreateInstance(CorLib.Int32, CorLib.String)));
			Assert.Equal("Ns.Outer`1/Nested Owner::Event", FullNameFactory.EventFullName("Owner", "Event", nestedRef));
		}

		[Fact]
		public void MemberNamesAreWrittenToTheStringBuilder() {
			var builder = new StringBuilder("old text ");
			var methodSig = MethodSig.CreateStatic(CorLib.Void, CorLib.Int32);

			Assert.Same(builder, FullNameFactory.MethodFullNameSB("Owner", "Method", methodSig, null, null, null, builder));
			Assert.Same(builder, FullNameFactory.FieldFullNameSB("Owner", "Field", new FieldSig(CorLib.Int32), null, builder.Append('|')));
			Assert.Same(builder, FullNameFactory.PropertyFullNameSB("Owner", "Property", PropertySig.CreateStatic(CorLib.Int32), null, builder.Append('|')));
			Assert.Same(builder, FullNameFactory.EventFullNameSB("Owner", "Event", nestedRef, null, builder.Append('|')));
			Assert.Same(builder, FullNameFactory.MethodBaseSigFullNameSB(methodSig, builder.Append('|')));

			Assert.Equal("old text System.Void Owner::Method(System.Int32)|System.Int32 Owner::Field|System.Int32 Owner::Property()|" +
				"Ns.Outer`1/Nested Owner::Event|System.Void (System.Int32)", builder.ToString());
		}

		[Fact]
		public void SignatureNames() {
			var methodSig = MethodSig.CreateInstance(CorLib.Void, CorLib.Int32, CorLib.String);
			Assert.Equal("System.Void (System.Int32,System.String)", FullNameFactory.MethodBaseSigFullName(methodSig));
			Assert.Equal("System.Void Owner::Method(System.Int32,System.String)", FullNameFactory.MethodBaseSigFullName("Owner", "Method", methodSig, null));
			Assert.Equal("<<<NULL>>>", FullNameFactory.MethodBaseSigFullName(null));

			// The parameters after the sentinel of a call to a method with a variable number of arguments
			var varArgSig = new MethodSig(CallingConvention.VarArg, 0, CorLib.Void, CorLib.Int32);
			varArgSig.ParamsAfterSentinel = new System.Collections.Generic.List<TypeSig?> { CorLib.String };
			Assert.Equal("System.Void (System.Int32,...,System.String)", FullNameFactory.MethodBaseSigFullName(varArgSig));
		}
	}
}
