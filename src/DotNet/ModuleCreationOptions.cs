/*
    Copyright (C) 2012-2014 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System.Diagnostics.SymbolStore;
using dnlib.IO;

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
		/// Set it to A) the path (string) of the PDB file, B) the data (byte[]) of the PDB file or
		/// C) to an <see cref="IImageStream"/> of the PDB data. The <see cref="IImageStream"/> will
		/// be owned by the module. You don't need to initialize <see cref="TryToLoadPdbFromDisk"/>
		/// or <see cref="CreateSymbolReader"/>
		/// </summary>
		public object PdbFileOrData { get; set; }

		/// <summary>
		/// If <c>true</c>, will load the PDB file from disk if present. You don't need to
		/// initialize <see cref="CreateSymbolReader"/> or <see cref="PdbFileOrData"/>.
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
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="context">Module context</param>
		public ModuleCreationOptions(ModuleContext context) {
			this.Context = context;
		}
	}

	/// <summary>
	/// Creates a <see cref="ISymbolReader"/>
	/// </summary>
	/// <param name="module">Module</param>
	/// <returns>A <see cref="ISymbolReader"/> instance for (and now owned by)
	/// <paramref name="module"/> or <c>null</c>.</returns>
	public delegate ISymbolReader CreateSymbolReaderDelegate(ModuleDefMD module);
}
