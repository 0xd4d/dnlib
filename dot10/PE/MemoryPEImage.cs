namespace dot10.PE {
	/// <summary>
	/// Accesses a PE file that has the same structure as a PE file loaded
	/// into memory by the OS PE file loader.
	/// </summary>
	/// <seealso cref="FilePEImage"/>
	class MemoryPEImage : IPEImage {
		/// <inheritdoc/>
		public RVA ToRVA(FileOffset offset) {
			return new RVA((uint)offset.Value);
		}

		/// <inheritdoc/>
		public FileOffset ToFileOffset(RVA rva) {
			return new FileOffset(rva.Value);
		}
	}
}
