// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Comparing {
	/// <summary>
	/// The types of the runtime's own signatures compared with reflection types without loading a file (so they
	/// also run under a profiler, see <see cref="RuntimeOwnSignatureTypeReflectionTests"/>): a generic parameter by
	/// the rid of its row is the reflection type of that generic parameter, and <see cref="CanonZapSig"/> is
	/// <c>System.__Canon</c>. The module is this test assembly.
	/// </summary>
	public class RuntimeOwnSignatureTypeComparisonTests {
		public static class Generic<T> {
			public static void Method(T value) { }
		}

		static readonly Lazy<ModuleDefMD> module = new Lazy<ModuleDefMD>(() => ModuleDefMD.Load(typeof(RuntimeOwnSignatureTypeComparisonTests).Module));

		[Fact]
		public void AGenericParameterByItsRidIsTheReflectionGenericParameter() {
			// Mono's reflection has other tokens for generic parameters
			Assert.SkipWhen(Type.GetType("Mono.Runtime") is not null, "Mono's generic parameters have other tokens");
			var type = module.Value.Find(typeof(Generic<>).FullName!, isReflectionName: true)!;
			var genericParam = type.GenericParameters[0];
			var sig = new VarZapSig(genericParam.Rid, module.Value);
			var reflectionType = typeof(Generic<>).GetGenericArguments()[0];
			var comparer = new SigComparer();

			Assert.True(comparer.Equals(sig, reflectionType));
			Assert.Equal(comparer.GetHashCode(reflectionType), comparer.GetHashCode(sig));
			Assert.False(comparer.Equals(sig, typeof(int)));
			// Another row
			Assert.False(comparer.Equals(new VarZapSig(genericParam.Rid + 1000, module.Value), reflectionType));
		}

		[Fact]
		public void CanonIsSystemCanon() {
			// Mono doesn't have System.__Canon
			var canon = typeof(object).Assembly.GetType("System.__Canon", throwOnError: false);
			Assert.SkipWhen(canon is null, "The runtime has no System.__Canon");
			var comparer = new SigComparer();

			Assert.True(comparer.Equals(new CanonZapSig(module.Value), canon));
			Assert.False(comparer.Equals(new CanonZapSig(module.Value), typeof(object)));
		}

		// The native view of a type is never a reflection type (reflection doesn't have it)
		[Fact]
		public void TheNativeViewOfATypeIsNoReflectionType() =>
			Assert.False(new SigComparer().Equals(new NativeValueTypeZapSig(module.Value.CorLibTypes.Int32), typeof(int)));
	}
}
