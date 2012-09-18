using System.Collections.Generic;
using System.Text;
using dot10.DotNet.MD;

namespace dot10.DotNet {
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
			return EscapeIdentifier(name ?? string.Empty);
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
			return EscapeIdentifier(@namespace ?? string.Empty);
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
				return EscapeIdentifier(name ?? string.Empty);
			return EscapeIdentifier(@namespace) + "." + EscapeIdentifier(name ?? string.Empty);
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
		/// Returns the full name of a generic instance type
		/// </summary>
		/// <param name="namespace">The namespace or <c>null</c> if none</param>
		/// <param name="name">The name</param>
		/// <param name="genArgs">The generic args or null if not a generic instance type</param>
		/// <returns>The full name, eg. <c>The.NameSpace.Name&lt;System.Int32&gt;</c></returns>
		public static string GetGenericInstanceFullName(string @namespace, string name, IList<ITypeSig> genArgs) {
			var sb = new StringBuilder();
			sb.Append(GetFullName(@namespace, name));
			if (genArgs != null) {
				sb.Append('<');
				for (int i = 0; i < genArgs.Count; i++) {
					if (i != 0)
						sb.Append(',');
					sb.Append(GetTypeFullName(genArgs[i]));
				}
				sb.Append('>');
			}
			return sb.ToString();
		}

		static string GetTypeFullName(ITypeSig typeSig) {
			if (typeSig == null)
				return "<<<NULL>>>";
			return typeSig.FullName;
		}

		/// <summary>
		/// Returns the reflection full name of a generic instance type
		/// </summary>
		/// <param name="namespace">The namespace or <c>null</c> if none</param>
		/// <param name="name">The name</param>
		/// <param name="genArgs">The generic args or null if not a generic instance type</param>
		/// <returns>The full name, eg. <c>The.NameSpace.Name&lt;System.Int32&gt;</c></returns>
		public static string GetGenericInstanceReflectionFullName(string @namespace, string name, IList<ITypeSig> genArgs) {
			var sb = new StringBuilder();
			sb.Append(GetReflectionFullName(@namespace, name));
			if (genArgs != null && genArgs.Count > 0) {
				sb.Append('[');
				for (int i = 0; i < genArgs.Count; i++) {
					if (i != 0)
						sb.Append(',');
					var genArg = genArgs[i];
					sb.Append('[');
					sb.Append(GetTypeReflectionFullName(genArg));
					sb.Append(", ");
					var asm = genArg.DefinitionAssembly;
					if (asm == null)
						sb.Append("<<<NULL>>>");
					else
						sb.Append(EscapeAssemblyName(GetAssemblyName(asm)));
					sb.Append(']');
				}
				sb.Append(']');
			}
			return sb.ToString();
		}

		static string GetTypeReflectionFullName(ITypeSig typeSig) {
			if (typeSig == null)
				return "<<<NULL>>>";
			return typeSig.ReflectionFullName;
		}

		static string GetAssemblyName(IAssembly assembly) {
			var pk = assembly.PublicKeyOrToken;
			if (pk is PublicKey)
				pk = ((PublicKey)pk).Token;
			return Utils.GetAssemblyNameString(new UTF8String(EscapeAssemblyName(assembly.Name)), assembly.Version, assembly.Locale, pk);
		}

		/// <summary>
		/// Escapes reserved characters in an identifier
		/// </summary>
		/// <param name="id">The identifier</param>
		/// <returns>The escaped identifier</returns>
		static string EscapeIdentifier(string id) {
			var sb = new StringBuilder(id.Length);
			// Periods are not escaped by Reflection, even if they're part of a type name.
			foreach (var c in id) {
				switch (c) {
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

		static string EscapeAssemblyName(UTF8String asmSimplName) {
			return EscapeAssemblyName(UTF8String.ToSystemString(asmSimplName));
		}

		static string EscapeAssemblyName(string asmSimplName) {
			var sb = new StringBuilder(asmSimplName.Length);
			foreach (var c in asmSimplName) {
				if (c == ']')
					sb.Append('\\');
				sb.Append(c);
			}
			return sb.ToString();
		}
	}
}
