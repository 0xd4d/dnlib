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
			using (var provider = MetadataReaderProvider.FromPortablePdbImage(ImmutableArray.Create(written.Pdb!))) {
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
	}
}
