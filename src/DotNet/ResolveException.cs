/*
    Copyright (C) 2012-2013 de4dot@gmail.com

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

﻿using System;

namespace dnlib.DotNet {
	/// <summary>
	/// Resolve exception base class
	/// </summary>
	[Serializable]
	public class ResolveException : Exception {
		/// <summary>
		/// Default constructor
		/// </summary>
		public ResolveException() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Exception message</param>
		public ResolveException(string message)
			: base(message) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Exception message</param>
		/// <param name="innerException">Inner exception or <c>null</c> if none</param>
		public ResolveException(string message, Exception innerException)
			: base(message, innerException) {
		}
	}

	/// <summary>
	/// Thrown if an assembly couldn't be resolved
	/// </summary>
	[Serializable]
	public class AssemblyResolveException : ResolveException {
		/// <summary>
		/// Default constructor
		/// </summary>
		public AssemblyResolveException() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Exception message</param>
		public AssemblyResolveException(string message)
			: base(message) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Exception message</param>
		/// <param name="innerException">Inner exception or <c>null</c> if none</param>
		public AssemblyResolveException(string message, Exception innerException)
			: base(message, innerException) {
		}
	}

	/// <summary>
	/// Thrown if a type couldn't be resolved
	/// </summary>
	[Serializable]
	public class TypeResolveException : ResolveException {
		/// <summary>
		/// Default constructor
		/// </summary>
		public TypeResolveException() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Exception message</param>
		public TypeResolveException(string message)
			: base(message) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Exception message</param>
		/// <param name="innerException">Inner exception or <c>null</c> if none</param>
		public TypeResolveException(string message, Exception innerException)
			: base(message, innerException) {
		}
	}

	/// <summary>
	/// Thrown if a method/field couldn't be resolved
	/// </summary>
	[Serializable]
	public class MemberRefResolveException : ResolveException {
		/// <summary>
		/// Default constructor
		/// </summary>
		public MemberRefResolveException() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Exception message</param>
		public MemberRefResolveException(string message)
			: base(message) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Exception message</param>
		/// <param name="innerException">Inner exception or <c>null</c> if none</param>
		public MemberRefResolveException(string message, Exception innerException)
			: base(message, innerException) {
		}
	}
}
