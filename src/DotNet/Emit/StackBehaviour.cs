namespace dot10.DotNet.Emit {
	/// <summary>
	/// CIL opcode stack behavior
	/// </summary>
	public enum StackBehaviour : byte {
		/// <summary></summary>
		Pop0,
		/// <summary></summary>
		Pop1,
		/// <summary></summary>
		Pop1_pop1,
		/// <summary></summary>
		Popi,
		/// <summary></summary>
		Popi_pop1,
		/// <summary></summary>
		Popi_popi,
		/// <summary></summary>
		Popi_popi8,
		/// <summary></summary>
		Popi_popi_popi,
		/// <summary></summary>
		Popi_popr4,
		/// <summary></summary>
		Popi_popr8,
		/// <summary></summary>
		Popref,
		/// <summary></summary>
		Popref_pop1,
		/// <summary></summary>
		Popref_popi,
		/// <summary></summary>
		Popref_popi_popi,
		/// <summary></summary>
		Popref_popi_popi8,
		/// <summary></summary>
		Popref_popi_popr4,
		/// <summary></summary>
		Popref_popi_popr8,
		/// <summary></summary>
		Popref_popi_popref,
		/// <summary></summary>
		Push0,
		/// <summary></summary>
		Push1,
		/// <summary></summary>
		Push1_push1,
		/// <summary></summary>
		Pushi,
		/// <summary></summary>
		Pushi8,
		/// <summary></summary>
		Pushr4,
		/// <summary></summary>
		Pushr8,
		/// <summary></summary>
		Pushref,
		/// <summary></summary>
		Varpop,
		/// <summary></summary>
		Varpush,
		/// <summary></summary>
		Popref_popi_pop1,
		/// <summary></summary>
		PopAll = 0xFF,
	}
}
