// dnlib: See LICENSE.txt for more info

using System;
using System.IO;

namespace dnlib.IO {
	static class DataReaderFactoryFactory {
#if NET35
		static readonly bool isUnix;

		static DataReaderFactoryFactory() {
			// See http://mono-project.com/FAQ:_Technical#Mono_Platforms for platform detection.
			int p = (int)Environment.OSVersion.Platform;
			if (p == 4 || p == 6 || p == 128)
				isUnix = true;
		}
#endif

		public static DataReaderFactory Create(string fileName) {
			var creator = CreateDataReaderFactory(fileName);
			if (creator is not null)
				return creator;

			return ByteArrayDataReaderFactory.Create(File.ReadAllBytes(fileName), fileName);
		}

		static DataReaderFactory CreateDataReaderFactory(string fileName) {
#if NET35
			if (!isUnix)
				return MemoryMappedDataReaderFactory.CreateWindows(fileName);
			else
				return MemoryMappedDataReaderFactory.CreateUnix(fileName);
#else
			return MemoryMappedDataReaderFactory.Create(fileName);
#endif
		}
	}
}
