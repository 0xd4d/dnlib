// dnlib: See LICENSE.txt for more info

#if NET35
namespace System.Runtime.InteropServices {
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Method, AllowMultiple = false)]
	sealed class DefaultDllImportSearchPathsAttribute : Attribute {
		public DefaultDllImportSearchPathsAttribute(DllImportSearchPath paths) => _paths = paths;
		public DllImportSearchPath Paths => _paths;
		internal DllImportSearchPath _paths;
	}

	[Flags]
	enum DllImportSearchPath {
		LegacyBehavior					= 0,
		AssemblyDirectory				= 2,
		UseDllDirectoryForDependencies	= 0x100,
		ApplicationDirectory			= 0x200,
		UserDirectories					= 0x400,
		System32						= 0x800,
		SafeDirectories					= 0x1000,
	}
}
#endif
