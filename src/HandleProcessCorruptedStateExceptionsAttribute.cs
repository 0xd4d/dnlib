// dnlib: See LICENSE.txt for more info

﻿using System;
#pragma warning disable 1591	// XML doc warning

namespace System.Runtime.ExceptionServices {
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	sealed class HandleProcessCorruptedStateExceptionsAttribute : Attribute {
	}
}
