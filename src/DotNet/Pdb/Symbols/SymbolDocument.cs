// dnlib: See LICENSE.txt for more info

using System;

namespace dnlib.DotNet.Pdb.Symbols {
	/// <summary>
	/// A document
	/// </summary>
	public abstract class SymbolDocument {
		/// <summary>
		/// Gets the URL
		/// </summary>
		public abstract string URL { get; }

		/// <summary>
		/// Gets the language
		/// </summary>
		public abstract Guid Language { get; }

		/// <summary>
		/// Gets the language vendor
		/// </summary>
		public abstract Guid LanguageVendor { get; }

		/// <summary>
		/// Gets the document type
		/// </summary>
		public abstract Guid DocumentType { get; }

		/// <summary>
		/// Gets the checksum algorithm id
		/// </summary>
		public abstract Guid CheckSumAlgorithmId { get; }

		/// <summary>
		/// Gets the checksum
		/// </summary>
		public abstract byte[] CheckSum { get; }

		/// <summary>
		/// Gets the custom debug infos
		/// </summary>
		public abstract PdbCustomDebugInfo[] CustomDebugInfos { get; }
	}
}
