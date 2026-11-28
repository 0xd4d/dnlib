// dnlib: See LICENSE.txt for more info

using System;
using System.Collections;
using System.Linq;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Model {
	/// <summary>
	/// The lists of the object model (<c>LazyList</c>) read their items from the file the first time they're used.
	/// A list can't be changed while it's enumerated.
	/// </summary>
	public class LazyListTests {
		static readonly Lazy<ModuleDefMD> module = new Lazy<ModuleDefMD>(() => ModuleDefMD.Load(Fixtures.Fixtures.Features.Image));

		[Fact]
		public void AListThatIsChangedWhileItIsEnumeratedThrows() {
			var type = new TypeDefUser("Type");
			type.Fields.Add(new FieldDefUser("First"));
			type.Fields.Add(new FieldDefUser("Second"));

			Assert.Throws<InvalidOperationException>(() => {
				foreach (var field in type.Fields)
					type.Fields.Add(new FieldDefUser("Added"));
			});
		}

		[Fact]
		public void TheEnumeratorCanNotBeReset() {
			var type = new TypeDefUser("Type");
			type.Fields.Add(new FieldDefUser("Field"));
			IEnumerator enumerator = type.Fields.GetEnumerator();

			Assert.True(enumerator.MoveNext());
			Assert.Same(type.Fields[0], enumerator.Current);
			Assert.Throws<NotSupportedException>(() => enumerator.Reset());
		}

		// The items of a list of a module that was read are read when they're first used: the list has them all
		// and in the order of the file
		[Fact]
		public void TheItemsOfAReadListAreReadWhenTheyAreUsed() {
			var type = module.Value.GetTypes().First(a => a.Methods.Count > 3);
			var methods = type.Methods;

			var last = methods[methods.Count - 1];
			Assert.Equal(methods.Count - 1, methods.IndexOf(last));
			Assert.Contains(last, methods);
			Assert.Equal(methods.OrderBy(a => a.Rid).ToList(), methods.ToList());
			var copy = new MethodDef[methods.Count + 1];
			methods.CopyTo(copy, 1);
			Assert.Same(methods[0], copy[1]);
		}
	}
}
