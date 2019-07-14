// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Text;
using dnlib.DotNet.Writer;

namespace dnlib.DotNet.Pdb.Portable {
	// https://github.com/dotnet/corefx/blob/master/src/System.Reflection.Metadata/specs/PortablePdb-Metadata.md#imports-blob
	readonly struct ImportScopeBlobWriter {
		readonly IWriterError helper;
		readonly Metadata systemMetadata;
		readonly BlobHeap blobHeap;

		ImportScopeBlobWriter(IWriterError helper, Metadata systemMetadata, BlobHeap blobHeap) {
			this.helper = helper;
			this.systemMetadata = systemMetadata;
			this.blobHeap = blobHeap;
		}

		public static void Write(IWriterError helper, Metadata systemMetadata, DataWriter writer, BlobHeap blobHeap, IList<PdbImport> imports) {
			var blobWriter = new ImportScopeBlobWriter(helper, systemMetadata, blobHeap);
			blobWriter.Write(writer, imports);
		}

		uint WriteUTF8(string s) {
			if (s is null) {
				helper.Error("String is null");
				s = string.Empty;
			}
			var bytes = Encoding.UTF8.GetBytes(s);
			return blobHeap.Add(bytes);
		}

		void Write(DataWriter writer, IList<PdbImport> imports) {
			int count = imports.Count;
			for (int i = 0; i < count; i++) {
				var import = imports[i];
				if (!ImportDefinitionKindUtils.ToImportDefinitionKind(import.Kind, out uint rawKind)) {
					helper.Error("Unknown import definition kind: " + import.Kind.ToString());
					return;
				}
				writer.WriteCompressedUInt32(rawKind);
				switch (import.Kind) {
				case PdbImportDefinitionKind.ImportNamespace:
					// <import> ::= ImportNamespace <target-namespace>
					writer.WriteCompressedUInt32(WriteUTF8(((PdbImportNamespace)import).TargetNamespace));
					break;

				case PdbImportDefinitionKind.ImportAssemblyNamespace:
					// <import> ::= ImportAssemblyNamespace <target-assembly> <target-namespace>
					writer.WriteCompressedUInt32(systemMetadata.GetToken(((PdbImportAssemblyNamespace)import).TargetAssembly).Rid);
					writer.WriteCompressedUInt32(WriteUTF8(((PdbImportAssemblyNamespace)import).TargetNamespace));
					break;

				case PdbImportDefinitionKind.ImportType:
					// <import> ::= ImportType <target-type>
					writer.WriteCompressedUInt32(GetTypeDefOrRefEncodedToken(((PdbImportType)import).TargetType));
					break;

				case PdbImportDefinitionKind.ImportXmlNamespace:
					// <import> ::= ImportXmlNamespace <alias> <target-namespace>
					writer.WriteCompressedUInt32(WriteUTF8(((PdbImportXmlNamespace)import).Alias));
					writer.WriteCompressedUInt32(WriteUTF8(((PdbImportXmlNamespace)import).TargetNamespace));
					break;

				case PdbImportDefinitionKind.ImportAssemblyReferenceAlias:
					// <import> ::= ImportReferenceAlias <alias>
					writer.WriteCompressedUInt32(WriteUTF8(((PdbImportAssemblyReferenceAlias)import).Alias));
					break;

				case PdbImportDefinitionKind.AliasAssemblyReference:
					// <import> ::= AliasAssemblyReference <alias> <target-assembly>
					writer.WriteCompressedUInt32(WriteUTF8(((PdbAliasAssemblyReference)import).Alias));
					writer.WriteCompressedUInt32(systemMetadata.GetToken(((PdbAliasAssemblyReference)import).TargetAssembly).Rid);
					break;

				case PdbImportDefinitionKind.AliasNamespace:
					// <import> ::= AliasNamespace <alias> <target-namespace>
					writer.WriteCompressedUInt32(WriteUTF8(((PdbAliasNamespace)import).Alias));
					writer.WriteCompressedUInt32(WriteUTF8(((PdbAliasNamespace)import).TargetNamespace));
					break;

				case PdbImportDefinitionKind.AliasAssemblyNamespace:
					// <import> ::= AliasAssemblyNamespace <alias> <target-assembly> <target-namespace>
					writer.WriteCompressedUInt32(WriteUTF8(((PdbAliasAssemblyNamespace)import).Alias));
					writer.WriteCompressedUInt32(systemMetadata.GetToken(((PdbAliasAssemblyNamespace)import).TargetAssembly).Rid);
					writer.WriteCompressedUInt32(WriteUTF8(((PdbAliasAssemblyNamespace)import).TargetNamespace));
					break;

				case PdbImportDefinitionKind.AliasType:
					// <import> ::= AliasType <alias> <target-type>
					writer.WriteCompressedUInt32(WriteUTF8(((PdbAliasType)import).Alias));
					writer.WriteCompressedUInt32(GetTypeDefOrRefEncodedToken(((PdbAliasType)import).TargetType));
					break;

				default:
					helper.Error("Unknown import definition kind: " + import.Kind.ToString());
					return;
				}
			}
		}

		uint GetTypeDefOrRefEncodedToken(ITypeDefOrRef tdr) {
			if (tdr is null) {
				helper.Error("ITypeDefOrRef is null");
				return 0;
			}
			var token = systemMetadata.GetToken(tdr);
			if (MD.CodedToken.TypeDefOrRef.Encode(token, out uint codedToken))
				return codedToken;
			helper.Error($"Could not encode token 0x{token.Raw:X8}");
			return 0;
		}
	}
}
