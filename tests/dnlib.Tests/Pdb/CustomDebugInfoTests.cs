// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using dnlib.DotNet;
using dnlib.DotNet.Pdb;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Pdb {
	/// <summary>
	/// "Custom debug information" is everything in a PDB that isn't a sequence point, a scope, a local or an
	/// import: a row with the thing it belongs to (the module, a type, a method, a source file), a GUID that
	/// says what it is, and a blob. dnlib has a class for every kind it knows. The tests add one of each kind
	/// with the API, write a portable PDB and read it again.
	/// </summary>
	public class CustomDebugInfoTests {
		static CompiledAssembly ChangeAndWrite(Action<ModuleDefMD> change, Action<ModuleWriterOptions>? changeOptions = null) {
			using (var module = DebugInfoSource.Load()) {
				change(module);
				return RoundTrip.WriteWithPortablePdb(module, preserveMetadata: true, changeOptions);
			}
		}

		static T Single<T>(IList<PdbCustomDebugInfo> infos) where T : PdbCustomDebugInfo => Assert.Single(infos.OfType<T>());

		[Fact]
		public void ModuleInfos() {
			var mvid = new Guid("11111111-2222-3333-4444-555555555555");
			var sourceLink = System.Text.Encoding.UTF8.GetBytes("{\"documents\":{\"/_/*\":\"https://example.com/*\"}}");
			var written = ChangeAndWrite(module => {
				// The compiler wrote some of these: replace them
				module.CustomDebugInfos.Clear();
				module.CustomDebugInfos.Add(new PdbDefaultNamespaceCustomDebugInfo("Default.Namespace"));
				module.CustomDebugInfos.Add(new PdbSourceLinkCustomDebugInfo(sourceLink));
				var references = new PdbCompilationMetadataReferencesCustomDebugInfo();
				references.References.Add(new PdbCompilationMetadataReference("First.dll", "", PdbCompilationMetadataReferenceFlags.Assembly, 0x12345678, 0x9000, mvid));
				references.References.Add(new PdbCompilationMetadataReference("Second.dll", "alias1,alias2", PdbCompilationMetadataReferenceFlags.Assembly | PdbCompilationMetadataReferenceFlags.EmbedInteropTypes, 1, 2, Guid.Empty));
				module.CustomDebugInfos.Add(references);
				var options = new PdbCompilationOptionsCustomDebugInfo();
				options.Options.Add(new KeyValuePair<string, string>("language", "C#"));
				options.Options.Add(new KeyValuePair<string, string>("define", "DEBUG,TRACE"));
				options.Options.Add(new KeyValuePair<string, string>("empty", ""));
				module.CustomDebugInfos.Add(options);
			});

			using (var module = RoundTrip.LoadWithPdb(written.Image, written.Pdb!)) {
				var infos = module.CustomDebugInfos;
				Assert.Equal(4, infos.Count);
				Assert.Equal("Default.Namespace", Single<PdbDefaultNamespaceCustomDebugInfo>(infos).Namespace);
				Assert.Equal(sourceLink, Single<PdbSourceLinkCustomDebugInfo>(infos).FileBlob);

				var references = Single<PdbCompilationMetadataReferencesCustomDebugInfo>(infos).References;
				Assert.Equal(2, references.Count);
				Assert.Equal("First.dll", references[0].Name);
				Assert.Equal("", references[0].Aliases);
				Assert.Equal(PdbCompilationMetadataReferenceFlags.Assembly, references[0].Flags);
				Assert.Equal(0x12345678U, references[0].Timestamp);
				Assert.Equal(0x9000U, references[0].SizeOfImage);
				Assert.Equal(mvid, references[0].Mvid);
				Assert.Equal("alias1,alias2", references[1].Aliases);
				Assert.Equal(PdbCompilationMetadataReferenceFlags.Assembly | PdbCompilationMetadataReferenceFlags.EmbedInteropTypes, references[1].Flags);

				var options = Single<PdbCompilationOptionsCustomDebugInfo>(infos).Options;
				Assert.Equal(new[] { "language=C#", "define=DEBUG,TRACE", "empty=" }, options.Select(a => a.Key + "=" + a.Value));
			}
		}

		[Fact]
		public void SourceFileAndTypeInfos() {
			var embeddedSource = new byte[] { 0, 0, 0, 0, (byte)'c', (byte)'o', (byte)'d', (byte)'e' };
			var primaryConstructor = new byte[] { 1, 2, 3 };
			var written = ChangeAndWrite(module => {
				var document = module.PdbState!.Documents.Single();
				document.CustomDebugInfos.Clear();
				document.CustomDebugInfos.Add(new PdbEmbeddedSourceCustomDebugInfo(embeddedSource));

				var type = module.Find("Code", isReflectionName: true)!;
				type.CustomDebugInfos.Clear();
				var documents = new PdbTypeDefinitionDocumentsDebugInfo();
				documents.Documents.Add(document);
				type.CustomDebugInfos.Add(documents);
				type.CustomDebugInfos.Add(new PrimaryConstructorInformationBlobDebugInfo(primaryConstructor));
			});

			using (var module = RoundTrip.LoadWithPdb(written.Image, written.Pdb!)) {
				var document = module.PdbState!.Documents.Single();
				Assert.Equal(embeddedSource, Single<PdbEmbeddedSourceCustomDebugInfo>(document.CustomDebugInfos).SourceCodeBlob);

				var type = module.Find("Code", isReflectionName: true)!;
				Assert.Same(document, Assert.Single(Single<PdbTypeDefinitionDocumentsDebugInfo>(type.CustomDebugInfos).Documents));
				Assert.Equal(primaryConstructor, Single<PrimaryConstructorInformationBlobDebugInfo>(type.CustomDebugInfos).Blob);
			}
		}

		// A kind that dnlib doesn't know keeps its GUID and its bytes
		[Fact]
		public void UnknownKindKeepsItsGuidAndData() {
			var guid = new Guid("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");
			var data = new byte[] { 9, 8, 7, 6, 5 };
			var written = ChangeAndWrite(module => {
				DebugInfoSource.GetMethod(module, "Sum").CustomDebugInfos.Add(new PdbUnknownCustomDebugInfo(guid, data));
				module.CustomDebugInfos.Add(new PdbUnknownCustomDebugInfo(guid, new byte[0]));
			});

			using (var module = RoundTrip.LoadWithPdb(written.Image, written.Pdb!)) {
				var info = Single<PdbUnknownCustomDebugInfo>(DebugInfoSource.GetMethod(module, "Sum").CustomDebugInfos);
				Assert.Equal(PdbCustomDebugInfoKind.Unknown, info.Kind);
				Assert.Equal(guid, info.Guid);
				Assert.Equal(data, info.Data);
				Assert.Empty(Single<PdbUnknownCustomDebugInfo>(module.CustomDebugInfos).Data);
			}
			// The same row read with System.Reflection.Metadata
			using (var provider = MetadataReaderProvider.FromPortablePdbImage(ImmutableArray.Create(written.Pdb))) {
				var reader = provider.GetMetadataReader();
				var rows = reader.CustomDebugInformation.Select(reader.GetCustomDebugInformation).Where(a => reader.GetGuid(a.Kind) == guid).ToList();
				Assert.Equal(2, rows.Count);
				Assert.Contains(rows, a => a.Parent.Kind == HandleKind.MethodDefinition && reader.GetBlobBytes(a.Value).SequenceEqual(data));
				Assert.Contains(rows, a => a.Parent.Kind == HandleKind.ModuleDefinition);
			}
		}

		[Fact]
		public void StateMachineStates() {
			var written = ChangeAndWrite(module => {
				var method = DebugInfoSource.GetMethod(module, "Sum");
				var states = new PdbEditAndContinueStateMachineStateMapDebugInfo();
				states.StateMachineStates.Add(new StateMachineStateInfo(10, (StateMachineState)0));
				states.StateMachineStates.Add(new StateMachineStateInfo(-5, (StateMachineState)(-3)));
				states.StateMachineStates.Add(new StateMachineStateInfo(300, (StateMachineState)1));
				method.CustomDebugInfos.Add(states);
			});

			using (var module = RoundTrip.LoadWithPdb(written.Image, written.Pdb!)) {
				var states = Single<PdbEditAndContinueStateMachineStateMapDebugInfo>(DebugInfoSource.GetMethod(module, "Sum").CustomDebugInfos).StateMachineStates;
				// They're stored in the order of their syntax offsets
				Assert.Equal(new[] { "-5:-3", "10:0", "300:1" }, states.Select(a => $"{a.SyntaxOffset}:{(int)a.State}"));
			}
		}

		// The steps of an async method: the yield instruction is in the method, the breakpoint (where it continues)
		// can be in another method (the MoveNext() of its state machine)
		[Fact]
		public void AsyncStepsWithABreakpointInAnotherMethod() {
			var written = ChangeAndWrite(module => {
				var method = DebugInfoSource.GetMethod(module, "Sum");
				var other = DebugInfoSource.GetMethod(module, "NoLocals");
				var info = new PdbAsyncMethodCustomDebugInfo(method);
				info.CatchHandlerInstruction = method.Body!.Instructions[1];
				info.StepInfos.Add(new PdbAsyncStepInfo(method.Body.Instructions[0], other, other.Body!.Instructions[other.Body.Instructions.Count - 1]));
				info.StepInfos.Add(new PdbAsyncStepInfo(method.Body.Instructions[1], method, method.Body.Instructions[2]));
				method.CustomDebugInfos.Add(info);
			});

			using (var module = RoundTrip.LoadWithPdb(written.Image, written.Pdb!)) {
				var method = DebugInfoSource.GetMethod(module, "Sum");
				var other = DebugInfoSource.GetMethod(module, "NoLocals");
				var info = Single<PdbAsyncMethodCustomDebugInfo>(method.CustomDebugInfos);
				Assert.Same(method.Body!.Instructions[1], info.CatchHandlerInstruction);
				Assert.Equal(2, info.StepInfos.Count);
				Assert.Same(other, info.StepInfos[0].BreakpointMethod);
				Assert.Same(other.Body!.Instructions[other.Body.Instructions.Count - 1], info.StepInfos[0].BreakpointInstruction);
				Assert.Same(method.Body.Instructions[2], info.StepInfos[1].BreakpointInstruction);
			}
		}

		// Custom debug info that only a Windows PDB can store is an error when a portable PDB is written
		[Fact]
		public void WindowsPdbOnlyKindIsAnErrorInAPortablePdb() {
			var exception = Assert.Throws<ModuleWriterException>(() => ChangeAndWrite(module =>
				DebugInfoSource.GetMethod(module, "Sum").CustomDebugInfos.Add(new PdbUsingGroupsCustomDebugInfo())));
			Assert.Contains("UsingGroups", exception.Message);

			// If the error is ignored, the rest is written
			var written = ChangeAndWrite(module => DebugInfoSource.GetMethod(module, "Sum").CustomDebugInfos.Add(new PdbUsingGroupsCustomDebugInfo()),
				options => options.Logger = DummyLogger.NoThrowInstance);
			using (var module = RoundTrip.LoadWithPdb(written.Image, written.Pdb!))
				Assert.DoesNotContain(DebugInfoSource.GetMethod(module, "Sum").CustomDebugInfos, a => a is PdbUsingGroupsCustomDebugInfo);
		}

		// A name with a zero char can't be stored: the names of the references end at a zero byte
		[Fact]
		public void MetadataReferenceNameWithAZeroCharIsAnError() {
			var exception = Assert.Throws<ModuleWriterException>(() => ChangeAndWrite(module => {
				var references = new PdbCompilationMetadataReferencesCustomDebugInfo();
				references.References.Add(new PdbCompilationMetadataReference("Na\0me.dll", "", PdbCompilationMetadataReferenceFlags.Assembly, 0, 0, Guid.Empty));
				module.CustomDebugInfos.Clear();
				module.CustomDebugInfos.Add(references);
			}));
			Assert.Contains("NUL", exception.Message);
		}

		// Custom debug info that the API accepts but that can't be written: something is null, or an instruction
		// isn't in the method
		static void AddInvalidInfo(ModuleDefMD module, string what) {
			var method = DebugInfoSource.GetMethod(module, "Sum");
			var instructions = method.Body!.Instructions;
			var instructionOfAnotherMethod = dnlib.DotNet.Emit.OpCodes.Nop.ToInstruction();
			switch (what) {
			case "compiler option without a key": {
				var options = new PdbCompilationOptionsCustomDebugInfo();
				options.Options.Add(new KeyValuePair<string, string>(null!, "value"));
				module.CustomDebugInfos.Add(options);
				break;
			}
			case "compiler option without a value": {
				var options = new PdbCompilationOptionsCustomDebugInfo();
				options.Options.Add(new KeyValuePair<string, string>("key", null!));
				module.CustomDebugInfos.Add(options);
				break;
			}
			case "metadata reference without a name": {
				var references = new PdbCompilationMetadataReferencesCustomDebugInfo();
				references.References.Add(new PdbCompilationMetadataReference(null!, "", PdbCompilationMetadataReferenceFlags.Assembly, 0, 0, Guid.Empty));
				module.CustomDebugInfos.Add(references);
				break;
			}
			case "metadata reference without aliases": {
				var references = new PdbCompilationMetadataReferencesCustomDebugInfo();
				references.References.Add(new PdbCompilationMetadataReference("Name.dll", null!, PdbCompilationMetadataReferenceFlags.Assembly, 0, 0, Guid.Empty));
				module.CustomDebugInfos.Add(references);
				break;
			}
			case "no default namespace":
				module.CustomDebugInfos.Add(new PdbDefaultNamespaceCustomDebugInfo(null!));
				break;
			case "no source link data":
				module.CustomDebugInfos.Add(new PdbSourceLinkCustomDebugInfo(null!));
				break;
			case "no embedded source":
				module.PdbState!.Documents.First().CustomDebugInfos.Add(new PdbEmbeddedSourceCustomDebugInfo(null!));
				break;
			case "scope of a hoisted local without a start": {
				var scopes = new PdbStateMachineHoistedLocalScopesCustomDebugInfo();
				scopes.Scopes.Add(new StateMachineHoistedLocalScope(null, instructions[1]));
				method.CustomDebugInfos.Add(scopes);
				break;
			}
			case "scope of a hoisted local that ends before it starts": {
				var scopes = new PdbStateMachineHoistedLocalScopesCustomDebugInfo();
				scopes.Scopes.Add(new StateMachineHoistedLocalScope(instructions[2], instructions[0]));
				method.CustomDebugInfos.Add(scopes);
				break;
			}
			case "async step without a yield instruction":
				AddAsyncStep(method, new PdbAsyncStepInfo(null!, method, instructions[0]));
				break;
			case "async step without a breakpoint method":
				AddAsyncStep(method, new PdbAsyncStepInfo(instructions[0], null!, instructions[0]));
				break;
			case "async step without a breakpoint instruction":
				AddAsyncStep(method, new PdbAsyncStepInfo(instructions[0], method, null!));
				break;
			case "async step with a breakpoint in a method without a body": {
				var noBody = new MethodDefUser("NoBody", MethodSig.CreateInstance(module.CorLibTypes.Void), MethodAttributes.Abstract | MethodAttributes.Virtual);
				method.DeclaringType!.Methods.Add(noBody);
				AddAsyncStep(method, new PdbAsyncStepInfo(instructions[0], noBody, instructionOfAnotherMethod));
				break;
			}
			case "async step with an instruction that the breakpoint method does not have": {
				var otherMethod = module.GetTypes().SelectMany(a => a.Methods).First(a => a.HasBody && a != method);
				AddAsyncStep(method, new PdbAsyncStepInfo(instructions[0], otherMethod, instructionOfAnotherMethod));
				break;
			}
			case "dynamic local variables without flags":
				method.CustomDebugInfos.Add(new PdbDynamicLocalVariablesCustomDebugInfo(null!));
				break;
			case "primary constructor information without a blob":
				method.DeclaringType!.CustomDebugInfos.Add(new PrimaryConstructorInformationBlobDebugInfo(null!));
				break;
			case "hoisted local scopes of a method without a body": {
				var noBody = new MethodDefUser("NoBody", MethodSig.CreateInstance(module.CorLibTypes.Void), MethodAttributes.Abstract | MethodAttributes.Virtual);
				method.DeclaringType!.Methods.Add(noBody);
				noBody.CustomDebugInfos.Add(new PdbStateMachineHoistedLocalScopesCustomDebugInfo());
				break;
			}
			case "async method information of a method without a body": {
				var noBody = new MethodDefUser("NoBody", MethodSig.CreateInstance(module.CorLibTypes.Void), MethodAttributes.Abstract | MethodAttributes.Virtual);
				method.DeclaringType!.Methods.Add(noBody);
				noBody.CustomDebugInfos.Add(new PdbAsyncMethodCustomDebugInfo(method));
				break;
			}
			default:
				throw new ArgumentException(what);
			}
		}

		static void AddAsyncStep(MethodDef method, PdbAsyncStepInfo step) {
			var info = new PdbAsyncMethodCustomDebugInfo(method);
			info.StepInfos.Add(step);
			method.CustomDebugInfos.Add(info);
		}

		[Theory]
		[InlineData("compiler option without a key", "Compiler option `key` is null")]
		[InlineData("compiler option without a value", "Compiler option `value` is null")]
		[InlineData("metadata reference without a name", "Metadata reference name is null")]
		[InlineData("metadata reference without aliases", "Metadata reference aliases is null")]
		[InlineData("no default namespace", "Default namespace is null")]
		[InlineData("no source link data", "Source link blob is null")]
		[InlineData("no embedded source", "Source code blob is null")]
		[InlineData("scope of a hoisted local without a start", "Instruction is null")]
		[InlineData("scope of a hoisted local that ends before it starts", "End instruction is before start instruction")]
		[InlineData("async step without a yield instruction", "YieldInstruction is null")]
		[InlineData("async step without a breakpoint method", "BreakpointMethod is null")]
		[InlineData("async step without a breakpoint instruction", "BreakpointInstruction is null")]
		[InlineData("async step with a breakpoint in a method without a body", "Method has no body")]
		[InlineData("async step with an instruction that the breakpoint method does not have", "Couldn't find an instruction")]
		[InlineData("dynamic local variables without flags", "Dynamic flags is null")]
		[InlineData("primary constructor information without a blob", "Primary constructor information blob is null")]
		[InlineData("hoisted local scopes of a method without a body", "Method has no body")]
		[InlineData("async method information of a method without a body", "Method has no body")]
		public void InfoThatCanNotBeWrittenIsAnError(string what, string expectedError) {
			var exception = Assert.Throws<ModuleWriterException>(() => ChangeAndWrite(module => AddInvalidInfo(module, what)));
			Assert.Contains(expectedError, exception.Message);

			// If the error is ignored, the module and the rest of the PDB are written
			var written = ChangeAndWrite(module => AddInvalidInfo(module, what), options => options.Logger = DummyLogger.NoThrowInstance);
			using (var module = RoundTrip.LoadWithPdb(written.Image, written.Pdb!))
				Assert.Contains(DebugInfoSource.GetMethod(module, "Sum").Body!.Instructions, a => a.SequencePoint is not null);
		}
	}
}
