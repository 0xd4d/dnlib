using System;
using System.Collections.Generic;
using System.IO;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// #GUID heap
	/// </summary>
	sealed class GuidHeap : HeapBase {
		List<Guid> guids = new List<Guid>();

		/// <inheritdoc/>
		public override string Name {
			get { return "#GUID"; }
		}

		/// <summary>
		/// Adds a guid to the #GUID heap
		/// </summary>
		/// <param name="guid">The guid</param>
		/// <returns>The index of the guid in the #GUID heap</returns>
		public uint Add(Guid? guid) {
			if (isReadOnly)
				throw new ModuleWriterException("Trying to modify #GUID when it's read-only");
			if (guid == null)
				return 0;

			// The number of GUIDs will almost always be 1 so there's no need for a dictionary.
			// The only table that contains GUIDs is the Module table, and it has three GUID
			// columns. Only one of them (Mvid) is normally set and the others are null.
			int index = guids.IndexOf(guid.Value);
			if (index >= 0)
				return (uint)index + 1;

			guids.Add(guid.Value);
			return (uint)guids.Count;
		}

		/// <inheritdoc/>
		public override uint GetRawLength() {
			return (uint)guids.Count * 16;
		}

		/// <inheritdoc/>
		protected override void WriteToImpl(BinaryWriter writer) {
			foreach (var guid in guids)
				writer.Write(guid.ToByteArray());
		}
	}
}
