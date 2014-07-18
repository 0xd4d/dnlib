using System;
using System.IO;
using dnlib.DotNet;
using dnlib.PE;
using dnlib.IO;

namespace dnlib.Examples {
	/// <summary>
	/// Dumps all PE sections to disk
	/// </summary>
	public class Example5 {
		public static void Run() {
			string sectionFileName = @"c:\section{0}.bin";

			// Open the current mscorlib
			var mod = ModuleDefMD.Load(typeof(int).Module);

			// Get PE image interface
			var peImage = mod.MetaData.PEImage;

			// Print some info
			Console.WriteLine("Machine: {0}", peImage.ImageNTHeaders.FileHeader.Machine);
			Console.WriteLine("Characteristics: {0}", peImage.ImageNTHeaders.FileHeader.Characteristics);

			Console.WriteLine("Dumping all sections");
			for (int i = 0; i < peImage.ImageSectionHeaders.Count; i++) {
				var section = peImage.ImageSectionHeaders[i];

				// Create a stream for the whole section
				var stream = peImage.CreateStream(section.VirtualAddress, section.SizeOfRawData);

				// Write the data to disk
				var fileName = string.Format(sectionFileName, i);
				Console.WriteLine("Dumping section {0} to file {1}", section.DisplayName, fileName);
				File.WriteAllBytes(fileName, stream.ReadAllBytes());
			}
		}
	}
}
