// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// An embedded resource is a row of the ManifestResource table with an offset into the resources of the
	/// .NET header. There the resource has a 4 byte length and then its bytes. These tests write offsets,
	/// lengths, flags and implementations that a compiler doesn't write, and read the resource with dnlib
	/// and with the runtime that runs the tests. If the runtime reads bytes, dnlib must read the same bytes.
	/// Where the runtime throws or has no resource, dnlib has an empty resource (or the kind of resource
	/// that the implementation says it is).
	/// </summary>
	public class ClrResourceRuleTests {
		// Three resources: R1 (4 bytes), R2 (3 bytes) and R3 (2 bytes)
		static byte[] Compile() {
			var options = new CSharpCompilerOptions();
			options.Resources.Add("R1", new byte[] { 0x11, 0x12, 0x13, 0x14 });
			options.Resources.Add("R2", new byte[] { 0x21, 0x22, 0x23 });
			options.Resources.Add("R3", new byte[] { 0x31, 0x32 });
			return CSharpCompiler.Compile("public class C { }", options).Image;
		}
		static readonly byte[] baseImage = Compile();

		// "What = value", separated by "; ". 'What' is a column of the row of R1 (Offset, Flags, Implementation),
		// "Length" (the 4 byte length that R1 has in the file) or "Size" (the size of all resources in the
		// .NET header). A value that starts with '+' is added to the original value.
		static byte[] CreateImage(string changes) {
			long resourcesOffset, sizeOffset;
			using (var module = ModuleDefMD.Load(baseImage)) {
				var header = module.Metadata.ImageCor20Header;
				resourcesOffset = (long)module.Metadata.PEImage.ToFileOffset(header.Resources.VirtualAddress);
				// The RVA and the size of the resources are at offset 24 and 28 of the .NET header
				sizeOffset = (long)header.StartOffset + 28;
			}
			var patcher = new MetadataPatcher(baseImage);
			long lengthOffset = resourcesOffset + patcher.GetCell(Table.ManifestResource, 1, "Offset");
			foreach (var change in changes.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries)) {
				var parts = change.Split(new[] { " = " }, StringSplitOptions.None);
				string what = parts[0];
				uint original = what == "Length" ? LittleEndian.ToUInt32(baseImage, (int)lengthOffset) :
					what == "Size" ? LittleEndian.ToUInt32(baseImage, (int)sizeOffset) :
					patcher.GetCell(Table.ManifestResource, 1, what);
				uint value = parts[1].StartsWith("+") ? original + uint.Parse(parts[1].Substring(1)) : uint.Parse(parts[1]);
				if (what == "Length")
					patcher.SetBytes(lengthOffset, LittleEndian.GetBytes(value));
				else if (what == "Size")
					patcher.SetBytes(sizeOffset, LittleEndian.GetBytes(value));
				else
					patcher.SetCell(Table.ManifestResource, 1, what, value);
			}
			return patcher.ToArray();
		}

		static string Describe(byte[] bytes) => bytes.Length == 0 ? "empty" :
			string.Join(" ", bytes.Take(8).Select(a => a.ToString("X2"))) + (bytes.Length > 8 ? " ... (" + bytes.Length + " bytes)" : "");

		[Theory]
		[InlineData("", "11 12 13 14", "11 12 13 14")]
		// The offset of the resource
		[InlineData("Offset = +8", "21 22 23", "21 22 23")]
		[InlineData("Offset = +16", "31 32", "31 32")]
		[InlineData("Offset = +1", "empty", "throws BadImageFormatException")]
		[InlineData("Offset = +4", "empty", "throws BadImageFormatException")]
		[InlineData("Offset = +17", "empty", "throws BadImageFormatException")]
		[InlineData("Offset = +18", "empty", "throws BadImageFormatException")]
		[InlineData("Offset = +19", "empty", "throws BadImageFormatException")]
		[InlineData("Offset = +20", "empty", "throws BadImageFormatException")]
		[InlineData("Offset = +21", "empty", "throws BadImageFormatException")]
		[InlineData("Offset = +22", "empty", "throws BadImageFormatException")]
		[InlineData("Offset = +23", "empty", "throws BadImageFormatException")]
		[InlineData("Offset = +24", "empty", "throws BadImageFormatException")]
		[InlineData("Offset = +1000", "empty", "throws BadImageFormatException")]
		[InlineData("Offset = 2147483648", "empty", "throws BadImageFormatException")]
		[InlineData("Offset = 4294967295", "empty", "throws BadImageFormatException")]
		[InlineData("Offset = 4294967288", "empty", "throws BadImageFormatException")]
		// The length of the resource
		[InlineData("Length = 0", "empty", "empty")]
		[InlineData("Length = 1", "11", "11")]
		[InlineData("Length = 5", "11 12 13 14 03", "11 12 13 14 03")]
		[InlineData("Length = 12", "11 12 13 14 03 00 00 00 ... (12 bytes)", "11 12 13 14 03 00 00 00 ... (12 bytes)")]
		[InlineData("Length = 17", "11 12 13 14 03 00 00 00 ... (17 bytes)", "11 12 13 14 03 00 00 00 ... (17 bytes)")]
		[InlineData("Length = 18", "11 12 13 14 03 00 00 00 ... (18 bytes)", "11 12 13 14 03 00 00 00 ... (18 bytes)")]
		[InlineData("Length = 19", "11 12 13 14 03 00 00 00 ... (19 bytes)", "11 12 13 14 03 00 00 00 ... (19 bytes)")]
		[InlineData("Length = 20", "11 12 13 14 03 00 00 00 ... (20 bytes)", "11 12 13 14 03 00 00 00 ... (20 bytes)")]
		[InlineData("Length = 21", "empty", "throws BadImageFormatException")]
		[InlineData("Length = 1000", "empty", "throws BadImageFormatException")]
		[InlineData("Length = 2147483648", "empty", "throws BadImageFormatException")]
		[InlineData("Length = 4294967295", "empty", "throws BadImageFormatException")]
		[InlineData("Length = 4294967292", "empty", "throws BadImageFormatException")]
		// The size of all resources
		[InlineData("Size = 0", "empty", "throws BadImageFormatException")]
		[InlineData("Size = 3", "empty", "throws BadImageFormatException")]
		[InlineData("Size = 4", "empty", "throws BadImageFormatException")]
		[InlineData("Size = 5", "empty", "throws BadImageFormatException")]
		[InlineData("Size = 7", "empty", "throws BadImageFormatException")]
		[InlineData("Size = 8", "11 12 13 14", "11 12 13 14")]
		[InlineData("Size = 9", "11 12 13 14", "11 12 13 14")]
		[InlineData("Size = +8", "11 12 13 14", "11 12 13 14")]
		[InlineData("Size = +8; Length = 28", "11 12 13 14 03 00 00 00 ... (28 bytes)", "11 12 13 14 03 00 00 00 ... (28 bytes)")]
		[InlineData("Size = +8; Length = 29", "empty", "throws BadImageFormatException")]
		// The flags: 1 = public, 2 = private
		[InlineData("Flags = 0", "11 12 13 14", "11 12 13 14")]
		[InlineData("Flags = 2", "11 12 13 14", "11 12 13 14")]
		[InlineData("Flags = 3", "11 12 13 14", "11 12 13 14")]
		[InlineData("Flags = 4294967295", "11 12 13 14", "11 12 13 14")]
		// The implementation: 0 = in this file. It's a coded token: tag 0 = File, 1 = AssemblyRef,
		// 2 = ExportedType, 3 = not used
		[InlineData("Implementation = 4", "empty", "null")]
		[InlineData("Implementation = 5", "AssemblyLinked", "null")]
		[InlineData("Implementation = 1", "11 12 13 14", "throws BadImageFormatException")]
		[InlineData("Implementation = 2", "11 12 13 14", "throws BadImageFormatException")]
		[InlineData("Implementation = 6", "empty", "throws BadImageFormatException")]
		[InlineData("Implementation = 3", "11 12 13 14", "11 12 13 14")]
		[InlineData("Implementation = 7", "11 12 13 14", "11 12 13 14")]
		[InlineData("Implementation = 401", "empty", "throws BadImageFormatException")]
		public void EmbeddedResource(string changes, string expectedDnlib, string expectedRuntime) {
			var image = CreateImage(changes);

			string dnlib;
			using (var module = ModuleDefMD.Load(image)) {
				var resource = module.Resources.Find("R1");
				dnlib = resource is EmbeddedResource embedded ? Describe(embedded.CreateReader().ToArray()) : resource!.ResourceType.ToString();
			}

			ClrReaderComparison.AssertRead(expectedDnlib, dnlib, expectedRuntime, () => {
				using (var loaded = new LoadedAssembly(image))
				using (var stream = loaded.Assembly.GetManifestResourceStream("R1")) {
					if (stream is null)
						return "null";
					var bytes = new MemoryStream();
					stream.CopyTo(bytes);
					return Describe(bytes.ToArray());
				}
			});
		}
	}
}
