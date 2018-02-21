// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.IO;
using System.Text;
using dnlib.DotNet.Writer;

namespace dnlib.DotNet.Pdb.Portable {
	// https://github.com/dotnet/corefx/blob/master/src/System.Reflection.Metadata/specs/PortablePdb-Metadata.md#imports-blob
	struct ImportScopeBlobWriter {
		readonly IWriterError helper;
		readonly MetaData systemMetaData;
		readonly BlobHeap blobHeap;

		ImportScopeBlobWriter(IWriterError helper, MetaData systemMetaData, BlobHeap blobHeap) {
			this.helper = helper;
			this.systemMetaData = systemMetaData;
			this.blobHeap = blobHeap;
		}

		public static void Write(IWriterError helper, MetaData systemMetaData, BinaryWriter writer, BlobHeap blobHeap, IList<PdbImport> imports) {
			var blobWriter = new ImportScopeBlobWriter(helper, systemMetaData, blobHeap);
			blobWriter.Write(writer, imports);
		}

		uint WriteUTF8(string s) {
			if (s == null) {
				helper.Error("String is null");
				s = string.Empty;
			}
			var bytes = Encoding.UTF8.GetBytes(s);
			return blobHeap.Add(bytes);
		}

		void Write(BinaryWriter writer, IList<PdbImport> imports) {
			foreach (var import in imports) {
				uint rawKind;
				if (!ImportDefinitionKindUtils.ToImportDefinitionKind(import.Kind, out rawKind)) {
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
					writer.WriteCompressedUInt32(systemMetaData.GetToken(((PdbImportAssemblyNamespace)import).TargetAssembly).Rid);
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
					writer.WriteCompressedUInt32(systemMetaData.GetToken(((PdbAliasAssemblyReference)import).TargetAssembly).Rid);
					break;

				case PdbImportDefinitionKind.AliasNamespace:
					// <import> ::= AliasNamespace <alias> <target-namespace>
					writer.WriteCompressedUInt32(WriteUTF8(((PdbAliasNamespace)import).Alias));
					writer.WriteCompressedUInt32(WriteUTF8(((PdbAliasNamespace)import).TargetNamespace));
					break;

				case PdbImportDefinitionKind.AliasAssemblyNamespace:
					// <import> ::= AliasAssemblyNamespace <alias> <target-assembly> <target-namespace>
					writer.WriteCompressedUInt32(WriteUTF8(((PdbAliasAssemblyNamespace)import).Alias));
					writer.WriteCompressedUInt32(systemMetaData.GetToken(((PdbAliasAssemblyNamespace)import).TargetAssembly).Rid);
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
			if (tdr == null) {
				helper.Error("ITypeDefOrRef is null");
				return 0;
			}
			var token = systemMetaData.GetToken(tdr);
			uint codedToken;
			if (MD.CodedToken.TypeDefOrRef.Encode(token, out codedToken))
				return codedToken;
			helper.Error(string.Format("Could not encode token 0x{0:X8}", token.Raw));
			return 0;
		}
	}
}
