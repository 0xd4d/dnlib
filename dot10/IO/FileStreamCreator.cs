using System;
using System.IO;

namespace dot10.IO {
	/// <summary>
	/// Creates streams to partially access a FileStream
	/// </summary>
	/// <seealso cref="MemoryStreamCreator"/>
	/// <seealso cref="UnmanagedMemoryStreamCreator"/>
	public class FileStreamCreator : IStreamCreator {
		string filename;
		Stream theFile;

		/// <inheritdoc/>
		public long Length {
			get { return theFile.Length; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="filename">Name of the file</param>
		public FileStreamCreator(string filename) {
			this.filename = filename;

			// Open the file to make sure no-one can write to it while we're reading it
			this.theFile = CreateFileStream(filename);
		}

		static FileStream CreateFileStream(string filename) {
			return new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
		}

		/// <inheritdoc/>
		public Stream Create(FileOffset offset, long length) {
			if (offset.Value < 0)
				throw new ArgumentOutOfRangeException("offset");
			if (length < 0)
				throw new ArgumentOutOfRangeException("length");
			if (offset.Value + length < offset.Value)
				throw new ArgumentOutOfRangeException("length");

			if (offset == FileOffset.Zero && length == theFile.Length)
				return CreateFull();
			return new RangeStream(CreateFileStream(filename), offset.Value, length);
		}

		/// <inheritdoc/>
		public Stream CreateFull() {
			return CreateFileStream(filename);
		}

		/// <inheritdoc/>
		public void Dispose() {
		}
	}
}
