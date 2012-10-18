using System.IO;
using dot10.IO;
using dot10.PE;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// .NET meta data
	/// </summary>
	class MetaData : IChunk {
		ModuleDef module;
		UniqueChunkList<IChunk> constants;
		MethodBodyChunks methodBodies;
		ChunkList<IChunk> netResources;
		TablesHeap tablesHeap;
		StringsHeap stringsHeap;
		USHeap usHeap;
		GuidHeap guidHeap;
		BlobHeap blobHeap;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="constants">Constants list</param>
		/// <param name="methodBodies">Method bodies list</param>
		/// <param name="netResources">.NET resources list</param>
		public MetaData(ModuleDef module, UniqueChunkList<IChunk> constants, MethodBodyChunks methodBodies, ChunkList<IChunk> netResources) {
			this.module = module;
			this.constants = constants;
			this.methodBodies = methodBodies;
			this.netResources = netResources;
			this.tablesHeap = new TablesHeap();
			this.stringsHeap = new StringsHeap();
			this.usHeap = new USHeap();
			this.guidHeap = new GuidHeap();
			this.blobHeap = new BlobHeap();
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			throw new System.NotImplementedException();
		}

		/// <inheritdoc/>
		public uint GetLength() {
			throw new System.NotImplementedException();
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			throw new System.NotImplementedException();
		}
	}
}
