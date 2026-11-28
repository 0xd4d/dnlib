// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using dnlib.DotNet.Pdb.Dss;
using dnlib.IO;
using Xunit;

namespace dnlib.Tests.Pdb {
	/// <summary>
	/// The native Windows PDB reader and writer (Microsoft.DiaSymReader.Native) read and write the PDB through a
	/// COM <see cref="IStream"/>. dnlib has one for a <see cref="Stream"/> (the writer) and one for a
	/// <see cref="DataReaderFactory"/> (the reader). They're plain .NET code, so they're tested on every OS.
	/// </summary>
	public class ComStreamTests {
		// Calls a method that has an out parameter that's a pointer to a number
		static long WithNumber(Action<IntPtr> call, bool is64Bit = true) {
			var memory = Marshal.AllocHGlobal(8);
			try {
				Marshal.WriteInt64(memory, 0);
				call(memory);
				return is64Bit ? Marshal.ReadInt64(memory) : Marshal.ReadInt32(memory);
			}
			finally {
				Marshal.FreeHGlobal(memory);
			}
		}

		const int SeekSet = 0, SeekCurrent = 1, SeekEnd = 2;

		[Fact]
		public void AStreamIsReadWrittenAndSeeked() {
			var stream = new MemoryStream();
			var istream = new StreamIStream(stream, "name");

			Assert.Equal(4, WithNumber(p => istream.Write(new byte[] { 1, 2, 3, 4 }, 4, p), is64Bit: false));
			Assert.Equal(1, WithNumber(p => istream.Seek(1, SeekSet, p)));
			Assert.Equal(2, WithNumber(p => istream.Seek(1, SeekCurrent, p)));
			Assert.Equal(3, WithNumber(p => istream.Seek(-1, SeekEnd, p)));
			Assert.Equal(3, WithNumber(p => istream.Seek(0, 99, p)));
			var buffer = new byte[4];
			Assert.Equal(1, WithNumber(p => istream.Read(buffer, 4, p), is64Bit: false));
			Assert.Equal(4, buffer[0]);
			istream.Read(buffer, -1, IntPtr.Zero);
			// A position before the start can't be set
			Assert.ThrowsAny<Exception>(() => istream.Seek(-1, SeekSet, IntPtr.Zero));

			istream.SetSize(2);
			Assert.Equal(2, stream.Length);
			Assert.ThrowsAny<Exception>(() => istream.SetSize(-1));
			istream.Stat(out var stat, 0);
			Assert.Equal(2, stat.cbSize);
			Assert.Equal("name", stat.pwcsName);
			istream.Stat(out stat, 1);
			Assert.Null(stat.pwcsName);
			// These do nothing or aren't supported
			istream.Commit(0);
			istream.Revert();
			Assert.ThrowsAny<Exception>(() => istream.Clone(out _));
			Assert.ThrowsAny<Exception>(() => istream.LockRegion(0, 1, 0));
			Assert.ThrowsAny<Exception>(() => istream.UnlockRegion(0, 1, 0));
		}

		[Fact]
		public void AStreamIsCopiedToAnotherStream() {
			var source = new StreamIStream(new MemoryStream(new byte[] { 1, 2, 3, 4, 5 }));
			var destinationStream = new MemoryStream();
			var destination = new StreamIStream(destinationStream);
			source.Seek(1, SeekSet, IntPtr.Zero);

			var memory = Marshal.AllocHGlobal(16);
			try {
				// More bytes than the stream has: the rest of it is copied
				source.CopyTo(destination, 100, memory, memory + 8);
				Assert.Equal(4, Marshal.ReadInt64(memory));
				Assert.Equal(4, Marshal.ReadInt64(memory + 8));
			}
			finally {
				Marshal.FreeHGlobal(memory);
			}
			source.Seek(0, SeekSet, IntPtr.Zero);
			source.CopyTo(destination, -1, IntPtr.Zero, IntPtr.Zero);

			Assert.Equal(new byte[] { 2, 3, 4, 5 }, destinationStream.ToArray());
		}

		// A stream that starts at the position of the Stream: the data before it can't be read or changed
		[Fact]
		public void AnEmptyStreamStartsAtThePositionOfTheStream() {
			var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
			stream.Position = 2;
			var istream = StreamIStream.CreateEmpty(stream);

			Assert.Equal(2, stream.Length);
			istream.Write(new byte[] { 9 }, 1, IntPtr.Zero);
			Assert.Equal(1, WithNumber(p => istream.Seek(0, SeekCurrent, p)));
			Assert.Equal(new byte[] { 1, 2, 9 }, stream.ToArray());
		}

		[Fact]
		public void TheDataOfAReaderFactoryIsRead() {
			var factory = ByteArrayDataReaderFactory.Create(new byte[] { 1, 2, 3, 4, 5 }, "file.pdb");
			var istream = new DataReaderIStream(factory);

			Assert.Equal(4, WithNumber(p => istream.Seek(-1, SeekEnd, p)));
			Assert.Equal(1, WithNumber(p => istream.Seek(1, SeekSet, p)));
			Assert.Equal(3, WithNumber(p => istream.Seek(2, SeekCurrent, p)));
			var buffer = new byte[4];
			Assert.Equal(2, WithNumber(p => istream.Read(buffer, 4, p), is64Bit: false));
			Assert.Equal(new byte[] { 4, 5, 0, 0 }, buffer);
			istream.Read(buffer, -1, IntPtr.Zero);

			// A clone has the position of the stream it was cloned from
			istream.Seek(1, SeekSet, IntPtr.Zero);
			istream.Clone(out var clone);
			clone.Read(buffer, 1, IntPtr.Zero);
			Assert.Equal(2, buffer[0]);

			istream.Stat(out var stat, 0);
			Assert.Equal(5, stat.cbSize);
			istream.Stat(out stat, 1);
			Assert.Null(stat.pwcsName);
			// A reader can't be changed
			istream.Commit(0);
			istream.Revert();
			Assert.ThrowsAny<Exception>(() => istream.Write(new byte[1], 1, IntPtr.Zero));
			Assert.ThrowsAny<Exception>(() => istream.SetSize(1));
			Assert.ThrowsAny<Exception>(() => istream.LockRegion(0, 1, 0));
			Assert.ThrowsAny<Exception>(() => istream.UnlockRegion(0, 1, 0));
			istream.Dispose();
		}

		[Fact]
		public void TheDataOfAReaderFactoryIsCopiedToAStream() {
			var factory = ByteArrayDataReaderFactory.Create(new byte[] { 1, 2, 3, 4, 5 }, null);
			var istream = new DataReaderIStream(factory);
			var destinationStream = new MemoryStream();
			istream.Seek(2, SeekSet, IntPtr.Zero);

			var memory = Marshal.AllocHGlobal(16);
			try {
				istream.CopyTo(new StreamIStream(destinationStream), 100, memory, memory + 8);
				Assert.Equal(3, Marshal.ReadInt64(memory));
				Assert.Equal(3, Marshal.ReadInt64(memory + 8));
			}
			finally {
				Marshal.FreeHGlobal(memory);
			}
			istream.CopyTo(new StreamIStream(destinationStream), -1, IntPtr.Zero, IntPtr.Zero);

			Assert.Equal(new byte[] { 3, 4, 5 }, destinationStream.ToArray());
		}
	}
}
