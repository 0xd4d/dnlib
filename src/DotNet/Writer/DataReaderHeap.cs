// dnlib: See LICENSE.txt for more info

using System;
using dnlib.DotNet.MD;
using dnlib.IO;

namespace dnlib.DotNet.Writer {
	/// <summary>
	/// Copies existing data to a new metadata heap
	/// </summary>
	public sealed class DataReaderHeap : HeapBase {
		/// <summary>
		/// Gets the name of the heap
		/// </summary>
		public override string Name { get; }

		internal DotNetStream OptionalOriginalStream { get; }

		readonly DataReader heapReader;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="stream">The stream whose data will be copied to the new metadata file</param>
		public DataReaderHeap(DotNetStream stream) {
			OptionalOriginalStream = stream ?? throw new ArgumentNullException(nameof(stream));
			heapReader = stream.CreateReader();
			Name = stream.Name;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Heap name</param>
		/// <param name="heapReader">Heap content</param>
		public DataReaderHeap(string name, DataReader heapReader) {
			this.heapReader = heapReader;
			this.heapReader.Position = 0;
			Name = name ?? throw new ArgumentNullException(nameof(name));
		}

		/// <inheritdoc/>
		public override uint GetRawLength() => heapReader.Length;

		/// <inheritdoc/>
		protected override void WriteToImpl(DataWriter writer) => heapReader.CopyTo(writer);
	}
}
