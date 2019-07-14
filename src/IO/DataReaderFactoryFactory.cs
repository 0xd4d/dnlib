// dnlib: See LICENSE.txt for more info

using System;
using System.IO;

namespace dnlib.IO {
	static class DataReaderFactoryFactory {
		static readonly bool isUnix;

		static DataReaderFactoryFactory() {
			// See http://mono-project.com/FAQ:_Technical#Mono_Platforms for platform detection.
			int p = (int)Environment.OSVersion.Platform;
			if (p == 4 || p == 6 || p == 128)
				isUnix = true;
		}

		public static DataReaderFactory Create(string fileName, bool mapAsImage) {
			var creator = CreateDataReaderFactory(fileName, mapAsImage);
			if (!(creator is null))
				return creator;

			return ByteArrayDataReaderFactory.Create(File.ReadAllBytes(fileName), fileName);
		}

		static DataReaderFactory CreateDataReaderFactory(string fileName, bool mapAsImage) {
			if (!isUnix)
				return MemoryMappedDataReaderFactory.CreateWindows(fileName, mapAsImage);
			else
				return MemoryMappedDataReaderFactory.CreateUnix(fileName, mapAsImage);
		}
	}
}
