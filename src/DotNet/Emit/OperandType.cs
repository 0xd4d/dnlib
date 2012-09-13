namespace dot10.DotNet.Emit {
	/// <summary>
	/// CIL opcode operand type
	/// </summary>
	public enum OperandType : byte {
		/// <summary></summary>
		InlineBrTarget,
		/// <summary></summary>
		InlineField,
		/// <summary></summary>
		InlineI,
		/// <summary></summary>
		InlineI8,
		/// <summary></summary>
		InlineMethod,
		/// <summary></summary>
		InlineNone,
		/// <summary></summary>
		InlinePhi,
		/// <summary></summary>
		InlineR,
		/// <summary></summary>
		NOT_USED_8,
		/// <summary></summary>
		InlineSig,
		/// <summary></summary>
		InlineString,
		/// <summary></summary>
		InlineSwitch,
		/// <summary></summary>
		InlineTok,
		/// <summary></summary>
		InlineType,
		/// <summary></summary>
		InlineVar,
		/// <summary></summary>
		ShortInlineBrTarget,
		/// <summary></summary>
		ShortInlineI,
		/// <summary></summary>
		ShortInlineR,
		/// <summary></summary>
		ShortInlineVar,
	}
}
