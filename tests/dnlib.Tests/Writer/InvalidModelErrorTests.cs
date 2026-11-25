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
			case "method without a signature":
				created.AddMethod("Method", OpCodes.Ret.ToInstruction()).Signature = null;
				break;
			case "method with 65536 locals": {
				var body = created.AddMethod("Method", OpCodes.Ret.ToInstruction()).Body!;
				for (int i = 0; i < 0x10000; i++)
					body.Variables.Add(new Local(Types.Int32));
				break;
			}
			case "marshal type with an unknown native type":
				created.AddField("Field", Types.Int32).MarshalType = new MarshalType((NativeType)0x12345);
				break;
			case "custom marshaler without its values":
				created.AddField("Field", Types.Int32).MarshalType = new MarshalType(NativeType.CustomMarshaler);
				break;
			case "raw marshal blob without its data":
				created.AddField("Field", Types.Int32).MarshalType = new MarshalType(NativeType.RawBlob);
				break;
			case "safe array with a type name but no variant type":
				created.AddField("Field", Types.Int32).MarshalType = new SafeArrayMarshalType(VariantType.NotInitialized, created.Type);
				break;
			case "security attribute without a type": {
				var declSecurity = new DeclSecurityUser { Action = SecurityAction.Demand };
				declSecurity.SecurityAttributes.Add(new SecurityAttribute(null));
				created.Type.DeclSecurities.Add(declSecurity);
				break;
			}
			case "property whose getter is in no type": {
				var property = new PropertyDefUser("Property", PropertySig.CreateStatic(Types.Int32));
				property.GetMethod = new MethodDefUser("get_Property", MethodSig.CreateStatic(Types.Int32));
				created.Type.Properties.Add(property);
				break;
			}
			case "type references that are nested in each other": {
				// More than a few of them: the writer then finds the loop with a hash set
				var first = new TypeRefUser(module, "Ns", "Nested0");
				TypeRef last = first;
				for (int i = 1; i < 20; i++)
					last = new TypeRefUser(module, "Ns", "Nested" + i, last);
				first.ResolutionScope = last;
				created.AddField("Field", new ClassSig(first));
				break;
			}
			case "type reference that is nested in itself": {
				var typeRef = new TypeRefUser(module, "Ns", "Nested");
				typeRef.ResolutionScope = typeRef;
				created.AddField("Field", new ClassSig(typeRef));
				break;
			}
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
		[InlineData("method without a signature", "CallingConventionSig is null")]
		[InlineData("method with 65536 locals", "Too many locals")]
		[InlineData("marshal type with an unknown native type", "Invalid MarshalType.NativeType")]
		[InlineData("custom marshaler without its values", "a custom marshaler must be a CustomMarshalType")]
		[InlineData("raw marshal blob without its data", "a raw blob must be a RawMarshalType")]
		[InlineData("safe array with a type name but no variant type", "is valid even though a previous field was invalid")]
		[InlineData("security attribute without a type", "SecurityAttribute attribute type is null")]
		[InlineData("property whose getter is in no type", "is not defined in this module")]
		[InlineData("type references that are nested in each other", "infinite ResolutionScope loop")]
		[InlineData("type reference that is nested in itself", "infinite ResolutionScope loop")]
		public void ModuleThatCanNotBeWrittenAsItIsIsAnError(string what, string expectedError) {
			MakeInvalid(what);

			var exception = Assert.Throws<ModuleWriterException>(() => created.Write());
			Assert.Contains(expectedError, exception.Message);

			// With a logger that doesn't throw, a file is written and can be read
			var stream = new MemoryStream();
			created.Module.Write(stream, new ModuleWriterOptions(created.Module) { Logger = DummyLogger.NoThrowInstance });
			using (var module = ModuleDefMD.Load(stream.ToArray())) {
				var type = CreatedModule.GetType(module);
				Assert.Equal(created.Type.Fields.Count, type.Fields.Count);
				Assert.Equal(created.Type.Methods.Count, type.Methods.Count);
			}
		}

		// Most lists of a module can't have null: adding it throws
		[Fact]
		public void NullCanNotBeAddedToTheListsOfAModule() {
			var module = created.Module;
			var method = created.AddMethod("Method", OpCodes.Ret.ToInstruction());
			var parameter = new GenericParamUser(0, GenericParamAttributes.NonVariant, "T");

			Assert.Throws<ArgumentNullException>(() => module.Types.Add(null!));
			Assert.Throws<ArgumentNullException>(() => created.Type.NestedTypes.Add(null!));
			Assert.Throws<ArgumentNullException>(() => created.Type.Fields.Add(null!));
			Assert.Throws<ArgumentNullException>(() => created.Type.Methods.Add(null!));
			Assert.Throws<ArgumentNullException>(() => created.Type.Events.Add(null!));
			Assert.Throws<ArgumentNullException>(() => created.Type.Properties.Add(null!));
			Assert.Throws<ArgumentNullException>(() => created.Type.GenericParameters.Add(null!));
			Assert.Throws<ArgumentNullException>(() => created.Type.CustomAttributes.Add(null!));
			Assert.Throws<ArgumentNullException>(() => created.Type.Fields.Insert(0, null!));
			Assert.Throws<ArgumentNullException>(() => method.ParamDefs.Add(null!));
			Assert.Throws<ArgumentNullException>(() => method.Body!.Variables.Add(null!));
			Assert.Throws<ArgumentNullException>(() => parameter.GenericParamConstraints.Add(null!));
			Assert.Throws<ArgumentNullException>(() => module.ExportedTypes.Add(null!));
			Assert.Throws<ArgumentNullException>(() => module.Resources.Add(null!));
		}

		// The other lists can have null. The writer reports an error or leaves the null out, it never throws
		// another exception (eg. a NullReferenceException), and with a logger that doesn't throw the file can be read.
		// Fixed in this commit: null in the list of exception handlers threw a NullReferenceException.
		[Theory]
		[InlineData("exception handler", "Exception handler is null")]
		[InlineData("method override", "MethodDefOrRef is null")]
		[InlineData("custom attribute argument", "Custom attribute arguments count != method sig arguments count")]
		[InlineData("instruction", null)]
		[InlineData("security declaration", null)]
		[InlineData("interface", null)]
		[InlineData("other method of a property", null)]
		public void NullInAListIsAnErrorOrIsLeftOut(string what, string? expectedError) {
			var module = created.Module;
			var method = created.AddMethod("Method", OpCodes.Nop.ToInstruction(), OpCodes.Ret.ToInstruction());
			switch (what) {
			case "exception handler": method.Body!.ExceptionHandlers.Add(null!); break;
			case "method override": method.Overrides.Add(new MethodOverride(null, null)); break;
			case "custom attribute argument": {
				var constructor = new MemberRefUser(module, ".ctor", MethodSig.CreateInstance(Types.Void, Types.String), Types.GetTypeRef("System", "ObsoleteAttribute"));
				var attribute = new CustomAttribute(constructor);
				attribute.NamedArguments.Add(null!);
				created.Type.CustomAttributes.Add(attribute);
				break;
			}
			case "instruction": method.Body!.Instructions.Insert(0, null!); break;
			case "security declaration": created.Type.DeclSecurities.Add(null!); break;
			case "interface": created.Type.Interfaces.Add(null!); break;
			case "other method of a property": {
				var property = new PropertyDefUser("Property", PropertySig.CreateStatic(Types.Int32));
				created.Type.Properties.Add(property);
				property.OtherMethods.Add(null!);
				break;
			}
			default: throw new ArgumentException(what);
			}

			if (expectedError is null)
				created.Write();
			else
				Assert.Contains(expectedError, Assert.Throws<ModuleWriterException>(() => created.Write()).Message);

			var stream = new MemoryStream();
			module.Write(stream, new ModuleWriterOptions(module) { Logger = DummyLogger.NoThrowInstance });
			using (var written = ModuleDefMD.Load(stream.ToArray())) {
				var writtenMethod = CreatedModule.GetType(written).FindMethod("Method")!;
				Assert.Equal(2, writtenMethod.Body!.Instructions.Count);
				Assert.Empty(writtenMethod.Body.ExceptionHandlers);
			}
		}

		// A module where almost everything is missing: the writer reports the errors, and with a logger
		// that doesn't throw it still writes a file that dnlib reads
		[Fact]
		public void ModuleWithManyMissingPartsIsWrittenIfTheErrorsAreIgnored() {
			var module = dnlib.Tests.Model.ModuleWithMissingParts.Create();

			Assert.Throws<ModuleWriterException>(() => module.Write(new MemoryStream()));

			var errors = new System.Collections.Generic.List<string>();
			var logger = new ErrorCollector(errors);
			var stream = new MemoryStream();
			module.Write(stream, new ModuleWriterOptions(module) { Logger = logger, MetadataLogger = logger });
			Assert.True(errors.Count >= 5, string.Join("\n", errors));
			using (var written = ModuleDefMD.Load(stream.ToArray())) {
				var type = written.Find("Ns.Type", isReflectionName: true)!;
				Assert.Equal(2, type.Fields.Count);
				Assert.Equal(3, type.Methods.Count);
				Assert.Equal(3, written.Resources.Count);
				written.LoadEverything();
			}
		}

		sealed class ErrorCollector : ILogger {
			readonly System.Collections.Generic.List<string> errors;
			public ErrorCollector(System.Collections.Generic.List<string> errors) => this.errors = errors;
			public void Log(object? sender, LoggerEvent loggerEvent, string format, params object?[] args) {
				if (loggerEvent == LoggerEvent.Error)
					errors.Add(string.Format(format, args));
			}
			public bool IgnoresEvent(LoggerEvent loggerEvent) => loggerEvent != LoggerEvent.Error;
		}
	}
}
