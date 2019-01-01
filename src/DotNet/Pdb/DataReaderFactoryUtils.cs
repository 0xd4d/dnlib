// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Security;
using dnlib.IO;

namespace dnlib.DotNet.Pdb {
	static class DataReaderFactoryUtils {
		public static DataReaderFactory TryCreateDataReaderFactory(string filename) {
			try {
				if (!File.Exists(filename))
					return null;
				// Don't use memory mapped I/O
				return ByteArrayDataReaderFactory.Create(File.ReadAllBytes(filename), filename);
			}
			catch (IOException) {
			}
			catch (UnauthorizedAccessException) {
			}
			catch (SecurityException) {
			}
			return null;
		}
	}
}
