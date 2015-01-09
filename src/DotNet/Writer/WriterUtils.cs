// dnlib: See LICENSE.txt for more info

using System.IO;

namespace dnlib.DotNet.Writer {
	static class WriterUtils {
		public static uint WriteCompressedUInt32(this BinaryWriter writer, IWriterError helper, uint value) {
			if (value > 0x1FFFFFFF) {
				helper.Error("UInt32 value is too big and can't be compressed");
				value = 0x1FFFFFFF;
			}
			writer.WriteCompressedUInt32(value);
			return value;
		}

		public static int WriteCompressedInt32(this BinaryWriter writer, IWriterError helper, int value) {
			if (value < -0x10000000) {
				helper.Error("Int32 value is too small and can't be compressed.");
				value = -0x10000000;
			}
			else if (value > 0x0FFFFFFF) {
				helper.Error("Int32 value is too big and can't be compressed.");
				value = 0x0FFFFFFF;
			}
			writer.WriteCompressedInt32(value);
			return value;
		}

		public static void Write(this BinaryWriter writer, IWriterError helper, UTF8String s) {
			if (UTF8String.IsNull(s)) {
				helper.Error("UTF8String is null");
				s = UTF8String.Empty;
			}

			writer.WriteCompressedUInt32(helper, (uint)s.DataLength);
			writer.Write(s.Data);
		}
	}
}
