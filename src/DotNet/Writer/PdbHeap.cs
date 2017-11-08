// dnlib: See LICENSE.txt for more info

using System;
using System.IO;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// #Pdb heap
	/// </summary>
	public sealed class PdbHeap : HeapBase {
		/// <inheritdoc/>
		public override string Name {
			get { return "#Pdb"; }
		}

		/// <summary>
		/// Gets the PDB ID. This is always 20 bytes in size.
		/// </summary>
		public byte[] PdbId {
			get { return pdbId; }
		}
		readonly byte[] pdbId;

		/// <summary>
		/// Gets/sets the entry point token
		/// </summary>
		public uint EntryPoint {
			get { return entryPoint; }
			set { entryPoint = value; }
		}
		uint entryPoint;

		/// <summary>
		/// Gets/sets the referenced type system tables
		/// </summary>
		public ulong ReferencedTypeSystemTables {
			get {
				if (!referencedTypeSystemTablesInitd)
					throw new InvalidOperationException("ReferencedTypeSystemTables hasn't been initialized yet");
				return referencedTypeSystemTables;
			}
			set {
				if (isReadOnly)
					throw new InvalidOperationException("Size has already been calculated, can't write a new value");
				referencedTypeSystemTables = value;
				referencedTypeSystemTablesInitd = true;

				typeSystemTablesCount = 0;
				ulong l = value;
				while (l != 0) {
					if (((int)l & 1) != 0)
						typeSystemTablesCount++;
					l >>= 1;
				}
			}
		}
		ulong referencedTypeSystemTables;
		bool referencedTypeSystemTablesInitd;
		int typeSystemTablesCount;

		/// <summary>
		/// Gets the type system table rows. This table has 64 elements.
		/// </summary>
		public uint[] TypeSystemTableRows {
			get { return typeSystemTableRows; }
		}
		readonly uint[] typeSystemTableRows;

		/// <summary>
		/// Constructor
		/// </summary>
		public PdbHeap() {
			pdbId = new byte[20];
			typeSystemTableRows = new uint[64];
		}

		/// <inheritdoc/>
		public override uint GetRawLength() {
			if (!referencedTypeSystemTablesInitd)
				throw new InvalidOperationException("ReferencedTypeSystemTables hasn't been initialized yet");
			return (uint)(pdbId.Length + 4 + 8 + 4 * typeSystemTablesCount);
		}

		/// <inheritdoc/>
		protected override void WriteToImpl(BinaryWriter writer) {
			if (!referencedTypeSystemTablesInitd)
				throw new InvalidOperationException("ReferencedTypeSystemTables hasn't been initialized yet");
			writer.Write(pdbId);
			writer.Write(entryPoint);
			writer.Write(referencedTypeSystemTables);
			ulong t = referencedTypeSystemTables;
			for (int i = 0; i < typeSystemTableRows.Length; i++, t >>= 1) {
				if (((int)t & 1) != 0)
					writer.Write(typeSystemTableRows[i]);
			}
		}
	}
}
