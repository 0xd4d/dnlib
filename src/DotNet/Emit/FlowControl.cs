namespace dot10.DotNet.Emit {
	/// <summary>
	/// CIL opcode flow control
	/// </summary>
	public enum FlowControl {
		/// <summary></summary>
		Branch,
		/// <summary></summary>
		Break,
		/// <summary></summary>
		Call,
		/// <summary></summary>
		Cond_Branch,
		/// <summary></summary>
		Meta,
		/// <summary></summary>
		Next,
		/// <summary></summary>
		Phi,
		/// <summary></summary>
		Return,
		/// <summary></summary>
		Throw,
	}
}
