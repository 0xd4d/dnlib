// dnlib: See LICENSE.txt for more info

using dnlib.PE;

namespace dnlib.DotNet.Pdb {
	/// <summary>
	/// IMAGE_DEBUG_DIRECTORY
	/// </summary>
	public struct IMAGE_DEBUG_DIRECTORY {
#pragma warning disable 1591
		public uint Characteristics;
		public uint TimeDateStamp;
		public ushort MajorVersion;
		public ushort MinorVersion;
		public ImageDebugType Type;
		public uint SizeOfData;
		public uint AddressOfRawData;
		public uint PointerToRawData;
#pragma warning restore 1591
	}
}
