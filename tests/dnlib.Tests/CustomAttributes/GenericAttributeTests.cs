// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.CustomAttributes {
	/// <summary>
	/// Generic custom attributes (C# 11, eg. <c>[Generic&lt;int&gt;(5)]</c>) and custom attribute arguments
	/// whose type is an enum that's nested in a generic type. The attributes are on
	/// <c>Features.GenericAttributeUser</c> in Fixtures/Sources/Features.cs.
	/// </summary>
	public class GenericAttributeTests {
		static ModuleDefMD LoadFeatures() => ModuleDefMD.Load(Fixtures.Fixtures.Features.Image);

		static ModuleDefMD LoadWrittenFeatures() => ModuleDefMD.Load(RoundTrip.CheckWithNewMetadata(Fixtures.Fixtures.Features.Image));

		// The tests run with the original module and with the module after dnlib wrote it
		public static TheoryData<bool> OriginalAndWritten => new TheoryData<bool> { false, true };

		static ModuleDefMD Load(bool written) => written ? LoadWrittenFeatures() : LoadFeatures();

		static List<CustomAttribute> GetAttributes(ModuleDef module, string attributeTypeFullName) {
			var user = module.Find("Features.GenericAttributeUser", isReflectionName: true);
			Assert.NotNull(user);
			return user.CustomAttributes.Where(a => a.AttributeType?.FullName == attributeTypeFullName).ToList();
		}

		static CustomAttribute GetAttribute(ModuleDef module, string attributeTypeFullName) =>
			Assert.Single(GetAttributes(module, attributeTypeFullName));

		[Theory, MemberData(nameof(OriginalAndWritten))]
		public void NoCustomAttributeInTheModuleIsARawBlob(bool written) {
			// dnlib keeps the bytes of a custom attribute it can't decode. Everything in this module can be decoded
			// without loading other assemblies.
			using (var module = Load(written)) {
				foreach (var type in module.GetTypes()) {
					Assert.DoesNotContain(type.CustomAttributes, a => a.IsRawBlob);
					foreach (var method in type.Methods)
						Assert.DoesNotContain(method.CustomAttributes, a => a.IsRawBlob);
					foreach (var field in type.Fields)
						Assert.DoesNotContain(field.CustomAttributes, a => a.IsRawBlob);
				}
			}
		}

		[Theory, MemberData(nameof(OriginalAndWritten))]
		public void ArgumentTypesAreTheGenericArguments(bool written) {
			using (var module = Load(written)) {
				// [Generic<int>(5, Property = 6)]
				var attribute = GetAttribute(module, "Features.GenericAttribute`1<System.Int32>");

				var argument = Assert.Single(attribute.ConstructorArguments);
				Assert.Equal("System.Int32", argument.Type?.FullName);
				Assert.Equal(5, argument.Value);
				var property = Assert.Single(attribute.NamedArguments);
				Assert.Equal("Property", property.Name);
				Assert.False(property.IsField);
				Assert.Equal("System.Int32", property.Type?.FullName);
				Assert.Equal(6, property.Value);
			}
		}

		[Theory, MemberData(nameof(OriginalAndWritten))]
		public void ArrayArgumentHasTheGenericArgumentAsElementType(bool written) {
			using (var module = Load(written)) {
				// [Generic<string>(new[] { "a", null }, 1.5, Field = new[] { "b" })]
				var attribute = GetAttribute(module, "Features.GenericAttribute`1<System.String>");

				Assert.Equal(2, attribute.ConstructorArguments.Count);
				Assert.Equal("System.String[]", attribute.ConstructorArguments[0].Type?.FullName);
				var elements = Assert.IsAssignableFrom<IList<CAArgument>>(attribute.ConstructorArguments[0].Value);
				Assert.Equal(2, elements.Count);
				Assert.Equal("a", (UTF8String?)elements[0].Value);
				Assert.Null(elements[1].Value);

				// The second parameter's type is object. dnlib gives the argument the type of the value.
				var boxed = attribute.ConstructorArguments[1];
				Assert.Equal("System.Double", boxed.Type?.FullName);
				Assert.Equal(1.5, boxed.Value);

				var field = Assert.Single(attribute.NamedArguments);
				Assert.True(field.IsField);
				Assert.Equal("System.String[]", field.Type?.FullName);
			}
		}

		[Theory, MemberData(nameof(OriginalAndWritten))]
		public void TypeArgumentIsAType(bool written) {
			using (var module = Load(written)) {
				// [Generic<Type>(typeof(List<>), Property = typeof(Dictionary<int, string[]>))]
				var attribute = GetAttribute(module, "Features.GenericAttribute`1<System.Type>");

				var argument = Assert.Single(attribute.ConstructorArguments);
				Assert.Equal("System.Collections.Generic.List`1", Assert.IsAssignableFrom<TypeSig>(argument.Value).FullName);
				var property = Assert.Single(attribute.NamedArguments);
				Assert.Equal("System.Collections.Generic.Dictionary`2<System.Int32,System.String[]>", Assert.IsAssignableFrom<TypeSig>(property.Value).FullName);
			}
		}

		[Theory, MemberData(nameof(OriginalAndWritten))]
		public void EnumArgumentIsReadAsItsUnderlyingType(bool written) {
			using (var module = Load(written)) {
				// [Generic<Color>(Color.Blue, Field = new[] { Color.Red, Color.Green })], Color is a byte enum
				var attribute = GetAttribute(module, "Features.GenericAttribute`1<Features.Color>");

				var argument = Assert.Single(attribute.ConstructorArguments);
				Assert.Equal("Features.Color", argument.Type?.FullName);
				Assert.Equal((byte)4, argument.Value);
				var field = Assert.Single(attribute.NamedArguments);
				var elements = Assert.IsAssignableFrom<IList<CAArgument>>(field.Value);
				Assert.Equal(new object[] { (byte)1, (byte)2 }, elements.Select(a => a.Value).ToArray());
			}
		}

		[Theory, MemberData(nameof(OriginalAndWritten))]
		public void AttributeWithTwoGenericArguments(bool written) {
			using (var module = Load(written)) {
				// [Generic<string, long>("key", long.MinValue, new long[] { 1, 2 })]
				var attribute = GetAttribute(module, "Features.GenericAttribute`2<System.String,System.Int64>");

				Assert.Equal(3, attribute.ConstructorArguments.Count);
				Assert.Equal("key", (UTF8String?)attribute.ConstructorArguments[0].Value);
				Assert.Equal(long.MinValue, attribute.ConstructorArguments[1].Value);
				Assert.Equal("System.Int64[]", attribute.ConstructorArguments[2].Type?.FullName);
			}
		}

		[Theory, MemberData(nameof(OriginalAndWritten))]
		public void EnumNestedInAGenericTypeIsReadAsAnEnum(bool written) {
			using (var module = Load(written)) {
				// [Everything(typeof(GenericOwner<int>.Kind), Color.Red, GenericOwner<string>.Kind.Second)]
				// Kind is a short enum
				var attribute = GetAttributes(module, "Features.EverythingAttribute").Single(a => a.ConstructorArguments.Count == 3);

				var type = Assert.IsAssignableFrom<TypeSig>(attribute.ConstructorArguments[0].Value);
				Assert.Equal("Features.GenericOwner`1/Kind<System.Int32>", type.FullName);
				// The third parameter's type is object. dnlib gives the argument the type of the value.
				var boxed = attribute.ConstructorArguments[2];
				Assert.Equal("Features.GenericOwner`1/Kind<System.String>", boxed.Type?.FullName);
				Assert.Equal((short)5, boxed.Value);
			}
		}

		[Theory, MemberData(nameof(OriginalAndWritten))]
		public void EnumNestedInAGenericTypeInAnObjectArray(bool written) {
			using (var module = Load(written)) {
				// [Everything(null, null, null, new object[] { GenericOwner<int>.Kind.Second, new[] { GenericOwner<int>.Kind.First } })]
				var attribute = GetAttributes(module, "Features.EverythingAttribute").Single(a => a.ConstructorArguments.Count == 4);

				var objects = Assert.IsAssignableFrom<IList<CAArgument>>(attribute.ConstructorArguments[3].Value);
				Assert.Equal(2, objects.Count);
				var first = objects[0];
				Assert.Equal("Features.GenericOwner`1/Kind<System.Int32>", first.Type?.FullName);
				Assert.Equal((short)5, first.Value);
				var second = objects[1];
				Assert.Equal("Features.GenericOwner`1/Kind<System.Int32>[]", second.Type?.FullName);
				var elements = Assert.IsAssignableFrom<IList<CAArgument>>(second.Value);
				Assert.Equal((short)0, Assert.Single(elements).Value);
			}
		}
	}
}
