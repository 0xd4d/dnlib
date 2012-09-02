using System;

namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the Assembly table
	/// </summary>
	public class AssemblyDef : IHasCustomAttribute, IHasDeclSecurity {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// From column Assembly.HashAlgId
		/// </summary>
		uint hashAlgId;

		/// <summary>
		/// From columns Assembly.MajorVersion, Assembly.MinorVersion, Assembly.BuildNumber,
		/// Assembly.RevisionNumber
		/// </summary>
		Version version;

		/// <summary>
		/// From column Assembly.Flags
		/// </summary>
		uint flags;

		/// <summary>
		/// From column Assembly.PublicKey
		/// </summary>
		byte[] publicKey;

		/// <summary>
		/// From column Assembly.Name
		/// </summary>
		string name;

		/// <summary>
		/// From column Assembly.Locale
		/// </summary>
		string locale;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.Assembly, rid); }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 14; }
		}

		/// <inheritdoc/>
		public int HasDeclSecurityTag {
			get { return 2; }
		}
	}
}
