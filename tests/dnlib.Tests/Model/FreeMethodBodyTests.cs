// dnlib: See LICENSE.txt for more info

using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Xunit;

namespace dnlib.Tests.Model {
	/// <summary>
	/// The body of a method is read from the file when <see cref="MethodDef.Body"/> is used the first time, and
	/// the method then keeps it. <see cref="MethodDef.FreeMethodBody"/> makes the method forget it (to save
	/// memory): the next use reads it from the file again.
	/// </summary>
	public class FreeMethodBodyTests {
		static MethodDef GetMain(ModuleDefMD module) => module.Find("Features.Program", isReflectionName: true)!.FindMethod("Main")!;

		[Fact]
		public void FreedBodyIsReadAgainFromTheFile() {
			using (var module = ModuleDefMD.Load(Fixtures.Fixtures.Features.Image)) {
				var main = GetMain(module);
				var body = main.Body!;
				var codes = body.Instructions.Select(a => a.OpCode.Code).ToList();
				Assert.Same(body, main.Body);

				main.FreeMethodBody();

				Assert.NotSame(body, main.Body);
				Assert.Equal(codes.ToArray(), main.Body!.Instructions.Select(a => a.OpCode.Code).ToArray());
				Assert.Same(main.Body, main.Body);
			}
		}

		// The changes of a body that's freed are lost: it's the caller's job not to free such a body
		[Fact]
		public void ChangedBodyThatIsFreedIsTheOriginalBodyAgain() {
			using (var module = ModuleDefMD.Load(Fixtures.Fixtures.Features.Image)) {
				var main = GetMain(module);
				int originalCount = main.Body!.Instructions.Count;
				main.Body.Instructions.Insert(0, OpCodes.Nop.ToInstruction());

				main.FreeMethodBody();

				Assert.Equal(originalCount, main.Body.Instructions.Count);
			}
		}

		[Fact]
		public void FreeingABodyThatWasNotReadDoesNothing() {
			using (var module = ModuleDefMD.Load(Fixtures.Fixtures.Features.Image)) {
				var main = GetMain(module);

				main.FreeMethodBody();
				main.FreeMethodBody();

				Assert.NotEmpty(main.Body!.Instructions);
			}
		}
	}
}
