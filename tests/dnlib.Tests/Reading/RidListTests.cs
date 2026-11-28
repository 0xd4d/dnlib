// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.MD;
using dnlib.Tests.Writer;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// A type's row has the number of its first field and of its first method; its fields end where the next
	/// type's fields start. A type without fields has the number after the last field. If the Field table has
	/// exactly 0xFFFF rows, these numbers are stored in 2 bytes, and "after the last field" (0x10000) doesn't fit:
	/// it's stored as 0. The same is true for the parameters of methods.
	/// </summary>
	public class RidListTests {
		const int MaxRowsWithSmallIndexes = 0xFFFF;

		static TypeDefUser AddType(CreatedModule created, string name) {
			var type = new TypeDefUser("Ns", name, created.Module.CorLibTypes.Object.TypeDefOrRef);
			created.Module.Types.Add(type);
			return type;
		}

		// Types in this order: Ns.Type (empty), Before (3 fields), Big (the rest of the 0xFFFF fields), and
		// then some types without fields
		static byte[] CreateImageWith0xFFFFFields(int emptyTypesAtTheEnd) {
			var created = new CreatedModule();
			var int32 = created.Module.CorLibTypes.Int32;
			var before = AddType(created, "Before");
			for (int i = 0; i < 3; i++)
				before.Fields.Add(new FieldDefUser("Before" + i, new FieldSig(int32), FieldAttributes.Public));
			var big = AddType(created, "Big");
			for (int i = 0; i < MaxRowsWithSmallIndexes - 3; i++)
				big.Fields.Add(new FieldDefUser("F" + i, new FieldSig(int32), FieldAttributes.Public | FieldAttributes.Static));
			for (int i = 0; i < emptyTypesAtTheEnd; i++)
				AddType(created, "Empty" + i);
			return created.Write();
		}

		// Fixed in e7215596: with two or more types without fields at the end, the last type with fields had
		// no fields
		[Theory, InlineData(0), InlineData(1), InlineData(2), InlineData(5)]
		public void FieldsAreReadIfTheFieldTableHas0xFFFFRows(int emptyTypesAtTheEnd) {
			var image = CreateImageWith0xFFFFFields(emptyTypesAtTheEnd);

			using (var module = ModuleDefMD.Load(image)) {
				Assert.Equal((uint)MaxRowsWithSmallIndexes, module.TablesStream.FieldTable.Rows);
				Assert.Equal(3, module.Find("Ns.Before", isReflectionName: true)!.Fields.Count);
				var big = module.Find("Ns.Big", isReflectionName: true)!;
				Assert.Equal(MaxRowsWithSmallIndexes - 3, big.Fields.Count);
				Assert.Equal("F0", big.Fields[0].Name);
				Assert.Equal("F" + (MaxRowsWithSmallIndexes - 4), big.Fields[big.Fields.Count - 1].Name);
				for (int i = 0; i < emptyTypesAtTheEnd; i++)
					Assert.Empty(module.Find("Ns.Empty" + i, isReflectionName: true)!.Fields);
			}
		}

		// The same file read by System.Reflection.Metadata. Only without types at the end that have no fields:
		// with them, System.Reflection.Metadata (and the CLR's reader for normal metadata) doesn't know that 0
		// means "after the last field" and gets a negative number of fields for the 'Big' type. dnlib reads
		// more than they do here.
		[Fact]
		public void AnotherReaderReadsTheSameFields() {
			var image = CreateImageWith0xFFFFFields(emptyTypesAtTheEnd: 0);

			using (var peReader = new PEReader(ImmutableArray.Create(image))) {
				var reader = peReader.GetMetadataReader();
				var fieldCounts = reader.TypeDefinitions.Select(a => reader.GetTypeDefinition(a)).ToDictionary(a => reader.GetString(a.Name), a => a.GetFields().Count);

				Assert.Equal(3, fieldCounts["Before"]);
				Assert.Equal(MaxRowsWithSmallIndexes - 3, fieldCounts["Big"]);
			}
		}

		// The parameters of methods: 255 methods with 257 parameters each are 0xFFFF Param rows
		[Theory, InlineData(0), InlineData(3)]
		public void ParametersAreReadIfTheParamTableHas0xFFFFRows(int methodsWithoutParametersAtTheEnd) {
			const int methodCount = 255, parameterCount = 257;
			var created = new CreatedModule();
			var int32 = created.Module.CorLibTypes.Int32;
			for (int i = 0; i < methodCount; i++) {
				var signature = MethodSig.CreateStatic(created.Module.CorLibTypes.Void, Enumerable.Repeat<TypeSig>(int32, parameterCount).ToArray());
				var method = new MethodDefUser("Method" + i, signature, MethodImplAttributes.IL, MethodAttributes.Public | MethodAttributes.Static);
				method.Body = new CilBody();
				method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
				created.Type.Methods.Add(method);
				for (int j = 0; j < parameterCount; j++)
					method.ParamDefs.Add(new ParamDefUser("p" + j, (ushort)(j + 1)));
			}
			for (int i = 0; i < methodsWithoutParametersAtTheEnd; i++)
				created.AddMethod("NoParameters" + i, OpCodes.Ret.ToInstruction());

			using (var module = created.WriteAndRead()) {
				Assert.Equal((uint)MaxRowsWithSmallIndexes, module.TablesStream.ParamTable.Rows);
				var type = CreatedModule.GetType(module);
				var last = type.FindMethod("Method" + (methodCount - 1))!;
				Assert.Equal(parameterCount, last.ParamDefs.Count);
				Assert.Equal("p" + (parameterCount - 1), last.Parameters[parameterCount - 1].Name);
				Assert.Equal(parameterCount, type.FindMethod("Method0")!.ParamDefs.Count);
				for (int i = 0; i < methodsWithoutParametersAtTheEnd; i++)
					Assert.Empty(type.FindMethod("NoParameters" + i)!.ParamDefs);
			}
		}

		// A RidList is a range of rows (the members of a type) or a list of rows (eg. with a FieldPtr table, or the
		// rows a lookup found)
		[Fact]
		public void ARangeIsEnumeratedAndIndexed() {
			var list = RidList.Create(5, 3);

			Assert.Equal(3, list.Count);
			Assert.Equal(new uint[] { 5, 6, 7 }, list.ToArray());
			Assert.Equal(5U, list[0]);
			Assert.Equal(7U, list[2]);
			Assert.Equal(0U, list[3]);
			Assert.Equal(0U, list[-1]);
		}

		[Fact]
		public void AListIsEnumeratedAndIndexed() {
			var list = RidList.Create(new uint[] { 9, 2, 4 });

			Assert.Equal(3, list.Count);
			Assert.Equal(new uint[] { 9, 2, 4 }, list.ToArray());
			Assert.Equal(9U, list[0]);
			Assert.Equal(4U, list[2]);
			Assert.Equal(0U, list[3]);
			Assert.Equal(0U, list[-1]);
		}

		[Fact]
		public void TheEmptyListHasNoRids() {
			Assert.Equal(0, RidList.Empty.Count);
			Assert.Empty(RidList.Empty);
			Assert.Equal(0U, RidList.Empty[0]);
		}

		// The struct enumerator (foreach), and the boxed one (IEnumerable<uint>, IEnumerable)
		[Fact]
		public void TheEnumeratorReturnsEveryRidOnceAndThenStops() {
			foreach (var list in new[] { RidList.Create(5, 2), RidList.Create(new uint[] { 5, 6 }) }) {
				var e = list.GetEnumerator();
				Assert.True(e.MoveNext());
				Assert.Equal(5U, e.Current);
				Assert.True(e.MoveNext());
				Assert.Equal(6U, ((System.Collections.IEnumerator)e).Current);
				Assert.False(e.MoveNext());
				Assert.Equal(0U, e.Current);
				Assert.False(e.MoveNext());
				Assert.Throws<NotSupportedException>(() => ((System.Collections.IEnumerator)e).Reset());
				e.Dispose();

				var items = new List<uint>();
				foreach (uint rid in (System.Collections.IEnumerable)list)
					items.Add(rid);
				Assert.Equal(new uint[] { 5, 6 }, items);
				Assert.Equal(new uint[] { 5, 6 }, ((IEnumerable<uint>)list).ToList());
			}
		}

		[Fact]
		public void AListMustNotBeNull() =>
			Assert.Throws<ArgumentNullException>(() => RidList.Create(null!));
	}
}
