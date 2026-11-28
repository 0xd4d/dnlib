// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Resolving {
	/// <summary>
	/// The extension methods that get the definition of whatever names a type, a field or a method:
	/// <c>ResolveTypeDef()</c>, <c>ResolveFieldDef()</c>, <c>ResolveMethodDef()</c>, <c>GetBaseType()</c> and
	/// their variants that throw if nothing is found. The input can be the definition itself, a reference in
	/// another module, or an instantiation of a generic type or method.
	///
	/// The module <c>App</c> references the assembly <c>Library</c>, which the module's resolver knows, and
	/// the assembly <c>Missing</c>, which it doesn't know.
	/// </summary>
	public class ResolveExtensionTests {
		readonly ModuleDefUser library = CreateModule("Library");
		readonly ModuleDefUser app = CreateModule("App");
		readonly TypeDefUser baseType;
		readonly TypeDefUser genericType;
		readonly FieldDefUser field;
		readonly MethodDefUser method;
		readonly MethodDefUser genericMethod;
		readonly TypeRefUser baseTypeRef;
		readonly TypeRefUser missingTypeRef;

		ICorLibTypes CorLib => app.CorLibTypes;

		static ModuleDefUser CreateModule(string assemblyName) {
			var module = new ModuleDefUser(assemblyName + ".dll");
			new AssemblyDefUser(assemblyName, new Version(1, 0, 0, 0)).Modules.Add(module);
			return module;
		}

		public ResolveExtensionTests() {
			// class Base { static int Field; static void Method(); static void GenericMethod<T>(); }
			baseType = new TypeDefUser("Ns", "Base", library.CorLibTypes.Object.TypeDefOrRef);
			library.Types.Add(baseType);
			field = new FieldDefUser("Field", new FieldSig(library.CorLibTypes.Int32), FieldAttributes.Public | FieldAttributes.Static);
			baseType.Fields.Add(field);
			method = new MethodDefUser("Method", MethodSig.CreateStatic(library.CorLibTypes.Void), MethodAttributes.Public | MethodAttributes.Static);
			baseType.Methods.Add(method);
			genericMethod = new MethodDefUser("GenericMethod", MethodSig.CreateStaticGeneric(1, library.CorLibTypes.Void), MethodAttributes.Public | MethodAttributes.Static);
			genericMethod.GenericParameters.Add(new GenericParamUser(0, GenericParamAttributes.NonVariant, "T"));
			baseType.Methods.Add(genericMethod);
			// class Generic<T> : Base { }
			genericType = new TypeDefUser("Ns", "Generic`1", baseType);
			genericType.GenericParameters.Add(new GenericParamUser(0, GenericParamAttributes.NonVariant, "T"));
			library.Types.Add(genericType);

			var resolver = new AssemblyResolver();
			app.Context = new ModuleContext(resolver, new Resolver(resolver));
			resolver.AddToCache(library);

			baseTypeRef = new TypeRefUser(app, "Ns", "Base", new AssemblyRefUser(library.Assembly!));
			missingTypeRef = new TypeRefUser(app, "Ns", "Type", new AssemblyRefUser("Missing", new Version(1, 0, 0, 0)));
		}

		TypeSpecUser CreateInstanceOfGenericType() =>
			new TypeSpecUser(new GenericInstSig(new ClassSig(new TypeRefUser(app, "Ns", "Generic`1", new AssemblyRefUser(library.Assembly!))), CorLib.Int32));

		[Fact]
		public void ResolveTypeDef() {
			Assert.Same(baseType, baseType.ResolveTypeDef());
			Assert.Same(baseType, baseTypeRef.ResolveTypeDef());
			Assert.Same(genericType, CreateInstanceOfGenericType().ResolveTypeDef());
			// A TypeSpec of a definition
			Assert.Same(baseType, new TypeSpecUser(new ClassSig(baseType)).ResolveTypeDef());

			Assert.Null(missingTypeRef.ResolveTypeDef());
			Assert.Null(((ITypeDefOrRef?)null).ResolveTypeDef());
			// An array resolves to its element type: that's the type that has a scope
			Assert.Same(baseType, new TypeSpecUser(new SZArraySig(new ClassSig(baseTypeRef))).ResolveTypeDef());
			Assert.Null(new TypeSpecUser(new SZArraySig(new ClassSig(missingTypeRef))).ResolveTypeDef());
		}

		[Fact]
		public void ResolveTypeDefOrThrow() {
			Assert.Same(baseType, baseType.ResolveTypeDefOrThrow());
			Assert.Same(baseType, baseTypeRef.ResolveTypeDefOrThrow());
			Assert.Same(genericType, CreateInstanceOfGenericType().ResolveTypeDefOrThrow());

			Assert.Throws<TypeResolveException>(() => missingTypeRef.ResolveTypeDefOrThrow());
			Assert.Throws<TypeResolveException>(() => ((ITypeDefOrRef?)null).ResolveTypeDefOrThrow());
			Assert.Throws<TypeResolveException>(() => new TypeSpecUser(new SZArraySig(new ClassSig(missingTypeRef))).ResolveTypeDefOrThrow());
		}

		[Fact]
		public void GetBaseType() {
			var derived = new TypeDefUser("Ns", "Derived", baseTypeRef);

			// Of a definition: what it has
			Assert.Same(baseTypeRef, derived.GetBaseType());
			// Of a reference or an instantiation: the base type of its definition
			Assert.Same(baseType.BaseType, baseTypeRef.GetBaseType());
			Assert.Same(baseType, CreateInstanceOfGenericType().GetBaseType());
			Assert.Same(baseType, new TypeSpecUser(new ClassSig(genericType)).GetBaseType());

			Assert.Null(missingTypeRef.GetBaseType());
			Assert.Null(((ITypeDefOrRef?)null).GetBaseType());
			Assert.Null(new TypeSpecUser(new SZArraySig(CorLib.Int32)).GetBaseType());
		}

		[Fact]
		public void GetBaseTypeOrThrow() {
			Assert.Same(baseType.BaseType, baseTypeRef.GetBaseTypeOrThrow());
			Assert.Same(baseType, CreateInstanceOfGenericType().GetBaseTypeOrThrow());

			Assert.Throws<TypeResolveException>(() => missingTypeRef.GetBaseTypeOrThrow());
			Assert.Throws<TypeResolveException>(() => new TypeSpecUser(new GenericInstSig(new ClassSig(missingTypeRef), CorLib.Int32)).GetBaseTypeOrThrow());
			// Nothing to resolve
			Assert.Null(((ITypeDefOrRef?)null).GetBaseTypeOrThrow());
		}

		[Fact]
		public void ResolveFieldDef() {
			var reference = new MemberRefUser(app, "Field", new FieldSig(CorLib.Int32), baseTypeRef);
			var otherName = new MemberRefUser(app, "OtherField", new FieldSig(CorLib.Int32), baseTypeRef);
			var inMissingType = new MemberRefUser(app, "Field", new FieldSig(CorLib.Int32), missingTypeRef);

			Assert.Same(field, field.ResolveFieldDef());
			Assert.Same(field, reference.ResolveFieldDef());
			Assert.Same(field, reference.ResolveFieldDefOrThrow());
			Assert.Same(field, field.ResolveFieldDefOrThrow());

			Assert.Null(otherName.ResolveFieldDef());
			Assert.Null(inMissingType.ResolveFieldDef());
			Assert.Null(((IField?)null).ResolveFieldDef());
			Assert.Throws<MemberRefResolveException>(() => otherName.ResolveFieldDefOrThrow());
			Assert.Throws<MemberRefResolveException>(() => inMissingType.ResolveFieldDefOrThrow());
			Assert.Throws<MemberRefResolveException>(() => ((IField?)null).ResolveFieldDefOrThrow());
		}

		[Fact]
		public void ResolveMethodDef() {
			var reference = new MemberRefUser(app, "Method", MethodSig.CreateStatic(CorLib.Void), baseTypeRef);
			var otherSignature = new MemberRefUser(app, "Method", MethodSig.CreateStatic(CorLib.Int32), baseTypeRef);

			Assert.Same(method, method.ResolveMethodDef());
			Assert.Same(method, reference.ResolveMethodDef());
			Assert.Same(method, method.ResolveMethodDefOrThrow());
			Assert.Same(method, reference.ResolveMethodDefOrThrow());

			Assert.Null(otherSignature.ResolveMethodDef());
			Assert.Null(((IMethod?)null).ResolveMethodDef());
			Assert.Throws<MemberRefResolveException>(() => otherSignature.ResolveMethodDefOrThrow());
			Assert.Throws<MemberRefResolveException>(() => ((IMethod?)null).ResolveMethodDefOrThrow());
		}

		// An instantiated generic method resolves to the generic method
		[Fact]
		public void ResolveMethodDefOfAMethodSpec() {
			var instantiation = new GenericInstMethodSig(CorLib.Int32);
			var reference = new MemberRefUser(app, "GenericMethod", MethodSig.CreateStaticGeneric(1, CorLib.Void), baseTypeRef);
			var missing = new MemberRefUser(app, "GenericMethod", MethodSig.CreateStaticGeneric(1, CorLib.Void), missingTypeRef);

			Assert.Same(genericMethod, new MethodSpecUser(genericMethod, instantiation).ResolveMethodDef());
			Assert.Same(genericMethod, new MethodSpecUser(reference, instantiation).ResolveMethodDef());
			Assert.Same(genericMethod, new MethodSpecUser(genericMethod, instantiation).ResolveMethodDefOrThrow());
			Assert.Same(genericMethod, new MethodSpecUser(reference, instantiation).ResolveMethodDefOrThrow());

			Assert.Null(new MethodSpecUser(missing, instantiation).ResolveMethodDef());
			Assert.Throws<MemberRefResolveException>(() => new MethodSpecUser(missing, instantiation).ResolveMethodDefOrThrow());
		}

		// The old names of the OrThrow methods still work
		[Fact]
		public void TheObsoleteThrowMethodsAreTheOrThrowMethods() {
			var fieldRef = new MemberRefUser(app, "Field", new FieldSig(CorLib.Int32), baseTypeRef);
			var methodRef = new MemberRefUser(app, "Method", MethodSig.CreateStatic(CorLib.Void), baseTypeRef);
			var exportedType = new ExportedTypeUser(app, 0, "Ns", "Base", TypeAttributes.Public, new AssemblyRefUser(library.Assembly!));
			var derivedRef = new TypeRefUser(app, "Ns", "Generic`1", new AssemblyRefUser(library.Assembly!));
#pragma warning disable CS0618 // Obsolete
			Assert.Same(baseType, baseTypeRef.ResolveTypeDefThrow());
			Assert.Equal("Ns.Base", derivedRef.GetBaseTypeThrow()!.FullName);
			Assert.Same(field, ((IField)fieldRef).ResolveFieldDefThrow());
			Assert.Same(method, ((IMethod)methodRef).ResolveMethodDefThrow());
			Assert.Same(field, fieldRef.ResolveThrow());
			Assert.Same(field, fieldRef.ResolveFieldThrow());
			Assert.Same(method, methodRef.ResolveMethodThrow());
			Assert.Same(baseType, exportedType.ResolveThrow());
			Assert.ThrowsAny<ResolveException>(() => missingTypeRef.ResolveTypeDefThrow());
#pragma warning restore CS0618
		}
	}
}
