// dnlib: See LICENSE.txt for more info

using System;
using System.Runtime.InteropServices;
using dnlib.PE;
using Xunit;

namespace dnlib.Tests.IO {
	/// <summary>
	/// dnlib must know the processor of the process it runs in: it reads memory in another way if the processor
	/// can't read unaligned values, and it loads the native PDB library for that processor. The value is found in
	/// different ways on .NET, .NET Framework and Mono, so the tests run on all of them.
	/// </summary>
	public class ProcessArchitectureTests {
		[Fact]
		public void ArchitectureIsTheOneOfThisProcess() {
			var machine = ProcessorArchUtils.GetProcessCpuArchitecture();

			var expected = RuntimeInformation.ProcessArchitecture switch {
				Architecture.X86 => Machine.I386,
				Architecture.X64 => Machine.AMD64,
				Architecture.Arm => Machine.ARMNT,
				Architecture.Arm64 => Machine.ARM64,
				_ => machine,
			};
			Assert.Equal(expected, machine);
			// The same value every time
			Assert.Equal(machine, ProcessorArchUtils.GetProcessCpuArchitecture());
		}

		[Fact]
		public void PointerSizeOfTheArchitectureIsThePointerSizeOfThisProcess() {
			var machine = ProcessorArchUtils.GetProcessCpuArchitecture();

			Assert.Equal(IntPtr.Size == 8, machine.Is64Bit());
		}
	}
}
