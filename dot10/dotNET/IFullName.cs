namespace dot10.dotNET {
	/// <summary>
	/// Interface to get the full name of a type
	/// </summary>
	public interface IFullName {
		/// <summary>
		/// Returns the name of this type
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Returns the reflection name of this type
		/// </summary>
		string ReflectionName { get; }

		/// <summary>
		/// Returns the namespace of this type
		/// </summary>
		string Namespace { get; }

		/// <summary>
		/// Returns the reflection namespace of this type
		/// </summary>
		string ReflectionNamespace { get; }

		/// <summary>
		/// Returns the human readable full name of this type
		/// </summary>
		string FullName { get; }

		/// <summary>
		/// Returns the reflection name of this type. It can be passed to
		/// <see cref="System.Type.GetType(string)"/> to load the type.
		/// </summary>
		string ReflectionFullName { get; }
	}
}
