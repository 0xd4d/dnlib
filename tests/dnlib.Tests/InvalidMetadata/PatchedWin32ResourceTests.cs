// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.PortableExecutable;
using dnlib.DotNet;
using dnlib.TestUtils;
using dnlib.W32Resources;
using Xunit;

namespace dnlib.Tests.InvalidMetadata {
	/// <summary>
	/// Changes one byte of the Win32 resources of a valid file at a time (the directories, the names, the data
	/// entries), then reads all resources and writes the module. See <see cref="InvalidFileChecker"/> for what
	/// dnlib may do with such a file. The other tests of this directory use a file without Win32 resources.
	/// </summary>
	public class PatchedWin32ResourceTests {
		static readonly Lazy<byte[]> validImage = new Lazy<byte[]>(() =>
			CSharpCompiler.Compile("public class Program { public static void Main() { } }", new CSharpCompilerOptions {
				OutputKind = Microsoft.CodeAnalysis.OutputKind.ConsoleApplication,
				Win32Resources = true,
			}).Image);

		static readonly byte[] allValues = { 0x00, 0x01, 0x7F, 0x80, 0xFF };

		// Where the resource directory is in the file
		static void GetResourceRange(out int start, out int end) {
			using (var peReader = new PEReader(ImmutableArray.Create(validImage.Value))) {
				var directory = peReader.PEHeaders.PEHeader!.ResourceTableDirectory;
				Assert.True(peReader.PEHeaders.TryGetDirectoryOffset(directory, out start));
				end = start + directory.Size;
			}
		}

		// Reads every directory, name and data of the Win32 resources. Returns the number of data bytes.
		static long ReadAllResources(ModuleDef module, string description) {
			var root = module.Win32Resources?.Root;
			return root is null ? 0 : ReadDirectory(root, depth: 0, description);
		}

		static long ReadDirectory(ResourceDirectory directory, int depth, string description) {
			// A valid file has 3 levels. dnlib stops reading directories that are nested too deeply.
			Assert.True(depth < 50, $"The resource directories are nested {depth} levels deep ({description})");
			long dataLength = 0;
			GC.KeepAlive(directory.Name.ToString());
			foreach (var data in directory.Data) {
				GC.KeepAlive(data.Name.ToString());
				dataLength += data.CreateReader().ToArray().Length;
			}
			foreach (var child in directory.Directories)
				dataLength += ReadDirectory(child, depth + 1, description);
			return dataLength;
		}

		static void PatchBytes(int start, int end, int step) {
			var valid = validImage.Value;
			int patches = 0;
			for (int offset = start; offset < end; offset += step) {
				foreach (byte value in allValues) {
					if (valid[offset] == value)
						continue;
					var image = (byte[])valid.Clone();
					image[offset] = value;
					string description = $"byte at offset 0x{offset:X} (resource directory + 0x{offset - start:X}): 0x{valid[offset]:X2} -> 0x{value:X2}";

					try {
						using (var module = ModuleDefMD.Load(image)) {
							long dataLength = ReadAllResources(module, description);
							// A size in the file can't make dnlib return more data than the file has
							Assert.True(dataLength <= 100L * image.Length, $"{dataLength} bytes of resource data were read ({description})");
						}
					}
					catch (Exception ex) when (!(ex is Xunit.Sdk.XunitException)) {
						throw new InvalidOperationException($"Reading the Win32 resources threw {ex.GetType().Name} ({description})", ex);
					}
					InvalidFileChecker.ReadAndWrite(valid, image, description);
					patches++;
				}
			}
			Assert.True(patches > 0, "No byte was patched");
		}

		[Fact]
		public void TheValidFileHasWin32Resources() {
			using (var module = ModuleDefMD.Load(validImage.Value))
				Assert.True(ReadAllResources(module, "valid file") > 100);
			GetResourceRange(out int start, out int end);
			Assert.True(end - start > 500);
		}

		// The directories and their entries are at the start: every byte
		[Fact]
		public void DirectoriesAndEntries() {
			GetResourceRange(out int start, out int end);

			PatchBytes(start, Math.Min(end, start + 0x100), step: 1);
		}

		// The rest: the data entries, the names and the data
		[Fact]
		public void NamesAndData() {
			GetResourceRange(out int start, out int end);

			PatchBytes(Math.Min(end, start + 0x100), end, step: 7);
		}

		[Fact, Trait(TestCategories.Name, TestCategories.Slow)]
		public void EveryByte() {
			GetResourceRange(out int start, out int end);

			PatchBytes(start, end, step: 1);
		}

		[Fact]
		public void TruncatedFile() {
			var valid = validImage.Value;
			GetResourceRange(out int start, out int end);
			for (int length = start; length < end; length += 11) {
				var image = new byte[length];
				Array.Copy(valid, image, length);
				InvalidFileChecker.ReadAndWrite(valid, image, $"file truncated to 0x{length:X} of 0x{valid.Length:X} bytes");
			}
		}
	}
}
