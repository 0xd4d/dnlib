// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.Text;
using dnlib.IO;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// Represents the .NET metadata header
	/// </summary>
	/// <remarks><c>IMAGE_COR20_HEADER.MetaData</c> points to this header</remarks>
	public sealed class MetaDataHeader : FileSection {
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
		public uint Signature {
			get { return signature; }
		}

		/// <summary>
		/// Returns the major version
		/// </summary>
		public ushort MajorVersion {
			get { return majorVersion; }
		}

		/// <summary>
		/// Returns the minor version
		/// </summary>
		public ushort MinorVersion {
			get { return minorVersion; }
		}

		/// <summary>
		/// Returns the reserved dword (pointer to extra header data)
		/// </summary>
		public uint Reserved1 {
			get { return reserved1; }
		}

		/// <summary>
		/// Returns the version string length value
		/// </summary>
		public uint StringLength {
			get { return stringLength; }
		}

		/// <summary>
		/// Returns the version string
		/// </summary>
		public string VersionString {
			get { return versionString; }
		}

		/// <summary>
		/// Returns the offset of <c>STORAGEHEADER</c>
		/// </summary>
		public FileOffset StorageHeaderOffset {
			get { return offset2ndPart; }
		}

		/// <summary>
		/// Returns the flags (reserved)
		/// </summary>
		public StorageFlags Flags {
			get { return flags; }
		}

		/// <summary>
		/// Returns the reserved byte (padding)
		/// </summary>
		public byte Reserved2 {
			get { return reserved2; }
		}

		/// <summary>
		/// Returns the number of streams
		/// </summary>
		public ushort Streams {
			get { return streams; }
		}

		/// <summary>
		/// Returns all stream headers
		/// </summary>
		public IList<StreamHeader> StreamHeaders {
			get { return streamHeaders; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">PE file reader pointing to the start of this section</param>
		/// <param name="verify">Verify section</param>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		public MetaDataHeader(IImageStream reader, bool verify) {
			SetStartOffset(reader);
			this.signature = reader.ReadUInt32();
			if (verify && this.signature != 0x424A5342)
				throw new BadImageFormatException("Invalid MetaData header signature");
			this.majorVersion = reader.ReadUInt16();
			this.minorVersion = reader.ReadUInt16();
			if (verify && !((majorVersion == 1 && minorVersion == 1) || (majorVersion == 0 && minorVersion >= 19)))
				throw new BadImageFormatException(string.Format("Unknown MetaData header version: {0}.{1}", majorVersion, minorVersion));
			this.reserved1 = reader.ReadUInt32();
			this.stringLength = reader.ReadUInt32();
			this.versionString = ReadString(reader, stringLength);
			this.offset2ndPart = reader.FileOffset + reader.Position;
			this.flags = (StorageFlags)reader.ReadByte();
			this.reserved2 = reader.ReadByte();
			this.streams = reader.ReadUInt16();
			this.streamHeaders = new StreamHeader[streams];
			for (int i = 0; i < streamHeaders.Count; i++)
				streamHeaders[i] = new StreamHeader(reader, verify);
			SetEndoffset(reader);
		}

		static string ReadString(IImageStream reader, uint maxLength) {
			long endPos = reader.Position + maxLength;
			if (endPos < reader.Position || endPos > reader.Length)
				throw new BadImageFormatException("Invalid MD version string");
			byte[] utf8Bytes = new byte[maxLength];
			uint i;
			for (i = 0; i < maxLength; i++) {
				byte b = reader.ReadByte();
				if (b == 0)
					break;
				utf8Bytes[i] = b;
			}
			reader.Position = endPos;
			return Encoding.UTF8.GetString(utf8Bytes, 0, (int)i);
		}
	}
}
