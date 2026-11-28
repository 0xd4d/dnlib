// dnlib: See LICENSE.txt for more info

using System;
using System.Collections;
using System.Linq;
using dnlib.DotNet;
using Xunit;

namespace dnlib.Tests.Model {
	/// <summary>
	/// Small parts of the API: the hash algorithms that have a .NET name, the element type of a constant that's
	/// created from a value, which ITypes contain a generic parameter, and the vtables of a module.
	/// </summary>
	public class SmallApiTests {
		// Only the algorithms that .NET has are named
		[Theory]
		[InlineData(AssemblyHashAlgorithm.MD5, "MD5")]
		[InlineData(AssemblyHashAlgorithm.SHA1, "SHA1")]
		[InlineData(AssemblyHashAlgorithm.SHA_256, "SHA256")]
		[InlineData(AssemblyHashAlgorithm.MD2, null)]
		[InlineData(AssemblyHashAlgorithm.MD4, null)]
		[InlineData(AssemblyHashAlgorithm.MAC, null)]
		[InlineData(AssemblyHashAlgorithm.SSL3_SHAMD5, null)]
		[InlineData(AssemblyHashAlgorithm.HMAC, null)]
		[InlineData(AssemblyHashAlgorithm.TLS1PRF, null)]
		[InlineData(AssemblyHashAlgorithm.HASH_REPLACE_OWF, null)]
		public void HashAlgorithmsHaveTheirDotNetNames(AssemblyHashAlgorithm algorithm, string? expectedName) =>
			Assert.Equal(expectedName, algorithm.GetName());

		[Theory]
		[InlineData(true, ElementType.Boolean)]
		[InlineData('c', ElementType.Char)]
		[InlineData((sbyte)1, ElementType.I1)]
		[InlineData((byte)1, ElementType.U1)]
		[InlineData((short)1, ElementType.I2)]
		[InlineData((ushort)1, ElementType.U2)]
		[InlineData(1, ElementType.I4)]
		[InlineData(1U, ElementType.U4)]
		[InlineData(1L, ElementType.I8)]
		[InlineData(1UL, ElementType.U8)]
		[InlineData(1.5f, ElementType.R4)]
		[InlineData(1.5, ElementType.R8)]
		[InlineData("text", ElementType.String)]
		[InlineData(null, ElementType.Class)]
		public void AConstantHasTheElementTypeOfItsValue(object? value, ElementType expected) {
			var constant = new ConstantUser(value);

			Assert.Equal(expected, constant.Type);
			Assert.Equal(value, constant.Value);
		}

		[Fact]
		public void AConstantOfAnotherTypeIsVoid() {
			Assert.Equal(ElementType.Void, new ConstantUser(DateTime.MinValue).Type);
			Assert.Equal(ElementType.End, new ConstantUser().Type);
		}

		[Fact]
		public void EveryKindOfITypeIsCheckedForGenericParameters() {
			var module = new ModuleDefUser("Test.dll");
			var exported = new ExportedTypeUser(module, 0, "Ns", "Exported", TypeAttributes.Public, new AssemblyRefUser("Other"));

			Assert.True(TypeHelper.ContainsGenericParameter((IType)new GenericVar(0)));
			Assert.True(TypeHelper.ContainsGenericParameter((IType)new TypeSpecUser(new GenericMVar(0))));
			Assert.False(TypeHelper.ContainsGenericParameter((IType)exported));
			Assert.False(TypeHelper.ContainsGenericParameter(exported));
			Assert.True(TypeHelper.ContainsGenericParameter((CallingConventionSig)new FieldSig(new GenericVar(0))));
			Assert.True(TypeHelper.ContainsGenericParameter((CallingConventionSig)new LocalSig(new GenericVar(0))));
			Assert.True(TypeHelper.ContainsGenericParameter((CallingConventionSig)new GenericInstMethodSig(new GenericMVar(0))));
			Assert.False(TypeHelper.ContainsGenericParameter((CallingConventionSig)new LocalSig(module.CorLibTypes.Int32)));
		}

		// A module can get vtables (the methods that native code calls) created with the API
		[Fact]
		public void VTablesAreCreatedWithTheirFlags() {
			var vtables = new VTableFixups();
			var vtable = new VTable(VTableFlags.Bit64 | VTableFlags.FromUnmanaged);
			vtable.Methods.Add(null);
			vtables.VTables.Add(vtable);
			vtables.VTables.Add(new VTable());

			Assert.Equal(2, vtables.VTables.Count);
			Assert.Same(vtable, ((IEnumerable)vtables).Cast<VTable>().First());
			Assert.Equal(VTableFlags.Bit64 | VTableFlags.FromUnmanaged, vtable.Flags);
			Assert.True(vtable.Is64Bit);
			Assert.Null(((IEnumerable)vtable).Cast<IMethod?>().Single());
			Assert.Empty(vtables.VTables[1].Methods);
			Assert.NotNull(vtable.ToString());
		}
	}
}
