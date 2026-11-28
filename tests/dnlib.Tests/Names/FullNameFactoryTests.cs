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

		// Fixed in this commit: the three kinds of types of the runtime's own signatures had no TypeSig class.
		// System.Reflection shows the native view of a type as "VALUETYPE" and the type's name, and
		// System.__Canon like the class. It shows a generic parameter by its name, which dnlib doesn't know:
		// the signature only has the rid of the GenericParam row.
		[Fact]
		public void NamesOfTheTypesOfTheRuntimesOwnSignatures() {
			var native = new NativeValueTypeZapSig(CorLib.Int32);
			Assert.Equal("native System.Int32", native.FullName);
			Assert.Equal("native Int32", native.TypeName);
			Assert.Equal("System", native.Namespace);
			Assert.Equal("VALUETYPESystem.Int32", native.ReflectionFullName);
			Assert.Equal("VALUETYPEInt32", native.ReflectionName);
			Assert.Equal("native System.Int32*[]", new SZArraySig(new PtrSig(native)).FullName);
			Assert.Equal("VALUETYPESystem.Int32*[]", new SZArraySig(new PtrSig(native)).ReflectionFullName);
			Assert.Equal("VALUETYPEVALUETYPESystem.Int32", new NativeValueTypeZapSig(native).ReflectionFullName);

			var canon = new CanonZapSig(module);
			// Changed in this commit: it has the module, and its assembly and scope are the core library like
			// System.Type.Assembly of System.__Canon (they were null)
			Assert.Same(module, canon.OwnerModule);
			Assert.Same(module, canon.Module);
			Assert.Same(CorLib.AssemblyRef, canon.DefinitionAssembly);
			Assert.Same(CorLib.AssemblyRef, canon.Scope);
			Assert.Equal("System.__Canon", canon.ScopeType!.FullName);
			Assert.Same(CorLib.AssemblyRef, Assert.IsType<TypeRefUser>(canon.ScopeType).ResolutionScope);
			Assert.Equal("System.__Canon, " + CorLib.AssemblyRef.FullName, canon.AssemblyQualifiedName);
			Assert.Same(CorLib.AssemblyRef, new SZArraySig(canon).DefinitionAssembly);
			Assert.Same(CorLib.AssemblyRef, new TypeSpecUser(new SZArraySig(canon)).DefinitionAssembly);
			Assert.Equal("System.__Canon", canon.FullName);
			Assert.Equal("__Canon", canon.TypeName);
			Assert.Equal("System", canon.Namespace);
			Assert.Equal("System.__Canon", canon.ReflectionFullName);
			Assert.Equal("__Canon", canon.ReflectionName);
			Assert.Equal("System", canon.ReflectionNamespace);
			Assert.Equal("System.__Canon[]", new SZArraySig(canon).FullName);

			// The module has no such row (and it wasn't read from a file), so the generic parameter isn't known
			var genericParam = new VarZapSig(3, new ModuleDefUser("Test.dll"));
			Assert.Equal("GenericParam(3)", genericParam.FullName);
			Assert.Equal("GenericParam(3)", genericParam.TypeName);
			Assert.Equal("", genericParam.Namespace);
			// Like a normal generic parameter (System.Type.FullName is null)
			Assert.Equal("", genericParam.ReflectionFullName);
			Assert.Equal("GenericParam(3)", genericParam.ReflectionName);
			Assert.Equal("GenericParam(3)&", new ByRefSig(genericParam).FullName);
		}

		// Two kinds of types that only the runtime itself uses (dnlib reads them): their names have the number
		[Fact]
		public void NamesOfValueArraysAndModuleTypes() {
			Assert.Equal("System.Int32 ValueArray(4)", new ValueArraySig(CorLib.Int32, 4).FullName);
			Assert.Equal("Int32 ValueArray(4)", new ValueArraySig(CorLib.Int32, 4).TypeName);
			Assert.Equal("System", new ValueArraySig(CorLib.Int32, 4).Namespace);
			Assert.Equal("System.Int32 Module(2)", new ModuleSig(2, CorLib.Int32).FullName);
			Assert.Equal("System.Int32 Module(2)[]", new SZArraySig(new ModuleSig(2, CorLib.Int32)).FullName);
		}

		// An exported type is in another assembly (a type forwarder) or in another file of this assembly. Its
		// scope is a reference to that assembly or to that file's module, also if it's a nested type.
		[Fact]
		public void ScopeOfAnExportedType() {
			var libraryRef = new AssemblyRefUser(library.Assembly!);
			var forwarded = new ExportedTypeUser(module, 0, "Ns", "Forwarded", TypeAttributes.Public, libraryRef);
			var inFile = new ExportedTypeUser(module, 1, "Ns", "InFile", TypeAttributes.Public, new FileDefUser("Other.netmodule", FileAttributes.ContainsMetadata, new byte[0]));
			var nestedInFile = new ExportedTypeUser(module, 2, "", "Nested", TypeAttributes.NestedPublic, inFile);

			Assert.Same(libraryRef, FullNameFactory.Scope(forwarded));
			Assert.Same(libraryRef, forwarded.Scope);
			foreach (var exportedType in new[] { inFile, nestedInFile }) {
				var moduleRef = Assert.IsAssignableFrom<ModuleRef>(FullNameFactory.Scope(exportedType));
				Assert.Equal("Other.netmodule", moduleRef.Name);
				Assert.Same(module, moduleRef.Module);
			}
			Assert.Null(FullNameFactory.Scope((ExportedType?)null));
			Assert.Null(FullNameFactory.Scope(new ExportedTypeUser(module, 0, "Ns", "NoImplementation", TypeAttributes.Public, null)));
		}

		// A row of the ExportedType table (a type that is in another file or assembly) has names like a type
		[Fact]
		public void ExportedTypeHasTheNamesOfItsType() {
			var module = new ModuleDefUser("Test.dll");
			var assembly = new AssemblyDefUser("Test", new Version(1, 2, 3, 4));
			assembly.Modules.Add(module);
			var other = new AssemblyRefUser("Other", new Version(5, 6, 7, 8));
			var outer = new ExportedTypeUser(module, 0, "Ns.Inner", "Outer`1", TypeAttributes.Public | TypeAttributes.Forwarder, other);
			var nested = new ExportedTypeUser(module, 0, "", "Nested", TypeAttributes.NestedPublic, outer);
			module.ExportedTypes.Add(outer);
			module.ExportedTypes.Add(nested);

			foreach (IType type in new IType[] { outer, nested }) {
				bool isNested = type == nested;
				Assert.Equal(isNested ? "Ns.Inner.Outer`1/Nested" : "Ns.Inner.Outer`1", FullNameFactory.FullName(type, isReflection: false, helper: null, sb: null));
				Assert.Equal(isNested ? "Ns.Inner.Outer`1+Nested" : "Ns.Inner.Outer`1", FullNameFactory.FullName(type, isReflection: true, helper: null, sb: null));
				Assert.Equal(isNested ? "Nested" : "Outer`1", FullNameFactory.Name(type, isReflection: false, sb: null));
				Assert.Equal(isNested ? "Nested" : "Outer`1", FullNameFactory.Name(type, isReflection: true, sb: null));
				Assert.Equal(isNested ? "" : "Ns.Inner", FullNameFactory.Namespace(type, isReflection: false, sb: null));
				Assert.Equal("Ns.Inner", FullNameFactory.Namespace(type, isReflection: true, sb: null));
				Assert.Equal((isNested ? "Ns.Inner.Outer`1+Nested" : "Ns.Inner.Outer`1") + ", Other, Version=5.6.7.8, Culture=neutral, PublicKeyToken=null", FullNameFactory.AssemblyQualifiedName(type));
				// The properties of the type give the same names
				Assert.Equal(type.FullName, FullNameFactory.FullName(type, isReflection: false, helper: null, sb: null));
				Assert.Equal(type.ReflectionFullName, FullNameFactory.FullName(type, isReflection: true, helper: null, sb: null));
				Assert.Equal(type.AssemblyQualifiedName, FullNameFactory.AssemblyQualifiedName(type));
				Assert.Same(other, FullNameFactory.DefinitionAssembly(type as ExportedType));
			}
			// Something that is no type has no name
			Assert.Equal("", FullNameFactory.FullName((IType?)null, isReflection: false, helper: null, sb: null));
			Assert.Equal("", FullNameFactory.Name((IType?)null, isReflection: true, sb: null));
			Assert.Equal("", FullNameFactory.Namespace((IType?)null, isReflection: true, sb: null));
		}

		const string Null = "<<<NULL>>>";

		// The methods for a specific kind of type write "<<<NULL>>>" for null, and so do the methods that take
		// an ITypeDefOrRef for a missing part, eg. a TypeSpec without a signature
		[Fact]
		public void NullIsWrittenAsNull() {
			Assert.Equal(Null, FullNameFactory.FullName((TypeRef?)null, false));
			Assert.Equal(Null, FullNameFactory.Name((TypeRef?)null, false));
			Assert.Equal(Null, FullNameFactory.Namespace((TypeRef?)null, false));
			Assert.Equal(Null, FullNameFactory.AssemblyQualifiedName((TypeRef?)null));
			Assert.Equal(Null, FullNameFactory.FullName((TypeDef?)null, false));
			Assert.Equal(Null, FullNameFactory.Name((TypeDef?)null, false));
			Assert.Equal(Null, FullNameFactory.Namespace((TypeDef?)null, false));
			Assert.Equal(Null, FullNameFactory.AssemblyQualifiedName((TypeDef?)null));
			Assert.Equal(Null, FullNameFactory.FullName((TypeSpec?)null, false));
			Assert.Equal(Null, FullNameFactory.Name((TypeSpec?)null, false));
			Assert.Equal(Null, FullNameFactory.Namespace((TypeSpec?)null, false));
			Assert.Equal(Null, FullNameFactory.AssemblyQualifiedName((TypeSpec?)null));
			Assert.Equal(Null, FullNameFactory.FullName((TypeSig?)null, false));
			Assert.Equal(Null, FullNameFactory.Name((TypeSig?)null, false));
			Assert.Equal(Null, FullNameFactory.Namespace((TypeSig?)null, false));
			Assert.Equal(Null, FullNameFactory.AssemblyQualifiedName((TypeSig?)null));
			Assert.Equal(Null, FullNameFactory.FullName((ExportedType?)null, false));
			Assert.Equal(Null, FullNameFactory.Name((ExportedType?)null, false));
			Assert.Equal(Null, FullNameFactory.Namespace((ExportedType?)null, false));
			Assert.Equal(Null, FullNameFactory.AssemblyQualifiedName((ExportedType?)null));
		}

		// The type of a signature can be any ITypeDefOrRef: a TypeSpec (only an invalid file has one) has the
		// names of its signature, and null is written as null
		[Fact]
		public void TypeSpecOrNullInATypeSignature() {
			var typeSpec = new TypeSpecUser(new SZArraySig(CorLib.Int32));
			var withTypeSpec = new ClassSig(typeSpec);
			var withNull = new ClassSig(null);

			Assert.Equal("System.Int32[]", FullNameFactory.FullName(withTypeSpec, false));
			Assert.Equal("Int32[]", FullNameFactory.Name(withTypeSpec, false));
			Assert.Equal("System", FullNameFactory.Namespace(withTypeSpec, false));
			Assert.StartsWith("System.Int32[], ", FullNameFactory.AssemblyQualifiedName(withTypeSpec));
			Assert.Equal(Null, FullNameFactory.FullName(withNull, false));
			Assert.Equal(Null, FullNameFactory.Name(withNull, false));
			Assert.Equal(Null, FullNameFactory.Namespace(withNull, false));
			// The type and its assembly are both missing
			Assert.Equal(Null + ", " + Null, FullNameFactory.AssemblyQualifiedName(withNull));
			// A modifier that is a TypeSpec
			var modified = new CModReqdSig(typeSpec, CorLib.Int32);
			Assert.Equal("System.Int32 modreq(System.Int32[])", FullNameFactory.FullName(modified, false));
		}

		// A TypeSpec without a signature
		[Fact]
		public void TypeSpecWithoutASignature() {
			var typeSpec = new TypeSpecUser((TypeSig?)null);

			Assert.Equal(Null, FullNameFactory.FullName(typeSpec, false));
			Assert.Equal(Null, FullNameFactory.Name(typeSpec, false));
			Assert.Equal(Null, FullNameFactory.Namespace(typeSpec, false));
			Assert.Equal(Null, FullNameFactory.AssemblyQualifiedName(typeSpec));
			Assert.Null(FullNameFactory.ScopeType(typeSpec));
			Assert.Null(FullNameFactory.Scope(typeSpec));
			Assert.Null(FullNameFactory.DefinitionAssembly(typeSpec));
		}

		// The module and the assembly that a reference is in
		[Fact]
		public void OwnerModuleAndDefinitionAssembly() {
			var typeSpec = new TypeSpecUser(new ClassSig(nestedRef));
			var exportedType = new ExportedTypeUser(module, 0, "Ns", "Exported", TypeAttributes.Public, new AssemblyRefUser(library.Assembly!));

			Assert.Same(module, FullNameFactory.OwnerModule(nestedRef));
			Assert.Same(library, FullNameFactory.OwnerModule(nested));
			Assert.Same(module, FullNameFactory.OwnerModule(typeSpec));
			Assert.Same(module, FullNameFactory.OwnerModule(new ClassSig(nestedRef)));
			Assert.Same(module, FullNameFactory.OwnerModule(exportedType));
			Assert.Null(FullNameFactory.OwnerModule((TypeRef?)null));
			Assert.Null(FullNameFactory.OwnerModule((ExportedType?)null));
			Assert.Null(FullNameFactory.OwnerModule((TypeSig?)null));
			Assert.Equal("Library", FullNameFactory.DefinitionAssembly(typeSpec)!.Name);
			Assert.Equal("Library", FullNameFactory.DefinitionAssembly(new ClassSig(typeSpec))!.Name);
			Assert.Same(nestedRef, FullNameFactory.ScopeType(new ClassSig(typeSpec)));
			Assert.Equal(LibraryName, FullNameFactory.Scope(new ClassSig(typeSpec))!.ScopeName);
		}

		// An event's type can have generic arguments of the declaring type
		[Fact]
		public void EventNameWithGenericArguments() {
			var handler = new TypeSpecUser(new GenericVar(0));

			Assert.Equal("System.Int32 Ns.Type::Changed", FullNameFactory.EventFullName("Ns.Type", "Changed", handler, new TypeSig[] { CorLib.Int32 }));
			Assert.Equal("!0 Ns.Type::Changed", FullNameFactory.EventFullName("Ns.Type", "Changed", handler));
		}

		// A type signature with the generic arguments of a type and of a method
		[Fact]
		public void TypeSignatureWithGenericArguments() {
			var sig = new GenericInstSig(new ClassSig(nestedRef), new GenericVar(0), new GenericMVar(0));

			Assert.Equal("Ns.Outer`1/Nested<System.Int32,System.String>", FullNameFactory.FullName(sig, false, null, new TypeSig[] { CorLib.Int32 }, new TypeSig[] { CorLib.String }));
			Assert.Equal("Ns.Outer`1/Nested<System.Int32,!!0>", FullNameFactory.FullName(sig, false, null, new TypeSig[] { CorLib.Int32 }, null));
			Assert.Equal("Ns.Outer`1/Nested<!0,System.String>", FullNameFactory.FullName(sig, false, null, null, new TypeSig[] { CorLib.String }));
		}

		// A ']' in an assembly name is escaped in an assembly qualified name inside a generic argument
		[Fact]
		public void ClosingBracketInAnAssemblyNameIsEscaped() {
			var other = new TypeRefUser(module, "Ns", "Other", new AssemblyRefUser("Odd]Name", new Version(1, 0, 0, 0)));
			var sig = new GenericInstSig(new ClassSig(nestedRef), new ClassSig(other));

			var name = FullNameFactory.AssemblyQualifiedName(sig);

			Assert.Contains("[Ns.Other, Odd\\]Name, Version=1.0.0.0", name);
		}
	}
}
