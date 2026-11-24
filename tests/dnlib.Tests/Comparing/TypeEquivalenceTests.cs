// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Comparing {
	/// <summary>
	/// Type equivalence ("embedded interop types", ECMA-335 doesn't have it, see the CLR's siginfo.cpp): two
	/// types in different assemblies are the same type for the CLR if both have a TypeIdentifierAttribute (or
	/// are COM interfaces with the same GUID) and the same members. <see cref="SigComparer"/> does the same
	/// unless <see cref="SigComparerOptions.DontCheckTypeEquivalence"/> is set.
	/// </summary>
	public sealed class TypeEquivalenceTests : IDisposable {
		// {0} is replaced by the type of Point's second field and of the delegate's parameter
		const string SourceFormat = @"
using System.Runtime.InteropServices;

namespace Interop {{
	[ComImport, Guid(""11111111-2222-3333-4444-555555555555""), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), TypeIdentifier]
	public interface IThing {{
		void Do();
	}}

	[TypeIdentifier(""scope"", ""Interop.Point"")]
	public struct Point {{
		public int X;
		public {0} Y;
	}}

	[TypeIdentifier(""scope"", ""Interop.Color"")]
	public enum Color {{ Red, Green }}

	[TypeIdentifier(""scope"", ""Interop.Callback"")]
	public delegate int Callback({0} value);

	// No TypeIdentifierAttribute
	public struct Plain {{
		public int X;
	}}
}}";

		readonly ModuleDefMD first;
		readonly ModuleDefMD second;
		readonly ModuleDefMD different;
		// Modules that a test created
		readonly List<ModuleDefMD> otherModules = new List<ModuleDefMD>();

		public TypeEquivalenceTests() {
			first = Compile("First", "int");
			second = Compile("Second", "int");
			different = Compile("Different", "long");
		}

		static ModuleDefMD Compile(string assemblyName, string memberType) =>
			ModuleDefMD.Load(CSharpCompiler.Compile(string.Format(SourceFormat, memberType), new CSharpCompilerOptions { AssemblyName = assemblyName }).Image);

		public void Dispose() {
			foreach (var module in otherModules)
				module.Dispose();
			first.Dispose();
			second.Dispose();
			different.Dispose();
		}

		static TypeDef Find(ModuleDef module, string name) => module.Find("Interop." + name, isReflectionName: true)!;

		static bool AreEqual(TypeDef a, TypeDef b, SigComparerOptions options = 0) => new SigComparer(options).Equals(a, b);

		[Theory]
		[InlineData("IThing")]
		[InlineData("Point")]
		[InlineData("Color")]
		[InlineData("Callback")]
		public void TypesWithTheSameIdentityAndMembersAreEqual(string typeName) {
			var a = Find(first, typeName);
			var b = Find(second, typeName);

			Assert.True(AreEqual(a, b));
			Assert.True(AreEqual(b, a));
			Assert.Equal(new SigComparer().GetHashCode(a), new SigComparer().GetHashCode(b));
			Assert.True(new SigComparer().Equals(a.ToTypeSig(), b.ToTypeSig()));
		}

		[Theory]
		[InlineData("IThing")]
		[InlineData("Point")]
		[InlineData("Color")]
		[InlineData("Callback")]
		public void TypesAreNotEqualIfTypeEquivalenceIsNotChecked(string typeName) =>
			Assert.False(AreEqual(Find(first, typeName), Find(second, typeName), SigComparerOptions.DontCheckTypeEquivalence));

		[Fact]
		public void TypesWithoutTheAttributeAreNotEqual() =>
			Assert.False(AreEqual(Find(first, "Plain"), Find(second, "Plain")));

		// Fixed in 5207f81a: the fields of structs and the Invoke methods of delegates weren't compared
		[Theory]
		[InlineData("Point")]
		[InlineData("Callback")]
		public void TypesWithOtherMembersAreNotEqual(string typeName) {
			Assert.False(AreEqual(Find(first, typeName), Find(different, typeName)));
			Assert.False(AreEqual(Find(different, typeName), Find(first, typeName)));
		}

		// The source with 'int' in a new assembly with its own name: the tests change the types, and two types
		// with the same name in assemblies with the same name are the same type without any type equivalence
		ModuleDefMD CompileAnother() {
			var module = Compile("Other" + otherModules.Count, "int");
			otherModules.Add(module);
			return module;
		}

		// Structs must also have the same layout: the CLR's CompareTypeLayout()
		[Fact]
		public void StructsWithAnotherLayoutAreNotEqual() {
			var point = Find(first, "Point");
			TypeDef OtherPoint(Action<TypeDef> change) {
				var type = Find(CompileAnother(), "Point");
				change(type);
				return type;
			}

			Assert.True(AreEqual(point, OtherPoint(type => { })));
			Assert.False(AreEqual(point, OtherPoint(type => type.ClassLayout = new ClassLayoutUser(4, 0))));
			Assert.False(AreEqual(point, OtherPoint(type => type.ClassLayout = new ClassLayoutUser(0, 64))));
			// No layout info is the same as a packing size and a size of 0
			Assert.True(AreEqual(point, OtherPoint(type => type.ClassLayout = new ClassLayoutUser(0, 0))));
			Assert.False(AreEqual(point, OtherPoint(type => type.Attributes |= TypeAttributes.UnicodeClass)));
			Assert.False(AreEqual(point, OtherPoint(type => type.Layout = TypeAttributes.ExplicitLayout)));
			Assert.False(AreEqual(OtherPoint(type => type.Layout = TypeAttributes.ExplicitLayout), point));
			// The CLR's layout is its own choice: such structs are never equivalent
			Assert.False(AreEqual(OtherPoint(type => type.Layout = TypeAttributes.AutoLayout), OtherPoint(type => type.Layout = TypeAttributes.AutoLayout)));
		}

		// Equivalent structs have the same public instance fields and nothing else: the CLR's
		// CompareStructuresForEquivalence()
		[Theory]
		[InlineData("a method")]
		[InlineData("a private field")]
		[InlineData("a static field")]
		[InlineData("one more field")]
		[InlineData("one field less")]
		public void StructsWithOtherMembersAreNotEqual(string change) {
			var point = Find(first, "Point");
			var other = Find(CompileAnother(), "Point");
			var int32 = other.Module!.CorLibTypes.Int32;
			switch (change) {
			case "a method":
				other.Methods.Add(new MethodDefUser("Method", MethodSig.CreateInstance(other.Module.CorLibTypes.Void), MethodAttributes.Public));
				break;
			case "a private field":
				other.FindField("Y")!.Access = FieldAttributes.Private;
				break;
			case "a static field":
				other.Fields.Insert(0, new FieldDefUser("Shared", new FieldSig(int32), FieldAttributes.Public | FieldAttributes.Static));
				break;
			case "one more field":
				other.Fields.Add(new FieldDefUser("Z", new FieldSig(int32), FieldAttributes.Public));
				break;
			case "one field less":
				other.Fields.RemoveAt(1);
				break;
			default:
				throw new ArgumentException(change);
			}

			Assert.False(AreEqual(point, other));
			Assert.False(AreEqual(other, point));
		}

		// The values of an enum are constants: they aren't compared. The type of the values is.
		[Fact]
		public void EnumsAreComparedByTheirUnderlyingType() {
			var color = Find(first, "Color");
			var withOtherValues = Find(CompileAnother(), "Color");
			withOtherValues.Fields.Remove(withOtherValues.FindField("Green")!);
			var withOtherType = Find(CompileAnother(), "Color");
			withOtherType.FindField("value__")!.FieldSig!.Type = withOtherType.Module!.CorLibTypes.Int64;

			Assert.True(AreEqual(color, withOtherValues));
			Assert.False(AreEqual(color, withOtherType));
			// An enum and a struct with the same name aren't the same kind of type
			var structNamedColor = Find(CompileAnother(), "Point");
			structNamedColor.Name = "Color";
			structNamedColor.CustomAttributes.Clear();
			foreach (var attribute in color.CustomAttributes)
				structNamedColor.CustomAttributes.Add(new CustomAttribute(attribute.Constructor, attribute.ConstructorArguments.Select(a => a.Clone())));
			Assert.False(AreEqual(color, structNamedColor));
		}

		[Fact]
		public void DelegateWithoutAnInvokeMethodIsNotEqual() {
			var callback = Find(first, "Callback");
			var other = Find(CompileAnother(), "Callback");
			Assert.True(AreEqual(callback, other));

			other.Methods.Remove(other.FindMethod("Invoke")!);

			Assert.False(AreEqual(callback, other));
			Assert.False(AreEqual(other, callback));
		}

		[Fact]
		public void StructsWithExplicitLayoutMustHaveTheSameFieldOffsets() {
			TypeDef PointWithOffsets(uint? x, uint? y) {
				var type = Find(CompileAnother(), "Point");
				type.Layout = TypeAttributes.ExplicitLayout;
				type.FindField("X")!.FieldOffset = x;
				type.FindField("Y")!.FieldOffset = y;
				return type;
			}

			Assert.True(AreEqual(PointWithOffsets(0, 4), PointWithOffsets(0, 4)));
			Assert.False(AreEqual(PointWithOffsets(0, 4), PointWithOffsets(0, 8)));
			Assert.False(AreEqual(PointWithOffsets(0, 4), PointWithOffsets(4, 4)));
			// A field without an offset: the CLR would compare whatever is in its memory
			Assert.False(AreEqual(PointWithOffsets(0, null), PointWithOffsets(0, null)));
		}

		[Fact]
		public void FieldsOfStructsMustBeMarshaledInTheSameWay() {
			TypeDef PointWithMarshalType(MarshalType? marshalType) {
				var type = Find(CompileAnother(), "Point");
				type.FindField("Y")!.MarshalType = marshalType;
				return type;
			}

			Assert.True(AreEqual(PointWithMarshalType(new MarshalType(NativeType.I4)), PointWithMarshalType(new MarshalType(NativeType.I4))));
			Assert.False(AreEqual(PointWithMarshalType(new MarshalType(NativeType.I4)), PointWithMarshalType(new MarshalType(NativeType.U4))));
			Assert.False(AreEqual(PointWithMarshalType(new MarshalType(NativeType.I4)), PointWithMarshalType(null)));
			Assert.False(AreEqual(PointWithMarshalType(null), PointWithMarshalType(new MarshalType(NativeType.I4))));
			// The same kind with other values
			Assert.False(AreEqual(PointWithMarshalType(new FixedArrayMarshalType(4)), PointWithMarshalType(new FixedArrayMarshalType(8))));
			Assert.True(AreEqual(PointWithMarshalType(new FixedArrayMarshalType(4)), PointWithMarshalType(new FixedArrayMarshalType(4))));
		}

		[Fact]
		public void TypesWithOtherNamesAreNotEqual() =>
			Assert.False(AreEqual(Find(first, "Point"), Find(second, "Color")));

		// A signature that uses the types: 'Point Method(Color)'
		[Fact]
		public void MethodSignaturesWithEquivalentTypesAreEqual() {
			MethodSig CreateSignature(ModuleDef module) =>
				MethodSig.CreateStatic(Find(module, "Point").ToTypeSig(), Find(module, "Color").ToTypeSig());

			Assert.True(new SigComparer().Equals(CreateSignature(first), CreateSignature(second)));
			Assert.False(new SigComparer().Equals(CreateSignature(first), CreateSignature(different)));
			Assert.False(new SigComparer(SigComparerOptions.DontCheckTypeEquivalence).Equals(CreateSignature(first), CreateSignature(second)));
		}
	}
}
