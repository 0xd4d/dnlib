// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using Xunit;
using FieldAttributes = dnlib.DotNet.FieldAttributes;
using MethodAttributes = dnlib.DotNet.MethodAttributes;
using MethodImplAttributes = dnlib.DotNet.MethodImplAttributes;
using TypeAttributes = dnlib.DotNet.TypeAttributes;

namespace dnlib.Tests.Model {
	/// <summary>
	/// The model classes have a <c>bool</c> property for most bits of their flags (<c>MethodDef.IsStatic</c>,
	/// <c>TypeDef.IsSealed</c>, ...), and read-only properties for the values of a group of bits (the access
	/// of a member, the layout of a type, ...). There are a few hundred of them, so the properties that can
	/// be set are found with reflection.
	/// </summary>
	public class FlagPropertyTests {
		// An object of every class that has such properties, without flags where the constructor takes them
		static IEnumerable<object> CreateObjects() {
			var module = new ModuleDefUser("Test.dll");
			yield return module;
			yield return new AssemblyDefUser("Test");
			yield return new AssemblyRefUser("Test");
			yield return new TypeDefUser("Type") { Attributes = 0 };
			yield return new FieldDefUser("Field", new FieldSig(module.CorLibTypes.Int32), 0);
			yield return new MethodDefUser("Method", MethodSig.CreateStatic(module.CorLibTypes.Void), 0, 0);
			yield return new ParamDefUser("Parameter", 1, 0);
			yield return new PropertyDefUser("Property", PropertySig.CreateStatic(module.CorLibTypes.Int32), 0);
			yield return new EventDefUser("Event", null, 0);
			yield return new GenericParamUser(0, 0, "T");
			yield return new ExportedTypeUser(module, 0, "Ns", "Type", 0, null);
			yield return new ImplMapUser(null, "Name", 0);
			yield return new FileDefUser("File", 0, null);
		}

		public static IEnumerable<object[]> ObjectTypes() => CreateObjects().Select(a => new object[] { a.GetType() });

		// Every bool property that can be set
		static List<PropertyInfo> GetFlagProperties(Type type) =>
			type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(a => a.PropertyType == typeof(bool) && a.CanWrite && a.GetIndexParameters().Length == 0)
				.OrderBy(a => a.Name, StringComparer.Ordinal).ToList();

		// The values of all bool properties, eg. "IsStatic=True IsFinal=False ..."
		static string GetValues(object obj, List<PropertyInfo> properties) =>
			string.Join(" ", properties.Select(a => $"{a.Name}={a.GetValue(obj)}"));

		// A property that's changed has its new value. Changing it back gives the object it was before: no
		// other property has another value. Most properties are false if no flag is set, but not all (eg.
		// MethodDef.IsManaged is true if the Unmanaged bit isn't set).
		[Theory]
		[MemberData(nameof(ObjectTypes))]
		public void FlagPropertyCanBeChangedAndChangedBack(Type type) {
			var properties = GetFlagProperties(type);
			Assert.NotEmpty(properties);
			foreach (var property in properties) {
				var obj = CreateObjects().Single(a => a.GetType() == type);
				string before = GetValues(obj, properties);
				bool original = (bool)property.GetValue(obj)!;

				property.SetValue(obj, !original);
				Assert.True((bool)property.GetValue(obj)! == !original, $"{type.Name}.{property.Name} is {original} after it was set to {!original}");

				property.SetValue(obj, original);
				Assert.True((bool)property.GetValue(obj)! == original, $"{type.Name}.{property.Name} is {!original} after it was set to {original}");
				Assert.Equal(before, GetValues(obj, properties));
			}
		}

		[Theory]
		[InlineData(MethodAttributes.PrivateScope, "IsCompilerControlled IsPrivateScope")]
		[InlineData(MethodAttributes.Private, "IsPrivate")]
		[InlineData(MethodAttributes.FamANDAssem, "IsFamilyAndAssembly")]
		[InlineData(MethodAttributes.Assembly, "IsAssembly")]
		[InlineData(MethodAttributes.Family, "IsFamily")]
		[InlineData(MethodAttributes.FamORAssem, "IsFamilyOrAssembly")]
		[InlineData(MethodAttributes.Public, "IsPublic")]
		public void MethodAccess(MethodAttributes access, string expectedTrueProperties) {
			var method = new MethodDefUser("Method", null, MethodAttributes.Static | MethodAttributes.HideBySig) { Access = access };

			Assert.Equal(access, method.Access);
			Assert.Equal(access | MethodAttributes.Static | MethodAttributes.HideBySig, method.Attributes);
			AssertOnlyTheseAreTrue(method, expectedTrueProperties, "IsCompilerControlled IsPrivateScope IsPrivate IsFamilyAndAssembly IsAssembly IsFamily IsFamilyOrAssembly IsPublic");
		}

		[Theory]
		[InlineData(FieldAttributes.PrivateScope, "IsCompilerControlled IsPrivateScope")]
		[InlineData(FieldAttributes.Private, "IsPrivate")]
		[InlineData(FieldAttributes.FamANDAssem, "IsFamilyAndAssembly")]
		[InlineData(FieldAttributes.Assembly, "IsAssembly")]
		[InlineData(FieldAttributes.Family, "IsFamily")]
		[InlineData(FieldAttributes.FamORAssem, "IsFamilyOrAssembly")]
		[InlineData(FieldAttributes.Public, "IsPublic")]
		public void FieldAccess(FieldAttributes access, string expectedTrueProperties) {
			var field = new FieldDefUser("Field", null, FieldAttributes.Static) { Access = access };

			Assert.Equal(access, field.Access);
			Assert.Equal(access | FieldAttributes.Static, field.Attributes);
			AssertOnlyTheseAreTrue(field, expectedTrueProperties, "IsCompilerControlled IsPrivateScope IsPrivate IsFamilyAndAssembly IsAssembly IsFamily IsFamilyOrAssembly IsPublic");
		}

		[Theory]
		[InlineData(TypeAttributes.NotPublic, "IsNotPublic")]
		[InlineData(TypeAttributes.Public, "IsPublic")]
		[InlineData(TypeAttributes.NestedPublic, "IsNestedPublic")]
		[InlineData(TypeAttributes.NestedPrivate, "IsNestedPrivate")]
		[InlineData(TypeAttributes.NestedFamily, "IsNestedFamily")]
		[InlineData(TypeAttributes.NestedAssembly, "IsNestedAssembly")]
		[InlineData(TypeAttributes.NestedFamANDAssem, "IsNestedFamilyAndAssembly")]
		[InlineData(TypeAttributes.NestedFamORAssem, "IsNestedFamilyOrAssembly")]
		public void TypeVisibility(TypeAttributes visibility, string expectedTrueProperties) {
			var type = new TypeDefUser("Type") { Attributes = TypeAttributes.Sealed };
			type.Visibility = visibility;

			Assert.Equal(visibility, type.Visibility);
			Assert.Equal(visibility | TypeAttributes.Sealed, type.Attributes);
			AssertOnlyTheseAreTrue(type, expectedTrueProperties, "IsNotPublic IsPublic IsNestedPublic IsNestedPrivate IsNestedFamily IsNestedAssembly IsNestedFamilyAndAssembly IsNestedFamilyOrAssembly");
		}

		[Theory]
		[InlineData(TypeAttributes.AutoLayout, "IsAutoLayout")]
		[InlineData(TypeAttributes.SequentialLayout, "IsSequentialLayout")]
		[InlineData(TypeAttributes.ExplicitLayout, "IsExplicitLayout")]
		public void TypeLayout(TypeAttributes layout, string expectedTrueProperties) {
			var type = new TypeDefUser("Type") { Attributes = TypeAttributes.Public };
			type.Layout = layout;

			Assert.Equal(layout, type.Layout);
			AssertOnlyTheseAreTrue(type, expectedTrueProperties, "IsAutoLayout IsSequentialLayout IsExplicitLayout");
		}

		[Theory]
		[InlineData(TypeAttributes.AnsiClass, "IsAnsiClass")]
		[InlineData(TypeAttributes.UnicodeClass, "IsUnicodeClass")]
		[InlineData(TypeAttributes.AutoClass, "IsAutoClass")]
		[InlineData(TypeAttributes.CustomFormatClass, "IsCustomFormatClass")]
		public void TypeStringFormat(TypeAttributes stringFormat, string expectedTrueProperties) {
			var type = new TypeDefUser("Type") { Attributes = TypeAttributes.Public };
			type.StringFormat = stringFormat;

			Assert.Equal(stringFormat, type.StringFormat);
			AssertOnlyTheseAreTrue(type, expectedTrueProperties, "IsAnsiClass IsUnicodeClass IsAutoClass IsCustomFormatClass");
		}

		[Theory]
		[InlineData(MethodImplAttributes.IL, "IsIL")]
		[InlineData(MethodImplAttributes.Native, "IsNative")]
		[InlineData(MethodImplAttributes.OPTIL, "IsOPTIL")]
		[InlineData(MethodImplAttributes.Runtime, "IsRuntime")]
		public void MethodCodeType(MethodImplAttributes codeType, string expectedTrueProperties) {
			var method = new MethodDefUser("Method", null, MethodImplAttributes.NoInlining, 0) { CodeType = codeType };

			Assert.Equal(codeType, method.CodeType);
			Assert.Equal(codeType | MethodImplAttributes.NoInlining, method.ImplAttributes);
			AssertOnlyTheseAreTrue(method, expectedTrueProperties, "IsIL IsNative IsOPTIL IsRuntime");
		}

		// Of the properties in 'all' (names with spaces between them), only those in 'expectedTrue' are true
		static void AssertOnlyTheseAreTrue(object obj, string expectedTrue, string all) {
			var trueProperties = all.Split(' ').Where(name => (bool)obj.GetType().GetProperty(name)!.GetValue(obj)!);
			Assert.Equal(expectedTrue, string.Join(" ", trueProperties));
		}
	}
}
