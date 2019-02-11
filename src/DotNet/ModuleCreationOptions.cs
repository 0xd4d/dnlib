// dnlib: See LICENSE.txt for more info

using dnlib.IO;
using dnlib.DotNet.Pdb;

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

		internal const PdbReaderOptions DefaultPdbReaderOptions = PdbReaderOptions.None;

		/// <summary>
		/// PDB reader options
		/// </summary>
		public PdbReaderOptions PdbOptions { get; set; } = DefaultPdbReaderOptions;

		/// <summary>
		/// Set it to A) the path (string) of the PDB file, B) the data (byte[]) of the PDB file or
		/// C) to an <see cref="DataReaderFactory"/> of the PDB data. The <see cref="DataReaderFactory"/> will
		/// be owned by the module. You don't need to initialize <see cref="TryToLoadPdbFromDisk"/>
		/// </summary>
		public object PdbFileOrData { get; set; }

		/// <summary>
		/// If <c>true</c>, will load the PDB file from disk if present, or an embedded portable PDB file
		/// stored in the PE file. The default value is <c>true</c>.
		/// You don't need to initialize <see cref="PdbFileOrData"/>.
		/// </summary>
		public bool TryToLoadPdbFromDisk { get; set; } = true;

		/// <summary>
		/// corlib assembly reference to use or <c>null</c> if the default one from the opened
		/// module should be used.
		/// </summary>
		public AssemblyRef CorLibAssemblyRef { get; set; }

		/// <summary>
		/// Default constructor
		/// </summary>
		public ModuleCreationOptions() { }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="context">Module context</param>
		public ModuleCreationOptions(ModuleContext context) => Context = context;
	}
}
