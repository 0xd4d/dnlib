using System.IO;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// Extension methods
	/// </summary>
	public static partial class Extensions {
		public static void WriteZeros(this BinaryWriter writer, int count) {
			if (count <= 0x20) {
				for (int i = 0; i < count; i++)
					writer.Write((byte)0);
			}
			else
				writer.Write(new byte[count]);
		}
	}
}
