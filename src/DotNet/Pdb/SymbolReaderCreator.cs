// dnlib: See LICENSE.txt for more info

using System;
using System.Diagnostics.SymbolStore;
using dnlib.DotNet.MD;
using dnlib.IO;

namespace dnlib.DotNet.Pdb {
	/// <summary>
	/// Creates a <see cref="ISymbolReader"/> instance
	/// </summary>
	public static class SymbolReaderCreator {
		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="pdbImpl">PDB implementation to use</param>
		/// <param name="assemblyFileName">Path to assembly</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if there's no PDB
		/// file on disk or if it's not possible to create a <see cref="ISymbolReader"/>.</returns>
		public static ISymbolReader Create(PdbImplType pdbImpl, string assemblyFileName) {
			switch (pdbImpl) {
			case PdbImplType.MicrosoftCOM:
				return Dss.SymbolReaderCreator.Create(assemblyFileName);

			case PdbImplType.Managed:
				return Managed.SymbolReaderCreator.CreateFromAssemblyFile(assemblyFileName);

			default: throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="pdbImpl">PDB implementation to use</param>
		/// <param name="metaData">.NET metadata. Only need to be non-null if MS COM API should be used</param>
		/// <param name="pdbFileName">Path to PDB file</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if there's no PDB
		/// file on disk or if it's not possible to create a <see cref="ISymbolReader"/>.</returns>
		public static ISymbolReader Create(PdbImplType pdbImpl, IMetaData metaData, string pdbFileName) {
			switch (pdbImpl) {
			case PdbImplType.MicrosoftCOM:
				return Dss.SymbolReaderCreator.Create(metaData, pdbFileName);

			case PdbImplType.Managed:
				return Managed.SymbolReaderCreator.Create(pdbFileName);

			default: throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="pdbImpl">PDB implementation to use</param>
		/// <param name="metaData">.NET metadata. Only need to be non-null if MS COM API should be used</param>
		/// <param name="pdbData">PDB file data</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if it's not possible
		/// to create a <see cref="ISymbolReader"/>.</returns>
		public static ISymbolReader Create(PdbImplType pdbImpl, IMetaData metaData, byte[] pdbData) {
			switch (pdbImpl) {
			case PdbImplType.MicrosoftCOM:
				return Dss.SymbolReaderCreator.Create(metaData, pdbData);

			case PdbImplType.Managed:
				return Managed.SymbolReaderCreator.Create(pdbData);

			default: throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="pdbImpl">PDB implementation to use</param>
		/// <param name="metaData">.NET metadata. Only need to be non-null if MS COM API should be used</param>
		/// <param name="pdbStream">PDB file stream which is now owned by us</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if it's not possible
		/// to create a <see cref="ISymbolReader"/>.</returns>
		public static ISymbolReader Create(PdbImplType pdbImpl, IMetaData metaData, IImageStream pdbStream) {
			switch (pdbImpl) {
			case PdbImplType.MicrosoftCOM:
				return Dss.SymbolReaderCreator.Create(metaData, pdbStream);

			case PdbImplType.Managed:
				return Managed.SymbolReaderCreator.Create(pdbStream);

			default:
				if (pdbStream != null)
					pdbStream.Dispose();
				throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="pdbImpl">PDB implementation to use</param>
		/// <param name="mdStream">.NET metadata stream which is now owned by us. Only need to be
		/// non-null if MS COM API should be used</param>
		/// <param name="pdbFileName">Path to PDB file</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if there's no PDB
		/// file on disk or if it's not possible to create a <see cref="ISymbolReader"/>.</returns>
		public static ISymbolReader Create(PdbImplType pdbImpl, IImageStream mdStream, string pdbFileName) {
			switch (pdbImpl) {
			case PdbImplType.MicrosoftCOM:
				return Dss.SymbolReaderCreator.Create(mdStream, pdbFileName);

			case PdbImplType.Managed:
				if (mdStream != null)
					mdStream.Dispose();
				return Managed.SymbolReaderCreator.Create(pdbFileName);

			default:
				if (mdStream != null)
					mdStream.Dispose();
				throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="pdbImpl">PDB implementation to use</param>
		/// <param name="mdStream">.NET metadata stream which is now owned by us. Only need to be
		/// non-null if MS COM API should be used</param>
		/// <param name="pdbData">PDB file data</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if it's not possible
		/// to create a <see cref="ISymbolReader"/>.</returns>
		public static ISymbolReader Create(PdbImplType pdbImpl, IImageStream mdStream, byte[] pdbData) {
			switch (pdbImpl) {
			case PdbImplType.MicrosoftCOM:
				return Dss.SymbolReaderCreator.Create(mdStream, pdbData);

			case PdbImplType.Managed:
				if (mdStream != null)
					mdStream.Dispose();
				return Managed.SymbolReaderCreator.Create(pdbData);

			default:
				if (mdStream != null)
					mdStream.Dispose();
				throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// Creates a new <see cref="ISymbolReader"/> instance
		/// </summary>
		/// <param name="pdbImpl">PDB implementation to use</param>
		/// <param name="mdStream">.NET metadata stream which is now owned by us. Only need to be
		/// non-null if MS COM API should be used</param>
		/// <param name="pdbStream">PDB file stream which is now owned by us</param>
		/// <returns>A new <see cref="ISymbolReader"/> instance or <c>null</c> if it's not possible
		/// to create a <see cref="ISymbolReader"/>.</returns>
		public static ISymbolReader Create(PdbImplType pdbImpl, IImageStream mdStream, IImageStream pdbStream) {
			switch (pdbImpl) {
			case PdbImplType.MicrosoftCOM:
				return Dss.SymbolReaderCreator.Create(mdStream, pdbStream);

			case PdbImplType.Managed:
				if (mdStream != null)
					mdStream.Dispose();
				return Managed.SymbolReaderCreator.Create(pdbStream);

			default:
				if (mdStream != null)
					mdStream.Dispose();
				if (pdbStream != null)
					pdbStream.Dispose();
				throw new InvalidOperationException();
			}
		}
	}
}
