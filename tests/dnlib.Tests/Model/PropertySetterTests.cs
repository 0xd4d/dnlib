// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Model {
	/// <summary>
	/// Every property of the object model that can be set keeps the value it's set to. The objects are the ones
	/// of a module read from a file (their values are read lazily, a setter must also mark the value as read)
	/// and the ones created with the API. Each property is set to the value it has, so nothing changes.
	/// </summary>
	public class PropertySetterTests {
		static readonly Lazy<ModuleDefMD> module = new Lazy<ModuleDefMD>(() => ModuleDefMD.Load(Fixtures.Fixtures.Features.Image));

		static IEnumerable<object> GetObjectsOfTheModule(ModuleDefMD module) {
			yield return module;
			yield return module.Assembly!;
			foreach (var type in module.GetTypes()) {
				yield return type;
				foreach (var o in type.Fields) yield return o;
				foreach (var o in type.Methods) {
					yield return o;
					foreach (var p in o.ParamDefs) yield return p;
					foreach (var p in o.GenericParameters) yield return p;
				}
				foreach (var o in type.Properties) yield return o;
				foreach (var o in type.Events) yield return o;
				foreach (var o in type.GenericParameters) {
					yield return o;
					foreach (var c in o.GenericParamConstraints) yield return c;
				}
				foreach (var o in type.Interfaces) yield return o;
				foreach (var o in type.DeclSecurities) yield return o;
				if (type.ClassLayout is not null) yield return type.ClassLayout;
			}
			foreach (var o in module.GetTypeRefs()) yield return o;
			foreach (var o in module.GetMemberRefs()) yield return o;
			foreach (var o in module.GetAssemblyRefs()) yield return o;
			foreach (var o in module.GetModuleRefs()) yield return o;
			foreach (var o in module.ExportedTypes) yield return o;
			foreach (var o in module.Resources) yield return o;
			for (uint rid = 1; rid <= module.TablesStream.TypeSpecTable.Rows; rid++) yield return module.ResolveTypeSpec(rid)!;
			for (uint rid = 1; rid <= module.TablesStream.MethodSpecTable.Rows; rid++) yield return module.ResolveMethodSpec(rid)!;
			for (uint rid = 1; rid <= module.TablesStream.ImplMapTable.Rows; rid++) yield return module.ResolveImplMap(rid)!;
			for (uint rid = 1; rid <= module.TablesStream.ConstantTable.Rows; rid++) yield return module.ResolveConstant(rid)!;
			for (uint rid = 1; rid <= module.TablesStream.StandAloneSigTable.Rows; rid++) yield return module.ResolveStandAloneSig(rid)!;
			for (uint rid = 1; rid <= module.TablesStream.ManifestResourceTable.Rows; rid++) yield return module.ResolveManifestResource(rid)!;
		}

		static IEnumerable<object> GetUserObjects() {
			var user = new ModuleDefUser("User.dll");
			var types = user.CorLibTypes;
			yield return user;
			yield return new AssemblyDefUser("User");
			yield return new TypeDefUser("Ns", "Type");
			yield return new FieldDefUser("Field", new FieldSig(types.Int32));
			yield return new MethodDefUser("Method", MethodSig.CreateStatic(types.Void));
			yield return new ParamDefUser("param", 1);
			yield return new PropertyDefUser("Property", PropertySig.CreateStatic(types.Int32));
			yield return new EventDefUser("Event", types.Object.TypeDefOrRef);
			yield return new GenericParamUser(0, GenericParamAttributes.NonVariant, "T");
			yield return new GenericParamConstraintUser(types.Object.TypeDefOrRef);
			yield return new InterfaceImplUser(types.Object.TypeDefOrRef);
			yield return new TypeRefUser(user, "Ns", "Ref");
			yield return new MemberRefUser(user, "Member", MethodSig.CreateStatic(types.Void));
			yield return new AssemblyRefUser("Other");
			yield return new ModuleRefUser(user, "Other.dll");
			yield return new ExportedTypeUser(user, 0, "Ns", "Exported", 0, null);
			yield return new TypeSpecUser(types.Int32);
			yield return new MethodSpecUser();
			yield return new ImplMapUser(null, "Function", 0);
			yield return new ConstantUser(1);
			yield return new StandAloneSigUser(new LocalSig(types.Int32));
			yield return new ManifestResourceUser("Resource", null);
			yield return new FileDefUser("File.bin", 0, null);
			yield return new ClassLayoutUser(4, 8);
			yield return new DeclSecurityUser();
		}

		public static TheoryData<string> ObjectKinds => new TheoryData<string> { "module", "user" };

		static IEnumerable<PropertyInfo> GetSettableProperties(Type type) =>
			type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(a => a.CanRead && a.CanWrite && a.GetIndexParameters().Length == 0 && a.SetMethod!.IsPublic);

		[Theory, MemberData(nameof(ObjectKinds))]
		public void EveryPropertyKeepsTheValueItIsSetTo(string kind) {
			var objects = kind == "module" ? GetObjectsOfTheModule(module.Value).ToList() : GetUserObjects().ToList();
			int properties = 0;
			foreach (var o in objects) {
				foreach (var property in GetSettableProperties(o.GetType())) {
					var value = property.GetValue(o);
					property.SetValue(o, value);
					var after = property.GetValue(o);
					// A value type is a copy, a reference type must be the same object
					if (value is null || value.GetType().IsValueType)
						Assert.Equal(value, after);
					else
						Assert.Same(value, after);
					properties++;
				}
			}
			Assert.True(properties > 100, properties.ToString());
		}
	}
}
