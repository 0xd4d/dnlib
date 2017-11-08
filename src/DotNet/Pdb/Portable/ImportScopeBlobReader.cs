// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using dnlib.DotNet.MD;
using dnlib.IO;

namespace dnlib.DotNet.Pdb.Portable {
	// https://github.com/dotnet/corefx/blob/master/src/System.Reflection.Metadata/specs/PortablePdb-Metadata.md#imports-blob
	struct ImportScopeBlobReader {
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
			using (var stream = blobStream.CreateStream(imports)) {
				while (stream.Position < stream.Length) {
					var kind = ImportDefinitionKindUtils.ToPdbImportDefinitionKind(stream.ReadCompressedUInt32());
					string targetNamespace, alias;
					AssemblyRef targetAssembly;
					PdbImport import;
					ITypeDefOrRef targetType;
					switch (kind) {
					case PdbImportDefinitionKind.ImportNamespace:
						// <import> ::= ImportNamespace <target-namespace>
						targetNamespace = ReadUTF8(stream.ReadCompressedUInt32());
						import = new PdbImportNamespace(targetNamespace);
						break;

					case PdbImportDefinitionKind.ImportAssemblyNamespace:
						// <import> ::= ImportAssemblyNamespace <target-assembly> <target-namespace>
						targetAssembly = TryReadAssemblyRef(stream.ReadCompressedUInt32());
						targetNamespace = ReadUTF8(stream.ReadCompressedUInt32());
						import = new PdbImportAssemblyNamespace(targetAssembly, targetNamespace);
						break;

					case PdbImportDefinitionKind.ImportType:
						// <import> ::= ImportType <target-type>
						targetType = TryReadType(stream.ReadCompressedUInt32());
						import = new PdbImportType(targetType);
						break;

					case PdbImportDefinitionKind.ImportXmlNamespace:
						// <import> ::= ImportXmlNamespace <alias> <target-namespace>
						alias = ReadUTF8(stream.ReadCompressedUInt32());
						targetNamespace = ReadUTF8(stream.ReadCompressedUInt32());
						import = new PdbImportXmlNamespace(alias, targetNamespace);
						break;

					case PdbImportDefinitionKind.ImportAssemblyReferenceAlias:
						// <import> ::= ImportReferenceAlias <alias>
						alias = ReadUTF8(stream.ReadCompressedUInt32());
						import = new PdbImportAssemblyReferenceAlias(alias);
						break;

					case PdbImportDefinitionKind.AliasAssemblyReference:
						// <import> ::= AliasAssemblyReference <alias> <target-assembly>
						alias = ReadUTF8(stream.ReadCompressedUInt32());
						targetAssembly = TryReadAssemblyRef(stream.ReadCompressedUInt32());
						import = new PdbAliasAssemblyReference(alias, targetAssembly);
						break;

					case PdbImportDefinitionKind.AliasNamespace:
						// <import> ::= AliasNamespace <alias> <target-namespace>
						alias = ReadUTF8(stream.ReadCompressedUInt32());
						targetNamespace = ReadUTF8(stream.ReadCompressedUInt32());
						import = new PdbAliasNamespace(alias, targetNamespace);
						break;

					case PdbImportDefinitionKind.AliasAssemblyNamespace:
						// <import> ::= AliasAssemblyNamespace <alias> <target-assembly> <target-namespace>
						alias = ReadUTF8(stream.ReadCompressedUInt32());
						targetAssembly = TryReadAssemblyRef(stream.ReadCompressedUInt32());
						targetNamespace = ReadUTF8(stream.ReadCompressedUInt32());
						import = new PdbAliasAssemblyNamespace(alias, targetAssembly, targetNamespace);
						break;

					case PdbImportDefinitionKind.AliasType:
						// <import> ::= AliasType <alias> <target-type>
						alias = ReadUTF8(stream.ReadCompressedUInt32());
						targetType = TryReadType(stream.ReadCompressedUInt32());
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
					if (import != null)
						result.Add(import);
				}
				Debug.Assert(stream.Position == stream.Length);
			}
		}

		ITypeDefOrRef TryReadType(uint codedToken) {
			uint token;
			bool b = CodedToken.TypeDefOrRef.Decode(codedToken, out token);
			Debug.Assert(b);
			if (!b)
				return null;
			var type = module.ResolveToken(token) as ITypeDefOrRef;
			Debug.Assert(type != null);
			return type;
		}

		AssemblyRef TryReadAssemblyRef(uint rid) {
			var asmRef = module.ResolveToken(0x23000000 + rid) as AssemblyRef;
			Debug.Assert(asmRef != null);
			return asmRef;
		}

		string ReadUTF8(uint offset) {
			var bytes = blobStream.ReadNoNull(offset);
			return Encoding.UTF8.GetString(bytes);
		}
	}
}
