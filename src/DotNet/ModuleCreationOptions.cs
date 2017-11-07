// dnlib: See LICENSE.txt for more info

using dnlib.IO;
using dnlib.DotNet.Pdb;
using dnlib.DotNet.Pdb.Symbols;

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
		public CreateSymbolReaderDelegate CreateSymbolReader { get; set; }

		/// <summary>
		/// Which PDB reader to use. Default is <see cref="PdbImplType.Default"/>.
		/// </summary>
		public PdbImplType PdbImplementation { get; set; }

		/// <summary>
		/// Set it to A) the path (string) of the PDB file, B) the data (byte[]) of the PDB file or
		/// C) to an <see cref="IImageStream"/> of the PDB data. The <see cref="IImageStream"/> will
		/// be owned by the module. You don't need to initialize <see cref="TryToLoadPdbFromDisk"/>
		/// or <see cref="CreateSymbolReader"/>
		/// </summary>
		public object PdbFileOrData { get; set; }

		/// <summary>
		/// If <c>true</c>, will load the PDB file from disk if present. The default value is <c>true</c>.
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
			this.PdbImplementation = PdbImplType.Default;
			this.TryToLoadPdbFromDisk = true;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="context">Module context</param>
		public ModuleCreationOptions(ModuleContext context) {
			this.Context = context;
			this.PdbImplementation = PdbImplType.Default;
			this.TryToLoadPdbFromDisk = true;
		}
	}

	/// <summary>
	/// Creates a <see cref="SymbolReader"/>
	/// </summary>
	/// <param name="module">Module</param>
	/// <returns>A <see cref="SymbolReader"/> instance for (and now owned by)
	/// <paramref name="module"/> or <c>null</c>.</returns>
	public delegate SymbolReader CreateSymbolReaderDelegate(ModuleDefMD module);
}
