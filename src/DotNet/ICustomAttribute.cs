// dnlib: See LICENSE.txt for more info

using System.Collections.Generic;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet {
	/// <summary>
	/// Custom attribute interface. Implemented by <see cref="SecurityAttribute"/> and
	/// <see cref="CustomAttribute"/>
	/// </summary>
	public interface ICustomAttribute {
		/// <summary>
		/// Gets the attribute type
		/// </summary>
		ITypeDefOrRef AttributeType { get; }

		/// <summary>
		/// Gets the full name of the attribute type
		/// </summary>
		string TypeFullName { get; }

		/// <summary>
		/// Gets all named arguments (field and property values)
		/// </summary>
		ThreadSafe.IList<CANamedArgument> NamedArguments { get; }

		/// <summary>
		/// <c>true</c> if <see cref="NamedArguments"/> is not empty
		/// </summary>
		bool HasNamedArguments { get; }

		/// <summary>
		/// Gets all <see cref="CANamedArgument"/>s that are field arguments
		/// </summary>
		IEnumerable<CANamedArgument> Fields { get; }

		/// <summary>
		/// Gets all <see cref="CANamedArgument"/>s that are property arguments
		/// </summary>
		IEnumerable<CANamedArgument> Properties { get; }
	}
}
