// dnlib: See LICENSE.txt for more info

using System;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Stores the instruction that jumps to _CorExeMain/_CorDllMain
	/// </summary>
	public sealed class StartupStub : IChunk {
		const StubType stubType = StubType.EntryPoint;
		readonly RelocDirectory relocDirectory;
		readonly Machine machine;
		readonly CpuArch cpuArch;
		readonly Action<string, object[]> logError;
		FileOffset offset;
		RVA rva;

		/// <summary>
		/// Gets/sets the <see cref="ImportDirectory"/>
		/// </summary>
		public ImportDirectory ImportDirectory { get; set; }

		/// <summary>
		/// Gets/sets the <see cref="PEHeaders"/>
		/// </summary>
		public PEHeaders PEHeaders { get; set; }

		/// <inheritdoc/>
		public FileOffset FileOffset => offset;

		/// <inheritdoc/>
		public RVA RVA => rva;

		/// <summary>
		/// Gets the address of the JMP instruction
		/// </summary>
		public RVA EntryPointRVA => rva + (cpuArch is null ? 0 : cpuArch.GetStubCodeOffset(stubType));

		internal bool Enable { get; set; }
		internal uint Alignment => cpuArch is null ? 1 : cpuArch.GetStubAlignment(stubType);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="relocDirectory">Reloc directory</param>
		/// <param name="machine">Machine</param>
		/// <param name="logError">Error logger</param>
		internal StartupStub(RelocDirectory relocDirectory, Machine machine, Action<string, object[]> logError) {
			this.relocDirectory = relocDirectory;
			this.machine = machine;
			this.logError = logError;
			CpuArch.TryGetCpuArch(machine, out cpuArch);
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;

			if (!Enable)
				return;

			if (cpuArch is null) {
				logError("The module needs an unmanaged entry point but the CPU architecture isn't supported: {0} (0x{1:X4})", new object[] { machine, (ushort)machine });
				return;
			}

			cpuArch.WriteStubRelocs(stubType, relocDirectory, this, 0);
		}

		/// <inheritdoc/>
		public uint GetFileLength() {
			if (!Enable)
				return 0;
			if (cpuArch is null)
				return 0;
			return cpuArch.GetStubSize(stubType);
		}

		/// <inheritdoc/>
		public uint GetVirtualSize() => GetFileLength();

		/// <inheritdoc/>
		public void WriteTo(DataWriter writer) {
			if (!Enable)
				return;
			if (cpuArch is null)
				return;
			cpuArch.WriteStub(stubType, writer, PEHeaders.ImageBase, (uint)rva, (uint)ImportDirectory.IatCorXxxMainRVA);
		}
	}
}
