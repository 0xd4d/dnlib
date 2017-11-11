// dnlib: See LICENSE.txt for more info

namespace dnlib.DotNet.Writer {
	static class PortablePdbConstants {
		// See System.Reflection.Metadata.PortablePdbVersions

		// Portable PDB version (v1.0)
		// Format version is stored in DebugDirectory.MajorVersion
		//	SRM: DefaultFormatVersion, MinFormatVersion
		public const ushort FormatVersion = 0x0100;

		// Embedded Portable PDB Blob verison (v1.0)
		// Embedded version is stored in DebugDirectory.MinorVersion
		//	SRM: MinEmbeddedVersion, DefaultEmbeddedVersion, MinUnsupportedEmbeddedVersion
		public const ushort EmbeddedVersion = 0x0100;

		// Stored in DebugDirectory.MinorVersion and indicates that it's a portable PDB file
		// and not a Windows PDB file
		public const ushort PortableCodeViewVersionMagic = 0x504D;
	}
}
