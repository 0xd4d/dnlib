#pragma warning disable 0169	//TODO:

using System;

namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the AssemblyRef table
	/// </summary>
	public class AssemblyRef : IHasCustomAttribute, IImplementation, IResolutionScope {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// From columns AssemblyRef.MajorVersion, AssemblyRef.MinorVersion,
		/// AssemblyRef.BuildNumber, AssemblyRef.RevisionNumber
		/// </summary>
		Version version;

		/// <summary>
		/// From column AssemblyRef.Flags
		/// </summary>
		uint Flags;

		/// <summary>
		/// From column AssemblyRef.PublicKeyOrToken
		/// </summary>
		byte[] publicKeyOrToken;

		/// <summary>
		/// From column AssemblyRef.Name
		/// </summary>
		string name;

		/// <summary>
		/// From column AssemblyRef.Locale
		/// </summary>
		string locale;

		/// <summary>
		/// From column AssemblyRef.HashValue
		/// </summary>
		byte[] hashValue;

		/// <inheritdoc/>
		public MDToken MDToken {
			get { return new MDToken(Table.AssemblyRef, rid); }
		}

		/// <inheritdoc/>
		public int HasCustomAttributeTag {
			get { return 15; }
		}

		/// <inheritdoc/>
		public int ImplementationTag {
			get { return 1; }
		}

		/// <inheritdoc/>
		public int ResolutionScopeTag {
			get { return 2; }
		}
	}
}
