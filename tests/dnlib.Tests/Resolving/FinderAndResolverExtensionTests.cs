// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Resolving {
	/// <summary>
	/// The short forms of the methods that find a type by its name (<see cref="ITypeDefFinder"/>: a module or
	/// an assembly) and that resolve a reference (<see cref="ITypeResolver"/>, <see cref="IMemberRefResolver"/>):
	/// <c>FindNormal()</c>, <c>FindReflection()</c>, <c>TypeExists()</c>, <c>ResolveField()</c>, and the
	/// <c>OrThrow</c> methods that throw instead of returning null.
	/// </summary>
	public sealed class FinderAndResolverExtensionTests : IDisposable {
		const string Source = @"
namespace Ns {
	public class Outer {
		public class Nested { }
		public int Field;
		public void Method() { }
	}
}
";
		readonly ModuleContext context = ModuleDef.CreateModuleContext();
		readonly ModuleDefMD module;
		readonly TypeDef outer, nested;

		public FinderAndResolverExtensionTests() {
			module = ModuleDefMD.Load(CSharpCompiler.Compile(Source).Image, context);
			outer = module.Find("Ns.Outer", isReflectionName: false)!;
			nested = Assert.Single(outer.NestedTypes);
		}

		public void Dispose() => module.Dispose();

		public static TheoryData<bool> ModuleOrAssembly => new TheoryData<bool> { false, true };

		ITypeDefFinder GetFinder(bool assembly) => assembly ? module.Assembly! : module;

		// A nested type is "Outer/Nested" in a normal name (as in IL) and "Outer+Nested" in a Reflection name
		[Theory, MemberData(nameof(ModuleOrAssembly))]
		public void TypeIsFoundByItsNormalOrReflectionName(bool assembly) {
			var finder = GetFinder(assembly);

			Assert.Same(outer, finder.FindNormal("Ns.Outer"));
			Assert.Same(outer, finder.FindReflection("Ns.Outer"));
			Assert.Same(nested, finder.FindNormal("Ns.Outer/Nested"));
			Assert.Same(nested, finder.FindReflection("Ns.Outer+Nested"));
			Assert.Null(finder.FindNormal("Ns.Outer+Nested"));
			Assert.Null(finder.FindReflection("Ns.Outer/Nested"));
			Assert.Null(finder.FindNormal("Ns.DoesNotExist"));

			Assert.Same(nested, finder.FindNormalOrThrow("Ns.Outer/Nested"));
			Assert.Same(nested, finder.FindReflectionOrThrow("Ns.Outer+Nested"));
			Assert.Same(nested, finder.FindOrThrow("Ns.Outer/Nested", isReflectionName: false));
			Assert.Same(nested, finder.FindOrThrow("Ns.Outer+Nested", isReflectionName: true));
		}

		[Theory, MemberData(nameof(ModuleOrAssembly))]
		public void TypeExists(bool assembly) {
			var finder = GetFinder(assembly);

			Assert.True(finder.TypeExistsNormal("Ns.Outer/Nested"));
			Assert.True(finder.TypeExistsReflection("Ns.Outer+Nested"));
			Assert.True(finder.TypeExists("Ns.Outer", isReflectionName: false));
			Assert.True(finder.TypeExists("Ns.Outer", isReflectionName: true));
			Assert.False(finder.TypeExistsNormal("Ns.Outer+Nested"));
			Assert.False(finder.TypeExistsReflection("Ns.Outer/Nested"));
			Assert.False(finder.TypeExists("Outer", isReflectionName: false));
			Assert.True(finder.TypeExists(new TypeRefUser(module, "Ns", "Outer", module)));
			Assert.False(finder.TypeExists(new TypeRefUser(module, "Ns", "DoesNotExist", module)));
			Assert.False(finder.TypeExists((TypeRef?)null));
		}

		[Theory, MemberData(nameof(ModuleOrAssembly))]
		public void TypeThatIsNotFoundIsATypeResolveException(bool assembly) {
			var finder = GetFinder(assembly);

			Assert.Contains("Ns.DoesNotExist", Assert.Throws<TypeResolveException>(() => finder.FindNormalOrThrow("Ns.DoesNotExist")).Message);
			Assert.Contains("Ns.DoesNotExist", Assert.Throws<TypeResolveException>(() => finder.FindReflectionOrThrow("Ns.DoesNotExist")).Message);
			Assert.Throws<TypeResolveException>(() => finder.FindOrThrow("Ns.Outer+Nested", isReflectionName: false));
			Assert.Throws<TypeResolveException>(() => finder.FindOrThrow("Ns.Outer/Nested", isReflectionName: true));
			Assert.Contains("Ns.DoesNotExist", Assert.Throws<TypeResolveException>(() => finder.FindOrThrow(new TypeRefUser(module, "Ns", "DoesNotExist", module))).Message);
			Assert.Throws<TypeResolveException>(() => finder.FindOrThrow((TypeRef?)null));
		}

		[Theory, MemberData(nameof(ModuleOrAssembly))]
		public void TypeIsFoundByAReference(bool assembly) {
			var finder = GetFinder(assembly);
			var outerRef = new TypeRefUser(module, "Ns", "Outer", module);
			var nestedRef = new TypeRefUser(module, "", "Nested", outerRef);

			Assert.Same(outer, finder.FindOrThrow(outerRef));
			Assert.Same(nested, finder.FindOrThrow(nestedRef));
		}

		[Fact]
		public void TypeReferenceIsResolved() {
			var resolver = context.Resolver;
			var outerRef = new TypeRefUser(module, "Ns", "Outer", module);
			var missingRef = new TypeRefUser(module, "Ns", "DoesNotExist", module);

			Assert.Same(outer, resolver.Resolve(outerRef));
			Assert.Same(outer, resolver.ResolveOrThrow(outerRef));
			Assert.Same(outer, resolver.ResolveOrThrow(outerRef, module));
			Assert.Null(resolver.Resolve(missingRef));
			Assert.Contains("Ns.DoesNotExist", Assert.Throws<TypeResolveException>(() => resolver.ResolveOrThrow(missingRef)).Message);
			Assert.Throws<TypeResolveException>(() => resolver.ResolveOrThrow(missingRef, module));
			Assert.Throws<TypeResolveException>(() => resolver.ResolveOrThrow((TypeRef?)null));
		}

		[Fact]
		public void MemberReferenceIsResolvedAsAFieldOrAMethod() {
			var resolver = context.Resolver;
			var outerRef = new TypeRefUser(module, "Ns", "Outer", module);
			var fieldRef = new MemberRefUser(module, "Field", new FieldSig(module.CorLibTypes.Int32), outerRef);
			var methodRef = new MemberRefUser(module, "Method", MethodSig.CreateInstance(module.CorLibTypes.Void), outerRef);
			var field = outer.FindField("Field");
			var method = outer.FindMethod("Method");

			Assert.Same(field, resolver.ResolveField(fieldRef));
			Assert.Same(field, resolver.ResolveFieldOrThrow(fieldRef));
			Assert.Same(field, resolver.ResolveOrThrow(fieldRef));
			Assert.Same(method, resolver.ResolveMethod(methodRef));
			Assert.Same(method, resolver.ResolveMethodOrThrow(methodRef));
			Assert.Same(method, resolver.ResolveOrThrow(methodRef));

			// A field isn't a method
			Assert.Null(resolver.ResolveMethod(fieldRef));
			Assert.Null(resolver.ResolveField(methodRef));
			Assert.Contains("Field", Assert.Throws<MemberRefResolveException>(() => resolver.ResolveMethodOrThrow(fieldRef)).Message);
			Assert.Contains("Method", Assert.Throws<MemberRefResolveException>(() => resolver.ResolveFieldOrThrow(methodRef)).Message);
		}

		[Fact]
		public void MemberThatIsNotFoundIsAMemberRefResolveException() {
			var resolver = context.Resolver;
			var outerRef = new TypeRefUser(module, "Ns", "Outer", module);
			var missingField = new MemberRefUser(module, "DoesNotExist", new FieldSig(module.CorLibTypes.Int32), outerRef);
			var missingMethod = new MemberRefUser(module, "Method", MethodSig.CreateInstance(module.CorLibTypes.Int32), outerRef);

			Assert.Null(resolver.ResolveField(missingField));
			Assert.Null(resolver.ResolveMethod(missingMethod));
			Assert.Contains("DoesNotExist", Assert.Throws<MemberRefResolveException>(() => resolver.ResolveOrThrow(missingField)).Message);
			Assert.Throws<MemberRefResolveException>(() => resolver.ResolveFieldOrThrow(missingField));
			Assert.Throws<MemberRefResolveException>(() => resolver.ResolveMethodOrThrow(missingMethod));
			Assert.Throws<MemberRefResolveException>(() => resolver.ResolveOrThrow((MemberRef?)null));
			Assert.Throws<MemberRefResolveException>(() => resolver.ResolveFieldOrThrow(null));
			Assert.Throws<MemberRefResolveException>(() => resolver.ResolveMethodOrThrow(null));
		}

		// The old names of the OrThrow methods still exist
#pragma warning disable CS0618 // obsolete
		[Fact]
		public void ObsoleteThrowMethodsAreTheOrThrowMethods() {
			var resolver = context.Resolver;
			var outerRef = new TypeRefUser(module, "Ns", "Outer", module);
			var missingRef = new TypeRefUser(module, "Ns", "DoesNotExist", module);
			var fieldRef = new MemberRefUser(module, "Field", new FieldSig(module.CorLibTypes.Int32), outerRef);
			var methodRef = new MemberRefUser(module, "Method", MethodSig.CreateInstance(module.CorLibTypes.Void), outerRef);

			Assert.Same(outer, module.FindThrow(outerRef));
			Assert.Same(outer, module.FindThrow("Ns.Outer", isReflectionName: false));
			Assert.Same(nested, module.FindNormalThrow("Ns.Outer/Nested"));
			Assert.Same(nested, module.FindReflectionThrow("Ns.Outer+Nested"));
			Assert.Same(outer, resolver.ResolveThrow(outerRef));
			Assert.Same(outer, resolver.ResolveThrow(outerRef, module));
			Assert.Same(outer.FindField("Field"), resolver.ResolveThrow(fieldRef));
			Assert.Same(outer.FindField("Field"), resolver.ResolveFieldThrow(fieldRef));
			Assert.Same(outer.FindMethod("Method"), resolver.ResolveMethodThrow(methodRef));

			Assert.Throws<TypeResolveException>(() => module.FindThrow(missingRef));
			Assert.Throws<TypeResolveException>(() => module.FindNormalThrow("Ns.DoesNotExist"));
			Assert.Throws<TypeResolveException>(() => module.FindReflectionThrow("Ns.DoesNotExist"));
			Assert.Throws<TypeResolveException>(() => resolver.ResolveThrow(missingRef));
			Assert.Throws<MemberRefResolveException>(() => resolver.ResolveMethodThrow(fieldRef));
			Assert.Throws<MemberRefResolveException>(() => resolver.ResolveFieldThrow(methodRef));
		}
#pragma warning restore CS0618
	}
}
