// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using Xunit;
using FieldAttributes = dnlib.DotNet.FieldAttributes;
using MethodAttributes = dnlib.DotNet.MethodAttributes;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// Edit-and-continue metadata (<c>#-</c>) can't remove a row, so a deleted type or member keeps its row and
	/// gets a name that starts with <c>_Deleted</c>. A flag in the header of the tables says that there are
	/// such rows. The CLR and dnlib then skip them when the types and the members of a type are listed.
	/// Members also need the RTSpecialName flag to be deleted.
	///
	/// The tests rename things with dnlib, and write uncompressed metadata with the flag.
	/// </summary>
	public class DeletedRowTests {
		const string Source = @"
public class Kept {
	public int KeptField;
	public int _DeletedField;
	public int _DeletedFieldWithoutTheFlag;
	public void KeptMethod() { }
	public void _DeletedMethod() { }
	public void _DeletedMethodWithoutTheFlag() { }
}
public class _DeletedType { }
";

		static byte[] CreateImage(bool hasDeletedRows, Action<TypeDef>? changeKeptType = null) {
			using (var module = ModuleDefMD.Load(CSharpCompiler.Compile(Source).Image)) {
				var kept = module.Find("Kept", isReflectionName: true)!;
				kept.FindField("_DeletedField")!.Attributes |= FieldAttributes.RTSpecialName;
				kept.FindMethod("_DeletedMethod")!.Attributes |= MethodAttributes.RTSpecialName;
				changeKeptType?.Invoke(kept);

				var options = new ModuleWriterOptions(module);
				options.MetadataOptions.TablesHeapOptions.UseENC = true;
				options.MetadataOptions.TablesHeapOptions.HasDeletedRows = hasDeletedRows;
				var stream = new MemoryStream();
				module.Write(stream, options);
				return stream.ToArray();
			}
		}

		static string[] GetTypeNames(ModuleDef module) => module.Types.Select(a => a.Name.String).ToArray();
		static string[] GetFieldNames(ModuleDef module) => module.Find("Kept", isReflectionName: true)!.Fields.Select(a => a.Name.String).ToArray();
		static string[] GetMethodNames(ModuleDef module) => module.Find("Kept", isReflectionName: true)!.Methods.Select(a => a.Name.String).ToArray();

		[Fact]
		public void DeletedTypesAndMembersAreNotInTheLists() {
			using (var module = ModuleDefMD.Load(CreateImage(hasDeletedRows: true))) {
				Assert.False(module.Metadata.IsCompressed);
				Assert.True(module.TablesStream.HasDelete);

				// With .NET Framework references, the compiler adds types for attributes it needs
				Assert.Contains("Kept", GetTypeNames(module));
				Assert.DoesNotContain("_DeletedType", GetTypeNames(module));
				Assert.Equal(new[] { "KeptField", "_DeletedFieldWithoutTheFlag" }, GetFieldNames(module));
				Assert.Equal(new[] { "KeptMethod", "_DeletedMethodWithoutTheFlag", ".ctor" }, GetMethodNames(module));
			}
		}

		// The rows are still there and can be read by their tokens
		[Fact]
		public void DeletedRowsAreStillResolvedByToken() {
			using (var module = ModuleDefMD.Load(CreateImage(hasDeletedRows: true))) {
				Assert.Contains(Enumerable.Range(1, (int)module.TablesStream.TypeDefTable.Rows), rid => module.ResolveTypeDef((uint)rid)!.Name == "_DeletedType");
				Assert.Contains(Enumerable.Range(1, (int)module.TablesStream.FieldTable.Rows), rid => module.ResolveField((uint)rid)!.Name == "_DeletedField");
			}
		}

		[Fact]
		public void NothingIsDeletedIfTheHeaderFlagIsNotSet() {
			using (var module = ModuleDefMD.Load(CreateImage(hasDeletedRows: false))) {
				Assert.False(module.TablesStream.HasDelete);

				Assert.Contains("_DeletedType", GetTypeNames(module));
				Assert.Contains("_DeletedField", GetFieldNames(module));
				Assert.Contains("_DeletedMethod", GetMethodNames(module));
			}
		}

		// Mono doesn't use the header flag. It only skips a field whose name is exactly _Deleted and that has
		// the SpecialName and RTSpecialName flags.
		[Fact]
		public void MonoOnlyDeletesFieldsThatAreNamedDeleted() {
			var image = CreateImage(hasDeletedRows: false, kept => {
				var field = kept.FindField("KeptField")!;
				field.Name = "_Deleted";
				field.Attributes |= FieldAttributes.SpecialName | FieldAttributes.RTSpecialName;
			});

			using (var module = ModuleDefMD.Load(image, new ModuleCreationOptions(CLRRuntimeReaderKind.Mono))) {
				Assert.Contains("_DeletedType", GetTypeNames(module));
				Assert.Equal(new[] { "_DeletedField", "_DeletedFieldWithoutTheFlag" }, GetFieldNames(module));
				Assert.Contains("_DeletedMethod", GetMethodNames(module));
			}
		}

		// What the runtime that runs the tests does with the same file
		[Fact]
		public void RuntimeSkipsTheSameRows() {
			Assert.SkipWhen(Type.GetType("Mono.Runtime") is not null, "Mono has other rules, see MonoOnlyDeletesFieldsThatAreNamedDeleted");
			using (var loaded = new LoadedAssembly(CreateImage(hasDeletedRows: true))) {
				var kept = loaded.Assembly.GetType("Kept", throwOnError: true)!;
				const BindingFlags declared = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

				Assert.Equal(new[] { "KeptField", "_DeletedFieldWithoutTheFlag" }, kept.GetFields(declared).Select(a => a.Name).OrderBy(a => a, StringComparer.Ordinal));
				Assert.Equal(new[] { "KeptMethod", "_DeletedMethodWithoutTheFlag" }, kept.GetMethods(declared).Select(a => a.Name).OrderBy(a => a, StringComparer.Ordinal));
				Assert.DoesNotContain("_DeletedType", loaded.Assembly.GetTypes().Select(a => a.Name));
			}
		}
	}
}
