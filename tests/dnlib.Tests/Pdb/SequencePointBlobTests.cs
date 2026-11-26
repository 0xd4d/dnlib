// dnlib: See LICENSE.txt for more info

using System.Linq;
using System.Reflection.Metadata;
using dnlib.TestUtils;
using Xunit;
using SequencePoint = dnlib.DotNet.Pdb.SequencePoint;

namespace dnlib.Tests.Pdb {
	/// <summary>
	/// The biggest and smallest lines and columns of a sequence point in a portable PDB. A line is in
	/// [0, 0x20000000) and isn't 0xFEEFEE (the line of a hidden sequence point), a column is in [0, 0x10000).
	/// A sequence point with another value is invalid, and dnlib then reads no sequence point of the method.
	///
	/// These are the tests and the values of System.Reflection.Metadata (dotnet/runtime#136403). Its versions
	/// before .NET 12 also reject lines above 0xFEEFEE and the column 0xFFFF, so the files are only read with
	/// dnlib here.
	/// </summary>
	public class SequencePointBlobTests {
		const int HiddenLine = 0xFEEFEE;

		// Returns the sequence points of the method's instructions (null: the instruction has none)
		static SequencePoint?[] Read(BlobBuilder sequencePoints) {
			using (var module = RoundTrip.LoadWithPdb(RawPortablePdb.Image, RawPortablePdb.WriteSequencePoints(sequencePoints)))
				return RawPortablePdb.GetMethod(module).Body!.Instructions.Select(a => a.SequencePoint).ToArray();
		}

		// The blob of one sequence point at IL offset 0, the first one of a method
		static BlobBuilder FirstPoint(int deltaLines, int deltaColumns, int startLine, int startColumn) {
			var points = new BlobBuilder();
			points.WriteCompressedInteger(0); // local signature
			points.WriteCompressedInteger(0); // IL offset
			AddLinesAndColumns(points, deltaLines, deltaColumns);
			points.WriteCompressedInteger(startLine);
			points.WriteCompressedInteger(startColumn);
			return points;
		}

		// Adds a sequence point that follows another one, at the next IL offset
		static void AddNextPoint(BlobBuilder points, int deltaLines, int deltaColumns, int deltaStartLine, int deltaStartColumn) {
			points.WriteCompressedInteger(1); // delta IL offset
			AddLinesAndColumns(points, deltaLines, deltaColumns);
			points.WriteCompressedSignedInteger(deltaStartLine);
			points.WriteCompressedSignedInteger(deltaStartColumn);
		}

		// The number of columns is signed if the sequence point has more than one line
		static void AddLinesAndColumns(BlobBuilder points, int deltaLines, int deltaColumns) {
			points.WriteCompressedInteger(deltaLines);
			if (deltaLines == 0)
				points.WriteCompressedInteger(deltaColumns);
			else
				points.WriteCompressedSignedInteger(deltaColumns);
		}

		static void AssertIsRead(SequencePoint?[] sequencePoints, int index, int startLine, int startColumn, int endLine, int endColumn) {
			var sequencePoint = sequencePoints[index];
			Assert.NotNull(sequencePoint);
			Assert.Equal((startLine, startColumn, endLine, endColumn),
				(sequencePoint.StartLine, sequencePoint.StartColumn, sequencePoint.EndLine, sequencePoint.EndColumn));
		}

		static void AssertNothingIsRead(SequencePoint?[] sequencePoints) => Assert.All(sequencePoints, a => Assert.Null(a));

		[Theory]
		[InlineData(0, true)]
		[InlineData(65534, true)]
		[InlineData(65535, true)]
		[InlineData(65536, false)]
		[InlineData(0x1FFFFFFF, false)]
		public void StartColumnOfTheFirstPoint(int startColumn, bool valid) {
			var sequencePoints = Read(FirstPoint(deltaLines: 1, deltaColumns: 0, startLine: 1, startColumn));

			if (valid)
				AssertIsRead(sequencePoints, 0, 1, startColumn, 2, startColumn);
			else
				AssertNothingIsRead(sequencePoints);
		}

		[Theory]
		[InlineData(0, 1, 65533, true)]
		[InlineData(0, 1, 65534, true)]
		[InlineData(0, 1, 65535, false)]
		[InlineData(0, 65535, 0x1FFFFFFF, false)]
		[InlineData(1, 1, 65533, true)]
		[InlineData(1, 1, 65534, true)]
		[InlineData(1, 1, 65535, false)]
		[InlineData(1, 65535, -65535, true)]
		[InlineData(1, 65535, -65536, false)]
		[InlineData(1, 65535, 0x0FFFFFFF, false)]
		[InlineData(1, 0, -0x10000000, false)]
		public void EndColumn(int deltaLines, int startColumn, int deltaColumns, bool valid) {
			var sequencePoints = Read(FirstPoint(deltaLines, deltaColumns, startLine: 1, startColumn));

			if (valid)
				AssertIsRead(sequencePoints, 0, 1, startColumn, 1 + deltaLines, startColumn + deltaColumns);
			else
				AssertNothingIsRead(sequencePoints);
		}

		[Theory]
		[InlineData(1, -1, true)]
		[InlineData(1, 65533, true)]
		[InlineData(1, 65534, true)]
		[InlineData(1, 65535, false)]
		[InlineData(1, -2, false)]
		[InlineData(65535, -65535, true)]
		[InlineData(65535, -65536, false)]
		[InlineData(65535, 0x0FFFFFFF, false)]
		[InlineData(0, -0x10000000, false)]
		public void StartColumnOfTheNextPoint(int startColumn, int deltaStartColumn, bool valid) {
			var points = FirstPoint(deltaLines: 1, deltaColumns: 0, startLine: 1, startColumn);
			AddNextPoint(points, deltaLines: 1, deltaColumns: 0, deltaStartLine: 0, deltaStartColumn);
			var sequencePoints = Read(points);

			if (valid) {
				AssertIsRead(sequencePoints, 0, 1, startColumn, 2, startColumn);
				AssertIsRead(sequencePoints, 1, 1, startColumn + deltaStartColumn, 2, startColumn + deltaStartColumn);
			}
			else
				AssertNothingIsRead(sequencePoints);
		}

		[Theory]
		[InlineData(0, true)]
		[InlineData(HiddenLine - 2, true)]
		[InlineData(HiddenLine - 1, false)] // the end line is the hidden line
		[InlineData(HiddenLine, false)]
		[InlineData(HiddenLine + 1, true)]
		[InlineData(0x1FFFFFFE, true)]
		public void StartLineOfTheFirstPoint(int startLine, bool valid) {
			var sequencePoints = Read(FirstPoint(deltaLines: 1, deltaColumns: 0, startLine, startColumn: 1));

			if (valid)
				AssertIsRead(sequencePoints, 0, startLine, 1, startLine + 1, 1);
			else
				AssertNothingIsRead(sequencePoints);
		}

		[Theory]
		[InlineData(0, HiddenLine - 1, true)]
		[InlineData(0, HiddenLine, false)]
		[InlineData(0, HiddenLine + 1, true)]
		[InlineData(0x1FFFFFFE, 1, true)]
		[InlineData(0x1FFFFFFF, 0, true)]
		[InlineData(0x1FFFFFFF, 1, false)]
		[InlineData(0x1FFFFFFF, 0x1FFFFFFF, false)]
		public void EndLine(int startLine, int deltaLines, bool valid) {
			var sequencePoints = Read(FirstPoint(deltaLines, deltaColumns: 1, startLine, startColumn: 1));

			if (valid)
				AssertIsRead(sequencePoints, 0, startLine, 1, startLine + deltaLines, 2);
			else
				AssertNothingIsRead(sequencePoints);
		}

		[Theory]
		[InlineData(0, HiddenLine - 1, true)]
		[InlineData(0, HiddenLine, false)]
		[InlineData(0, HiddenLine + 1, true)]
		[InlineData(HiddenLine + 1, -1, false)]
		[InlineData(0x10000000, 0x0FFFFFFF, true)]
		[InlineData(0x1FFFFFFF, 1, false)]
		[InlineData(0, -1, false)]
		[InlineData(0x10000000, -0x10000000, true)]
		[InlineData(0x1FFFFFFF, -0x10000000, true)]
		public void StartLineOfTheNextPoint(int startLine, int deltaStartLine, bool valid) {
			var points = FirstPoint(deltaLines: 0, deltaColumns: 1, startLine, startColumn: 1);
			AddNextPoint(points, deltaLines: 0, deltaColumns: 1, deltaStartLine, deltaStartColumn: 0);
			var sequencePoints = Read(points);

			if (valid) {
				AssertIsRead(sequencePoints, 0, startLine, 1, startLine, 2);
				AssertIsRead(sequencePoints, 1, startLine + deltaStartLine, 1, startLine + deltaStartLine, 2);
			}
			else
				AssertNothingIsRead(sequencePoints);
		}

		// A hidden sequence point has no line: the point after it has the difference to the line of the last
		// point that isn't hidden
		[Fact]
		public void HiddenPointBetweenTwoPointsWithBigLines() {
			var points = FirstPoint(deltaLines: 0, deltaColumns: 1, startLine: 0x1FFFFFFF, startColumn: 1);
			points.WriteCompressedInteger(1); // delta IL offset
			points.WriteCompressedInteger(0); // hidden: no lines
			points.WriteCompressedInteger(0); // and no columns
			AddNextPoint(points, deltaLines: 0, deltaColumns: 1, deltaStartLine: -1, deltaStartColumn: 0);
			var sequencePoints = Read(points);

			AssertIsRead(sequencePoints, 0, 0x1FFFFFFF, 1, 0x1FFFFFFF, 2);
			AssertIsRead(sequencePoints, 1, HiddenLine, 0, HiddenLine, 0);
			AssertIsRead(sequencePoints, 2, 0x1FFFFFFE, 1, 0x1FFFFFFE, 2);
			Assert.Null(sequencePoints[3]);
		}

		// An IL offset is the sum of the differences that are in the blob. System.Reflection.Metadata throws if
		// the sum doesn't fit in an int, and dnlib reads no sequence point of the method.
		// Fixed in af20934b: the offset became a negative number or started at 0 again.
		[Theory]
		[InlineData(4, true)]
		[InlineData(5, false)]
		[InlineData(9, false)]
		public void OffsetThatDoesNotFitInAnInt(int bigDifferences, bool valid) {
			var points = FirstPoint(deltaLines: 0, deltaColumns: 1, startLine: 1, startColumn: 1);
			// 4 * 0x1FFFFFFF is 0x7FFFFFFC, the next sum is too big, and the 9th one is bigger than a uint
			for (int i = 0; i < bigDifferences; i++) {
				points.WriteCompressedInteger(0x1FFFFFFF); // delta IL offset
				points.WriteCompressedInteger(0); // hidden: no lines
				points.WriteCompressedInteger(0); // and no columns
			}
			var sequencePoints = Read(points);

			if (valid) {
				AssertIsRead(sequencePoints, 0, 1, 1, 1, 2);
				Assert.All(sequencePoints.Skip(1), a => Assert.Null(a));
			}
			else
				AssertNothingIsRead(sequencePoints);
		}
	}
}
