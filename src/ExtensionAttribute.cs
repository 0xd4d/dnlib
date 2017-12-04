// dnlib: See LICENSE.txt for more info

#pragma warning disable 1591	// XML doc warning

#if !NETSTANDARD2_0
namespace System.Runtime.CompilerServices {
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
	public sealed class ExtensionAttribute : Attribute {
	}
}
#endif
