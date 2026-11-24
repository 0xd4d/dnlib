// dnlib: See LICENSE.txt for more info

using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Names {
	/// <summary>
	/// With the API, a type can be nested in itself and a signature can contain itself. The name of such a
	/// type has no end: the properties that create names and the ones that follow the chain of types must
	/// return something instead of never returning or failing with a stack overflow. The name they return
	/// has the text <c>&lt;&lt;&lt;INFRECURSION&gt;&gt;&gt;</c> where they stopped.
	/// </summary>
	public class RecursiveTypeNameTests {
		const string RecursionError = "<<<INFRECURSION>>>";

		static ModuleDefUser CreateModule() {
			var module = new ModuleDefUser("Module.dll");
			new AssemblyDefUser("Assembly").Modules.Add(module);
			return module;
		}

		[Fact]
		public void TypeReferenceThatIsNestedInItself() {
			var typeRef = new TypeRefUser(CreateModule(), "Ns", "Self");
			typeRef.ResolutionScope = typeRef;

			Assert.StartsWith(RecursionError + "/Ns.Self/Ns.Self", typeRef.FullName);
			Assert.EndsWith("/Ns.Self", typeRef.FullName);
			Assert.StartsWith(RecursionError + "+Ns.Self+Ns.Self", typeRef.ReflectionFullName);
			Assert.Contains(RecursionError, typeRef.AssemblyQualifiedName);
			Assert.Equal("Self", typeRef.Name);
			// It has no assembly: there's no type at the end of the chain that says where it is
			Assert.Null(typeRef.DefinitionAssembly);
			Assert.Null(typeRef.Scope);
			Assert.Same(typeRef, typeRef.DeclaringType);
			Assert.Null(typeRef.Resolve());
			Assert.False(typeRef.IsValueType);
		}

		[Fact]
		public void TwoTypeReferencesThatAreNestedInEachOther() {
			var module = CreateModule();
			var first = new TypeRefUser(module, "Ns", "First");
			var second = new TypeRefUser(module, "Ns", "Second", first);
			first.ResolutionScope = second;

			Assert.StartsWith(RecursionError + "/Ns.Second/Ns.First/Ns.Second", first.FullName);
			Assert.EndsWith("/Ns.Second/Ns.First", first.FullName);
			Assert.EndsWith("/Ns.First/Ns.Second", second.FullName);
			Assert.Null(first.DefinitionAssembly);
			Assert.Null(first.Resolve());
			Assert.Null(second.Resolve());
			// Comparing them ends too
			Assert.False(new SigComparer().Equals(first, second));
			Assert.True(new SigComparer().Equals(first, first));
		}

		[Fact]
		public void ExportedTypeThatIsNestedInItself() {
			var exportedType = new ExportedTypeUser(CreateModule(), 0, "Ns", "Self", TypeAttributes.NestedPublic, null);
			exportedType.Implementation = exportedType;

			Assert.StartsWith(RecursionError + "/Ns.Self/Ns.Self", exportedType.FullName);
			Assert.StartsWith(RecursionError + "+Ns.Self+Ns.Self", exportedType.ReflectionFullName);
			Assert.Null(exportedType.DefinitionAssembly);
			Assert.Same(exportedType, exportedType.DeclaringType);
			Assert.Null(exportedType.Resolve());
		}

		[Fact]
		public void GenericInstanceThatIsItsOwnArgument() {
			var module = CreateModule();
			var genericType = new TypeRefUser(module, "Ns", "Generic`1", module.CorLibTypes.AssemblyRef);
			var instance = new GenericInstSig(new ClassSig(genericType));
			instance.GenericArguments.Add(instance);

			Assert.StartsWith("Ns.Generic`1<Ns.Generic`1<Ns.Generic`1<", instance.FullName);
			Assert.EndsWith(">>>", instance.FullName);
			Assert.StartsWith("Ns.Generic`1[[Ns.Generic`1[[", instance.AssemblyQualifiedName);
			Assert.Equal("Generic`1", instance.TypeName);
			Assert.Same(module.CorLibTypes.AssemblyRef, instance.DefinitionAssembly);
			Assert.False(instance.ContainsGenericParameter);
			Assert.True(new SigComparer().Equals(instance, instance));
			Assert.Equal(new SigComparer().GetHashCode(instance), new SigComparer().GetHashCode(instance));
		}

		// Not a loop, but more types in each other than any name is created for
		[Fact]
		public void ArrayOfArraysThatIsTooDeep() {
			var module = CreateModule();
			TypeSig array = module.CorLibTypes.Int32;
			for (int i = 0; i < 300; i++)
				array = new SZArraySig(array);

			Assert.StartsWith(RecursionError + "[][][]", array.FullName);
			Assert.StartsWith(RecursionError + "[][][]", array.ReflectionFullName);
			Assert.DoesNotContain("Int32", array.FullName);
			Assert.Null(array.DefinitionAssembly);
			Assert.Null(array.ScopeType);
			Assert.Equal(ElementType.SZArray, array.ElementType);
			Assert.StartsWith(RecursionError, new TypeSpecUser(array).FullName);
			Assert.Contains(RecursionError, FullNameFactory.MethodFullName("Type", "Method", MethodSig.CreateStatic(array, array)));

			// 50 arrays aren't too deep
			TypeSig smallArray = module.CorLibTypes.Int32;
			for (int i = 0; i < 50; i++)
				smallArray = new SZArraySig(smallArray);
			Assert.StartsWith("System.Int32[][][]", smallArray.FullName);
			Assert.Same(module.CorLibTypes.AssemblyRef, smallArray.DefinitionAssembly);
		}
	}
}
