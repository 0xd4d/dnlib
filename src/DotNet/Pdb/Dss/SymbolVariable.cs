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

using System;
using System.Diagnostics.SymbolStore;

namespace dnlib.DotNet.Pdb.Dss {
	sealed class SymbolVariable : ISymbolVariable {
		readonly ISymUnmanagedVariable variable;

		public SymbolVariable(ISymUnmanagedVariable variable) {
			this.variable = variable;
		}

		public int AddressField1 {
			get {
				uint result;
				variable.GetAddressField1(out result);
				return (int)result;
			}
		}

		public int AddressField2 {
			get {
				uint result;
				variable.GetAddressField2(out result);
				return (int)result;
			}
		}

		public int AddressField3 {
			get {
				uint result;
				variable.GetAddressField3(out result);
				return (int)result;
			}
		}

		public SymAddressKind AddressKind {
			get {
				uint result;
				variable.GetAddressKind(out result);
				return (SymAddressKind)result;
			}
		}

		public object Attributes {
			get {
				uint result;
				variable.GetAttributes(out result);
				return (int)result;
			}
		}

		public int EndOffset {
			get {
				uint result;
				variable.GetEndOffset(out result);
				return (int)result;
			}
		}

		public string Name {
			get {
				uint count;
				variable.GetName(0, out count, null);
				var chars = new char[count];
				variable.GetName((uint)chars.Length, out count, chars);
				if (chars.Length == 0)
					return string.Empty;
				return new string(chars, 0, chars.Length - 1);
			}
		}

		public int StartOffset {
			get {
				uint result;
				variable.GetStartOffset(out result);
				return (int)result;
			}
		}

		public byte[] GetSignature() {
			uint bufSize;
			variable.GetSignature(0, out bufSize, null);
			var buffer = new byte[bufSize];
			variable.GetSignature((uint)buffer.Length, out bufSize, buffer);
			return buffer;
		}
	}
}
