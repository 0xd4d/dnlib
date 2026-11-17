// dnlib: See LICENSE.txt for more info

#nullable enable

#if NET35
namespace System.Runtime.ExceptionServices {
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	sealed class HandleProcessCorruptedStateExceptionsAttribute : Attribute {
	}
}
#endif
