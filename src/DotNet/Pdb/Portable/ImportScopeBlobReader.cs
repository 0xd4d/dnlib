// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using dnlib.DotNet.MD;
using dnlib.IO;

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
			int count = result.Count;
			try {
				Read(ref reader, result);
			}
			catch (IOException) {
				// A corrupt blob loses all of its imports
				for (int i = result.Count - 1; i >= count; i--)
					result.RemoveAt(i);
			}
		}

		void Read(ref DataReader reader, IList<PdbImport> result) {
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
					Debug.Fail($"Unknown import definition kind: {kind}");
					import = null;
					break;
				}
				if (import is not null)
					result.Add(import);
			}
			Debug.Assert(reader.Position == reader.Length);
		}

		/// <summary>
		/// Gets the number of bytes that <see cref="Read(uint, IList{PdbImport})"/> parses: the imports blob
		/// and every string blob it uses, counted once per use. Many imports can use the same string blob,
		/// so this can be much bigger than the blob heap.
		/// </summary>
		/// <param name="imports">Imports blob offset</param>
		public long GetSize(uint imports) {
			if (imports == 0)
				return 0;
			if (!blobStream.TryCreateReader(imports, out var reader))
				return 0;
			long size = reader.Length;
			try {
				while (reader.Position < reader.Length) {
					var kind = ImportDefinitionKindUtils.ToPdbImportDefinitionKind(reader.ReadCompressedUInt32());
					switch (kind) {
					case PdbImportDefinitionKind.ImportNamespace:
					case PdbImportDefinitionKind.ImportAssemblyReferenceAlias:
						size += GetUTF8Size(reader.ReadCompressedUInt32());
						break;

					case PdbImportDefinitionKind.ImportAssemblyNamespace:
						reader.ReadCompressedUInt32();
						size += GetUTF8Size(reader.ReadCompressedUInt32());
						break;

					case PdbImportDefinitionKind.ImportType:
						reader.ReadCompressedUInt32();
						break;

					case PdbImportDefinitionKind.ImportXmlNamespace:
					case PdbImportDefinitionKind.AliasNamespace:
						size += GetUTF8Size(reader.ReadCompressedUInt32());
						size += GetUTF8Size(reader.ReadCompressedUInt32());
						break;

					case PdbImportDefinitionKind.AliasAssemblyReference:
					case PdbImportDefinitionKind.AliasType:
						size += GetUTF8Size(reader.ReadCompressedUInt32());
						reader.ReadCompressedUInt32();
						break;

					case PdbImportDefinitionKind.AliasAssemblyNamespace:
						size += GetUTF8Size(reader.ReadCompressedUInt32());
						reader.ReadCompressedUInt32();
						size += GetUTF8Size(reader.ReadCompressedUInt32());
						break;
					}
				}
			}
			catch (IOException) {
				// Read() stops here too, after creating the imports before this point
			}
			return size;
		}

		long GetUTF8Size(uint offset) => blobStream.TryCreateReader(offset, out var reader) ? reader.Length : 0;

		ITypeDefOrRef TryReadType(uint codedToken) {
			bool b = CodedToken.TypeDefOrRef.Decode(codedToken, out uint token);
			Debug.Assert(b);
			if (!b)
				return null;
			var type = module.ResolveToken(token) as ITypeDefOrRef;
			Debug.Assert(type is not null);
			return type;
		}

		AssemblyRef TryReadAssemblyRef(uint rid) {
			var asmRef = module.ResolveToken(0x23000000 + rid) as AssemblyRef;
			Debug.Assert(asmRef is not null);
			return asmRef;
		}

		string ReadUTF8(uint offset) {
			if (!blobStream.TryCreateReader(offset, out var reader))
				return string.Empty;
			return reader.ReadUtf8String((int)reader.BytesLeft);
		}
	}
}
