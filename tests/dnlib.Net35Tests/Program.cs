// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb;
using dnlib.DotNet.Writer;

namespace dnlib.Net35Tests {
	/// <summary>
	/// Checks that dnlib's build for .NET Framework 3.5 works on that runtime (CLR 2.0): the other tests run it
	/// on .NET Framework 4.8.
	///
	/// The exit code is the number of failed checks.
	/// </summary>
	static class Program {
		sealed class NamedCheck {
			public readonly string Name;
			public readonly Action Check;
			public NamedCheck(string name, Action check) {
				Name = name;
				Check = check;
			}
		}

		static int Main() {
			Console.WriteLine($"CLR {Environment.Version}, {IntPtr.Size * 8}-bit, dnlib for {typeof(ModuleDefMD).Assembly.ImageRuntimeVersion}");
			var checks = new[] {
				new NamedCheck("The runtime is CLR 2.0 and dnlib is its net35 build", RuntimeIsClr2),
				new NamedCheck("Create a module, write it and read it", CreateWriteAndRead),
				new NamedCheck("Read a module from a file", ReadFromFile),
				new NamedCheck("Read a file that isn't a .NET module", ReadInvalidFile),
				new NamedCheck("Write and read a portable PDB", WriteAndReadPortablePdb),
				new NamedCheck("Import reflection types and members", ImportReflectionObjects),
				new NamedCheck("Resolve types of the core library", ResolveTypesOfTheCoreLibrary),
				new NamedCheck("Read a module on several threads", ReadOnSeveralThreads),
				new NamedCheck("Read and write every assembly of the runtime", ReadAndWriteTheRuntimesAssemblies),
			};

			int failures = 0;
			foreach (var check in checks) {
				try {
					check.Check();
					Console.WriteLine("ok      " + check.Name);
				}
				catch (Exception ex) {
					failures++;
					Console.WriteLine("FAILED  " + check.Name);
					Console.WriteLine(ex);
				}
			}
			Console.WriteLine(failures == 0 ? "All checks passed" : failures + " check(s) failed");
			return failures;
		}

		static void Check(bool condition, string what) {
			if (!condition)
				throw new InvalidOperationException("Check failed: " + what);
		}

		static void RuntimeIsClr2() {
			Check(Environment.Version.Major == 2, "the runtime is " + Environment.Version);
			Check(typeof(ModuleDefMD).Assembly.ImageRuntimeVersion == "v2.0.50727", "dnlib is for " + typeof(ModuleDefMD).Assembly.ImageRuntimeVersion);
		}

		// A module with one class that has a method: static int Add(int a, int b) => a + b + 1000;
		static ModuleDefUser CreateModule() {
			var module = new ModuleDefUser("Created.dll") { Kind = ModuleKind.Dll };
			var assembly = new AssemblyDefUser("Created", new Version(1, 2, 3, 4));
			assembly.Modules.Add(module);

			var type = new TypeDefUser("Ns", "Calculator", module.CorLibTypes.Object.TypeDefOrRef);
			type.Attributes = TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed;
			module.Types.Add(type);

			var signature = MethodSig.CreateStatic(module.CorLibTypes.Int32, module.CorLibTypes.Int32, module.CorLibTypes.Int32);
			var method = new MethodDefUser("Add", signature, MethodImplAttributes.IL, MethodAttributes.Public | MethodAttributes.Static);
			type.Methods.Add(method);
			var body = new CilBody();
			method.Body = body;
			body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
			body.Instructions.Add(OpCodes.Ldarg_1.ToInstruction());
			body.Instructions.Add(OpCodes.Add.ToInstruction());
			body.Instructions.Add(Instruction.CreateLdcI4(1000));
			body.Instructions.Add(OpCodes.Add.ToInstruction());
			body.Instructions.Add(OpCodes.Ret.ToInstruction());
			return module;
		}

		static byte[] Write(ModuleDef module, ModuleWriterOptions? options = null) {
			var stream = new MemoryStream();
			module.Write(stream, options ?? new ModuleWriterOptions(module));
			return stream.ToArray();
		}

		static void CheckCreatedModule(ModuleDefMD module) {
			Check(module.Assembly?.Name == "Created", "assembly name");
			Check(module.Assembly?.Version == new Version(1, 2, 3, 4), "assembly version");
			var method = module.Find("Ns.Calculator", isReflectionName: true)?.FindMethod("Add");
			Check(method is not null, "the method exists");
			var instructions = method!.Body!.Instructions;
			Check(instructions.Count == 6, "number of instructions");
			Check(instructions[3].GetLdcI4Value() == 1000, "ldc.i4 operand");
			Check(method.MethodSig!.Params.Count == 2, "number of parameters");
		}

		static void CreateWriteAndRead() {
			var image = Write(CreateModule());
			using (var module = ModuleDefMD.Load(image)) {
				CheckCreatedModule(module);
				// The created method runs on this runtime
				var loaded = System.Reflection.Assembly.Load(image);
				var result = loaded.GetType("Ns.Calculator", true).GetMethod("Add").Invoke(null, new object[] { 1, 2 });
				Check(Equals(result, 1003), "the written method returns " + result);
			}
		}

		// .NET Framework 3.5 has no memory mapped files: the file is read in another way
		static void ReadFromFile() {
			var file = Path.GetTempFileName();
			try {
				File.WriteAllBytes(file, Write(CreateModule()));
				using (var module = ModuleDefMD.Load(file)) {
					CheckCreatedModule(module);
					Check(module.Location == file, "location is " + module.Location);
				}
				// The file isn't in use after that
				File.Delete(file);
			}
			finally {
				File.Delete(file);
			}
		}

		static void ReadInvalidFile() {
			foreach (var data in new[] { new byte[0], new byte[100], new byte[] { (byte)'M', (byte)'Z' } }) {
				try {
					ModuleDefMD.Load(data).Dispose();
					Check(false, "reading invalid data must throw");
				}
				catch (BadImageFormatException) {
				}
			}
		}

		static void WriteAndReadPortablePdb() {
			var module = CreateModule();
			var method = module.Find("Ns.Calculator", isReflectionName: true)!.FindMethod("Add")!;
			var document = new PdbDocument("/src/Calculator.cs", PdbDocumentConstants.LanguageCSharp, PdbDocumentConstants.LanguageVendorMicrosoft, PdbDocumentConstants.DocumentTypeText, Guid.Empty, new byte[0]);
			method.Body!.Instructions[0].SequencePoint = new SequencePoint(document, 10, 5, 10, 25);
			module.SetPdbState(new PdbState(module, PdbFileKind.PortablePDB));
			var pdb = new MemoryStream();
			var image = Write(module, new ModuleWriterOptions(module) { WritePdb = true, PdbStream = pdb, PdbFileName = "Created.pdb" });
			Check(pdb.Length != 0, "a PDB was written");

			using (var read = ModuleDefMD.Load(image, new ModuleCreationOptions { PdbFileOrData = pdb.ToArray(), TryToLoadPdbFromDisk = false })) {
				Check(read.PdbState is not null, "the PDB was loaded");
				var sequencePoint = read.Find("Ns.Calculator", isReflectionName: true)!.FindMethod("Add")!.Body!.Instructions[0].SequencePoint;
				Check(sequencePoint is not null && sequencePoint.StartLine == 10 && sequencePoint.EndColumn == 25, "sequence point position");
				Check(sequencePoint!.Document.Url == "/src/Calculator.cs", "document name");
			}
		}

		static void ImportReflectionObjects() {
			var module = CreateModule();
			var importer = new Importer(module);
			var listOfInt = importer.ImportAsTypeSig(typeof(List<int>));
			Check(listOfInt?.FullName == "System.Collections.Generic.List`1<System.Int32>", "imported List<int>: " + listOfInt?.FullName);
			var concat = importer.Import(typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) }));
			Check(concat?.FullName == "System.String System.String::Concat(System.String,System.String)", "imported string.Concat: " + concat?.FullName);
			Check(new SigComparer().Equals(listOfInt, typeof(List<int>)), "List<int> equals typeof(List<int>)");
		}

		// The references of this program are resolved to the assemblies of this runtime (in the GAC)
		static void ResolveTypesOfTheCoreLibrary() {
			var context = ModuleDef.CreateModuleContext();
			using (var module = ModuleDefMD.Load(typeof(Program).Module, context)) {
				Check(module.CorLibTypes.AssemblyRef.Version == new Version(2, 0, 0, 0), "core library version " + module.CorLibTypes.AssemblyRef.Version);
				var objectType = module.CorLibTypes.Object.TypeDefOrRef.ResolveTypeDef();
				Check(objectType is not null, "System.Object is resolved");
				Check(objectType!.Module!.Assembly!.Version == new Version(2, 0, 0, 0), "resolved to version " + objectType.Module.Assembly.Version);
				Check(objectType.FindMethod("ToString") is not null, "System.Object.ToString() exists");
				foreach (var assembly in ((AssemblyResolver)context.AssemblyResolver).GetCachedAssemblies()) {
					foreach (var other in assembly.Modules)
						other.Dispose();
				}
			}
		}

		// The same module is read by several threads at the same time (the locks are other ones than on newer runtimes)
		static void ReadOnSeveralThreads() {
			using (var module = ModuleDefMD.Load(typeof(ModuleDefMD).Module)) {
				var results = new int[4];
				Exception? exception = null;
				var threads = new List<Thread>();
				for (int i = 0; i < results.Length; i++) {
					int index = i;
					var thread = new Thread(() => {
						try {
							int count = 0;
							foreach (var type in module.GetTypes()) {
								foreach (var method in type.Methods) {
									if (method.HasBody)
										count += method.Body.Instructions.Count;
								}
							}
							results[index] = count;
						}
						catch (Exception ex) {
							exception = ex;
						}
					});
					threads.Add(thread);
					thread.Start();
				}
				foreach (var thread in threads)
					thread.Join();
				if (exception is not null)
					throw new InvalidOperationException("A thread failed", exception);
				Check(results[0] > 100000, "instructions read: " + results[0]);
				foreach (int result in results)
					Check(result == results[0], "every thread reads the same: " + result + " and " + results[0]);
			}
		}

		// Every managed DLL and EXE in the directory of the runtime: all types and method bodies are read, the
		// module is written, and the written module has the same number of types, methods and instructions
		static void ReadAndWriteTheRuntimesAssemblies() {
			var directory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
			var files = new List<string>(Directory.GetFiles(directory, "*.dll"));
			files.AddRange(Directory.GetFiles(directory, "*.exe"));
			int modules = 0;
			foreach (var file in files) {
				ModuleDefMD module;
				try {
					module = ModuleDefMD.Load(file);
				}
				catch (BadImageFormatException) {
					// A native file
					continue;
				}
				using (module) {
					string counts = Count(module);
					using (var written = ModuleDefMD.Load(Write(module, new ModuleWriterOptions(module) { Logger = DummyLogger.NoThrowInstance })))
						Check(Count(written) == counts, Path.GetFileName(file) + ": " + counts + " was written as " + Count(written));
					modules++;
				}
			}
			Check(modules >= 50, "only " + modules + " assemblies in " + directory);
		}

		static string Count(ModuleDef module) {
			int types = 0, methods = 0, instructions = 0, fields = 0, attributes = 0;
			foreach (var type in module.GetTypes()) {
				types++;
				fields += type.Fields.Count;
				attributes += type.CustomAttributes.Count;
				foreach (var method in type.Methods) {
					methods++;
					if (method.HasBody)
						instructions += method.Body.Instructions.Count;
				}
			}
			return $"{types} types, {fields} fields, {methods} methods, {instructions} instructions, {attributes} attributes";
		}
	}
}
