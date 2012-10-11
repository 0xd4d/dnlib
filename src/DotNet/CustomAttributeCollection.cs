using System.Collections.Generic;

namespace dot10.DotNet {
	/// <summary>
	/// Stores <see cref="CustomAttribute"/>s
	/// </summary>
	public class CustomAttributeCollection : LazyList<CustomAttribute> {
		/// <summary>
		/// Default constructor
		/// </summary>
		internal CustomAttributeCollection() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="length">Initial length of the list</param>
		/// <param name="context">Context passed to <paramref name="readOriginalValue"/></param>
		/// <param name="readOriginalValue">Delegate instance that returns original values</param>
		internal CustomAttributeCollection(int length, object context, MFunc<object, uint, CustomAttribute> readOriginalValue)
			: base(length, context, readOriginalValue) {
		}
	}
}
