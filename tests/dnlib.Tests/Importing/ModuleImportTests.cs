// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Importing {
	/// <summary>
	/// <c>ModuleDef.Import()</c> is the short form of <c>new Importer(module).Import()</c>: it returns a
	/// reference that the module can use, to a type or member of System.Reflection or of another module.
	/// </summary>
	public sealed class ModuleImportTests : IDisposable {
		const string LibrarySource = @"
public class Library<T> {
	public int Field;
	public T GenericField;
	public void Method() { }
	public TMethod GenericMethod<TMethod>(TMethod value) => value;
}
public class Plain {
	public static int Field;
	public static void Method() { }
}
";
		readonly ModuleDefMD library = ModuleDefMD.Load(CSharpCompiler.Compile(LibrarySource, new CSharpCompilerOptions { AssemblyName = "Library" }).Image);
		readonly ModuleDefUser module = CreateModule();

		static ModuleDefUser CreateModule() {
			var module = new ModuleDefUser("Target.dll", null, new AssemblyRefUser(typeof(object).Assembly.GetName()));
			new AssemblyDefUser("Target").Modules.Add(module);
			return module;
		}

		public void Dispose() => library.Dispose();

		TypeDef Plain => library.Find("Plain", isReflectionName: true)!;
		TypeDef Generic => library.Find("Library`1", isReflectionName: true)!;

		[Fact]
		public void ReflectionTypesAndMembers() {
			var type = Assert.IsAssignableFrom<TypeRef>(module.Import(typeof(Uri)));
			Assert.Equal("System.Uri", type.FullName);
			Assert.Same(module, type.Module);
			Assert.Equal(typeof(Uri).Assembly.GetName().Name, type.DefinitionAssembly!.Name);
			Assert.Equal("System.Collections.Generic.List`1<System.Int32>", Assert.IsAssignableFrom<TypeSpec>(module.Import(typeof(List<int>))).FullName);

			var field = module.Import(typeof(Uri).GetField("SchemeDelimiter"))!;
			Assert.True(field.IsFieldRef);
			Assert.Equal("System.String System.Uri::SchemeDelimiter", field.FullName);
			Assert.Same(module, field.Module);

			var method = Assert.IsAssignableFrom<MemberRef>(module.Import(typeof(Uri).GetMethod("get_Host")));
			Assert.Equal("System.String System.Uri::get_Host()", method.FullName);
			var constructor = module.Import(typeof(Uri).GetConstructor(new[] { typeof(string) }))!;
			Assert.Equal("System.Void System.Uri::.ctor(System.String)", constructor.FullName);
			var genericMethod = typeof(Array).GetMethods().Single(a => a.Name == "Empty").MakeGenericMethod(typeof(int));
			Assert.Equal("System.Int32", Assert.IsAssignableFrom<MethodSpec>(module.Import(genericMethod)).GenericInstMethodSig!.GenericArguments.Single()!.FullName);

			Assert.Null(module.Import((Type?)null));
			Assert.Null(module.Import((System.Reflection.FieldInfo?)null));
			Assert.Null(module.Import((System.Reflection.MethodBase?)null));
		}

		[Fact]
		public void TypesOfAnotherModule() {
			var typeRef = module.Import(Plain);
			Assert.Equal("Plain", typeRef.FullName);
			Assert.Same(module, typeRef.Module);
			Assert.Equal("Library", typeRef.DefinitionAssembly!.Name);
			Assert.Same(Plain, new Resolver(new SingleAssemblyResolver(library.Assembly!)).Resolve(typeRef, module));

			// A reference of the other module becomes a reference of this module
			var referenceOfTheLibrary = Plain.BaseType as TypeRef;
			Assert.NotNull(referenceOfTheLibrary);
			var importedReference = module.Import(referenceOfTheLibrary);
			Assert.NotSame(referenceOfTheLibrary, importedReference);
			Assert.Equal("System.Object", importedReference.FullName);
			Assert.Same(module, importedReference.Module);

			var instance = new GenericInstSig(new ClassSig(Generic), library.CorLibTypes.Int32);
			var importedSig = Assert.IsType<GenericInstSig>(module.Import((TypeSig)instance));
			Assert.Equal("Library`1<System.Int32>", importedSig.FullName);
			Assert.Same(module, importedSig.GenericType!.TypeDefOrRef!.Module);
			var importedSpec = module.Import(new TypeSpecUser(instance));
			Assert.Equal("Library`1<System.Int32>", importedSpec.FullName);
			Assert.Equal("Plain", module.Import((IType)Plain).FullName);
			Assert.IsAssignableFrom<TypeRef>(module.Import((IType)Plain));

			Assert.Null(module.Import((TypeDef?)null));
			Assert.Null(module.Import((TypeRef?)null));
			Assert.Null(module.Import((TypeSpec?)null));
			Assert.Null(module.Import((TypeSig?)null));
			Assert.Null(module.Import((IType?)null));
		}

		[Fact]
		public void MembersOfAnotherModule() {
			var fieldDef = Plain.FindField("Field")!;
			var methodDef = Plain.FindMethod("Method")!;

			var fieldRef = module.Import(fieldDef);
			Assert.Equal("System.Int32 Plain::Field", fieldRef.FullName);
			Assert.Same(module, fieldRef.Module);
			Assert.Equal(fieldRef.FullName, module.Import((IField)fieldDef).FullName);

			var methodRef = module.Import(methodDef);
			Assert.Equal("System.Void Plain::Method()", methodRef.FullName);
			Assert.Same(module, methodRef.Module);
			Assert.Equal(methodRef.FullName, module.Import((IMethod)methodDef).FullName);

			// A reference to a member of a generic instance, and a generic method with its type arguments
			var owner = new TypeSpecUser(new GenericInstSig(new ClassSig(Generic), library.CorLibTypes.String));
			var genericMethodDef = Generic.FindMethod("GenericMethod")!;
			var referenceOfTheLibrary = new MemberRefUser(library, "GenericMethod", genericMethodDef.MethodSig, owner);
			var importedReference = module.Import(referenceOfTheLibrary);
			Assert.NotSame(referenceOfTheLibrary, importedReference);
			// The same member (an imported generic parameter doesn't know its owner, so the texts aren't the same)
			Assert.True(new SigComparer(SigComparerOptions.CompareMethodFieldDeclaringType).Equals(referenceOfTheLibrary, importedReference));
			Assert.Equal("!!0 Library`1<System.String>::GenericMethod<!!0>(!!0)", importedReference.FullName);
			Assert.Same(module, importedReference.Module);
			var methodSpec = new MethodSpecUser(referenceOfTheLibrary, new GenericInstMethodSig(library.CorLibTypes.Int32));
			var importedSpec = module.Import(methodSpec);
			Assert.NotSame(methodSpec, importedSpec);
			Assert.True(new SigComparer(SigComparerOptions.CompareMethodFieldDeclaringType).Equals(methodSpec, importedSpec));
			Assert.Equal("System.Int32", importedSpec.GenericInstMethodSig!.GenericArguments.Single()!.FullName);
			Assert.Same(module, ((MemberRef)importedSpec.Method!).Module);

			Assert.Null(module.Import((FieldDef?)null));
			Assert.Null(module.Import((MethodDef?)null));
			Assert.Null(module.Import((IField?)null));
			Assert.Null(module.Import((IMethod?)null));
			Assert.Null(module.Import((MemberRef?)null));
			Assert.Null(module.Import((MethodSpec?)null));
		}

		// Resolves references to one assembly
		sealed class SingleAssemblyResolver : IAssemblyResolver {
			readonly AssemblyDef assembly;
			public SingleAssemblyResolver(AssemblyDef assembly) => this.assembly = assembly;
			public AssemblyDef? Resolve(IAssembly? assembly, ModuleDef? sourceModule) => assembly?.Name == this.assembly.Name ? this.assembly : null;
		}
	}
}
