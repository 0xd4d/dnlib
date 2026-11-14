// dnlib: See LICENSE.txt for more info

namespace dnlib.PE {
	/// <summary>
	/// Image debug type, see <c>IMAGE_DEBUG_TYPE_*</c> in winnt.n
	/// </summary>
	public enum ImageDebugType : uint {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		Unknown = 0,
		Coff = 1,

		/// <summary>
		/// Contains PDB info
		/// </summary>
		CodeView = 2,

		FPO = 3,
		Misc = 4,
		Exception = 5,
		Fixup = 6,
		OmapToSrc = 7,
		OmapFromSrc = 8,
		Borland = 9,
		Reserved10 = 10,
		CLSID = 11,
		VcFeature = 12,
		POGO = 13,
		ILTCG = 14,
		MPX = 15,

		/// <summary>
		/// It's a deterministic (reproducible) PE file
		/// </summary>
		Reproducible = 16,

		/// <summary>
		/// Embedded portable PDB data
		/// </summary>
		EmbeddedPortablePdb = 17,

		/// <summary>
		/// Checksum of the PDB file. 0 or more entries allowed.
		/// </summary>
		PdbChecksum = 19,

		/// <summary>
		/// ReadyToRun perf map info (written by crossgen2). The data starts with magic <c>0x4D523252</c> ("R2RM"),
		/// followed by a 16-byte signature, a 4-byte perf map format version and a null terminated UTF-8 file name.
		/// </summary>
		R2RPerfMap = 21,
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
