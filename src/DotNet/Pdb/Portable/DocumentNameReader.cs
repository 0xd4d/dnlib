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
			using (var stream = blobStream.CreateStream(offset)) {
				int sepCharsLength;
				var sepChars = ReadSeparatorChar(stream, out sepCharsLength);
				bool needSep = false;
				while (stream.Position < stream.Length) {
					if (needSep)
						sb.Append(sepChars, 0, sepCharsLength);
					needSep = !(sepCharsLength == 1 && sepChars[0] == '\0');
					var part = ReadDocumentNamePart(stream.ReadCompressedUInt32());
					sb.Append(part);
					if (sb.Length > MAX_NAME_LENGTH) {
						sb.Length = MAX_NAME_LENGTH;
						break;
					}
				}
			}
			return sb.ToString();
		}

		string ReadDocumentNamePart(uint offset) {
			string name;
			if (docNamePartDict.TryGetValue(offset, out name))
				return name;
			var data = blobStream.ReadNoNull(offset);
			name = Encoding.UTF8.GetString(data);
			docNamePartDict.Add(offset, name);
			return name;
		}

		char[] ReadSeparatorChar(IImageStream stream, out int charLength) {
			if (prevSepCharBytesCount != 0 && prevSepCharBytesCount <= stream.Length) {
				var pos = stream.Position;
				bool ok = true;
				for (int i = 0; i < prevSepCharBytesCount; i++) {
					if (i >= prevSepCharBytes.Length || stream.ReadByte() != prevSepCharBytes[i]) {
						ok = false;
						break;
					}
				}
				if (ok) {
					charLength = prevSepCharsLength;
					return prevSepChars;
				}
				stream.Position = pos;
			}

			var decoder = Encoding.UTF8.GetDecoder();
			var bytes = new byte[1];
			prevSepCharBytesCount = 0;
			for (int i = 0; ; i++) {
				byte b = stream.ReadByte();
				prevSepCharBytesCount++;
				if (i == 0 && b == 0)
					break;
				if (i < prevSepCharBytes.Length)
					prevSepCharBytes[i] = b;
				bytes[0] = b;
				bool isLastByte = stream.Position + 1 == stream.Length;
				int bytesUsed;
				bool completed;
				decoder.Convert(bytes, 0, 1, prevSepChars, 0, prevSepChars.Length, isLastByte, out bytesUsed, out prevSepCharsLength, out completed);
				if (prevSepCharsLength > 0)
					break;
			}
			charLength = prevSepCharsLength;
			return prevSepChars;
		}
	}
}
