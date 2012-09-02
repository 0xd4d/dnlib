using System;

namespace dot10.dotNET.Types {
	/// <summary>
	/// A high-level representation of a row in the Assembly table
	/// </summary>
	public abstract class AssemblyDef : IHasCustomAttribute, IHasDeclSecurity {
		/// <summary>
		/// The row id in its table
		/// </summary>
		protected uint rid;

		/// <summary>
		/// The manifest module
		/// </summary>
		protected ModuleDef manifestModule;

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

		/// <summary>
		/// From column Assembly.HashAlgId
		/// </summary>
		public abstract AssemblyHashAlgorithm HashAlgId { get; set; }

		/// <summary>
		/// From columns Assembly.MajorVersion, Assembly.MinorVersion, Assembly.BuildNumber,
		/// Assembly.RevisionNumber.
		/// </summary>
		/// <exception cref="ArgumentNullException">If <paramref name="value"/> is null</exception>
		public abstract Version Version { get; set; }

		/// <summary>
		/// From column Assembly.Flags
		/// </summary>
		public abstract AssemblyFlags Flags { get; set; }

		/// <summary>
		/// From column Assembly.PublicKey
		/// </summary>
		public abstract byte[] PublicKey { get; set; }

		/// <summary>
		/// From column Assembly.Name
		/// </summary>
		public abstract UTF8String Name { get; set; }

		/// <summary>
		/// From column Assembly.Locale
		/// </summary>
		public abstract UTF8String Locale { get; set; }

		/// <summary>
		/// Gets/sets the manifest module
		/// </summary>
		public ModuleDef ManifestModule {
			get { return manifestModule; }
			set { manifestModule = value; }
		}
	}

	/// <summary>
	/// An Assembly row created by the user and not present in the original .NET file
	/// </summary>
	public class AssemblyDefUser : AssemblyDef {
		AssemblyHashAlgorithm hashAlgId;
		Version version;
		AssemblyFlags flags;
		byte[] publicKey;
		UTF8String name;
		UTF8String locale;

		/// <inheritdoc/>
		public override AssemblyHashAlgorithm HashAlgId {
			get { return hashAlgId; }
			set { hashAlgId = value; }
		}

		/// <inheritdoc/>
		public override Version Version {
			get { return version; }
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				version = value;
			}
		}

		/// <inheritdoc/>
		public override AssemblyFlags Flags {
			get { return flags; }
			set { flags = value; }
		}

		/// <inheritdoc/>
		public override byte[] PublicKey {
			get { return publicKey; }
			set { publicKey = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Name {
			get { return name; }
			set { name = value; }
		}

		/// <inheritdoc/>
		public override UTF8String Locale {
			get { return locale; }
			set { locale = value; }
		}
	}
}
