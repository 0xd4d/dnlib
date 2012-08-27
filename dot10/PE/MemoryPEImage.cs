using System;
using System.Collections.Generic;
using System.IO;

namespace dot10.PE {
	/// <summary>
	/// Accesses a PE file that has the same structure as a PE file loaded
	/// into memory by the OS PE file loader.
	/// </summary>
	/// <seealso cref="FilePEImage"/>
	public class MemoryPEImage : PEImageBase {
		/// <summary>
		/// Constructor for a PE image in a Stream
		/// </summary>
		/// <param name="data">The PE file data</param>
		/// <param name="verify">Verify PE file data</param>
		public MemoryPEImage(Stream data, bool verify)
			: base(data, verify) {
		}

		/// <summary>
		/// Constructor for a PE image in memory
		/// </summary>
		/// <param name="baseAddr">Address of PE image</param>
		/// <param name="verify">Verify PE file data</param>
		public unsafe MemoryPEImage(IntPtr baseAddr, bool verify)
			: this(new UnmanagedMemoryStream((byte*)baseAddr.ToPointer(), 0x10000), verify) {
			resetStream(new UnmanagedMemoryStream((byte*)baseAddr.ToPointer(), getTotalMemorySize()));
		}

		static ulong alignUp(ulong val, uint alignment) {
			return (val + alignment - 1) & ~(ulong)(alignment - 1);
		}

		long getTotalMemorySize() {
			var optHdr = ImageNTHeaders.OptionalHeader;
			uint alignment = optHdr.SectionAlignment;
			ulong len = alignUp(optHdr.SizeOfHeaders, alignment);
			foreach (var section in ImageSectionHeaders) {
				ulong len2 = alignUp((ulong)section.VirtualAddress.Value + Math.Max(section.VirtualSize, section.SizeOfRawData), alignment);
				if (len2 > len)
					len = len2;
			}
			return (long)len;
		}

		/// <inheritdoc/>
		public override RVA ToRVA(FileOffset offset) {
			return new RVA((uint)offset.Value);
		}

		/// <inheritdoc/>
		public override FileOffset ToFileOffset(RVA rva) {
			return new FileOffset(rva.Value);
		}
	}
}
