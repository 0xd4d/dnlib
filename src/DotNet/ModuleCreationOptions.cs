// dnlib: See LICENSE.txt for more info

using dnlib.IO;
using dnlib.DotNet.Pdb;
using dnlib.DotNet.Pdb.Symbols;
using System;

namespace dnlib.DotNet {
	/// <summary>
	/// <see cref="ModuleDefMD"/> creation options
	/// </summary>
	public sealed class ModuleCreationOptions {
		internal static readonly ModuleCreationOptions Default = new ModuleCreationOptions();

		/// <summary>
		/// Module context
		/// </summary>
		public ModuleContext Context { get; set; }

		/// <summary>
		/// Set this if you want to decide how to create the PDB symbol reader. You don't need to
		/// initialize <see cref="PdbFileOrData"/> or <see cref="TryToLoadPdbFromDisk"/>.
		/// </summary>
		public Func<ModuleDefMD, SymbolReader> CreateSymbolReader { get; set; }

		/// <summary>
		/// Which PDB reader to use. Default is <see cref="PdbImplType.Default"/>.
		/// </summary>
		public PdbImplType PdbImplementation { get; set; }

		/// <summary>
		/// Set it to A) the path (string) of the PDB file, B) the data (byte[]) of the PDB file or
		/// C) to an <see cref="DataReaderFactory"/> of the PDB data. The <see cref="DataReaderFactory"/> will
		/// be owned by the module. You don't need to initialize <see cref="TryToLoadPdbFromDisk"/>
		/// or <see cref="CreateSymbolReader"/>
		/// </summary>
		public object PdbFileOrData { get; set; }

		/// <summary>
		/// If <c>true</c>, will load the PDB file from disk if present, or an embedded portable PDB file
		/// stored in the PE file. The default value is <c>true</c>.
		/// You don't need to initialize <see cref="CreateSymbolReader"/> or <see cref="PdbFileOrData"/>.
		/// </summary>
		public bool TryToLoadPdbFromDisk { get; set; }

		/// <summary>
		/// corlib assembly reference to use or <c>null</c> if the default one from the opened
		/// module should be used.
		/// </summary>
		public AssemblyRef CorLibAssemblyRef { get; set; }

		/// <summary>
		/// Default constructor
		/// </summary>
		public ModuleCreationOptions() {
			PdbImplementation = PdbImplType.Default;
			TryToLoadPdbFromDisk = true;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="context">Module context</param>
		public ModuleCreationOptions(ModuleContext context) {
			Context = context;
			PdbImplementation = PdbImplType.Default;
			TryToLoadPdbFromDisk = true;
		}
	}
}
