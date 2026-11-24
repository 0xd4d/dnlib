// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using dnlib.Tests.Writer;
using Xunit;

namespace dnlib.Tests.CustomAttributes {
	/// <summary>
	/// Custom attributes that can't be written: the arguments don't match the constructor, or a value isn't
	/// what its type says. The writer reports an error (a <see cref="ModuleWriterException"/> by default), and
	/// if the logger ignores the error, it still writes a file that can be read.
	/// </summary>
	public class CustomAttributeWriterErrorTests {
		readonly CreatedModule created = new CreatedModule();

		ICorLibTypes Types => created.Module.CorLibTypes;
		TypeSig SystemType => new ClassSig(Types.GetTypeRef("System", "Type"));

		// A reference to a constructor of an attribute class that doesn't exist: only its signature is needed
		MemberRef CreateConstructor(params TypeSig[] parameters) =>
			new MemberRefUser(created.Module, ".ctor", MethodSig.CreateInstance(Types.Void, parameters), Types.GetTypeRef("System", "SomeAttribute"));

		void AssertIsAWriterError(CustomAttribute attribute, string expectedMessage) {
			created.Type.CustomAttributes.Add(attribute);

			var exception = Assert.Throws<ModuleWriterException>(() => created.Write());
			Assert.Contains(expectedMessage, exception.Message);

			// With a logger that doesn't throw, the module is written and can be read
			var options = new ModuleWriterOptions(created.Module) { Logger = DummyLogger.NoThrowInstance };
			var stream = new MemoryStream();
			created.Module.Write(stream, options);
			using (var module = ModuleDefMD.Load(stream.ToArray()))
				Assert.NotNull(CreatedModule.GetType(module));
		}

		[Fact]
		public void ValidAttributeIsWritten() {
			var attribute = new CustomAttribute(CreateConstructor(Types.Int32, Types.String));
			attribute.ConstructorArguments.Add(new CAArgument(Types.Int32, 5));
			attribute.ConstructorArguments.Add(new CAArgument(Types.String, new UTF8String("text")));
			created.Type.CustomAttributes.Add(attribute);

			using (var module = created.WriteAndRead()) {
				var read = Assert.Single(CreatedModule.GetType(module).CustomAttributes);
				Assert.Equal(new object[] { 5, new UTF8String("text") }, read.ConstructorArguments.Select(a => a.Value));
			}
		}

		[Fact]
		public void AttributeWithoutAConstructor() =>
			AssertIsAWriterError(new CustomAttribute(null), "ctor is null");

		[Fact]
		public void TooFewArguments() =>
			AssertIsAWriterError(new CustomAttribute(CreateConstructor(Types.Int32)), "arguments count != method sig arguments count");

		[Fact]
		public void TooManyArguments() {
			var attribute = new CustomAttribute(CreateConstructor());
			attribute.ConstructorArguments.Add(new CAArgument(Types.Int32, 5));

			AssertIsAWriterError(attribute, "arguments count != method sig arguments count");
		}

		[Fact]
		public void ArgumentOfAnotherTypeThanTheParameter() {
			var attribute = new CustomAttribute(CreateConstructor(Types.Int32));
			attribute.ConstructorArguments.Add(new CAArgument(Types.String, new UTF8String("text")));

			AssertIsAWriterError(attribute, "arg type != value.Type");
		}

		[Fact]
		public void ArgumentWithoutAType() {
			var attribute = new CustomAttribute(CreateConstructor(Types.Int32));
			attribute.ConstructorArguments.Add(new CAArgument(null, 5));

			AssertIsAWriterError(attribute, "Custom attribute");
		}

		[Fact]
		public void TypeArgumentWhoseValueIsNotAType() {
			var attribute = new CustomAttribute(CreateConstructor(SystemType));
			attribute.ConstructorArguments.Add(new CAArgument(SystemType, 5));

			AssertIsAWriterError(attribute, "value is not a type");
		}

		[Fact]
		public void ArrayArgumentWhoseValueIsNotAListOfArguments() {
			var arrayType = new SZArraySig(Types.Int32);
			var attribute = new CustomAttribute(CreateConstructor(arrayType));
			attribute.ConstructorArguments.Add(new CAArgument(arrayType, new[] { 1, 2 }));

			AssertIsAWriterError(attribute, "CAArgument.Value is not null or an array");
		}

		[Fact]
		public void NamedArgumentWithoutAType() {
			var attribute = new CustomAttribute(CreateConstructor());
			attribute.NamedArguments.Add(new CANamedArgument(isField: true, null, "Field", new CAArgument(Types.Int32, 5)));

			AssertIsAWriterError(attribute, "Field/property type is null");
		}

		[Fact]
		public void ConstructorWithAnotherKindOfSignature() {
			var notAMethod = new MemberRefUser(created.Module, ".ctor", new FieldSig(Types.Int32), Types.GetTypeRef("System", "SomeAttribute"));

			AssertIsAWriterError(new CustomAttribute(notAMethod), "method signature is invalid");
		}

		// An attribute can be the bytes of its value: they're written as they are
		[Fact]
		public void RawAttributeIsWrittenAsItIs() {
			var rawData = new byte[] { 1, 0, 5, 0, 0, 0, 0, 0 };
			created.Type.CustomAttributes.Add(new CustomAttribute(CreateConstructor(Types.Int32), rawData));

			using (var module = created.WriteAndRead()) {
				var read = Assert.Single(CreatedModule.GetType(module).CustomAttributes);
				// The bytes are a valid value for this constructor, so the reader decodes them
				Assert.False(read.IsRawBlob);
				Assert.Equal(5, read.ConstructorArguments.Single().Value);
			}
		}

		// Bytes that aren't a valid value are kept as raw data by the reader, and written again unchanged
		[Fact]
		public void InvalidRawAttributeSurvivesAWrite() {
			var rawData = new byte[] { 0xFF, 0xFE, 0xFD };
			created.Type.CustomAttributes.Add(new CustomAttribute(CreateConstructor(Types.Int32), rawData));

			using (var module = created.WriteAndRead()) {
				var read = Assert.Single(CreatedModule.GetType(module).CustomAttributes);
				Assert.True(read.IsRawBlob);
				Assert.Equal(rawData, read.RawData);
				Assert.Empty(read.ConstructorArguments);

				var stream = new MemoryStream();
				module.Write(stream);
				using (var writtenAgain = ModuleDefMD.Load(stream.ToArray()))
					Assert.Equal(rawData, Assert.Single(CreatedModule.GetType(writtenAgain).CustomAttributes).RawData);
			}
		}

		[Fact]
		public void RawAttributeWithArgumentsIsAnError() {
			var attribute = new CustomAttribute(CreateConstructor(Types.Int32), new byte[] { 1, 0, 5, 0, 0, 0, 0, 0 });
			attribute.ConstructorArguments.Add(new CAArgument(Types.Int32, 5));

			AssertIsAWriterError(attribute, "Raw custom attribute contains arguments");
		}
	}
}
