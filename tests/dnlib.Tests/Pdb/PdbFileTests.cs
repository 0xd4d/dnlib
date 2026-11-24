// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Pdb {
	/// <summary>
	/// The module writer can create the PDB file itself (<see cref="ModuleWriterOptionsBase.PdbFileName"/>, or
	/// the name of the module file with the extension <c>.pdb</c>). These tests check which files exist after
	/// a write that worked and after a write that failed. They write portable PDB files; the same code
	/// decides about a Windows PDB file.
	/// </summary>
	public class PdbFileTests {
		static ModuleWriterOptions CreateOptions(ModuleDefMD module) =>
			new ModuleWriterOptions(module) { WritePdb = true, PdbOptions = PdbWriterOptions.None };

		[Fact]
		public void PdbFileIsCreatedNextToTheModuleByDefault() {
			using (var directory = new TempDirectory())
			using (var module = DebugInfoSource.Load()) {
				module.Write(directory.GetPath("written.dll"), CreateOptions(module));

				Assert.True(File.Exists(directory.GetPath("written.dll")));
				var pdb = File.ReadAllBytes(directory.GetPath("written.pdb"));
				using (var written = RoundTrip.LoadWithPdb(File.ReadAllBytes(directory.GetPath("written.dll")), pdb))
					Assert.NotEmpty(written.PdbState!.Documents);
			}
		}

		[Fact]
		public void PdbFileNameOptionIsTheNameOfThePdbFile() {
			using (var directory = new TempDirectory())
			using (var module = DebugInfoSource.Load()) {
				var options = CreateOptions(module);
				options.PdbFileName = directory.GetPath("other-name.pdb");

				module.Write(directory.GetPath("written.dll"), options);

				Assert.True(File.Exists(directory.GetPath("other-name.pdb")));
				Assert.False(File.Exists(directory.GetPath("written.pdb")));
			}
		}

		sealed class WriteFailedException : Exception {
		}

		// Fixed in 24e94420: the module file was deleted but the PDB file of the module that wasn't written
		// was left behind
		[Fact]
		public void BothFilesAreDeletedIfWritingFailsAfterThePdbWasWritten() {
			using (var directory = new TempDirectory())
			using (var module = DebugInfoSource.Load()) {
				var options = CreateOptions(module);
				bool pdbExistedWhenWritingFailed = false;
				options.WriterEvent += (sender, e) => {
					if (e.Event == ModuleWriterEvent.End) {
						pdbExistedWhenWritingFailed = File.Exists(directory.GetPath("written.pdb"));
						throw new WriteFailedException();
					}
				};

				Assert.Throws<WriteFailedException>(() => module.Write(directory.GetPath("written.dll"), options));

				Assert.True(pdbExistedWhenWritingFailed);
				Assert.False(File.Exists(directory.GetPath("written.dll")));
				Assert.False(File.Exists(directory.GetPath("written.pdb")));
			}
		}

		// Fixed in 24e94420: a PDB file that the writer couldn't replace was deleted after the failed write
		[Fact]
		public void ExistingPdbFileThatCanNotBeReplacedIsNotDeleted() {
			using (var directory = new TempDirectory())
			using (var module = DebugInfoSource.Load()) {
				string pdbFileName = directory.GetPath("written.pdb");
				File.WriteAllText(pdbFileName, "an older file");
				File.SetAttributes(pdbFileName, System.IO.FileAttributes.ReadOnly);
				try {
					Assert.SkipWhen(CanWrite(pdbFileName), "This user can write to a read-only file (root)");
					Assert.ThrowsAny<Exception>(() => module.Write(directory.GetPath("written.dll"), CreateOptions(module)));

					Assert.Equal("an older file", File.ReadAllText(pdbFileName));
					Assert.False(File.Exists(directory.GetPath("written.dll")));
				}
				finally {
					File.SetAttributes(pdbFileName, System.IO.FileAttributes.Normal);
				}
			}
		}

		static bool CanWrite(string fileName) {
			try {
				using (new FileStream(fileName, FileMode.Open, FileAccess.Write))
					return true;
			}
			catch (UnauthorizedAccessException) {
				return false;
			}
		}

		// Fixed in 24e94420: the writer remembered the PDB file of an earlier write and deleted it when a
		// later write failed before it had created a PDB file
		[Fact]
		public void PdbFileOfAnEarlierWriteIsNotDeletedIfALaterWriteFails() {
			using (var directory = new TempDirectory())
			using (var module = DebugInfoSource.Load()) {
				var options = CreateOptions(module);
				options.PdbFileName = directory.GetPath("written.pdb");
				var writer = new ModuleWriter(module, options);
				writer.Write(new MemoryStream());
				Assert.True(File.Exists(directory.GetPath("written.pdb")));

				options.WriterEvent += (sender, e) => {
					if (e.Event == ModuleWriterEvent.Begin)
						throw new WriteFailedException();
				};
				Assert.Throws<WriteFailedException>(() => writer.Write(new MemoryStream()));

				Assert.True(File.Exists(directory.GetPath("written.pdb")));
			}
		}
	}
}
