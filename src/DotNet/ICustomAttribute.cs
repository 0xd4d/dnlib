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

using System.Collections.Generic;
using dnlib.Threading;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet {
	/// <summary>
	/// Custom attribute interface. Implemented by <see cref="SecurityAttribute"/> and
	/// <see cref="CustomAttribute"/>
	/// </summary>
	public interface ICustomAttribute {
		/// <summary>
		/// Gets the attribute type
		/// </summary>
		ITypeDefOrRef AttributeType { get; }

		/// <summary>
		/// Gets the full name of the attribute type
		/// </summary>
		string TypeFullName { get; }

		/// <summary>
		/// Gets all named arguments (field and property values)
		/// </summary>
		ThreadSafe.IList<CANamedArgument> NamedArguments { get; }

		/// <summary>
		/// <c>true</c> if <see cref="NamedArguments"/> is not empty
		/// </summary>
		bool HasNamedArguments { get; }

		/// <summary>
		/// Gets all <see cref="CANamedArgument"/>s that are field arguments
		/// </summary>
		IEnumerable<CANamedArgument> Fields { get; }

		/// <summary>
		/// Gets all <see cref="CANamedArgument"/>s that are property arguments
		/// </summary>
		IEnumerable<CANamedArgument> Properties { get; }
	}
}
