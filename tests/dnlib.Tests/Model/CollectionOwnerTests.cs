// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Model {
	/// <summary>
	/// The lists of the object model know their owner: adding a method to <see cref="TypeDef.Methods"/> sets
	/// <see cref="MethodDef.DeclaringType"/>, removing it resets it, and something that already has an owner
	/// can't be added to another list. A call that throws must not change anything.
	/// </summary>
	public class CollectionOwnerTests {
		static MethodDefUser CreateMethod(string name) => new MethodDefUser(name, MethodSig.CreateStatic(null));

		[Fact]
		public void AddSetsTheOwnerAndRemoveResetsIt() {
			var type = new TypeDefUser("Type");
			var method = CreateMethod("M");

			type.Methods.Add(method);
			Assert.Same(type, method.DeclaringType);

			Assert.True(type.Methods.Remove(method));
			Assert.Null(method.DeclaringType);
		}

		[Fact]
		public void SettingTheOwnerAddsToTheList() {
			var type = new TypeDefUser("Type");
			var method = CreateMethod("M");

			method.DeclaringType = type;
			Assert.Same(method, Assert.Single(type.Methods));

			method.DeclaringType = null;
			Assert.Empty(type.Methods);
		}

		[Fact]
		public void ItemWithAnOwnerCanNotBeAddedToAnotherList() {
			var owner = new TypeDefUser("Owner");
			var other = new TypeDefUser("Other");
			var method = CreateMethod("M");
			owner.Methods.Add(method);

			Assert.Throws<InvalidOperationException>(() => other.Methods.Add(method));

			Assert.Same(owner, method.DeclaringType);
			Assert.Empty(other.Methods);
			Assert.Same(method, Assert.Single(owner.Methods));
		}

		[Fact]
		public void ClearResetsTheOwnerOfAllItems() {
			var type = new TypeDefUser("Type");
			var first = CreateMethod("First");
			var second = CreateMethod("Second");
			type.Methods.Add(first);
			type.Methods.Add(second);

			type.Methods.Clear();

			Assert.Null(first.DeclaringType);
			Assert.Null(second.DeclaringType);
		}

		// Fixed in 43005650: the item got an owner although it wasn't inserted
		[Theory, InlineData(-1), InlineData(2)]
		public void InsertWithAnInvalidIndexDoesNotSetTheOwner(int index) {
			var type = new TypeDefUser("Type");
			type.Methods.Add(CreateMethod("Existing"));
			var method = CreateMethod("M");

			Assert.Throws<ArgumentOutOfRangeException>(() => type.Methods.Insert(index, method));

			Assert.Null(method.DeclaringType);
			Assert.Single(type.Methods);
		}

		// Fixed in a9aeabdd: the old item lost its owner although it was still in the list
		[Fact]
		public void ReplacingAnItemWithOneThatHasAnOwnerChangesNothing() {
			var type = new TypeDefUser("Type");
			var oldMethod = CreateMethod("Old");
			type.Methods.Add(oldMethod);
			var other = new TypeDefUser("Other");
			var ownedMethod = CreateMethod("Owned");
			other.Methods.Add(ownedMethod);

			Assert.Throws<InvalidOperationException>(() => type.Methods[0] = ownedMethod);

			Assert.Same(oldMethod, type.Methods[0]);
			Assert.Same(type, oldMethod.DeclaringType);
			Assert.Same(other, ownedMethod.DeclaringType);
		}

		[Fact]
		public void ReplacingAnItemMovesTheOwner() {
			var type = new TypeDefUser("Type");
			var oldMethod = CreateMethod("Old");
			var newMethod = CreateMethod("New");
			type.Methods.Add(oldMethod);

			type.Methods[0] = newMethod;

			Assert.Null(oldMethod.DeclaringType);
			Assert.Same(type, newMethod.DeclaringType);
		}

		[Fact]
		public void ReplacingAnItemWithItselfKeepsTheOwner() {
			var type = new TypeDefUser("Type");
			var method = CreateMethod("M");
			type.Methods.Add(method);

			type.Methods[0] = method;

			Assert.Same(type, method.DeclaringType);
		}

		[Fact]
		public void TypeAddedToAModuleGetsTheModule() {
			var module = new ModuleDefUser("Module.dll");
			var type = new TypeDefUser("Type");
			var nested = new TypeDefUser("Nested");
			type.NestedTypes.Add(nested);

			module.Types.Add(type);
			Assert.Same(module, type.Module);
			Assert.Same(module, nested.Module);
			Assert.Same(type, nested.DeclaringType);

			module.Types.Remove(type);
			Assert.Null(type.Module);
			Assert.Null(nested.Module);
		}

		// Fixed in 44e1e969: a nested type that was read from a file still returned the file's module
		[Fact]
		public void ClearingNestedTypesResetsTheirModule() {
			var image = CSharpCompiler.Compile("public class Outer { public class Inner { } }").Image;
			using (var module = ModuleDefMD.Load(image)) {
				var outer = module.Find("Outer", isReflectionName: true)!;
				var inner = Assert.Single(outer.NestedTypes);

				outer.NestedTypes.Clear();

				Assert.Null(inner.DeclaringType);
				Assert.Null(inner.Module);
			}
		}

		[Fact]
		public void RemovingANestedTypeResetsItsModule() {
			var image = CSharpCompiler.Compile("public class Outer { public class Inner { } }").Image;
			using (var module = ModuleDefMD.Load(image)) {
				var outer = module.Find("Outer", isReflectionName: true)!;
				var inner = Assert.Single(outer.NestedTypes);

				outer.NestedTypes.Remove(inner);

				Assert.Null(inner.DeclaringType);
				Assert.Null(inner.Module);
			}
		}
	}
}
