// dnlib: See LICENSE.txt for more info

namespace dnlib.IO {
	/// <summary>
	/// Represents a file offset
	/// </summary>
	public enum FileOffset : uint {
	}

	partial class IOExtensions {
		/// <summary>
		/// Align up
		/// </summary>
		/// <param name="offset">this</param>
		/// <param name="alignment">Alignment</param>
		public static FileOffset AlignUp(this FileOffset offset, uint alignment) => (FileOffset)(((uint)offset + alignment - 1) & ~(alignment - 1));

		/// <summary>
		/// Align up
		/// </summary>
		/// <param name="offset">this</param>
		/// <param name="alignment">Alignment</param>
		public static FileOffset AlignUp(this FileOffset offset, int alignment) => (FileOffset)(((uint)offset + alignment - 1) & ~(alignment - 1));
	}
}
