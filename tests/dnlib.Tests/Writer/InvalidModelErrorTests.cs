// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// The API accepts modules that can't be written as they are (a constant whose value doesn't have the
	/// constant's type, a loop of exported types, ...). The writer reports each of these as an error
	/// (a <see cref="ModuleWriterException"/> by default), and if the logger ignores the error, it still writes a
	/// file that can be read.
	/// </summary>
	public class InvalidModelErrorTests {
		readonly CreatedModule created = new CreatedModule();

		ICorLibTypes Types => created.Module.CorLibTypes;

		// A method body of a kind that the writer doesn't know
		sealed class UnknownMethodBody : dnlib.DotNet.Emit.MethodBody {
		}

		// Changes the module so that it can't be written
		void MakeInvalid(string what) {
			var module = created.Module;
			switch (what) {
			case "constant whose value is not of its type":
				created.AddField("Field", Types.Int32).Constant = new ConstantUser("text", ElementType.I4);
				break;
			case "constant of a type that can't be a constant":
				created.AddField("Field", Types.Object).Constant = new ConstantUser(1.5m, ElementType.Class);
				break;
			case "initial value with another size than the field": {
				var field = created.AddField("Field", Types.Int32);
				field.InitialValue = new byte[] { 1, 2, 3 };
				field.Attributes |= FieldAttributes.HasFieldRVA;
				break;
			}
			case "exported types that are nested in each other": {
				var first = new ExportedTypeUser(module, 0, "Ns", "First", TypeAttributes.NestedPublic, null);
				var second = new ExportedTypeUser(module, 0, "Ns", "Second", TypeAttributes.NestedPublic, first);
				first.Implementation = second;
				module.ExportedTypes.Add(first);
				break;
			}
			case "instruction with an operand that has no token":
				created.AddMethod("Method", new Instruction(OpCodes.Ldtoken, new object()), OpCodes.Pop.ToInstruction(), OpCodes.Ret.ToInstruction());
				break;
			case "method body of an unknown kind":
				created.AddMethod("Method").MethodBody = new UnknownMethodBody();
				break;
			default:
				throw new ArgumentException(what);
			}
		}

		[Theory]
		[InlineData("constant whose value is not of its type", "Constant value's type is the wrong type")]
		[InlineData("constant of a type that can't be a constant", "Invalid constant type")]
		[InlineData("initial value with another size than the field", "initial value size != size of field type")]
		[InlineData("exported types that are nested in each other", "infinite Implementation loop")]
		[InlineData("instruction with an operand that has no token", "Invalid instruction operand")]
		[InlineData("method body of an unknown kind", "Unsupported method body")]
		public void ModuleThatCanNotBeWrittenAsItIsIsAnError(string what, string expectedError) {
			MakeInvalid(what);

			var exception = Assert.Throws<ModuleWriterException>(() => created.Write());
			Assert.Contains(expectedError, exception.Message);

			// With a logger that doesn't throw, a file is written and can be read
			var stream = new MemoryStream();
			created.Module.Write(stream, new ModuleWriterOptions(created.Module) { Logger = DummyLogger.NoThrowInstance });
			using (var module = ModuleDefMD.Load(stream.ToArray())) {
				var type = CreatedModule.GetType(module);
				Assert.All(type.Fields, a => Assert.NotNull(a.FieldSig));
				Assert.All(type.Methods, a => Assert.NotNull(a.MethodSig));
			}
		}
	}
}
