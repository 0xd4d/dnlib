// dnlib: See LICENSE.txt for more info

#if NET35
namespace System.Runtime.ExceptionServices {
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	sealed class HandleProcessCorruptedStateExceptionsAttribute : Attribute {
	}
}
#endif
