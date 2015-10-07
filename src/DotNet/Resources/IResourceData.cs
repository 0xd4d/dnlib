// dnlib: See LICENSE.txt for more info

using System.IO;
using System.Runtime.Serialization;
using dnlib.IO;

namespace dnlib.DotNet.Resources {
	/// <summary>
	/// Implemented by all resource data
	/// </summary>
	public interface IResourceData : IFileSection {
		/// <summary>
		/// Gets the type of data
		/// </summary>
		ResourceTypeCode Code { get; }

		/// <summary>
		/// Start offset of the section in the file
		/// </summary>
		new FileOffset StartOffset { get; set; }

		/// <summary>
		/// End offset of the section in the file. This is one byte after the last
		/// valid offset in the section.
		/// </summary>
		new FileOffset EndOffset { get; set; }

		/// <summary>
		/// Writes the data
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="formatter">Formatter if needed by implementer</param>
		void WriteData(BinaryWriter writer, IFormatter formatter);
	}
}
