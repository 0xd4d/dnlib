using System;
using System.IO;
using System.Text;
using dot10.IO;

namespace dot10.dotNET {
	/// <summary>
	/// A metadata stream header
	/// </summary>
	class StreamHeader : FileSection {
		uint offset;
		uint streamSize;
		string name;

		/// <summary>
		/// The offset of the stream relative to the start of the MetaData header
		/// </summary>
		public uint Offset {
			get { return offset; }
		}

		/// <summary>
		/// The size of the stream
		/// </summary>
		public uint StreamSize {
			get { return streamSize; }
		}

		/// <summary>
		/// The name of the stream
		/// </summary>
		public string Name {
			get { return name; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reader">PE file reader pointing to the start of this section</param>
		/// <param name="verify">Verify section</param>
		/// <exception cref="BadImageFormatException">Thrown if verification fails</exception>
		public StreamHeader(IImageStream reader, bool verify) {
			SetStartOffset(reader);
			this.offset = reader.ReadUInt32();
			this.streamSize = reader.ReadUInt32();
			this.name = ReadString(reader, 32, verify);
			SetEndoffset(reader);
			if (verify && offset + size < offset)
				throw new BadImageFormatException("Invalid stream header");
		}

		static string ReadString(IImageStream reader, int maxLen, bool verify) {
			var origPos = reader.Position;
			var sb = new StringBuilder(maxLen);
			int i;
			for (i = 0; i < maxLen; i++) {
				byte b = reader.ReadByte();
				if (b == 0)
					break;
				sb.Append((char)b);
			}
			if (verify && i == maxLen)
				throw new BadImageFormatException("Invalid stream name string");
			if (i != maxLen)
				reader.Position = origPos + ((i + 1 + 3) & ~3);
			return sb.ToString();
		}

		/// <inheritdoc/>
		public override string ToString() {
			return string.Format("O:{0:X8} L:{1:X8} {2}", offset, streamSize, name);
		}
	}
}
