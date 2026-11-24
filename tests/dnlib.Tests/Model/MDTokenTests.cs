// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using Xunit;

namespace dnlib.Tests.Model {
	/// <summary>
	/// <see cref="MDToken"/> is a metadata token: the number of a table in the upper 8 bits and the number of
	/// a row in that table (the "rid", the first row is 1) in the lower 24 bits.
	/// </summary>
	public class MDTokenTests {
		[Fact]
		public void TokenIsATableAndARid() {
			var token = new MDToken(Table.Method, 0x123456);

			Assert.Equal(0x06123456U, token.Raw);
			Assert.Equal(0x06123456U, token.ToUInt32());
			Assert.Equal(0x06123456, token.ToInt32());
			Assert.Equal(Table.Method, token.Table);
			Assert.Equal(0x123456U, token.Rid);
			Assert.False(token.IsNull);
			Assert.Equal("06123456", token.ToString());
		}

		[Fact]
		public void TokenCanBeCreatedFromItsValue() {
			Assert.Equal(new MDToken(Table.TypeSpec, 7), new MDToken(0x1B000007U));
			Assert.Equal(new MDToken(Table.TypeSpec, 7), new MDToken(0x1B000007));
			Assert.Equal(new MDToken(Table.TypeSpec, 7U), new MDToken(Table.TypeSpec, 7));
			// A value with the highest bit set is a negative int
			var genericParamConstraint = new MDToken(unchecked((int)0xFF000001));
			Assert.Equal(0xFF000001U, genericParamConstraint.Raw);
			Assert.Equal((Table)0xFF, genericParamConstraint.Table);
			Assert.Equal(-0x00FFFFFF, genericParamConstraint.ToInt32());
		}

		[Fact]
		public void RidHas24Bits() {
			var token = new MDToken(Table.Field, MDToken.RID_MAX);

			Assert.Equal(Table.Field, token.Table);
			Assert.Equal(0x00FFFFFFU, token.Rid);
			Assert.Equal(0x04FFFFFFU, token.Raw);
			Assert.Equal(0x00FFFFFFU, MDToken.RID_MASK);
			Assert.Equal(24, MDToken.TABLE_SHIFT);
		}

		[Fact]
		public void TokenWithRidZeroIsNull() {
			Assert.True(new MDToken(Table.TypeDef, 0).IsNull);
			Assert.True(default(MDToken).IsNull);
			Assert.Equal(Table.Module, default(MDToken).Table);
			Assert.False(new MDToken(Table.Module, 1).IsNull);
		}

		[Fact]
		public void StaticMethodsSplitAValue() {
			Assert.Equal(0x000010U, MDToken.ToRID(0x0A000010U));
			Assert.Equal(0x000010U, MDToken.ToRID(0x0A000010));
			Assert.Equal(Table.MemberRef, MDToken.ToTable(0x0A000010U));
			Assert.Equal(Table.MemberRef, MDToken.ToTable(0x0A000010));
			Assert.Equal((Table)0x80, MDToken.ToTable(unchecked((int)0x80000001)));
			Assert.Equal(1U, MDToken.ToRID(unchecked((int)0x80000001)));
		}

		[Fact]
		public void TokensAreEqualIfTheirValuesAreEqual() {
			var token = new MDToken(Table.Method, 5);
			var same = new MDToken(0x06000005U);
			var otherRid = new MDToken(Table.Method, 6);
			var otherTable = new MDToken(Table.Field, 5);

			Assert.True(token == same);
			Assert.False(token != same);
			Assert.True(token.Equals(same));
			Assert.True(token.Equals((object)same));
			Assert.Equal(token.GetHashCode(), same.GetHashCode());
			Assert.False(token == otherRid);
			Assert.True(token != otherTable);
			Assert.False(token.Equals(otherRid));
			Assert.False(token.Equals((object)otherTable));
			Assert.False(token.Equals(null));
			Assert.False(token.Equals(0x06000005U));
		}

		// Tokens are ordered by their value: by table, then by rid
		[Fact]
		public void TokensAreOrderedByTableAndRid() {
			var first = new MDToken(Table.Field, 9);
			var second = new MDToken(Table.Method, 1);
			var third = new MDToken(Table.Method, 2);
			var last = new MDToken(0xFF000001U);

			Assert.True(first < second);
			Assert.True(second < third);
			Assert.True(third < last);
			Assert.True(third > second);
			Assert.True(second <= third);
			Assert.True(second <= new MDToken(Table.Method, 1));
			Assert.True(third >= second);
			Assert.True(third >= new MDToken(Table.Method, 2));
			Assert.False(second > third);
			Assert.False(third < second);
			Assert.False(third <= second);
			Assert.False(second >= third);
			Assert.True(first.CompareTo(second) < 0);
			Assert.True(last.CompareTo(first) > 0);
			Assert.Equal(0, second.CompareTo(new MDToken(Table.Method, 1)));

			var sorted = new[] { last, second, first, third }.OrderBy(a => a).ToArray();
			Assert.Equal(new[] { first, second, third, last }, sorted);
			var array = new[] { third, last, first, second };
			Array.Sort(array);
			Assert.Equal(new[] { first, second, third, last }, array);
		}

		// The token of a member that was read is its table and its row
		[Fact]
		public void MembersOfAModuleHaveTheTokenOfTheirRow() {
			using (var module = ModuleDefMD.Load(Fixtures.Fixtures.Features.Image)) {
				Assert.Equal(new MDToken(Table.Module, 1), module.MDToken);
				Assert.Equal(new MDToken(Table.Assembly, 1), module.Assembly!.MDToken);
				Assert.Equal(new MDToken(Table.TypeDef, 1), module.GlobalType!.MDToken);
				uint rid = 0;
				foreach (var type in module.GetTypes().OrderBy(a => a.Rid)) {
					Assert.Equal(new MDToken(Table.TypeDef, ++rid), type.MDToken);
					Assert.Same(type, module.ResolveToken(type.MDToken));
					Assert.Same(type, module.ResolveToken(type.MDToken.Raw));
					Assert.Same(type, module.ResolveToken(type.MDToken.ToInt32()));
				}
				Assert.Equal(module.TablesStream.TypeDefTable.Rows, rid);
			}
		}
	}
}
