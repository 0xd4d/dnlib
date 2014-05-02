/*
    Copyright (C) 2012-2014 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

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
