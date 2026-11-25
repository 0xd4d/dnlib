// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Linq;
using System.Text;
using dnlib.IO;
using Xunit;

namespace dnlib.Tests.IO {
	/// <summary>
	/// The methods of <see cref="DataReader"/> that read byte arrays and strings
	/// </summary>
	public class DataReaderBytesAndStringsTests {
		public static TheoryData<DataStreamKind> Kinds => TestDataReader.AllKinds;

		[Theory, MemberData(nameof(Kinds))]
		public void ReadBytesReturnsANewArray(DataStreamKind kind) {
			using (var source = TestDataReader.Create(kind, 1, 2, 3, 4, 5)) {
				var reader = source.Reader;
				reader.Position = 1;

				Assert.Equal(new byte[] { 2, 3, 4 }, reader.ReadBytes(3));
				Assert.Equal(4U, reader.Position);
				Assert.Empty(reader.ReadBytes(0));
				Assert.Equal(4U, reader.Position);
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void ReadBytesIntoAnArray(DataStreamKind kind) {
			using (var source = TestDataReader.Create(kind, 1, 2, 3, 4, 5)) {
				var reader = source.Reader;
				var destination = new byte[] { 0xAA, 0xAA, 0xAA, 0xAA, 0xAA };

				reader.ReadBytes(destination, 1, 3);

				Assert.Equal(new byte[] { 0xAA, 1, 2, 3, 0xAA }, destination);
				Assert.Equal(3U, reader.Position);
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public unsafe void ReadBytesIntoNativeMemory(DataStreamKind kind) {
			using (var source = TestDataReader.Create(kind, 1, 2, 3, 4, 5)) {
				var reader = source.Reader;
				var destination = new byte[] { 0xAA, 0xAA, 0xAA, 0xAA, 0xAA };

				fixed (byte* p = destination)
					reader.ReadBytes(p + 1, 3);

				Assert.Equal(new byte[] { 0xAA, 1, 2, 3, 0xAA }, destination);
				Assert.Equal(3U, reader.Position);
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public unsafe void ReadBytesWithTooManyBytesThrowsAndDoesNotMoveTheReader(DataStreamKind kind) {
			using (var source = TestDataReader.Create(kind, 1, 2, 3, 4, 5)) {
				var reader = source.Reader;
				reader.Position = 3;
				var destination = new byte[10];

				Assert.Throws<DataReaderException>(() => reader.ReadBytes(3));
				Assert.Throws<DataReaderException>(() => reader.ReadBytes(destination, 0, 3));
				Assert.Throws<DataReaderException>(() => reader.ReadBytes(int.MaxValue));
				Assert.Throws<DataReaderException>(() => {
					fixed (byte* p = destination)
						reader.ReadBytes(p, 3);
				});
				Assert.Equal(3U, reader.Position);
				// Nothing was copied
				Assert.Equal(new byte[10], destination);
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public unsafe void ReadBytesWithInvalidArgumentsThrows(DataStreamKind kind) {
			using (var source = TestDataReader.Create(kind, 1, 2, 3, 4, 5)) {
				var reader = source.Reader;

				Assert.Throws<DataReaderException>(() => reader.ReadBytes(-1));
				Assert.Throws<DataReaderException>(() => reader.ReadBytes(new byte[5], -1, 1));
				Assert.Throws<DataReaderException>(() => reader.ReadBytes(new byte[5], 0, -1));
				Assert.Throws<ArgumentNullException>(() => reader.ReadBytes(null!, 0, 1));
				Assert.Throws<ArgumentNullException>(() => reader.ReadBytes(null, 1));
				Assert.Equal(0U, reader.Position);
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void ToArrayReturnsAllBytesAndIgnoresThePosition(DataStreamKind kind) {
			using (var source = TestDataReader.Create(kind, 1, 2, 3, 4, 5)) {
				var reader = source.Reader;
				reader.Position = 3;

				Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, reader.ToArray());
				Assert.Equal(3U, reader.Position);
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void ReadRemainingBytesReadsFromThePositionToTheEnd(DataStreamKind kind) {
			using (var source = TestDataReader.Create(kind, 1, 2, 3, 4, 5)) {
				var reader = source.Reader;
				reader.Position = 3;

				Assert.Equal(new byte[] { 4, 5 }, reader.ReadRemainingBytes());
				Assert.Equal(0U, reader.BytesLeft);
				Assert.Empty(reader.ReadRemainingBytes());
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void TryReadBytesUntilStopsBeforeTheValue(DataStreamKind kind) {
			// Long enough so that the code that checks several bytes at a time is used too
			var bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 0xFF, 12 };
			using (var source = TestDataReader.Create(kind, bytes)) {
				var reader = source.Reader;

				Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 }, reader.TryReadBytesUntil(0xFF));
				// The value itself isn't read
				Assert.Equal(0xFF, reader.ReadByte());
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void TryReadBytesUntilFindsTheValueAtEveryPosition(DataStreamKind kind) {
			for (int valueIndex = 0; valueIndex < 20; valueIndex++) {
				var bytes = new byte[20];
				bytes[valueIndex] = 0xFF;
				using (var source = TestDataReader.Create(kind, bytes)) {
					var reader = source.Reader;

					var result = reader.TryReadBytesUntil(0xFF);

					Assert.NotNull(result);
					Assert.Equal(valueIndex, result.Length);
				}
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void TryReadBytesUntilReturnsNullIfTheValueIsNotInTheReader(DataStreamKind kind) {
			// The byte after the reader's last byte is TestDataReader.GuardByte. It must not be found.
			using (var source = TestDataReader.Create(kind, 1, 2, 3, 4, 5)) {
				var reader = source.Reader;

				Assert.Null(reader.TryReadBytesUntil(TestDataReader.GuardByte));
				Assert.Equal(0U, reader.Position);
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void ReadUtf16String(DataStreamKind kind) {
			using (var source = TestDataReader.Create(kind, (byte)'a', 0, 0xAC, 0x20, (byte)'b', 0)) {
				var reader = source.Reader;

				Assert.Equal("a€", reader.ReadUtf16String(2));
				Assert.Equal(4U, reader.Position);
				Assert.Equal("", reader.ReadUtf16String(0));
				Assert.Throws<DataReaderException>(() => reader.ReadUtf16String(2));
				Assert.Throws<DataReaderException>(() => reader.ReadUtf16String(-1));
				Assert.Equal(4U, reader.Position);
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void ReadUtf16StringKeepsUnpairedSurrogatesAndNullChars(DataStreamKind kind) {
			// Strings in metadata (eg. in the #US heap) can have any UTF-16 code unit
			using (var source = TestDataReader.Create(kind, 0x00, 0xD8, 0x00, 0x00, (byte)'a', 0)) {
				var reader = source.Reader;

				Assert.Equal("\uD800\0a", reader.ReadUtf16String(3));
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void ReadUtf8String(DataStreamKind kind) {
			// "a€b": the euro sign is 3 bytes
			using (var source = TestDataReader.Create(kind, (byte)'a', 0xE2, 0x82, 0xAC, (byte)'b')) {
				var reader = source.Reader;

				Assert.Equal("a€", reader.ReadUtf8String(4));
				Assert.Equal(4U, reader.Position);
				Assert.Equal("", reader.ReadUtf8String(0));
				Assert.Throws<DataReaderException>(() => reader.ReadUtf8String(2));
				Assert.Throws<DataReaderException>(() => reader.ReadUtf8String(-1));
				Assert.Equal(4U, reader.Position);
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void ReadStringUsesTheEncoding(DataStreamKind kind) {
			using (var source = TestDataReader.Create(kind, (byte)'a', 0, (byte)'b', 0)) {
				var reader = source.Reader;

				Assert.Equal("ab", reader.ReadString(4, Encoding.Unicode));
				Assert.Throws<ArgumentNullException>(() => reader.ReadString(0, null!));
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void ReadSerializedStringReadsALengthPrefixedString(DataStreamKind kind) {
			using (var source = TestDataReader.Create(kind, 3, (byte)'a', (byte)'b', (byte)'c', 0, 5, (byte)'x')) {
				var reader = source.Reader;

				Assert.Equal("abc", reader.ReadSerializedString());
				Assert.Equal("", reader.ReadSerializedString());
				// The length is 5 but there's only 1 byte left
				Assert.Throws<DataReaderException>(() => reader.ReadSerializedString());
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void ReadSerializedStringWithNegativeLengthThrows(DataStreamKind kind) {
			// 7-bit encoded 0xFFFFFFFF, which is -1 as an int
			using (var source = TestDataReader.Create(kind, 0xFF, 0xFF, 0xFF, 0xFF, 0x0F, (byte)'a')) {
				var reader = source.Reader;

				Assert.Throws<DataReaderException>(() => reader.ReadSerializedString());
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void TryReadZeroTerminatedUtf8StringReadsTheTerminatorToo(DataStreamKind kind) {
			using (var source = TestDataReader.Create(kind, (byte)'a', (byte)'b', 0, 0, (byte)'c', 0)) {
				var reader = source.Reader;

				Assert.Equal("ab", reader.TryReadZeroTerminatedUtf8String());
				Assert.Equal(3U, reader.Position);
				Assert.Equal("", reader.TryReadZeroTerminatedUtf8String());
				Assert.Equal(4U, reader.Position);
				Assert.Equal("c", reader.TryReadZeroTerminatedUtf8String());
				// At the end
				Assert.Null(reader.TryReadZeroTerminatedUtf8String());
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void TryReadZeroTerminatedUtf8StringReturnsNullIfThereIsNoTerminator(DataStreamKind kind) {
			using (var source = TestDataReader.Create(kind, (byte)'a', (byte)'b')) {
				var reader = source.Reader;

				Assert.Null(reader.TryReadZeroTerminatedUtf8String());
				Assert.Equal(0U, reader.Position);
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void Utf8StringWithAnEncodedUnpairedSurrogateKeepsIt(DataStreamKind kind) {
			// ED A0 80 is U+D800 encoded as 3 bytes. It's not valid UTF-8 and the decoder replaces it with
			// U+FFFD, but a .NET string can have unpaired surrogates and compilers write them like this.
			using (var source = TestDataReader.Create(kind, (byte)'a', 0xED, 0xA0, 0x80, (byte)'b', 0)) {
				var reader = source.Reader;
				Assert.Equal("a\uD800b", reader.ReadUtf8StringKeepUnpairedSurrogates(5));

				reader = source.Reader;
				Assert.Equal("a\uD800b", reader.TryReadZeroTerminatedUtf8StringKeepUnpairedSurrogates());
				Assert.Equal(6U, reader.Position);

				// The public methods use the normal decoder
				reader = source.Reader;
				Assert.Contains('�', reader.ReadUtf8String(5));
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void CopyToStreamCopiesFromThePositionToTheEnd(DataStreamKind kind) {
			var bytes = new byte[0x5000];
			for (int i = 0; i < bytes.Length; i++)
				bytes[i] = (byte)(i * 7);
			using (var source = TestDataReader.Create(kind, bytes)) {
				var reader = source.Reader;
				reader.Position = 3;
				var destination = new MemoryStream();

				reader.CopyTo(destination);

				var expected = new byte[bytes.Length - 3];
				Array.Copy(bytes, 3, expected, 0, expected.Length);
				Assert.Equal(expected, destination.ToArray());
				Assert.Equal(0U, reader.BytesLeft);
			}
		}

		// The other overloads: the destination can be a BinaryWriter or dnlib's DataWriter, and the caller can
		// pass the buffer that's used for the copy (here a small one, so it's filled more than once)
		[Theory, MemberData(nameof(Kinds))]
		public void CopyToEveryKindOfDestination(DataStreamKind kind) {
			var data = Enumerable.Range(1, 100).Select(a => (byte)a).ToArray();
			var expected = data.Skip(3).ToArray();
			using (var source = TestDataReader.Create(kind, data)) {
				byte[] Copy(Action<DataReader, MemoryStream> copy) {
					var reader = source.Reader;
					reader.Position = 3;
					var destination = new MemoryStream();
					copy(reader, destination);
					return destination.ToArray();
				}

				Assert.Equal(expected, Copy((reader, stream) => reader.CopyTo(stream, new byte[7])));
				Assert.Equal(expected, Copy((reader, stream) => reader.CopyTo(new BinaryWriter(stream))));
				Assert.Equal(expected, Copy((reader, stream) => reader.CopyTo(new BinaryWriter(stream), new byte[7])));
				Assert.Equal(expected, Copy((reader, stream) => reader.CopyTo(new dnlib.DotNet.Writer.DataWriter(stream))));
				Assert.Equal(expected, Copy((reader, stream) => reader.CopyTo(new dnlib.DotNet.Writer.DataWriter(stream), new byte[7])));
				// Nothing is left to copy at the end
				Assert.Empty(Copy((reader, stream) => {
					reader.Position = reader.Length;
					reader.CopyTo(stream);
					reader.CopyTo(new BinaryWriter(stream));
					reader.CopyTo(new dnlib.DotNet.Writer.DataWriter(stream));
					reader.CopyTo(stream, new byte[7]);
				}));
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void CopyToWithoutADestinationOrABufferThrows(DataStreamKind kind) {
			using (var source = TestDataReader.Create(kind, 1, 2, 3)) {
				var reader = source.Reader;
				var stream = new MemoryStream();

				Assert.Throws<ArgumentNullException>(() => source.Reader.CopyTo((Stream)null!));
				Assert.Throws<ArgumentNullException>(() => source.Reader.CopyTo((Stream)null!, new byte[7]));
				Assert.Throws<ArgumentNullException>(() => source.Reader.CopyTo((BinaryWriter)null!));
				Assert.Throws<ArgumentNullException>(() => source.Reader.CopyTo((BinaryWriter)null!, new byte[7]));
				Assert.Throws<ArgumentNullException>(() => source.Reader.CopyTo((dnlib.DotNet.Writer.DataWriter)null!));
				Assert.Throws<ArgumentNullException>(() => source.Reader.CopyTo((dnlib.DotNet.Writer.DataWriter)null!, new byte[7]));
				Assert.Throws<ArgumentNullException>(() => source.Reader.CopyTo(stream, null!));
				// A buffer without room for a single byte
				Assert.Throws<DataReaderException>(() => source.Reader.CopyTo(stream, new byte[0]));
				Assert.Equal(0U, reader.Position);
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void AsStreamReadsTheSameBytes(DataStreamKind kind) {
			using (var source = TestDataReader.Create(kind, 1, 2, 3, 4, 5)) {
				var reader = source.Reader;
				reader.Position = 1;

				var stream = reader.AsStream();

				Assert.True(stream.CanRead);
				Assert.True(stream.CanSeek);
				Assert.False(stream.CanWrite);
				Assert.Equal(5, stream.Length);
				// The stream starts at the reader's position
				Assert.Equal(1, stream.Position);
				Assert.Equal(2, stream.ReadByte());

				var buffer = new byte[10];
				Assert.Equal(3, stream.Read(buffer, 1, 10 - 1));
				Assert.Equal(new byte[] { 0, 3, 4, 5, 0, 0, 0, 0, 0, 0 }, buffer);
				Assert.Equal(-1, stream.ReadByte());
				Assert.Equal(0, stream.Read(buffer, 0, 1));

				Assert.Equal(4, stream.Seek(-1, SeekOrigin.End));
				Assert.Equal(5, stream.ReadByte());
				Assert.Equal(0, stream.Seek(0, SeekOrigin.Begin));
				Assert.Equal(2, stream.Seek(2, SeekOrigin.Current));
				Assert.Equal(3, stream.ReadByte());

				// The reader the stream was created from isn't moved
				Assert.Equal(1U, reader.Position);
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void AsStreamPositionOutsideTheDataReadsNothing(DataStreamKind kind) {
			using (var source = TestDataReader.Create(kind, 1, 2, 3, 4, 5)) {
				var stream = source.Reader.AsStream();

				stream.Position = 6;
				Assert.Equal(-1, stream.ReadByte());
				Assert.Equal(0, stream.Read(new byte[1], 0, 1));

				stream.Position = -1;
				Assert.Equal(-1, stream.ReadByte());
				Assert.Equal(0, stream.Read(new byte[1], 0, 1));
			}
		}
	}
}
