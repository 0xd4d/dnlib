using System;
using System.Collections.Generic;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	/// <summary>
	/// Flags used by <see cref="AssemblyNameComparer"/>
	/// </summary>
	[Flags]
	enum AssemblyNameComparerFlags {
		/// <summary>
		/// Compare assembly simple name
		/// </summary>
		Name = 1,

		/// <summary>
		/// Compare assembly version
		/// </summary>
		Version = 2,

		/// <summary>
		/// Compare assembly public key token
		/// </summary>
		PublicKeyToken = 4,

		/// <summary>
		/// Compare assembly locale
		/// </summary>
		Locale = 8,

		/// <summary>
		/// Compare assembly simple name, version, public key token and locale
		/// </summary>
		All = 0xF,
	}

	/// <summary>
	/// Compares two assembly names
	/// </summary>
	struct AssemblyNameComparer : IEqualityComparer<AssemblyNameInfo> {
		AssemblyNameComparerFlags flags;

		/// <summary>
		/// Gets/sets the comparison flags
		/// </summary>
		public AssemblyNameComparerFlags Flags {
			get { return flags; }
			set { flags = value; }
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyNameComparerFlags.Name"/> bit
		/// </summary>
		public bool CompareName {
			get { return (flags & AssemblyNameComparerFlags.Name) != 0; }
			set {
				if (value)
					flags |= AssemblyNameComparerFlags.Name;
				else
					flags &= ~AssemblyNameComparerFlags.Name;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyNameComparerFlags.Version"/> bit
		/// </summary>
		public bool CompareVersion {
			get { return (flags & AssemblyNameComparerFlags.Version) != 0; }
			set {
				if (value)
					flags |= AssemblyNameComparerFlags.Version;
				else
					flags &= ~AssemblyNameComparerFlags.Version;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyNameComparerFlags.PublicKeyToken"/> bit
		/// </summary>
		public bool ComparePublicKeyToken {
			get { return (flags & AssemblyNameComparerFlags.PublicKeyToken) != 0; }
			set {
				if (value)
					flags |= AssemblyNameComparerFlags.PublicKeyToken;
				else
					flags &= ~AssemblyNameComparerFlags.PublicKeyToken;
			}
		}

		/// <summary>
		/// Gets/sets the <see cref="AssemblyNameComparerFlags.Locale"/> bit
		/// </summary>
		public bool CompareLocale {
			get { return (flags & AssemblyNameComparerFlags.Locale) != 0; }
			set {
				if (value)
					flags |= AssemblyNameComparerFlags.Locale;
				else
					flags &= ~AssemblyNameComparerFlags.Locale;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="flags">Comparison flags</param>
		public AssemblyNameComparer(AssemblyNameComparerFlags flags) {
			this.flags = flags;
		}

		/// <summary>
		/// Compares two assembly names
		/// </summary>
		/// <param name="a">First</param>
		/// <param name="b">Second</param>
		/// <returns>&lt; 0 if a &lt; b, 0 if a == b, &gt; 0 if a &gt; b</returns>
		public int CompareTo(AssemblyNameInfo a, AssemblyNameInfo b) {
			if (a == b)
				return 0;
			if (a == null)
				return -1;
			if (b == null)
				return 1;

			int v;

			if (CompareName && (v = UTF8String.CaseInsensitiveCompareTo(a.Name, b.Name)) != 0)
				return v;
			if (CompareVersion && (v = Utils.CompareTo(a.Version, b.Version)) != 0)
				return v;
			if (ComparePublicKeyToken && (v = PublicKeyBase.TokenCompareTo(a.PublicKeyOrToken, b.PublicKeyOrToken)) != 0)
				return v;
			if (CompareLocale && (v = Utils.LocaleCompareTo(a.Locale, b.Locale)) != 0)
				return v;

			return 0;
		}

		/// <summary>
		/// Compares two assembly names
		/// </summary>
		/// <param name="a">First</param>
		/// <param name="b">Second</param>
		/// <returns><c>true</c> if equal, <c>false</c> otherwise</returns>
		public bool Equals(AssemblyNameInfo a, AssemblyNameInfo b) {
			return CompareTo(a, b) == 0;
		}

		/// <summary>
		/// Figures out which of two assembly names is closer to another assembly name
		/// </summary>
		/// <param name="requested">Requested assembly name</param>
		/// <param name="a">First</param>
		/// <param name="b">Second</param>
		/// <returns>-1 if both are equally close, 0 if a is closest, 1 if b is closest</returns>
		public int CompareClosest(AssemblyNameInfo requested, AssemblyNameInfo a, AssemblyNameInfo b) {
			if (a == b)
				return 0;
			if (a == null)
				return 1;
			if (b == null)
				return 0;

			// Compare the most important parts first:
			//	1. Assembly simple name
			//	2. Public key token
			//	3. Version
			//	4. Locale

			if (CompareName) {
				// If the name only matches one of a or b, return that one.
				bool na = UTF8String.CaseInsensitiveCompareTo(requested.Name, a.Name) == 0;
				bool nb = UTF8String.CaseInsensitiveCompareTo(requested.Name, b.Name) == 0;
				if (na && !nb)
					return 0;
				if (!na && nb)
					return 1;
				if (!na && !nb)
					return -1;
			}

			if (ComparePublicKeyToken) {
				bool pa, pb;
				if (requested.PublicKeyOrToken == null) {
					// If one of them has a pkt but the other one hasn't, return the one with
					// no pkt.
					pa = a.PublicKeyOrToken == null;
					pb = b.PublicKeyOrToken == null;
				}
				else {
					// If one of them has the correct pkt, but the other one has an incorrect
					// pkt, return the one with the correct pkt.
					pa = PublicKeyBase.TokenCompareTo(requested.PublicKeyOrToken, a.PublicKeyOrToken) == 0;
					pb = PublicKeyBase.TokenCompareTo(requested.PublicKeyOrToken, b.PublicKeyOrToken) == 0;
				}
				if (pa && !pb)
					return 0;
				if (!pa && pb)
					return 1;
			}

			if (CompareVersion && Utils.CompareTo(a.Version, b.Version) != 0) {
				var rv = Utils.CreateVersionWithNoUndefinedValues(requested.Version);
				if (rv == new Version(0, 0, 0, 0))
					rv = new Version(ushort.MaxValue, ushort.MaxValue, ushort.MaxValue, ushort.MaxValue);
				int va = Utils.CompareTo(a.Version, rv);
				int vb = Utils.CompareTo(b.Version, rv);
				if (va == 0)
					return 0;	// vb != 0 so return 0
				if (vb == 0)
					return 1;	// va != 0 so return 1
				if (va > 0 && vb < 0)
					return 0;
				if (va < 0 && vb > 0)
					return 1;
				// Now either both a and b's version > req version or both are < req version
				if (va > 0) {
					// a.Version and b.Version > req.Version. Pick the one that is closest.
					return Utils.CompareTo(a.Version, b.Version) < 0 ? 0 : 1;
				}
				else {
					// a.Version and b.Version < req.Version. Pick the one that is closest.
					return Utils.CompareTo(a.Version, b.Version) > 0 ? 0 : 1;
				}
			}

			if (CompareLocale) {
				bool la = Utils.LocaleCompareTo(requested.Locale, a.Locale) == 0;
				bool lb = Utils.LocaleCompareTo(requested.Locale, b.Locale) == 0;
				if (la && !lb)
					return 0;
				if (!la && lb)
					return 1;
			}

			return -1;
		}

		/// <summary>
		/// Gets the hash code of an assembly name
		/// </summary>
		/// <param name="a">Assembly name</param>
		/// <returns>The hash code</returns>
		public int GetHashCode(AssemblyNameInfo a) {
			if (a == null)
				return 0;

			int hash = 0;

			if (CompareName)
				hash += UTF8String.GetHashCode(a.Name);
			if (CompareVersion)
				hash += Utils.CreateVersionWithNoUndefinedValues(a.Version).GetHashCode();
			if (ComparePublicKeyToken)
				hash += PublicKeyBase.GetHashCodeToken(a.PublicKeyOrToken);
			if (CompareLocale)
				hash += Utils.GetHashCodeLocale(a.Locale);

			return hash;
		}
	}
}
