// dnlib: See LICENSE.txt for more info

using System.IO;
using System.Runtime.Serialization;

namespace dnlib.DotNet.Resources {
	/// <summary>
	/// Implemented by all resource data
	/// </summary>
	public interface IResourceData {
		/// <summary>
		/// Gets the type of data
		/// </summary>
		ResourceTypeCode Code { get; }

		/// <summary>
		/// Writes the data
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="formatter">Formatter if needed by implementer</param>
		void WriteData(BinaryWriter writer, IFormatter formatter);
	}
}
