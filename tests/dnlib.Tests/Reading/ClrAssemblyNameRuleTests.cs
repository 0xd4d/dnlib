// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// The full name of an assembly ("Name, Version=1.2.3.4, Culture=neutral, PublicKeyToken=...") is made
	/// from a row of the Assembly or the AssemblyRef table. These tests write values that a compiler doesn't
	/// write (a name with a comma, a public key token with 7 bytes, a public key without its flag, unknown
	/// flags) and read the full name with dnlib and with the runtime that runs the tests. If the runtime
	/// has a full name, dnlib must have the same one.
	/// </summary>
	public class ClrAssemblyNameRuleTests {
		const string Source = "public class C { }";
		static readonly byte[] baseImage = ClrReaderComparison.CreateBaseImage(Source);

		// "Column = value", separated by commas. A value is a number, 'text' (a new string) or hex bytes in
		// brackets (a new blob), eg. "Flags = 1, PublicKey = [00 01], Name = 'A B'".
		static byte[] CreateImage(Table table, string changes) {
			var original = new MetadataBlock(baseImage);
			var newStrings = original.GetStream("#Strings").Data.ToList();
			var newBlobs = original.GetStream("#Blob").Data.ToList();
			var patcher = new MetadataPatcher(baseImage);
			foreach (var change in changes.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries)) {
				int index = change.IndexOf(" = ");
				string column = change.Substring(0, index), value = change.Substring(index + 3);
				uint cell;
				if (value.StartsWith("'")) {
					cell = (uint)newStrings.Count;
					newStrings.AddRange(Encoding.UTF8.GetBytes(value.Trim('\'')));
					newStrings.Add(0);
				}
				else if (value.StartsWith("[")) {
					var bytes = value.Trim('[', ']').Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(a => Convert.ToByte(a, 16)).ToArray();
					Assert.True(bytes.Length < 0x80);
					cell = (uint)newBlobs.Count;
					newBlobs.Add((byte)bytes.Length);
					newBlobs.AddRange(bytes);
				}
				else
					cell = uint.Parse(value);
				patcher.SetCell(table, 1, column, cell);
			}
			var block = new MetadataBlock(patcher.ToArray());
			block.RemoveStream(ClrReaderComparison.PaddingStreamName);
			block.GetStream("#Strings").Data = newStrings.ToArray();
			block.GetStream("#Blob").Data = newBlobs.ToArray();
			return block.Write();
		}

		// The public key that the standard has for the assemblies of the runtime (16 bytes), and its token
		const string EcmaKey = "[00 00 00 00 00 00 00 00 04 00 00 00 00 00 00 00]";
		const string Token = "[01 02 03 04 05 06 07 08]";

		// Fixed in this commit: like System.Reflection.AssemblyName.FullName, dnlib leaves out a version part that
		// is 65535 ("no value") and the parts after it, and puts a name with a quote or with a space at its
		// start or end in double quotes. (Assembly.FullName uses single quotes for a name with a double quote.)
		//
		// A name of a referenced assembly goes through CultureInfo: what it does with a culture that it doesn't
		// know depends on the operating system, so the tests only have a culture that every system knows.
		[Theory]
		[InlineData("", "TestAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "TestAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")]
		[InlineData("MajorVersion = 65535; MinorVersion = 65535; BuildNumber = 65535; RevisionNumber = 65535", "TestAssembly, Culture=neutral, PublicKeyToken=null", "TestAssembly, Culture=neutral, PublicKeyToken=null")]
		[InlineData("MajorVersion = 65535", "TestAssembly, Culture=neutral, PublicKeyToken=null", "TestAssembly, Culture=neutral, PublicKeyToken=null")]
		// Assembly.FullName has a 65535 that comes after a number, AssemblyName.FullName (and dnlib) ends the
		// version before it
		[InlineData("RevisionNumber = 65535", "TestAssembly, Version=0.0.0, Culture=neutral, PublicKeyToken=null", "TestAssembly, Version=0.0.0.65535, Culture=neutral, PublicKeyToken=null")]
		[InlineData("MinorVersion = 65535", "TestAssembly, Version=0, Culture=neutral, PublicKeyToken=null", "TestAssembly, Version=0.65535.0.0, Culture=neutral, PublicKeyToken=null")]
		[InlineData("BuildNumber = 65535", "TestAssembly, Version=0.0, Culture=neutral, PublicKeyToken=null", "TestAssembly, Version=0.0.65535.0, Culture=neutral, PublicKeyToken=null")]
		// The name
		[InlineData("Name = 'A B'", "A B, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "A B, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")]
		[InlineData("Name = 'A,B'", "A\\,B, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "A\\,B, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")]
		[InlineData("Name = 'A=B'", "A\\=B, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "A\\=B, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")]
		[InlineData("Name = 'A\"B'", "\"A\\\"B\", Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "'A\"B', Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")]
		[InlineData("Name = 'A\\B'", "A\\\\B, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "A\\\\B, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")]
		[InlineData("Name = ' A '", "\" A \", Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "\" A \", Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")]
		[InlineData("Name = ''", ", Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "throws FileLoadException")]
		// The culture
		[InlineData("Locale = 'de'", "TestAssembly, Version=0.0.0.0, Culture=de, PublicKeyToken=null", "TestAssembly, Version=0.0.0.0, Culture=de, PublicKeyToken=null")]
		[InlineData("Locale = 'DE-de'", "TestAssembly, Version=0.0.0.0, Culture=DE-de, PublicKeyToken=null", "TestAssembly, Version=0.0.0.0, Culture=DE-de, PublicKeyToken=null")]
		[InlineData("Locale = 'neutral'", "TestAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "TestAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")]
		[InlineData("Locale = 'no-such-culture'", "TestAssembly, Version=0.0.0.0, Culture=no-such-culture, PublicKeyToken=null", "TestAssembly, Version=0.0.0.0, Culture=no-such-culture, PublicKeyToken=null")]
		[InlineData("Locale = 'a,b'", "TestAssembly, Version=0.0.0.0, Culture=a\\,b, PublicKeyToken=null", "TestAssembly, Version=0.0.0.0, Culture=a\\,b, PublicKeyToken=null")]
		// The public key and the flag that says that it's a public key (1)
		[InlineData("PublicKey = " + EcmaKey, "TestAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "TestAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
		[InlineData("PublicKey = " + EcmaKey + "; Flags = 1", "TestAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "TestAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
		[InlineData("Flags = 1", "TestAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "TestAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")]
		[InlineData("PublicKey = [01 02 03]", "TestAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=cfdf4fa87937761d", "throws SecurityException")]
		[InlineData("PublicKey = [01 02 03]; Flags = 1", "TestAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=cfdf4fa87937761d", "throws SecurityException")]
		[InlineData("PublicKey = " + Token, "TestAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=d54abb4e6eed953a", "throws SecurityException")]
		// Other flags: 0x100 = retargetable, 0x200 = a Windows Runtime assembly, 0x10-0x70 = processor
		[InlineData("Flags = 256", "TestAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null, Retargetable=Yes", "TestAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null, Retargetable=Yes")]
		[InlineData("Flags = 512", "TestAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime", "throws FileLoadException")]
		[InlineData("Flags = 1024", "TestAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "throws FileLoadException")]
		[InlineData("Flags = 48", "TestAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "TestAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null, processorArchitecture=MSIL")]
		[InlineData("Flags = 32768", "TestAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "TestAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")]
		[InlineData("Flags = 4294967294", "TestAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null, Retargetable=Yes", "throws FileLoadException")]
		[InlineData("HashAlgId = 0", "TestAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "TestAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")]
		[InlineData("HashAlgId = 4294967295", "TestAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "TestAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")]
		public void NameOfTheAssembly(string changes, string expectedDnlib, string expectedRuntime) {
			var image = CreateImage(Table.Assembly, changes);

			string dnlib;
			using (var module = ModuleDefMD.Load(image))
				dnlib = module.Assembly!.FullName;

			ClrReaderComparison.AssertRead(expectedDnlib, dnlib, expectedRuntime, () => {
				using (var loaded = new LoadedAssembly(image))
					return loaded.Assembly.FullName!;
			});
		}


		[Theory]
		[InlineData("", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708")]
		[InlineData("MajorVersion = 65535; MinorVersion = 65535; BuildNumber = 65535; RevisionNumber = 65535", "Ref, Culture=neutral, PublicKeyToken=0102030405060708", "Ref, Culture=neutral, PublicKeyToken=0102030405060708")]
		[InlineData("MajorVersion = 65535", "Ref, Culture=neutral, PublicKeyToken=0102030405060708", "Ref, Culture=neutral, PublicKeyToken=0102030405060708")]
		[InlineData("RevisionNumber = 65535", "Ref, Version=1.2.3, Culture=neutral, PublicKeyToken=0102030405060708", "Ref, Version=1.2.3, Culture=neutral, PublicKeyToken=0102030405060708")]
		[InlineData("BuildNumber = 65535", "Ref, Version=1.2, Culture=neutral, PublicKeyToken=0102030405060708", "Ref, Version=1.2, Culture=neutral, PublicKeyToken=0102030405060708")]
		// Fixed in this commit: a reference has no version if its second number is 65535. dnlib had "Version=1",
		// which is what the runtime has for the assembly itself (see above).
		[InlineData("MinorVersion = 65535", "Ref, Culture=neutral, PublicKeyToken=0102030405060708", "Ref, Culture=neutral, PublicKeyToken=0102030405060708")]
		// The name
		[InlineData("Name = 'A B'", "A B, Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708", "A B, Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708")]
		[InlineData("Name = 'A,B'", "A\\,B, Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708", "A\\,B, Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708")]
		[InlineData("Name = 'A=B'", "A\\=B, Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708", "A\\=B, Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708")]
		[InlineData("Name = 'A\"B'", "\"A\\\"B\", Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708", "\"A\\\"B\", Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708")]
		[InlineData("Name = 'A\\B'", "A\\\\B, Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708", "A\\\\B, Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708")]
		[InlineData("Name = ' A '", "\" A \", Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708", "\" A \", Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708")]
		[InlineData("Name = ''", ", Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708", "")]
		// The culture
		[InlineData("Locale = 'de'", "Ref, Version=1.2.3.4, Culture=de, PublicKeyToken=0102030405060708", "Ref, Version=1.2.3.4, Culture=de, PublicKeyToken=0102030405060708")]
		[InlineData("Locale = 'a,b'", "Ref, Version=1.2.3.4, Culture=a\\,b, PublicKeyToken=0102030405060708", "throws CultureNotFoundException")]
		// The public key token, or the public key with the flag that says so (1)
		[InlineData("PublicKeyOrToken = " + Token, "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708")]
		[InlineData("PublicKeyOrToken = " + Token + "; Flags = 1", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=d54abb4e6eed953a", "throws SecurityException")]
		[InlineData("PublicKeyOrToken = " + EcmaKey + "; Flags = 1", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=b77a5c561934e089", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
		[InlineData("PublicKeyOrToken = " + EcmaKey, "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=00000000000000000400000000000000", "throws ArgumentException")]
		[InlineData("PublicKeyOrToken = []", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null")]
		[InlineData("PublicKeyOrToken = []; Flags = 1", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null")]
		[InlineData("PublicKeyOrToken = 0", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null")]
		[InlineData("PublicKeyOrToken = [01 02 03 04 05 06 07]", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=01020304050607", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=01020304050607")]
		[InlineData("PublicKeyOrToken = [01 02 03 04 05 06 07 08 09]", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=010203040506070809", "throws ArgumentException")]
		[InlineData("PublicKeyOrToken = [01]", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=01", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=01")]
		// Other flags
		[InlineData("Flags = 256", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708, Retargetable=Yes", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708, Retargetable=Yes")]
		[InlineData("Flags = 512", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708, ContentType=WindowsRuntime", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708, ContentType=WindowsRuntime")]
		[InlineData("Flags = 1024", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708")]
		[InlineData("Flags = 48", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708")]
		[InlineData("Flags = 32768", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708")]
		[InlineData("Flags = 4294967294", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708, Retargetable=Yes", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708, Retargetable=Yes")]
		[InlineData("HashValue = [01 02 03]", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708", "Ref, Version=1.2.3.4, Culture=neutral, PublicKeyToken=0102030405060708")]
		public void NameOfAReferencedAssembly(string changes, string expectedDnlib, string expectedRuntime) {
			// The first row gets values that don't depend on the runtime, and then the changes of the test
			var image = CreateImage(Table.AssemblyRef, "Name = 'Ref'; MajorVersion = 1; MinorVersion = 2; BuildNumber = 3; RevisionNumber = 4; Flags = 0; PublicKeyOrToken = " + Token + "; " + changes);

			string dnlib;
			using (var module = ModuleDefMD.Load(image))
				dnlib = module.ResolveAssemblyRef(1)!.FullName;

			ClrReaderComparison.AssertRead(expectedDnlib, dnlib, expectedRuntime, () => {
				using (var loaded = new LoadedAssembly(image))
					return loaded.Assembly.GetReferencedAssemblies()[0].FullName;
			});
		}

		// Fixed in this commit: dnlib wrote 0 for a number that a System.Version doesn't have. The runtime (and
		// System.Reflection.Metadata) writes 65535, "no value", and reads the same version again from that.
		[Theory]
		[InlineData("1.2", "1.2.65535.65535")]
		[InlineData("1.2.3", "1.2.3.65535")]
		[InlineData("1.2.3.4", "1.2.3.4")]
		public void MissingVersionNumberIsWrittenAs65535(string version, string expectedInTheFile) {
			var stream = new MemoryStream();
			using (var module = ModuleDefMD.Load(baseImage)) {
				module.Assembly!.Version = new Version(version);
				module.ResolveAssemblyRef(1)!.Version = new Version(version);
				module.Write(stream);
			}
			var image = stream.ToArray();

			var patcher = new MetadataPatcher(image);
			foreach (var table in new[] { Table.Assembly, Table.AssemblyRef }) {
				var written = new[] { "MajorVersion", "MinorVersion", "BuildNumber", "RevisionNumber" }.Select(a => patcher.GetCell(table, 1, a));
				Assert.Equal(expectedInTheFile, string.Join(".", written));
			}
			using (var module = ModuleDefMD.Load(image)) {
				Assert.Equal(new Version(expectedInTheFile), module.Assembly!.Version);
				Assert.Equal(new Version(expectedInTheFile), module.ResolveAssemblyRef(1)!.Version);
				// A full name ends before the first number that has no value
				Assert.StartsWith("TestAssembly, Version=" + version + ", Culture=", module.Assembly.FullName);
				Assert.Contains(", Version=" + version + ", Culture=", module.ResolveAssemblyRef(1)!.FullName);
			}

			// The version of the reference is the System.Version that dnlib got
			ClrReaderComparison.AssertSameAsTheRuntime(version, () => {
				using (var loaded = new LoadedAssembly(image))
					return loaded.Assembly.GetReferencedAssemblies()[0].Version!.ToString();
			});
		}
	}
}
