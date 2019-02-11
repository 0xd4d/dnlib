// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Diagnostics;

namespace dnlib.DotNet.Pdb {
	/// <summary>
	/// Import scope
	/// </summary>
	public sealed class PdbImportScope : IHasCustomDebugInformation {
		readonly IList<PdbImport> imports = new List<PdbImport>();

		/// <summary>
		/// Constructor
		/// </summary>
		public PdbImportScope() {
		}

		/// <summary>
		/// Gets/sets the parent import scope
		/// </summary>
		public PdbImportScope Parent { get; set; }

		/// <summary>
		/// Gets all imports
		/// </summary>
		public IList<PdbImport> Imports => imports;

		/// <summary>
		/// <c>true</c> if <see cref="Imports"/> is not empty
		/// </summary>
		public bool HasImports => imports.Count > 0;

		/// <inheritdoc/>
		public int HasCustomDebugInformationTag => 26;

		/// <inheritdoc/>
		public bool HasCustomDebugInfos => CustomDebugInfos.Count > 0;

		/// <summary>
		/// Gets all custom debug infos
		/// </summary>
		public IList<PdbCustomDebugInfo> CustomDebugInfos => customDebugInfos;
		readonly IList<PdbCustomDebugInfo> customDebugInfos = new List<PdbCustomDebugInfo>();
	}

	/// <summary>
	/// Import kind
	/// </summary>
	public enum PdbImportDefinitionKind {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		ImportNamespace,
		ImportAssemblyNamespace,
		ImportType,
		ImportXmlNamespace,
		ImportAssemblyReferenceAlias,
		AliasAssemblyReference,
		AliasNamespace,
		AliasAssemblyNamespace,
		AliasType,
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}

	/// <summary>
	/// PDB import base class
	/// </summary>
	public abstract class PdbImport {
		/// <summary>
		/// Gets the import kind
		/// </summary>
		public abstract PdbImportDefinitionKind Kind { get; }

		internal abstract void PreventNewClasses();
	}

	/// <summary>
	/// Import namespace
	/// </summary>
	[DebuggerDisplay("{GetDebuggerString(),nq}")]
	public sealed class PdbImportNamespace : PdbImport {
		/// <summary>
		/// Returns <see cref="PdbImportDefinitionKind.ImportNamespace"/>
		/// </summary>
		public sealed override PdbImportDefinitionKind Kind => PdbImportDefinitionKind.ImportNamespace;

		/// <summary>
		/// Gets the target namespace
		/// </summary>
		public string TargetNamespace { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public PdbImportNamespace() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="targetNamespace"></param>
		public PdbImportNamespace(string targetNamespace) => TargetNamespace = targetNamespace;

		internal sealed override void PreventNewClasses() { }

		string GetDebuggerString() => $"{Kind}: {TargetNamespace}";
	}

	/// <summary>
	/// Import assembly, namespace
	/// </summary>
	[DebuggerDisplay("{GetDebuggerString(),nq}")]
	public sealed class PdbImportAssemblyNamespace : PdbImport {
		/// <summary>
		/// Returns <see cref="PdbImportDefinitionKind.ImportAssemblyNamespace"/>
		/// </summary>
		public sealed override PdbImportDefinitionKind Kind => PdbImportDefinitionKind.ImportAssemblyNamespace;

		/// <summary>
		/// Gets the target assembly
		/// </summary>
		public AssemblyRef TargetAssembly { get; set; }

		/// <summary>
		/// Gets the target namespace
		/// </summary>
		public string TargetNamespace { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public PdbImportAssemblyNamespace() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="targetAssembly"></param>
		/// <param name="targetNamespace"></param>
		public PdbImportAssemblyNamespace(AssemblyRef targetAssembly, string targetNamespace) {
			TargetAssembly = targetAssembly;
			TargetNamespace = targetNamespace;
		}

		internal sealed override void PreventNewClasses() { }

		string GetDebuggerString() => $"{Kind}: {TargetAssembly} {TargetNamespace}";
	}

	/// <summary>
	/// Import type
	/// </summary>
	[DebuggerDisplay("{GetDebuggerString(),nq}")]
	public sealed class PdbImportType : PdbImport {
		/// <summary>
		/// Returns <see cref="PdbImportDefinitionKind.ImportType"/>
		/// </summary>
		public sealed override PdbImportDefinitionKind Kind => PdbImportDefinitionKind.ImportType;

		/// <summary>
		/// Gets the target type
		/// </summary>
		public ITypeDefOrRef TargetType { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public PdbImportType() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="targetType"></param>
		public PdbImportType(ITypeDefOrRef targetType) => TargetType = targetType;

		internal sealed override void PreventNewClasses() { }

		string GetDebuggerString() => $"{Kind}: {TargetType}";
	}

	/// <summary>
	/// Import xml namespace
	/// </summary>
	[DebuggerDisplay("{GetDebuggerString(),nq}")]
	public sealed class PdbImportXmlNamespace : PdbImport {
		/// <summary>
		/// Returns <see cref="PdbImportDefinitionKind.ImportXmlNamespace"/>
		/// </summary>
		public sealed override PdbImportDefinitionKind Kind => PdbImportDefinitionKind.ImportXmlNamespace;

		/// <summary>
		/// Gets the alias
		/// </summary>
		public string Alias { get; set; }

		/// <summary>
		/// Gets the target namespace
		/// </summary>
		public string TargetNamespace { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public PdbImportXmlNamespace() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="alias"></param>
		/// <param name="targetNamespace"></param>
		public PdbImportXmlNamespace(string alias, string targetNamespace) {
			Alias = alias;
			TargetNamespace = targetNamespace;
		}

		internal sealed override void PreventNewClasses() { }

		string GetDebuggerString() => $"{Kind}: {Alias} = {TargetNamespace}";
	}

	/// <summary>
	/// Import assembly reference alias
	/// </summary>
	[DebuggerDisplay("{GetDebuggerString(),nq}")]
	public sealed class PdbImportAssemblyReferenceAlias : PdbImport {
		/// <summary>
		/// Returns <see cref="PdbImportDefinitionKind.ImportAssemblyReferenceAlias"/>
		/// </summary>
		public sealed override PdbImportDefinitionKind Kind => PdbImportDefinitionKind.ImportAssemblyReferenceAlias;

		/// <summary>
		/// Gets the alias
		/// </summary>
		public string Alias { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public PdbImportAssemblyReferenceAlias() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="alias"></param>
		public PdbImportAssemblyReferenceAlias(string alias) => Alias = alias;

		internal sealed override void PreventNewClasses() { }

		string GetDebuggerString() => $"{Kind}: {Alias}";
	}

	/// <summary>
	/// Alias assembly reference
	/// </summary>
	[DebuggerDisplay("{GetDebuggerString(),nq}")]
	public sealed class PdbAliasAssemblyReference : PdbImport {
		/// <summary>
		/// Returns <see cref="PdbImportDefinitionKind.AliasAssemblyReference"/>
		/// </summary>
		public sealed override PdbImportDefinitionKind Kind => PdbImportDefinitionKind.AliasAssemblyReference;

		/// <summary>
		/// Gets the alias
		/// </summary>
		public string Alias { get; set; }

		/// <summary>
		/// Gets the target assembly
		/// </summary>
		public AssemblyRef TargetAssembly { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public PdbAliasAssemblyReference() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="alias"></param>
		/// <param name="targetAssembly"></param>
		public PdbAliasAssemblyReference(string alias, AssemblyRef targetAssembly) {
			Alias = alias;
			TargetAssembly = targetAssembly;
		}

		internal sealed override void PreventNewClasses() { }

		string GetDebuggerString() => $"{Kind}: {Alias} = {TargetAssembly}";
	}

	/// <summary>
	/// Alias namespace
	/// </summary>
	[DebuggerDisplay("{GetDebuggerString(),nq}")]
	public sealed class PdbAliasNamespace : PdbImport {
		/// <summary>
		/// Returns <see cref="PdbImportDefinitionKind.AliasNamespace"/>
		/// </summary>
		public sealed override PdbImportDefinitionKind Kind => PdbImportDefinitionKind.AliasNamespace;

		/// <summary>
		/// Gets the alias
		/// </summary>
		public string Alias { get; set; }

		/// <summary>
		/// Gets the target namespace
		/// </summary>
		public string TargetNamespace { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public PdbAliasNamespace() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="alias"></param>
		/// <param name="targetNamespace"></param>
		public PdbAliasNamespace(string alias, string targetNamespace) {
			Alias = alias;
			TargetNamespace = targetNamespace;
		}

		internal sealed override void PreventNewClasses() { }

		string GetDebuggerString() => $"{Kind}: {Alias} = {TargetNamespace}";
	}

	/// <summary>
	/// Alias assembly namespace
	/// </summary>
	[DebuggerDisplay("{GetDebuggerString(),nq}")]
	public sealed class PdbAliasAssemblyNamespace : PdbImport {
		/// <summary>
		/// Returns <see cref="PdbImportDefinitionKind.AliasAssemblyNamespace"/>
		/// </summary>
		public sealed override PdbImportDefinitionKind Kind => PdbImportDefinitionKind.AliasAssemblyNamespace;

		/// <summary>
		/// Gets the alias
		/// </summary>
		public string Alias { get; set; }

		/// <summary>
		/// Gets the target assembly
		/// </summary>
		public AssemblyRef TargetAssembly { get; set; }

		/// <summary>
		/// Gets the target namespace
		/// </summary>
		public string TargetNamespace { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public PdbAliasAssemblyNamespace() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="alias"></param>
		/// <param name="targetAssembly"></param>
		/// <param name="targetNamespace"></param>
		public PdbAliasAssemblyNamespace(string alias, AssemblyRef targetAssembly, string targetNamespace) {
			Alias = alias;
			TargetAssembly = targetAssembly;
			TargetNamespace = targetNamespace;
		}

		internal sealed override void PreventNewClasses() { }

		string GetDebuggerString() => $"{Kind}: {Alias} = {TargetAssembly} {TargetNamespace}";
	}

	/// <summary>
	/// Alias type
	/// </summary>
	[DebuggerDisplay("{GetDebuggerString(),nq}")]
	public sealed class PdbAliasType : PdbImport {
		/// <summary>
		/// Returns <see cref="PdbImportDefinitionKind.AliasType"/>
		/// </summary>
		public sealed override PdbImportDefinitionKind Kind => PdbImportDefinitionKind.AliasType;

		/// <summary>
		/// Gets the alias
		/// </summary>
		public string Alias { get; set; }

		/// <summary>
		/// Gets the target type
		/// </summary>
		public ITypeDefOrRef TargetType { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public PdbAliasType() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="alias"></param>
		/// <param name="targetType"></param>
		public PdbAliasType(string alias, ITypeDefOrRef targetType) {
			Alias = alias;
			TargetType = targetType;
		}

		internal sealed override void PreventNewClasses() { }

		string GetDebuggerString() => $"{Kind}: {Alias} = {TargetType}";
	}
}
