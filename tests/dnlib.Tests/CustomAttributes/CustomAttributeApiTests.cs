// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.CustomAttributes {
	/// <summary>
	/// The classes that a custom attribute is made of: <see cref="CustomAttribute"/> (the constructor that's
	/// called), <see cref="CAArgument"/> (a value and its type) and <see cref="CANamedArgument"/> (a field or
	/// property that's set). These tests are about the classes themselves, not about reading and writing.
	/// </summary>
	public class CustomAttributeApiTests {
		readonly ModuleDefUser module = new ModuleDefUser("Test.dll");

		ICorLibTypes Types => module.CorLibTypes;

		MemberRefUser CreateConstructor(params TypeSig[] parameters) =>
			new MemberRefUser(module, ".ctor", MethodSig.CreateInstance(Types.Void, parameters), new TypeRefUser(module, "Ns", "SomeAttribute", module));

		[Fact]
		public void AttributeTypeIsTheTypeOfItsConstructor() {
			var constructor = CreateConstructor();
			var attribute = new CustomAttribute(constructor);

			Assert.Same(constructor, attribute.Constructor);
			Assert.Same(constructor, attribute.ConstructorOrThrow);
			Assert.Same(constructor.Class, attribute.AttributeType);
			Assert.Equal("Ns.SomeAttribute", attribute.TypeFullName);
			Assert.Equal("Ns.SomeAttribute", attribute.ToString());
			Assert.False(attribute.IsRawBlob);
			Assert.Null(attribute.RawData);
			Assert.False(attribute.HasConstructorArguments || attribute.HasNamedArguments);
		}

		[Fact]
		public void AttributeWithoutAConstructorHasNoType() {
			var attribute = new CustomAttribute(null);

			Assert.Null(attribute.Constructor);
			Assert.Null(attribute.AttributeType);
			Assert.Equal("", attribute.TypeFullName);
			Assert.Throws<InvalidMetadataException>(() => attribute.ConstructorOrThrow);
		}

		// The constructor can also be a MethodDef: the name is the name of its class
		[Fact]
		public void TypeNameOfAnAttributeWhoseConstructorIsADefinition() {
			var type = new TypeDefUser("Ns", "LocalAttribute", Types.Object.TypeDefOrRef);
			module.Types.Add(type);
			var constructor = new MethodDefUser(".ctor", MethodSig.CreateInstance(Types.Void));
			type.Methods.Add(constructor);

			Assert.Equal("Ns.LocalAttribute", new CustomAttribute(constructor).TypeFullName);
			// A constructor that isn't in a type
			Assert.Equal("", new CustomAttribute(new MethodDefUser(".ctor", MethodSig.CreateInstance(Types.Void))).TypeFullName);
		}

		[Fact]
		public void EveryConstructorCopiesItsArguments() {
			var constructor = CreateConstructor(Types.Int32);
			var arguments = new List<CAArgument> { new CAArgument(Types.Int32, 5) };
			var namedArguments = new List<CANamedArgument> { new CANamedArgument(true, Types.Int32, "Field", new CAArgument(Types.Int32, 6)) };

			var withArguments = new CustomAttribute(constructor, arguments);
			var withNamedArguments = new CustomAttribute(constructor, namedArguments);
			var withBoth = new CustomAttribute(constructor, arguments, namedArguments);
			var withOffset = new CustomAttribute(constructor, arguments, namedArguments, 0x1234);
			var withNothing = new CustomAttribute(constructor, (IEnumerable<CAArgument>?)null, null);

			Assert.Equal(5, withArguments.ConstructorArguments.Single().Value);
			Assert.True(withArguments.HasConstructorArguments && !withArguments.HasNamedArguments);
			Assert.Equal(6, withNamedArguments.NamedArguments.Single().Value);
			Assert.True(withNamedArguments.HasNamedArguments && !withNamedArguments.HasConstructorArguments);
			Assert.True(withBoth.HasConstructorArguments && withBoth.HasNamedArguments);
			Assert.Equal(0x1234U, withOffset.BlobOffset);
			Assert.Equal(0U, withBoth.BlobOffset);
			Assert.False(withNothing.HasConstructorArguments || withNothing.HasNamedArguments);
			// The attribute has its own lists
			arguments.Clear();
			Assert.True(withArguments.HasConstructorArguments);
		}

		[Fact]
		public void RawAttributeOnlyHasItsBytes() {
			var bytes = new byte[] { 1, 0, 0, 0 };
			var attribute = new CustomAttribute(CreateConstructor(), bytes);

			Assert.True(attribute.IsRawBlob);
			Assert.Same(bytes, attribute.RawData);
			Assert.False(attribute.HasConstructorArguments || attribute.HasNamedArguments);
		}

		[Fact]
		public void NamedArgumentsAreFoundByNameAndKind() {
			var attribute = new CustomAttribute(CreateConstructor());
			var field = new CANamedArgument(isField: true, Types.Int32, "Value", new CAArgument(Types.Int32, 1));
			var property = new CANamedArgument(isField: false, Types.String, "Value", new CAArgument(Types.String, new UTF8String("text")));
			var otherProperty = new CANamedArgument(isField: false, Types.Int32, "Other", new CAArgument(Types.Int32, 2));
			attribute.NamedArguments.Add(field);
			attribute.NamedArguments.Add(property);
			attribute.NamedArguments.Add(otherProperty);

			Assert.Equal(new[] { field }, attribute.Fields);
			Assert.Equal(new[] { property, otherProperty }, attribute.Properties);
			// A field and a property can have the same name
			Assert.Same(field, attribute.GetField("Value"));
			Assert.Same(field, attribute.GetField(new UTF8String("Value")));
			Assert.Same(property, attribute.GetProperty("Value"));
			Assert.Same(property, attribute.GetProperty(new UTF8String("Value")));
			Assert.Same(otherProperty, attribute.GetNamedArgument("Other", isField: false));
			Assert.Same(otherProperty, attribute.GetNamedArgument(new UTF8String("Other"), isField: false));
			Assert.Null(attribute.GetField("Other"));
			Assert.Null(attribute.GetProperty("Missing"));
			// Names are case sensitive
			Assert.Null(attribute.GetField("value"));
		}

		[Fact]
		public void ArgumentIsAValueAndItsType() {
			var argument = new CAArgument(Types.Int32, 5);
			var withoutValue = new CAArgument(Types.String);

			Assert.Same(Types.Int32, argument.Type);
			Assert.Equal(5, argument.Value);
			Assert.Equal("5 (System.Int32)", argument.ToString());
			Assert.Null(withoutValue.Value);
			Assert.Equal("null (System.String)", withoutValue.ToString());

			argument.Value = 6;
			argument.Type = Types.Int64;
			Assert.Equal("6 (System.Int64)", argument.ToString());
		}

		// A copy of an array argument has its own list and its own elements, also if arrays are in arrays
		[Fact]
		public void CloneOfAnArrayArgumentIsADeepCopy() {
			var inner = new List<CAArgument> { new CAArgument(Types.Int32, 1), new CAArgument(Types.Int32, 2) };
			var outer = new List<CAArgument> { new CAArgument(new SZArraySig(Types.Int32), inner), new CAArgument(Types.String, new UTF8String("text")) };
			var argument = new CAArgument(new SZArraySig(Types.Object), outer);

			var clone = argument.Clone();
			var boxedClone = (CAArgument)((System.ICloneable)argument).Clone();

			var clonedOuter = Assert.IsAssignableFrom<IList<CAArgument>>(clone.Value);
			var clonedInner = Assert.IsAssignableFrom<IList<CAArgument>>(clonedOuter[0].Value);
			Assert.NotSame(outer, clonedOuter);
			Assert.NotSame(inner, clonedInner);
			Assert.Equal(new object[] { 1, 2 }, clonedInner.Select(a => a.Value));
			Assert.Equal(new UTF8String("text"), clonedOuter[1].Value);
			Assert.Same(argument.Type, clone.Type);
			Assert.NotSame(outer, boxedClone.Value);
			// A change of the copy isn't a change of the original
			clonedInner[0] = new CAArgument(Types.Int32, 99);
			Assert.Equal(1, inner[0].Value);
		}

		[Fact]
		public void NamedArgumentIsAFieldOrAPropertyWithAValue() {
			var named = new CANamedArgument(isField: true, Types.Int32, "Count", new CAArgument(Types.Int32, 5));

			Assert.True(named.IsField && !named.IsProperty);
			Assert.Equal("Count", named.Name);
			Assert.Same(Types.Int32, named.Type);
			Assert.Same(Types.Int32, named.ArgumentType);
			Assert.Equal(5, named.Value);
			Assert.Equal("(field) System.Int32 Count = 5 (System.Int32)", named.ToString());

			named.IsProperty = true;
			named.Value = 6;
			named.ArgumentType = Types.Int64;
			Assert.True(named.IsProperty && !named.IsField);
			Assert.Equal(6, named.Argument.Value);
			Assert.Same(Types.Int64, named.Argument.Type);
			Assert.Equal("(property) System.Int32 Count = 6 (System.Int64)", named.ToString());
			named.IsField = true;
			Assert.True(named.IsField);
		}

		[Fact]
		public void EveryNamedArgumentConstructorSetsWhatItGets() {
			Assert.False(new CANamedArgument().IsField);
			Assert.True(new CANamedArgument(isField: true).IsField);
			Assert.Same(Types.Int32, new CANamedArgument(false, Types.Int32).Type);
			var withName = new CANamedArgument(false, Types.Int32, "Name");
			Assert.Equal("Name", withName.Name);
			Assert.Null(withName.Value);
			Assert.Equal("(property) System.Int32 Name = null ()", withName.ToString());
		}

		[Fact]
		public void CloneOfANamedArgumentHasItsOwnArgument() {
			var values = new List<CAArgument> { new CAArgument(Types.Int32, 1) };
			var named = new CANamedArgument(isField: false, new SZArraySig(Types.Int32), "Values", new CAArgument(new SZArraySig(Types.Int32), values));

			var clone = named.Clone();
			var cloneAsObject = (CANamedArgument)((System.ICloneable)named).Clone();

			Assert.NotSame(named, clone);
			Assert.NotSame(named, cloneAsObject);
			Assert.Same(named.Type, clone.Type);
			Assert.Same(named.ArgumentType, clone.ArgumentType);
			Assert.NotSame(values, clone.Value);
			Assert.Equal(1, ((IList<CAArgument>)clone.Value!)[0].Value);
			Assert.True(clone.IsProperty);
			Assert.Equal("Values", clone.Name);
		}
	}
}
