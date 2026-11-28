// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Signatures {
	/// <summary>
	/// GenericTypeArgsSubstitutor.Substitute() (internal, used to find the members of a generic base type) replace the generic
	/// parameters of a type (<c>!0</c>, <c>!1</c>) with the type arguments, in every part of a signature.
	/// </summary>
	public class GenericSubstitutionTests {
		static readonly ModuleDefUser module = new ModuleDefUser("Test.dll");
		static ICorLibTypes CorLib => module.CorLibTypes;
		static readonly IList<TypeSig?> Args = new TypeSig?[] { CorLib.Int32, CorLib.String };
		static readonly TypeRefUser modifier = new TypeRefUser(module, "System.Runtime.CompilerServices", "IsVolatile");

		static string? Substitute(TypeSig sig) => GenericTypeArgsSubstitutor.Substitute(sig, Args)?.FullName;

		// Every kind of signature that has another signature inside it
		[Fact]
		public void TheGenericParametersInsideOtherSignaturesAreReplaced() {
			var t0 = new GenericVar(0);
			var t1 = new GenericVar(1);

			Assert.Equal("System.Int32*", Substitute(new PtrSig(t0)));
			Assert.Equal("System.String&", Substitute(new ByRefSig(t1)));
			Assert.Equal("System.Int32[]", Substitute(new SZArraySig(t0)));
			Assert.Equal("System.Int32[,]", Substitute(new ArraySig(t0, 2)));
			Assert.Equal("System.Int32 modreq(System.Runtime.CompilerServices.IsVolatile)", Substitute(new CModReqdSig(modifier, t0)));
			Assert.Equal("System.Int32 modopt(System.Runtime.CompilerServices.IsVolatile)", Substitute(new CModOptSig(modifier, t0)));
			Assert.Equal("System.String", Substitute(new PinnedSig(t1)));
			Assert.IsType<PinnedSig>(GenericTypeArgsSubstitutor.Substitute(new PinnedSig(t1), Args));
			var valueArray = Assert.IsType<ValueArraySig>(GenericTypeArgsSubstitutor.Substitute(new ValueArraySig(t0, 3), Args));
			Assert.Equal(3U, valueArray.Size);
			Assert.Equal(ElementType.I4, valueArray.Next!.ElementType);
			var moduleSig = Assert.IsType<ModuleSig>(GenericTypeArgsSubstitutor.Substitute(new ModuleSig(2, t1), Args));
			Assert.Equal(2U, moduleSig.Index);
			Assert.Equal(ElementType.String, moduleSig.Next!.ElementType);
			var nativeValueType = Assert.IsType<NativeValueTypeZapSig>(GenericTypeArgsSubstitutor.Substitute(new NativeValueTypeZapSig(t0), Args));
			Assert.Equal(ElementType.I4, nativeValueType.Next!.ElementType);
		}

		// A signature without generic parameters is returned as it is, and so is a missing one
		[Fact]
		public void ASignatureWithoutGenericParametersIsNotCopied() {
			var sig = new SZArraySig(CorLib.Int32);

			Assert.Same(sig, GenericTypeArgsSubstitutor.Substitute(sig, Args));
			Assert.Same(sig, GenericTypeArgsSubstitutor.Substitute(sig, null));
			Assert.Null(GenericTypeArgsSubstitutor.Substitute((TypeSig?)null, Args));
			var property = PropertySig.CreateStatic(CorLib.Int32);
			Assert.Same(property, GenericTypeArgsSubstitutor.Substitute(property, Args));
			Assert.Null(GenericTypeArgsSubstitutor.Substitute((PropertySig?)null, Args));
			var list = new List<TypeSig?> { CorLib.Int32 };
			Assert.Same(list, GenericTypeArgsSubstitutor.Substitute(list, null));
		}

		[Fact]
		public void TheGenericParametersOfOtherSignaturesAreReplaced() {
			var property = GenericTypeArgsSubstitutor.Substitute(PropertySig.CreateInstance(new GenericVar(1), new GenericVar(0)), Args)!;
			Assert.Equal(ElementType.String, property.RetType!.ElementType);
			Assert.Equal(ElementType.I4, property.Params[0]!.ElementType);

			var list = GenericTypeArgsSubstitutor.Substitute(new List<TypeSig?> { new GenericVar(1), CorLib.Boolean }, Args)!;
			Assert.Equal(new[] { ElementType.String, ElementType.Boolean }, new[] { list[0]!.ElementType, list[1]!.ElementType });

			var field = (FieldSig)GenericTypeArgsSubstitutor.Substitute((CallingConventionSig)new FieldSig(new GenericVar(0)), Args)!;
			Assert.Equal(ElementType.I4, field.Type!.ElementType);
			// A signature that isn't a method, field or property signature isn't changed
			var local = new LocalSig(new GenericVar(0));
			Assert.Same(local, GenericTypeArgsSubstitutor.Substitute((CallingConventionSig)local, Args));
		}

		[Fact]
		public void NullIsResolvedToNull() => Assert.Null(new GenericArguments().Resolve((TypeSig?)null));
	}
}
