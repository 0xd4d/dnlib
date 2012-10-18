namespace dot10.DotNet.Writer {
	/// <summary>
	/// A PE section
	/// </summary>
	class PESection : ChunkList<IChunk> {
		string name;
		uint characteristics;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Section name</param>
		/// <param name="characteristics">Section characteristics</param>
		public PESection(string name, uint characteristics) {
			this.name = name;
			this.characteristics = characteristics;
		}
	}
}
