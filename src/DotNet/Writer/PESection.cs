using dot10.PE;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// A PE section
	/// </summary>
	public class PESection : ChunkList<IChunk> {
		string name;
		uint characteristics;

		/// <summary>
		/// Gets the name
		/// </summary>
		public string Name {
			get { return name; }
		}

		/// <summary>
		/// Gets the Characteristics
		/// </summary>
		public uint Characteristics {
			get { return characteristics; }
		}

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
