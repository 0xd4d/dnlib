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
	/// <see cref="ModuleDefMD"/> returns the object of a metadata token (<c>ResolveToken()</c>), of a coded
	/// token (a row number and a few bits that say which table it's in, ECMA-335 II.24.2.6), and the
	/// signature or other blob of a row (<c>ReadBlob()</c>)
	/// </summary>
	public class TokenResolutionTests {
		static readonly ModuleDefMD module = ModuleDefMD.Load(Fixtures.Fixtures.Features.Image);

		// One method of ModuleDefMD for every kind of coded token
		static readonly Dictionary<CodedToken, Func<uint, object?>> resolvers = new Dictionary<CodedToken, Func<uint, object?>> {
			{ CodedToken.TypeDefOrRef, a => module.ResolveTypeDefOrRef(a) },
			{ CodedToken.HasConstant, a => module.ResolveHasConstant(a) },
			{ CodedToken.HasCustomAttribute, a => module.ResolveHasCustomAttribute(a) },
			{ CodedToken.HasFieldMarshal, a => module.ResolveHasFieldMarshal(a) },
			{ CodedToken.HasDeclSecurity, a => module.ResolveHasDeclSecurity(a) },
			{ CodedToken.MemberRefParent, a => module.ResolveMemberRefParent(a) },
			{ CodedToken.HasSemantic, a => module.ResolveHasSemantic(a) },
			{ CodedToken.MethodDefOrRef, a => module.ResolveMethodDefOrRef(a) },
			{ CodedToken.MemberForwarded, a => module.ResolveMemberForwarded(a) },
			{ CodedToken.Implementation, a => module.ResolveImplementation(a) },
			{ CodedToken.CustomAttributeType, a => module.ResolveCustomAttributeType(a) },
			{ CodedToken.ResolutionScope, a => module.ResolveResolutionScope(a) },
			{ CodedToken.TypeOrMethodDef, a => module.ResolveTypeOrMethodDef(a) },
		};

		[Fact]
		public void CodedTokenIsTheRowOfItsToken() {
			int checkedTokens = 0;
			foreach (var pair in resolvers) {
				var codedToken = pair.Key;
				foreach (var table in codedToken.TableTypes) {
					// Three of the five bit patterns of a CustomAttributeType aren't used. The list has table 0
					// (Module) for them.
					if (codedToken == CodedToken.CustomAttributeType && table == Table.Module)
						continue;
					// Not every table has rows
					var mdTable = module.TablesStream.Get(table);
					if (mdTable is null || mdTable.Rows == 0)
						continue;
					foreach (uint rid in new[] { 1U, mdTable.Rows }) {
						var token = new MDToken(table, rid);
						Assert.True(codedToken.Encode(token, out uint encoded));

						var resolved = pair.Value(encoded);

						// Not always the same object: a row whose signature can use a generic parameter (eg. an
						// InterfaceImpl or a MemberRef) gets a new object every time
						Assert.True(resolved is not null, $"Table {table}, row {rid}");
						Assert.Equal(module.ResolveToken(token)!.GetType(), resolved.GetType());
						Assert.Equal(token, ((IMDTokenProvider)resolved).MDToken);
						checkedTokens++;
					}
				}
			}
			Assert.True(checkedTokens > 40, $"Only {checkedTokens} tokens were checked");
		}

		[Fact]
		public void CodedTokenOfARowThatDoesNotExistIsNull() {
			foreach (var pair in resolvers) {
				var codedToken = pair.Key;
				var table = codedToken.TableTypes[0];
				uint rows = module.TablesStream.Get(table)!.Rows;

				Assert.True(codedToken.Encode(new MDToken(table, rows + 1), out uint afterLastRow));
				Assert.Null(pair.Value(afterLastRow));
				Assert.True(codedToken.Encode(new MDToken(table, 0), out uint rowZero));
				Assert.Null(pair.Value(rowZero));
			}
		}

		[Fact]
		public void TokenOfEveryTableWithObjectsIsResolved() {
			var tablesWithObjects = new[] {
				Table.Module, Table.TypeRef, Table.TypeDef, Table.Field, Table.Method, Table.Param, Table.InterfaceImpl,
				Table.MemberRef, Table.StandAloneSig, Table.Event, Table.Property, Table.ModuleRef, Table.TypeSpec,
				Table.Assembly, Table.AssemblyRef, Table.ExportedType, Table.GenericParam, Table.MethodSpec, Table.GenericParamConstraint,
			};
			foreach (var table in tablesWithObjects) {
				uint rows = module.TablesStream.Get(table)!.Rows;
				if (rows == 0)
					continue;

				var first = module.ResolveToken(new MDToken(table, 1));
				var last = module.ResolveToken(new MDToken(table, rows));

				Assert.True(first is not null && last is not null, $"Table {table}");
				Assert.Equal(new MDToken(table, 1), first.MDToken);
				Assert.Equal(new MDToken(table, rows), last.MDToken);
				Assert.Null(module.ResolveToken(new MDToken(table, rows + 1)));
				Assert.Null(module.ResolveToken(new MDToken(table, 0)));
			}
			// A table without an object for its rows
			Assert.Null(module.ResolveToken(new MDToken(Table.CustomAttribute, 1)));
		}

		// The same object every time, also from several kinds of tokens
		[Fact]
		public void DefinitionsAreResolvedToTheSameObject() {
			var type = module.Find("Features.Code", isReflectionName: true)!;
			var method = type.Methods[0];

			Assert.Same(type, module.ResolveToken(type.MDToken));
			Assert.Same(type, module.ResolveTypeDef(type.Rid));
			Assert.Same(method, module.ResolveToken(method.MDToken.Raw));
			Assert.Same(method, module.ResolveMethod(method.Rid));
		}

		[Fact]
		public void ReadBlobReturnsTheSignatureOfARow() {
			var type = module.Find("Features.Code", isReflectionName: true)!;
			var field = module.GetTypes().SelectMany(a => a.Fields).First();
			var method = type.Methods.First(a => a.MethodSig!.Params.Count == 0 && a.IsStatic);

			// A field signature starts with 6, a static method without generic parameters with 0
			Assert.Equal(6, module.ReadBlob(field.MDToken.Raw)![0]);
			Assert.Equal(new byte[] { 0, 0 }, module.ReadBlob(method.MDToken.Raw)!.Take(2));
			Assert.NotEmpty(module.ReadBlob(new MDToken(Table.MemberRef, 1).Raw)!);
			Assert.NotEmpty(module.ReadBlob(new MDToken(Table.CustomAttribute, 1).Raw)!);
			Assert.NotEmpty(module.ReadBlob(new MDToken(Table.StandAloneSig, 1).Raw)!);
			Assert.NotEmpty(module.ReadBlob(new MDToken(Table.TypeSpec, 1).Raw)!);
			Assert.NotEmpty(module.ReadBlob(new MDToken(Table.MethodSpec, 1).Raw)!);
			Assert.NotEmpty(module.ReadBlob(new MDToken(Table.Property, 1).Raw)!);
			Assert.NotEmpty(module.ReadBlob(new MDToken(Table.Constant, 1).Raw)!);
			// The assembly has no public key, the reference to the core library has a public key token
			Assert.Empty(module.ReadBlob(new MDToken(Table.Assembly, 1).Raw)!);
			Assert.Equal(8, module.ReadBlob(module.CorLibTypes.AssemblyRef.MDToken.Raw)!.Length);
		}

		[Fact]
		public void ReadBlobOfARowThatDoesNotExistIsNull() {
			Assert.Null(module.ReadBlob(new MDToken(Table.Field, 0).Raw));
			Assert.Null(module.ReadBlob(new MDToken(Table.Method, module.TablesStream.MethodTable.Rows + 1).Raw));
			// A table whose rows have no blob
			Assert.Null(module.ReadBlob(new MDToken(Table.TypeDef, 1).Raw));
		}

		// The same blobs as System.Reflection.Metadata reads
		[Fact]
		public void ReadBlobReturnsWhatAnotherReaderReturns() {
			using (var peReader = new System.Reflection.PortableExecutable.PEReader(System.Collections.Immutable.ImmutableArray.Create(Fixtures.Fixtures.Features.Image))) {
				var reader = System.Reflection.Metadata.PEReaderExtensions.GetMetadataReader(peReader);
				foreach (var handle in reader.MethodDefinitions) {
					int token = System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(handle);
					Assert.Equal(reader.GetBlobBytes(reader.GetMethodDefinition(handle).Signature), module.ReadBlob((uint)token));
				}
				foreach (var handle in reader.FieldDefinitions) {
					int token = System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(handle);
					Assert.Equal(reader.GetBlobBytes(reader.GetFieldDefinition(handle).Signature), module.ReadBlob((uint)token));
				}
				foreach (var handle in reader.MemberReferences) {
					int token = System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(handle);
					Assert.Equal(reader.GetBlobBytes(reader.GetMemberReference(handle).Signature), module.ReadBlob((uint)token));
				}
				foreach (var handle in reader.CustomAttributes) {
					int token = System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(handle);
					Assert.Equal(reader.GetBlobBytes(reader.GetCustomAttribute(handle).Value), module.ReadBlob((uint)token));
				}
			}
		}
	}
}
