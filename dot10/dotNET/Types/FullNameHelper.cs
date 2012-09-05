using System.Text;

namespace dot10.dotNET.Types {
	static class FullNameHelper {
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
