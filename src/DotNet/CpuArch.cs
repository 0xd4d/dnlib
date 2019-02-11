// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet.Writer;
using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet {
	enum StubType {
		Export,
		EntryPoint,
	}

	abstract class CpuArch {
		// To support a new CPU arch, the easiest way is to check coreclr/src/ilasm/writer.cpp or
		// coreclr/src/dlls/mscorpe/stubs.h, eg. ExportStubAMD64Template, ExportStubX86Template,
		// ExportStubARMTemplate, ExportStubIA64Template, or use ilasm to generate a file with
		// exports and check the stub
		static readonly X86CpuArch x86CpuArch = new X86CpuArch();
		static readonly X64CpuArch x64CpuArch = new X64CpuArch();
		static readonly ItaniumCpuArch itaniumCpuArch = new ItaniumCpuArch();
		static readonly ArmCpuArch armCpuArch = new ArmCpuArch();

		/// <summary>
		/// Gets the required alignment for the stubs, must be a power of 2
		/// </summary>
		/// <param name="stubType">Stub type</param>
		/// <returns></returns>
		public abstract uint GetStubAlignment(StubType stubType);

		/// <summary>
		/// Gets the size of a stub, it doesn't have to be a multiple of <see cref="GetStubAlignment(StubType)"/>
		/// </summary>
		/// <param name="stubType">Stub type</param>
		/// <returns></returns>
		public abstract uint GetStubSize(StubType stubType);

		/// <summary>
		/// Gets the offset of the code (entry point) relative to the start of the stub
		/// </summary>
		/// <param name="stubType">Stub type</param>
		/// <returns></returns>
		public abstract uint GetStubCodeOffset(StubType stubType);

		public static bool TryGetCpuArch(Machine machine, out CpuArch cpuArch) {
			switch (machine) {
			case Machine.I386:
			case Machine.I386_Native_Apple:
			case Machine.I386_Native_FreeBSD:
			case Machine.I386_Native_Linux:
			case Machine.I386_Native_NetBSD:
				cpuArch = x86CpuArch;
				return true;

			case Machine.AMD64:
			case Machine.AMD64_Native_Apple:
			case Machine.AMD64_Native_FreeBSD:
			case Machine.AMD64_Native_Linux:
			case Machine.AMD64_Native_NetBSD:
				cpuArch = x64CpuArch;
				return true;

			case Machine.IA64:
				cpuArch = itaniumCpuArch;
				return true;

			case Machine.ARMNT:
			case Machine.ARMNT_Native_Apple:
			case Machine.ARMNT_Native_FreeBSD:
			case Machine.ARMNT_Native_Linux:
			case Machine.ARMNT_Native_NetBSD:
				cpuArch = armCpuArch;
				return true;

			case Machine.ARM64:
			case Machine.ARM64_Native_Apple:
			case Machine.ARM64_Native_FreeBSD:
			case Machine.ARM64_Native_Linux:
			case Machine.ARM64_Native_NetBSD:
				//TODO: Support ARM64
				goto default;

			default:
				cpuArch = null;
				return false;
			}
		}

		/// <summary>
		/// Gets the RVA of the func field that the stub jumps to
		/// </summary>
		/// <param name="reader">Reader, positioned at the stub func</param>
		/// <param name="peImage">PE image</param>
		/// <param name="funcRva">Updated with RVA of func field</param>
		/// <returns></returns>
		public bool TryGetExportedRvaFromStub(ref DataReader reader, IPEImage peImage, out uint funcRva) =>
			TryGetExportedRvaFromStubCore(ref reader, peImage, out funcRva);

		protected abstract bool TryGetExportedRvaFromStubCore(ref DataReader reader, IPEImage peImage, out uint funcRva);

		/// <summary>
		/// Writes stub relocs, if needed
		/// </summary>
		/// <param name="stubType">Stub type</param>
		/// <param name="relocDirectory">Reloc directory</param>
		/// <param name="chunk">The chunk where this stub will be written to</param>
		/// <param name="stubOffset">Offset of this stub in <paramref name="chunk"/></param>
		public abstract void WriteStubRelocs(StubType stubType, RelocDirectory relocDirectory, IChunk chunk, uint stubOffset);

		/// <summary>
		/// Writes the stub that jumps to the managed function
		/// </summary>
		/// <param name="stubType">Stub type</param>
		/// <param name="writer">Writer</param>
		/// <param name="imageBase">Image base</param>
		/// <param name="stubRva">RVA of this stub</param>
		/// <param name="managedFuncRva">RVA of a pointer-sized field that contains the absolute address of the managed function</param>
		public abstract void WriteStub(StubType stubType, DataWriter writer, ulong imageBase, uint stubRva, uint managedFuncRva);
	}

	sealed class X86CpuArch : CpuArch {
		public override uint GetStubAlignment(StubType stubType) {
			switch (stubType) {
			case StubType.Export:
			case StubType.EntryPoint:
				return 4;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}

		public override uint GetStubSize(StubType stubType) {
			switch (stubType) {
			case StubType.Export:
			case StubType.EntryPoint:
				return 2/*padding*/ + 6;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}

		public override uint GetStubCodeOffset(StubType stubType) {
			switch (stubType) {
			case StubType.Export:
			case StubType.EntryPoint:
				return 2/*padding*/;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}

		protected override bool TryGetExportedRvaFromStubCore(ref DataReader reader, IPEImage peImage, out uint funcRva) {
			funcRva = 0;

			// FF25xxxxxxxx		jmp DWORD PTR [xxxxxxxx]
			if (reader.ReadUInt16() != 0x25FF)
				return false;
			funcRva = reader.ReadUInt32() - (uint)peImage.ImageNTHeaders.OptionalHeader.ImageBase;
			return true;
		}

		public override void WriteStubRelocs(StubType stubType, RelocDirectory relocDirectory, IChunk chunk, uint stubOffset) {
			switch (stubType) {
			case StubType.Export:
			case StubType.EntryPoint:
				relocDirectory.Add(chunk, stubOffset + 4);
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}

		public override void WriteStub(StubType stubType, DataWriter writer, ulong imageBase, uint stubRva, uint managedFuncRva) {
			switch (stubType) {
			case StubType.Export:
			case StubType.EntryPoint:
				writer.WriteUInt16(0);// padding
				writer.WriteUInt16(0x25FF);
				writer.WriteUInt32((uint)imageBase + managedFuncRva);
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}
	}

	sealed class X64CpuArch : CpuArch {
		public override uint GetStubAlignment(StubType stubType) {
			switch (stubType) {
			case StubType.Export:
			case StubType.EntryPoint:
				return 4;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}

		public override uint GetStubSize(StubType stubType) {
			switch (stubType) {
			case StubType.Export:
			case StubType.EntryPoint:
				return 2/*padding*/ + 12;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}

		public override uint GetStubCodeOffset(StubType stubType) {
			switch (stubType) {
			case StubType.Export:
			case StubType.EntryPoint:
				return 2/*padding*/;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}

		protected override bool TryGetExportedRvaFromStubCore(ref DataReader reader, IPEImage peImage, out uint funcRva) {
			funcRva = 0;

			// 48A1xxxxxxxxxxxxxxxx		movabs	rax,[xxxxxxxxxxxxxxxx]
			// FFE0						jmp		rax
			if (reader.ReadUInt16() != 0xA148)
				return false;
			ulong absAddr = reader.ReadUInt64();
			if (reader.ReadUInt16() != 0xE0FF)
				return false;
			ulong rva = absAddr - peImage.ImageNTHeaders.OptionalHeader.ImageBase;
			if (rva > uint.MaxValue)
				return false;
			funcRva = (uint)rva;
			return true;
		}

		public override void WriteStubRelocs(StubType stubType, RelocDirectory relocDirectory, IChunk chunk, uint stubOffset) {
			switch (stubType) {
			case StubType.Export:
			case StubType.EntryPoint:
				relocDirectory.Add(chunk, stubOffset + 4);
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}

		public override void WriteStub(StubType stubType, DataWriter writer, ulong imageBase, uint stubRva, uint managedFuncRva) {
			switch (stubType) {
			case StubType.Export:
			case StubType.EntryPoint:
				writer.WriteUInt16(0);// padding
				writer.WriteUInt16(0xA148);
				writer.WriteUInt64(imageBase + managedFuncRva);
				writer.WriteUInt16(0xE0FF);
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}
	}

	sealed class ItaniumCpuArch : CpuArch {
		public override uint GetStubAlignment(StubType stubType) {
			switch (stubType) {
			case StubType.Export:
			case StubType.EntryPoint:
				return 16;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}

		public override uint GetStubSize(StubType stubType) {
			switch (stubType) {
			case StubType.Export:
			case StubType.EntryPoint:
				return 0x30;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}

		public override uint GetStubCodeOffset(StubType stubType) {
			switch (stubType) {
			case StubType.Export:
			case StubType.EntryPoint:
				return 0x20;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}

		protected override bool TryGetExportedRvaFromStubCore(ref DataReader reader, IPEImage peImage, out uint funcRva) {
			funcRva = 0;

			// From ExportStubIA64Template in coreclr/src/ilasm/writer.cpp
			//
			// ld8    r9  = [gp]    ;;
			// ld8    r10 = [r9],8
			// nop.i                ;;
			// ld8    gp  = [r9]
			// mov    b6  = r10
			// br.cond.sptk.few  b6
			//
			// 0x0B, 0x48, 0x00, 0x02, 0x18, 0x10, 0xA0, 0x40, 
			// 0x24, 0x30, 0x28, 0x00, 0x00, 0x00, 0x04, 0x00, 
			// 0x10, 0x08, 0x00, 0x12, 0x18, 0x10, 0x60, 0x50, 
			// 0x04, 0x80, 0x03, 0x00, 0x60, 0x00, 0x80, 0x00,
			// 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,//address of the template
			// 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 //address of VTFixup slot
			ulong addrTemplate = reader.ReadUInt64();
			ulong absAddr = reader.ReadUInt64();
			reader.Position = (uint)peImage.ToFileOffset((RVA)(addrTemplate - peImage.ImageNTHeaders.OptionalHeader.ImageBase));
			if (reader.ReadUInt64() != 0x40A010180200480BUL)
				return false;
			if (reader.ReadUInt64() != 0x0004000000283024UL)
				return false;
			if (reader.ReadUInt64() != 0x5060101812000810UL)
				return false;
			if (reader.ReadUInt64() != 0x0080006000038004UL)
				return false;

			ulong rva = absAddr - peImage.ImageNTHeaders.OptionalHeader.ImageBase;
			if (rva > uint.MaxValue)
				return false;
			funcRva = (uint)rva;
			return true;
		}

		public override void WriteStubRelocs(StubType stubType, RelocDirectory relocDirectory, IChunk chunk, uint stubOffset) {
			switch (stubType) {
			case StubType.Export:
			case StubType.EntryPoint:
				relocDirectory.Add(chunk, stubOffset + 0x20);
				relocDirectory.Add(chunk, stubOffset + 0x28);
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}

		public override void WriteStub(StubType stubType, DataWriter writer, ulong imageBase, uint stubRva, uint managedFuncRva) {
			switch (stubType) {
			case StubType.Export:
			case StubType.EntryPoint:
				writer.WriteUInt64(0x40A010180200480BUL);
				writer.WriteUInt64(0x0004000000283024UL);
				writer.WriteUInt64(0x5060101812000810UL);
				writer.WriteUInt64(0x0080006000038004UL);
				writer.WriteUInt64(imageBase + stubRva);
				writer.WriteUInt64(imageBase + managedFuncRva);
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}
	}

	sealed class ArmCpuArch : CpuArch {
		public override uint GetStubAlignment(StubType stubType) {
			switch (stubType) {
			case StubType.Export:
			case StubType.EntryPoint:
				return 4;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}

		public override uint GetStubSize(StubType stubType) {
			switch (stubType) {
			case StubType.Export:
			case StubType.EntryPoint:
				return 8;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}

		public override uint GetStubCodeOffset(StubType stubType) {
			switch (stubType) {
			case StubType.Export:
			case StubType.EntryPoint:
				return 0;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}

		protected override bool TryGetExportedRvaFromStubCore(ref DataReader reader, IPEImage peImage, out uint funcRva) {
			funcRva = 0;

			// DFF800F0		ldr.w	pc,[pc]
			// xxxxxxxx
			if (reader.ReadUInt32() != 0xF000F8DF)
				return false;
			funcRva = reader.ReadUInt32() - (uint)peImage.ImageNTHeaders.OptionalHeader.ImageBase;
			return true;
		}

		public override void WriteStubRelocs(StubType stubType, RelocDirectory relocDirectory, IChunk chunk, uint stubOffset) {
			switch (stubType) {
			case StubType.Export:
			case StubType.EntryPoint:
				relocDirectory.Add(chunk, stubOffset + 4);
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}

		public override void WriteStub(StubType stubType, DataWriter writer, ulong imageBase, uint stubRva, uint managedFuncRva) {
			switch (stubType) {
			case StubType.Export:
			case StubType.EntryPoint:
				writer.WriteUInt32(0xF000F8DF);
				writer.WriteUInt32((uint)imageBase + managedFuncRva);
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}
	}
}
