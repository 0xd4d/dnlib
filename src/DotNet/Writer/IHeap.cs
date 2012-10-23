namespace dot10.DotNet.Writer {
	/// <summary>
	/// .NET Heap interface
	/// </summary>
	public interface IHeap : IChunk {
		/// <summary>
		/// Gets the name of the heap
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Checks whether the heap is empty
		/// </summary>
		bool IsEmpty { get; }
	}
}
