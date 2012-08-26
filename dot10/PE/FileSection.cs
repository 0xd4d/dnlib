using System.IO;

namespace dot10.PE {
	/// <summary>
	/// Base class for classes needing to implement IFileSection
	/// </summary>
	class FileSection : IFileSection {
		/// <summary>
		/// The start file offset of this section
		/// </summary>
		protected FileOffset startOffset;

		/// <summary>
		/// Size of the section
		/// </summary>
		protected uint size;

		/// <inheritdoc/>
		public FileOffset StartOffset {
			get { return startOffset; }
		}

		/// <inheritdoc/>
		public FileOffset EndOffset {
			get { return startOffset + size; }
		}

		/// <summary>
		/// Set <see cref="startOffset"/> to <paramref name="reader"/>'s current position
		/// </summary>
		/// <param name="reader">The reader</param>
		protected void SetStartOffset(BinaryReader reader) {
			startOffset = new FileOffset(reader.BaseStream.Position);
		}

		/// <summary>
		/// Set <see cref="size"/> according to <paramref name="reader"/>'s current position
		/// </summary>
		/// <param name="reader">The reader</param>
		protected void SetEndoffset(BinaryReader reader) {
			size = (uint)(reader.BaseStream.Position - startOffset.Value);
		}

		/// <inheritdoc/>
		public override string ToString() {
			return string.Format("O:{0:X8} L:{1:X4} - {2}", startOffset.Value, size, GetType().Name);
		}
	}
}
