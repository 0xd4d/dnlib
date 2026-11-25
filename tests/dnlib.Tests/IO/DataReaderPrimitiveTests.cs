// dnlib: See LICENSE.txt for more info

using System;
using dnlib.IO;
using Xunit;

namespace dnlib.Tests.IO {
	/// <summary>
	/// The methods of <see cref="DataReader"/> that read one fixed-size value
	/// </summary>
	public class DataReaderPrimitiveTests {
		public static TheoryData<DataStreamKind> Kinds => TestDataReader.AllKinds;

		delegate T ReadValue<T>(ref DataReader reader);

		// Reads a value and checks it. Then checks that reading fails, and doesn't move the reader, when the
		// last byte of the value is missing.
		static void CheckRead<T>(DataStreamKind kind, byte[] bytes, T expected, ReadValue<T> read) {
			using (var source = TestDataReader.Create(kind, bytes)) {
				var reader = source.Reader;
				Assert.Equal(expected, read(ref reader));
				Assert.Equal((uint)bytes.Length, reader.Position);
				Assert.Equal(0U, reader.BytesLeft);

				var truncated = source.Reader.Slice(0, bytes.Length - 1);
				Assert.Throws<DataReaderException>(() => read(ref truncated));
				Assert.Equal(0U, truncated.Position);
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void ReadBoolean(DataStreamKind kind) {
			CheckRead(kind, new byte[] { 0 }, false, (ref DataReader reader) => reader.ReadBoolean());
			CheckRead(kind, new byte[] { 1 }, true, (ref DataReader reader) => reader.ReadBoolean());
			// Every value that isn't 0 is true
			CheckRead(kind, new byte[] { 0x80 }, true, (ref DataReader reader) => reader.ReadBoolean());
		}

		[Theory, MemberData(nameof(Kinds))]
		public void ReadByte(DataStreamKind kind) =>
			CheckRead(kind, new byte[] { 0xFE }, (byte)0xFE, (ref DataReader reader) => reader.ReadByte());

		[Theory, MemberData(nameof(Kinds))]
		public void ReadSByte(DataStreamKind kind) =>
			CheckRead(kind, new byte[] { 0xFE }, (sbyte)-2, (ref DataReader reader) => reader.ReadSByte());

		[Theory, MemberData(nameof(Kinds))]
		public void ReadChar(DataStreamKind kind) =>
			CheckRead(kind, new byte[] { 0xAC, 0x20 }, '€', (ref DataReader reader) => reader.ReadChar());

		[Theory, MemberData(nameof(Kinds))]
		public void ReadInt16(DataStreamKind kind) =>
			CheckRead(kind, new byte[] { 0x34, 0x92 }, unchecked((short)0x9234), (ref DataReader reader) => reader.ReadInt16());

		[Theory, MemberData(nameof(Kinds))]
		public void ReadUInt16(DataStreamKind kind) =>
			CheckRead(kind, new byte[] { 0x34, 0x92 }, (ushort)0x9234, (ref DataReader reader) => reader.ReadUInt16());

		[Theory, MemberData(nameof(Kinds))]
		public void ReadInt32(DataStreamKind kind) =>
			CheckRead(kind, new byte[] { 0x78, 0x56, 0x34, 0x92 }, unchecked((int)0x92345678), (ref DataReader reader) => reader.ReadInt32());

		[Theory, MemberData(nameof(Kinds))]
		public void ReadUInt32(DataStreamKind kind) =>
			CheckRead(kind, new byte[] { 0x78, 0x56, 0x34, 0x92 }, 0x92345678U, (ref DataReader reader) => reader.ReadUInt32());

		[Theory, MemberData(nameof(Kinds))]
		public void ReadInt64(DataStreamKind kind) =>
			CheckRead(kind, new byte[] { 0xF0, 0xDE, 0xBC, 0x9A, 0x78, 0x56, 0x34, 0x92 }, unchecked((long)0x923456789ABCDEF0), (ref DataReader reader) => reader.ReadInt64());

		[Theory, MemberData(nameof(Kinds))]
		public void ReadUInt64(DataStreamKind kind) =>
			CheckRead(kind, new byte[] { 0xF0, 0xDE, 0xBC, 0x9A, 0x78, 0x56, 0x34, 0x92 }, 0x923456789ABCDEF0UL, (ref DataReader reader) => reader.ReadUInt64());

		[Theory, MemberData(nameof(Kinds))]
		public void ReadSingle(DataStreamKind kind) =>
			// 1.5 is 0x3FC00000
			CheckRead(kind, new byte[] { 0x00, 0x00, 0xC0, 0x3F }, 1.5f, (ref DataReader reader) => reader.ReadSingle());

		[Theory, MemberData(nameof(Kinds))]
		public void ReadDouble(DataStreamKind kind) =>
			// 1.5 is 0x3FF8000000000000
			CheckRead(kind, new byte[] { 0, 0, 0, 0, 0, 0, 0xF8, 0x3F }, 1.5, (ref DataReader reader) => reader.ReadDouble());

		[Theory, MemberData(nameof(Kinds))]
		public void ReadGuid(DataStreamKind kind) {
			var bytes = new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };
			CheckRead(kind, bytes, new Guid("33221100-5544-7766-8899-AABBCCDDEEFF"), (ref DataReader reader) => reader.ReadGuid());
		}

		[Theory, MemberData(nameof(Kinds))]
		public void ReadDecimal(DataStreamKind kind) {
			// lo, mid, hi, flags. Flags: the scale is in bits 16-23, bit 31 is the sign
			var bytes = new byte[] {
				0x39, 0x30, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x02, 0x80,
			};
			CheckRead(kind, bytes, -123.45m, (ref DataReader reader) => reader.ReadDecimal());
		}

		[Theory, MemberData(nameof(Kinds))]
		public void ReadDecimalWithInvalidScaleThrows(DataStreamKind kind) {
			// The biggest valid scale is 28
			var bytes = new byte[16];
			bytes[14] = 29;
			using (var source = TestDataReader.Create(kind, bytes)) {
				var reader = source.Reader;
				Assert.Throws<DataReaderException>(() => reader.ReadDecimal());
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void ValuesAreReadOneAfterTheOther(DataStreamKind kind) {
			using (var source = TestDataReader.Create(kind, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07)) {
				var reader = source.Reader;
				Assert.Equal(0x01, reader.ReadByte());
				Assert.Equal(0x0302, reader.ReadUInt16());
				Assert.Equal(0x07060504U, reader.ReadUInt32());
				Assert.Throws<DataReaderException>(() => reader.ReadByte());
			}
		}
	}
}
