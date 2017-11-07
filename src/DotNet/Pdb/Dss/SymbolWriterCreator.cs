// dnlib: See LICENSE.txt for more info

using System;
using System.IO;
using dnlib.DotNet.Pdb.WindowsPdb;

namespace dnlib.DotNet.Pdb.Dss {
	/// <summary>
	/// Creates a <see cref="ISymbolWriter2"/>
	/// </summary>
	static class SymbolWriterCreator {
		static readonly Guid CLSID_CorSymWriter_SxS = new Guid(0x0AE2DEB0, 0xF901, 0x478B, 0xBB, 0x9F, 0x88, 0x1E, 0xE8, 0x06, 0x67, 0x88);

		/// <summary>
		/// Creates a <see cref="ISymUnmanagedWriter2"/> instance
		/// </summary>
		/// <returns>A new <see cref="ISymUnmanagedWriter2"/> instance</returns>
		public static ISymUnmanagedWriter2 CreateSymUnmanagedWriter2() {
			return (ISymUnmanagedWriter2)Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_CorSymWriter_SxS));
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolWriter2"/> instance
		/// </summary>
		/// <param name="pdbFileName">PDB file name</param>
		/// <returns>A new <see cref="ISymbolWriter2"/> instance</returns>
		public static ISymbolWriter2 Create(string pdbFileName) {
			if (File.Exists(pdbFileName))
				File.Delete(pdbFileName);
			return new SymbolWriter(CreateSymUnmanagedWriter2(), pdbFileName);
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolWriter2"/> instance
		/// </summary>
		/// <param name="pdbStream">PDB output stream</param>
		/// <param name="pdbFileName">PDB file name</param>
		/// <returns>A new <see cref="ISymbolWriter2"/> instance</returns>
		public static ISymbolWriter2 Create(Stream pdbStream, string pdbFileName) {
			return new SymbolWriter(CreateSymUnmanagedWriter2(), pdbFileName, pdbStream);
		}
	}
}
