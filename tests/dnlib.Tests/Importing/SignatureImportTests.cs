// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Importing {
	/// <summary>
	/// Importing signatures from one module into another: every type definition of the source module inside a
	/// signature becomes a reference to it, whatever the signature is made of
	/// </summary>
	public sealed class SignatureImportTests : IDisposable {
		const string LibrarySource = @"
public class Thing { }
public struct Value { }
public class Box<T> { }
";

		readonly ModuleDefMD library = ModuleDefMD.Load(CSharpCompiler.Compile(LibrarySource, new CSharpCompilerOptions { AssemblyName = "SignatureLibrary" }).Image);
		readonly ModuleDefUser target;
		readonly Importer importer;

		public SignatureImportTests() {
			target = new ModuleDefUser("Target.dll");
			new AssemblyDefUser("Target").Modules.Add(target);
			importer = new Importer(target);
		}

		public void Dispose() => library.Dispose();

		TypeSig Thing => library.Find("Thing", isReflectionName: true)!.ToTypeSig()!;
		TypeSig Value => library.Find("Value", isReflectionName: true)!.ToTypeSig()!;
		ClassOrValueTypeSig Box => (ClassOrValueTypeSig)library.Find("Box`1", isReflectionName: true)!.ToTypeSig()!;
		ICorLibTypes SourceTypes => library.CorLibTypes;

		// All type definitions and references a signature is made of
		static void CollectTypes(TypeSig? signature, List<ITypeDefOrRef> types) {
			for (; signature is not null; signature = signature.Next) {
				switch (signature) {
				case TypeDefOrRefSig typeDefOrRefSig:
					types.Add(typeDefOrRefSig.TypeDefOrRef!);
					break;
				case GenericInstSig genericInstSig:
					CollectTypes(genericInstSig.GenericType, types);
					foreach (var argument in genericInstSig.GenericArguments)
						CollectTypes(argument, types);
					break;
				case FnPtrSig fnPtrSig:
					CollectTypes(fnPtrSig.MethodSig, types);
					break;
				case ModifierSig modifierSig:
					types.Add(modifierSig.Modifier!);
					break;
				}
			}
		}

		static void CollectTypes(MethodBaseSig? signature, List<ITypeDefOrRef> types) {
			if (signature is null)
				return;
			CollectTypes(signature.RetType, types);
			foreach (var parameter in signature.Params)
				CollectTypes(parameter, types);
		}

		void AssertOnlyReferences(List<ITypeDefOrRef> types) {
			Assert.NotEmpty(types);
			foreach (var type in types) {
				var typeRef = Assert.IsAssignableFrom<TypeRef>(type);
				Assert.Same(target, typeRef.Module);
				// The library's types are in the library, the others in the target's core library
				if (typeRef.Namespace == "")
					Assert.Equal("SignatureLibrary", typeRef.DefinitionAssembly!.Name);
				else
					Assert.Equal(target.CorLibTypes.AssemblyRef.Name, typeRef.DefinitionAssembly!.Name);
			}
		}

		void AssertImported(TypeSig original) {
			var imported = importer.Import(original);

			Assert.NotSame(original, imported);
			Assert.Equal(original.FullName, imported.FullName);
			Assert.Equal(original.GetType(), imported.GetType());
			Assert.True(new SigComparer().Equals(original, imported));
			var types = new List<ITypeDefOrRef>();
			CollectTypes(imported, types);
			AssertOnlyReferences(types);
		}

		[Fact]
		public void ClassAndValueType() {
			AssertImported(Thing);
			AssertImported(Value);
			Assert.IsType<ClassSig>(importer.Import(Thing));
			Assert.IsType<ValueTypeSig>(importer.Import(Value));
		}

		[Fact]
		public void ArraysPointersAndReferences() {
			AssertImported(new SZArraySig(Thing));
			AssertImported(new SZArraySig(new SZArraySig(Value)));
			AssertImported(new PtrSig(Value));
			AssertImported(new ByRefSig(Thing));
			AssertImported(new PinnedSig(new ByRefSig(Value)));
		}

		[Fact]
		public void MultiDimensionalArrayKeepsItsSizesAndLowerBounds() {
			var original = new ArraySig(Thing, 3U, new uint[] { 4, 5 }, new[] { -1, 0, 2 });
			AssertImported(original);

			var imported = Assert.IsType<ArraySig>(importer.Import(original));
			Assert.Equal(3U, imported.Rank);
			Assert.Equal(new uint[] { 4, 5 }, imported.Sizes);
			Assert.Equal(new[] { -1, 0, 2 }, imported.LowerBounds);
			Assert.NotSame(original.Sizes, imported.Sizes);
		}

		[Fact]
		public void GenericInstance() {
			AssertImported(new GenericInstSig(Box, Thing));
			AssertImported(new GenericInstSig(Box, new GenericInstSig(Box, new SZArraySig(Value))));
		}

		[Fact]
		public void GenericParameters() {
			var typeParameter = importer.Import(new GenericVar(2));
			var methodParameter = importer.Import(new GenericMVar(1));

			Assert.Equal(2U, Assert.IsType<GenericVar>(typeParameter).Number);
			Assert.Equal(1U, Assert.IsType<GenericMVar>(methodParameter).Number);
			AssertImported(new GenericInstSig(Box, new GenericVar(0)));
		}

		[Fact]
		public void FunctionPointer() {
			var signature = MethodSig.CreateStatic(Thing, Value, new SZArraySig(Thing));
			signature.CallingConvention = CallingConvention.StdCall;
			var original = new FnPtrSig(signature);
			AssertImported(original);

			var imported = Assert.IsType<FnPtrSig>(importer.Import(original)).MethodSig!;
			Assert.NotSame(signature, imported);
			Assert.Equal(CallingConvention.StdCall, imported.CallingConvention);
			Assert.Equal(2, imported.Params.Count);
		}

		[Fact]
		public void CustomModifiers() {
			var thingRef = library.Find("Thing", isReflectionName: true)!;
			AssertImported(new CModReqdSig(thingRef, Value));
			AssertImported(new CModOptSig(thingRef, new PtrSig(Thing)));
		}

		// The core library types of the source module become the core library types of the target module
		[Fact]
		public void CoreLibraryTypes() {
			Assert.Same(target.CorLibTypes.Int32, importer.Import(SourceTypes.Int32));
			Assert.Same(target.CorLibTypes.String, importer.Import(SourceTypes.String));
			Assert.Same(target.CorLibTypes.Object, importer.Import(SourceTypes.Object));
			Assert.Same(target.CorLibTypes.IntPtr, importer.Import(SourceTypes.IntPtr));
			var array = Assert.IsType<SZArraySig>(importer.Import(new SZArraySig(SourceTypes.Byte)));
			Assert.Same(target.CorLibTypes.Byte, array.Next);
		}

		[Fact]
		public void MethodSignature() {
			var original = MethodSig.CreateInstanceGeneric(2, Thing, Value, new GenericMVar(1), SourceTypes.Int32);
			original.ParamsAfterSentinel = new List<TypeSig?> { Thing };

			var imported = importer.Import(original);

			Assert.NotSame(original, imported);
			Assert.Equal(original.CallingConvention, imported.CallingConvention);
			Assert.Equal(2U, imported.GenParamCount);
			Assert.Equal(original.Params.Select(a => a!.FullName), imported.Params.Select(a => a!.FullName));
			Assert.Same(target.CorLibTypes.Int32, imported.Params[2]);
			Assert.Equal("Thing", imported.ParamsAfterSentinel!.Single()!.FullName);
			var types = new List<ITypeDefOrRef>();
			CollectTypes(imported, types);
			CollectTypes(imported.ParamsAfterSentinel!.Single(), types);
			AssertOnlyReferences(types);
		}

		[Fact]
		public void OtherSignatures() {
			var field = importer.Import(new FieldSig(new SZArraySig(Thing)));
			Assert.Equal("Thing[]", field.Type!.FullName);

			var property = importer.Import(PropertySig.CreateInstance(Value, SourceTypes.Int32));
			Assert.True(property.HasThis);
			Assert.Equal("Value", property.RetType!.FullName);
			Assert.Same(target.CorLibTypes.Int32, property.Params[0]);

			var locals = importer.Import(new LocalSig(Thing, new PinnedSig(new ByRefSig(Value))));
			Assert.Equal(new[] { "Thing", "Value&" }, locals.Locals.Select(a => a!.FullName));

			var instantiation = importer.Import(new GenericInstMethodSig(Thing, SourceTypes.String));
			Assert.Equal(new[] { "Thing", "System.String" }, instantiation.GenericArguments.Select(a => a!.FullName));
			Assert.Same(target.CorLibTypes.String, instantiation.GenericArguments[1]);

			// The method that takes any signature returns the same kind of signature
			Assert.IsType<FieldSig>(importer.Import((CallingConventionSig)new FieldSig(Thing)));
			Assert.IsType<MethodSig>(importer.Import((CallingConventionSig)MethodSig.CreateStatic(Thing)));
			Assert.IsType<LocalSig>(importer.Import((CallingConventionSig)new LocalSig(Thing)));
		}

		// A signature that's already made of the target module's types is copied
		[Fact]
		public void SignatureOfTheTargetModuleIsCopied() {
			var targetType = new TypeDefUser("Ns", "Own", target.CorLibTypes.Object.TypeDefOrRef);
			target.Types.Add(targetType);
			var original = new SZArraySig(targetType.ToTypeSig());

			var imported = Assert.IsType<SZArraySig>(new Importer(target, ImporterOptions.TryToUseTypeDefs).Import(original));

			Assert.NotSame(original, imported);
			Assert.Same(targetType, imported.Next!.ToTypeDefOrRef());
		}

		[Fact]
		public void NullSignaturesAreNull() {
			Assert.Null(importer.Import((TypeSig?)null));
			Assert.Null(importer.Import((MethodSig?)null));
			Assert.Null(importer.Import((FieldSig?)null));
			Assert.Null(importer.Import((CallingConventionSig?)null));
		}
	}
}
