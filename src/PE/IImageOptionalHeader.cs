/*
    Copyright (C) 2012-2013 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

﻿using dnlib.IO;

namespace dnlib.PE {
	/// <summary>
	/// Interface for PE optional header classes
	/// </summary>
	public interface IImageOptionalHeader : IFileSection {
		/// <summary>
		/// Returns the Magic field
		/// </summary>
		ushort Magic { get; }

		/// <summary>
		/// Returns the MajorLinkerVersion field
		/// </summary>
		byte MajorLinkerVersion { get; }

		/// <summary>
		/// Returns the MinorLinkerVersion field
		/// </summary>
		byte MinorLinkerVersion { get; }

		/// <summary>
		/// Returns the SizeOfCode field
		/// </summary>
		uint SizeOfCode { get; }

		/// <summary>
		/// Returns the SizeOfInitializedData field
		/// </summary>
		uint SizeOfInitializedData { get; }

		/// <summary>
		/// Returns the SizeOfUninitializedData field
		/// </summary>
		uint SizeOfUninitializedData { get; }

		/// <summary>
		/// Returns the AddressOfEntryPoint field
		/// </summary>
		RVA AddressOfEntryPoint { get; }

		/// <summary>
		/// Returns the BaseOfCode field
		/// </summary>
		RVA BaseOfCode { get; }

		/// <summary>
		/// Returns the BaseOfData field
		/// </summary>
		RVA BaseOfData { get; }

		/// <summary>
		/// Returns the ImageBase field
		/// </summary>
		ulong ImageBase { get; }

		/// <summary>
		/// Returns the SectionAlignment field
		/// </summary>
		uint SectionAlignment { get; }

		/// <summary>
		/// Returns the FileAlignment field
		/// </summary>
		uint FileAlignment { get; }

		/// <summary>
		/// Returns the MajorOperatingSystemVersion field
		/// </summary>
		ushort MajorOperatingSystemVersion { get; }

		/// <summary>
		/// Returns the MinorOperatingSystemVersion field
		/// </summary>
		ushort MinorOperatingSystemVersion { get; }

		/// <summary>
		/// Returns the MajorImageVersion field
		/// </summary>
		ushort MajorImageVersion { get; }

		/// <summary>
		/// Returns the MinorImageVersion field
		/// </summary>
		ushort MinorImageVersion { get; }

		/// <summary>
		/// Returns the MajorSubsystemVersion field
		/// </summary>
		ushort MajorSubsystemVersion { get; }

		/// <summary>
		/// Returns the MinorSubsystemVersion field
		/// </summary>
		ushort MinorSubsystemVersion { get; }

		/// <summary>
		/// Returns the Win32VersionValue field
		/// </summary>
		uint Win32VersionValue { get; }

		/// <summary>
		/// Returns the SizeOfImage field
		/// </summary>
		uint SizeOfImage { get; }

		/// <summary>
		/// Returns the SizeOfHeaders field
		/// </summary>
		uint SizeOfHeaders { get; }

		/// <summary>
		/// Returns the CheckSum field
		/// </summary>
		uint CheckSum { get; }

		/// <summary>
		/// Returns the Subsystem field
		/// </summary>
		Subsystem Subsystem { get; }

		/// <summary>
		/// Returns the DllCharacteristics field
		/// </summary>
		DllCharacteristics DllCharacteristics { get; }

		/// <summary>
		/// Returns the SizeOfStackReserve field
		/// </summary>
		ulong SizeOfStackReserve { get; }

		/// <summary>
		/// Returns the SizeOfStackCommit field
		/// </summary>
		ulong SizeOfStackCommit { get; }

		/// <summary>
		/// Returns the SizeOfHeapReserve field
		/// </summary>
		ulong SizeOfHeapReserve { get; }

		/// <summary>
		/// Returns the SizeOfHeapCommit field
		/// </summary>
		ulong SizeOfHeapCommit { get; }

		/// <summary>
		/// Returns the LoaderFlags field
		/// </summary>
		uint LoaderFlags { get; }

		/// <summary>
		/// Returns the NumberOfRvaAndSizes field
		/// </summary>
		uint NumberOfRvaAndSizes { get; }

		/// <summary>
		/// Returns the DataDirectories field
		/// </summary>
		ImageDataDirectory[] DataDirectories { get; }
	}
}
