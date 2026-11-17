using System.IO;

namespace dnlib.DotNet.Resources {
	/// <summary>
	/// Extension of <see cref="BinaryWriter"/> for writing resource set elements
	/// </summary>
	public sealed class ResourceBinaryWriter : BinaryWriter {
		/// <summary>
		/// Format version of the resource set
		/// </summary>
		public int FormatVersion { get; internal set; }

		/// <summary>
		/// Specifies the target reader type of the resource set
		/// </summary>
		public ResourceReaderType ReaderType { get; internal set; }

		internal ResourceBinaryWriter(Stream stream) : base(stream) { }

		/// <summary>
		/// Writes a 7-bit encoded integer.
		/// </summary>
		/// <param name="value">The value to write</param>
		public new void Write7BitEncodedInt(int value) => base.Write7BitEncodedInt(value);
	}
}
