// dnlib: See LICENSE.txt for more info

using dnlib.DotNet;
using dnlib.Tests.Writer;
using Xunit;

namespace dnlib.Tests.Signatures {
	/// <summary>
	/// A signature can use a TypeSpec row (eg. as a custom modifier), whose signature can use another TypeSpec
	/// row, and so on
	/// </summary>
	public class TypeSpecChainTests {
		// Creates a field whose type is 'int32 modopt(TypeSpec 1)', where TypeSpec 1 is
		// 'int32 modopt(TypeSpec 2)', ..., and the last TypeSpec is 'int32 modopt(System.Object)'.
		// Returns the number of TypeSpecs that dnlib reads when the written module is read.
		static int WriteAndCountTypeSpecs(int typeSpecCount) {
			var created = new CreatedModule();
			var int32 = created.Module.CorLibTypes.Int32;
			ITypeDefOrRef modifier = created.Module.CorLibTypes.Object.TypeDefOrRef!;
			for (int i = 0; i < typeSpecCount; i++)
				modifier = new TypeSpecUser(new CModOptSig(modifier, int32));
			created.AddField("Field", new CModOptSig(modifier, int32));

			using (var module = created.WriteAndRead()) {
				Assert.Equal((uint)typeSpecCount, module.TablesStream.TypeSpecTable.Rows);
				int count = 0;
				var signature = CreatedModule.GetType(module).FindField("Field")!.FieldSig!.Type as CModOptSig;
				while (signature?.Modifier is TypeSpec typeSpec) {
					count++;
					signature = typeSpec.TypeSig as CModOptSig;
				}
				return count;
			}
		}

		// Fixed in e1d7e07f: only 4 were read
		[Theory, InlineData(1), InlineData(5), InlineData(32)]
		public void ChainOfTypeSpecsIsRead(int typeSpecCount) =>
			Assert.Equal(typeSpecCount, WriteAndCountTypeSpecs(typeSpecCount));

		// The chain could also be a loop, so there's a limit
		[Fact]
		public void VeryLongChainOfTypeSpecsEndsAtALimit() {
			int count = WriteAndCountTypeSpecs(500);

			Assert.InRange(count, 32, 499);
		}
	}
}
