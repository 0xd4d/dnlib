// dnlib: See LICENSE.txt for more info

#nullable enable

using dnlib.IO;
using dnlib.PE;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Overflow checked file offset, RVA and length arithmetic. All file offsets, RVAs and
	/// lengths are 32-bit values so the image can't be 4GB or bigger.
	/// </summary>
	static class OffsetUtils {
		static ModuleWriterException CreateTooBigException() =>
			new ModuleWriterException("The image is too big: all file offsets, RVAs and lengths must be less than 4GB");

		static uint CheckedResult(ulong value) {
			if (value > uint.MaxValue)
				throw CreateTooBigException();
			return (uint)value;
		}

		public static uint Add(uint a, uint b) => CheckedResult((ulong)a + b);
		public static uint Add(uint a, uint b, uint c) => CheckedResult((ulong)a + b + c);
		public static FileOffset Add(FileOffset offset, uint length) => (FileOffset)Add((uint)offset, length);
		public static RVA Add(RVA rva, uint length) => (RVA)Add((uint)rva, length);

		public static uint Multiply(uint a, uint b) => CheckedResult((ulong)a * b);

		/// <summary>
		/// Aligns up. <paramref name="alignment"/> must be 0 or a power of 2.
		/// </summary>
		public static uint AlignUp(uint value, uint alignment) {
			if (alignment <= 1)
				return value;
			return CheckedResult(((ulong)value + alignment - 1) & ~((ulong)alignment - 1));
		}
		public static FileOffset AlignUp(FileOffset offset, uint alignment) => (FileOffset)AlignUp((uint)offset, alignment);
		public static RVA AlignUp(RVA rva, uint alignment) => (RVA)AlignUp((uint)rva, alignment);
	}
}
