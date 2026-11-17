// dnlib: See LICENSE.txt for more info

#nullable enable

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
		/// Gets the Metadata token of the document if available.
		/// </summary>
		public MDToken? MDToken { get; internal set; }

		/// <summary>
		/// Set by the <see cref="PdbState"/> that created this document from its PDB file. It's used
		/// to find the document again if it's removed and then added back to that <see cref="PdbState"/>.
		/// It must not reference the <see cref="PdbState"/>, the reader or the module.
		/// </summary>
		internal object? readerOrigin;

		/// <summary>
		/// Default constructor
		/// </summary>
		public PdbDocument() {
			Url = string.Empty;
			CheckSum = Array2.Empty<byte>();
			customDebugInfos = new List<PdbCustomDebugInfo>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="symDoc">A <see cref="SymbolDocument"/> instance</param>
		public PdbDocument(SymbolDocument symDoc) {
			if (symDoc is null)
				throw new ArgumentNullException(nameof(symDoc));
			Url = symDoc.URL;
			Language = symDoc.Language;
			CheckSumAlgorithmId = symDoc.CheckSumAlgorithmId;
			CheckSum = symDoc.CheckSum;
			LanguageVendor = symDoc.LanguageVendor;
			DocumentType = symDoc.DocumentType;
			customDebugInfos = new List<PdbCustomDebugInfo>();
			foreach (var cdi in symDoc.CustomDebugInfos)
				customDebugInfos.Add(cdi);
			MDToken = symDoc.MDToken;
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
			customDebugInfos = new List<PdbCustomDebugInfo>();
		}

		/// <summary>
		/// Gets the hash code of the properties compared by <see cref="Equals(object)"/>
		/// </summary>
		/// <remarks>The hash code changes when one of those properties is changed. Don't change them
		/// while the document is a key in your own dictionary or hash set. It's safe to change them
		/// while the document is in a <see cref="PdbState"/>.</remarks>
		public override int GetHashCode() {
			unchecked {
				int hash = StringComparer.Ordinal.GetHashCode(Url ?? string.Empty);
				hash += CheckSumAlgorithmId.GetHashCode();
				hash += Utils.GetHashCode(CheckSum);
				hash += Language.GetHashCode();
				return hash;
			}
		}

		/// <summary>
		/// Compares <see cref="Url"/>, <see cref="Language"/>, <see cref="CheckSumAlgorithmId"/> and
		/// <see cref="CheckSum"/>. Equal documents are written as one document to the PDB file.
		/// </summary>
		public override bool Equals(object? obj) {
			var other = obj as PdbDocument;
			if (other is null)
				return false;
			return StringComparer.Ordinal.Equals(Url ?? string.Empty, other.Url ?? string.Empty)
				&& CheckSumAlgorithmId == other.CheckSumAlgorithmId
				&& CheckSumEquals(CheckSum, other.CheckSum)
				&& Language == other.Language;
		}

		// Treat null and empty the same to be consistent with other code
		static bool CheckSumEquals(byte[]? a, byte[]? b) {
			if ((a is null || a.Length == 0) && (b is null || b.Length == 0))
				return true;
			return Utils.Equals(a, b);
		}
	}
}
