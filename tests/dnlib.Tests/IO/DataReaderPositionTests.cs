// dnlib: See LICENSE.txt for more info

using dnlib.IO;
using Xunit;

namespace dnlib.Tests.IO {
	/// <summary>
	/// Offsets, positions and slices of a <see cref="DataReader"/>
	/// </summary>
	public class DataReaderPositionTests {
		public static TheoryData<DataStreamKind> Kinds => TestDataReader.AllKinds;

		[Theory, MemberData(nameof(Kinds))]
		public void NewReaderIsAtItsStart(DataStreamKind kind) {
			using (var source = TestDataReader.Create(kind, 1, 2, 3, 4)) {
				var reader = source.Reader;
				// TestDataReader stores one extra byte before the data
				Assert.Equal(1U, reader.StartOffset);
				Assert.Equal(5U, reader.EndOffset);
				Assert.Equal(1U, reader.CurrentOffset);
				Assert.Equal(4U, reader.Length);
				Assert.Equal(0U, reader.Position);
				Assert.Equal(4U, reader.BytesLeft);
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void PositionIsRelativeToTheStartAndCurrentOffsetIsNot(DataStreamKind kind) {
			using (var source = TestDataReader.Create(kind, 1, 2, 3, 4)) {
				var reader = source.Reader;

				reader.Position = 3;
				Assert.Equal(4U, reader.CurrentOffset);
				Assert.Equal(1U, reader.BytesLeft);
				Assert.Equal(4, reader.ReadByte());

				reader.CurrentOffset = 2;
				Assert.Equal(1U, reader.Position);
				Assert.Equal(2, reader.ReadByte());
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void PositionCanBeTheEnd(DataStreamKind kind) {
			using (var source = TestDataReader.Create(kind, 1, 2, 3, 4)) {
				var reader = source.Reader;

				reader.Position = 4;
				Assert.Equal(0U, reader.BytesLeft);

				reader.CurrentOffset = reader.EndOffset;
				Assert.Equal(0U, reader.BytesLeft);
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void InvalidPositionThrowsAndDoesNotMoveTheReader(DataStreamKind kind) {
			using (var source = TestDataReader.Create(kind, 1, 2, 3, 4)) {
				var reader = source.Reader;
				reader.Position = 2;

				Assert.Throws<DataReaderException>(() => reader.Position = 5);
				Assert.Throws<DataReaderException>(() => reader.CurrentOffset = 0);
				Assert.Throws<DataReaderException>(() => reader.CurrentOffset = 6);
				Assert.Equal(2U, reader.Position);
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void ResetMovesToTheStart(DataStreamKind kind) {
			using (var source = TestDataReader.Create(kind, 1, 2, 3, 4)) {
				var reader = source.Reader;
				reader.ReadUInt16();

				reader.Reset();

				Assert.Equal(0U, reader.Position);
				Assert.Equal(1, reader.ReadByte());
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void CanRead(DataStreamKind kind) {
			using (var source = TestDataReader.Create(kind, 1, 2, 3, 4)) {
				var reader = source.Reader;
				reader.Position = 1;

				Assert.True(reader.CanRead(0));
				Assert.True(reader.CanRead(3));
				Assert.False(reader.CanRead(4));
				Assert.False(reader.CanRead(-1));
				Assert.True(reader.CanRead(3U));
				Assert.False(reader.CanRead(4U));
				Assert.False(reader.CanRead(uint.MaxValue));
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void SliceIsRelativeToTheStartAndIgnoresThePosition(DataStreamKind kind) {
			using (var source = TestDataReader.Create(kind, 1, 2, 3, 4, 5)) {
				var reader = source.Reader;
				reader.Position = 4;

				var slice = reader.Slice(1, 3);

				Assert.Equal(3U, slice.Length);
				Assert.Equal(0U, slice.Position);
				Assert.Equal(reader.StartOffset + 1, slice.StartOffset);
				Assert.Equal(new byte[] { 2, 3, 4 }, slice.ToArray());
				// The original reader isn't changed
				Assert.Equal(4U, reader.Position);
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void SliceWithoutLengthGoesToTheEnd(DataStreamKind kind) {
			using (var source = TestDataReader.Create(kind, 1, 2, 3, 4, 5)) {
				var reader = source.Reader;

				Assert.Equal(new byte[] { 3, 4, 5 }, reader.Slice(2).ToArray());
				Assert.Equal(new byte[] { 3, 4, 5 }, reader.Slice(2U).ToArray());
				Assert.Empty(reader.Slice(5).ToArray());
				Assert.Empty(reader.Slice(5U).ToArray());
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void SliceCanNotReadOutsideItsRange(DataStreamKind kind) {
			using (var source = TestDataReader.Create(kind, 1, 2, 3, 4, 5)) {
				var slice = source.Reader.Slice(1, 2);

				slice.ReadUInt16();

				Assert.Throws<DataReaderException>(() => slice.ReadByte());
			}
		}

		[Theory, MemberData(nameof(Kinds))]
		public void SliceOutsideTheReaderThrows(DataStreamKind kind) {
			using (var source = TestDataReader.Create(kind, 1, 2, 3, 4, 5)) {
				var reader = source.Reader;

				Assert.Throws<DataReaderException>(() => reader.Slice(0, 6));
				Assert.Throws<DataReaderException>(() => reader.Slice(5, 1));
				Assert.Throws<DataReaderException>(() => reader.Slice(6));
				Assert.Throws<DataReaderException>(() => reader.Slice(-1));
				Assert.Throws<DataReaderException>(() => reader.Slice(-1, 1));
				Assert.Throws<DataReaderException>(() => reader.Slice(0, -1));
				Assert.Throws<DataReaderException>(() => reader.Slice(0U, 6U));
				Assert.Throws<DataReaderException>(() => reader.Slice(6U));
				// start + length overflows a uint
				Assert.Throws<DataReaderException>(() => reader.Slice(2U, uint.MaxValue));
			}
		}

		[Fact]
		public void DefaultReaderIsEmpty() {
			var reader = default(DataReader);

			Assert.Equal(0U, reader.Length);
			Assert.Equal(0U, reader.BytesLeft);
			Assert.False(reader.CanRead(1));
			Assert.Empty(reader.ToArray());
			Assert.Empty(reader.ReadBytes(0));
			Assert.Empty(reader.ReadRemainingBytes());
			Assert.Null(reader.TryReadZeroTerminatedUtf8String());
			Assert.Null(reader.TryReadBytesUntil(0));
			Assert.Throws<DataReaderException>(() => reader.ReadByte());
			Assert.False(reader.TryReadCompressedUInt32(out _));
		}
	}
}
