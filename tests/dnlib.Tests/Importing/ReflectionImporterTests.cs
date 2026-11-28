// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.Tests.Writer;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Importing {
	/// <summary>
	/// <see cref="Importer"/> creates references (TypeRef, MemberRef, MethodSpec, signatures) from the types,
	/// methods and fields of System.Reflection. <c>Reflection/ReflectionComparisonTests</c> imports every
	/// member of the core library and compares it with its definition, these tests check single cases.
	/// </summary>
	public class ReflectionImporterTests {
		// A module whose core library is the core library of the runtime that runs the tests
		static ModuleDefUser CreateModule() {
			var coreLibrary = new AssemblyRefUser(typeof(object).Assembly.GetName());
			var module = new ModuleDefUser("Test.dll", null, coreLibrary);
			new AssemblyDefUser("Test").Modules.Add(module);
			return module;
		}

		static Importer CreateImporter() => new Importer(CreateModule());

		public class Generic<T> {
			public T Value = default!;
			public Generic<T>? Self;
			public volatile int Counter;
			public volatile Generic<T>? VolatileSelf;
			public T Echo(T value) => value;
			public TResult Convert<TResult>(T value) where TResult : new() => new TResult();
			public static Generic<T> Create() => new Generic<T>();
		}

		[Fact]
		public void TypeIsATypeRefToItsAssembly() {
			var imported = CreateImporter().Import(typeof(Uri));

			var typeRef = Assert.IsAssignableFrom<TypeRef>(imported);
			Assert.Equal("System.Uri", typeRef.FullName);
			Assert.Equal(typeof(Uri).Assembly.GetName().Name, typeRef.DefinitionAssembly!.Name.String);
			Assert.Equal(typeof(Uri).Assembly.GetName().Version, typeRef.DefinitionAssembly.Version);
		}

		[Fact]
		public void NestedTypeIsATypeRefInATypeRef() {
			var typeRef = Assert.IsAssignableFrom<TypeRef>(CreateImporter().Import(typeof(Environment.SpecialFolder)));

			Assert.Equal("SpecialFolder", typeRef.Name);
			Assert.Equal("System.Environment", Assert.IsAssignableFrom<TypeRef>(typeRef.ResolutionScope).FullName);
		}

		[Fact]
		public void CoreLibraryTypesAreTheModulesCorLibTypes() {
			var module = CreateModule();
			var importer = new Importer(module);

			Assert.Same(module.CorLibTypes.Int32, importer.ImportAsTypeSig(typeof(int)));
			Assert.Same(module.CorLibTypes.String, importer.ImportAsTypeSig(typeof(string)));
			Assert.Same(module.CorLibTypes.Object, importer.ImportAsTypeSig(typeof(object)));
		}

		[Fact]
		public void GenericInstanceIsATypeSpec() {
			var imported = CreateImporter().Import(typeof(Dictionary<string, int[]>));

			var typeSpec = Assert.IsAssignableFrom<TypeSpec>(imported);
			Assert.Equal("System.Collections.Generic.Dictionary`2<System.String,System.Int32[]>", typeSpec.FullName);
		}

		[Fact]
		public void GenericTypeDefinitionIsATypeRef() {
			var imported = CreateImporter().Import(typeof(Dictionary<,>));

			Assert.Equal("System.Collections.Generic.Dictionary`2", Assert.IsAssignableFrom<TypeRef>(imported).FullName);
		}

		// The signature of a method of a generic type uses the type's parameters (!0), also if the type is
		// instantiated: 'instance !0 Generic`1<int32>::Echo(!0)'
		[Fact]
		public void MethodOfAGenericType() {
			var imported = CreateImporter().Import(typeof(Generic<int>).GetMethod("Echo"));

			var memberRef = Assert.IsAssignableFrom<MemberRef>(imported);
			Assert.Equal("dnlib.Tests.Importing.ReflectionImporterTests/Generic`1<System.Int32>", Assert.IsAssignableFrom<TypeSpec>(memberRef.Class).FullName);
			var signature = memberRef.MethodSig!;
			Assert.True(signature.HasThis);
			Assert.Equal(ElementType.Var, signature.RetType!.ElementType);
			Assert.Equal(ElementType.Var, Assert.Single(signature.Params)!.ElementType);
		}

		// The method of the generic type definition (typeof(Generic<>)) is referenced through a TypeRef, not
		// through 'Generic`1<!0>'
		[Fact]
		public void MethodOfAGenericTypeDefinition() {
			var imported = CreateImporter().Import(typeof(Generic<>).GetMethod("Echo"));

			var memberRef = Assert.IsAssignableFrom<MemberRef>(imported);
			Assert.Equal("dnlib.Tests.Importing.ReflectionImporterTests/Generic`1", Assert.IsAssignableFrom<TypeRef>(memberRef.Class).FullName);
			Assert.Equal(ElementType.Var, memberRef.MethodSig!.RetType!.ElementType);
		}

		// Code inside a generic type uses the type with its own parameters: in Generic<T>, the operand of
		// "ldtoken Generic<T>" or of a call of another method of Generic<T> is the instance Generic<!0>.
		// ImportAsOperand() creates that for a generic type definition.
		[Fact]
		public void GenericTypeDefinitionAsAnOperandIsAnInstanceWithItsOwnParameters() {
			var importer = CreateImporter();

			var typeSpec = Assert.IsAssignableFrom<TypeSpec>(importer.ImportAsOperand(typeof(Generic<>)));
			var instance = Assert.IsType<GenericInstSig>(typeSpec.TypeSig);
			Assert.Equal("dnlib.Tests.Importing.ReflectionImporterTests/Generic`1", instance.GenericType!.FullName);
			var argument = Assert.IsType<GenericVar>(Assert.Single(instance.GenericArguments));
			Assert.Equal(0U, argument.Number);

			// Import() returns the definition
			Assert.IsAssignableFrom<TypeRef>(importer.Import(typeof(Generic<>)));
			// Other types are the same as an operand
			Assert.IsAssignableFrom<TypeRef>(importer.ImportAsOperand(typeof(Uri)));
			Assert.Equal("dnlib.Tests.Importing.ReflectionImporterTests/Generic`1<System.Int32>", importer.ImportAsOperand(typeof(Generic<int>)).FullName);
			Assert.Null(importer.ImportAsOperand((Type?)null));
			Assert.Null(importer.ImportAsOperandOrNull((Type?)null));
		}

		[Fact]
		public void MemberOfAGenericTypeDefinitionAsAnOperandIsAMemberOfTheInstance() {
			var importer = CreateImporter();
			var method = typeof(Generic<>).GetMethod("Echo");
			var field = typeof(KeyValuePairHolder<>).GetField("Value");

			var methodRef = Assert.IsAssignableFrom<MemberRef>(importer.ImportAsOperand(method));
			var methodOwner = Assert.IsAssignableFrom<TypeSpec>(methodRef.Class);
			Assert.Equal("dnlib.Tests.Importing.ReflectionImporterTests/Generic`1<!0>", methodOwner.FullName);
			Assert.Equal(ElementType.Var, methodRef.MethodSig!.RetType!.ElementType);

			var fieldRef = Assert.IsAssignableFrom<MemberRef>(importer.ImportAsOperand(field));
			Assert.Equal("dnlib.Tests.Importing.ReflectionImporterTests/KeyValuePairHolder`1<!0>", Assert.IsAssignableFrom<TypeSpec>(fieldRef.Class).FullName);
			Assert.Equal(ElementType.Var, fieldRef.FieldSig!.Type!.ElementType);
			// Without "as an operand", the member is in the definition
			Assert.IsAssignableFrom<TypeRef>(Assert.IsAssignableFrom<MemberRef>(importer.Import(field)).Class);

			Assert.Null(importer.ImportAsOperand((System.Reflection.MethodBase?)null));
			Assert.Null(importer.ImportAsOperand((System.Reflection.FieldInfo?)null));
			Assert.Null(importer.ImportAsOperandOrNull((System.Reflection.MethodBase?)null));
			Assert.Null(importer.ImportAsOperandOrNull((System.Reflection.FieldInfo?)null));
		}

		public class KeyValuePairHolder<T> {
			public T? Value;
		}

		[Fact]
		public void InstantiatedGenericMethodIsAMethodSpec() {
			var method = typeof(Generic<int>).GetMethod("Convert")!.MakeGenericMethod(typeof(List<string>));

			var methodSpec = Assert.IsAssignableFrom<MethodSpec>(CreateImporter().Import(method));

			Assert.Equal("System.Collections.Generic.List`1<System.String>", Assert.Single(methodSpec.GenericInstMethodSig!.GenericArguments)!.FullName);
			var memberRef = Assert.IsAssignableFrom<MemberRef>(methodSpec.Method);
			Assert.Equal("Convert", memberRef.Name);
			Assert.Equal(1U, memberRef.MethodSig!.GenParamCount);
			Assert.Equal(ElementType.MVar, memberRef.MethodSig.RetType!.ElementType);
			Assert.Equal(ElementType.Var, memberRef.MethodSig.Params[0]!.ElementType);
		}

		// Fixed in 2f7b7afd and 46e58086: the type of a member that's its own declaring type is 'Generic`1<!0>'
		// in the signature. Reflection returns the generic type definition for it, which was imported as
		// 'Generic`1' without generic arguments.
		[Fact]
		public void DeclaringTypeInASignatureOfItsOwnMemberIsAGenericInstance() {
			var importer = CreateImporter();

			var create = Assert.IsAssignableFrom<MemberRef>(importer.Import(typeof(Generic<>).GetMethod("Create")));
			var returnType = Assert.IsType<GenericInstSig>(create.MethodSig!.RetType);
			Assert.Equal(ElementType.Var, Assert.Single(returnType.GenericArguments)!.ElementType);

			var self = importer.Import(typeof(Generic<>).GetField("Self")!);
			var fieldType = Assert.IsType<GenericInstSig>(self.FieldSig!.Type);
			Assert.Equal(ElementType.Var, Assert.Single(fieldType.GenericArguments)!.ElementType);
		}

		[Fact]
		public void FieldOfAGenericType() {
			var imported = CreateImporter().Import(typeof(Generic<string>).GetField("Value")!);

			Assert.Equal("Value", imported.Name);
			Assert.Equal(ElementType.Var, imported.FieldSig!.Type!.ElementType);
			Assert.Equal("dnlib.Tests.Importing.ReflectionImporterTests/Generic`1<System.String>", imported.DeclaringType!.FullName);
		}

		[Fact]
		public void Constructor() {
			var imported = CreateImporter().Import(typeof(Uri).GetConstructor(new[] { typeof(string) }));

			var memberRef = Assert.IsAssignableFrom<MemberRef>(imported);
			Assert.Equal(".ctor", memberRef.Name);
			Assert.Equal("System.Void System.Uri::.ctor(System.String)", memberRef.FullName);
			Assert.True(memberRef.MethodSig!.HasThis);
		}

		// Fixed in 0cedd4d2: the runtime creates the methods of a multi-dimensional array, they have no
		// metadata token
		[Fact]
		public void MethodOfAMultiDimensionalArray() {
			var imported = CreateImporter().Import(typeof(int[,]).GetMethod("Get"));

			var memberRef = Assert.IsAssignableFrom<MemberRef>(imported);
			Assert.Equal("System.Int32 System.Int32[0...,0...]::Get(System.Int32,System.Int32)", memberRef.FullName);
		}

		[Fact]
		public void ByRefPointerAndArrayParameters() {
			var method = typeof(Methods).GetMethod(nameof(Methods.ManyKindsOfParameters))!;

			var imported = CreateImporter().Import(method);

			Assert.Equal(new[] { "System.Int32&", "System.Byte*", "System.String[]", "System.Int64[0...,0...]" }, imported.MethodSig!.Params.Select(a => a!.FullName));
			Assert.False(imported.MethodSig.HasThis);
		}

		public static unsafe class Methods {
			public static void ManyKindsOfParameters(ref int byRef, byte* pointer, string[] array, long[,] matrix) { }
		}

#if NET
		// Fixed in 0c77e621: FieldInfo.FieldType doesn't have the custom modifiers of the field's type.
		// FieldInfo.GetModifiedFieldType() (.NET 8) does, and the importer uses it.
		[Fact]
		public void VolatileFieldHasItsRequiredModifier() {
			var imported = CreateImporter().Import(typeof(Generic<int>).GetField("Counter")!);

			var modifier = Assert.IsType<CModReqdSig>(imported.FieldSig!.Type);
			Assert.Equal("System.Runtime.CompilerServices.IsVolatile", modifier.Modifier!.FullName);
			Assert.Equal(ElementType.I4, modifier.Next!.ElementType);
		}

		public unsafe class FunctionPointers {
			public delegate*<int, string, void> Managed;
			public delegate* unmanaged[Cdecl]<byte, long> Unmanaged;
			public List<delegate*<int, void>[]>? InGenericArgument;
			public delegate* unmanaged[Stdcall]<void> Stdcall;
			public delegate* unmanaged[Thiscall]<void> Thiscall;
			public delegate* unmanaged[Fastcall]<void> Fastcall;
			public delegate* unmanaged<void> PlatformDefault;
			public delegate* unmanaged[Stdcall, SuppressGCTransition]<void> StdcallWithoutGCTransition;
			public delegate* unmanaged[MemberFunction]<void> MemberFunction;
		}

		// The calling convention of an unmanaged function pointer: one of the four that the signature can have
		// (C, StdCall, ThisCall, FastCall), or 'unmanaged' with the CallConv* types as modifiers of the return type
		[Theory]
		[InlineData("Stdcall", CallingConvention.StdCall)]
		[InlineData("Thiscall", CallingConvention.ThisCall)]
		[InlineData("Fastcall", CallingConvention.FastCall)]
		[InlineData("PlatformDefault", CallingConvention.Unmanaged)]
		[InlineData("StdcallWithoutGCTransition", CallingConvention.Unmanaged)]
		[InlineData("MemberFunction", CallingConvention.Unmanaged)]
		public void CallingConventionsOfUnmanagedFunctionPointers(string field, CallingConvention expected) {
			var fieldInfo = typeof(FunctionPointers).GetField(field)!;
			var imported = CreateImporter().Import(fieldInfo);

			var signature = Assert.IsType<FnPtrSig>(imported.FieldSig!.Type).MethodSig!;
			Assert.Equal(expected, signature.CallingConvention);
			// The imported field is the field of the type
			Assert.True(new SigComparer().Equals(imported, fieldInfo));
		}

		// Fixed in e9d791e4: function pointer types (.NET 8) weren't supported
		[Fact]
		public void FunctionPointerType() {
			var imported = CreateImporter().Import(typeof(FunctionPointers).GetField("Managed")!);

			var signature = Assert.IsType<FnPtrSig>(imported.FieldSig!.Type).MethodSig!;
			Assert.Equal("System.Void", signature.RetType!.FullName);
			Assert.Equal(new[] { "System.Int32", "System.String" }, signature.Params.Select(a => a!.FullName));
			Assert.Equal(CallingConvention.Default, signature.CallingConvention);
		}

		// The calling convention is only in the modified type
		[Fact]
		public void UnmanagedFunctionPointerTypeHasItsCallingConvention() {
			var imported = CreateImporter().Import(typeof(FunctionPointers).GetField("Unmanaged")!);

			var signature = Assert.IsType<FnPtrSig>(imported.FieldSig!.Type).MethodSig!;
			Assert.Equal(CallingConvention.C, signature.CallingConvention);
			Assert.Equal("System.Int64", signature.RetType!.FullName);
		}

		// A field whose type is a function pointer is the reflection field only if the function pointer has the same
		// kind (managed or unmanaged), calling convention, return type and parameters
		[Fact]
		public void FunctionPointerFieldsAreComparedWithReflectionFields() {
			var module = CreateModule();
			var types = module.CorLibTypes;
			// The FunctionPointers class is nested in this class
			var outer = new TypeRefUser(module, typeof(ReflectionImporterTests).Namespace, nameof(ReflectionImporterTests), new AssemblyRefUser(typeof(ReflectionImporterTests).Assembly.GetName()));
			var owner = new TypeRefUser(module, "", nameof(FunctionPointers), outer);
			MemberRef Field(string name, MethodSig sig) => new MemberRefUser(module, name, new FieldSig(new FnPtrSig(sig)), owner);
			var comparer = new SigComparer(SigComparerOptions.CompareMethodFieldDeclaringType);
			var managed = typeof(FunctionPointers).GetField(nameof(FunctionPointers.Managed))!;
			var unmanaged = typeof(FunctionPointers).GetField(nameof(FunctionPointers.Unmanaged))!;
			var managedSig = MethodSig.CreateStatic(types.Void, types.Int32, types.String);
			var cdeclSig = new MethodSig(CallingConvention.C, 0, types.Int64, types.Byte);

			Assert.True(comparer.Equals(Field("Managed", managedSig), managed));
			Assert.True(comparer.Equals(Field("Unmanaged", cdeclSig), unmanaged));
			// Another kind or calling convention
			Assert.False(comparer.Equals(Field("Managed", new MethodSig(CallingConvention.C, 0, types.Void, types.Int32, types.String)), managed));
			Assert.False(comparer.Equals(Field("Unmanaged", new MethodSig(CallingConvention.StdCall, 0, types.Int64, types.Byte)), unmanaged));
			// Another return type, other parameters, and parameters after a sentinel (reflection doesn't have them)
			Assert.False(comparer.Equals(Field("Managed", MethodSig.CreateStatic(types.Int32, types.Int32, types.String)), managed));
			Assert.False(comparer.Equals(Field("Managed", MethodSig.CreateStatic(types.Void, types.Int32)), managed));
			var varArg = new MethodSig(CallingConvention.VarArg, 0, types.Void, types.Int32, types.String) { ParamsAfterSentinel = new List<TypeSig?> { types.Int32 } };
			Assert.False(comparer.Equals(Field("Managed", varArg), managed));
		}

		// Fixed in 9d66dba3
		[Fact]
		public void FunctionPointerInAGenericArgument() {
			var imported = CreateImporter().Import(typeof(FunctionPointers).GetField("InGenericArgument")!);

			var list = Assert.IsType<GenericInstSig>(imported.FieldSig!.Type);
			var array = Assert.IsType<SZArraySig>(Assert.Single(list.GenericArguments));
			var signature = Assert.IsType<FnPtrSig>(array.Next).MethodSig!;
			Assert.Equal(new[] { "System.Int32" }, signature.Params.Select(a => a!.FullName));
		}

		// Fixed in efdc8f31: same as DeclaringTypeInASignatureOfItsOwnMemberIsAGenericInstance, for a type with
		// a custom modifier
		[Fact]
		public void DeclaringTypeWithAModifierInASignatureOfItsOwnMemberIsAGenericInstance() {
			var imported = CreateImporter().Import(typeof(Generic<>).GetField("VolatileSelf")!);

			var modifier = Assert.IsType<CModReqdSig>(imported.FieldSig!.Type);
			var fieldType = Assert.IsType<GenericInstSig>(modifier.Next);
			Assert.Equal(ElementType.Var, Assert.Single(fieldType.GenericArguments)!.ElementType);
		}
#endif

		// A module with a method that's outside of all classes (C# can't create one)
		static byte[] CreateImageWithGlobalMethod() {
			var created = new CreatedModule();
			var globalMethod = new MethodDefUser("GlobalMethod", MethodSig.CreateStatic(created.Module.CorLibTypes.Int32), MethodImplAttributes.IL, MethodAttributes.Public | MethodAttributes.Static);
			globalMethod.Body = new CilBody();
			globalMethod.Body.Instructions.Add(OpCodes.Ldc_I4_5.ToInstruction());
			globalMethod.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
			created.Module.GlobalType!.Methods.Add(globalMethod);

			// static T GenericGlobalMethod<T>(T value) => value;
			var typeParameter = new GenericMVar(0);
			var genericMethod = new MethodDefUser("GenericGlobalMethod", MethodSig.CreateStaticGeneric(1, typeParameter, typeParameter), MethodImplAttributes.IL, MethodAttributes.Public | MethodAttributes.Static);
			genericMethod.GenericParameters.Add(new GenericParamUser(0, GenericParamAttributes.NonVariant, "T"));
			genericMethod.Body = new CilBody();
			genericMethod.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
			genericMethod.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
			created.Module.GlobalType.Methods.Add(genericMethod);
			return created.Write();
		}

		// In .NET and .NET Framework, a global method has no declaring type. In Mono it's the <Module> type.
		static void SkipOnMono() =>
			Assert.SkipWhen(Type.GetType("Mono.Runtime") is not null, "Mono's global methods have a declaring type");

		// Fixed in 03fc7b49: a global method is referenced through the name of its module (a ModuleRef), which
		// must be a module of the same assembly. The importer used the name of the module it imports into.
		[Fact]
		public void GlobalMethodIsReferencedThroughItsModule() {
			SkipOnMono();
			using (var loaded = new LoadedAssembly(CreateImageWithGlobalMethod())) {
				var reflectionMethod = loaded.Assembly.ManifestModule.GetMethod("GlobalMethod")!;
				Assert.Equal(5, reflectionMethod.Invoke(null, null));
				// Another module of the same assembly
				var otherModule = new ModuleDefUser("Other.netmodule");
				new AssemblyDefUser("Created").Modules.Add(otherModule);

				var imported = Assert.IsAssignableFrom<MemberRef>(new Importer(otherModule).Import(reflectionMethod));

				Assert.Equal("GlobalMethod", imported.Name);
				var moduleRef = Assert.IsAssignableFrom<ModuleRef>(imported.Class);
				Assert.Equal("Created.dll", moduleRef.Name);
			}
		}

		// Fixed in 60e384cc: an instantiated generic method has no declaring type either, which wasn't expected
		[Fact]
		public void InstantiatedGenericGlobalMethodIsAMethodSpecOfAReferenceThroughItsModule() {
			SkipOnMono();
			using (var loaded = new LoadedAssembly(CreateImageWithGlobalMethod())) {
				var reflectionMethod = loaded.Assembly.ManifestModule.GetMethod("GenericGlobalMethod")!.MakeGenericMethod(typeof(string));
				Assert.Equal("text", reflectionMethod.Invoke(null, new object[] { "text" }));
				var otherModule = new ModuleDefUser("Other.netmodule");
				new AssemblyDefUser("Created").Modules.Add(otherModule);

				var imported = Assert.IsAssignableFrom<MethodSpec>(new Importer(otherModule).Import(reflectionMethod));

				var genericMethod = Assert.IsAssignableFrom<MemberRef>(imported.Method);
				Assert.Equal("GenericGlobalMethod", genericMethod.Name);
				Assert.Equal("Created.dll", Assert.IsAssignableFrom<ModuleRef>(genericMethod.Class).Name);
				Assert.Equal(new[] { "System.String" }, imported.GenericInstMethodSig!.GenericArguments.Select(a => a!.FullName));

				Assert.Throws<ImportException>(() => CreateImporter().Import(reflectionMethod));
			}
		}

		// A global method of another assembly can't be referenced: there's no way to name its "class"
		[Fact]
		public void GlobalMethodOfAnotherAssemblyCanNotBeImported() {
			SkipOnMono();
			using (var loaded = new LoadedAssembly(CreateImageWithGlobalMethod())) {
				var reflectionMethod = loaded.Assembly.ManifestModule.GetMethod("GlobalMethod");

				Assert.Throws<ImportException>(() => CreateImporter().Import(reflectionMethod));
			}
		}
	}
}
