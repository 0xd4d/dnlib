// dnlib: See LICENSE.txt for more info

﻿namespace dnlib.PE {
	/// <summary>
	/// Represents an RVA (relative virtual address)
	/// </summary>
	public enum RVA : uint {
	}

	partial class PEExtensions {
		/// <summary>
		/// Align up
		/// </summary>
		/// <param name="rva">this</param>
		/// <param name="alignment">Alignment</param>
		public static RVA AlignUp(this RVA rva, uint alignment) {
			return (RVA)(((uint)rva + alignment - 1) & ~(alignment - 1));
		}

		/// <summary>
		/// Align up
		/// </summary>
		/// <param name="rva">this</param>
		/// <param name="alignment">Alignment</param>
		public static RVA AlignUp(this RVA rva, int alignment) {
			return (RVA)(((uint)rva + alignment - 1) & ~(alignment - 1));
		}
	}
}
