// dnlib: See LICENSE.txt for more info

﻿namespace dnlib.DotNet.Writer {
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

		/// <summary>
		/// Called when the heap should be set to read-only mode
		/// </summary>
		void SetReadOnly();
	}
}
