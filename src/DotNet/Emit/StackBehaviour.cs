// dnlib: See LICENSE.txt for more info

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
