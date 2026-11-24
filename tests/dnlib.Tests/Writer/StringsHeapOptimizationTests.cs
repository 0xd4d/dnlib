// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// The <c>#Strings</c> heap has the names in the metadata, each one ends with a zero byte. A name that's the
	/// end of another name needs no bytes of its own: if <c>Name</c> is at offset X, then <c>ame</c> is at
	/// offset X + 1. The writer stores every name like that, in whatever order the names are added.
	/// </summary>
	public class StringsHeapOptimizationTests {
		// A module whose class has a field for every name. Returns the offset of every field's name in the
		// written #Strings heap, and the heap.
		static Dictionary<string, uint> WriteFields(out byte[] heap, params string[] names) {
			var created = new CreatedModule();
			foreach (var name in names)
				created.AddField(name, created.Module.CorLibTypes.Int32);

			using (var module = created.WriteAndRead()) {
				heap = module.StringsStream.CreateReader().ToArray();
				var offsets = new Dictionary<string, uint>();
				for (uint rid = 1; rid <= module.TablesStream.FieldTable.Rows; rid++) {
					Assert.True(module.TablesStream.TryReadFieldRow(rid, out var row));
					string name = module.StringsStream.ReadNoNull(row.Name);
					Assert.Equal(names[rid - 1], name);
					offsets[name] = row.Name;
				}
				return offsets;
			}
		}

		// How many times the bytes of the text are in the heap
		static int Count(byte[] heap, string text) {
			var bytes = Encoding.UTF8.GetBytes(text);
			int count = 0;
			for (int i = 0; i + bytes.Length <= heap.Length; i++) {
				if (heap.Skip(i).Take(bytes.Length).SequenceEqual(bytes))
					count++;
			}
			return count;
		}

		[Theory]
		[InlineData("Name", "ame")]
		[InlineData("ame", "Name")]
		public void EndOfANameIsNotStoredAgain(string first, string second) {
			var offsets = WriteFields(out var heap, first, second);

			Assert.Equal(offsets["Name"] + 1, offsets["ame"]);
			Assert.Equal(1, Count(heap, "ame\0"));
		}

		[Theory]
		[InlineData("TheLongNamX", "LongNamX", "NamX", "mX", "X")]
		[InlineData("X", "mX", "NamX", "LongNamX", "TheLongNamX")]
		[InlineData("NamX", "X", "TheLongNamX", "mX", "LongNamX")]
		public void EveryEndOfTheLongestNameIsInsideIt(params string[] names) {
			var offsets = WriteFields(out var heap, names);

			uint longest = offsets["TheLongNamX"];
			foreach (var name in names)
				Assert.Equal(longest + (uint)("TheLongNamX".Length - name.Length), offsets[name]);
			Assert.Equal(1, Count(heap, "X\0"));
		}

		// Only the end of a name can be used: every name ends at a zero byte
		[Fact]
		public void StartAndMiddleOfANameAreStored() {
			var offsets = WriteFields(out var heap, "LongName", "Long", "ongNam");

			Assert.Equal(3, offsets.Values.Distinct().Count());
			Assert.Equal(1, Count(heap, "Long\0"));
			Assert.Equal(1, Count(heap, "ongNam\0"));
			Assert.Equal(1, Count(heap, "LongName\0"));
		}

		// Two names with the same end: the end is in one of them, the other one is stored completely
		[Fact]
		public void EndOfTwoNamesIsInOneOfThem() {
			var offsets = WriteFields(out var heap, "FirstName", "LastName", "Name");

			Assert.Contains(offsets["Name"], new[] { offsets["FirstName"] + 5, offsets["LastName"] + 4 });
			Assert.Equal(2, Count(heap, "Name\0"));
			Assert.NotEqual(offsets["FirstName"], offsets["LastName"]);
		}

		// The class and its namespace are "Type" and "Ns": the names of all tables are in the same heap
		[Fact]
		public void NamesOfOtherTablesAreUsedToo() {
			var created = new CreatedModule();
			created.AddField("pe", created.Module.CorLibTypes.Int32);
			created.AddMethod("s");

			using (var module = created.WriteAndRead()) {
				Assert.True(module.TablesStream.TryReadTypeDefRow(2, out var type));
				Assert.True(module.TablesStream.TryReadFieldRow(1, out var field));
				Assert.True(module.TablesStream.TryReadMethodRow(1, out var method));
				Assert.Equal("Type", module.StringsStream.ReadNoNull(type.Name));
				Assert.Equal(type.Name + 2, field.Name);
				Assert.Equal(type.Namespace + 1, method.Name);
			}
		}

		// The bytes are compared, not the characters: 'é' is the two UTF-8 bytes C3 A9
		[Fact]
		public void EndOfANameWithOtherCharacters() {
			var offsets = WriteFields(out var heap, "Café", "é", "fé", "\u00A9");

			Assert.Equal(offsets["Café"] + 2, offsets["fé"]);
			Assert.Equal(offsets["Café"] + 3, offsets["é"]);
			Assert.Equal(1, Count(heap, "é\0"));
			// U+00A9 is the bytes C2 A9: only its last byte is the last byte of "Café"
			Assert.NotInRange(offsets["\u00A9"], offsets["Café"], offsets["Café"] + 5);
			Assert.Equal(1, Count(heap, "\u00A9\0"));
		}

		// The empty string is always at offset 0, and a name that's used twice is stored once
		[Fact]
		public void EmptyNameAndNameThatIsUsedTwice() {
			var created = new CreatedModule();
			created.AddField("", created.Module.CorLibTypes.Int32);
			created.AddField("Twice", created.Module.CorLibTypes.Int32);
			created.AddField("Twice", created.Module.CorLibTypes.Int64);

			using (var module = created.WriteAndRead()) {
				var names = Enumerable.Range(1, 3).Select(rid => {
					Assert.True(module.TablesStream.TryReadFieldRow((uint)rid, out var row));
					return row.Name;
				}).ToArray();
				Assert.Equal(0U, names[0]);
				Assert.Equal(names[1], names[2]);
				Assert.Equal(1, Count(module.StringsStream.CreateReader().ToArray(), "Twice\0"));
			}
		}

		// StringsHeap.Create() gives a string an offset at once (Add() only does when all names are known). The
		// names that are added later use the end of such a string too.
		[Fact]
		public void EndOfAStringThatAlreadyHasAnOffsetIsUsed() {
			var created = new CreatedModule();
			created.AddField("Name", created.Module.CorLibTypes.Int32);
			uint createdOffset = 0;
			var options = new ModuleWriterOptions(created.Module);
			options.WriterEvent += (sender, e) => {
				if (e.Event == ModuleWriterEvent.MDBeginCreateTables)
					createdOffset = e.Writer.Metadata.StringsHeap.Create("CreatedName");
			};
			var stream = new MemoryStream();
			created.Module.Write(stream, options);

			using (var module = ModuleDefMD.Load(stream.ToArray())) {
				Assert.Equal("CreatedName", module.StringsStream.ReadNoNull(createdOffset));
				Assert.True(module.TablesStream.TryReadFieldRow(1, out var field));
				Assert.Equal(createdOffset + 7, field.Name);
			}
		}

		// The same heap, whatever the order of the fields is: the size doesn't depend on it
		[Fact]
		public void SizeOfTheHeapDoesNotDependOnTheOrder() {
			var names = new[] { "Name", "ame", "LongName", "Other", "ther", "r", "Another" };
			WriteFields(out var heap, names);
			WriteFields(out var reversedHeap, names.Reverse().ToArray());

			Assert.Equal(heap.Length, reversedHeap.Length);
			// "LongName", "Another" and "Other" are stored ("Other" isn't the end of "Another": 'O' and 'o')
			Assert.Equal(1, Count(heap, "Name\0"));
			Assert.Equal(2, Count(heap, "ther\0"));
		}
	}
}
