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

﻿namespace dnlib.DotNet.Emit {
	/// <summary>
	/// CIL opcode stack behavior
	/// </summary>
	public enum StackBehaviour : byte {
		/// <summary/>
		Pop0,
		/// <summary/>
		Pop1,
		/// <summary/>
		Pop1_pop1,
		/// <summary/>
		Popi,
		/// <summary/>
		Popi_pop1,
		/// <summary/>
		Popi_popi,
		/// <summary/>
		Popi_popi8,
		/// <summary/>
		Popi_popi_popi,
		/// <summary/>
		Popi_popr4,
		/// <summary/>
		Popi_popr8,
		/// <summary/>
		Popref,
		/// <summary/>
		Popref_pop1,
		/// <summary/>
		Popref_popi,
		/// <summary/>
		Popref_popi_popi,
		/// <summary/>
		Popref_popi_popi8,
		/// <summary/>
		Popref_popi_popr4,
		/// <summary/>
		Popref_popi_popr8,
		/// <summary/>
		Popref_popi_popref,
		/// <summary/>
		Push0,
		/// <summary/>
		Push1,
		/// <summary/>
		Push1_push1,
		/// <summary/>
		Pushi,
		/// <summary/>
		Pushi8,
		/// <summary/>
		Pushr4,
		/// <summary/>
		Pushr8,
		/// <summary/>
		Pushref,
		/// <summary/>
		Varpop,
		/// <summary/>
		Varpush,
		/// <summary/>
		Popref_popi_pop1,
		/// <summary/>
		PopAll = 0xFF,
	}
}
