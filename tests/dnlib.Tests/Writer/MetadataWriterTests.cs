// dnlib: See LICENSE.txt for more info

using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// Specific things the metadata writer must (or must not) do, each found as a bug at some time
	/// </summary>
	public class MetadataWriterTests {
		// Fixed in ef69c3d5: the string was encoded with Encoding.Unicode, which replaces a char like this
		[Theory]
		[InlineData("\uD800")]
		[InlineData("a\uDC00b")]
		[InlineData("😀")]
		[InlineData("")]
		[InlineData("text\0with null")]
		public void StringConstantIsWrittenCharByChar(string value) {
			var created = new CreatedModule();
			var field = created.AddField("Constant", created.Module.CorLibTypes.String);
			field.Attributes |= FieldAttributes.Literal;
			field.Constant = new ConstantUser(value);

			using (var module = created.WriteAndRead())
				Assert.Equal(value, CreatedModule.GetType(module).FindField("Constant")!.Constant!.Value);
		}

		// Fixed in 3360fab9: the writer cast the MarshalType to the class that has the native type's optional
		// fields and threw InvalidCastException
		[Theory]
		[InlineData(NativeType.IUnknown)]
		[InlineData(NativeType.IDispatch)]
		[InlineData(NativeType.IntF)]
		[InlineData(NativeType.FixedSysString)]
		[InlineData(NativeType.SafeArray)]
		[InlineData(NativeType.FixedArray)]
		[InlineData(NativeType.Array)]
		[InlineData(NativeType.Boolean)]
		public void MarshalTypeWithoutTheOptionalFieldsOfItsNativeTypeIsWritten(NativeType nativeType) {
			var created = new CreatedModule();
			created.AddField("Field", created.Module.CorLibTypes.Object).MarshalType = new MarshalType(nativeType);

			using (var module = created.WriteAndRead()) {
				var marshalType = CreatedModule.GetType(module).FindField("Field")!.MarshalType;

				Assert.NotNull(marshalType);
				Assert.Equal(nativeType, marshalType.NativeType);
			}
		}

		// Fixed in 1fafd7da: the ParamDefs were sorted with a sort that isn't stable
		[Fact]
		public void ParamDefsWithTheSameSequenceKeepTheirOrder() {
			var created = new CreatedModule();
			var method = created.AddMethod("M", OpCodes.Ret.ToInstruction());
			// Enough of them that an unstable sort changes the order
			var names = Enumerable.Range(0, 40).Select(a => "p" + a).ToArray();
			foreach (var name in names)
				method.ParamDefs.Add(new ParamDefUser(name, 1));

			using (var module = created.WriteAndRead()) {
				var written = CreatedModule.GetType(module).FindMethod("M")!;

				Assert.Equal(names, written.ParamDefs.Select(a => a.Name.String).ToArray());
			}
		}

		[Fact]
		public void ParamDefsAreSortedBySequence() {
			var created = new CreatedModule();
			var method = created.AddMethod("M", OpCodes.Ret.ToInstruction());
			method.MethodSig!.Params.Add(created.Module.CorLibTypes.Int32);
			method.MethodSig.Params.Add(created.Module.CorLibTypes.Int32);
			method.ParamDefs.Add(new ParamDefUser("second", 2));
			method.ParamDefs.Add(new ParamDefUser("first", 1));
			method.ParamDefs.Add(new ParamDefUser("returnValue", 0));

			using (var module = created.WriteAndRead()) {
				var written = CreatedModule.GetType(module).FindMethod("M")!;

				Assert.Equal(new[] { "returnValue", "first", "second" }, written.ParamDefs.Select(a => a.Name.String).ToArray());
			}
		}

		// Fixed in fc27e811: CLASS/VALUETYPE in a signature must be followed by a TypeDef or TypeRef. The CLR
		// rejects a TypeSpec there, and the reader reads it as a null type.
		[Theory, InlineData(false), InlineData(true)]
		public void ClassSigWithATypeSpecIsAnError(bool valueType) {
			var created = new CreatedModule();
			var typeSpec = new TypeSpecUser(new SZArraySig(created.Module.CorLibTypes.Int32));
			TypeSig signature = valueType ? new ValueTypeSig(typeSpec) : new ClassSig(typeSpec);
			created.AddField("Field", signature);

			Assert.Throws<ModuleWriterException>(() => created.Write());
		}

		[Fact]
		public void WriterErrorIsIgnoredIfTheLoggerDoesNotThrow() {
			var created = new CreatedModule();
			created.AddField("Field", new ClassSig(new TypeSpecUser(new SZArraySig(created.Module.CorLibTypes.Int32))));
			var options = new ModuleWriterOptions(created.Module) { Logger = DummyLogger.NoThrowInstance };

			var stream = new System.IO.MemoryStream();
			created.Module.Write(stream, options);

			using (var module = ModuleDefMD.Load(stream.ToArray()))
				Assert.NotNull(CreatedModule.GetType(module).FindField("Field"));
		}
	}
}
