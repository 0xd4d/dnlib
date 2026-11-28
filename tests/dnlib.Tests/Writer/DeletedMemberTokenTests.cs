// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// If the members must keep their tokens (<see cref="MetadataFlags.PreserveRids"/>) and some members were
	/// removed, their rows can't be removed: the rows after them would get other numbers. The writer keeps the
	/// rows as unused members of a new type at the end of the TypeDef table, and writes pointer tables
	/// (FieldPtr, MethodPtr etc) so the other members are still found.
	/// </summary>
	public class DeletedMemberTokenTests {
		const string Source = @"
using System;

public class Program {
	public static int UnusedField;
	public static int UsedField = 5;
	public static void UnusedMethod(int firstUnusedParameter, int secondUnusedParameter) { }
	public static int UnusedProperty { get; set; }
	public static event Action UnusedEvent;

	public static int Main() {
		var after = new After { AfterField = UsedField, AfterProperty = 2 };
		after.AfterEvent += () => { };
		return after.AfterMethod(-7);
	}
}

public class After {
	public int AfterField;
	public int AfterProperty { get; set; }
	public event Action AfterEvent;
	public int AfterMethod(int value) { return AfterField + AfterProperty + value; }
}
";

		static readonly Lazy<byte[]> original = new Lazy<byte[]>(() =>
			CSharpCompiler.Compile(Source, new CSharpCompilerOptions { OutputKind = Microsoft.CodeAnalysis.OutputKind.ConsoleApplication }).Image);

		// Removes every member of Program whose name has "Unused" (with the accessors of the property and
		// the event) and writes the module
		static byte[] WriteWithoutTheUnusedMembers(MetadataFlags flags) {
			using (var module = ModuleDefMD.Load(original.Value)) {
				var program = module.Find("Program", isReflectionName: true)!;
				RemoveUnused(program.Fields);
				RemoveUnused(program.Methods);
				RemoveUnused(program.Properties);
				RemoveUnused(program.Events);

				var options = new ModuleWriterOptions(module);
				options.MetadataOptions.Flags |= flags;
				var stream = new MemoryStream();
				module.Write(stream, options);
				return stream.ToArray();
			}
		}

		static void RemoveUnused<T>(IList<T> members) where T : IFullName {
			for (int i = members.Count - 1; i >= 0; i--) {
				if (members[i].Name.Contains("Unused"))
					members.RemoveAt(i);
			}
		}

		// The tokens of the fields, methods, parameters, properties and events of a type. The key is the kind
		// of member and its name, eg. "FieldDef AfterField".
		static Dictionary<string, MDToken> GetMemberTokens(ModuleDef module, string typeName) {
			var type = module.Find(typeName, isReflectionName: true)!;
			var members = type.Fields.Cast<IMemberDef>().Concat(type.Methods).Concat(type.Properties).Concat(type.Events);
			var tokens = members.ToDictionary(a => a.MDToken.Table + " " + a.Name, a => a.MDToken);
			foreach (var method in type.Methods) {
				foreach (var parameter in method.ParamDefs)
					tokens.Add($"Param {method.Name}.{parameter.Name}", parameter.MDToken);
			}
			return tokens;
		}

		static int CountUnused(ModuleDef module, Table table) =>
			GetMemberTokens(module, "Program").Keys.Count(name => name.StartsWith(table + " ") && name.Contains("Unused"));

		static void RunUnlessMono(byte[] image) {
			// Mono doesn't load metadata with pointer tables
			if (Type.GetType("Mono.Runtime") is null)
				Assert.Equal(0, AssemblyRunner.RunEntryPoint(image));
		}

		[Fact]
		public void OriginalRuns() {
			Assert.Equal(0, AssemblyRunner.RunEntryPoint(original.Value));
			using (var module = ModuleDefMD.Load(original.Value))
				Assert.Contains("Field UnusedField", GetMemberTokens(module, "Program").Keys);
		}

		[Fact]
		public void MembersAfterTheRemovedOnesKeepTheirTokens() {
			var written = WriteWithoutTheUnusedMembers(MetadataFlags.PreserveRids);

			using (var originalModule = ModuleDefMD.Load(original.Value))
			using (var module = ModuleDefMD.Load(written)) {
				Assert.Equal(GetMemberTokens(originalModule, "After"), GetMemberTokens(module, "After"));

				var programMembers = GetMemberTokens(module, "Program");
				Assert.DoesNotContain(programMembers.Keys, name => name.Contains("Unused"));
				var originalProgramMembers = GetMemberTokens(originalModule, "Program");
				Assert.All(programMembers, member => Assert.Equal(originalProgramMembers[member.Key], member.Value));
			}
			RunUnlessMono(written);
		}

		[Fact]
		public void RowsOfTheRemovedMembersBelongToANewLastType() {
			var written = WriteWithoutTheUnusedMembers(MetadataFlags.PreserveRids);

			using (var originalModule = ModuleDefMD.Load(original.Value))
			using (var module = ModuleDefMD.Load(written)) {
				Assert.False(module.Metadata.IsCompressed);
				foreach (var table in new[] { Table.Field, Table.Param, Table.Property, Table.Event })
					Assert.Equal(originalModule.TablesStream.Get(table)!.Rows, module.TablesStream.Get(table)!.Rows);
				// An unused parameter row gets a new method
				Assert.True(module.TablesStream.MethodTable.Rows >= originalModule.TablesStream.MethodTable.Rows);
				Assert.Equal(originalModule.TablesStream.TypeDefTable.Rows + 1, module.TablesStream.TypeDefTable.Rows);

				var lastType = module.Types.Last();
				Assert.DoesNotContain(originalModule.Types, type => type.Name == lastType.Name);
				// The field, and the fields that the compiler created for the property and the event
				Assert.Equal(3, CountUnused(originalModule, Table.Field));
				Assert.Equal(3, lastType.Fields.Count);
				Assert.Single(lastType.Properties);
				Assert.Single(lastType.Events);
				Assert.True(lastType.Methods.Count >= CountUnused(originalModule, Table.Method));
			}
		}

		// Without the flag, the rows are removed and the members after them get other tokens
		[Fact]
		public void RowsAreRemovedIfTheTokensAreNotPreserved() {
			var written = WriteWithoutTheUnusedMembers(0);

			using (var originalModule = ModuleDefMD.Load(original.Value))
			using (var module = ModuleDefMD.Load(written)) {
				Assert.True(module.Metadata.IsCompressed);
				Assert.Equal(originalModule.TablesStream.FieldTable.Rows - 3, module.TablesStream.FieldTable.Rows);
				Assert.Equal(originalModule.TablesStream.TypeDefTable.Rows, module.TablesStream.TypeDefTable.Rows);
				Assert.NotEqual(GetMemberTokens(originalModule, "After"), GetMemberTokens(module, "After"));
			}
			Assert.Equal(0, AssemblyRunner.RunEntryPoint(written));
		}

		// A member that was removed but that an instruction still uses has no row: that's an error, with and without
		// the option that keeps the tokens. (An instruction can't use a parameter, an event or a property in a valid
		// method, but the API allows it.)
		[Theory]
		[InlineData("type", 0)]
		[InlineData("field", 0)]
		[InlineData("method", 0)]
		[InlineData("parameter", 0)]
		[InlineData("event", 0)]
		[InlineData("property", 0)]
		[InlineData("type", MetadataFlags.PreserveAll)]
		[InlineData("field", MetadataFlags.PreserveAll)]
		[InlineData("method", MetadataFlags.PreserveAll)]
		[InlineData("parameter", MetadataFlags.PreserveAll)]
		[InlineData("event", MetadataFlags.PreserveAll)]
		[InlineData("property", MetadataFlags.PreserveAll)]
		public void ARemovedMemberThatIsStillUsedIsAnError(string what, MetadataFlags flags) {
			using (var module = ModuleDefMD.Load(original.Value)) {
				var after = module.Find("After", isReflectionName: true)!;
				var main = module.Find("Program", isReflectionName: true)!.FindMethod("Main")!;
				var afterMethod = after.FindMethod("AfterMethod")!;
				IMDTokenProvider removed;
				switch (what) {
				case "type": removed = after; module.Types.Remove(after); break;
				case "field": removed = after.Fields[0]; after.Fields.RemoveAt(0); break;
				case "method": removed = afterMethod; after.Methods.Remove(afterMethod); break;
				case "parameter": removed = afterMethod.ParamDefs[0]; afterMethod.ParamDefs.RemoveAt(0); break;
				case "event": removed = after.Events[0]; after.Events.RemoveAt(0); break;
				case "property": removed = after.Properties[0]; after.Properties.RemoveAt(0); break;
				default: throw new ArgumentException(what);
				}
				main.Body!.Instructions.Insert(0, OpCodes.Pop.ToInstruction());
				main.Body.Instructions.Insert(0, new Instruction(OpCodes.Ldtoken, removed));

				var options = new ModuleWriterOptions(module);
				options.MetadataOptions.Flags |= flags;
				var exception = Assert.Throws<ModuleWriterException>(() => module.Write(new MemoryStream(), options));

				Assert.Contains("is not defined in this module", exception.Message);
			}
		}
	}
}
