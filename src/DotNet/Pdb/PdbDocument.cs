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
		IList<PdbCustomDebugInfo> customDebugInfos;

		/// <summary>
		/// Default constructor
		/// </summary>
		public PdbDocument() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="symDoc">A <see cref="SymbolDocument"/> instance</param>
		public PdbDocument(SymbolDocument symDoc) : this(symDoc, partial: false) {
		}

		PdbDocument(SymbolDocument symDoc, bool partial) {
			if (symDoc is null)
				throw new ArgumentNullException(nameof(symDoc));
			Url = symDoc.URL;
			if (!partial)
				Initialize(symDoc);
		}

		internal static PdbDocument CreatePartialForCompare(SymbolDocument symDoc) =>
			new PdbDocument(symDoc, partial: true);

		internal void Initialize(SymbolDocument symDoc) {
			Language = symDoc.Language;
			LanguageVendor = symDoc.LanguageVendor;
			DocumentType = symDoc.DocumentType;
			CheckSumAlgorithmId = symDoc.CheckSumAlgorithmId;
			CheckSum = symDoc.CheckSum;
			customDebugInfos = new List<PdbCustomDebugInfo>();
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
		public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Url ?? string.Empty);

		/// <inheritdoc/>
		public override bool Equals(object obj) {
			var other = obj as PdbDocument;
			if (other is null)
				return false;
			return StringComparer.OrdinalIgnoreCase.Equals(Url ?? string.Empty, other.Url ?? string.Empty);
		}
	}
}
