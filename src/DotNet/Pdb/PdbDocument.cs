// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.DotNet.Pdb.Symbols;

namespace dnlib.DotNet.Pdb {
	/// <summary>
	/// A PDB document
	/// </summary>
	[DebuggerDisplay("{Url}")]
	public sealed class PdbDocument : IHasCustomDebugInformation {
		/// <summary>
		/// Gets/sets the document URL
		/// </summary>
		public string Url { get; set; }

		/// <summary>
		/// Gets/sets the language GUID. See <see cref="PdbDocumentConstants"/>
		/// </summary>
		public Guid Language { get; set; }

		/// <summary>
		/// Gets/sets the language vendor GUID. See <see cref="PdbDocumentConstants"/>
		/// </summary>
		public Guid LanguageVendor { get; set; }

		/// <summary>
		/// Gets/sets the document type GUID. See <see cref="PdbDocumentConstants"/>
		/// </summary>
		public Guid DocumentType { get; set; }

		/// <summary>
		/// Gets/sets the checksum algorithm ID. See <see cref="PdbDocumentConstants"/>
		/// </summary>
		public Guid CheckSumAlgorithmId { get; set; }

		/// <summary>
		/// Gets/sets the checksum
		/// </summary>
		public byte[] CheckSum { get; set; }

		/// <inheritdoc/>
		public int HasCustomDebugInformationTag => 22;

		/// <inheritdoc/>
		public bool HasCustomDebugInfos => CustomDebugInfos.Count > 0;

		/// <summary>
		/// Gets all custom debug infos
		/// </summary>
		public IList<PdbCustomDebugInfo> CustomDebugInfos => customDebugInfos;
		readonly IList<PdbCustomDebugInfo> customDebugInfos = new List<PdbCustomDebugInfo>();

		/// <summary>
		/// Default constructor
		/// </summary>
		public PdbDocument() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="symDoc">A <see cref="SymbolDocument"/> instance</param>
		public PdbDocument(SymbolDocument symDoc) {
			if (symDoc == null)
				throw new ArgumentNullException(nameof(symDoc));
			Url = symDoc.URL;
			Language = symDoc.Language;
			LanguageVendor = symDoc.LanguageVendor;
			DocumentType = symDoc.DocumentType;
			CheckSumAlgorithmId = symDoc.CheckSumAlgorithmId;
			CheckSum = symDoc.CheckSum;
			foreach (var cdi in symDoc.CustomDebugInfos)
				customDebugInfos.Add(cdi);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="url">Document URL</param>
		/// <param name="language">Language. See <see cref="PdbDocumentConstants"/></param>
		/// <param name="languageVendor">Language vendor. See <see cref="PdbDocumentConstants"/></param>
		/// <param name="documentType">Document type. See <see cref="PdbDocumentConstants"/></param>
		/// <param name="checkSumAlgorithmId">Checksum algorithm ID. See <see cref="PdbDocumentConstants"/></param>
		/// <param name="checkSum">Checksum</param>
		public PdbDocument(string url, Guid language, Guid languageVendor, Guid documentType, Guid checkSumAlgorithmId, byte[] checkSum) {
			Url = url;
			Language = language;
			LanguageVendor = languageVendor;
			DocumentType = documentType;
			CheckSumAlgorithmId = checkSumAlgorithmId;
			CheckSum = checkSum;
		}

		/// <inheritdoc/>
		public override int GetHashCode() => (Url ?? string.Empty).ToUpperInvariant().GetHashCode();

		/// <inheritdoc/>
		public override bool Equals(object obj) {
			var other = obj as PdbDocument;
			if (other == null)
				return false;
			return (Url ?? string.Empty).Equals(other.Url ?? string.Empty, StringComparison.OrdinalIgnoreCase);
		}
	}
}
