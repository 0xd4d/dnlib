// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.CustomAttributes {
	/// <summary>
	/// The bytes of a custom attribute are: <c>01 00</c>, the constructor arguments, the number of named arguments
	/// (2 bytes) and the named arguments. Some tools don't write the number if it's 0. The tests replace the
	/// bytes of an attribute and check that dnlib reads the attribute if the runtime (.NET) creates it.
	/// </summary>
	public class AttributeBlobEndTests {
		const string Source = @"
using System;

public enum Level : short { Low = -2, High = 5 }
public class IntAttribute : Attribute { public int Value; public IntAttribute(int value) { Value = value; } }
public class LevelAttribute : Attribute { public Level Value; public LevelAttribute(Level value) { Value = value; } }
public class NoArgumentsAttribute : Attribute { }

[Int(1)] public class HasInt { }
[Level(Level.High)] public class HasLevel { }
[NoArguments] public class HasNoArguments { }
";

		// Replaces the bytes of the attribute of a class and writes the module
		static byte[] WriteWithAttributeBytes(string className, byte[] attributeBytes) {
			using (var module = ModuleDefMD.Load(CSharpCompiler.Compile(Source).Image)) {
				var attributes = module.Find(className, isReflectionName: true)!.CustomAttributes;
				attributes[0] = new CustomAttribute(attributes[0].Constructor, attributeBytes);
				var stream = new MemoryStream();
				module.Write(stream);
				return stream.ToArray();
			}
		}

		// The value that the runtime's attribute object has (the name of an enum value), "no value" if it has
		// no Value field, or null if the runtime can't create the attribute
		static string? GetValueFromTheRuntime(byte[] image, string className) {
			using (var loaded = new LoadedAssembly(image)) {
				object attribute;
				try {
					attribute = loaded.Assembly.GetType(className)!.GetCustomAttributes(false).Single();
				}
				catch (System.Reflection.CustomAttributeFormatException) {
					return null;
				}
				var field = attribute.GetType().GetField("Value");
				return field is null ? "no value" : Convert.ToString(field.GetValue(attribute), System.Globalization.CultureInfo.InvariantCulture);
			}
		}

		// Mono and .NET Framework accept other files than .NET
		static bool IsDotNet => typeof(object).Assembly.GetName().Name == "System.Private.CoreLib";

		[Theory]
		[InlineData("HasInt", new byte[] { 1, 0, 0x78, 0x56, 0x34, 0x12, 0, 0 }, "305419896")]
		// No number of named arguments
		[InlineData("HasInt", new byte[] { 1, 0, 0x78, 0x56, 0x34, 0x12 }, "305419896")]
		[InlineData("HasLevel", new byte[] { 1, 0, 0xFE, 0xFF }, "Low")]
		// An attribute without any arguments can also have no bytes at all
		[InlineData("HasNoArguments", new byte[] { 1, 0, 0, 0 }, "no value")]
		[InlineData("HasNoArguments", new byte[] { }, "no value")]
		public void AttributeThatTheRuntimeCreatesIsRead(string className, byte[] attributeBytes, string expectedValue) {
			var image = WriteWithAttributeBytes(className, attributeBytes);
			if (IsDotNet)
				Assert.Equal(expectedValue, GetValueFromTheRuntime(image, className));

			using (var module = ModuleDefMD.Load(image)) {
				var attribute = module.Find(className, isReflectionName: true)!.CustomAttributes[0];

				Assert.False(attribute.IsRawBlob);
				var value = attribute.ConstructorArguments.Count == 0 ? "no value" : attribute.ConstructorArguments[0].Value;
				// dnlib returns the number of an enum value
				Assert.Equal(expectedValue == "Low" ? "-2" : expectedValue, Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
			}
		}

		// dnlib returns such an attribute with its bytes (and writes the same bytes again)
		[Theory]
		[InlineData("HasInt", new byte[] { 1, 0, 0x78, 0x56 })]                     // half the value
		[InlineData("HasInt", new byte[] { 1, 0, 0x78, 0x56, 0x34, 0x12, 0 })]      // half the number of named arguments
		[InlineData("HasInt", new byte[] { 2, 0, 0x78, 0x56, 0x34, 0x12, 0, 0 })]   // doesn't start with 01 00
		[InlineData("HasInt", new byte[] { })]
		[InlineData("HasNoArguments", new byte[] { 2, 0, 0, 0 })]
		public void AttributeThatTheRuntimeDoesNotCreateHasOnlyItsBytes(string className, byte[] attributeBytes) {
			var image = WriteWithAttributeBytes(className, attributeBytes);
			if (IsDotNet)
				Assert.Null(GetValueFromTheRuntime(image, className));

			using (var module = ModuleDefMD.Load(image)) {
				var attribute = module.Find(className, isReflectionName: true)!.CustomAttributes[0];

				Assert.True(attribute.IsRawBlob);
				Assert.Equal(attributeBytes, attribute.RawData);
			}
		}

		// dnlib reads more than the runtime does: these aren't created by .NET, but the arguments can be read
		[Theory]
		[InlineData("HasInt", new byte[] { 1, 0, 0x78, 0x56, 0x34, 0x12, 0, 0, 0xAA, 0xBB }, 0x12345678)]   // bytes after the end
		[InlineData("HasLevel", new byte[] { 1, 0, 0xFE, 0xFF, 0, 0, 0, 0 }, (short)-2)]
		public void AttributeWithBytesAfterItsEndIsRead(string className, byte[] attributeBytes, object expectedValue) {
			var image = WriteWithAttributeBytes(className, attributeBytes);
			if (IsDotNet)
				Assert.Null(GetValueFromTheRuntime(image, className));

			using (var module = ModuleDefMD.Load(image)) {
				var attribute = module.Find(className, isReflectionName: true)!.CustomAttributes[0];

				Assert.False(attribute.IsRawBlob);
				Assert.Equal(expectedValue, attribute.ConstructorArguments[0].Value);
			}
		}
	}
}
