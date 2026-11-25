// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Importing {
	/// <summary>
	/// An <see cref="ImportMapper"/> replaces types and members while they're imported, eg. to import code into
	/// a module that has its own copy of a class
	/// </summary>
	public sealed class ImportMapperTests : IDisposable {
		const string LibrarySource = @"
public class Original {
	public int Field;
	public void Method() { }
	public T Generic<T>(T value) { return value; }
}";

		readonly ModuleDefMD library = ModuleDefMD.Load(CSharpCompiler.Compile(LibrarySource, new CSharpCompilerOptions { AssemblyName = "MapperLibrary" }).Image);
		readonly ModuleDefUser target;
		readonly TypeDefUser replacement;
		readonly FieldDefUser replacementField;
		readonly MethodDefUser replacementMethod;

		public ImportMapperTests() {
			target = new ModuleDefUser("Target.dll");
			new AssemblyDefUser("Target").Modules.Add(target);
			replacement = new TypeDefUser("Ns", "Replacement", target.CorLibTypes.Object.TypeDefOrRef);
			target.Types.Add(replacement);
			replacementField = new FieldDefUser("ReplacementField", new FieldSig(target.CorLibTypes.Int32), FieldAttributes.Public);
			replacement.Fields.Add(replacementField);
			replacementMethod = new MethodDefUser("ReplacementMethod", MethodSig.CreateInstance(target.CorLibTypes.Void), MethodAttributes.Public);
			replacement.Methods.Add(replacementMethod);
		}

		public void Dispose() => library.Dispose();

		TypeDef Original => library.Find("Original", isReflectionName: true)!;

		// Replaces the Original class and its members with the Replacement class and its members
		sealed class ReplacingMapper : ImportMapper {
			readonly ImportMapperTests test;
			public ReplacingMapper(ImportMapperTests test) => this.test = test;

			public override ITypeDefOrRef? Map(ITypeDefOrRef source) => source.FullName == "Original" ? test.replacement : null;
			public override IField? Map(FieldDef source) => source.Name == "Field" ? test.replacementField : null;
			public override IMethod? Map(MethodDef source) => source.Name == "Method" ? test.replacementMethod : null;
			public override ITypeDefOrRef? Map(Type source) => source == typeof(Uri) ? test.replacement : null;
		}

		Importer CreateImporter(ImportMapper mapper) => new Importer(target, 0, new GenericParamContext(), mapper);

		[Fact]
		public void WithoutAMapperTheOriginalsAreReferenced() {
			var importer = new Importer(target);

			Assert.Equal("Original", Assert.IsAssignableFrom<TypeRef>(importer.Import(Original)).FullName);
			Assert.Equal("Field", importer.Import(Original.FindField("Field")!).Name);
			Assert.Equal("Method", importer.Import(Original.FindMethod("Method")!).Name);
		}

		[Fact]
		public void MappedTypeAndMembersAreReturned() {
			var importer = CreateImporter(new ReplacingMapper(this));

			Assert.Same(replacement, importer.Import(Original));
			Assert.Same(replacementField, importer.Import(Original.FindField("Field")!));
			Assert.Same(replacementMethod, importer.Import(Original.FindMethod("Method")!));
			// The method isn't mapped, but its class is
			var generic = Assert.IsAssignableFrom<MemberRef>(importer.Import(Original.FindMethod("Generic")!));
			Assert.Same(replacement, generic.Class);
		}

		[Fact]
		public void MappedTypeIsUsedInsideSignatures() {
			var importer = CreateImporter(new ReplacingMapper(this));
			var signature = MethodSig.CreateStatic(new SZArraySig(Original.ToTypeSig()), library.CorLibTypes.Int32);

			var imported = importer.Import(signature);

			var arrayType = Assert.IsType<SZArraySig>(imported.RetType);
			Assert.Same(replacement, arrayType.Next!.ToTypeDefOrRef());
			Assert.Same(target.CorLibTypes.Int32, imported.Params[0]);
		}

		[Fact]
		public void ReflectionTypeIsMapped() {
			var importer = CreateImporter(new ReplacingMapper(this));

			Assert.Same(replacement, importer.Import(typeof(Uri)));
			Assert.Equal("System.Version", importer.Import(typeof(Version)).FullName);
		}

		// References are mapped too, not only definitions
		[Fact]
		public void MappedTypeRefIsReturned() {
			var importer = CreateImporter(new ReplacingMapper(this));
			var other = new ModuleDefUser("Other.dll");
			var originalRef = new TypeRefUser(other, "", "Original", new AssemblyRefUser("MapperLibrary"));
			var anotherRef = new TypeRefUser(other, "", "Another", new AssemblyRefUser("MapperLibrary"));

			Assert.Same(replacement, importer.Import(originalRef));
			Assert.Same(replacement, importer.Import(new ClassSig(originalRef)).ToTypeDefOrRef());
			var imported = Assert.IsAssignableFrom<TypeRef>(importer.Import(anotherRef));
			Assert.Equal("Another", imported.Name);
			Assert.Same(target, imported.Module);
		}

		// Replaces a reference to Original::Method with a reference to another method
		sealed class MemberRefMapper : ImportMapper {
			public MemberRef? Replacement;
			public override MemberRef? Map(MemberRef source) => source.Name == "Method" ? Replacement : null;
		}

		[Fact]
		public void MappedMemberRefIsReturned() {
			var other = new ModuleDefUser("Other.dll");
			var originalRef = new TypeRefUser(other, "", "Original", new AssemblyRefUser("MapperLibrary"));
			var signature = MethodSig.CreateInstance(other.CorLibTypes.Void);
			var methodRef = new MemberRefUser(other, "Method", signature, originalRef);
			var unmappedRef = new MemberRefUser(other, "Unmapped", signature, originalRef);
			var mapper = new MemberRefMapper { Replacement = new MemberRefUser(target, "ReplacementMethod", MethodSig.CreateInstance(target.CorLibTypes.Void), replacement) };
			var importer = CreateImporter(mapper);

			Assert.Same(mapper.Replacement, importer.Import(methodRef));
			Assert.Same(mapper.Replacement, importer.Import((IMethod)methodRef));
			var imported = importer.Import(unmappedRef);
			Assert.Equal("Unmapped", imported.Name);
			Assert.Same(target, imported.Module);
		}

		// What Map(Type) returns for typeof(Uri)
		sealed class TypeMapper : ImportMapper {
			public ITypeDefOrRef? Replacement;
			public override ITypeDefOrRef? Map(Type source) => source == typeof(Uri) ? Replacement : null;
		}

		[Fact]
		public void ReflectionTypeCanBeMappedToATypeRef() {
			var replacementRef = new TypeRefUser(target, "Ns", "Elsewhere", new AssemblyRefUser("Elsewhere"));
			var importer = CreateImporter(new TypeMapper { Replacement = replacementRef });

			Assert.Same(replacementRef, importer.Import(typeof(Uri)));
			// Also where the type is a part of another type
			Assert.Same(replacementRef, importer.ImportAsTypeSig(typeof(Uri[])).Next!.ToTypeDefOrRef());
		}

		// A Type is a class or a struct, and that can't be a TypeSpec: it's a mistake in the mapper
		[Fact]
		public void ReflectionTypeCanNotBeMappedToATypeSpec() {
			var typeSpec = new TypeSpecUser(new SZArraySig(target.CorLibTypes.Int32));
			var importer = CreateImporter(new TypeMapper { Replacement = typeSpec });

			Assert.Throws<InvalidOperationException>(() => importer.Import(typeof(Uri)));
		}

		// Maps the generic method to an instantiation of a method, which isn't a method that can be instantiated
		sealed class MethodSpecMapper : ImportMapper {
			readonly MethodSpec methodSpec;
			public MethodSpecMapper(MethodSpec methodSpec) => this.methodSpec = methodSpec;
			public override IMethod? Map(MethodDef source) => source.Name == "Generic" ? methodSpec : null;
		}

		// Fixed in c510832d: the importer cast what the mapper returned and threw InvalidCastException
		[Fact]
		public void MethodSpecWhoseMethodIsMappedToAMethodSpecIsNotImported() {
			var genericMethod = Original.FindMethod("Generic")!;
			var instantiation = new MethodSpecUser(genericMethod, new GenericInstMethodSig(library.CorLibTypes.Int32));
			var mappedTo = new MethodSpecUser(replacementMethod, new GenericInstMethodSig(target.CorLibTypes.Int32));
			var importer = CreateImporter(new MethodSpecMapper(mappedTo));

			Assert.Null(importer.ImportOrNull(instantiation));
			Assert.Throws<ImportException>(() => importer.Import(instantiation));
		}
	}
}
