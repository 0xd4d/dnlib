// dnlib: See LICENSE.txt for more info

using dnlib.IO;

namespace dnlib.DotNet.MD {
	/// <summary>
	/// #Pdb stream
	/// </summary>
	public sealed class PdbStream : HeapStream {
		/// <summary>
		/// Gets the PDB id
		/// </summary>
		public byte[] Id { get; private set; }

		/// <summary>
		/// Gets the entry point token or 0
		/// </summary>
		public MDToken EntryPoint { get; private set; }

		/// <summary>
		/// Gets the referenced type system tables in the PE metadata file
		/// </summary>
		public ulong ReferencedTypeSystemTables { get; private set; }

		/// <summary>
		/// Gets all type system table rows. This array has exactly 64 elements.
		/// </summary>
		public uint[] TypeSystemTableRows { get; private set; }

		/// <inheritdoc/>
		public PdbStream(IImageStream imageStream, StreamHeader streamHeader)
			: base(imageStream, streamHeader) {
			using (var stream = GetClonedImageStream()) {
				Id = stream.ReadBytes(20);
				EntryPoint = new MDToken(stream.ReadUInt32());
				var tables = stream.ReadUInt64();
				ReferencedTypeSystemTables = tables;
				var rows = new uint[64];
				for (int i = 0; i < rows.Length; i++, tables >>= 1) {
					if (((uint)tables & 1) != 0)
						rows[i] = stream.ReadUInt32();
				}
				TypeSystemTableRows = rows;
			}
		}
	}
}
