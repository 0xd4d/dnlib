// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb;
using dnlib.DotNet.Resources;
using dnlib.DotNet.Writer;
using dnlib.IO;
using dnlib.W32Resources;
using Xunit;

namespace dnlib.Tests.Performance {
	// These tests measure time, so nothing else may run at the same time
	[CollectionDefinition(Name, DisableParallelization = true)]
	public class TimingCollection {
		public const string Name = "Timing";
	}

	/// <summary>
	/// Finds code whose time or memory grows faster than its input, eg. a loop over all items for every item.
	/// A limit in seconds would depend on the computer and on what else it's doing, so each test creates the
	/// same module at two sizes, N and 4N items, and compares them: 4 times the items should take about 4 times
	/// the time and memory. A quadratic algorithm takes 16 times as long.
	/// Measured: writing the created module, reading the file and using everything in it, and writing what
	/// was read with preserved tokens.
	/// </summary>
	[Collection(TimingCollection.Name)]
	public class GrowthTests {
		// 4N items may take this many times longer and allocate this many times more than N items. Linear is 4.
		const double maxTimeRatio = 10;
		const double maxAllocationRatio = 6;
		// Added to the time limit: short times are mostly noise (a garbage collection, another program)
		static readonly TimeSpan timeSlack = TimeSpan.FromMilliseconds(50);

		sealed class Measurement {
			public TimeSpan Time;
			public long AllocatedBytes;
		}

		// The time is the best of a few runs: other programs can only make a run slower
		static Measurement Measure(Action action) {
			var result = new Measurement { Time = TimeSpan.MaxValue };
			for (int i = 0; i < 3; i++) {
				// So a collection of the garbage of earlier code isn't part of the time
				GC.Collect();
				long allocatedBefore = GetAllocatedBytes();
				var stopwatch = Stopwatch.StartNew();
				action();
				stopwatch.Stop();
				result.AllocatedBytes = GetAllocatedBytes() - allocatedBefore;
				if (stopwatch.Elapsed < result.Time)
					result.Time = stopwatch.Elapsed;
			}
			return result;
		}

		static long GetAllocatedBytes() {
#if NET
			return GC.GetAllocatedBytesForCurrentThread();
#else
			// Not available on .NET Framework: only the time is compared
			return 0;
#endif
		}

		static void AssertGrowsLikeTheInput(string operation, int size, Measurement small, Measurement big) {
			var timeLimit = TimeSpan.FromTicks((long)(small.Time.Ticks * maxTimeRatio)) + timeSlack;
			Assert.True(big.Time <= timeLimit,
				$"{operation}: {size} items took {small.Time.TotalMilliseconds:0.0} ms, {size * 4} items took {big.Time.TotalMilliseconds:0.0} ms, the limit is {timeLimit.TotalMilliseconds:0.0} ms");
			if (small.AllocatedBytes > 0) {
				double allocationRatio = (double)big.AllocatedBytes / small.AllocatedBytes;
				Assert.True(allocationRatio <= maxAllocationRatio,
					$"{operation}: {size} items allocated {small.AllocatedBytes:N0} bytes, {size * 4} items allocated {big.AllocatedBytes:N0} bytes ({allocationRatio:0.0} times more)");
			}
		}

		sealed class Measurements {
			public Measurement Write = new Measurement();
			public Measurement Read = new Measurement();
			public Measurement WritePreserved = new Measurement();
		}

		// createModule: creates the module with 'size' items
		// withPdb: the module has debug info, it's written to a portable PDB and read from it
		static Measurements MeasureModule(Func<int, ModuleDef> createModule, int size, bool withPdb) {
			var measurements = new Measurements();
			var module = createModule(size);
			byte[] image = Array.Empty<byte>();
			byte[]? pdb = null;

			measurements.Write = Measure(() => Write(module, withPdb, preserve: false, out image, out pdb));
			measurements.Read = Measure(() => {
				using (var readModule = Load(image, pdb))
					UseEverything(readModule);
			});
			using (var readModule = Load(image, pdb)) {
				UseEverything(readModule);
				measurements.WritePreserved = Measure(() => Write(readModule, withPdb, preserve: true, out _, out _));
			}
			return measurements;
		}

		static void Write(ModuleDef module, bool withPdb, bool preserve, out byte[] image, out byte[]? pdb) {
			var options = new ModuleWriterOptions(module);
			if (preserve)
				options.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
			var pdbStream = new MemoryStream();
			if (withPdb) {
				options.WritePdb = true;
				options.PdbStream = pdbStream;
				options.PdbFileName = "growth.pdb";
			}
			var stream = new MemoryStream();
			module.Write(stream, options);
			image = stream.ToArray();
			pdb = withPdb ? pdbStream.ToArray() : null;
		}

		static ModuleDefMD Load(byte[] image, byte[]? pdb) {
			var options = new ModuleCreationOptions { TryToLoadPdbFromDisk = false };
			if (pdb is not null)
				options.PdbFileOrData = pdb;
			return ModuleDefMD.Load(image, options);
		}

		// Reads what the tests below create: types, members, bodies, custom attributes, debug info, resources
		static void UseEverything(ModuleDefMD module) {
			long count = 0;
			foreach (var type in module.GetTypes()) {
				count += type.Fields.Count + type.Interfaces.Count + type.GenericParameters.Count;
				count += CountCustomAttributeArguments(type);
				foreach (var method in type.Methods) {
					count += CountCustomAttributeArguments(method);
					count += method.GenericParameters.Count;
					var body = method.Body;
					if (body is null)
						continue;
					count += body.Variables.Count + body.ExceptionHandlers.Count;
					foreach (var instruction in body.Instructions) {
						if (instruction.SequencePoint is not null)
							count++;
						if (instruction.Operand is not null)
							count++;
					}
					count += CountScopes(body.PdbMethod?.Scope);
				}
			}
			foreach (var resource in module.Resources) {
				if (resource is EmbeddedResource embedded)
					count += embedded.CreateReader().Length;
			}
			var win32Root = module.Win32Resources?.Root;
			if (win32Root is not null)
				count += win32Root.Directories.Sum(a => a.Directories.Sum(b => b.Data.Count));
			GC.KeepAlive(count);
		}

		static int CountCustomAttributeArguments(IHasCustomAttribute owner) {
			int count = 0;
			foreach (var attribute in owner.CustomAttributes)
				count += 1 + attribute.ConstructorArguments.Count;
			return count;
		}

		static int CountScopes(PdbScope? scope) {
			int count = 0;
			// Not recursive: the scopes can be nested very deeply
			while (scope is not null) {
				count += 1 + scope.Variables.Count + scope.Constants.Count;
				scope = scope.Scopes.FirstOrDefault();
			}
			return count;
		}

		static void AssertModuleGrowsLikeTheInput(Func<int, ModuleDef> createModule, int size, bool withPdb = false) {
			var small = MeasureModule(createModule, size, withPdb);
			var big = MeasureModule(createModule, size * 4, withPdb);

			AssertGrowsLikeTheInput("Writing the created module", size, small.Write, big.Write);
			AssertGrowsLikeTheInput("Reading the module", size, small.Read, big.Read);
			AssertGrowsLikeTheInput("Writing the module that was read", size, small.WritePreserved, big.WritePreserved);
		}

		// An empty module with one class 'Ns.Main'. 'main' is the class.
		static ModuleDefUser CreateModule(out TypeDefUser main) {
			var module = new ModuleDefUser("Growth.dll") { Kind = ModuleKind.Dll };
			new AssemblyDefUser("Growth", new Version(1, 0, 0, 0)).Modules.Add(module);
			main = new TypeDefUser("Ns", "Main", module.CorLibTypes.Object.TypeDefOrRef);
			module.Types.Add(main);
			return module;
		}

		static MethodDefUser AddMethod(ModuleDef module, TypeDef type, string name) {
			var method = new MethodDefUser(name, MethodSig.CreateStatic(module.CorLibTypes.Void), MethodImplAttributes.IL, MethodAttributes.Public | MethodAttributes.Static);
			method.Body = new CilBody();
			method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
			type.Methods.Add(method);
			return method;
		}

		[Fact]
		public void Types() => AssertModuleGrowsLikeTheInput(size => {
			var module = CreateModule(out _);
			for (int i = 0; i < size; i++) {
				var type = new TypeDefUser("Ns", "Type" + i, module.CorLibTypes.Object.TypeDefOrRef);
				module.Types.Add(type);
				AddMethod(module, type, "Method");
			}
			return module;
		}, 5000);

		[Fact]
		public void NestedTypesOfOneType() => AssertModuleGrowsLikeTheInput(size => {
			var module = CreateModule(out var main);
			for (int i = 0; i < size; i++)
				main.NestedTypes.Add(new TypeDefUser("Nested" + i, module.CorLibTypes.Object.TypeDefOrRef) { Attributes = TypeAttributes.NestedPublic });
			return module;
		}, 5000);

		[Fact]
		public void MethodsAndFieldsOfOneType() => AssertModuleGrowsLikeTheInput(size => {
			var module = CreateModule(out var main);
			for (int i = 0; i < size; i++) {
				var method = AddMethod(module, main, "Method" + i);
				var field = new FieldDefUser("Field" + i, new FieldSig(module.CorLibTypes.Int32), FieldAttributes.Public | FieldAttributes.Static);
				main.Fields.Add(field);
				// Every method uses a field and calls the previous method
				method.Body!.Instructions.Insert(0, OpCodes.Ldsfld.ToInstruction(field));
				method.Body.Instructions.Insert(1, OpCodes.Pop.ToInstruction());
				if (i > 0)
					method.Body.Instructions.Insert(2, OpCodes.Call.ToInstruction(main.Methods[i - 1]));
			}
			return module;
		}, 5000);

		[Fact]
		public void InstructionsWithBranches() => AssertModuleGrowsLikeTheInput(size => {
			var module = CreateModule(out var main);
			var instructions = AddMethod(module, main, "Method").Body!.Instructions;
			var ret = instructions[0];
			instructions.Clear();
			// Every third instruction is a branch to the end of the method
			for (int i = 0; i < size; i++) {
				instructions.Add(OpCodes.Ldc_I4.ToInstruction(i));
				instructions.Add(OpCodes.Brtrue.ToInstruction(ret));
				instructions.Add(OpCodes.Nop.ToInstruction());
			}
			instructions.Add(ret);
			return module;
		}, 20000);

		[Fact]
		public void ExceptionHandlers() => AssertModuleGrowsLikeTheInput(size => {
			var module = CreateModule(out var main);
			var body = AddMethod(module, main, "Method").Body!;
			var ret = body.Instructions[0];
			body.Instructions.Clear();
			// try { } finally { }, one after another
			for (int i = 0; i < size; i++) {
				var tryStart = OpCodes.Leave.ToInstruction(ret);
				var handlerStart = OpCodes.Endfinally.ToInstruction();
				body.Instructions.Add(tryStart);
				body.Instructions.Add(handlerStart);
				body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Finally) { TryStart = tryStart, TryEnd = handlerStart, HandlerStart = handlerStart });
			}
			body.Instructions.Add(ret);
			for (int i = 0; i < size; i++) {
				var nextTry = i + 1 < size ? body.ExceptionHandlers[i + 1].TryStart : ret;
				body.ExceptionHandlers[i].HandlerEnd = nextTry;
				body.ExceptionHandlers[i].TryStart!.Operand = nextTry;
			}
			return module;
		}, 5000);

		[Fact]
		public void Locals() => AssertModuleGrowsLikeTheInput(size => {
			var module = CreateModule(out var main);
			var body = AddMethod(module, main, "Method").Body!;
			for (int i = 0; i < size; i++) {
				var local = body.Variables.Add(new Local(module.CorLibTypes.Int32));
				body.Instructions.Insert(0, OpCodes.Ldloc.ToInstruction(local));
				body.Instructions.Insert(1, OpCodes.Pop.ToInstruction());
			}
			return module;
		}, 10000);

		// Fixed in 9e3b93aa: every type in a custom attribute was looked up by checking all types of the module
		[Fact]
		public void CustomAttributesWithATypeArgument() => AssertModuleGrowsLikeTheInput(size => {
			var module = CreateModule(out var main);
			var corLibTypes = module.CorLibTypes;
			var systemType = new ClassSig(corLibTypes.GetTypeRef("System", "Type"));
			var attributeType = corLibTypes.GetTypeRef("System.Runtime.CompilerServices", "AsyncStateMachineAttribute");
			var constructor = new MemberRefUser(module, ".ctor", MethodSig.CreateInstance(corLibTypes.Void, systemType), attributeType);
			for (int i = 0; i < size; i++) {
				var type = new TypeDefUser("Ns", "Type" + i, corLibTypes.Object.TypeDefOrRef);
				module.Types.Add(type);
				var attribute = new CustomAttribute(constructor);
				attribute.ConstructorArguments.Add(new CAArgument(systemType, type.ToTypeSig()));
				AddMethod(module, type, "Method").CustomAttributes.Add(attribute);
			}
			return module;
		}, 5000);

		[Fact]
		public void CustomAttributesOfOneMember() => AssertModuleGrowsLikeTheInput(size => {
			var module = CreateModule(out var main);
			var corLibTypes = module.CorLibTypes;
			var attributeType = corLibTypes.GetTypeRef("System", "ObsoleteAttribute");
			var constructor = new MemberRefUser(module, ".ctor", MethodSig.CreateInstance(corLibTypes.Void, corLibTypes.String), attributeType);
			for (int i = 0; i < size; i++) {
				var attribute = new CustomAttribute(constructor);
				attribute.ConstructorArguments.Add(new CAArgument(corLibTypes.String, new UTF8String("message " + i)));
				main.CustomAttributes.Add(attribute);
			}
			return module;
		}, 10000);

		[Fact]
		public void InterfacesAndGenericParametersOfOneType() => AssertModuleGrowsLikeTheInput(size => {
			var module = CreateModule(out var main);
			for (int i = 0; i < size; i++) {
				var interfaceType = new TypeDefUser("Ns", "IInterface" + i) { Attributes = TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.Public };
				module.Types.Add(interfaceType);
				main.Interfaces.Add(new InterfaceImplUser(interfaceType));
				main.GenericParameters.Add(new GenericParamUser((ushort)i, GenericParamAttributes.NonVariant, "T" + i));
			}
			return module;
		}, 3000);

		[Fact]
		public void Strings() => AssertModuleGrowsLikeTheInput(size => {
			var module = CreateModule(out var main);
			var instructions = AddMethod(module, main, "Method").Body!.Instructions;
			for (int i = 0; i < size; i++) {
				instructions.Insert(0, OpCodes.Ldstr.ToInstruction("a string that's used once, number " + i));
				instructions.Insert(1, OpCodes.Pop.ToInstruction());
			}
			return module;
		}, 10000);

		[Fact]
		public void Resources() => AssertModuleGrowsLikeTheInput(size => {
			var module = CreateModule(out _);
			for (int i = 0; i < size; i++)
				module.Resources.Add(new EmbeddedResource("Resource" + i, new byte[] { (byte)i, 2, 3 }, ManifestResourceAttributes.Public));
			return module;
		}, 5000);

		[Fact]
		public void Win32Resources() => AssertModuleGrowsLikeTheInput(size => {
			var module = CreateModule(out _);
			var typeDirectory = new ResourceDirectoryUser(10);
			for (int i = 0; i < size; i++) {
				var nameDirectory = new ResourceDirectoryUser(i + 1);
				var data = new byte[] { (byte)i, 1, 2, 3 };
				nameDirectory.Data.Add(new ResourceData(0, ByteArrayDataReaderFactory.Create(data, filename: null), 0, (uint)data.Length));
				typeDirectory.Directories.Add(nameDirectory);
			}
			var root = new ResourceDirectoryUser(0);
			root.Directories.Add(typeDirectory);
			module.Win32Resources = new Win32ResourcesUser { Root = root };
			return module;
		}, 5000);

		static PdbDocument AddDocument(ModuleDef module, string name) {
			if (module.PdbState is null)
				module.CreatePdbState(PdbFileKind.PortablePDB);
			return module.PdbState!.Add(new PdbDocument(name, PdbDocumentConstants.LanguageCSharp, PdbDocumentConstants.LanguageVendorMicrosoft, PdbDocumentConstants.DocumentTypeText, Guid.Empty, Array.Empty<byte>()));
		}

		[Fact]
		public void SequencePoints() => AssertModuleGrowsLikeTheInput(size => {
			var module = CreateModule(out var main);
			var document = AddDocument(module, "Source.cs");
			var instructions = AddMethod(module, main, "Method").Body!.Instructions;
			for (int i = 0; i < size; i++) {
				var instruction = OpCodes.Nop.ToInstruction();
				instruction.SequencePoint = new SequencePoint(document, i + 1, 1, i + 1, 10);
				instructions.Insert(0, instruction);
			}
			return module;
		}, 20000, withPdb: true);

		// Fixed in db6bbf7c: the reader looked for the first and last instruction of every scope from the
		// start of the method
		[Fact]
		public void ScopesOneAfterAnother() => AssertModuleGrowsLikeTheInput(size => {
			var module = CreateModule(out var main);
			AddDocument(module, "Source.cs");
			var body = AddMethod(module, main, "Method").Body!;
			var ret = body.Instructions[0];
			var rootScope = new PdbScope { Start = null, End = null };
			body.PdbMethod = new PdbMethod { Scope = rootScope };
			for (int i = 0; i < size; i++) {
				var start = OpCodes.Nop.ToInstruction();
				body.Instructions.Insert(body.Instructions.Count - 1, start);
				body.Instructions.Insert(body.Instructions.Count - 1, OpCodes.Nop.ToInstruction());
				var scope = new PdbScope { Start = start };
				scope.Constants.Add(new PdbConstant("Constant" + i, module.CorLibTypes.Int32, i));
				rootScope.Scopes.Add(scope);
				if (i > 0)
					rootScope.Scopes[i - 1].End = start;
			}
			rootScope.Scopes[size - 1].End = ret;
			rootScope.Start = body.Instructions[0];
			return module;
		}, 5000, withPdb: true);

		[Fact]
		public void ScopesInsideEachOther() => AssertModuleGrowsLikeTheInput(size => {
			var module = CreateModule(out var main);
			AddDocument(module, "Source.cs");
			var body = AddMethod(module, main, "Method").Body!;
			var ret = body.Instructions[0];
			for (int i = 0; i < size; i++)
				body.Instructions.Insert(0, OpCodes.Nop.ToInstruction());
			// Scope i starts at instruction i and ends at the end of the method
			var rootScope = new PdbScope { Start = body.Instructions[0], End = null };
			body.PdbMethod = new PdbMethod { Scope = rootScope };
			var parent = rootScope;
			for (int i = 1; i < size; i++) {
				var scope = new PdbScope { Start = body.Instructions[i], End = null };
				scope.Constants.Add(new PdbConstant("Constant" + i, module.CorLibTypes.Int32, i));
				parent.Scopes.Add(scope);
				parent = scope;
			}
			GC.KeepAlive(ret);
			return module;
		}, 2000, withPdb: true);

		// Fixed in d45d9c27: every byte of the flags was written with a loop over all flags
		[Fact]
		public void DynamicLocalFlags() => AssertModuleGrowsLikeTheInput(size => {
			var module = CreateModule(out var main);
			AddDocument(module, "Source.cs");
			var body = AddMethod(module, main, "Method").Body!;
			var local = body.Variables.Add(new Local(module.CorLibTypes.Object));
			var flags = new bool[size];
			for (int i = 0; i < flags.Length; i += 3)
				flags[i] = true;
			flags[flags.Length - 1] = true;
			var pdbLocal = new PdbLocal(local, "dynamicLocal", PdbLocalAttributes.None);
			pdbLocal.CustomDebugInfos.Add(new PdbDynamicLocalVariablesCustomDebugInfo(flags));
			var rootScope = new PdbScope { Start = body.Instructions[0], End = null };
			rootScope.Variables.Add(pdbLocal);
			body.PdbMethod = new PdbMethod { Scope = rootScope };
			return module;
		}, 200000, withPdb: true);

		// Fixed in 27fd6c2c: the Windows PDB writer looked at all instructions of a method once for every
		// document the method uses. Only on Windows: the writer uses a native library.
		[Fact]
		public void SequencePointsInManyDocumentsInAWindowsPdb() {
			Assert.SkipUnless(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows),
				"A Windows PDB can only be written on Windows");

			Measurement MeasureWrite(int size) {
				var module = CreateModule(out var main);
				module.CreatePdbState(PdbFileKind.WindowsPDB);
				var instructions = AddMethod(module, main, "Method").Body!.Instructions;
				// Every instruction is in another source file
				for (int i = 0; i < size; i++) {
					var instruction = OpCodes.Nop.ToInstruction();
					instruction.SequencePoint = new SequencePoint(AddDocument(module, "Source" + i + ".cs"), i + 1, 1, i + 1, 10);
					instructions.Insert(instructions.Count - 1, instruction);
				}
				return Measure(() => Write(module, withPdb: true, preserve: false, out _, out _));
			}

			const int size = 2000;
			AssertGrowsLikeTheInput("Writing the Windows PDB", size, MeasureWrite(size), MeasureWrite(size * 4));
		}
	}
}
