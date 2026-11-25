// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb;
using dnlib.DotNet.Writer;
using dnlib.IO;

namespace dnlib.AotTests {
	/// <summary>
	/// Checks that dnlib works in a program that was compiled to native code (NativeAOT) or trimmed.
	/// In such programs there's no JIT compiler, code that isn't used is removed, reflection has no metadata
	/// tokens and there's no built-in COM interop, so some of dnlib's code takes other paths than usual.
	///
	/// The exit code is the number of failed checks.
	/// </summary>
	static class Program {
		static int Main() {
			var checks = new (string name, Action check)[] {
				("Create a module, write it and read it", CreateWriteAndRead),
				("Read a module from a file", ReadFromFile),
				("Read a file that isn't a .NET module", ReadInvalidFile),
				("Write and read a portable PDB", WriteAndReadPortablePdb),
				("Ask for the COM PDB reader", AskForComPdbReader),
				("Import reflection types and members", ImportReflectionObjects),
				("Compare with reflection types and members", CompareWithReflectionObjects),
				("Parse type names", ParseTypeNames),
			};

			int failures = 0;
			foreach (var (name, check) in checks) {
				try {
					check();
					Console.WriteLine("ok      " + name);
				}
				catch (Exception ex) {
					failures++;
					Console.WriteLine("FAILED  " + name);
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
			var type = module.Find("Ns.Calculator", isReflectionName: true);
			Check(type is not null, "the type exists");
			var method = type!.FindMethod("Add");
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
				// Write what was read (other writer code is used when the module was read from a file)
				var options = new ModuleWriterOptions(module);
				options.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
				using (var module2 = ModuleDefMD.Load(Write(module, options)))
					CheckCreatedModule(module2);
			}
		}

		static void ReadFromFile() {
			// A file name makes dnlib use memory mapped I/O
			var file = Path.Combine(AppContext.BaseDirectory, "dnlib-aot-test-" + Guid.NewGuid().ToString("N") + ".dll");
			File.WriteAllBytes(file, Write(CreateModule()));
			try {
				using (var module = ModuleDefMD.Load(file))
					CheckCreatedModule(module);
			}
			finally {
				File.Delete(file);
			}
		}

		static void ReadInvalidFile() {
			foreach (var data in new[] { new byte[0], new byte[] { (byte)'M', (byte)'Z' }, new byte[0x1000] }) {
				try {
					ModuleDefMD.Load(data).Dispose();
					Check(false, "reading invalid data must throw");
				}
				catch (BadImageFormatException) {
				}
				catch (DataReaderException) {
				}
			}
		}

		static byte[] WriteWithPortablePdb(out byte[] pdb) {
			var module = CreateModule();
			module.SetPdbState(new PdbState(module, PdbFileKind.PortablePDB));
			var document = new PdbDocument("/src/Calculator.cs", PdbDocumentConstants.LanguageCSharp, PdbDocumentConstants.LanguageVendorMicrosoft,
				PdbDocumentConstants.DocumentTypeText, Guid.Empty, new byte[0]);
			var method = module.Find("Ns.Calculator", isReflectionName: true)!.FindMethod("Add")!;
			method.Body!.Instructions[0].SequencePoint = new SequencePoint(document, 10, 5, 10, 25);
			method.Body.Instructions[5].SequencePoint = new SequencePoint(document, 11, 1, 11, 2);

			var pdbStream = new MemoryStream();
			var options = new ModuleWriterOptions(module) {
				WritePdb = true,
				PdbStream = pdbStream,
				PdbFileName = "Created.pdb",
			};
			var image = Write(module, options);
			pdb = pdbStream.ToArray();
			return image;
		}

		static void CheckSequencePoints(byte[] image, byte[] pdb, PdbReaderOptions pdbOptions) {
			var options = new ModuleCreationOptions {
				PdbFileOrData = pdb,
				PdbOptions = pdbOptions,
				TryToLoadPdbFromDisk = false,
			};
			using (var module = ModuleDefMD.Load(image, options)) {
				Check(module.PdbState is not null, "the PDB was loaded");
				var method = module.Find("Ns.Calculator", isReflectionName: true)!.FindMethod("Add")!;
				var sequencePoint = method.Body!.Instructions[0].SequencePoint;
				Check(sequencePoint is not null, "the first instruction has a sequence point");
				Check(sequencePoint!.StartLine == 10 && sequencePoint.StartColumn == 5 && sequencePoint.EndColumn == 25, "sequence point position");
				Check(sequencePoint.Document.Url == "/src/Calculator.cs", "document name");
				Check(method.Body.Instructions[1].SequencePoint is null, "the second instruction has no sequence point");
				Check(method.Body.Instructions[5].SequencePoint?.StartLine == 11, "the last instruction's sequence point");
			}
		}

		static void WriteAndReadPortablePdb() {
			var image = WriteWithPortablePdb(out var pdb);
			Check(pdb.Length != 0, "a PDB was written");
			CheckSequencePoints(image, pdb, PdbReaderOptions.None);
		}

		// The COM reader (Microsoft.DiaSymReader.Native) only exists on Windows and needs built-in COM interop,
		// which NativeAOT doesn't have. Asking for it must use the managed readers and not throw.
		static void AskForComPdbReader() {
			var image = WriteWithPortablePdb(out var pdb);
			CheckSequencePoints(image, pdb, PdbReaderOptions.MicrosoftComReader);
		}

		static void ImportReflectionObjects() {
			var module = CreateModule();
			var importer = new Importer(module);

			var listOfInt = importer.ImportAsTypeSigOrNull(typeof(List<int>));
			Check(listOfInt?.FullName == "System.Collections.Generic.List`1<System.Int32>", "imported List<int>: " + listOfInt?.FullName);

			var arrayOfString = importer.ImportAsTypeSigOrNull(typeof(string[,]));
			Check(arrayOfString?.FullName == "System.String[0...,0...]", "imported string[,]: " + arrayOfString?.FullName);

			var concat = importer.ImportOrNull(typeof(string).GetMethod(nameof(string.Concat), new[] { typeof(string), typeof(string) }));
			Check(concat?.FullName == "System.String System.String::Concat(System.String,System.String)", "imported string.Concat: " + concat?.FullName);

			// A method of a generic type: without metadata tokens, dnlib must find the method in List<T> another way
			// The parameter type in the signature must be the generic parameter (!0) and not int.
			var add = importer.ImportOrNull(typeof(List<int>).GetMethod(nameof(List<int>.Add)));
			Check(add?.MethodSig?.Params[0] is GenericVar { Number: 0 }, "imported List<int>.Add has parameter type " + add?.MethodSig?.Params[0]);
			Check(add?.DeclaringType?.FullName == "System.Collections.Generic.List`1<System.Int32>", "imported List<int>.Add is in " + add?.DeclaringType?.FullName);

			var empty = importer.ImportOrNull(typeof(string).GetField(nameof(string.Empty)));
			Check(empty?.FullName == "System.String System.String::Empty", "imported string.Empty: " + empty?.FullName);

			// What was imported can be written
			var type = module.Types.First(a => a.Name == "Calculator");
			var method = new MethodDefUser("UseImports", MethodSig.CreateStatic(module.CorLibTypes.Void), MethodImplAttributes.IL, MethodAttributes.Public | MethodAttributes.Static);
			type.Methods.Add(method);
			method.Body = new CilBody();
			method.Body.Instructions.Add(OpCodes.Ldsfld.ToInstruction(empty!));
			method.Body.Instructions.Add(OpCodes.Ldsfld.ToInstruction(empty!));
			method.Body.Instructions.Add(OpCodes.Call.ToInstruction(concat!));
			method.Body.Instructions.Add(OpCodes.Pop.ToInstruction());
			method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
			using (var written = ModuleDefMD.Load(Write(module))) {
				var operand = written.Types.First(a => a.Name == "Calculator").FindMethod("UseImports")!.Body!.Instructions[2].Operand as IMethod;
				Check(operand?.FullName == concat!.FullName, "the imported method is in the written module");
			}
		}

		static void CompareWithReflectionObjects() {
			var module = CreateModule();
			var importer = new Importer(module);
			var comparer = new SigComparer();

			var listOfInt = importer.ImportAsTypeSigOrNull(typeof(List<int>));
			Check(comparer.Equals(listOfInt, typeof(List<int>)), "List<int> equals typeof(List<int>)");
			Check(!comparer.Equals(listOfInt, typeof(List<string>)), "List<int> doesn't equal typeof(List<string>)");
			Check(comparer.GetHashCode(listOfInt) == comparer.GetHashCode(typeof(List<int>)), "same hash code");

			var concatInfo = typeof(string).GetMethod(nameof(string.Concat), new[] { typeof(string), typeof(string) })!;
			var concat = importer.ImportOrNull(concatInfo) as MemberRef;
			Check(comparer.Equals(concat, concatInfo), "string.Concat equals its MethodInfo");
			var concat3Info = typeof(string).GetMethod(nameof(string.Concat), new[] { typeof(string), typeof(string), typeof(string) })!;
			Check(!comparer.Equals(concat, concat3Info), "string.Concat(2 args) doesn't equal string.Concat(3 args)");
		}

		static void ParseTypeNames() {
			var module = CreateModule();

			var type = TypeNameParser.ParseAsTypeSigReflection(module, "System.Collections.Generic.Dictionary`2[[System.String, mscorlib],[System.Int32[], mscorlib]], mscorlib", null);
			Check(type?.FullName == "System.Collections.Generic.Dictionary`2<System.String,System.Int32[]>", "parsed type: " + type?.FullName);

			var typeInModule = TypeNameParser.ParseReflection(module, "Ns.Calculator", null);
			Check(typeInModule is TypeDef, "a type without an assembly name is found in the module");
		}
	}
}
