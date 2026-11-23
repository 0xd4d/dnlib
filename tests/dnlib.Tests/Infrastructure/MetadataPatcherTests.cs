// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Infrastructure {
	public class MetadataPatcherTests {
		static readonly byte[] validImage = CSharpCompiler.Compile(@"
public class Base { }
public class Derived : Base { }
").Image;

		[Fact]
		public void GetCellReadsTheOriginalValue() {
			var patcher = new MetadataPatcher(validImage);
			uint rid = patcher.FindRow(Table.TypeDef, "Name", "Derived");

			using (var module = ModuleDefMD.Load(validImage)) {
				var row = module.TablesStream.TryReadTypeDefRow(rid, out var typeDefRow);
				Assert.True(row);
				Assert.Equal(typeDefRow.Extends, patcher.GetCell(Table.TypeDef, rid, "Extends"));
				Assert.Equal(typeDefRow.Flags, patcher.GetCell(Table.TypeDef, rid, "Flags"));
			}
		}

		[Fact]
		public void SetCellChangesOnlyThatCell() {
			var patcher = new MetadataPatcher(validImage);
			uint baseRid = patcher.FindRow(Table.TypeDef, "Name", "Base");
			uint derivedRid = patcher.FindRow(Table.TypeDef, "Name", "Derived");
			uint baseExtends = patcher.GetCell(Table.TypeDef, baseRid, "Extends");

			// Derived's base class is now what Base's base class is: System.Object
			var patchedImage = patcher.SetCell(Table.TypeDef, derivedRid, "Extends", baseExtends).ToArray();

			Assert.Equal(validImage.Length, patchedImage.Length);
			int differentBytes = 0;
			for (int i = 0; i < validImage.Length; i++) {
				if (validImage[i] != patchedImage[i])
					differentBytes++;
			}
			Assert.InRange(differentBytes, 1, 4);
			using (var module = ModuleDefMD.Load(patchedImage)) {
				Assert.Equal("System.Object", module.Find("Derived", isReflectionName: true)!.BaseType!.FullName);
				Assert.Equal("System.Object", module.Find("Base", isReflectionName: true)!.BaseType!.FullName);
			}
		}

		[Fact]
		public void TheInputArrayIsNotModified() {
			var original = (byte[])validImage.Clone();
			var patcher = new MetadataPatcher(validImage);

			patcher.SetCell(Table.TypeDef, 1, "Flags", 0x12345678);

			Assert.Equal(original, validImage);
		}

		[Fact]
		public void RenameStreamRenamesTheTablesStream() {
			var patchedImage = new MetadataPatcher(validImage).RenameStream("#~", "#-").ToArray();

			using (var module = ModuleDefMD.Load(patchedImage)) {
				Assert.False(module.Metadata.IsCompressed);
				Assert.NotNull(module.Find("Derived", isReflectionName: true));
			}
		}

		[Fact]
		public void InvalidArgumentsThrow() {
			var patcher = new MetadataPatcher(validImage);

			Assert.Throws<ArgumentOutOfRangeException>(() => patcher.GetCell(Table.TypeDef, 0, "Name"));
			Assert.Throws<ArgumentOutOfRangeException>(() => patcher.GetCell(Table.TypeDef, 1000, "Name"));
			Assert.Throws<ArgumentException>(() => patcher.GetCell(Table.TypeDef, 1, "NoSuchColumn"));
			Assert.Throws<ArgumentException>(() => patcher.FindRow(Table.TypeDef, "Name", "NoSuchType"));
			Assert.Throws<ArgumentException>(() => patcher.RenameStream("#~", "#Longer"));
			Assert.Throws<ArgumentException>(() => patcher.RenameStream("#No", "#Xy"));
			Assert.Throws<ArgumentOutOfRangeException>(() => patcher.SetBytes(validImage.Length, 0));
		}

		[Fact]
		public void ValueThatDoesNotFitInTheColumnThrows() {
			var patcher = new MetadataPatcher(validImage);

			// A small file has 2-byte heap offsets and coded tokens
			Assert.Throws<ArgumentOutOfRangeException>(() => patcher.SetCell(Table.TypeDef, 1, "Name", 0x10000));
		}
	}
}
