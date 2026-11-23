// dnlib: See LICENSE.txt for more info

namespace dnlib.Tests {
	/// <summary>
	/// Values of the <c>Category</c> trait. Usage: <c>[Trait(TestCategories.Name, TestCategories.Slow)]</c>
	/// </summary>
	static class TestCategories {
		public const string Name = "Category";

		/// <summary>
		/// Tests that take too long to run on every change, eg. tests that use every assembly of the runtime.
		/// tests/test.sh only runs them if it's started with --slow.
		/// </summary>
		public const string Slow = "Slow";
	}
}
