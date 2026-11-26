// dnlib: See LICENSE.txt for more info

using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Reading {
	/// <summary>
	/// A property or an event can have "other" methods: methods that belong to it but aren't its getter,
	/// setter, add, remove or raise method. C# can't declare them. A type library importer writes them, eg. a
	/// COM property with a <c>let</c> accessor next to its <c>set</c> accessor. This is IL code like the one
	/// that the tests of System.Reflection.Metadata have a compiled file of (<c>OtherAccessors.dll</c>).
	/// </summary>
	public class OtherAccessorTests {
		const string Source = @"
.assembly extern mscorlib { }
.assembly TestAssembly { }
.module TestAssembly.dll

.class interface public abstract auto ansi IContainer
{
  .method public hidebysig newslot specialname abstract virtual instance string get_Value() cil managed { }
  .method public hidebysig newslot specialname abstract virtual instance void let_Value(string 'value') cil managed { }
  .method public hidebysig newslot specialname abstract virtual instance void set_Value(class IContainer 'value') cil managed { }

  .property string Value()
  {
    .get instance string IContainer::get_Value()
    .set instance void IContainer::set_Value(class IContainer)
    .other instance void IContainer::let_Value(string)
  }
}

.class interface public abstract auto ansi ISource
{
  .method public hidebysig newslot specialname abstract virtual instance void add_Changed(class [mscorlib]System.EventHandler 'value') cil managed { }
  .method public hidebysig newslot specialname abstract virtual instance void remove_Changed(class [mscorlib]System.EventHandler 'value') cil managed { }
  .method public hidebysig newslot specialname abstract virtual instance void suspend_Changed() cil managed { }
  .method public hidebysig newslot specialname abstract virtual instance void resume_Changed() cil managed { }
  .method public hidebysig newslot specialname abstract virtual instance void raise_Changed() cil managed { }
  .method public hidebysig newslot specialname abstract virtual instance void other_Changed() cil managed { }

  .event [mscorlib]System.EventHandler Changed
  {
    .addon instance void ISource::add_Changed(class [mscorlib]System.EventHandler)
    .removeon instance void ISource::remove_Changed(class [mscorlib]System.EventHandler)
    .fire instance void ISource::raise_Changed()
    .other instance void ISource::other_Changed()
    .other instance void ISource::suspend_Changed()
    .other instance void ISource::resume_Changed()
  }
}";

		static readonly byte[] image = IlAssembler.Assemble(Source).Image;

		// In the order of their rows in the MethodSemantics table. ilasm sorts the table by the event or
		// property and doesn't keep the order of the declaration.
		static readonly string[] otherMethodsOfTheEvent = new[] { "resume_Changed", "other_Changed", "suspend_Changed" };

		static void AssertAccessors(ModuleDef module) {
			var property = module.Find("IContainer", isReflectionName: true)!.FindProperty("Value")!;
			Assert.Equal("get_Value", property.GetMethod!.Name);
			Assert.Equal("set_Value", property.SetMethod!.Name);
			Assert.Equal("let_Value", Assert.Single(property.OtherMethods).Name);
			Assert.Single(property.GetMethods);
			Assert.Single(property.SetMethods);

			var eventDef = module.Find("ISource", isReflectionName: true)!.FindEvent("Changed")!;
			Assert.Equal("add_Changed", eventDef.AddMethod!.Name);
			Assert.Equal("remove_Changed", eventDef.RemoveMethod!.Name);
			Assert.Equal("raise_Changed", eventDef.InvokeMethod!.Name);
			Assert.Equal(otherMethodsOfTheEvent, eventDef.OtherMethods.Select(a => a.Name.String));
		}

		[Fact]
		public void OtherMethodsOfAPropertyAndAnEventAreRead() {
			using (var module = ModuleDefMD.Load(image))
				AssertAccessors(module);
		}

		// System.Reflection.Metadata reads the same methods from the file
		[Fact]
		public void AnotherReaderReadsTheSameOtherMethods() {
			using (var peReader = new PEReader(ImmutableArray.Create(image))) {
				var reader = peReader.GetMetadataReader();
				string Name(MethodDefinitionHandle method) => reader.GetString(reader.GetMethodDefinition(method).Name);

				var property = reader.GetPropertyDefinition(reader.PropertyDefinitions.Single()).GetAccessors();
				Assert.Equal("let_Value", Name(property.Others.Single()));
				var eventDef = reader.GetEventDefinition(reader.EventDefinitions.Single()).GetAccessors();
				Assert.Equal("raise_Changed", Name(eventDef.Raiser));
				Assert.Equal(otherMethodsOfTheEvent, eventDef.Others.Select(Name));
			}
		}

		[Theory, InlineData(false), InlineData(true)]
		public void OtherMethodsAreWritten(bool preserveMetadata) {
			var written = preserveMetadata ? RoundTrip.CheckWithPreservedMetadata(image) : RoundTrip.CheckWithNewMetadata(image);

			using (var module = ModuleDefMD.Load(written))
				AssertAccessors(module);
		}
	}
}
