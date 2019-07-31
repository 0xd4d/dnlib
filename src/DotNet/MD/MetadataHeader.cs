// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Text;
using dnlib.IO;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// Represents the .NET metadata header
	/// </summary>
	/// <remarks><c>IMAGE_COR20_HEADER.Metadata</c> points to this header</remarks>
	public sealed class MetadataHeader : FileSection {
		readonly uint signature;
		readonly ushort majorVersion;
		readonly ushort minorVersion;
		readonly uint reserved1;
		readonly uint stringLength;
		readonly string versionString;
		readonly FileOffset offset2ndPart;
		readonly StorageFlags flags;
		readonly byte reserved2;
		readonly ushort streams;
		readonly IList<StreamHeader> streamHeaders;

		/// <summary>
		/// Returns the signature (should be 0x424A5342)
		/// </summary>
		public uint Signature => signature;

		/// <summary>
		/// Returns the major version
		/// </summary>
		public ushort MajorVersion => majorVersion;

		/// <summary>
		/// Returns the minor version
		/// </summary>
		public ushort MinorVersion => minorVersion;

		/// <summary>
		/// Returns the reserved dword (pointer to extra header data)
		/// </summary>
		public uint Reserved1 => reserved1;

		/// <summary>
		/// Returns the version string length value
		/// </summary>
		public uint StringLength => stringLength;

		/// <summary>
		/// Returns the version string
		/// </summary>
		public string VersionString => versionString;

		/// <summary>
		/// Returns the offset of <c>STORAGEHEADER</c>
		/// </summary>
		public FileOffset StorageHeaderOffset => offset2ndPart;

		/// <summary>
		/// Returns the flags (reserved)
		/// </summary>
		public StorageFlags Flags => flags;

		/// <summary>
		/// Returns the reserved byte (padding)
		/// </summary>
		public byte Reserved2 => reserved2;

		/// <summary>
		/// Returns the number of streams
		/// </summary>
		public ushort Streams => streams;

		/// <summary>
		/// Returns all stream headers
		/// </summary>
		public IList<StreamHeader> StreamHeaders => streamHeaders;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">PE file reader pointing to the start of this section</param>
		/// <param name="verify">Verify section</param>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		public MetadataHeader(ref DataReader reader, bool verify)
			: this(ref reader, CLRRuntimeReaderKind.CLR, verify) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">PE file reader pointing to the start of this section</param>
		/// <param name="runtime">Runtime reader kind</param>
		/// <param name="verify">Verify section</param>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		public MetadataHeader(ref DataReader reader, CLRRuntimeReaderKind runtime, bool verify) {
			SetStartOffset(ref reader);
			signature = reader.ReadUInt32();
			if (verify && signature != 0x424A5342)
				throw new BadImageFormatException("Invalid metadata header signature");
			majorVersion = reader.ReadUInt16();
			minorVersion = reader.ReadUInt16();
			reserved1 = reader.ReadUInt32();
			stringLength = reader.ReadUInt32();
			versionString = ReadString(ref reader, stringLength, runtime);
			offset2ndPart = (FileOffset)reader.CurrentOffset;
			flags = (StorageFlags)reader.ReadByte();
			reserved2 = reader.ReadByte();
			streams = reader.ReadUInt16();
			streamHeaders = new StreamHeader[streams];
			for (int i = 0; i < streamHeaders.Count; i++) {
				// Mono doesn't verify all of these so we can't either
				var sh = new StreamHeader(ref reader, throwOnError: false, verify, runtime, out bool failedVerification);
				if (failedVerification || (ulong)sh.Offset + sh.StreamSize > reader.EndOffset)
					sh = new StreamHeader(0, 0, "<invalid>");
				streamHeaders[i] = sh;
			}
			SetEndoffset(ref reader);
		}

		static string ReadString(ref DataReader reader, uint maxLength, CLRRuntimeReaderKind runtime) {
			ulong endOffset = (ulong)reader.CurrentOffset + maxLength;
			if (runtime == CLRRuntimeReaderKind.Mono)
				endOffset = (endOffset + 3) / 4 * 4;
			if (endOffset > reader.EndOffset)
				throw new BadImageFormatException("Invalid MD version string");
			var utf8Bytes = new byte[maxLength];
			uint i;
			for (i = 0; i < maxLength; i++) {
				byte b = reader.ReadByte();
				if (b == 0)
					break;
				utf8Bytes[i] = b;
			}
			reader.CurrentOffset = (uint)endOffset;
			return Encoding.UTF8.GetString(utf8Bytes, 0, (int)i);
		}
	}
}
