// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Model {
	/// <summary>
	/// <see cref="IMemberRef"/> is implemented by every type and member class. It has a property for every
	/// kind (<c>IsTypeDef</c>, <c>IsMethodSpec</c>, ...) so code doesn't have to cast: each object must say
	/// what it is and nothing else.
	/// </summary>
	public class MemberKindTests {
		// The kind properties of IMemberRef that are true
		static string GetKinds(IMemberRef member) {
			var kinds = new List<string>();
			if (member.IsTypeDef) kinds.Add("TypeDef");
			if (member.IsTypeRef) kinds.Add("TypeRef");
			if (member.IsTypeSpec) kinds.Add("TypeSpec");
			if (member.IsMethodDef) kinds.Add("MethodDef");
			if (member.IsFieldDef) kinds.Add("FieldDef");
			if (member.IsMemberRef) kinds.Add("MemberRef");
			if (member.IsMethodSpec) kinds.Add("MethodSpec");
			if (member.IsPropertyDef) kinds.Add("PropertyDef");
			if (member.IsEventDef) kinds.Add("EventDef");
			if (member.IsGenericParam) kinds.Add("GenericParam");
			// A type, a method or a field in any form (definition or reference)
			if (member.IsType) kinds.Add("type");
			if (member.IsMethod) kinds.Add("method");
			if (member.IsField) kinds.Add("field");
			return string.Join(" ", kinds);
		}

		[Fact]
		public void EveryMemberClassSaysWhatItIs() {
			var module = new ModuleDefUser("Test.dll");
			var types = module.CorLibTypes;
			var typeRef = new TypeRefUser(module, "Ns", "Type", module);
			var methodSig = MethodSig.CreateStatic(types.Void);
			var genericMethodRef = new MemberRefUser(module, "Generic", MethodSig.CreateStaticGeneric(1, types.Void), typeRef);

			Assert.Equal("TypeDef type", GetKinds(new TypeDefUser("Ns", "Type")));
			Assert.Equal("TypeRef type", GetKinds(typeRef));
			Assert.Equal("TypeSpec type", GetKinds(new TypeSpecUser(new SZArraySig(types.Int32))));
			Assert.Equal("MethodDef method", GetKinds(new MethodDefUser("Method", methodSig)));
			Assert.Equal("FieldDef field", GetKinds(new FieldDefUser("Field", new FieldSig(types.Int32))));
			// A MemberRef is a method or a field: its signature says which
			Assert.Equal("MemberRef method", GetKinds(new MemberRefUser(module, "Method", methodSig, typeRef)));
			Assert.Equal("MemberRef field", GetKinds(new MemberRefUser(module, "Field", new FieldSig(types.Int32), typeRef)));
			Assert.Equal("MemberRef", GetKinds(new MemberRefUser(module, "NoSignature")));
			Assert.Equal("MethodSpec method", GetKinds(new MethodSpecUser(genericMethodRef, new GenericInstMethodSig(types.Int32))));
			Assert.Equal("PropertyDef", GetKinds(new PropertyDefUser("Property", PropertySig.CreateStatic(types.Int32))));
			Assert.Equal("EventDef", GetKinds(new EventDefUser("Event", typeRef)));
			Assert.Equal("GenericParam", GetKinds(new GenericParamUser(0, GenericParamAttributes.NonVariant, "T")));
		}

		// The same for everything that a compiled file has
		[Fact]
		public void MembersReadFromAFileSayWhatTheyAre() {
			using (var module = ModuleDefMD.Load(Fixtures.Fixtures.Features.Image)) {
				foreach (var type in module.GetTypes()) {
					Assert.Equal("TypeDef type", GetKinds(type));
					Assert.All(type.Methods, a => Assert.Equal("MethodDef method", GetKinds(a)));
					Assert.All(type.Fields, a => Assert.Equal("FieldDef field", GetKinds(a)));
					Assert.All(type.Properties, a => Assert.Equal("PropertyDef", GetKinds(a)));
					Assert.All(type.Events, a => Assert.Equal("EventDef", GetKinds(a)));
					Assert.All(type.GenericParameters, a => Assert.Equal("GenericParam", GetKinds(a)));
				}
				Assert.All(module.GetTypeRefs(), a => Assert.Equal("TypeRef type", GetKinds(a)));
				Assert.All(module.GetMemberRefs(), a => Assert.Equal(a.IsMethodRef ? "MemberRef method" : "MemberRef field", GetKinds(a)));
				Assert.Contains(module.GetMemberRefs(), a => a.IsFieldRef);
				var tables = module.TablesStream;
				for (uint rid = 1; rid <= tables.TypeSpecTable.Rows; rid++)
					Assert.Equal("TypeSpec type", GetKinds(module.ResolveTypeSpec(rid)!));
				for (uint rid = 1; rid <= tables.MethodSpecTable.Rows; rid++)
					Assert.Equal("MethodSpec method", GetKinds(module.ResolveMethodSpec(rid)!));
				Assert.True(tables.TypeSpecTable.Rows > 0 && tables.MethodSpecTable.Rows > 0);
			}
		}
	}
}
