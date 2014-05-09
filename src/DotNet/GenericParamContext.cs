/*
    Copyright (C) 2012-2014 de4dot@gmail.com

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

namespace dnlib.DotNet {
	/// <summary>
	/// Generic parameter context
	/// </summary>
	public struct GenericParamContext {
		/// <summary>
		/// Type context
		/// </summary>
		public readonly TypeDef Type;

		/// <summary>
		/// Method context
		/// </summary>
		public readonly MethodDef Method;

		/// <summary>
		/// Creates a new <see cref="GenericParamContext"/> instance and initializes the
		/// <see cref="Type"/> field to <paramref name="method"/>'s <see cref="MethodDef.DeclaringType"/>
		/// and the <see cref="Method"/> field to <paramref name="method"/>.
		/// </summary>
		/// <param name="method">Method</param>
		/// <returns>A new <see cref="GenericParamContext"/> instance</returns>
		public static GenericParamContext Create(MethodDef method) {
			if (method == null)
				return new GenericParamContext();
			return new GenericParamContext(method.DeclaringType, method);
		}

		/// <summary>
		/// Creates a new <see cref="GenericParamContext"/> instance and initializes the
		/// <see cref="Type"/> field to <paramref name="type"/> and the <see cref="Method"/> field
		/// to <c>null</c>
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns>A new <see cref="GenericParamContext"/> instance</returns>
		public static GenericParamContext Create(TypeDef type) {
			return new GenericParamContext(type);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Type context</param>
		public GenericParamContext(TypeDef type) {
			this.Type = type;
			this.Method = null;
		}

		/// <summary>
		/// Constructor. The <see cref="Type"/> field is set to <c>null</c> and <c>NOT</c> to
		/// <paramref name="method"/>'s <see cref="MethodDef.DeclaringType"/>. Use
		/// <see cref="Create(MethodDef)"/> if you want that behavior.
		/// </summary>
		/// <param name="method">Method context</param>
		public GenericParamContext(MethodDef method) {
			this.Type = null;
			this.Method = method;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Type context</param>
		/// <param name="method">Method context</param>
		public GenericParamContext(TypeDef type, MethodDef method) {
			this.Type = type;
			this.Method = method;
		}
	}
}
