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

﻿namespace dnlib.DotNet.Emit {
	/// <summary>
	/// A CIL method exception handler
	/// </summary>
	public sealed class ExceptionHandler {
		/// <summary>
		/// First instruction of try block
		/// </summary>
		public Instruction TryStart;

		/// <summary>
		/// One instruction past the end of try block or <c>null</c> if it ends at the end
		/// of the method.
		/// </summary>
		public Instruction TryEnd;

		/// <summary>
		/// Start of filter handler or <c>null</c> if none. The end of filter handler is
		/// always <see cref="HandlerStart"/>.
		/// </summary>
		public Instruction FilterStart;

		/// <summary>
		/// First instruction of try handler block
		/// </summary>
		public Instruction HandlerStart;

		/// <summary>
		/// One instruction past the end of try handler block or <c>null</c> if it ends at the end
		/// of the method.
		/// </summary>
		public Instruction HandlerEnd;

		/// <summary>
		/// The catch type if <see cref="HandlerType"/> is <see cref="ExceptionHandlerType.Catch"/>
		/// </summary>
		public ITypeDefOrRef CatchType;

		/// <summary>
		/// Type of exception handler clause
		/// </summary>
		public ExceptionHandlerType HandlerType;

		/// <summary>
		/// Default constructor
		/// </summary>
		public ExceptionHandler() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="handlerType">Exception clause type</param>
		public ExceptionHandler(ExceptionHandlerType handlerType) {
			this.HandlerType = handlerType;
		}
	}
}
