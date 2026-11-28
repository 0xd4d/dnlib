// dnlib: See LICENSE.txt for more info

using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using Metadata = dnlib.DotNet.MD.Metadata;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// The lookups of <see cref="Metadata"/> that find the rows that belong to a row (the custom attributes of a
	/// type, the owner of a field, ...), with a row of a table that can't have them or a row that doesn't exist,
	/// and the helpers of the tables: coded tokens, columns and table sizes.
	/// </summary>
	public class MetadataLookupTests {
		static readonly ModuleDefMD module = ModuleDefMD.Load(Fixtures.Fixtures.Features.Image);
		static Metadata Metadata => module.Metadata;

		// The Assembly table can't have generic parameters, a field marshal or a constant, and no table has its
		// rows' custom attributes in the table number 0x3F
		[Fact]
		public void ARowOfATableThatCanNotHaveTheRowsHasNone() {
			Assert.Equal(0, Metadata.GetGenericParamRidList(Table.Assembly, 1).Count);
			Assert.Equal(0, Metadata.GetCustomAttributeRidList((Table)0x3F, 1).Count);
			Assert.Equal(0, Metadata.GetDeclSecurityRidList(Table.Field, 1).Count);
			Assert.Equal(0, Metadata.GetMethodSemanticsRidList(Table.TypeDef, 1).Count);
			Assert.Equal(0U, Metadata.GetFieldMarshalRid(Table.TypeDef, 1));
			Assert.Equal(0U, Metadata.GetImplMapRid(Table.TypeDef, 1));
			Assert.Equal(0U, Metadata.GetConstantRid(Table.TypeDef, 1));
		}

		// The owner of a member that doesn't exist is no type. The owners are found with a list that's created once.
		[Fact]
		public void TheOwnerOfAMemberThatDoesNotExistIsNoType() {
			var type = module.Find("Features.Code", isReflectionName: true)!;
			var field = type.Fields[0];
			var method = type.Methods[0];

			Assert.Equal(type.Rid, Metadata.GetOwnerTypeOfField(field.Rid));
			Assert.Equal(type.Rid, Metadata.GetOwnerTypeOfField(field.Rid));
			Assert.Equal(type.Rid, Metadata.GetOwnerTypeOfMethod(method.Rid));
			Assert.Equal(type.Rid, Metadata.GetOwnerTypeOfMethod(method.Rid));
			Assert.Equal(0U, Metadata.GetOwnerTypeOfField(0xFFFFFF));
			Assert.Equal(0U, Metadata.GetOwnerTypeOfMethod(0xFFFFFF));
			Assert.Equal(0U, Metadata.GetOwnerTypeOfEvent(0xFFFFFF));
			Assert.Equal(0U, Metadata.GetOwnerTypeOfProperty(0xFFFFFF));
			var withEvent = module.GetTypes().First(a => a.Events.Count > 0);
			var withProperty = module.GetTypes().First(a => a.Properties.Count > 0);
			Assert.Equal(withEvent.Rid, Metadata.GetOwnerTypeOfEvent(withEvent.Events[0].Rid));
			Assert.Equal(withEvent.Rid, Metadata.GetOwnerTypeOfEvent(withEvent.Events[0].Rid));
			Assert.Equal(withProperty.Rid, Metadata.GetOwnerTypeOfProperty(withProperty.Properties[0].Rid));
			Assert.Equal(withProperty.Rid, Metadata.GetOwnerTypeOfProperty(withProperty.Properties[0].Rid));
		}

		[Fact]
		public void TheMetadataHasTheValuesOfItsHeaders() {
			Assert.Equal(((uint)Metadata.MetadataHeader.MajorVersion << 16) | Metadata.MetadataHeader.MinorVersion, Metadata.Version);
			Assert.False(Metadata.IsStandalonePortablePdb);
			Assert.Same(Metadata.ImageCor20Header, Metadata.ImageCor20Header);
			Assert.True(Metadata.ImageCor20Header.Metadata.VirtualAddress != 0);
		}

		// The forms of encoding and decoding a coded token
		[Fact]
		public void CodedTokensAreEncodedAndDecoded() {
			var token = new MDToken(Table.TypeRef, 5);
			var coded = CodedToken.TypeDefOrRef;

			Assert.Equal((5U << 2) | 1, coded.Encode(token));
			Assert.Equal((5U << 2) | 1, coded.Encode(token.Raw));
			Assert.Equal(token.Raw, coded.Decode((5U << 2) | 1));
			// A table that the coded token can't have
			Assert.False(coded.Encode(new MDToken(Table.Field, 1).Raw, out uint invalid));
			Assert.Equal(uint.MaxValue, invalid);
			Assert.Equal(uint.MaxValue, coded.Encode(new MDToken(Table.Field, 1)));
		}

		// A column can be created with its offset and size, and reads and writes values of its size
		[Theory, InlineData(1), InlineData(2), InlineData(4)]
		public void AColumnReadsAndWritesValuesOfItsSize(byte size) {
			var column = new ColumnInfo(0, "Column", ColumnSize.UInt32, 0, size);
			var stream = new System.IO.MemoryStream();

			column.Write(new DataWriter(stream), 0x12345678U & (uint)((1UL << (size * 8)) - 1));

			Assert.Equal(size, stream.Length);
			var reader = dnlib.IO.ByteArrayDataReaderFactory.CreateReader(stream.ToArray());
			Assert.Equal(0x12345678U & (uint)((1UL << (size * 8)) - 1), column.Read(ref reader));
			Assert.Equal("Column", column.Name);
		}

		[Fact]
		public void AStreamHasValidOffsets() {
			var blob = Metadata.BlobStream;

			Assert.True(blob.IsValidOffset(0, 0));
			Assert.True(blob.IsValidOffset(1, 1));
			Assert.False(blob.IsValidOffset(blob.StreamLength, 1));
			Assert.True(Metadata.GuidStream!.IsValidIndex(1));
			Assert.False(Metadata.GuidStream.IsValidIndex(0x10000001));
			Assert.False(blob.TryCreateReader(uint.MaxValue, out _));
		}
	}
}
