// dnlib: See LICENSE.txt for more info

using System.Diagnostics;

namespace dnlib.IO {
	/// <summary>
	/// Base class for classes needing to implement IFileSection
	/// </summary>
	[DebuggerDisplay("O:{startOffset} L:{size} {GetType().Name}")]
	public class FileSection : IFileSection {
		/// <summary>
		/// The start file offset of this section
		/// </summary>
		protected FileOffset startOffset;

		/// <summary>
		/// Size of the section
		/// </summary>
		protected uint size;

		/// <inheritdoc/>
		public FileOffset StartOffset => startOffset;

		/// <inheritdoc/>
		public FileOffset EndOffset => startOffset + size;

		/// <summary>
		/// Set <see cref="startOffset"/> to <paramref name="reader"/>'s current position
		/// </summary>
		/// <param name="reader">The reader</param>
		protected void SetStartOffset(ref DataReader reader) =>
			startOffset = (FileOffset)reader.CurrentOffset;

		/// <summary>
		/// Set <see cref="size"/> according to <paramref name="reader"/>'s current position
		/// </summary>
		/// <param name="reader">The reader</param>
		protected void SetEndoffset(ref DataReader reader) =>
			size = reader.CurrentOffset - (uint)startOffset;
	}
}
