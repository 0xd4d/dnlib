// dnlib: See LICENSE.txt for more info

using System;
using dnlib.IO;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// #Pdb heap
	/// </summary>
	public sealed class PdbHeap : HeapBase {
		/// <inheritdoc/>
		public override string Name => "#Pdb";

		/// <summary>
		/// Gets the PDB ID. This is always 20 bytes in size.
		/// </summary>
		public byte[] PdbId => pdbId;
		readonly byte[] pdbId;

		/// <summary>
		/// Gets/sets the entry point token
		/// </summary>
		public uint EntryPoint {
			get => entryPoint;
			set => entryPoint = value;
		}
		uint entryPoint;

		/// <summary>
		/// Gets the offset of the 20-byte PDB ID
		/// </summary>
		public FileOffset PdbIdOffset => FileOffset;

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
		public uint[] TypeSystemTableRows => typeSystemTableRows;
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
		protected override void WriteToImpl(DataWriter writer) {
			if (!referencedTypeSystemTablesInitd)
				throw new InvalidOperationException("ReferencedTypeSystemTables hasn't been initialized yet");
			writer.WriteBytes(pdbId);
			writer.WriteUInt32(entryPoint);
			writer.WriteUInt64(referencedTypeSystemTables);
			ulong t = referencedTypeSystemTables;
			for (int i = 0; i < typeSystemTableRows.Length; i++, t >>= 1) {
				if (((int)t & 1) != 0)
					writer.WriteUInt32(typeSystemTableRows[i]);
			}
		}
	}
}
