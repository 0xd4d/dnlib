// dnlib: See LICENSE.txt for more info

using System.Linq;
using dnlib.DotNet;
using dnlib.TestUtils;
using Xunit;

namespace dnlib.Tests.Infrastructure {
	public class CSharpCompilerTests {
		const string Source = @"
namespace Ns {
	public class MyClass {
		public int Add(int a, int b) => a + b;
	}
}";

		[Fact]
		public void CompiledAssemblyCanBeLoaded() {
			var assembly = CSharpCompiler.Compile(Source);

			using (var module = ModuleDefMD.Load(assembly.Image)) {
				var type = module.Find("Ns.MyClass", isReflectionName: true);
				Assert.NotNull(type);
				Assert.NotNull(type.FindMethod("Add"));
			}
			Assert.Null(assembly.Pdb);
		}

		[Fact]
		public void PortablePdbIsCreated() {
			var assembly = CSharpCompiler.Compile(Source, new CSharpCompilerOptions { PdbKind = PdbKind.Portable });

			Assert.NotNull(assembly.Pdb);
			// Portable PDBs are metadata files
			Assert.Equal(new byte[] { (byte)'B', (byte)'S', (byte)'J', (byte)'B' }, assembly.Pdb.Take(4).ToArray());
		}

		[Fact]
		public void CompileErrorThrows() =>
			Assert.Throws<System.InvalidOperationException>(() => CSharpCompiler.Compile("class {"));
	}
}
