// dnlib: See LICENSE.txt for more info

using System;
using System.Runtime.InteropServices;
using dnlib.IO;
using Xunit;

namespace dnlib.Tests.IO {
	/// <summary>
	/// The <see cref="DataStream"/> implementations. dnlib picks the aligned or the unaligned ones depending on
	/// the CPU, the tests use all of them on every CPU.
	/// </summary>
	public enum DataStreamKind {
		UnalignedByteArray,
		AlignedByteArray,
		UnalignedNativeMemory,
		AlignedNativeMemory,
	}

	/// <summary>
	/// Creates a <see cref="DataReader"/> that reads the bytes a test gives it from one of the
	/// <see cref="DataStream"/> implementations.
	/// </summary>
	/// <remarks>
	/// The bytes are stored at an odd address/index with one extra byte before and after them. So multi-byte
	/// values aren't aligned, and a reader that reads outside its range reads <see cref="GuardByte"/>.
	/// </remarks>
	sealed unsafe class TestDataReader : IDisposable {
		public const byte GuardByte = 0xCC;

		/// <summary>
		/// All <see cref="DataStreamKind"/> values, for <c>[MemberData]</c>
		/// </summary>
		public static TheoryData<DataStreamKind> AllKinds => new TheoryData<DataStreamKind>(KindValues);

		/// <summary>
		/// All <see cref="DataStreamKind"/> values, for tests that have other test data and loop over the kinds
		/// </summary>
		public static readonly DataStreamKind[] KindValues = new[] {
			DataStreamKind.UnalignedByteArray,
			DataStreamKind.AlignedByteArray,
			DataStreamKind.UnalignedNativeMemory,
			DataStreamKind.AlignedNativeMemory,
		};

		/// <summary>
		/// A reader positioned at the first of the bytes that were passed to <see cref="Create"/>.
		/// It's a struct: copy it to a local to read from it.
		/// </summary>
		public DataReader Reader { get; }

		IntPtr nativeMemory;

		TestDataReader(DataStream stream, int length, IntPtr nativeMemory) {
			Reader = new DataReader(stream, 1, (uint)length);
			this.nativeMemory = nativeMemory;
		}

		public static TestDataReader Create(DataStreamKind kind, params byte[] bytes) {
			var buffer = new byte[bytes.Length + 2];
			buffer[0] = GuardByte;
			Array.Copy(bytes, 0, buffer, 1, bytes.Length);
			buffer[buffer.Length - 1] = GuardByte;

			switch (kind) {
			case DataStreamKind.UnalignedByteArray:
				return new TestDataReader(new UnalignedByteArrayDataStream(buffer), bytes.Length, IntPtr.Zero);
			case DataStreamKind.AlignedByteArray:
				return new TestDataReader(new AlignedByteArrayDataStream(buffer), bytes.Length, IntPtr.Zero);
			}

			var memory = Marshal.AllocHGlobal(buffer.Length);
			Marshal.Copy(buffer, 0, memory, buffer.Length);
			DataStream stream = kind == DataStreamKind.UnalignedNativeMemory ?
				new UnalignedNativeMemoryDataStream((byte*)memory) :
				new AlignedNativeMemoryDataStream((byte*)memory);
			return new TestDataReader(stream, bytes.Length, memory);
		}

		public void Dispose() {
			if (nativeMemory != IntPtr.Zero) {
				Marshal.FreeHGlobal(nativeMemory);
				nativeMemory = IntPtr.Zero;
			}
		}
	}
}
