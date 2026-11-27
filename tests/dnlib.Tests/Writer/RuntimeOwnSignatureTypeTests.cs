// dnlib: See LICENSE.txt for more info

using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.MD;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// The runtime has element types that it only uses in its own signatures (ELEMENT_TYPE_VAR_ZAPSIG,
	/// ELEMENT_TYPE_NATIVE_VALUETYPE_ZAPSIG and ELEMENT_TYPE_CANON_ZAPSIG in the runtime's corpriv.h), but it
	/// also reads them in a method signature of a file. dnlib reads them (<see cref="VarZapSig"/>,
	/// <see cref="NativeValueTypeZapSig"/>, <see cref="CanonZapSig"/>) and writes the same bytes.
	/// </summary>
	public class RuntimeOwnSignatureTypeTests {
		static byte[] GetSignatureBlob(ModuleDefMD module, MethodDef method) {
			Assert.True(module.TablesStream.TryReadMethodRow(method.Rid, out var row));
			return module.BlobStream.Read(row.Signature)!;
		}

		// Fixed in this commit: the writer had no case for these types, and the reader read them as null
		[Fact]
		public void MethodSignatureWithTheseTypesIsWrittenAndReadBack() {
			var created = new CreatedModule();
			var types = created.Module.CorLibTypes;
			var method = created.AddMethod("Method", OpCodes.Ret.ToInstruction());
			method.MethodSig = MethodSig.CreateStatic(new CanonZapSig(),
				new NativeValueTypeZapSig(types.Byte),
				new SZArraySig(new NativeValueTypeZapSig(new NativeValueTypeZapSig(types.Int32))),
				new VarZapSig(1, created.Module),
				new ByRefSig(new VarZapSig(0x1234, created.Module)),
				new CanonZapSig(),
				types.String);

			using (var module = created.WriteAndRead()) {
				var readMethod = CreatedModule.GetType(module).FindMethod("Method")!;

				Assert.Equal(new byte[] { 0x00, 0x06, 0x3E, 0x3D, 0x05, 0x1D, 0x3D, 0x3D, 0x08, 0x3B, 0x01, 0x10, 0x3B, 0x92, 0x34, 0x3E, 0x0E },
					GetSignatureBlob(module, readMethod));
				var signature = readMethod.MethodSig!;
				Assert.IsType<CanonZapSig>(signature.RetType);
				Assert.Equal(new[] { "native System.Byte", "native native System.Int32[]", "GenericParam(1)", "GenericParam(4660)&", "System.__Canon", "System.String" },
					signature.Params.Select(a => a!.FullName));
				Assert.Equal(6, readMethod.Parameters.Count);
				// A rid is a row of a module: the signature that was read has the module that was read, which
				// isn't the module that was written, so the two signatures aren't equal
				Assert.False(new SigComparer().Equals(method.MethodSig, signature));
				var sameModule = MethodSig.CreateStatic(new CanonZapSig(),
					new NativeValueTypeZapSig(types.Byte),
					new SZArraySig(new NativeValueTypeZapSig(new NativeValueTypeZapSig(types.Int32))),
					new VarZapSig(1, module),
					new ByRefSig(new VarZapSig(0x1234, module)),
					new CanonZapSig(),
					types.String);
				Assert.True(new SigComparer().Equals(sameModule, signature));
				Assert.Equal(new SigComparer().GetHashCode(sameModule), new SigComparer().GetHashCode(signature));
			}
		}

		[Fact]
		public void TheyAreWrittenInTheSignaturesOfLocalsFieldsAndTypeSpecs() {
			var created = new CreatedModule();
			var types = created.Module.CorLibTypes;
			created.AddField("Field", new NativeValueTypeZapSig(types.Int32));
			var typeSpec = new TypeSpecUser(new SZArraySig(new CanonZapSig()));
			var method = created.AddMethod("Method", OpCodes.Ldtoken.ToInstruction(typeSpec), OpCodes.Pop.ToInstruction(), OpCodes.Ret.ToInstruction());
			method.Body!.Variables.Add(new Local(new VarZapSig(2, created.Module)));

			using (var module = created.WriteAndRead()) {
				var type = CreatedModule.GetType(module);
				var readMethod = type.FindMethod("Method")!;

				Assert.Equal("native System.Int32", type.FindField("Field")!.FieldType!.FullName);
				Assert.Equal(2U, Assert.IsType<VarZapSig>(readMethod.Body!.Variables[0].Type).GenericParamRid);
				var readTypeSpec = Assert.IsAssignableFrom<TypeSpec>(readMethod.Body.Instructions[0].Operand);
				Assert.IsType<CanonZapSig>(Assert.IsType<SZArraySig>(readTypeSpec.TypeSig).Next);
			}
		}

		// The rid isn't the generic parameter itself: it's written as it is
		[Fact]
		public void GenericParameterRowIsNotAGenericParameterOfTheSignature() {
			var created = new CreatedModule();
			created.Type.GenericParameters.Add(new GenericParamUser(0, GenericParamAttributes.NonVariant, "T"));
			var method = created.AddMethod("Method", OpCodes.Ret.ToInstruction());
			method.MethodSig = MethodSig.CreateStatic(created.Module.CorLibTypes.Void, new GenericVar(0), new VarZapSig(1, created.Module));

			using (var module = created.WriteAndRead()) {
				var signature = module.Find("Ns.Type", isReflectionName: true)!.FindMethod("Method")!.MethodSig!;

				Assert.True(signature.Params[0]!.IsGenericParameter);
				Assert.False(signature.Params[1]!.IsGenericParameter);
				Assert.True(signature.Params[1]!.ContainsGenericParameter);
				Assert.False(new SigComparer().Equals(signature.Params[0], signature.Params[1]));

				// It knows the module it was read from, so the generic parameter with that rid is known
				var withRid = Assert.IsType<VarZapSig>(signature.Params[1]);
				Assert.Same(module, withRid.OwnerModule);
				Assert.Same(module.Find("Ns.Type", isReflectionName: true)!.GenericParameters[0], withRid.GenericParam);
				Assert.Equal("T", withRid.FullName);
				Assert.Equal("T", withRid.ReflectionName);
				Assert.Equal("Ns", withRid.ReflectionNamespace);
				Assert.Equal("T[]", new SZArraySig(withRid).FullName);
				// With a rid that isn't a row, or in a module that wasn't read from a file, it only has the rid
				Assert.Null(new VarZapSig(2, module).GenericParam);
				Assert.Null(new VarZapSig(0, module).GenericParam);
				Assert.Equal("GenericParam(2)", new VarZapSig(2, module).FullName);
				Assert.Null(new VarZapSig(1, created.Module).GenericParam);
				Assert.False(new NativeValueTypeZapSig(created.Module.CorLibTypes.Int32).ContainsGenericParameter);
				Assert.True(new NativeValueTypeZapSig(new GenericMVar(0)).ContainsGenericParameter);
				Assert.False(new CanonZapSig().ContainsGenericParameter);
			}
		}
	}
}
