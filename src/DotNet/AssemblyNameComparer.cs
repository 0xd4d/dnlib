// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Collections.Generic;

namespace dnlib.DotNet {
	/// <summary>
	/// Flags used by <see cref="AssemblyNameComparer"/>
	/// </summary>
	[Flags]
	public enum AssemblyNameComparerFlags {
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
		/// Compare assembly culture
		/// </summary>
		Culture = 8,

		/// <summary>
		/// Compare content type
		/// </summary>
		ContentType = 0x10,

		/// <summary>
		/// Compare assembly simple name, version, public key token, locale and content type
		/// </summary>
		All = Name | Version | PublicKeyToken | Culture | ContentType,
	}

	/// <summary>
	/// Compares two assembly names
	/// </summary>
	public struct AssemblyNameComparer : IEqualityComparer<IAssembly> {
		/// <summary>
		/// Compares the name, version, public key token, culture and content type
		/// </summary>
		public static AssemblyNameComparer CompareAll = new AssemblyNameComparer(AssemblyNameComparerFlags.All);

		/// <summary>
		/// Compares only the name and the public key token
		/// </summary>
		public static AssemblyNameComparer NameAndPublicKeyTokenOnly = new AssemblyNameComparer(AssemblyNameComparerFlags.Name | AssemblyNameComparerFlags.PublicKeyToken);

		/// <summary>
		/// Compares only the name
		/// </summary>
		public static AssemblyNameComparer NameOnly = new AssemblyNameComparer(AssemblyNameComparerFlags.Name);

		readonly AssemblyNameComparerFlags flags;

		/// <summary>
		/// Gets the <see cref="AssemblyNameComparerFlags.Name"/> bit
		/// </summary>
		public bool CompareName {
			get { return (flags & AssemblyNameComparerFlags.Name) != 0; }
		}

		/// <summary>
		/// Gets the <see cref="AssemblyNameComparerFlags.Version"/> bit
		/// </summary>
		public bool CompareVersion {
			get { return (flags & AssemblyNameComparerFlags.Version) != 0; }
		}

		/// <summary>
		/// Gets the <see cref="AssemblyNameComparerFlags.PublicKeyToken"/> bit
		/// </summary>
		public bool ComparePublicKeyToken {
			get { return (flags & AssemblyNameComparerFlags.PublicKeyToken) != 0; }
		}

		/// <summary>
		/// Gets the <see cref="AssemblyNameComparerFlags.Culture"/> bit
		/// </summary>
		public bool CompareCulture {
			get { return (flags & AssemblyNameComparerFlags.Culture) != 0; }
		}

		/// <summary>
		/// Gets the <see cref="AssemblyNameComparerFlags.ContentType"/> bit
		/// </summary>
		public bool CompareContentType {
			get { return (flags & AssemblyNameComparerFlags.ContentType) != 0; }
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
		public int CompareTo(IAssembly a, IAssembly b) {
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
			if (CompareCulture && (v = Utils.LocaleCompareTo(a.Culture, b.Culture)) != 0)
				return v;
			if (CompareContentType && (v = a.ContentType.CompareTo(b.ContentType)) != 0)
				return v;

			return 0;
		}

		/// <summary>
		/// Compares two assembly names
		/// </summary>
		/// <param name="a">First</param>
		/// <param name="b">Second</param>
		/// <returns><c>true</c> if equal, <c>false</c> otherwise</returns>
		public bool Equals(IAssembly a, IAssembly b) {
			return CompareTo(a, b) == 0;
		}

		/// <summary>
		/// Figures out which of two assembly names is closer to another assembly name
		/// </summary>
		/// <param name="requested">Requested assembly name</param>
		/// <param name="a">First</param>
		/// <param name="b">Second</param>
		/// <returns>-1 if both are equally close, 0 if <paramref name="a"/> is closest, 1 if
		/// <paramref name="b"/> is closest</returns>
		public int CompareClosest(IAssembly requested, IAssembly a, IAssembly b) {
			if (a == b)
				return 0;
			if (a == null)
				return !CompareName ? 1 : UTF8String.CaseInsensitiveEquals(requested.Name, b.Name) ? 1 : 0;
			if (b == null)
				return !CompareName ? 0 : UTF8String.CaseInsensitiveEquals(requested.Name, a.Name) ? 0 : 1;

			// Compare the most important parts first:
			//	1. Assembly simple name
			//	2. Public key token
			//	3. Version
			//	4. Locale
			//	5. Content type

			if (CompareName) {
				// If the name only matches one of a or b, return that one.
				bool na = UTF8String.CaseInsensitiveEquals(requested.Name, a.Name);
				bool nb = UTF8String.CaseInsensitiveEquals(requested.Name, b.Name);
				if (na && !nb)
					return 0;
				if (!na && nb)
					return 1;
				if (!na && !nb)
					return -1;
			}

			if (ComparePublicKeyToken) {
				bool pa, pb;
				if (PublicKeyBase.IsNullOrEmpty2(requested.PublicKeyOrToken)) {
					// If one of them has a pkt but the other one hasn't, return the one with
					// no pkt.
					pa = PublicKeyBase.IsNullOrEmpty2(a.PublicKeyOrToken);
					pb = PublicKeyBase.IsNullOrEmpty2(b.PublicKeyOrToken);
				}
				else {
					// If one of them has the correct pkt, but the other one has an incorrect
					// pkt, return the one with the correct pkt.
					pa = PublicKeyBase.TokenEquals(requested.PublicKeyOrToken, a.PublicKeyOrToken);
					pb = PublicKeyBase.TokenEquals(requested.PublicKeyOrToken, b.PublicKeyOrToken);
				}
				if (pa && !pb)
					return 0;
				if (!pa && pb)
					return 1;
			}

			if (CompareVersion && !Utils.Equals(a.Version, b.Version)) {
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

			if (CompareCulture) {
				bool la = Utils.LocaleEquals(requested.Culture, a.Culture);
				bool lb = Utils.LocaleEquals(requested.Culture, b.Culture);
				if (la && !lb)
					return 0;
				if (!la && lb)
					return 1;
			}

			if (CompareContentType) {
				bool ca = requested.ContentType == a.ContentType;
				bool cb = requested.ContentType == b.ContentType;
				if (ca && !cb)
					return 0;
				if (!ca && cb)
					return 1;
			}

			return -1;
		}

		/// <summary>
		/// Gets the hash code of an assembly name
		/// </summary>
		/// <param name="a">Assembly name</param>
		/// <returns>The hash code</returns>
		public int GetHashCode(IAssembly a) {
			if (a == null)
				return 0;

			int hash = 0;

			if (CompareName)
				hash += UTF8String.GetHashCode(a.Name);
			if (CompareVersion)
				hash += Utils.CreateVersionWithNoUndefinedValues(a.Version).GetHashCode();
			if (ComparePublicKeyToken)
				hash += PublicKeyBase.GetHashCodeToken(a.PublicKeyOrToken);
			if (CompareCulture)
				hash += Utils.GetHashCodeLocale(a.Culture);
			if (CompareContentType)
				hash += (int)a.ContentType;

			return hash;
		}
	}
}
