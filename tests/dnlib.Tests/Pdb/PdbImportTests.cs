// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Pdb;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Pdb {
	/// <summary>
	/// The imports of a scope: the <c>using</c> directives (C#) and <c>Imports</c> statements (Visual Basic)
	/// that the debugger needs to understand an expression. A portable PDB has nine kinds. The tests create
	/// one of each with the API, write a portable PDB, and read it with dnlib and with
	/// System.Reflection.Metadata.
	/// </summary>
	public class PdbImportTests {
		// Replaces the imports of the method Sum by one import of every kind, in a new scope whose parent
		// is the scope the compiler created
		static CompiledAssembly WriteWithEveryKindOfImport() {
			using (var module = DebugInfoSource.Load()) {
				var coreLibrary = module.CorLibTypes.AssemblyRef;
				var type = module.CorLibTypes.GetTypeRef("System", "Console");
				var scope = DebugInfoSource.GetRootScope(module, "Sum");
				var imports = new PdbImportScope { Parent = scope.ImportScope };
				imports.Imports.Add(new PdbImportNamespace("Imported.Namespace"));
				imports.Imports.Add(new PdbImportAssemblyNamespace(coreLibrary, "Assembly.Namespace"));
				imports.Imports.Add(new PdbImportType(type));
				imports.Imports.Add(new PdbImportXmlNamespace("xml", "http://example.com/xml"));
				imports.Imports.Add(new PdbImportAssemblyReferenceAlias("ExternAlias"));
				imports.Imports.Add(new PdbAliasAssemblyReference("AssemblyAlias", coreLibrary));
				imports.Imports.Add(new PdbAliasNamespace("NamespaceAlias", "Aliased.Namespace"));
				imports.Imports.Add(new PdbAliasAssemblyNamespace("AssemblyNamespaceAlias", coreLibrary, "Aliased.Assembly.Namespace"));
				imports.Imports.Add(new PdbAliasType("TypeAlias", type));
				scope.ImportScope = imports;
				return RoundTrip.WriteWithPortablePdb(module, preserveMetadata: true);
			}
		}

		[Fact]
		public void EveryKindOfImportIsWrittenAndRead() {
			var written = WriteWithEveryKindOfImport();

			using (var module = RoundTrip.LoadWithPdb(written.Image, written.Pdb!)) {
				var scope = DebugInfoSource.GetRootScope(module, "Sum").ImportScope!;
				var imports = scope.Imports;
				Assert.Equal(new[] {
					"ImportNamespace", "ImportAssemblyNamespace", "ImportType", "ImportXmlNamespace", "ImportAssemblyReferenceAlias",
					"AliasAssemblyReference", "AliasNamespace", "AliasAssemblyNamespace", "AliasType",
				}, imports.Select(a => a.Kind.ToString()));
				string coreLibraryName = module.CorLibTypes.AssemblyRef.Name;

				Assert.Equal("Imported.Namespace", ((PdbImportNamespace)imports[0]).TargetNamespace);

				var assemblyNamespace = (PdbImportAssemblyNamespace)imports[1];
				Assert.Equal(coreLibraryName, assemblyNamespace.TargetAssembly!.Name);
				Assert.Equal("Assembly.Namespace", assemblyNamespace.TargetNamespace);

				Assert.Equal("System.Console", ((PdbImportType)imports[2]).TargetType!.FullName);

				var xmlNamespace = (PdbImportXmlNamespace)imports[3];
				Assert.Equal("xml", xmlNamespace.Alias);
				Assert.Equal("http://example.com/xml", xmlNamespace.TargetNamespace);

				Assert.Equal("ExternAlias", ((PdbImportAssemblyReferenceAlias)imports[4]).Alias);

				var assemblyAlias = (PdbAliasAssemblyReference)imports[5];
				Assert.Equal("AssemblyAlias", assemblyAlias.Alias);
				Assert.Equal(coreLibraryName, assemblyAlias.TargetAssembly!.Name);

				var namespaceAlias = (PdbAliasNamespace)imports[6];
				Assert.Equal("NamespaceAlias", namespaceAlias.Alias);
				Assert.Equal("Aliased.Namespace", namespaceAlias.TargetNamespace);

				var assemblyNamespaceAlias = (PdbAliasAssemblyNamespace)imports[7];
				Assert.Equal("AssemblyNamespaceAlias", assemblyNamespaceAlias.Alias);
				Assert.Equal(coreLibraryName, assemblyNamespaceAlias.TargetAssembly!.Name);
				Assert.Equal("Aliased.Assembly.Namespace", assemblyNamespaceAlias.TargetNamespace);

				var typeAlias = (PdbAliasType)imports[8];
				Assert.Equal("TypeAlias", typeAlias.Alias);
				Assert.Equal("System.Console", typeAlias.TargetType!.FullName);

				// The scope the compiler created is the parent, the other methods still use it
				Assert.NotNull(scope.Parent);
				Assert.Same(scope.Parent, DebugInfoSource.GetRootScope(module, "NoLocals").ImportScope);
			}
		}

		// The same file read with System.Reflection.Metadata
		[Fact]
		public void WrittenImportsAreReadBySystemReflectionMetadata() {
			var written = WriteWithEveryKindOfImport();

			using (var provider = MetadataReaderProvider.FromPortablePdbImage(ImmutableArray.Create(written.Pdb!))) {
				var reader = provider.GetMetadataReader();
				var scope = reader.ImportScopes.Select(reader.GetImportScope).Single(a => a.GetImports().Count() == 9);
				var texts = new List<string>();
				foreach (var import in scope.GetImports()) {
					string alias = import.Alias.IsNil ? "" : Encoding.UTF8.GetString(reader.GetBlobBytes(import.Alias));
					bool hasType = import.Kind == ImportDefinitionKind.ImportType || import.Kind == ImportDefinitionKind.AliasType;
					// Reading a property that the kind of import doesn't have throws
					bool hasNamespace = import.Kind.ToString().EndsWith("Namespace");
					string target = hasNamespace ? Encoding.UTF8.GetString(reader.GetBlobBytes(import.TargetNamespace)) : "";
					texts.Add($"{import.Kind} alias='{alias}' namespace='{target}' assembly={!import.TargetAssembly.IsNil} type={hasType && !import.TargetType.IsNil}");
				}

				Assert.Equal(new[] {
					"ImportNamespace alias='' namespace='Imported.Namespace' assembly=False type=False",
					"ImportAssemblyNamespace alias='' namespace='Assembly.Namespace' assembly=True type=False",
					"ImportType alias='' namespace='' assembly=False type=True",
					"ImportXmlNamespace alias='xml' namespace='http://example.com/xml' assembly=False type=False",
					"ImportAssemblyReferenceAlias alias='ExternAlias' namespace='' assembly=False type=False",
					"AliasAssemblyReference alias='AssemblyAlias' namespace='' assembly=True type=False",
					"AliasNamespace alias='NamespaceAlias' namespace='Aliased.Namespace' assembly=False type=False",
					"AliasAssemblyNamespace alias='AssemblyNamespaceAlias' namespace='Aliased.Assembly.Namespace' assembly=True type=False",
					"AliasType alias='TypeAlias' namespace='' assembly=False type=True",
				}, texts);
				Assert.False(scope.Parent.IsNil);
			}
		}

		// The constructors without arguments create an import whose properties are set later
		[Fact]
		public void ImportsHaveTheirKind() {
			Assert.Equal(PdbImportDefinitionKind.ImportNamespace, new PdbImportNamespace().Kind);
			Assert.Equal(PdbImportDefinitionKind.ImportAssemblyNamespace, new PdbImportAssemblyNamespace().Kind);
			Assert.Equal(PdbImportDefinitionKind.ImportType, new PdbImportType().Kind);
			Assert.Equal(PdbImportDefinitionKind.ImportXmlNamespace, new PdbImportXmlNamespace().Kind);
			Assert.Equal(PdbImportDefinitionKind.ImportAssemblyReferenceAlias, new PdbImportAssemblyReferenceAlias().Kind);
			Assert.Equal(PdbImportDefinitionKind.AliasAssemblyReference, new PdbAliasAssemblyReference().Kind);
			Assert.Equal(PdbImportDefinitionKind.AliasNamespace, new PdbAliasNamespace().Kind);
			Assert.Equal(PdbImportDefinitionKind.AliasAssemblyNamespace, new PdbAliasAssemblyNamespace().Kind);
			Assert.Equal(PdbImportDefinitionKind.AliasType, new PdbAliasType().Kind);

			var scope = new PdbImportScope();
			Assert.False(scope.HasImports);
			Assert.False(scope.HasCustomDebugInfos);
			Assert.Null(scope.Parent);
		}
	}
}
