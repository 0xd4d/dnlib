namespace dot10.DotNet {
	/// <summary>
	/// Interface to access a local or a parameter
	/// </summary>
	public interface IVariable {
		/// <summary>
		/// Gets the variable type
		/// </summary>
		ITypeSig Type { get; }

		/// <summary>
		/// Gets the 0-based position
		/// </summary>
		int Number { get; }

		/// <summary>
		/// Gets/sets the variable name
		/// </summary>
		string Name { get; set; }
	}
}
