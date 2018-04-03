// dnlib: See LICENSE.txt for more info

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Extension methods
	/// </summary>
	static partial class Extensions {
		/// <summary>
		/// Write zeros
		/// </summary>
		/// <param name="writer">this</param>
		/// <param name="count">Number of zeros</param>
		public static void WriteZeroes(this DataWriter writer, int count) {
			while (count >= 8) {
				writer.WriteUInt64(0);
				count -= 8;
			}
			for (int i = 0; i < count; i++)
				writer.WriteByte(0);
		}
	}
}
