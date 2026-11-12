// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics;
using System.IO;
#if !NET35
using System.IO.MemoryMappedFiles;
#endif
using System.Runtime.InteropServices;
#if NET35
using System.Runtime.Serialization;
#endif
using System.Threading;
#if NET35
using Microsoft.Win32.SafeHandles;
#endif

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
#if NET35
		OSType osType;
		long origDataLength;
#else
		MemoryMappedViewAccessor viewAccessor;
#endif

		MemoryMappedDataReaderFactory(string filename) {
#if NET35
			osType = OSType.Unknown;
#endif
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

#if NET35
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

			public static void Mmap(MemoryMappedDataReaderFactory creator) {
				using (var fileHandle = CreateFile(creator.filename, GENERIC_READ, FILE_SHARE_READ, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero)) {
					if (fileHandle.IsInvalid) {
						// The caller reads the file with File.ReadAllBytes() which throws the correct exception (eg. FileNotFoundException)
						throw new MemoryMappedIONotSupportedException($"Could not open file {creator.filename} for reading. Error: {Marshal.GetLastWin32Error():X8}");
					}

					uint sizeLo = GetFileSize(fileHandle, out uint sizeHi);
					int hr;
					if (sizeLo == INVALID_FILE_SIZE && (hr = Marshal.GetLastWin32Error()) != NO_ERROR)
						throw new MemoryMappedIONotSupportedException($"Could not get file size. File: {creator.filename}, error: {hr:X8}");
					var fileSize = ((long)sizeHi << 32) | sizeLo;

					using (var fileMapping = CreateFileMapping(fileHandle, IntPtr.Zero, PAGE_READONLY, 0, 0, null)) {
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
			[DllImport("libc", SetLastError = true)]
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

			public static void Mmap(MemoryMappedDataReaderFactory creator) {
				int fd = open(creator.filename, O_RDONLY);
				try {
					if (fd < 0) {
						// The caller reads the file with File.ReadAllBytes() which throws the correct exception (eg. FileNotFoundException)
						throw new MemoryMappedIONotSupportedException($"Could not open file {creator.filename} for reading. Error: {Marshal.GetLastWin32Error()}");
					}

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

		internal static MemoryMappedDataReaderFactory CreateWindows(string filename) {
			if (!canTryWindows)
				return null;

			var creator = new MemoryMappedDataReaderFactory(GetFullPath(filename));
			try {
				Windows.Mmap(creator);
				return creator;
			}
			catch (MemoryMappedIONotSupportedException ex) {
				// Only this file can't be mapped (eg. it's empty), so don't disable it for other files
				Debug.WriteLine($"Memory mapped I/O didn't work: {ex.Message}");
				return null;
			}
			catch (EntryPointNotFoundException) {
			}
			catch (DllNotFoundException) {
			}
			canTryWindows = false;
			return null;
		}

		internal static MemoryMappedDataReaderFactory CreateUnix(string filename) {
			if (!canTryUnix)
				return null;

			var creator = new MemoryMappedDataReaderFactory(GetFullPath(filename));
			try {
				Unix.Mmap(creator);
				return creator;
			}
			catch (MemoryMappedIONotSupportedException ex) {
				// Only this file can't be mapped (eg. it's empty), so don't disable it for other files
				Debug.WriteLine($"mmap'd IO didn't work: {ex.Message}");
				return null;
			}
			catch (EntryPointNotFoundException) {
			}
			catch (DllNotFoundException) {
			}
			canTryUnix = false;
			return null;
		}
#else
		/// <summary>
		/// Maps the file with <see cref="MemoryMappedFile"/>. Returns <c>null</c> if the file can't be
		/// mapped (eg. it's empty), in which case the caller should read it into memory instead.
		/// Exceptions from opening the file are not caught.
		/// </summary>
		internal static MemoryMappedDataReaderFactory Create(string filename) {
			var fullPath = GetFullPath(filename);
			var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1, FileOptions.None);
			MemoryMappedFile mmf = null;
			MemoryMappedViewAccessor viewAccessor = null;
			bool pointerAcquired = false;
			MemoryMappedDataReaderFactory creator = null;
			try {
				long size = fileStream.Length;
				// MemoryMappedFile can't map empty files
				if (size == 0)
					return null;
				// leaveOpen: true, fileStream is closed below once the view has been mapped (also on error)
				// .NET Framework 4.5 only has the overload with a MemoryMappedFileSecurity (null = default
				// security) parameter. The overload without it exists in .NET Framework 4.6.1+ and .NET Standard 2.0.
#if NETFRAMEWORK
				mmf = MemoryMappedFile.CreateFromFile(fileStream, null, 0, MemoryMappedFileAccess.Read, null, HandleInheritability.None, true);
#else
				mmf = MemoryMappedFile.CreateFromFile(fileStream, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
#endif
				viewAccessor = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
				byte* p = null;
				viewAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref p);
				pointerAcquired = true;
				// The view starts at offset 0 (always page aligned) so p points to the first byte of the
				// file (PointerOffset is always 0 and doesn't exist in .NET Framework 4.5)
				// The view stays valid after the file and the mapping are closed. Close them now like the old
				// OS API code did: the mapping keeps the file open and on Unix, .NET locks open files with
				// flock(), so other .NET code couldn't overwrite the file while the module is loaded.
				mmf.Dispose();
				fileStream.Dispose();
				creator = new MemoryMappedDataReaderFactory(fullPath) {
					viewAccessor = viewAccessor,
					data = (IntPtr)p,
					length = (uint)size,
				};
				creator.stream = DataStreamFactory.Create(p);
				return creator;
			}
			catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is UnauthorizedAccessException || ex is NotSupportedException) {
				// Only this file can't be mapped, the caller reads it into memory instead
				Debug.WriteLine($"Memory mapped I/O didn't work: {ex.Message}");
				return null;
			}
			finally {
				if (creator is null) {
					if (pointerAcquired)
						viewAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
					viewAccessor?.Dispose();
					mmf?.Dispose();
					fileStream.Dispose();
				}
			}
		}
#endif

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
			if (dataAry is not null)
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
#if NET35
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
#else
					var origViewAccessor = Interlocked.Exchange(ref viewAccessor, null);
					if (origViewAccessor is not null) {
						origViewAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
						origViewAccessor.Dispose();
					}
#endif
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
