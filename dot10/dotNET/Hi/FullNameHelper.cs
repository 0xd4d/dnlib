using System.Text;
using dot10.dotNET.MD;

namespace dot10.dotNET.Hi {
	static class FullNameHelper {
		/// <summary>
		/// Returns the name
		/// </summary>
		/// <param name="name">The name</param>
		/// <returns>The name (always non-null)</returns>
		public static string GetName(string name) {
			return name ?? string.Empty;
		}

		/// <summary>
		/// Returns the name
		/// </summary>
		/// <param name="name">The name</param>
		/// <returns>The name (always non-null)</returns>
		public static string GetName(UTF8String name) {
			return GetName(UTF8String.ToSystemString(name));
		}

		/// <summary>
		/// Returns the reflection name
		/// </summary>
		/// <param name="name">The name</param>
		/// <returns>The reflection name (always non-null)</returns>
		public static string GetReflectionName(string name) {
			return EscapeIdentifier(name ?? string.Empty, true);
		}

		/// <summary>
		/// Returns the reflection name
		/// </summary>
		/// <param name="name">The name</param>
		/// <returns>The reflection name (always non-null)</returns>
		public static string GetReflectionName(UTF8String name) {
			return GetReflectionName(UTF8String.ToSystemString(name));
		}

		/// <summary>
		/// Returns the namespace
		/// </summary>
		/// <param name="namespace">The namespace</param>
		/// <returns>The namespace (always non-null)</returns>
		public static string GetNamespace(string @namespace) {
			return @namespace ?? string.Empty;
		}

		/// <summary>
		/// Returns the namespace
		/// </summary>
		/// <param name="namespace">The namespace</param>
		/// <returns>The namespace (always non-null)</returns>
		public static string GetNamespace(UTF8String @namespace) {
			return GetNamespace(UTF8String.ToSystemString(@namespace));
		}

		/// <summary>
		/// Returns the reflection namespace
		/// </summary>
		/// <param name="namespace">The namespace</param>
		/// <returns>The reflection namespace (always non-null)</returns>
		public static string GetReflectionNamespace(string @namespace) {
			return EscapeIdentifier(@namespace ?? string.Empty, false);
		}

		/// <summary>
		/// Returns the reflection namespace
		/// </summary>
		/// <param name="namespace">The namespace</param>
		/// <returns>The reflection namespace (always non-null)</returns>
		public static string GetReflectionNamespace(UTF8String @namespace) {
			return GetReflectionNamespace(UTF8String.ToSystemString(@namespace));
		}

		/// <summary>
		/// Returns the full name
		/// </summary>
		/// <param name="namespace">The namespace or <c>null</c> if none</param>
		/// <param name="name">The name</param>
		/// <returns>The full name, eg. <c>The.NameSpace.Name</c></returns>
		public static string GetFullName(string @namespace, string name) {
			if (string.IsNullOrEmpty(@namespace))
				return name ?? string.Empty;
			return @namespace + "." + (name ?? string.Empty);
		}

		/// <summary>
		/// Returns the full name
		/// </summary>
		/// <param name="namespace">The namespace or <c>null</c> if none</param>
		/// <param name="name">The name</param>
		/// <returns>The full name, eg. <c>The.NameSpace.Name</c></returns>
		public static string GetFullName(UTF8String @namespace, UTF8String name) {
			return GetFullName(UTF8String.ToSystemString(@namespace), UTF8String.ToSystemString(name));
		}

		/// <summary>
		/// Returns the reflection full name
		/// </summary>
		/// <param name="namespace">The namespace or <c>null</c> if none</param>
		/// <param name="name">The name</param>
		/// <returns>The full name, eg. <c>The.NameSpace.Name</c></returns>
		public static string GetReflectionFullName(string @namespace, string name) {
			if (string.IsNullOrEmpty(@namespace))
				return EscapeIdentifier(name ?? string.Empty, true);
			return EscapeIdentifier(@namespace, false) + "." + EscapeIdentifier(name ?? string.Empty, true);
		}

		/// <summary>
		/// Returns the reflection full name
		/// </summary>
		/// <param name="namespace">The namespace or <c>null</c> if none</param>
		/// <param name="name">The name</param>
		/// <returns>The full name, eg. <c>The.NameSpace.Name</c></returns>
		public static string GetReflectionFullName(UTF8String @namespace, UTF8String name) {
			return GetReflectionFullName(UTF8String.ToSystemString(@namespace), UTF8String.ToSystemString(name));
		}

		/// <summary>
		/// Escapes reserved characters in an identifier
		/// </summary>
		/// <param name="id">The identifier</param>
		/// <param name="escapePeriods"><c>true</c> if periods should be escaped. This should
		/// be <c>false</c> if <paramref name="id"/> is a namespace identifier.</param>
		/// <returns>The escaped identifier</returns>
		static string EscapeIdentifier(string id, bool escapePeriods) {
			var sb = new StringBuilder(id.Length);
			foreach (var c in id) {
				switch (c) {
				case '.':
					if (!escapePeriods)
						break;
					sb.Append('\\');
					break;

				case ',':
				case '+':
				case '&':
				case '*':
				case '[':
				case ']':
				case '\\':
					sb.Append('\\');
					break;
				}
				sb.Append(c);
			}
			return sb.ToString();
		}
	}
}
