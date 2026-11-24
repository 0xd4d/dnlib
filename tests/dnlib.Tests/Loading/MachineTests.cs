// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.PE;
using Xunit;

namespace dnlib.Tests.Loading {
	/// <summary>
	/// The machine of a PE file is its processor. A file with code that the .NET runtime compiled ahead of time
	/// (ReadyToRun) for another operating system than Windows has the processor's number XORed with a number
	/// of the operating system (<c>IMAGE_FILE_MACHINE_NATIVE_OS_OVERRIDE</c> in the runtime's pedecoder.h):
	/// <see cref="Machine"/> has a value for each of them, and the extension methods say which processor a
	/// value is.
	/// </summary>
	public class MachineTests {
		// The numbers of pedecoder.h
		static readonly Dictionary<string, int> operatingSystems = new Dictionary<string, int> {
			{ "Apple", 0x4644 }, { "FreeBSD", 0xADC4 }, { "OpenBSD", 0xADC5 }, { "Linux", 0x7B79 }, { "NetBSD", 0x1993 }, { "Sun", 0x1992 },
		};

		// The processors that have values for other operating systems, and the processor number they use
		static readonly Dictionary<string, Machine> processors = new Dictionary<string, Machine> {
			{ "I386", Machine.I386 }, { "AMD64", Machine.AMD64 }, { "ARMNT", Machine.ARMNT }, { "ARM64", Machine.ARM64 },
			{ "LOONGARCH64", Machine.LOONGARCH64 }, { "RISCV64", Machine.RISCV64 },
			// These two have no number of their own on Windows
			{ "S390X", Machine.Unknown }, { "POWERPC64", Machine.POWERPC },
		};

		public static TheoryData<Machine> AllMachines {
			get {
				var data = new TheoryData<Machine>();
				foreach (Machine machine in Enum.GetValues(typeof(Machine)).Cast<Machine>().Distinct())
					data.Add(machine);
				return data;
			}
		}

		[Fact]
		public void EveryProcessorHasAValueForEveryOperatingSystem() {
			foreach (var processor in processors) {
				foreach (var operatingSystem in operatingSystems) {
					string name = $"{processor.Key}_Native_{operatingSystem.Key}";
					Assert.True(Enum.IsDefined(typeof(Machine), name), name);
					var machine = (Machine)Enum.Parse(typeof(Machine), name);
					Assert.Equal((int)processor.Value ^ operatingSystem.Value, (int)machine);
				}
			}
			int nativeValues = Enum.GetNames(typeof(Machine)).Count(a => a.Contains("_Native_"));
			Assert.Equal(processors.Count * operatingSystems.Count, nativeValues);
		}

		// The processor of a value: the part of its name before "_Native_"
		static string GetProcessor(Machine machine) {
			string name = Enum.GetNames(typeof(Machine)).First(a => (Machine)Enum.Parse(typeof(Machine), a) == machine);
			int index = name.IndexOf("_Native_", StringComparison.Ordinal);
			return index < 0 ? name : name.Substring(0, index);
		}

		[Theory, MemberData(nameof(AllMachines))]
		public void ExtensionMethodsSayWhichProcessorItIs(Machine machine) {
			string processor = GetProcessor(machine);
			bool isNative = Enum.GetName(typeof(Machine), machine)!.Contains("_Native_");

			Assert.Equal(processor == "I386", machine.IsI386());
			Assert.Equal(processor == "AMD64", machine.IsAMD64());
			Assert.Equal(processor == "ARMNT", machine.IsARMNT());
			Assert.Equal(processor == "ARM64", machine.IsARM64());
			Assert.Equal(processor == "LOONGARCH64", machine.IsLoongArch64());
			Assert.Equal(processor == "RISCV64", machine.IsRISCV64());
			// Their numbers without an operating system are Unknown (0) and the 32-bit PowerPC
			Assert.Equal(processor == "S390X" && isNative, machine.IsS390x());
			Assert.Equal(processor == "POWERPC64" && isNative, machine.IsPowerPC64());
		}

		[Theory, MemberData(nameof(AllMachines))]
		public void ProcessorsWith64BitAddresses(Machine machine) {
			var processorsWith64Bits = new[] { "IA64", "AMD64", "ARM64", "LOONGARCH64", "RISCV64", "S390X", "POWERPC64" };

			Assert.Equal(processorsWith64Bits.Contains(GetProcessor(machine)), machine.Is64Bit());
		}

		[Fact]
		public void ValuesThatAreNotAProcessorOfTheEnum() {
			foreach (var machine in new[] { Machine.Unknown, Machine.POWERPC, (Machine)0x1234, (Machine)0xFFFF }) {
				Assert.False(machine.Is64Bit());
				Assert.False(machine.IsI386());
				Assert.False(machine.IsAMD64());
				Assert.False(machine.IsS390x());
				Assert.False(machine.IsPowerPC64());
			}
		}
	}
}
