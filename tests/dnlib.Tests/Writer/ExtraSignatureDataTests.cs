// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Writer {
	/// <summary>
	/// A signature blob can be longer than the signature in it: the runtime ignores the bytes after the end
	/// (obfuscators add them). dnlib reads them as the signature's <c>ExtraData</c> and only writes them if the
	/// option <see cref="MetadataFlags.PreserveExtraSignatureData"/> is set. The tests add extra bytes to a
	/// signature of every kind.
	/// </summary>
	public class ExtraSignatureDataTests {
		static readonly byte[] extraData = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

		// A module that has a signature of every kind, all with extra data
		static CreatedModule CreateModule() {
			var created = new CreatedModule();
			var types = created.Module.CorLibTypes;

			created.AddField("Field", types.Int32).FieldSig!.ExtraData = extraData;

			var property = new PropertyDefUser("Property", PropertySig.CreateStatic(types.Int32));
			property.PropertySig!.ExtraData = extraData;
			created.Type.Properties.Add(property);

			// A method with a local, a call of an instantiated generic method and a type that's a TypeSpec
			var genericMethod = new MemberRefUser(created.Module, "Generic", MethodSig.CreateStaticGeneric(1, types.Void), types.GetTypeRef("System", "SomeClass"));
			genericMethod.MethodSig!.ExtraData = extraData;
			var instantiation = new GenericInstMethodSig(types.Int32) { ExtraData = extraData };
			var arrayType = new TypeSpecUser(new SZArraySig(types.Int32)) { ExtraData = extraData };
			var method = created.AddMethod("Method",
				OpCodes.Ldtoken.ToInstruction(new MethodSpecUser(genericMethod, instantiation)), OpCodes.Pop.ToInstruction(),
				OpCodes.Ldtoken.ToInstruction(arrayType), OpCodes.Pop.ToInstruction(),
				OpCodes.Ret.ToInstruction());
			method.MethodSig!.ExtraData = extraData;
			method.Body!.Variables.Add(new Local(types.Int32));
			return created;
		}

		// The extra data of every signature of the written module: "field", "property", ... or nothing
		static string GetSignaturesWithExtraData(ModuleDefMD module) {
			var type = CreatedModule.GetType(module);
			var method = type.FindMethod("Method")!;
			var instructions = method.Body!.Instructions;
			var methodSpec = (MethodSpec)instructions[0].Operand!;
			var typeSpec = (TypeSpec)instructions[2].Operand!;
			bool HasExtraData(byte[]? data) {
				if (data is null)
					return false;
				Assert.Equal(extraData, data);
				return true;
			}
			var found = new[] {
				HasExtraData(type.FindField("Field")!.FieldSig!.ExtraData) ? "field" : null,
				HasExtraData(type.FindProperty("Property")!.PropertySig!.ExtraData) ? "property" : null,
				HasExtraData(method.MethodSig!.ExtraData) ? "method" : null,
				HasExtraData(methodSpec.Method!.MethodSig!.ExtraData) ? "member reference" : null,
				HasExtraData(methodSpec.GenericInstMethodSig!.ExtraData) ? "generic method arguments" : null,
				HasExtraData(typeSpec.ExtraData) ? "type" : null,
			};
			return string.Join(", ", found.Where(a => a is not null));
		}

		static byte[] Write(ModuleDef module, bool preserveExtraData) {
			var options = new ModuleWriterOptions(module);
			if (preserveExtraData)
				options.MetadataOptions.Flags |= MetadataFlags.PreserveExtraSignatureData;
			var stream = new MemoryStream();
			module.Write(stream, options);
			return stream.ToArray();
		}

		[Fact]
		public void ExtraDataIsWrittenWithTheOptionAndReadAgain() {
			var image = Write(CreateModule().Module, preserveExtraData: true);

			using (var module = ModuleDefMD.Load(image)) {
				Assert.Equal("field, property, method, member reference, generic method arguments, type", GetSignaturesWithExtraData(module));

				// It survives another write with the option, and is removed without it
				using (var again = ModuleDefMD.Load(Write(module, preserveExtraData: true)))
					Assert.Equal("field, property, method, member reference, generic method arguments, type", GetSignaturesWithExtraData(again));
				using (var removed = ModuleDefMD.Load(Write(module, preserveExtraData: false)))
					Assert.Equal("", GetSignaturesWithExtraData(removed));
			}
		}

		[Fact]
		public void ExtraDataIsNotWrittenWithoutTheOption() {
			var image = Write(CreateModule().Module, preserveExtraData: false);

			using (var module = ModuleDefMD.Load(image))
				Assert.Equal("", GetSignaturesWithExtraData(module));
		}

		// The runtime ignores the extra bytes: the field and the method of the written file can be used
		[Fact]
		public void RuntimeLoadsSignaturesWithExtraData() {
			Assert.SkipWhen(Type.GetType("Mono.Runtime") is not null, "Mono doesn't accept every signature with extra data");
			var created = new CreatedModule();
			created.AddField("Field", created.Module.CorLibTypes.Int32).FieldSig!.ExtraData = extraData;
			var method = created.AddMethod("Method", OpCodes.Ret.ToInstruction());
			method.MethodSig!.ExtraData = extraData;

			using (var loaded = new LoadedAssembly(Write(created.Module, preserveExtraData: true))) {
				var type = loaded.Assembly.GetType("Ns.Type")!;
				type.GetField("Field")!.SetValue(null, 5);
				Assert.Equal(5, type.GetField("Field")!.GetValue(null));
				type.GetMethod("Method")!.Invoke(null, null);
			}
		}
	}
}
