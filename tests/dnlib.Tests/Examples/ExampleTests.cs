// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using dnlib.DotNet;
using dnlib.Examples;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Examples {
	// The examples write files to the current directory and text to the console, which are the same for the
	// whole process: nothing else may run at the same time
	[CollectionDefinition(Name, DisableParallelization = true)]
	public class CurrentDirectoryCollection {
		public const string Name = "Current directory";
	}

	/// <summary>
	/// Runs the example programs in the <c>Examples</c> directory of the repository (their source files are
	/// compiled into the test assembly), so the code that users copy keeps working
	/// </summary>
	[Collection(CurrentDirectoryCollection.Name)]
	public sealed class ExampleTests : IDisposable {
		readonly TempDirectory directory = new TempDirectory();
		readonly string oldCurrentDirectory = Environment.CurrentDirectory;
		readonly TextWriter oldConsoleOut = Console.Out;
		readonly StringWriter consoleOutput = new StringWriter();

		public ExampleTests() {
			Environment.CurrentDirectory = directory.Path;
			Console.SetOut(consoleOutput);
		}

		public void Dispose() {
			Console.SetOut(oldConsoleOut);
			Environment.CurrentDirectory = oldCurrentDirectory;
			directory.Dispose();
		}

		// Example1 prints the types of the core library
		[Fact]
		public void Example1PrintsTypes() {
			Example1.Run();

			var output = consoleOutput.ToString();
			Assert.Contains("Type: System.Object", output);
			Assert.Contains("Total number of types: ", output);
		}

		// Example2 adds custom attributes and a resource to its own module and saves it
		[Fact]
		public void Example2SavesAnAssembly() {
			Example2.Run();

			using (var module = ModuleDefMD.Load(File.ReadAllBytes(directory.GetPath("saved-assembly.dll")))) {
				Assert.NotNull(module.Assembly);
				Assert.NotNull(module.Find("dnlib.Examples.Example2", isReflectionName: true));
			}
		}

		// Example3 creates a "Hello World" program
		[Fact]
		public void Example3CreatesAProgram() {
			Example3.Run();

			using (var module = ModuleDefMD.Load(File.ReadAllBytes(directory.GetPath("saved-assembly.exe")))) {
				var entryPoint = module.EntryPoint;
				Assert.NotNull(entryPoint);
				Assert.Contains(entryPoint.Body!.Instructions, a => "Hello World!".Equals(a.Operand));
			}
		}

		// Example4 creates a program with a class that has constructors and a base class
		[Fact]
		public void Example4CreatesAProgramWithConstructors() {
			Example4.Run();

			using (var module = ModuleDefMD.Load(File.ReadAllBytes(directory.GetPath("ctor-test.exe")))) {
				Assert.NotNull(module.EntryPoint);
				Assert.NotNull(module.Find("Ctor.Test.Main", isReflectionName: true));
			}
		}

		// Example5 writes every PE section of the core library to a file
		[Fact]
		public void Example5WritesTheSectionsOfAFile() {
			Example5.Run();

			Assert.NotEmpty(Directory.GetFiles(directory.Path, "section*.bin"));
			Assert.Contains("Dumping all sections", consoleOutput.ToString());
		}

		// Example6 adds a metadata heap and a PE section when it writes its own module
		[Fact]
		public void Example6SavesAnAssemblyWithAnExtraHeap() {
			Example6.Run();

			using (var module = ModuleDefMD.Load(File.ReadAllBytes(directory.GetPath("output.dll"))))
				Assert.NotNull(module.Find("dnlib.Examples.Example6", isReflectionName: true));
		}

		// Example7 resolves the types its own module uses and writes the module to memory
		[Fact]
		public void Example7Runs() => Example7.Run();
	}
}
