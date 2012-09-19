namespace dot10.DotNet {
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
		/// Returns the reflection name of this type. See also <see cref="AssemblyQualifiedName"/>.
		/// </summary>
		string ReflectionFullName { get; }

		/// <summary>
		/// Returns the reflection name of this type, and includes the assembly name where the
		/// type is located. It can be passed to <see cref="System.Type.GetType(string)"/> to
		/// load the type.
		/// </summary>
		string AssemblyQualifiedName { get; }

		/// <summary>
		/// Gets the assembly where this type is defined
		/// </summary>
		IAssembly DefinitionAssembly { get; }

		/// <summary>
		/// Gets the owner module. The type was created from metadata in this module.
		/// </summary>
		ModuleDef OwnerModule { get; }
	}
}
