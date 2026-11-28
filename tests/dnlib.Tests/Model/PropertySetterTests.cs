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
			// Signatures and marshal types
			yield return new GenericInstSig(new ClassSig(types.Object.TypeDefOrRef), types.Int32);
			yield return new ArraySig(types.Int32, 2);
			yield return new ValueArraySig(types.Int32, 3);
			yield return new ModuleSig(1, types.Int32);
			yield return new VarZapSig(1, user);
			yield return MethodSig.CreateStatic(types.Void);
			yield return new FieldSig(types.Int32);
			yield return new RawMarshalType(new byte[] { 1 });
			yield return new FixedSysStringMarshalType(4);
			yield return new SafeArrayMarshalType(VariantType.I4, types.Object.TypeDefOrRef);
			yield return new FixedArrayMarshalType(4, NativeType.I4);
			yield return new ArrayMarshalType(NativeType.I4, 1, 2, 3);
			yield return new CustomMarshalType("guid", "native", types.Object.TypeDefOrRef, "cookie");
			yield return new InterfaceMarshalType(NativeType.IUnknown, 1);
			// Options of the writer
			yield return new dnlib.DotNet.Writer.Cor20HeaderOptions();
			yield return new dnlib.DotNet.Writer.PEHeadersOptions();
			yield return new dnlib.DotNet.Writer.MetadataHeaderOptions();
			yield return new dnlib.DotNet.Writer.TablesHeapOptions();
		}

		// The constructors that take some of the values
		[Fact]
		public void TheConstructorsStoreTheirValues() {
			var types = new ModuleDefUser("User.dll").CorLibTypes;
			var type = types.Object.TypeDefOrRef;

			Assert.Equal(type, new SafeArrayMarshalType(type).UserDefinedSubType);
			Assert.Equal(VariantType.NotInitialized, new SafeArrayMarshalType(type).VariantType);
			Assert.Equal("guid", new CustomMarshalType("guid").Guid);
			Assert.Equal("native", new CustomMarshalType("guid", "native").NativeTypeName);
			Assert.Same(type, new CustomMarshalType("guid", "native", type).CustomMarshaler);
			Assert.Throws<ArgumentException>(() => new InterfaceMarshalType(NativeType.I4, 1));
			Assert.Equal(1U, new GenericVar(1).Number);
			Assert.Equal(2U, new GenericMVar(2).Number);
			Assert.Equal(ElementType.Sentinel, new SentinelSig().ElementType);

			var array = new ArraySig(types.Int32);
			Assert.Equal(0U, array.Rank);
			Assert.Empty(array.GetSizes());
			var withBounds = new ArraySig(types.Int32, 2, (IEnumerable<uint>)new uint[] { 3 }, (IEnumerable<int>)new[] { 1, 0 });
			Assert.Equal(new uint[] { 3 }, withBounds.GetSizes());
			Assert.Equal(new[] { 1, 0 }, withBounds.GetLowerBounds());
			Assert.Equal(2U, new ArraySig(types.Int32, 2U, (IEnumerable<uint>)new uint[0], (IEnumerable<int>)new int[0]).Rank);
			var szArray = new SZArraySig(types.Int32);
			Assert.Equal(1U, szArray.Rank);
			Assert.Empty(szArray.GetSizes());
			Assert.Empty(szArray.GetLowerBounds());
			Assert.Throws<NotSupportedException>(() => szArray.Rank = 2);

			var flags = dnlib.DotNet.MD.ComImageFlags.ILOnly;
			Assert.Equal(flags, new dnlib.DotNet.Writer.Cor20HeaderOptions(flags).Flags);
			var cor20 = new dnlib.DotNet.Writer.Cor20HeaderOptions(2, 5, flags);
			Assert.Equal((ushort)2, cor20.MajorRuntimeVersion);
			Assert.Equal((ushort)5, cor20.MinorRuntimeVersion);
		}

		// The text of a marshal type has its values
		[Fact]
		public void MarshalTypesAreShownWithTheirValues() {
			var type = new ModuleDefUser("User.dll").CorLibTypes.Object.TypeDefOrRef;

			Assert.Equal("SafeArray (I4, System.Object)", new SafeArrayMarshalType(VariantType.I4, type).ToString());
			Assert.Equal("CustomMarshaler (guid, native, System.Object, cookie)", new CustomMarshalType("guid", "native", type, "cookie").ToString());
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
