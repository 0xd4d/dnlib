// dnlib: See LICENSE.txt for more info

#pragma warning disable 1591	// XML doc warning

#if !NETCOREAPP2_0
namespace System.Runtime.ExceptionServices {
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	sealed class HandleProcessCorruptedStateExceptionsAttribute : Attribute {
	}
}
#endif