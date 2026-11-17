// dnlib: See LICENSE.txt for more info

using System;
using dnlib.IO;

namespace dnlib.DotNet.Pdb.Managed {
	sealed class MsfStream {
		public MsfStream(DataReader[] pages, uint length) {
			var buf = new byte[length];
			int offset = 0;
			for (int i = 0; i < pages.Length; i++) {
				var page = pages[i];
				page.Position = 0;
				int len = Math.Min((int)page.Length, (int)(length - offset));
				page.ReadBytes(buf, offset, len);
				offset += len;
			}
			Content = ByteArrayDataReaderFactory.CreateReader(buf);
		}

		public DataReader Content;
	}
}
