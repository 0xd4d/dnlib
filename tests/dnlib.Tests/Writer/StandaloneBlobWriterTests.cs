// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// The writers of the blobs in the metadata can be used without writing a module: <see cref="SignatureWriter"/>,
	/// <see cref="CustomAttributeWriter"/>, <see cref="MarshalBlobWriter"/> and <see cref="DeclSecurityWriter"/>
	/// return the bytes of one blob. Every blob of a compiled module is read and written again: the bytes
	/// must be the bytes the compiler wrote.
	/// </summary>
	public class StandaloneBlobWriterTests {
		static readonly ModuleDefMD module = ModuleDefMD.Load(Fixtures.Fixtures.Features.Image);

		// What a blob writer needs to know: the token of a type in the module that the blob is written for,
		// and whether a type name in a custom attribute needs its assembly
		sealed class Helper : ICustomAttributeWriterHelper, ISignatureWriterHelper {
			public List<string> Errors { get; } = new List<string>();

			public void Error(string message) => Errors.Add(message);

			// The compiler writes the assembly of every type that isn't in the module. If this is true, a type
			// of the core library has no assembly either (the runtime looks for a type there too).
			public bool ShortNamesOfCoreLibraryTypes { get; set; }

			public bool MustUseAssemblyName(IType? type) => FullNameFactory.MustUseAssemblyName(module, type, allowCorlib: ShortNamesOfCoreLibraryTypes);

			public uint ToEncodedToken(ITypeDefOrRef typeDefOrRef) {
				Assert.True(CodedToken.TypeDefOrRef.Encode(typeDefOrRef.MDToken, out uint encodedToken));
				return encodedToken;
			}
		}

		public static TheoryData<Table, string> SignatureColumns => new TheoryData<Table, string> {
			{ Table.Field, "Signature" },
			{ Table.Method, "Signature" },
			{ Table.MemberRef, "Signature" },
			{ Table.StandAloneSig, "Signature" },
			{ Table.Property, "Type" },
			{ Table.MethodSpec, "Instantiation" },
		};

		static uint ReadCell(Table table, uint rid, string columnName) {
			var mdTable = module.TablesStream.Get(table)!;
			for (int i = 0; i < mdTable.Columns.Count; i++) {
				if (mdTable.Columns[i].Name == columnName) {
					Assert.True(module.TablesStream.TryReadColumn(mdTable, rid, i, out uint value));
					return value;
				}
			}
			Assert.Fail($"{table} has no column {columnName}");
			return 0;
		}

		[Theory, MemberData(nameof(SignatureColumns))]
		public void SignatureIsWrittenAsItWasRead(Table table, string column) {
			uint rows = module.TablesStream.Get(table)!.Rows;
			Assert.True(rows > 0);
			for (uint rid = 1; rid <= rows; rid++) {
				uint offset = ReadCell(table, rid, column);
				var signature = module.ReadSignature(offset);
				Assert.NotNull(signature);
				var helper = new Helper();

				var written = SignatureWriter.Write(helper, signature);

				Assert.Empty(helper.Errors);
				Assert.True(module.BlobStream.Read(offset)!.SequenceEqual(written), $"{table} row {rid}: {signature}");
			}
		}

		[Fact]
		public void TypeSignatureIsWrittenAsItWasRead() {
			uint rows = module.TablesStream.TypeSpecTable.Rows;
			Assert.True(rows > 0);
			for (uint rid = 1; rid <= rows; rid++) {
				uint offset = ReadCell(Table.TypeSpec, rid, "Signature");
				var signature = module.ReadTypeSignature(offset);
				Assert.NotNull(signature);
				var helper = new Helper();

				var written = SignatureWriter.Write(helper, signature);

				Assert.Empty(helper.Errors);
				Assert.True(module.BlobStream.Read(offset)!.SequenceEqual(written), $"TypeSpec row {rid}: {signature}");
			}
		}

		// The writer reports the error and writes a type so that the blob is valid
		[Fact]
		public void SignatureThatIsNullIsAnError() {
			var helper = new Helper();

			Assert.Equal(new byte[] { (byte)ElementType.Boolean }, SignatureWriter.Write(helper, (TypeSig?)null));
			Assert.Equal(new[] { "TypeSig is null" }, helper.Errors);
			SignatureWriter.Write(helper, (CallingConventionSig?)null);
			Assert.Equal(2, helper.Errors.Count);
		}

		[Fact]
		public void CustomAttributeIsWrittenAsItWasRead() {
			uint rows = module.TablesStream.CustomAttributeTable.Rows;
			Assert.True(rows > 20);
			for (uint rid = 1; rid <= rows; rid++) {
				var attribute = module.ReadCustomAttribute(rid);
				Assert.NotNull(attribute);
				var helper = new Helper();

				var written = CustomAttributeWriter.Write(helper, attribute);

				Assert.Empty(helper.Errors);
				var original = module.ReadBlob(new MDToken(Table.CustomAttribute, rid).Raw)!;
				Assert.True(original.SequenceEqual(written), $"CustomAttribute row {rid}: {attribute.TypeFullName}");
			}
		}

		// An attribute whose argument is typeof(List<>): a type argument is written as the name of the type
		[Fact]
		public void TypeNameInACustomAttributeHasItsAssemblyIfTheHelperSaysSo() {
			var attribute = module.GetTypes().SelectMany(a => a.CustomAttributes).First(a =>
				a.ConstructorArguments.Count == 1 && a.ConstructorArguments[0].Value is TypeSig type && type.TypeName == "List`1");
			string coreLibrary = module.CorLibTypes.AssemblyRef.FullName;

			string withAssembly = System.Text.Encoding.UTF8.GetString(CustomAttributeWriter.Write(new Helper(), attribute));
			string withoutAssembly = System.Text.Encoding.UTF8.GetString(CustomAttributeWriter.Write(new Helper { ShortNamesOfCoreLibraryTypes = true }, attribute));

			Assert.Contains("System.Collections.Generic.List`1, " + coreLibrary, withAssembly);
			Assert.Contains("System.Collections.Generic.List`1", withoutAssembly);
			Assert.DoesNotContain(coreLibrary, withoutAssembly);
		}

		[Fact]
		public void CustomAttributeThatIsNullIsAnEmptyBlob() {
			var helper = new Helper();

			Assert.Empty(CustomAttributeWriter.Write(helper, (CustomAttribute?)null));
		}

		// The owner of a FieldMarshal row is a field or a parameter
		[Fact]
		public void MarshalTypeIsWrittenAsItWasRead() {
			uint rows = module.TablesStream.FieldMarshalTable.Rows;
			Assert.True(rows > 0);
			for (uint rid = 1; rid <= rows; rid++) {
				Assert.True(module.TablesStream.TryReadFieldMarshalRow(rid, out var row));
				Assert.True(CodedToken.HasFieldMarshal.Decode(row.Parent, out MDToken parent));
				var owner = Assert.IsAssignableFrom<IHasFieldMarshal>(module.ResolveToken(parent));
				Assert.NotNull(owner.MarshalType);
				var helper = new Helper();

				var written = MarshalBlobWriter.Write(module, owner.MarshalType, helper);

				Assert.Empty(helper.Errors);
				Assert.True(module.BlobStream.Read(row.NativeType)!.SequenceEqual(written), $"FieldMarshal row {rid}: {owner.MarshalType}");
			}
			Assert.Null(MarshalBlobWriter.Write(module, null, new Helper()));
		}

		[Fact]
		public void SecurityAttributesAreWrittenAsTheyWereRead() {
			uint rows = module.TablesStream.DeclSecurityTable.Rows;
			Assert.True(rows > 0);
			for (uint rid = 1; rid <= rows; rid++) {
				var declSecurity = module.ResolveDeclSecurity(rid)!;
				Assert.NotEmpty(declSecurity.SecurityAttributes);
				var helper = new Helper();

				var written = DeclSecurityWriter.Write(module, declSecurity.SecurityAttributes, helper);

				Assert.Empty(helper.Errors);
				Assert.True(module.ReadBlob(declSecurity.MDToken.Raw)!.SequenceEqual(written), $"DeclSecurity row {rid}");
			}
		}
	}
}
