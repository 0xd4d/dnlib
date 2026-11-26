// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using dnlib.DotNet.Pdb;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Pdb {
	/// <summary>
	/// Scopes and imports of a portable PDB with values that the format doesn't allow. The module is still
	/// read with the PDB, only the invalid part is ignored.
	/// </summary>
	public class InvalidScopeAndImportTests {
		static readonly Guid stateMachineHoistedLocalScopes = new Guid("6DA9A61E-F8C7-4874-BE62-68BC5630DF71");

		// The method of the module has 4 bytes of code
		const int CodeSize = 4;

		static MetadataBuilder CreateMetadata() {
			var metadata = new MetadataBuilder();
			metadata.AddMethodDebugInformation(default, default);
			return metadata;
		}

		static void AddScope(MetadataBuilder metadata, uint start, uint length, ImportScopeHandle importScope = default) =>
			metadata.AddLocalScope(MetadataTokens.MethodDefinitionHandle(1), importScope, MetadataTokens.LocalVariableHandle(1),
				MetadataTokens.LocalConstantHandle(1), unchecked((int)start), unchecked((int)length));

		static BlobHandle AddUtf8Blob(MetadataBuilder metadata, string text) => metadata.GetOrAddBlobUTF8(text);

		// The kinds of imports are 1-9. The values of another kind aren't known, so the imports that follow it
		// can't be read. System.Reflection.Metadata returns the imports before it and then throws.
		// Fixed in a02b98d7: dnlib read the bytes after the unknown kind as new imports.
		[Theory]
		[InlineData(0)]
		[InlineData(10)]
		[InlineData(0x1FFFFFFF)]
		public void ImportsAfterAnImportOfAnUnknownKindAreNotRead(int unknownKind) {
			var metadata = CreateMetadata();
			var imports = new BlobBuilder();
			imports.WriteCompressedInteger(1); // imports a namespace
			imports.WriteCompressedInteger(MetadataTokens.GetHeapOffset(AddUtf8Blob(metadata, "First")));
			imports.WriteCompressedInteger(unknownKind);
			imports.WriteCompressedInteger(1);
			imports.WriteCompressedInteger(MetadataTokens.GetHeapOffset(AddUtf8Blob(metadata, "Second")));
			AddScope(metadata, 0, CodeSize, metadata.AddImportScope(default, metadata.GetOrAddBlob(imports)));
			var pdb = RawPortablePdb.Write(metadata);

			using (var module = RoundTrip.LoadWithPdb(RawPortablePdb.Image, pdb)) {
				var import = Assert.Single(RawPortablePdb.GetMethod(module).Body!.PdbMethod!.Scope!.ImportScope!.Imports);
				Assert.Equal("First", Assert.IsType<PdbImportNamespace>(import).TargetNamespace);
			}
		}

		// The start and the end of a scope are in [0, 0x80000000). System.Reflection.Metadata returns them as
		// negative numbers if they aren't.
		// Fixed in af20934b: dnlib did the same, and it added a scope around all scopes of the method,
		// since the valid scope didn't contain the invalid ones.
		[Theory]
		[InlineData(1U, 0x7FFFFFFFU)]
		[InlineData(0x7FFFFFFFU, 1U)]
		[InlineData(0x80000000U, 0U)]
		[InlineData(0x80000000U, 1U)]
		[InlineData(0xFFFFFFFFU, 1U)]
		[InlineData(0xFFFFFFFFU, 0xFFFFFFFFU)]
		public void ScopeThatIsOutsideOfTheRangeOfOffsetsIsIgnored(uint start, uint length) {
			var metadata = CreateMetadata();
			// System.Reflection.Metadata sorts the scopes as if the start were an int
			if ((int)start < 0)
				AddScope(metadata, start, length);
			AddScope(metadata, 0, CodeSize);
			if ((int)start >= 0)
				AddScope(metadata, start, length);
			var pdb = RawPortablePdb.Write(metadata);

			using (var module = RoundTrip.LoadWithPdb(RawPortablePdb.Image, pdb)) {
				var body = RawPortablePdb.GetMethod(module).Body!;
				var root = body.PdbMethod!.Scope!;
				Assert.Same(body.Instructions[0], root.Start);
				Assert.Null(root.End);
				Assert.Empty(root.Scopes);
			}
		}

		// The biggest valid scope ends at 0x7FFFFFFF. It isn't in the first scope, so dnlib adds a scope
		// around both.
		[Fact]
		public void ScopeThatEndsAtTheBiggestOffsetIsRead() {
			var metadata = CreateMetadata();
			AddScope(metadata, 0, CodeSize);
			AddScope(metadata, 1, 0x7FFFFFFE);
			var pdb = RawPortablePdb.Write(metadata);

			using (var module = RoundTrip.LoadWithPdb(RawPortablePdb.Image, pdb)) {
				var body = RawPortablePdb.GetMethod(module).Body!;
				var scopes = body.PdbMethod!.Scope!.Scopes;
				Assert.Equal(2, scopes.Count);
				Assert.Same(body.Instructions[0], scopes[0].Start);
				Assert.Same(body.Instructions[1], scopes[1].Start);
			}
		}

		// The same range for the scopes of the locals that are fields of a state machine
		// Fixed in af20934b: the end of the scope was the sum of two uints, which could be any offset
		[Theory]
		[InlineData(1U, 2U, true)]
		[InlineData(1U, 0x7FFFFFFEU, true)]
		[InlineData(1U, 0x7FFFFFFFU, false)]
		[InlineData(1U, 0xFFFFFFFFU, false)]
		[InlineData(2U, 0xFFFFFFFFU, false)]
		public void HoistedLocalScopeThatIsOutsideOfTheRangeOfOffsetsIsIgnored(uint start, uint length, bool valid) {
			var metadata = CreateMetadata();
			var scopes = new BlobBuilder();
			scopes.WriteUInt32(start);
			scopes.WriteUInt32(length);
			metadata.AddCustomDebugInformation(MetadataTokens.MethodDefinitionHandle(1), metadata.GetOrAddGuid(stateMachineHoistedLocalScopes), metadata.GetOrAddBlob(scopes));
			var pdb = RawPortablePdb.Write(metadata);

			using (var module = RoundTrip.LoadWithPdb(RawPortablePdb.Image, pdb)) {
				var method = RawPortablePdb.GetMethod(module);
				var body = method.Body!;
				var infos = method.CustomDebugInfos.OfType<PdbStateMachineHoistedLocalScopesCustomDebugInfo>().ToArray();
				if (valid) {
					var scope = Assert.Single(Assert.Single(infos).Scopes);
					Assert.Same(body.Instructions[1], scope.Start);
					Assert.Same(start + length == 3 ? body.Instructions[3] : null, scope.End);
				}
				else
					Assert.Empty(infos);
			}
		}
	}
}
