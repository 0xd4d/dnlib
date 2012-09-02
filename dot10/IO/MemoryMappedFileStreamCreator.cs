using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace dot10.IO {
	/// <summary>
	/// Maps a file into memory using MapViewOfFile() and creates streams
	/// that can access part of it in memory.
	/// </summary>
	/// <remarks>Since this class maps a file into memory, the user should call
	/// <see cref="UnmanagedMemoryStreamCreator.Dispose()"/> to free any resources
	/// used by the class when it's no longer needed.</remarks>
	class MemoryMappedFileStreamCreator : UnmanagedMemoryStreamCreator {
		const uint GENERIC_READ = 0x80000000;
		const uint FILE_SHARE_READ = 0x00000001;
		const uint OPEN_EXISTING = 3;
		const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
		const uint PAGE_READONLY = 0x02;
		const uint SEC_IMAGE = 0x1000000;
		const uint SECTION_MAP_READ = 0x0004;
		const uint FILE_MAP_READ = SECTION_MAP_READ;

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
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

		/// <summary>
		/// Constructor
		/// </summary>
		/// <remarks>If <paramref name="mapAsImage"/> is true, then the created
		/// <see cref="UnmanagedMemoryStreamCreator"/> that is used internally by the class,
		/// can only access bytes up to the file size, not to the end of the mapped image. You must
		/// set <see cref="UnmanagedMemoryStreamCreator.Length"/> to the correct image length to access the full image.</remarks>
		/// <param name="fileName">Name of the file</param>
		/// <param name="mapAsImage">true if we should map it as an executable</param>
		/// <exception cref="IOException">If we can't open/map the file</exception>
		public MemoryMappedFileStreamCreator(string fileName, bool mapAsImage) {
			this.theFileName = fileName;
			using (var fileHandle = CreateFile(fileName, GENERIC_READ, FILE_SHARE_READ, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero)) {
				if (fileHandle.IsInvalid)
					throw new IOException(string.Format("Could not open file {0} for reading. Error: {1:X8}", fileName, Marshal.GetLastWin32Error()));

				uint sizeHi;
				uint sizeLo = GetFileSize(fileHandle, out sizeHi);
				int hr;
				if (sizeLo == INVALID_FILE_SIZE && (hr = Marshal.GetLastWin32Error()) != NO_ERROR)
					throw new IOException(string.Format("Could not get file size. File: {0}, error: {1:X8}", fileName, hr));
				var fileSize = ((long)sizeHi << 32) | sizeLo;

				using (var fileMapping = CreateFileMapping(fileHandle, IntPtr.Zero, PAGE_READONLY | (mapAsImage ? SEC_IMAGE : 0), 0, 0, null)) {
					if (fileMapping.IsInvalid)
						throw new IOException(string.Format("Could not create a file mapping object. File: {0}, error: {1:X8}", fileName, Marshal.GetLastWin32Error()));
					this.data = MapViewOfFile(fileMapping, FILE_MAP_READ, 0, 0, UIntPtr.Zero);
					if (this.data == IntPtr.Zero)
						throw new IOException(string.Format("Could not map file {0}. Error: {1:X8}", fileName, Marshal.GetLastWin32Error()));
					this.theFileName = fileName;
					this.dataLength = fileSize;
				}
			}
		}

		/// <inheritdoc/>
		~MemoryMappedFileStreamCreator() {
			Dispose(false);
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing) {
			if (data != IntPtr.Zero) {
				UnmapViewOfFile(data);
				data = IntPtr.Zero;
				dataLength = 0;
			}
			base.Dispose(disposing);
		}

		/// <inheritdoc/>
		public override string ToString() {
			return string.Format("mmap: A:{0:X8} L:{1:X8} {2}", data.ToInt64(), dataLength, theFileName);
		}
	}
}
