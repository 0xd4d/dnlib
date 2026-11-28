// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Comparing {
	/// <summary>
	/// <see cref="SigComparer"/> and <see cref="Importer"/> call themselves for every part of a type or a
	/// signature. They stop after <see cref="RecursionCounter.MAX_RECURSION_COUNT"/> levels instead of running out
	/// of stack (an invalid file can have a signature that contains itself): two things that are too deep are
	/// then not equal, and a part that is too deep isn't imported.
	///
	/// The tests put the same kind of thing at every depth near the limit, so the limit is reached in each
	/// method that compares, hashes or imports that kind of thing.
	/// </summary>
	public class SigComparerDepthTests {
		const int Limit = RecursionCounter.MAX_RECURSION_COUNT;

		static readonly ModuleDefUser module = CreateModule("Module");
		static readonly ModuleDefUser library = CreateModule("Library");
		static readonly TypeDefUser outer;
		static readonly TypeDefUser nested;
		static readonly TypeDefUser genericMethodOwner;
		static readonly MethodDefUser genericMethod;

		static ModuleDefUser CreateModule(string name) {
			var module = new ModuleDefUser(name + ".dll");
			new AssemblyDefUser(name, new Version(1, 0, 0, 0)).Modules.Add(module);
			return module;
		}

		static SigComparerDepthTests() {
			outer = new TypeDefUser("Ns", "Outer", library.CorLibTypes.Object.TypeDefOrRef);
			library.Types.Add(outer);
			nested = new TypeDefUser("", "Nested", library.CorLibTypes.Object.TypeDefOrRef);
			outer.NestedTypes.Add(nested);
			genericMethodOwner = new TypeDefUser("Ns", "Owner", library.CorLibTypes.Object.TypeDefOrRef);
			library.Types.Add(genericMethodOwner);
			genericMethod = new MethodDefUser("M", MethodSig.CreateStaticGeneric(1, library.CorLibTypes.Void), MethodAttributes.Public | MethodAttributes.Static);
			genericMethod.GenericParameters.Add(new GenericParamUser(0, GenericParamAttributes.NonVariant, "T"));
			genericMethodOwner.Methods.Add(genericMethod);
		}

		static TypeRefUser NestedRef() =>
			new TypeRefUser(module, "", "Nested", new TypeRefUser(module, "Ns", "Outer", new AssemblyRefUser(library.Assembly!)));

		// The kinds of types at the bottom of the nested signatures. Each pair is the same type: once as it's
		// defined, once as a reference, so that every kind of comparison is used.
		public static TheoryData<string> Leaves => new TheoryData<string> {
			"TypeDef/TypeRef", "TypeRef/TypeRef", "TypeDef/TypeDef", "TypeSpec/TypeSpec", "TypeDef/TypeSpec", "TypeRef/TypeSpec",
			"function pointer", "generic instance", "generic method instance", "modifier", "array",
		};

		static (TypeSig, TypeSig) CreateLeaves(string leaf) {
			TypeSig Def() => new ClassSig(nested);
			TypeSig Ref() => new ClassSig(NestedRef());
			TypeSig Spec(TypeSig sig) => new ClassSig(new TypeSpecUser(sig));
			switch (leaf) {
			case "TypeDef/TypeRef": return (Def(), Ref());
			case "TypeRef/TypeRef": return (Ref(), Ref());
			case "TypeDef/TypeDef": return (Def(), Def());
			case "TypeSpec/TypeSpec": return (Spec(Ref()), Spec(Ref()));
			case "TypeDef/TypeSpec": return (Def(), Spec(Ref()));
			case "TypeRef/TypeSpec": return (Ref(), Spec(Def()));
			case "function pointer":
				return (new FnPtrSig(MethodSig.CreateStatic(Def(), Ref())), new FnPtrSig(MethodSig.CreateStatic(Ref(), Def())));
			case "generic instance":
				return (new GenericInstSig((ClassSig)Def(), Ref()), new GenericInstSig((ClassSig)Ref(), Def()));
			case "generic method instance":
				return (new GenericMVar(0, genericMethod), new GenericMVar(0, genericMethod));
			case "modifier":
				return (new CModReqdSig(nested, Ref()), new CModReqdSig(NestedRef(), Def()));
			case "array":
				return (new ArraySig(Def(), 2), new ArraySig(Ref(), 2));
			default:
				throw new ArgumentException(leaf);
			}
		}

		// 'depth' levels of arrays around the type
		static TypeSig Wrap(TypeSig sig, int depth) {
			for (int i = 0; i < depth; i++)
				sig = i % 2 == 0 ? new SZArraySig(sig) : new PtrSig(sig);
			return sig;
		}

		static IEnumerable<int> DepthsNearTheLimit() {
			for (int depth = Limit - 8; depth <= Limit + 2; depth++)
				yield return depth;
		}

		// Shallow: equal, and the hash codes are equal. Too deep: not equal, and nothing throws.
		[Theory, MemberData(nameof(Leaves))]
		public void TypesAreComparedUpToTheLimit(string leaf) {
			var comparer = new SigComparer();
			var (a, b) = CreateLeaves(leaf);
			Assert.True(comparer.Equals(Wrap(a, 3), Wrap(b, 3)), leaf);
			Assert.Equal(comparer.GetHashCode(Wrap(a, 3)), comparer.GetHashCode(Wrap(b, 3)));

			foreach (int depth in DepthsNearTheLimit()) {
				var deepA = Wrap(a, depth);
				var deepB = Wrap(b, depth);
				comparer.Equals(deepA, deepB);
				comparer.GetHashCode(deepA);
				comparer.GetHashCode(deepB);
				// The same types in TypeSpecs, in a list, in a field signature, in a method signature, a local
				// signature, a generic method signature and a property signature
				comparer.Equals(new TypeSpecUser(deepA), new TypeSpecUser(deepB));
				comparer.Equals((IType)new TypeSpecUser(deepA), new TypeSpecUser(deepB));
				comparer.GetHashCode((IType)new TypeSpecUser(deepA));
				comparer.Equals(new List<TypeSig?> { deepA }, new List<TypeSig?> { deepB });
				comparer.Equals(new FieldSig(deepA), new FieldSig(deepB));
				comparer.GetHashCode(new FieldSig(deepA));
				comparer.Equals(MethodSig.CreateStatic(deepA), MethodSig.CreateStatic(deepB));
				comparer.GetHashCode(MethodSig.CreateStatic(deepA));
				comparer.Equals(new LocalSig(deepA), new LocalSig(deepB));
				comparer.GetHashCode(new LocalSig(deepA));
				comparer.Equals(new GenericInstMethodSig(deepA), new GenericInstMethodSig(deepB));
				comparer.GetHashCode(new GenericInstMethodSig(deepA));
				comparer.Equals(PropertySig.CreateStatic(deepA), PropertySig.CreateStatic(deepB));
				comparer.GetHashCode((CallingConventionSig)PropertySig.CreateStatic(deepA));
			}

			// Far below the limit everything is not equal: the comparer gave up
			Assert.False(comparer.Equals(Wrap(a, Limit + 10), Wrap(b, Limit + 10)));
		}

		// Members whose declaring type is a TypeSpec with a deep signature
		[Theory, MemberData(nameof(Leaves))]
		public void MembersOfDeepTypesAreComparedUpToTheLimit(string leaf) {
			var comparer = new SigComparer();
			var (a, b) = CreateLeaves(leaf);

			foreach (int depth in DepthsNearTheLimit()) {
				var parentA = new TypeSpecUser(Wrap(a, depth));
				var parentB = new TypeSpecUser(Wrap(b, depth));
				var methodA = new MemberRefUser(module, "M", MethodSig.CreateStatic(Wrap(a, depth)), parentA);
				var methodB = new MemberRefUser(module, "M", MethodSig.CreateStatic(Wrap(b, depth)), parentB);
				var fieldA = new MemberRefUser(module, "F", new FieldSig(Wrap(a, depth)), parentA);
				var fieldB = new MemberRefUser(module, "F", new FieldSig(Wrap(b, depth)), parentB);
				comparer.Equals((IMethod)methodA, methodB);
				comparer.GetHashCode((IMethod)methodA);
				comparer.Equals((IField)fieldA, fieldB);
				comparer.GetHashCode((IField)fieldA);
				comparer.Equals((IMemberRef)methodA, methodB);
				comparer.GetHashCode((IMemberRef)fieldA);
				var specA = new MethodSpecUser(methodA, new GenericInstMethodSig(Wrap(a, depth)));
				var specB = new MethodSpecUser(methodB, new GenericInstMethodSig(Wrap(b, depth)));
				comparer.Equals(specA, specB);
				comparer.GetHashCode(specA);
			}
		}

		// A deep part of a signature is left out (null) when it's imported
		[Theory, MemberData(nameof(Leaves))]
		public void TypesAreImportedUpToTheLimit(string leaf) {
			var (a, _) = CreateLeaves(leaf);
			var importer = new Importer(CreateModule("Target"));
			Assert.True(new SigComparer().Equals(Wrap(a, 3), importer.Import(Wrap(a, 3))));

			foreach (int depth in DepthsNearTheLimit()) {
				var deep = Wrap(a, depth);
				importer.Import(deep);
				importer.Import(new TypeSpecUser(deep));
				importer.Import(new FieldSig(deep));
				importer.Import(MethodSig.CreateStatic(deep, deep));
				importer.Import((CallingConventionSig)PropertySig.CreateStatic(deep));
				importer.Import((CallingConventionSig)new LocalSig(deep));
				importer.Import((CallingConventionSig)new GenericInstMethodSig(deep));
				importer.Import(new MemberRefUser(module, "F", new FieldSig(deep), new TypeSpecUser(deep)));
				importer.Import(new MemberRefUser(module, "M", MethodSig.CreateStatic(deep), new TypeSpecUser(deep)));
			}

			var tooDeep = importer.Import(Wrap(a, Limit + 10));
			Assert.NotNull(tooDeep);
			Assert.False(new SigComparer().Equals(Wrap(a, Limit + 10), tooDeep));
		}
	}
}
