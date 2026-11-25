// dnlib: See LICENSE.txt for more info

using dnlib.DotNet;
using dnlib.PE;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Infrastructure {
	public class IlAssemblerTests {
		// A global method can't be written in C#
		const string Source = @"
.assembly extern mscorlib { }
.assembly TestAssembly { }
.module TestAssembly.dll

.method public static int32 GlobalMethod() cil managed {
	ldc.i4.s 42
	ret
}";

		[Fact]
		public void AssembledModuleCanBeLoaded() {
			var assembly = IlAssembler.Assemble(Source);

			using (var module = ModuleDefMD.Load(assembly.Image)) {
				var method = module.GlobalType!.FindMethod("GlobalMethod");
				Assert.NotNull(method);
				Assert.Equal(2, method.Body!.Instructions.Count);
			}
		}

		[Fact]
		public void ExtraArgumentsAreUsed() {
			var options = new IlAssemblerOptions();
			options.ExtraArguments.Add("/PE64");
			options.ExtraArguments.Add("/X64");
			var assembly = IlAssembler.Assemble(Source, options);

			using (var module = ModuleDefMD.Load(assembly.Image))
				Assert.Equal(Machine.AMD64, module.Machine);
		}

		[Fact]
		public void SameSourceGivesSameFile() =>
			Assert.Equal(IlAssembler.Assemble(Source).Image, IlAssembler.Assemble(Source).Image);

		[Fact]
		public void InvalidSourceThrows() =>
			Assert.Throws<System.InvalidOperationException>(() => IlAssembler.Assemble(".method bad"));
	}
}
