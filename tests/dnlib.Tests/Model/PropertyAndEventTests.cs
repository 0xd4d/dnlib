// dnlib: See LICENSE.txt for more info

using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Xunit;

namespace dnlib.Tests.Model {
	/// <summary>
	/// A property or an event is a name with a list of methods: the getter and the setter, or the add, remove and
	/// raise methods, and any number of "other" methods. In a file, the MethodSemantics table connects them. The
	/// tests create properties and events with the API, in ways that a C# compiler doesn't (two getters, other
	/// methods, a property with a constant), write them and read them again.
	/// </summary>
	public class PropertyAndEventTests {
		readonly ModuleDefUser module = new ModuleDefUser("Test.dll");
		readonly TypeDefUser type;

		public PropertyAndEventTests() {
			new AssemblyDefUser("Test").Modules.Add(module);
			type = new TypeDefUser("Ns", "Type", module.CorLibTypes.Object.TypeDefOrRef);
			module.Types.Add(type);
		}

		MethodDefUser AddMethod(string name) {
			var method = new MethodDefUser(name, MethodSig.CreateStatic(module.CorLibTypes.Void), MethodAttributes.Static | MethodAttributes.Public);
			method.Body = new CilBody();
			method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
			type.Methods.Add(method);
			return method;
		}

		PropertyDefUser AddProperty(string name) {
			var property = new PropertyDefUser(name, PropertySig.CreateStatic(module.CorLibTypes.Int32));
			type.Properties.Add(property);
			return property;
		}

		ModuleDefMD WriteAndRead() {
			var stream = new MemoryStream();
			module.Write(stream);
			return ModuleDefMD.Load(stream.ToArray());
		}

		static TypeDef GetType(ModuleDef readModule) => readModule.Find("Ns.Type", isReflectionName: true)!;

		[Fact]
		public void GetterAndSetterAreTheFirstOfTheirLists() {
			var property = AddProperty("Property");
			var getter = AddMethod("get_Property");
			var otherGetter = AddMethod("get_Other");
			var setter = AddMethod("set_Property");
			Assert.Null(property.GetMethod);
			Assert.Null(property.SetMethod);
			Assert.False(property.HasOtherMethods);

			property.GetMethod = getter;
			property.SetMethod = setter;
			Assert.Equal(new[] { getter }, property.GetMethods);
			Assert.Equal(new[] { setter }, property.SetMethods);

			// Setting it again replaces the first one, null removes all of them
			property.GetMethod = otherGetter;
			Assert.Equal(new[] { otherGetter }, property.GetMethods);
			property.GetMethods.Add(getter);
			Assert.Same(otherGetter, property.GetMethod);
			property.GetMethod = null;
			Assert.Empty(property.GetMethods);
			property.SetMethod = null;
			Assert.Empty(property.SetMethods);
		}

		[Fact]
		public void PropertyWithSeveralAccessorsAndOtherMethodsIsWritten() {
			var property = AddProperty("Property");
			property.GetMethods.Add(AddMethod("get_First"));
			property.GetMethods.Add(AddMethod("get_Second"));
			property.SetMethod = AddMethod("set_Property");
			property.OtherMethods.Add(AddMethod("other_1"));
			property.OtherMethods.Add(AddMethod("other_2"));

			using (var read = WriteAndRead()) {
				var readProperty = GetType(read).FindProperty("Property")!;

				Assert.Equal(new[] { "get_First", "get_Second" }, readProperty.GetMethods.Select(a => a.Name.String));
				Assert.Equal("get_First", readProperty.GetMethod!.Name);
				Assert.Equal("set_Property", readProperty.SetMethod!.Name);
				Assert.Equal(new[] { "other_1", "other_2" }, readProperty.OtherMethods.Select(a => a.Name.String));
				Assert.True(readProperty.HasOtherMethods);
			}
		}

		// C# has no constants for properties, but the metadata has (the Constant table can have a property)
		[Fact]
		public void PropertyCanHaveAConstant() {
			var property = AddProperty("Property");
			property.Constant = new ConstantUser(123);
			property.Attributes |= PropertyAttributes.HasDefault;

			using (var read = WriteAndRead()) {
				var readProperty = GetType(read).FindProperty("Property")!;

				Assert.True(readProperty.HasConstant);
				Assert.Equal(123, readProperty.Constant!.Value);
				Assert.Equal(ElementType.I4, readProperty.ElementType);
				// A new constant replaces the one that was read
				readProperty.Constant = new ConstantUser("text");
				Assert.Equal("text", readProperty.Constant.Value);
				readProperty.Constant = null;
				Assert.False(readProperty.HasConstant);
			}
		}

		[Fact]
		public void EventMethodsAreWritten() {
			var eventDef = new EventDefUser("Changed", module.CorLibTypes.GetTypeRef("System", "EventHandler"));
			type.Events.Add(eventDef);
			Assert.Null(eventDef.AddMethod);
			Assert.Null(eventDef.RemoveMethod);
			Assert.Null(eventDef.InvokeMethod);
			eventDef.AddMethod = AddMethod("add_Changed");
			eventDef.RemoveMethod = AddMethod("remove_Changed");
			eventDef.InvokeMethod = AddMethod("raise_Changed");
			eventDef.OtherMethods.Add(AddMethod("other_Changed"));

			using (var read = WriteAndRead()) {
				var readEvent = GetType(read).FindEvent("Changed")!;

				Assert.Equal("add_Changed", readEvent.AddMethod!.Name);
				Assert.Equal("remove_Changed", readEvent.RemoveMethod!.Name);
				Assert.Equal("raise_Changed", readEvent.InvokeMethod!.Name);
				Assert.Equal("other_Changed", readEvent.OtherMethods.Single().Name);
				Assert.True(readEvent.HasOtherMethods);
				Assert.Equal("System.EventHandler", readEvent.EventType!.FullName);
				// The methods of an event that was read can be replaced
				readEvent.AddMethod = null;
				readEvent.InvokeMethod = readEvent.RemoveMethod;
				Assert.Null(readEvent.AddMethod);
				Assert.Same(readEvent.RemoveMethod, readEvent.InvokeMethod);
			}
		}

		// Setting the declaring type moves the member from the list of its old type to the list of the new one
		[Fact]
		public void PropertyAndEventMoveToTheTypeThatIsSetAsTheirDeclaringType() {
			var otherType = new TypeDefUser("Ns", "Other", module.CorLibTypes.Object.TypeDefOrRef);
			module.Types.Add(otherType);
			var property = AddProperty("Property");
			var eventDef = new EventDefUser("Changed", module.CorLibTypes.GetTypeRef("System", "EventHandler"));
			type.Events.Add(eventDef);

			property.DeclaringType = otherType;
			eventDef.DeclaringType = otherType;

			Assert.Empty(type.Properties);
			Assert.Empty(type.Events);
			Assert.Same(property, otherType.Properties.Single());
			Assert.Same(eventDef, otherType.Events.Single());
			Assert.Same(module, property.Module);
			Assert.Equal("System.Int32 Ns.Other::Property()", property.FullName);

			// The same type again changes nothing, and null removes the member from its type
			property.DeclaringType = otherType;
			Assert.Single(otherType.Properties);
			property.DeclaringType = null;
			eventDef.DeclaringType = null;
			Assert.Empty(otherType.Properties);
			Assert.Empty(otherType.Events);
			Assert.Null(property.Module);
			Assert.Null(eventDef.Module);
		}
	}
}
