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
	/// <see cref="Dispose()"/> to free any resources used by the class when it's
	/// no longer needed.</remarks>
	public class MemoryMappedFileStreamCreator : IStreamCreator {
		UnmanagedMemoryStreamCreator otherCreator;
		IntPtr baseAddr;
		string filename;

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

		/// <inheritdoc/>
		public string Filename {
			get { return filename; }
		}

		/// <summary>
		/// Size of the data
		/// </summary>
		public long Length {
			get { return otherCreator.Length; }
			set { otherCreator.Length = value; }
		}

		/// <summary>
		/// Returns the base address of the mapped file
		/// </summary>
		public IntPtr Address {
			get { return baseAddr; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <remarks>If <paramref name="mapAsImage"/> is true, then the created
		/// <see cref="UnmanagedMemoryStreamCreator"/> that is used internally by the class,
		/// can only access bytes up to the file size, not to the end of the mapped image. You must
		/// set <see cref="Length"/> to the correct image length to access the full image.</remarks>
		/// <param name="filename">Name of the file</param>
		/// <param name="mapAsImage">true if we should map it as an executable</param>
		/// <exception cref="IOException">If we can't open/map the file</exception>
		public MemoryMappedFileStreamCreator(string filename, bool mapAsImage) {
			this.filename = filename;
			using (var fileHandle = CreateFile(filename, GENERIC_READ, FILE_SHARE_READ, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero)) {
				if (fileHandle.IsInvalid)
					throw new IOException(string.Format("Could not open file {0} for reading. Error: {1:X8}", filename, Marshal.GetLastWin32Error()));

				uint sizeHi;
				uint sizeLo = GetFileSize(fileHandle, out sizeHi);
				if (sizeLo == INVALID_FILE_SIZE && Marshal.GetLastWin32Error() != NO_ERROR)
					throw new IOException(string.Format("Could not get file size. File: {0}, error: {1:X8}", filename, Marshal.GetLastWin32Error()));
				var fileSize = ((long)sizeHi << 32) | sizeLo;

				using (var fileMapping = CreateFileMapping(fileHandle, IntPtr.Zero, PAGE_READONLY | (mapAsImage ? SEC_IMAGE : 0), 0, 0, null)) {
					if (fileMapping.IsInvalid)
						throw new IOException(string.Format("Could not create a file mapping object. File: {0}, error: {1:X8}", filename, Marshal.GetLastWin32Error()));
					baseAddr = MapViewOfFile(fileMapping, FILE_MAP_READ, 0, 0, UIntPtr.Zero);
					if (baseAddr == IntPtr.Zero)
						throw new IOException(string.Format("Could not map file {0}. Error: {1:X8}", filename, Marshal.GetLastWin32Error()));
					otherCreator = new UnmanagedMemoryStreamCreator(baseAddr, fileSize);
					otherCreator.Filename = filename;
				}
			}
		}

		/// <inheritdoc/>
		~MemoryMappedFileStreamCreator() {
			Dispose(false);
		}

		/// <inheritdoc/>
		public Stream Create(FileOffset offset, long length) {
			return otherCreator.Create(offset, length);
		}

		/// <inheritdoc/>
		public Stream CreateFull() {
			return otherCreator.CreateFull();
		}

		/// <inheritdoc/>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool disposing) {
			otherCreator = null;
			if (baseAddr != IntPtr.Zero) {
				UnmapViewOfFile(baseAddr);
				baseAddr = IntPtr.Zero;
			}
		}
	}
}
