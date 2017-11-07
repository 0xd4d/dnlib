// dnlib: See LICENSE.txt for more info

using System;

namespace dnlib.DotNet.Pdb {
	/// <summary>
	/// PDB document constants
	/// </summary>
	public static class PdbDocumentConstants {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public static readonly Guid LanguageCSharp = new Guid("3F5162F8-07C6-11D3-9053-00C04FA302A1");
		public static readonly Guid LanguageVisualBasic = new Guid("3A12D0B8-C26C-11D0-B442-00A0244A1DD2");
		public static readonly Guid LanguageFSharp = new Guid("AB4F38C9-B6E6-43BA-BE3B-58080B2CCCE3");

		public static readonly Guid HashSHA1 = new Guid("FF1816EC-AA5E-4D10-87F7-6F4963833460");
		public static readonly Guid HashSHA256 = new Guid("8829D00F-11B8-4213-878B-770E8597AC16");

		public static readonly Guid LanguageVendorMicrosoft = new Guid("994B45C4-E6E9-11D2-903F-00C04FA302A1");

		public static readonly Guid DocumentTypeText = new Guid("5A869D0B-6611-11D3-BD2A-0000F80849BD");
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
