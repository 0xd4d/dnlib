// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace dnlib.IO {
	/// <summary>
	/// Creates <see cref="DataReader"/>s that read memory mapped data
	/// </summary>
	sealed unsafe class MemoryMappedDataReaderFactory : DataReaderFactory {
		/// <summary>
		/// The filename or null if the data is not from a file
		/// </summary>
		public override string Filename => filename;

		/// <summary>
		/// Gets the total length of the data
		/// </summary>
		public override uint Length => length;

		/// <summary>
		/// Raised when all cached <see cref="DataReader"/>s created by this instance must be recreated
		/// </summary>
		public override event EventHandler DataReaderInvalidated;

		DataStream stream;
		uint length;
		string filename;
		GCHandle gcHandle;
		byte[] dataAry;
		IntPtr data;
		OSType osType;
		long origDataLength;

		MemoryMappedDataReaderFactory(string filename) {
			osType = OSType.Unknown;
			this.filename = filename;
		}

		~MemoryMappedDataReaderFactory() {
			Dispose(false);
		}

		/// <summary>
		/// Creates a data reader
		/// </summary>
		/// <param name="offset">Offset of data</param>
		/// <param name="length">Length of data</param>
		/// <returns></returns>
		public override DataReader CreateReader(uint offset, uint length) => CreateReader(stream, offset, length);

		/// <summary>
		/// Cleans up and frees all allocated memory
		/// </summary>
		public override void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		internal void SetLength(uint length) => this.length = length;

		enum OSType : byte {
			Unknown,
			Windows,
			Unix,
		}

		[Serializable]
		sealed class MemoryMappedIONotSupportedException : IOException {
			public MemoryMappedIONotSupportedException(string s) : base(s) { }
			public MemoryMappedIONotSupportedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
		}

		static class Windows {
			const uint GENERIC_READ = 0x80000000;
			const uint FILE_SHARE_READ = 0x00000001;
			const uint OPEN_EXISTING = 3;
			const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
			const uint PAGE_READONLY = 0x02;
			const uint SEC_IMAGE = 0x1000000;
			const uint SECTION_MAP_READ = 0x0004;
			const uint FILE_MAP_READ = SECTION_MAP_READ;

			[DllImport("kernel32", SetLastError = true, CharSet = CharSet.Auto)]
			static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

			[DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
			static extern SafeFileHandle CreateFileMapping(SafeFileHandle hFile, IntPtr lpAttributes, uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

			[DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
			static extern IntPtr MapViewOfFile(SafeFileHandle hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, UIntPtr dwNumberOfBytesToMap);

			[DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

			[DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
			static extern uint GetFileSize(SafeFileHandle hFile, out uint lpFileSizeHigh);
			const uint INVALID_FILE_SIZE = 0xFFFFFFFF;
			const int NO_ERROR = 0;

			public static void Mmap(MemoryMappedDataReaderFactory creator, bool mapAsImage) {
				using (var fileHandle = CreateFile(creator.filename, GENERIC_READ, FILE_SHARE_READ, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero)) {
					if (fileHandle.IsInvalid)
						throw new IOException($"Could not open file {creator.filename} for reading. Error: {Marshal.GetLastWin32Error():X8}");

					uint sizeLo = GetFileSize(fileHandle, out uint sizeHi);
					int hr;
					if (sizeLo == INVALID_FILE_SIZE && (hr = Marshal.GetLastWin32Error()) != NO_ERROR)
						throw new IOException($"Could not get file size. File: {creator.filename}, error: {hr:X8}");
					var fileSize = ((long)sizeHi << 32) | sizeLo;

					using (var fileMapping = CreateFileMapping(fileHandle, IntPtr.Zero, PAGE_READONLY | (mapAsImage ? SEC_IMAGE : 0), 0, 0, null)) {
						if (fileMapping.IsInvalid)
							throw new MemoryMappedIONotSupportedException($"Could not create a file mapping object. File: {creator.filename}, error: {Marshal.GetLastWin32Error():X8}");
						creator.data = MapViewOfFile(fileMapping, FILE_MAP_READ, 0, 0, UIntPtr.Zero);
						if (creator.data == IntPtr.Zero)
							throw new MemoryMappedIONotSupportedException($"Could not map file {creator.filename}. Error: {Marshal.GetLastWin32Error():X8}");
						creator.length = (uint)fileSize;
						creator.osType = OSType.Windows;
						creator.stream = DataStreamFactory.Create((byte*)creator.data);
					}
				}
			}

			public static void Dispose(IntPtr addr) {
				if (addr != IntPtr.Zero)
					UnmapViewOfFile(addr);
			}
		}

		static class Unix {
			// Can't use SafeFileHandle. Seems like a bug in mono. You'll get
			// "_wapi_handle_unref_full: Attempting to unref unused handle 0xYYY" when Dispose() is called.
			[DllImport("libc")]
			static extern int open(string pathname, int flags);
			const int O_RDONLY = 0;

			[DllImport("libc")]
			static extern int close(int fd);

			[DllImport("libc", EntryPoint = "lseek", SetLastError = true)]
			static extern int lseek32(int fd, int offset, int whence);
			[DllImport("libc", EntryPoint = "lseek", SetLastError = true)]
			static extern long lseek64(int fd, long offset, int whence);
			const int SEEK_END = 2;

			[DllImport("libc", EntryPoint = "mmap", SetLastError = true)]
			static extern IntPtr mmap32(IntPtr addr, IntPtr length, int prot, int flags, int fd, int offset);
			[DllImport("libc", EntryPoint = "mmap", SetLastError = true)]
			static extern IntPtr mmap64(IntPtr addr, IntPtr length, int prot, int flags, int fd, long offset);
			const int PROT_READ = 1;
			const int MAP_PRIVATE = 0x02;

			[DllImport("libc")]
			static extern int munmap(IntPtr addr, IntPtr length);

			public static void Mmap(MemoryMappedDataReaderFactory creator, bool mapAsImage) {
				int fd = open(creator.filename, O_RDONLY);
				try {
					if (fd < 0)
						throw new IOException($"Could not open file {creator.filename} for reading. Error: {fd}");

					long size;
					IntPtr data;

					if (IntPtr.Size == 4) {
						size = lseek32(fd, 0, SEEK_END);
						if (size == -1)
							throw new MemoryMappedIONotSupportedException($"Could not get length of {creator.filename} (lseek failed): {Marshal.GetLastWin32Error()}");

						data = mmap32(IntPtr.Zero, (IntPtr)size, PROT_READ, MAP_PRIVATE, fd, 0);
						if (data == new IntPtr(-1) || data == IntPtr.Zero)
							throw new MemoryMappedIONotSupportedException($"Could not map file {creator.filename}. Error: {Marshal.GetLastWin32Error()}");
					}
					else {
						size = lseek64(fd, 0, SEEK_END);
						if (size == -1)
							throw new MemoryMappedIONotSupportedException($"Could not get length of {creator.filename} (lseek failed): {Marshal.GetLastWin32Error()}");

						data = mmap64(IntPtr.Zero, (IntPtr)size, PROT_READ, MAP_PRIVATE, fd, 0);
						if (data == new IntPtr(-1) || data == IntPtr.Zero)
							throw new MemoryMappedIONotSupportedException($"Could not map file {creator.filename}. Error: {Marshal.GetLastWin32Error()}");
					}

					creator.data = data;
					creator.length = (uint)size;
					creator.origDataLength = size;
					creator.osType = OSType.Unix;
					creator.stream = DataStreamFactory.Create((byte*)creator.data);
				}
				finally {
					if (fd >= 0)
						close(fd);
				}
			}

			public static void Dispose(IntPtr addr, long size) {
				if (addr != IntPtr.Zero)
					munmap(addr, new IntPtr(size));
			}
		}

		static volatile bool canTryWindows = true;
		static volatile bool canTryUnix = true;

		internal static MemoryMappedDataReaderFactory CreateWindows(string filename, bool mapAsImage) {
			if (!canTryWindows)
				return null;

			var creator = new MemoryMappedDataReaderFactory(GetFullPath(filename));
			try {
				Windows.Mmap(creator, mapAsImage);
				return creator;
			}
			catch (EntryPointNotFoundException) {
			}
			catch (DllNotFoundException) {
			}
			canTryWindows = false;
			return null;
		}

		internal static MemoryMappedDataReaderFactory CreateUnix(string filename, bool mapAsImage) {
			if (!canTryUnix)
				return null;

			var creator = new MemoryMappedDataReaderFactory(GetFullPath(filename));
			try {
				Unix.Mmap(creator, mapAsImage);
				if (mapAsImage) { // Only check this if we know that mmap() works, i.e., if above call succeeds
					creator.Dispose();
					throw new ArgumentException("mapAsImage == true is not supported on this OS");
				}
				return creator;
			}
			catch (MemoryMappedIONotSupportedException ex) {
				Debug.WriteLine($"mmap'd IO didn't work: {ex.Message}");
			}
			catch (EntryPointNotFoundException) {
			}
			catch (DllNotFoundException) {
			}
			canTryUnix = false;
			return null;
		}

		static string GetFullPath(string filename) {
			try {
				return Path.GetFullPath(filename);
			}
			catch {
				return filename;
			}
		}

		void Dispose(bool disposing) {
			FreeMemoryMappedIoData();
			if (disposing) {
				length = 0;
				stream = EmptyDataStream.Instance;
				data = IntPtr.Zero;
				filename = null;
			}
		}

		/// <summary>
		/// <c>true</c> if memory mapped I/O is enabled
		/// </summary>
		internal bool IsMemoryMappedIO => dataAry is null;

		/// <summary>
		/// Call this to disable memory mapped I/O. This must only be called if no other code is
		/// trying to access the memory since that could lead to an exception.
		/// </summary>
		internal void UnsafeDisableMemoryMappedIO() {
			if (!(dataAry is null))
				return;
			var newAry = new byte[length];
			Marshal.Copy(data, newAry, 0, newAry.Length);
			FreeMemoryMappedIoData();
			length = (uint)newAry.Length;
			dataAry = newAry;
			gcHandle = GCHandle.Alloc(dataAry, GCHandleType.Pinned);
			data = gcHandle.AddrOfPinnedObject();
			stream = DataStreamFactory.Create((byte*)data);
			DataReaderInvalidated?.Invoke(this, EventArgs.Empty);
		}

		void FreeMemoryMappedIoData() {
			if (dataAry is null) {
				var origData = Interlocked.Exchange(ref data, IntPtr.Zero);
				if (origData != IntPtr.Zero) {
					length = 0;
					switch (osType) {
					case OSType.Windows:
						Windows.Dispose(origData);
						break;

					case OSType.Unix:
						Unix.Dispose(origData, origDataLength);
						break;

					default:
						throw new InvalidOperationException("Shouldn't be here");
					}
				}
			}

			if (gcHandle.IsAllocated) {
				try {
					gcHandle.Free();
				}
				catch (InvalidOperationException) {
				}
			}
			dataAry = null;
		}
	}
}
