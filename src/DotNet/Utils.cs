using System;
using System.Collections.Generic;
using System.Text;
using dot10.DotNet.MD;

namespace dot10.DotNet {
	static class Utils {
		/// <summary>
		/// Returns an assembly name string
		/// </summary>
		/// <param name="name">Simple assembly name</param>
		/// <param name="version">Version or null</param>
		/// <param name="culture">Culture or null</param>
		/// <param name="publicKey">Public key / public key token or null</param>
		/// <returns>An assembly name string</returns>
		internal static string GetAssemblyNameString(UTF8String name, Version version, UTF8String culture, PublicKeyBase publicKey) {
			var sb = new StringBuilder();
			sb.Append(UTF8String.IsNullOrEmpty(name) ? string.Empty : FullNameHelper.GetReflectionNamespace(name.String));

			if (version != null) {
				sb.Append(", Version=");
				sb.Append(version.ToString());
			}

			if ((object)culture != null) {
				sb.Append(", Culture=");
				sb.Append(UTF8String.IsNullOrEmpty(culture) ? "neutral" : FullNameHelper.GetReflectionName(culture.String));
			}

			sb.Append(", ");
			sb.Append(publicKey == null || publicKey is PublicKeyToken ? "PublicKeyToken=" : "PublicKey=");
			sb.Append(publicKey == null ? "null" : publicKey.ToString());

			return sb.ToString();
		}

		internal static string GetFieldString(string declaringType, UTF8String name, FieldSig fieldSig) {
			return GetFieldString(declaringType, name, fieldSig, null);
		}

		internal static string GetFieldString(string declaringType, UTF8String name, FieldSig fieldSig, IList<ITypeSig> typeGenArgs) {
			return GetFieldString(declaringType, UTF8String.IsNullOrEmpty(name) ? null : name.String, fieldSig, typeGenArgs);
		}

		internal static string GetFieldString(string declaringType, string name, FieldSig fieldSig) {
			return GetFieldString(declaringType, name, fieldSig, null);
		}

		internal static string GetFieldString(string declaringType, string name, FieldSig fieldSig, IList<ITypeSig> typeGenArgs) {
			if (fieldSig == null)
				return string.Empty;
			var sb = new StringBuilder();
			sb.Append(GetTypeFullName(fieldSig.Type, typeGenArgs));
			sb.Append(' ');
			if (!string.IsNullOrEmpty(declaringType)) {
				sb.Append(declaringType);
				sb.Append("::");
			}
			if (!string.IsNullOrEmpty(name))
				sb.Append(name);
			return sb.ToString();
		}

		internal static string GetMethodString(string declaringType, UTF8String name, MethodSig methodSig) {
			return GetMethodString(declaringType, name, methodSig, null, null);
		}

		internal static string GetMethodString(string declaringType, UTF8String name, MethodSig methodSig, IList<ITypeSig> typeGenArgs) {
			return GetMethodString(declaringType, name, methodSig, typeGenArgs, null);
		}

		internal static string GetMethodString(string declaringType, UTF8String name, MethodSig methodSig, IList<ITypeSig> typeGenArgs, IList<ITypeSig> methodGenArgs) {
			return GetMethodString(declaringType, UTF8String.IsNullOrEmpty(name) ? null : name.String, methodSig, typeGenArgs, methodGenArgs);
		}

		internal static string GetMethodString(string declaringType, string name, MethodSig methodSig) {
			return GetMethodString(declaringType, name, methodSig, null, null);
		}

		internal static string GetMethodString(string declaringType, string name, MethodSig methodSig, IList<ITypeSig> typeGenArgs) {
			return GetMethodString(declaringType, name, methodSig, typeGenArgs, null);
		}

		internal static string GetMethodString(string declaringType, string name, MethodSig methodSig, IList<ITypeSig> typeGenArgs, IList<ITypeSig> methodGenArgs) {
			if (methodSig == null)
				return string.Empty;

			var sb = new StringBuilder();

			sb.Append(GetTypeFullName(methodSig.RetType, typeGenArgs, methodGenArgs));
			sb.Append(' ');
			if (!string.IsNullOrEmpty(declaringType)) {
				sb.Append(declaringType);
				sb.Append("::");
			}
			if (name != null)
				sb.Append(name);

			if (methodSig.Generic) {
				sb.Append('<');
				for (int i = 0; i < methodSig.GenParamCount; i++) {
					if (i != 0)
						sb.Append(',');
					if (methodGenArgs != null && i < methodGenArgs.Count)
						sb.Append(GetTypeFullName(methodGenArgs[i]));
					else
						sb.Append(string.Format("!!{0}", i));
				}
				sb.Append('>');
			}
			sb.Append('(');
			int count = PrintMethodArgList(sb, methodSig.Params, typeGenArgs, methodGenArgs, false, false);
			PrintMethodArgList(sb, methodSig.ParamsAfterSentinel, typeGenArgs, methodGenArgs, count > 0, true);
			sb.Append(')');
			return sb.ToString();
		}

		static int PrintMethodArgList(StringBuilder sb, IEnumerable<ITypeSig> args, IList<ITypeSig> typeGenArgs, IList<ITypeSig> methodGenArgs, bool hasPrintedArgs, bool isAfterSentinel) {
			if (args == null)
				return 0;
			if (isAfterSentinel) {
				if (hasPrintedArgs)
					sb.Append(',');
				sb.Append("...");
				hasPrintedArgs = true;
			}
			int count = 0;
			foreach (var arg in args) {
				count++;
				if (hasPrintedArgs)
					sb.Append(',');
				sb.Append(GetTypeFullName(arg, typeGenArgs, methodGenArgs));
				hasPrintedArgs = true;
			}
			return count;
		}

		internal static string GetTypeFullName(ITypeSig typeSig) {
			return GetTypeFullName(typeSig, null, null);
		}

		internal static string GetTypeFullName(ITypeSig typeSig, IList<ITypeSig> typeGenArgs) {
			return GetTypeFullName(typeSig, typeGenArgs, null);
		}

		internal static string GetTypeFullName(ITypeSig typeSig, IList<ITypeSig> typeGenArgs, IList<ITypeSig> methodGenArgs) {
			return GetTypeFullName(typeSig, typeGenArgs, methodGenArgs, 0);
		}

		static string GetTypeFullName(ITypeSig typeSig, IList<ITypeSig> typeGenArgs, IList<ITypeSig> methodGenArgs, int recurse) {
			if (recurse++ >= 100)
				return "<<<INFRECURS>>>";
			if (typeSig == null)
				return "<<<NULL>>>";
			if (typeGenArgs != null && typeSig is GenericVar) {
				var gvar = (GenericVar)typeSig;
				if (gvar.Number < typeGenArgs.Count)
					return GetTypeFullName(typeGenArgs[(int)gvar.Number], typeGenArgs, methodGenArgs, recurse);
			}
			if (methodGenArgs != null && typeSig is GenericMVar) {
				var gmvar = (GenericMVar)typeSig;
				if (gmvar.Number < methodGenArgs.Count)
					return GetTypeFullName(methodGenArgs[(int)gmvar.Number], typeGenArgs, methodGenArgs, recurse);
			}
			return typeSig.FullName;
		}

		/// <summary>
		/// Convert a byte[] to a <see cref="string"/>
		/// </summary>
		/// <param name="bytes">All bytes</param>
		/// <param name="upper"><c>true</c> if output should be in upper case hex</param>
		/// <returns><paramref name="bytes"/> as a hex string</returns>
		internal static string ToHex(byte[] bytes, bool upper) {
			if (bytes == null)
				return "";
			var sb = new StringBuilder(bytes.Length / 2);
			foreach (var b in bytes) {
				sb.Append(ToHexChar(b >> 4, upper));
				sb.Append(ToHexChar(b & 0x0F, upper));
			}
			return sb.ToString();
		}

		static char ToHexChar(int val, bool upper) {
			if (0 <= val && val <= 9)
				return (char)(val + (int)'0');
			return (char)(val - 10 + (upper ? (int)'A' : (int)'a'));
		}

		/// <summary>
		/// Converts a hex string to a byte[]
		/// </summary>
		/// <param name="hexString">A string with an even number of hex characters</param>
		/// <returns><paramref name="hexString"/> converted to a byte[] or null
		/// if <paramref name="hexString"/> is invalid</returns>
		internal static byte[] ParseBytes(string hexString) {
			try {
				if (hexString.Length % 2 != 0)
					return null;
				var bytes = new byte[hexString.Length / 2];
				for (int i = 0; i < hexString.Length; i += 2) {
					int upper = TryParseHexChar(hexString[i]);
					int lower = TryParseHexChar(hexString[i + 1]);
					if (upper < 0 || lower < 0)
						return null;
					bytes[i / 2] = (byte)((upper << 4) | lower);
				}
				return bytes;
			}
			catch {
				return null;
			}
		}

		/// <summary>
		/// Converts a character to a hex digit
		/// </summary>
		/// <param name="c">Hex character</param>
		/// <returns><c>0x00</c>-<c>0x0F</c> if successful, <c>-1</c> if <paramref name="c"/> is not
		/// a valid hex digit</returns>
		static int TryParseHexChar(char c) {
			if ('0' <= c && c <= '9')
				return (ushort)c - (ushort)'0';
			if ('a' <= c && c <= 'f')
				return 10 + (ushort)c - (ushort)'a';
			if ('A' <= c && c <= 'F')
				return 10 + (ushort)c - (ushort)'A';
			return -1;
		}
	}
}
