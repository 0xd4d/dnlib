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

		// A type that has no name: an array of nothing
		static TypeSig CreateTypeWithoutAName() => new SZArraySig(null);

		// Fixed in b060c3b5: a type is stored as its name in a custom attribute. A type without a name was
		// written as "<<<NULL>>>" and the writer didn't report it.
		[Fact]
		public void TypeWithoutANameInACustomAttributeIsAnError() {
			var created = new CreatedModule();
			var corLibTypes = created.Module.CorLibTypes;
			var systemType = new ClassSig(corLibTypes.GetTypeRef("System", "Type"));
			var attributeType = corLibTypes.GetTypeRef("System", "SomeAttribute");
			var constructor = new MemberRefUser(created.Module, ".ctor", MethodSig.CreateInstance(corLibTypes.Void, systemType), attributeType);
			var attribute = new CustomAttribute(constructor);
			attribute.ConstructorArguments.Add(new CAArgument(systemType, CreateTypeWithoutAName()));
			created.Type.CustomAttributes.Add(attribute);

			var exception = Assert.Throws<ModuleWriterException>(() => created.Write());
			Assert.Contains("null value", exception.Message);
		}

		// Fixed in b060c3b5
		[Fact]
		public void CustomMarshalerWithoutANameIsAnError() {
			var created = new CreatedModule();
			var field = created.AddField("Field", created.Module.CorLibTypes.Object);
			field.MarshalType = new CustomMarshalType {
				Guid = string.Empty,
				NativeTypeName = string.Empty,
				CustomMarshaler = new TypeSpecUser(CreateTypeWithoutAName()),
				Cookie = string.Empty,
			};

			var exception = Assert.Throws<ModuleWriterException>(() => created.Write());
			Assert.Contains("null value", exception.Message);
		}

		// Fixed in 70e70fac: the writer adds a TypeRef's scope (the AssemblyRef) and the scope's custom attributes
		// before the TypeRef. If such an attribute's type is the TypeRef, it's a loop, and the writer failed with
		// 'TypeRef has an infinite ResolutionScope loop'.
		[Fact]
		public void AssemblyRefCanHaveACustomAttributeWhoseTypeIsInThatAssembly() {
			var created = new CreatedModule();
			var library = new AssemblyRefUser("Library", new System.Version(1, 0, 0, 0));
			var attributeType = new TypeRefUser(created.Module, "Library", "MarkerAttribute", library);
			var constructor = new MemberRefUser(created.Module, ".ctor", MethodSig.CreateInstance(created.Module.CorLibTypes.Void), attributeType);
			library.CustomAttributes.Add(new CustomAttribute(constructor));
			// The field's type is the first thing the writer sees that uses the TypeRef
			created.AddField("Field", new ClassSig(attributeType));

			using (var module = created.WriteAndRead()) {
				var writtenLibrary = module.GetAssemblyRefs().Single(a => a.Name == "Library");
				var attribute = Assert.Single(writtenLibrary.CustomAttributes);
				Assert.Equal("Library.MarkerAttribute", attribute.TypeFullName);
				Assert.Same(writtenLibrary, attribute.AttributeType!.Scope);
			}
		}

		// The same with a nested type: the enclosing TypeRef has an attribute whose type is its nested type
		[Fact]
		public void TypeRefCanHaveACustomAttributeWhoseTypeIsNestedInIt() {
			var created = new CreatedModule();
			var library = new AssemblyRefUser("Library", new System.Version(1, 0, 0, 0));
			var outerType = new TypeRefUser(created.Module, "Library", "Outer", library);
			var attributeType = new TypeRefUser(created.Module, string.Empty, "MarkerAttribute", outerType);
			var constructor = new MemberRefUser(created.Module, ".ctor", MethodSig.CreateInstance(created.Module.CorLibTypes.Void), attributeType);
			outerType.CustomAttributes.Add(new CustomAttribute(constructor));
			created.AddField("Field", new ClassSig(attributeType));

			using (var module = created.WriteAndRead()) {
				var writtenOuterType = module.GetTypeRefs().Single(a => a.Name == "Outer");
				var attribute = Assert.Single(writtenOuterType.CustomAttributes);
				Assert.Equal("Library.Outer/MarkerAttribute", attribute.TypeFullName);
			}
		}
	}
}
