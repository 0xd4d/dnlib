// dnlib: See LICENSE.txt for more info

using dnlib.IO;
using Xunit;

namespace dnlib.Tests.IO {
	/// <summary>
	/// Compressed integers (ECMA-335 II.23.2) and 7-bit encoded integers (<see cref="System.IO.BinaryReader"/>)
	/// </summary>
	public class DataReaderCompressedIntegerTests {
		public static TheoryData<DataStreamKind> Kinds => TestDataReader.AllKinds;

		// The examples in ECMA-335 II.23.2 and the limits of each size
		public static TheoryData<byte[], uint> UnsignedValues => new TheoryData<byte[], uint> {
			{ new byte[] { 0x00 }, 0x00 },
			{ new byte[] { 0x03 }, 0x03 },
			{ new byte[] { 0x7F }, 0x7F },
			{ new byte[] { 0x80, 0x80 }, 0x80 },
			{ new byte[] { 0xAE, 0x57 }, 0x2E57 },
			{ new byte[] { 0xBF, 0xFF }, 0x3FFF },
			{ new byte[] { 0xC0, 0x00, 0x40, 0x00 }, 0x4000 },
			{ new byte[] { 0xDF, 0xFF, 0xFF, 0xFF }, 0x1FFFFFFF },
			// Not the shortest encoding. It's not valid, but it's read
			{ new byte[] { 0x80, 0x03 }, 0x03 },
			{ new byte[] { 0xC0, 0x00, 0x00, 0x03 }, 0x03 },
		};

		[Theory, MemberData(nameof(UnsignedValues))]
		public void ReadCompressedUInt32(byte[] bytes, uint expected) {
			foreach (var kind in TestDataReader.KindValues) {
				using (var source = TestDataReader.Create(kind, bytes)) {
					var reader = source.Reader;
					Assert.Equal(expected, reader.ReadCompressedUInt32());
					Assert.Equal((uint)bytes.Length, reader.Position);

					reader = source.Reader;
					Assert.True(reader.TryReadCompressedUInt32(out uint value));
					Assert.Equal(expected, value);
					Assert.Equal((uint)bytes.Length, reader.Position);
				}
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void CompressedUInt32WithInvalid111PrefixIsReadAsFourBytes(DataStreamKind kind) {
			// The first byte of a 4-byte value is 110xxxxx. 111xxxxx isn't valid, but the CLR doesn't always
			// check it, so dnlib ignores the extra bit.
			using (var source = TestDataReader.Create(kind, 0xE0, 0x00, 0x40, 0x00)) {
				var reader = source.Reader;
				Assert.Equal(0x4000U, reader.ReadCompressedUInt32());
				Assert.Equal(4U, reader.Position);
			}
		}

		public static TheoryData<byte[]> TruncatedValues => new TheoryData<byte[]> {
			new byte[] { },
			new byte[] { 0x80 },
			new byte[] { 0xC0 },
			new byte[] { 0xC0, 0x00 },
			new byte[] { 0xC0, 0x00, 0x40 },
		};

		[Theory, MemberData(nameof(TruncatedValues))]
		public void TruncatedCompressedUInt32IsNotRead(byte[] bytes) {
			foreach (var kind in TestDataReader.KindValues) {
				using (var source = TestDataReader.Create(kind, bytes)) {
					var reader = source.Reader;
					Assert.False(reader.TryReadCompressedUInt32(out uint value));
					Assert.Equal(0U, value);
					Assert.Equal(0U, reader.Position);
					Assert.Throws<DataReaderException>(() => reader.ReadCompressedUInt32());
					Assert.Equal(0U, reader.Position);
				}
			}
		}

		// The examples in ECMA-335 II.23.2 and the limits of each size
		public static TheoryData<byte[], int> SignedValues => new TheoryData<byte[], int> {
			{ new byte[] { 0x00 }, 0 },
			{ new byte[] { 0x06 }, 3 },
			{ new byte[] { 0x7B }, -3 },
			{ new byte[] { 0x7E }, 63 },
			{ new byte[] { 0x01 }, -64 },
			{ new byte[] { 0x80, 0x80 }, 64 },
			{ new byte[] { 0x80, 0x01 }, -8192 },
			{ new byte[] { 0xBF, 0xFE }, 8191 },
			{ new byte[] { 0xC0, 0x00, 0x40, 0x00 }, 8192 },
			{ new byte[] { 0xDF, 0xFF, 0xFF, 0xFE }, 268435455 },
			{ new byte[] { 0xC0, 0x00, 0x00, 0x01 }, -268435456 },
		};

		[Theory, MemberData(nameof(SignedValues))]
		public void ReadCompressedInt32(byte[] bytes, int expected) {
			foreach (var kind in TestDataReader.KindValues) {
				using (var source = TestDataReader.Create(kind, bytes)) {
					var reader = source.Reader;
					Assert.Equal(expected, reader.ReadCompressedInt32());
					Assert.Equal((uint)bytes.Length, reader.Position);

					reader = source.Reader;
					Assert.True(reader.TryReadCompressedInt32(out int value));
					Assert.Equal(expected, value);
					Assert.Equal((uint)bytes.Length, reader.Position);
				}
			}
		}

		[Theory, MemberData(nameof(TruncatedValues))]
		public void TruncatedCompressedInt32IsNotRead(byte[] bytes) {
			foreach (var kind in TestDataReader.KindValues) {
				using (var source = TestDataReader.Create(kind, bytes)) {
					var reader = source.Reader;
					Assert.False(reader.TryReadCompressedInt32(out int value));
					Assert.Equal(0, value);
					Assert.Equal(0U, reader.Position);
					Assert.Throws<DataReaderException>(() => reader.ReadCompressedInt32());
					Assert.Equal(0U, reader.Position);
				}
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void CompressedInt32WithInvalid111PrefixIsNotRead(DataStreamKind kind) {
			// Unlike the unsigned reader, the signed reader rejects 111xxxxx
			using (var source = TestDataReader.Create(kind, 0xE0, 0x00, 0x40, 0x00)) {
				var reader = source.Reader;
				Assert.False(reader.TryReadCompressedInt32(out _));
				Assert.Equal(0U, reader.Position);
			}
		}

		public static TheoryData<byte[], uint> SevenBitValues => new TheoryData<byte[], uint> {
			{ new byte[] { 0x00 }, 0 },
			{ new byte[] { 0x7F }, 0x7F },
			{ new byte[] { 0x80, 0x01 }, 0x80 },
			{ new byte[] { 0xFF, 0x7F }, 0x3FFF },
			{ new byte[] { 0x80, 0x80, 0x01 }, 0x4000 },
			{ new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x07 }, 0x7FFFFFFF },
			{ new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x0F }, 0xFFFFFFFF },
		};

		[Theory, MemberData(nameof(SevenBitValues))]
		public void Read7BitEncodedUInt32(byte[] bytes, uint expected) {
			foreach (var kind in TestDataReader.KindValues) {
				using (var source = TestDataReader.Create(kind, bytes)) {
					var reader = source.Reader;
					Assert.Equal(expected, reader.Read7BitEncodedUInt32());
					Assert.Equal((uint)bytes.Length, reader.Position);

					reader = source.Reader;
					Assert.Equal((int)expected, reader.Read7BitEncodedInt32());
				}
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void SevenBitEncodedValueWithMoreThanFiveBytesThrows(DataStreamKind kind) {
			using (var source = TestDataReader.Create(kind, 0x80, 0x80, 0x80, 0x80, 0x80, 0x01)) {
				var reader = source.Reader;
				Assert.Throws<DataReaderException>(() => reader.Read7BitEncodedUInt32());
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void TruncatedSevenBitEncodedValueThrows(DataStreamKind kind) {
			using (var source = TestDataReader.Create(kind, 0x80, 0x80)) {
				var reader = source.Reader;
				Assert.Throws<DataReaderException>(() => reader.Read7BitEncodedUInt32());
			}
		}
	}
}
