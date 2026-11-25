// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// The NestedClass table says which types are nested and in which type. It's sorted by the nested type, and
	/// the CLR finds a type's row with a binary search. In normal (<c>#~</c>) metadata it does that even if the
	/// table isn't sorted: it then doesn't find every row, and a type whose row it doesn't find is not a nested
	/// type. The tests write the rows in the opposite order.
	/// </summary>
	public class UnsortedNestedClassTableTests {
		static byte[] ValidImage => Fixtures.Fixtures.Features.Image;

		static byte[] CreateImageWithReversedNestedClassTable() {
			var original = new MetadataPatcher(ValidImage);
			var patcher = new MetadataPatcher(ValidImage);
			uint rows = original.GetRowCount(Table.NestedClass);
			Assert.True(rows >= 5, "The test input needs more nested types");
			foreach (var column in original.GetColumnNames(Table.NestedClass)) {
				for (uint rid = 1; rid <= rows; rid++)
					patcher.SetCell(Table.NestedClass, rid, column, original.GetCell(Table.NestedClass, rows + 1 - rid, column));
			}
			return patcher.ToArray();
		}

		// Fixed in 36a53aa1: a type whose row isn't found was in no list at all: not a type of the module, and
		// not a nested type of another type
		[Fact]
		public void EveryTypeIsATypeOfTheModuleOrANestedType() {
			using (var module = ModuleDefMD.Load(CreateImageWithReversedNestedClassTable())) {
				var types = module.GetTypes().ToList();

				Assert.Equal((int)module.TablesStream.TypeDefTable.Rows, types.Count);
				Assert.Equal(types.Count, types.Distinct().Count());
				foreach (var type in types) {
					if (type.DeclaringType is null)
						Assert.Contains(type, module.Types);
					else
						Assert.Contains(type, type.DeclaringType.NestedTypes);
				}
			}
		}

		// The names of the types that have a declaring type, with the name of that type ("Outer+Inner")
		static List<string> GetNestedTypes(ModuleDef module) =>
			module.GetTypes().Where(a => a.DeclaringType is not null).Select(a => a.DeclaringType!.Name + "+" + a.Name).OrderBy(a => a, StringComparer.Ordinal).ToList();

		[Fact]
		public void OnlyTheTypesWhoseRowsABinarySearchFindsAreNested() {
			List<string> originalNestedTypes;
			using (var original = ModuleDefMD.Load(ValidImage))
				originalNestedTypes = GetNestedTypes(original);

			using (var module = ModuleDefMD.Load(CreateImageWithReversedNestedClassTable())) {
				var nestedTypes = GetNestedTypes(module);

				// Some rows are still found (eg. the one in the middle), and those types are in the same types
				Assert.NotEmpty(nestedTypes);
				Assert.True(nestedTypes.Count < originalNestedTypes.Count, $"{nestedTypes.Count} of {originalNestedTypes.Count} nested types");
				Assert.All(nestedTypes, a => Assert.Contains(a, originalNestedTypes));
			}
		}

		// The runtime's reflection agrees about which types are nested
		[Fact]
		public void RuntimeHasTheSameNestedTypes() {
			Assert.SkipWhen(Type.GetType("Mono.Runtime") is not null, "Mono reads the table in another way");
			var image = CreateImageWithReversedNestedClassTable();

			List<string> expected;
			using (var loaded = new LoadedAssembly(image, canBeUnloaded: false)) {
				Type?[] types;
				try {
					types = loaded.Assembly.GetTypes();
				}
				catch (System.Reflection.ReflectionTypeLoadException ex) {
					// The runtime doesn't load a type that isn't nested anymore but isn't public or internal:
					// it returns the types that it could load
					types = ex.Types;
				}
				expected = types.Where(a => a?.DeclaringType is not null).Select(a => a!.DeclaringType!.Name + "+" + a.Name)
					.OrderBy(a => a, StringComparer.Ordinal).ToList();
			}

			using (var module = ModuleDefMD.Load(image))
				Assert.Equal(expected, GetNestedTypes(module));
		}

		// Also fixed in 36a53aa1: the writer failed because the lost types were still used by the module
		[Theory, InlineData(false), InlineData(true)]
		public void ModuleWithSuchATableCanBeWritten(bool preserveMetadata) {
			using (var module = ModuleDefMD.Load(CreateImageWithReversedNestedClassTable())) {
				var options = new dnlib.DotNet.Writer.ModuleWriterOptions(module);
				if (preserveMetadata)
					options.MetadataOptions.Flags |= dnlib.DotNet.Writer.MetadataFlags.PreserveAll;
				var stream = new System.IO.MemoryStream();

				module.Write(stream, options);

				// The written file has a sorted table: the same types are nested, in the same types
				using (var written = ModuleDefMD.Load(stream.ToArray())) {
					Assert.Equal(module.GetTypes().Count(), written.GetTypes().Count());
					Assert.Equal(GetNestedTypes(module), GetNestedTypes(written));
				}
			}
		}
	}
}
