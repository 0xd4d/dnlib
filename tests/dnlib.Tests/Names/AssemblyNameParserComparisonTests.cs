// dnlib: See LICENSE.txt for more info

using System;
using System.Linq;
using dnlib.DotNet;
using Xunit;
using AssemblyName = System.Reflection.AssemblyName;
using AssemblyNameFlags = System.Reflection.AssemblyNameFlags;

namespace dnlib.Tests.Names {
	/// <summary>
	/// dnlib's parser of assembly names ("Name, Version=1.2.3.4, Culture=neutral, PublicKeyToken=...") and
	/// the one of System.Reflection.AssemblyName read the same text. The tests have what both read:
	/// the name, the version, the culture, the public key (token), and "retargetable" / "winrt".
	/// <list type="bullet">
	/// <item>Where System.Reflection reads a value, dnlib reads the same value.</item>
	/// <item>A part that isn't in the text is null in System.Reflection. In dnlib the version is
	/// 65535.65535.65535.65535 ("no version", shown as null here) and the culture is empty (an AssemblyRef
	/// always has them).</item>
	/// <item>dnlib reads texts that System.Reflection rejects ("invalid"): the same key twice, a key
	/// without a value, a quote in a name that isn't in quotes, a public key token that doesn't have 8
	/// bytes. And "Language" is another name for "Culture".</item>
	/// </list>
	/// </summary>
	public class AssemblyNameParserComparisonTests {
		static string Describe(string? name, Version? version, string? culture, byte[]? publicKeyOrToken, bool isPublicKey, bool isRetargetable, bool isWindowsRuntime) =>
			string.Join("|", new[] {
				name ?? "null",
				version?.ToString() ?? "null",
				culture ?? "null",
				publicKeyOrToken is null ? "null" : (isPublicKey ? "key:" : "") + string.Concat(publicKeyOrToken.Select(a => a.ToString("x2"))),
				isRetargetable ? "retargetable" : null,
				isWindowsRuntime ? "winrt" : null,
			}.Where(a => a is not null));

		static string ParseWithDnlib(string text) {
			var assemblyRef = ReflectionTypeNameParser.ParseAssemblyRef(text);
			if (assemblyRef is null)
				return "invalid";
			// No version is 65535 for every number in dnlib, like in the metadata
			var version = assemblyRef.Version == new Version(65535, 65535, 65535, 65535) ? null : assemblyRef.Version;
			return Describe(assemblyRef.Name, version, assemblyRef.Culture, assemblyRef.PublicKeyOrToken?.Data, assemblyRef.HasPublicKey, assemblyRef.IsRetargetable, assemblyRef.IsContentTypeWindowsRuntime);
		}

		static string ParseWithSystemReflection(string text) {
			AssemblyName name;
			try {
				name = new AssemblyName(text);
			}
			catch (Exception ex) when (ex is System.IO.FileLoadException || ex is ArgumentException) {
				return "invalid";
			}
			bool isPublicKey = (name.Flags & AssemblyNameFlags.PublicKey) != 0;
			return Describe(name.Name, name.Version, name.CultureName, isPublicKey ? name.GetPublicKey() : name.GetPublicKeyToken(), isPublicKey,
				(name.Flags & AssemblyNameFlags.Retargetable) != 0, name.ContentType == System.Reflection.AssemblyContentType.WindowsRuntime);
		}

		[Theory]
		[InlineData("Lib", "Lib|null||null", "Lib|null|null|null")]
		[InlineData("Lib, Version=1.2.3.4, Culture=neutral, PublicKeyToken=b77a5c561934e089", "Lib|1.2.3.4||b77a5c561934e089", "Lib|1.2.3.4||b77a5c561934e089")]
		[InlineData("Lib, Version=1.2.3.4, Culture=de-DE, PublicKeyToken=null", "Lib|1.2.3.4|de-DE|null", "Lib|1.2.3.4|de-DE|")]
		// White space and the case of the keys
		[InlineData("  Lib  ,  Version = 1.2.3.4 ,Culture=de", "Lib|1.2.3.4|de|null", "Lib|1.2.3.4|de|null")]
		[InlineData("Lib,Version=1.2.3.4", "Lib|1.2.3.4||null", "Lib|1.2.3.4|null|null")]
		[InlineData("Lib, version=1.2.3.4, CULTURE=de, publickeytoken=b77a5c561934e089", "Lib|1.2.3.4|de|b77a5c561934e089", "Lib|1.2.3.4|de|b77a5c561934e089")]
		[InlineData("Lib\t, Version=1.2.3.4", "Lib|1.2.3.4||null", "Lib|1.2.3.4|null|null")]
		// Names with special chars
		[InlineData("Lib.Name-1_2", "Lib.Name-1_2|null||null", "Lib.Name-1_2|null|null|null")]
		[InlineData("A B", "A B|null||null", "A B|null|null|null")]
		[InlineData("\"A B\"", "A B|null||null", "A B|null|null|null")]
		[InlineData("' A '", " A |null||null", " A |null|null|null")]
		[InlineData("\" A \", Version=1.2.3.4", " A |1.2.3.4||null", " A |1.2.3.4|null|null")]
		[InlineData("\"A\\\"B\"", "A\"B|null||null", "A\"B|null|null|null")]
		[InlineData("A\\,B", "A,B|null||null", "A,B|null|null|null")]
		[InlineData("A\\=B", "A=B|null||null", "A=B|null|null|null")]
		[InlineData("A\\\\B", "A\\B|null||null", "A\\B|null|null|null")]
		[InlineData("A\\tB", "A\tB|null||null", "A\tB|null|null|null")]
		[InlineData("A=B", "A=B|null||null", "invalid")]
		[InlineData("A\"B", "A\"B|null||null", "invalid")]
		[InlineData("A'B", "A'B|null||null", "invalid")]
		[InlineData("\"A", "invalid", "invalid")]
		[InlineData("\"A\"B", "A|null||null", "invalid")]
		[InlineData("", "invalid", "invalid")]
		[InlineData(" ", "invalid", "invalid")]
		[InlineData(", Version=1.2.3.4", "invalid", "invalid")]
		[InlineData("\"\"", "invalid", "invalid")]
		// The same key twice, keys that don't exist, values that are missing
		[InlineData("Lib, Version=1.2.3.4, Version=2.0.0.0", "Lib|2.0.0.0||null", "invalid")]
		[InlineData("Lib, Culture=de, Culture=fr", "Lib|null|fr|null", "invalid")]
		[InlineData("Lib, Unknown=1", "Lib|null||null", "Lib|null|null|null")]
		[InlineData("Lib, Unknown", "Lib|null||null", "invalid")]
		[InlineData("Lib, Version=", "invalid", "invalid")]
		[InlineData("Lib, Culture=", "invalid", "invalid")]
		[InlineData("Lib, PublicKeyToken=", "invalid", "invalid")]
		[InlineData("Lib,", "Lib|null||null", "invalid")]
		[InlineData("Lib,,Version=1.2.3.4", "Lib|1.2.3.4||null", "invalid")]
		[InlineData("Lib, =1", "Lib|null||null", "invalid")]
		// The culture
		[InlineData("Lib, Culture=NEUTRAL", "Lib|null||null", "Lib|null||null")]
		[InlineData("Lib, Culture=\"de\"", "Lib|null|de|null", "Lib|null|de|null")]
		[InlineData("Lib, Language=de", "Lib|null|de|null", "Lib|null|null|null")]
		[InlineData("Lib, Culture=de\\,x", "Lib|null|de,x|null", "invalid")]
		// The public key token and the public key
		[InlineData("Lib, PublicKeyToken=B77A5C561934E089", "Lib|null||b77a5c561934e089", "Lib|null|null|b77a5c561934e089")]
		[InlineData("Lib, PublicKeyToken=NULL", "Lib|null||null", "Lib|null|null|")]
		[InlineData("Lib, PublicKeyToken=neutral", "Lib|null||null", "invalid")]
		[InlineData("Lib, PublicKeyToken=b77a5c56", "Lib|null||b77a5c56", "invalid")]
		[InlineData("Lib, PublicKeyToken=b77a5c561934e08", "invalid", "invalid")]
		[InlineData("Lib, PublicKeyToken=xyz", "invalid", "invalid")]
		[InlineData("Lib, PublicKeyToken=b77a5c561934e089aa", "Lib|null||b77a5c561934e089aa", "invalid")]
		[InlineData("Lib, PublicKey=null", "Lib|null||null", "Lib|null|null|key:")]
		[InlineData("Lib, PublicKey=00000000000000000400000000000000", "Lib|null||key:00000000000000000400000000000000", "Lib|null|null|key:00000000000000000400000000000000")]
		[InlineData("Lib, PublicKey=0102", "Lib|null||key:0102", "Lib|null|null|key:0102")]
		[InlineData("Lib, PublicKey=00000000000000000400000000000000, PublicKeyToken=b77a5c561934e089", "Lib|null||b77a5c561934e089", "invalid")]
		// Other keys
		[InlineData("Lib, Retargetable=Yes", "Lib|null||null|retargetable", "Lib|null|null|null|retargetable")]
		[InlineData("Lib, Retargetable=yes", "Lib|null||null|retargetable", "Lib|null|null|null|retargetable")]
		[InlineData("Lib, Retargetable=No", "Lib|null||null", "Lib|null|null|null")]
		[InlineData("Lib, Retargetable=1", "Lib|null||null", "invalid")]
		[InlineData("Lib, ContentType=WindowsRuntime", "Lib|null||null|winrt", "Lib|null|null|null|winrt")]
		[InlineData("Lib, ContentType=windowsruntime", "Lib|null||null|winrt", "Lib|null|null|null|winrt")]
		[InlineData("Lib, ContentType=Default", "Lib|null||null", "invalid")]
		[InlineData("Lib, ContentType=Other", "Lib|null||null", "invalid")]
		[InlineData("Lib, ProcessorArchitecture=MSIL", "Lib|null||null", "Lib|null|null|null")]
		[InlineData("Lib, ProcessorArchitecture=Nothing", "Lib|null||null", "invalid")]
		[InlineData("Lib, Custom=null", "Lib|null||null", "Lib|null|null|null")]
		public void NameIsParsed(string text, string expectedDnlib, string expectedSystemReflection) {
			Assert.Equal(expectedDnlib, ParseWithDnlib(text));
			// .NET Framework and Mono have other parsers
			if (typeof(object).Assembly.GetName().Name == "System.Private.CoreLib")
				Assert.Equal(expectedSystemReflection, ParseWithSystemReflection(text));
		}
	}
}
