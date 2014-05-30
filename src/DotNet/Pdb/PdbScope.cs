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

using System.Diagnostics;
using dnlib.DotNet.Emit;
using dnlib.Threading;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet.Pdb {
	/// <summary>
	/// A PDB scope
	/// </summary>
	[DebuggerDisplay("{Start} - {End}")]
	public sealed class PdbScope {
		readonly ThreadSafe.IList<PdbScope> scopes = ThreadSafeListCreator.Create<PdbScope>();
		readonly ThreadSafe.IList<Local> locals = ThreadSafeListCreator.Create<Local>();
		readonly ThreadSafe.IList<string> namespaces = ThreadSafeListCreator.Create<string>();

		/// <summary>
		/// Gets/sets the first instruction
		/// </summary>
		public Instruction Start { get; set; }

		/// <summary>
		/// Gets/sets the last instruction. It's <c>null</c> if it ends at the end of the method.
		/// </summary>
		public Instruction End { get; set; }

		/// <summary>
		/// Gets all child scopes
		/// </summary>
		public ThreadSafe.IList<PdbScope> Scopes {
			get { return scopes; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="Scopes"/> is not empty
		/// </summary>
		public bool HasScopes {
			get { return scopes.Count > 0; }
		}

		/// <summary>
		/// Gets all locals in this scope
		/// </summary>
		public ThreadSafe.IList<Local> Variables {
			get { return locals; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="Variables"/> is not empty
		/// </summary>
		public bool HasVariables {
			get { return locals.Count > 0; }
		}

		/// <summary>
		/// Gets all namespaces
		/// </summary>
		public ThreadSafe.IList<string> Namespaces {
			get { return namespaces; }
		}

		/// <summary>
		/// <c>true</c> if <see cref="Namespaces"/> is not empty
		/// </summary>
		public bool HasNamespaces {
			get { return namespaces.Count > 0; }
		}
	}
}
