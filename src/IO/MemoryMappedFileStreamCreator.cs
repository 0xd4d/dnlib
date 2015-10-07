// dnlib: See LICENSE.txt for more info

﻿using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace dnlib.IO {
	/// <summary>
	/// Maps a file into memory using MapViewOfFile() and creates streams
	/// that can access part of it in memory.
	/// </summary>
	/// <remarks>Since this class maps a file into memory, the user should call
	/// <see cref="UnmanagedMemoryStreamCreator.Dispose()"/> to free any resources
	/// used by the class when it's no longer needed.</remarks>
	[DebuggerDisplay("mmap: A:{data} L:{dataLength} {theFileName}")]
	sealed class MemoryMappedFileStreamCreator : UnmanagedMemoryStreamCreator {

		OSType osType = OSType.Unknown;
		long origDataLength;

		enum OSType : byte {
			Unknown,
			Windows,
			Unix,
		}

		[Serializable]
		sealed class MemoryMappedIONotSupportedException : IOException {
			public MemoryMappedIONotSupportedException(string s)
				: base(s) {
			}
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

			public static void Mmap(MemoryMappedFileStreamCreator creator, bool mapAsImage) {
				using (var fileHandle = CreateFile(creator.theFileName, GENERIC_READ, FILE_SHARE_READ, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero)) {
					if (fileHandle.IsInvalid)
						throw new IOException(string.Format("Could not open file {0} for reading. Error: {1:X8}", creator.theFileName, Marshal.GetLastWin32Error()));

					uint sizeHi;
					uint sizeLo = GetFileSize(fileHandle, out sizeHi);
					int hr;
					if (sizeLo == INVALID_FILE_SIZE && (hr = Marshal.GetLastWin32Error()) != NO_ERROR)
						throw new IOException(string.Format("Could not get file size. File: {0}, error: {1:X8}", creator.theFileName, hr));
					var fileSize = ((long)sizeHi << 32) | sizeLo;

					using (var fileMapping = CreateFileMapping(fileHandle, IntPtr.Zero, PAGE_READONLY | (mapAsImage ? SEC_IMAGE : 0), 0, 0, null)) {
						if (fileMapping.IsInvalid)
							throw new MemoryMappedIONotSupportedException(string.Format("Could not create a file mapping object. File: {0}, error: {1:X8}", creator.theFileName, Marshal.GetLastWin32Error()));
						creator.data = MapViewOfFile(fileMapping, FILE_MAP_READ, 0, 0, UIntPtr.Zero);
						if (creator.data == IntPtr.Zero)
							throw new MemoryMappedIONotSupportedException(string.Format("Could not map file {0}. Error: {1:X8}", creator.theFileName, Marshal.GetLastWin32Error()));
						creator.dataLength = fileSize;
						creator.osType = OSType.Windows;
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

			public static void Mmap(MemoryMappedFileStreamCreator creator, bool mapAsImage) {
				int fd = open(creator.theFileName, O_RDONLY);
				try {
					if (fd < 0)
						throw new IOException(string.Format("Could not open file {0} for reading. Error: {1}", creator.theFileName, fd));

					long size;
					IntPtr data;

					if (IntPtr.Size == 4) {
						size = lseek32(fd, 0, SEEK_END);
						if (size == -1)
							throw new MemoryMappedIONotSupportedException(string.Format("Could not get length of {0} (lseek failed): {1}", creator.theFileName, Marshal.GetLastWin32Error()));

						data = mmap32(IntPtr.Zero, (IntPtr)size, PROT_READ, MAP_PRIVATE, fd, 0);
						if (data == new IntPtr(-1) || data == IntPtr.Zero)
							throw new MemoryMappedIONotSupportedException(string.Format("Could not map file {0}. Error: {1}", creator.theFileName, Marshal.GetLastWin32Error()));
					}
					else {
						size = lseek64(fd, 0, SEEK_END);
						if (size == -1)
							throw new MemoryMappedIONotSupportedException(string.Format("Could not get length of {0} (lseek failed): {1}", creator.theFileName, Marshal.GetLastWin32Error()));

						data = mmap64(IntPtr.Zero, (IntPtr)size, PROT_READ, MAP_PRIVATE, fd, 0);
						if (data == new IntPtr(-1) || data == IntPtr.Zero)
							throw new MemoryMappedIONotSupportedException(string.Format("Could not map file {0}. Error: {1}", creator.theFileName, Marshal.GetLastWin32Error()));
					}

					creator.data = data;
					creator.dataLength = size;
					creator.origDataLength = creator.dataLength;
					creator.osType = OSType.Unix;
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

		static bool canTryWindows = true;
		static bool canTryUnix = true;

		/// <summary>
		/// Creates a new <see cref="MemoryMappedFileStreamCreator"/> if supported or returns
		/// <c>null</c> if the OS functions aren't supported.
		/// </summary>
		/// <remarks>If <paramref name="mapAsImage"/> is <c>true</c>, then the created
		/// <see cref="UnmanagedMemoryStreamCreator"/> that is used internally by the class,
		/// can only access bytes up to the file size, not to the end of the mapped image. You must
		/// set <see cref="UnmanagedMemoryStreamCreator.Length"/> to the correct image length to access the full image.</remarks>
		/// <param name="fileName">Name of the file</param>
		/// <param name="mapAsImage"><c>true</c> if we should map it as an executable</param>
		/// <exception cref="IOException">If we can't open/map the file</exception>
		internal static MemoryMappedFileStreamCreator CreateWindows(string fileName, bool mapAsImage) {
			if (!canTryWindows)
				return null;

			var creator = new MemoryMappedFileStreamCreator();
			creator.theFileName = GetFullPath(fileName);
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

		/// <summary>
		/// Creates a new <see cref="MemoryMappedFileStreamCreator"/> if supported or returns
		/// <c>null</c> if the OS functions aren't supported.
		/// </summary>
		/// <remarks>If <paramref name="mapAsImage"/> is <c>true</c>, then the created
		/// <see cref="UnmanagedMemoryStreamCreator"/> that is used internally by the class,
		/// can only access bytes up to the file size, not to the end of the mapped image. You must
		/// set <see cref="UnmanagedMemoryStreamCreator.Length"/> to the correct image length to access the full image.</remarks>
		/// <param name="fileName">Name of the file</param>
		/// <param name="mapAsImage">NOT SUPPORTED. <c>true</c> if we should map it as an executable</param>
		/// <exception cref="IOException">If we can't open/map the file</exception>
		internal static MemoryMappedFileStreamCreator CreateUnix(string fileName, bool mapAsImage) {
			if (!canTryUnix)
				return null;

			var creator = new MemoryMappedFileStreamCreator();
			creator.theFileName = GetFullPath(fileName);
			try {
				Unix.Mmap(creator, mapAsImage);
				if (mapAsImage) { // Only check this if we know that mmap() works, i.e., if above call succeeds
					creator.Dispose();
					throw new ArgumentException("mapAsImage == true is not supported on this OS");
				}
				return creator;
			}
			catch (MemoryMappedIONotSupportedException ex) {
				Debug.WriteLine(string.Format("mmap'd IO didn't work: {0}", ex.Message));
			}
			catch (EntryPointNotFoundException) {
			}
			catch (DllNotFoundException) {
			}
			canTryUnix = false;
			return null;
		}

		static string GetFullPath(string fileName) {
			try {
				return Path.GetFullPath(fileName);
			}
			catch {
				return fileName;
			}
		}

		/// <inheritdoc/>
		~MemoryMappedFileStreamCreator() {
			Dispose(false);
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing) {
			FreeMemoryMappedIoData();
			base.Dispose(disposing);
		}

		/// <summary>
		/// <c>true</c> if memory mapped I/O is enabled
		/// </summary>
		public bool IsMemoryMappedIO {
			get { return dataAry == null; }
		}

		/// <summary>
		/// Call this to disable memory mapped I/O. This must only be called if no other code is
		/// trying to access the memory since that could lead to an exception.
		/// </summary>
		public void UnsafeDisableMemoryMappedIO() {
			if (dataAry != null)
				return;
			if (unsafeUseAddress)
				throw new InvalidOperationException("Can't convert to non-memory mapped I/O because the PDB reader uses the address. Use the managed PDB reader instead.");
			var newAry = new byte[Length];
			Marshal.Copy(data, newAry, 0, newAry.Length);
			FreeMemoryMappedIoData();
			dataLength = newAry.Length;
			dataAry = newAry;
			gcHandle = GCHandle.Alloc(dataAry, GCHandleType.Pinned);
			this.data = gcHandle.AddrOfPinnedObject();
		}
		GCHandle gcHandle;
		byte[] dataAry;

		void FreeMemoryMappedIoData() {
			if (dataAry == null) {
				var origData = Interlocked.Exchange(ref data, IntPtr.Zero);
				if (origData != IntPtr.Zero) {
					dataLength = 0;
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
