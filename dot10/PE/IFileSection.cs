namespace dot10.PE {
	/// <summary>
	/// Represents a section in a file
	/// </summary>
	interface IFileSection {
		/// <summary>
		/// Start offset of the section in the file
		/// </summary>
		FileOffset StartOffset { get; }

		/// <summary>
		/// End offset of the section in the file. This is one byte after the last
		/// valid offset in the section.
		/// </summary>
		FileOffset EndOffset { get; }
	}
}
