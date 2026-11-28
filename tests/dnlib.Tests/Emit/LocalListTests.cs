// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb;
using Xunit;

namespace dnlib.Tests.Emit {
	/// <summary>
	/// The locals of a method body are a list (<see cref="CilBody.Variables"/>). An instruction like
	/// <c>ldloc</c> has the <see cref="Local"/> object as its operand, and the writer needs the local's index:
	/// the list keeps <see cref="Local.Index"/> of every local up to date when locals are added or removed.
	/// </summary>
	public class LocalListTests {
		readonly ModuleDefUser module = new ModuleDefUser("Test.dll");

		Local CreateLocal(string? name = null) => new Local(module.CorLibTypes.Int32, name);

		static void AssertIndexesAreThePositions(LocalList locals) {
			for (int i = 0; i < locals.Count; i++)
				Assert.Equal(i, locals[i].Index);
		}

		[Fact]
		public void AddedLocalsGetTheirIndex() {
			var locals = new LocalList();
			var first = CreateLocal();
			var second = CreateLocal();

			Assert.Same(first, locals.Add(first));
			((ICollection<Local>)locals).Add(second);

			Assert.Equal(2, locals.Count);
			Assert.Equal(0, first.Index);
			Assert.Equal(1, second.Index);
			Assert.Same(second, locals[1]);
			Assert.Equal(1, locals.IndexOf(second));
			Assert.Contains(first, locals);
			Assert.False(locals.IsReadOnly);
		}

		[Fact]
		public void InsertingAndRemovingChangesTheIndexesOfTheLocalsAfterIt() {
			var locals = new LocalList(new[] { CreateLocal("a"), CreateLocal("b"), CreateLocal("c") });
			var inserted = CreateLocal("inserted");

			locals.Insert(1, inserted);
			Assert.Equal(new[] { "a", "inserted", "b", "c" }, locals.Select(a => a.Name));
			AssertIndexesAreThePositions(locals);

			locals.RemoveAt(0);
			Assert.Equal(new[] { "inserted", "b", "c" }, locals.Select(a => a.Name));
			AssertIndexesAreThePositions(locals);

			Assert.True(locals.Remove(inserted));
			Assert.False(locals.Remove(inserted));
			Assert.Equal(new[] { "b", "c" }, locals.Select(a => a.Name));
			AssertIndexesAreThePositions(locals);
			// A local that isn't in a list has the index -1
			Assert.Equal(-1, inserted.Index);
		}

		[Fact]
		public void ReplacedAndClearedLocalsAreNotInTheListAnymore() {
			var old = CreateLocal("old");
			var other = CreateLocal("other");
			var locals = new LocalList(new[] { old, other });
			var replacement = CreateLocal("new");

			locals[0] = replacement;
			Assert.Equal(-1, old.Index);
			Assert.Equal(0, replacement.Index);
			Assert.DoesNotContain(old, locals);
			// LocalList.Contains() itself (Assert.Contains would enumerate the list)
			bool containsOld = locals.Contains(old), containsReplacement = locals.Contains(replacement);
			Assert.False(containsOld);
			Assert.True(containsReplacement);

			locals.Clear();
			Assert.Empty(locals);
			Assert.Equal(-1, replacement.Index);
			Assert.Equal(-1, other.Index);
		}

		[Fact]
		public void ListCanBeCopiedAndEnumerated() {
			var a = CreateLocal("a");
			var b = CreateLocal("b");
			var locals = new LocalList(new[] { a, b });

			var copy = new Local?[3];
			locals.CopyTo(copy!, 1);
			Assert.Equal(new[] { null, a, b }, copy);
			var enumerated = new List<Local>();
			foreach (var local in locals)
				enumerated.Add(local);
			Assert.Equal(new[] { a, b }, enumerated);
			Assert.Equal(new[] { a, b }, ((System.Collections.IEnumerable)locals).Cast<Local>());
		}

		[Fact]
		public void LocalHasATypeAndCanHaveAName() {
			var unnamed = new Local(module.CorLibTypes.String);
			var named = new Local(module.CorLibTypes.Int32, "count", 3);

			Assert.Same(module.CorLibTypes.String, unnamed.Type);
			Assert.Same(module.CorLibTypes.String, unnamed.TypeOrThrow);
			Assert.Null(unnamed.Name);
			// The text of a local without a name is V_ and its index, like in ildasm
			Assert.Equal("V_0", unnamed.ToString());
			Assert.Equal("count", named.ToString());
			Assert.Equal(3, named.Index);

			unnamed.Name = "text";
			unnamed.Attributes = PdbLocalAttributes.DebuggerHidden;
			unnamed.Type = null;
			Assert.Equal("text", unnamed.ToString());
			Assert.Equal(PdbLocalAttributes.DebuggerHidden, unnamed.Attributes);
			Assert.Throws<InvalidMetadataException>(() => unnamed.TypeOrThrow);
		}

		// The body of a method uses the indexes: a local that's removed before a local that an instruction uses
		[Fact]
		public void WrittenBodyHasTheLocalsInTheirNewOrder() {
			var created = new dnlib.Tests.Writer.CreatedModule();
			var types = created.Module.CorLibTypes;
			var removed = new Local(types.String);
			var used = new Local(types.Int64);
			var method = created.AddMethod("M", OpCodes.Ldloc.ToInstruction(used), OpCodes.Pop.ToInstruction(), OpCodes.Ret.ToInstruction());
			method.Body!.Variables.Add(removed);
			method.Body.Variables.Add(used);

			method.Body.Variables.Remove(removed);

			using (var written = created.WriteAndRead()) {
				var body = dnlib.Tests.Writer.CreatedModule.GetType(written).FindMethod("M")!.Body!;
				var local = Assert.Single(body.Variables);
				Assert.Equal("System.Int64", local.Type!.FullName);
				// The instruction now loads local 0 (it's still an ldloc: the writer doesn't change the form)
				Assert.Same(local, body.Instructions[0].Operand);
				Assert.Equal(Code.Ldloc, body.Instructions[0].OpCode.Code);
			}
		}
	}
}
