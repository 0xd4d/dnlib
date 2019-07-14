// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Diagnostics;
using dnlib.DotNet.MD;

namespace dnlib.DotNet.Pdb.Portable {
	// https://github.com/dotnet/corefx/blob/master/src/System.Reflection.Metadata/specs/PortablePdb-Metadata.md#imports-blob
	readonly struct ImportScopeBlobReader {
		readonly ModuleDef module;
		readonly BlobStream blobStream;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module that resolves assembly and type references</param>
		/// <param name="blobStream">Portable PDB blob stream</param>
		public ImportScopeBlobReader(ModuleDef module, BlobStream blobStream) {
			this.module = module;
			this.blobStream = blobStream;
		}

		public void Read(uint imports, IList<PdbImport> result) {
			if (imports == 0)
				return;
			if (!blobStream.TryCreateReader(imports, out var reader))
				return;
			while (reader.Position < reader.Length) {
				var kind = ImportDefinitionKindUtils.ToPdbImportDefinitionKind(reader.ReadCompressedUInt32());
				string targetNamespace, alias;
				AssemblyRef targetAssembly;
				PdbImport import;
				ITypeDefOrRef targetType;
				switch (kind) {
				case PdbImportDefinitionKind.ImportNamespace:
					// <import> ::= ImportNamespace <target-namespace>
					targetNamespace = ReadUTF8(reader.ReadCompressedUInt32());
					import = new PdbImportNamespace(targetNamespace);
					break;

				case PdbImportDefinitionKind.ImportAssemblyNamespace:
					// <import> ::= ImportAssemblyNamespace <target-assembly> <target-namespace>
					targetAssembly = TryReadAssemblyRef(reader.ReadCompressedUInt32());
					targetNamespace = ReadUTF8(reader.ReadCompressedUInt32());
					import = new PdbImportAssemblyNamespace(targetAssembly, targetNamespace);
					break;

				case PdbImportDefinitionKind.ImportType:
					// <import> ::= ImportType <target-type>
					targetType = TryReadType(reader.ReadCompressedUInt32());
					import = new PdbImportType(targetType);
					break;

				case PdbImportDefinitionKind.ImportXmlNamespace:
					// <import> ::= ImportXmlNamespace <alias> <target-namespace>
					alias = ReadUTF8(reader.ReadCompressedUInt32());
					targetNamespace = ReadUTF8(reader.ReadCompressedUInt32());
					import = new PdbImportXmlNamespace(alias, targetNamespace);
					break;

				case PdbImportDefinitionKind.ImportAssemblyReferenceAlias:
					// <import> ::= ImportReferenceAlias <alias>
					alias = ReadUTF8(reader.ReadCompressedUInt32());
					import = new PdbImportAssemblyReferenceAlias(alias);
					break;

				case PdbImportDefinitionKind.AliasAssemblyReference:
					// <import> ::= AliasAssemblyReference <alias> <target-assembly>
					alias = ReadUTF8(reader.ReadCompressedUInt32());
					targetAssembly = TryReadAssemblyRef(reader.ReadCompressedUInt32());
					import = new PdbAliasAssemblyReference(alias, targetAssembly);
					break;

				case PdbImportDefinitionKind.AliasNamespace:
					// <import> ::= AliasNamespace <alias> <target-namespace>
					alias = ReadUTF8(reader.ReadCompressedUInt32());
					targetNamespace = ReadUTF8(reader.ReadCompressedUInt32());
					import = new PdbAliasNamespace(alias, targetNamespace);
					break;

				case PdbImportDefinitionKind.AliasAssemblyNamespace:
					// <import> ::= AliasAssemblyNamespace <alias> <target-assembly> <target-namespace>
					alias = ReadUTF8(reader.ReadCompressedUInt32());
					targetAssembly = TryReadAssemblyRef(reader.ReadCompressedUInt32());
					targetNamespace = ReadUTF8(reader.ReadCompressedUInt32());
					import = new PdbAliasAssemblyNamespace(alias, targetAssembly, targetNamespace);
					break;

				case PdbImportDefinitionKind.AliasType:
					// <import> ::= AliasType <alias> <target-type>
					alias = ReadUTF8(reader.ReadCompressedUInt32());
					targetType = TryReadType(reader.ReadCompressedUInt32());
					import = new PdbAliasType(alias, targetType);
					break;

				case ImportDefinitionKindUtils.UNKNOWN_IMPORT_KIND:
					import = null;
					break;

				default:
					Debug.Fail("Unknown import definition kind: " + kind.ToString());
					import = null;
					break;
				}
				if (!(import is null))
					result.Add(import);
			}
			Debug.Assert(reader.Position == reader.Length);
		}

		ITypeDefOrRef TryReadType(uint codedToken) {
			bool b = CodedToken.TypeDefOrRef.Decode(codedToken, out uint token);
			Debug.Assert(b);
			if (!b)
				return null;
			var type = module.ResolveToken(token) as ITypeDefOrRef;
			Debug.Assert(!(type is null));
			return type;
		}

		AssemblyRef TryReadAssemblyRef(uint rid) {
			var asmRef = module.ResolveToken(0x23000000 + rid) as AssemblyRef;
			Debug.Assert(!(asmRef is null));
			return asmRef;
		}

		string ReadUTF8(uint offset) {
			if (!blobStream.TryCreateReader(offset, out var reader))
				return string.Empty;
			return reader.ReadUtf8String((int)reader.BytesLeft);
		}
	}
}
