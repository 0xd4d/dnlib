// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using System.Security;
using dnlib.IO;

namespace dnlib.DotNet.Pdb {
	static class ImageStreamUtils {
		public static IImageStream OpenImageStream(string fileName) {
			try {
				if (!File.Exists(fileName))
					return null;
				return ImageStreamCreator.CreateImageStream(fileName);
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
