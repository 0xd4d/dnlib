// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;
using System.Text;
using dnlib.DotNet.MD;
using dnlib.IO;

namespace dnlib.DotNet.Pdb.Portable {
	struct DocumentNameReader {
		const int MAX_NAME_LENGTH = 64 * 1024;
		readonly Dictionary<uint, string> docNamePartDict;
		readonly BlobStream blobStream;
		readonly StringBuilder sb;

		char[] prevSepChars;
		int prevSepCharsLength;
		byte[] prevSepCharBytes;
		int prevSepCharBytesCount;

		public DocumentNameReader(BlobStream blobStream) {
			docNamePartDict = new Dictionary<uint, string>();
			this.blobStream = blobStream;
			sb = new StringBuilder();

			prevSepChars = new char[2];
			prevSepCharsLength = 0;
			prevSepCharBytes = new byte[3];
			prevSepCharBytesCount = 0;
		}

		public string ReadDocumentName(uint offset) {
			sb.Length = 0;
			if (!blobStream.TryCreateReader(offset, out var reader))
				return string.Empty;
			// A document without a name: offset 0 is the empty blob. It has no separator that could be read.
			if (reader.Length == 0)
				return string.Empty;
			var sepChars = ReadSeparatorChar(ref reader, out int sepCharsLength);
			bool needSep = false;
			while (reader.Position < reader.Length) {
				// The name ends at an offset that is cut off or isn't a valid compressed integer
				if (!reader.TryReadCompressedUInt32(out uint partOffset))
					break;
				if (needSep)
					sb.Append(sepChars, 0, sepCharsLength);
				needSep = !(sepCharsLength == 1 && sepChars[0] == '\0');
				var part = ReadDocumentNamePart(partOffset);
				sb.Append(part);
				if (sb.Length > MAX_NAME_LENGTH) {
					sb.Length = MAX_NAME_LENGTH;
					break;
				}
			}
			return sb.ToString();
		}

		string ReadDocumentNamePart(uint offset) {
			if (docNamePartDict.TryGetValue(offset, out var name))
				return name;
			if (!blobStream.TryCreateReader(offset, out var reader))
				return string.Empty;
			name = reader.ReadUtf8StringKeepUnpairedSurrogates((int)reader.BytesLeft);
			docNamePartDict.Add(offset, name);
			return name;
		}

		char[] ReadSeparatorChar(ref DataReader reader, out int charLength) {
			if (prevSepCharBytesCount != 0 && prevSepCharBytesCount <= reader.Length) {
				var pos = reader.Position;
				bool ok = true;
				for (int i = 0; i < prevSepCharBytesCount; i++) {
					if (i >= prevSepCharBytes.Length || reader.ReadByte() != prevSepCharBytes[i]) {
						ok = false;
						break;
					}
				}
				if (ok) {
					charLength = prevSepCharsLength;
					return prevSepChars;
				}
				reader.Position = pos;
			}

			var decoder = Encoding.UTF8.GetDecoder();
			var bytes = new byte[1];
			prevSepCharBytesCount = 0;
			for (int i = 0; ; i++) {
				if (reader.Position >= reader.Length) {
					// The blob ends in the middle of a char. Don't keep its bytes: they're the start of a valid
					// separator that another name can have.
					prevSepCharBytesCount = 0;
					prevSepChars[0] = '\uFFFD';
					prevSepCharsLength = 1;
					break;
				}
				byte b = reader.ReadByte();
				prevSepCharBytesCount++;
				if (i == 0 && b == 0) {
					// No separator. Don't keep the previous name's separator chars.
					prevSepCharBytes[0] = 0;
					prevSepChars[0] = '\0';
					prevSepCharsLength = 1;
					break;
				}
				if (i < prevSepCharBytes.Length)
					prevSepCharBytes[i] = b;
				bytes[0] = b;
				bool isLastByte = reader.Position + 1 == reader.Length;
				decoder.Convert(bytes, 0, 1, prevSepChars, 0, prevSepChars.Length, isLastByte, out int bytesUsed, out prevSepCharsLength, out bool completed);
				if (prevSepCharsLength > 0)
					break;
			}
			charLength = prevSepCharsLength;
			return prevSepChars;
		}
	}
}
