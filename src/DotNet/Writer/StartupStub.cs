// dnlib: See LICENSE.txt for more info

using System.IO;
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
		readonly LogError logError;
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
		public FileOffset FileOffset {
			get { return offset; }
		}

		/// <inheritdoc/>
		public RVA RVA {
			get { return rva; }
		}

		/// <summary>
		/// Gets the address of the JMP instruction
		/// </summary>
		public RVA EntryPointRVA {
			get { return rva + (cpuArch == null ? 0 : cpuArch.GetStubCodeOffset(stubType)); }
		}

		internal bool Enable { get; set; }

		internal uint Alignment {
			get { return cpuArch == null ? 1 : cpuArch.GetStubAlignment(stubType); }
		}

		internal delegate void LogError(string format, params object[] args);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="relocDirectory">Reloc directory</param>
		/// <param name="machine">Machine</param>
		/// <param name="logError">Error logger</param>
		internal StartupStub(RelocDirectory relocDirectory, Machine machine, LogError logError) {
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

			if (cpuArch == null) {
				logError("The module needs an unmanaged entry point but the CPU architecture isn't supported: {0} (0x{1:X4})", machine, (ushort)machine);
				return;
			}

			cpuArch.WriteStubRelocs(stubType, relocDirectory, this, 0);
		}

		/// <inheritdoc/>
		public uint GetFileLength() {
			if (!Enable)
				return 0;
			if (cpuArch == null)
				return 0;
			return cpuArch.GetStubSize(stubType);
		}

		/// <inheritdoc/>
		public uint GetVirtualSize() {
			return GetFileLength();
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			if (!Enable)
				return;
			if (cpuArch == null)
				return;
			cpuArch.WriteStub(stubType, writer, PEHeaders.ImageBase, (uint)rva, (uint)ImportDirectory.IatCorXxxMainRVA);
		}
	}
}
